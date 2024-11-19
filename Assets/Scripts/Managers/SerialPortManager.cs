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
                        // NmeaParserでデータをフィルタリング
                        var parsedData = NmeaParser.Parse(line);

                        if (parsedData != null)
                        {
                            // 有効なGGAデータの場合のみログを表示
                            Debug.Log($"Valid GGA Data: Latitude={parsedData.Latitude}, Longitude={parsedData.Longitude}, Altitude={parsedData.Altitude}");
                            OnNMEADataReceived?.Invoke(line); // イベントをトリガー
                        }
                        else if (line.StartsWith("$"))
                        {
                            // GGA以外のNMEAセンテンスは無視してログ出力
                            Debug.Log($"Non-relevant NMEA sentence received: {line}");
                        }
                        else
                        {
                            // 無効なデータ
                            Debug.LogWarning($"Invalid NMEA sentence: {line}");
                        }
                    }
                }
                catch (TimeoutException)
                {
                    // タイムアウトの場合は無視
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
