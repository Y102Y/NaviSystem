// SerialManager.cs
using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class SerialManager : IDisposable
{
    // serialPort を private に保ちつつ、必要な操作を提供する
    private SerialPort serialPort;
    private Thread readThread;
    private bool isRunning;
    private Action<string> onDataReceived;

    public SerialManager(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits, Action<string> dataReceivedCallback)
    {
        serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        onDataReceived = dataReceivedCallback;
    }

    public void Start()
    {
        try
        {
            serialPort.Open();
            Logger.LogInfo($"シリアルポート {serialPort.PortName} に接続しました。");
        }
        catch (Exception e)
        {
            Logger.LogError($"シリアルポート {serialPort.PortName} のオープンに失敗しました: {e.Message}");
            throw;
        }

        isRunning = true;
        readThread = new Thread(ReadLoop);
        readThread.Start();
    }

    private void ReadLoop()
    {
        string buffer = string.Empty;
        while (isRunning)
        {
            try
            {
                string data = serialPort.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    buffer += data;
                    while (buffer.Contains("\n"))
                    {
                        int index = buffer.IndexOf('\n');
                        string line = buffer.Substring(0, index).Trim();
                        buffer = buffer.Substring(index + 1);
                        if (line.StartsWith("$"))
                        {
                            onDataReceived?.Invoke(line);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"シリアル通信エラー: {e.Message}");
                Stop();
            }
            Thread.Sleep(10);
        }
    }

    // シリアルポートにデータを書き込むための public メソッドを追加
    public void WriteData(byte[] data)
    {
        if (serialPort.IsOpen)
        {
            serialPort.Write(data, 0, data.Length);
            Logger.LogDebug($"Wrote {data.Length} bytes to serial port.");
        }
        else
        {
            Logger.LogError("シリアルポートが開かれていません。");
        }
    }

    public void Stop()
    {
        isRunning = false;
        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join();
        }

        if (serialPort.IsOpen)
        {
            serialPort.Close();
            Logger.LogInfo($"シリアルポート {serialPort.PortName} をクローズしました。");
        }
    }

    public void Dispose()
    {
        Stop();
        serialPort.Dispose();
    }
}
