using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class SerialPortManager : MonoBehaviour
{
    [Header("Serial Port Settings")]
    public string portName = "COM3"; // デバイスのCOMポート番号
    public int baudRate = 9600;      // ボーレート（DG-PRO1RWSの場合は通常9600）

    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning = false;

    public delegate void NMEADataReceivedHandler(string data);
    public event NMEADataReceivedHandler OnNMEADataReceived;

    void Start()
    {
        OpenSerialPort();
    }

    private void OpenSerialPort()
    {
        try
        {
            serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort.ReadTimeout = 1000; // 1秒のタイムアウト
            serialPort.Open();
            isRunning = true;

            // シリアル読み取りスレッドを開始
            readThread = new Thread(ReadSerialPort);
            readThread.Start();

            Debug.Log($"SerialPort {portName} opened successfully.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open SerialPort: {e.Message}");
        }
    }

    private void ReadSerialPort()
    {
        try
        {
            while (isRunning && serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    // シリアルポートから1行を読み取る
                    string line = serialPort.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        // GGAセンテンス以外を完全に排除
                        if (line.StartsWith("$GPGGA") || line.StartsWith("$GNGGA"))
                        {
                            Debug.Log($"Valid GGA Data Received: {line}");
                            OnNMEADataReceived?.Invoke(line); // イベントをトリガー
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // タイムアウトは無視
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading SerialPort: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        CloseSerialPort();
    }

    private void CloseSerialPort()
    {
        isRunning = false;
        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join();
        }

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log($"SerialPort {portName} closed.");
        }
    }
}
