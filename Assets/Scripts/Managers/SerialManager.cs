using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

public class SerialManager : MonoBehaviour, ISerialManager // ISerialManagerを実装
{
    [Header("Serial Port Settings")]
    public string portName = "COM3"; // シリアルポート名
    public int baudRate = 9600; // ボーレート

    private SerialPort serialPort_;
    private Thread readThread_;
    private bool isRunning_ = false;

    // データ受信イベントの定義
    public event Action<string> OnDataReceived;
    
    // RTCMデータ受信イベントの定義
    public event Action<byte[]> OnRTCMDataReceived;

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
                string line = serialPort_.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    lock (queueLock)
                    {
                        receivedDataQueue.Enqueue(line);
                    }
                    Logger.LogDebug($"NMEAデータ受信: {line}");
                }
            }
            catch (TimeoutException)
            {
                // タイムアウトは無視
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
    /// 文字列データをシリアルポートに送信します。
    /// </summary>
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
