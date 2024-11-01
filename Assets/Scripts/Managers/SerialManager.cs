// Assets/Scripts/Managers/SerialManager.cs

using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

public class SerialManager : MonoBehaviour, IDisposable
{
    [Header("Serial Port Settings")]
    public string portName = "COM3"; // シリアルポート名
    public int baudRate = 9600; // ボーレート

    private SerialPort serialPort_;
    private Thread readThread_;
    private bool isRunning_ = false;

    // データ受信イベントの定義
    public delegate void DataReceivedHandler(string data);
    public event DataReceivedHandler OnDataReceived;

    // RTCMデータ受信イベントの定義
    public delegate void RTCMDataReceivedHandler(byte[] data);
    public event RTCMDataReceivedHandler OnRTCMDataReceived;

    // スレッドセーフなキューの追加
    private Queue<string> receivedDataQueue = new Queue<string>();
    private Queue<byte[]> rtcmDataQueue = new Queue<byte[]>();
    private object queueLock = new object();

    public bool IsOpen => serialPort_ != null && serialPort_.IsOpen;

    // シングルトンインスタンス
    private static SerialManager _instance;
    public static SerialManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 新しいGameObjectを作成し、SerialManagerをアタッチ
                GameObject obj = new GameObject("SerialManager");
                _instance = obj.AddComponent<SerialManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            OpenSerialPort();
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
        }
    }

    private void OpenSerialPort()
    {
        try
        {
            serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort_.ReadTimeout = 1000; // タイムアウト時間の設定（ミリ秒）
            serialPort_.Open();
            isRunning_ = true;
            readThread_ = new Thread(ReadSerialPort);
            readThread_.Start();
            Logger.LogInfo($"シリアルポート {portName} が開かれました。");
        }
        catch (Exception e)
        {
            Logger.LogError($"シリアルポートのオープンに失敗しました: {e.Message}");
        }
    }

    private void ReadSerialPort()
    {
        while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                // 非ブロッキングでデータを読み取る
                int bytesToRead = serialPort_.BytesToRead;
                if (bytesToRead > 0)
                {
                    byte[] buffer = new byte[bytesToRead];
                    serialPort_.Read(buffer, 0, bytesToRead);

                    // RTCMデータの判定
                    if (IsRTCMData(buffer))
                    {
                        lock (queueLock)
                        {
                            rtcmDataQueue.Enqueue(buffer);
                        }
                        // RTCMデータの詳細なログ出力
                        string hexData = BitConverter.ToString(buffer);
                        Logger.LogDebug($"RTCMデータ受信: {hexData}");
                    }
                    else
                    {
                        string line = System.Text.Encoding.ASCII.GetString(buffer).Trim();
                        lock (queueLock)
                        {
                            receivedDataQueue.Enqueue(line);
                        }
                        Logger.LogDebug($"NMEAデータ受信: {line}");
                    }
                }
                else
                {
                    Thread.Sleep(10); // 少し待機してから再試行
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"シリアル読み取りエラー: {e.Message}");
                isRunning_ = false;
            }
        }
    }

    void Update()
    {
        // メインスレッドでキューからデータを処理
        lock (queueLock)
        {
            while (receivedDataQueue.Count > 0)
            {
                string data = receivedDataQueue.Dequeue();
                OnDataReceived?.Invoke(data);
            }

            while (rtcmDataQueue.Count > 0)
            {
                byte[] data = rtcmDataQueue.Dequeue();
                OnRTCMDataReceived?.Invoke(data);
            }
        }
    }

    /// <summary>
    /// RTCMデータかどうかを判定します。
    /// RTCM3.0のスタートフレームは0xD3 0x00で始まる
    /// </summary>
    /// <param name="buffer">受信したバイトデータ。</param>
    /// <returns>RTCMデータであればtrue、そうでなければfalse。</returns>
    private bool IsRTCMData(byte[] buffer)
    {
        if (buffer.Length < 2)
            return false;

        // RTCM3.0のスタートフレームは0xD3 0x00で始まる
        return buffer[0] == 0xD3 && buffer[1] == 0x00;
    }

    /// <summary>
    /// 文字列データをシリアルポートに送信します。
    /// </summary>
    /// <param name="message">送信する文字列メッセージ。</param>
    public void Write(string message)
    {
        if (serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                serialPort_.WriteLine(message);
                Logger.LogInfo($"シリアルポートに送信: {message}");
            }
            catch (Exception e)
            {
                Logger.LogError($"シリアル書き込みエラー: {e.Message}");
            }
        }
        else
        {
            Logger.LogWarning("シリアルポートが開かれていません。");
        }
    }

    /// <summary>
    /// バイトデータをシリアルポートに送信します。
    /// </summary>
    /// <param name="data">送信するバイト配列。</param>
    public void WriteBytes(byte[] data)
    {
        if (serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                serialPort_.Write(data, 0, data.Length);
                Logger.LogInfo($"シリアルポートにバイトデータを送信: {data.Length} bytes");
            }
            catch (Exception e)
            {
                Logger.LogError($"シリアル書き込みエラー: {e.Message}");
            }
        }
        else
        {
            Logger.LogWarning("シリアルポートが開かれていません。");
        }
    }

    /// <summary>
    /// シリアルポートを閉じ、リソースを解放します。
    /// </summary>
    public void Dispose()
    {
        CloseSerialPort();
    }

    private void CloseSerialPort()
    {
        try
        {
            isRunning_ = false;
            if (readThread_ != null && readThread_.IsAlive)
            {
                readThread_.Join();
            }
            if (serialPort_ != null && serialPort_.IsOpen)
            {
                serialPort_.Close();
                Logger.LogInfo($"シリアルポート {portName} が閉じられました。");
            }
        }
        catch (Exception e)
        {
            Logger.LogError($"シリアルポートのクローズに失敗しました: {e.Message}");
        }
    }

    void OnDestroy()
    {
        Dispose();
    }
}
