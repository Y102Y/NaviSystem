using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Collections.Generic;

public class SerialPortManager : MonoBehaviour
{
    [Header("Serial Port Settings")]
    public string portName = "COM3"; // シリアルポート名
    public int baudRate = 9600; // ボーレート

    private SerialPort serialPort_;
    private Thread readThread_;
    private bool isRunning_ = false;

    // データ受信イベントの定義
    public event Action<string> OnNMEADataReceived;

    private Queue<string> dataQueue = new Queue<string>();
    private object queueLock = new object();

    // 最後にNMEAデータを処理した時間
    private DateTime lastProcessedTime = DateTime.MinValue;

    // 受信間隔（秒）
    public float receiveInterval = 1.0f;

    void Start()
    {
        OpenSerialPort();
    }

    private void OpenSerialPort()
    {
        try
        {
            serialPort_ = new SerialPort(portName, baudRate);
            serialPort_.ReadTimeout = 1000; // タイムアウト設定（ミリ秒）
            serialPort_.Open();

            isRunning_ = true;
            readThread_ = new Thread(ReadSerialPort);
            readThread_.Start();

            Debug.Log($"SerialPort {portName} opened successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to open SerialPort: {e.Message}");
        }
    }

    private void ReadSerialPort()
    {
        while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                string line = serialPort_.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    lock (queueLock)
                    {
                        dataQueue.Enqueue(line);
                    }
                }
            }
            catch (TimeoutException)
            {
                // タイムアウトエラーは無視
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading from SerialPort: {e.Message}");
                isRunning_ = false;
            }
        }
    }

    void Update()
    {
        lock (queueLock)
        {
            while (dataQueue.Count > 0)
            {
                // キューからデータを取得
                string data = dataQueue.Dequeue();

                // 現在の時間を取得
                DateTime currentTime = DateTime.Now;

                // 前回の処理から指定間隔が経過しているか確認
                if ((currentTime - lastProcessedTime).TotalSeconds >= receiveInterval)
                {
                    lastProcessedTime = currentTime; // 最後の処理時間を更新
                    OnNMEADataReceived?.Invoke(data);
                }
            }
        }
    }

    public void Dispose()
    {
        isRunning_ = false;

        if (readThread_ != null && readThread_.IsAlive)
        {
            readThread_.Join();
        }

        if (serialPort_ != null && serialPort_.IsOpen)
        {
            serialPort_.Close();
        }

        Debug.Log("SerialPort closed and disposed.");
    }

    void OnDestroy()
    {
        Dispose();
    }
}
