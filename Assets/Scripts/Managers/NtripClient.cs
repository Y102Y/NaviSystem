using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;

public class NtripClient : MonoBehaviour, IDisposable
{
    [Header("NTRIP Settings")]
    public string server = "ntrip.server.com";
    public int port = 2101;
    public string mountPoint = "RTCM32";
    public string username = "yourUsername";
    public string password = "yourPassword";

    public delegate void NMEADataReceivedHandler(string nmeaData);
    public event NMEADataReceivedHandler OnNMEADataReceived;

    private TcpClient client;
    private NetworkStream stream;
    private Thread readThread;
    private bool isRunning = false;

    // スレッド間で共有するキュー
    private ConcurrentQueue<string> nmeaDataQueue = new ConcurrentQueue<string>();

    void Start()
    {
        StartNtripClient();
    }

    void Update()
    {
        // メインスレッドで NMEA データを処理
        while (nmeaDataQueue.TryDequeue(out string nmeaData))
        {
            Debug.Log($"Received NMEA Data: {nmeaData}");
            OnNMEADataReceived?.Invoke(nmeaData);
        }
    }

    void OnDestroy()
    {
        Dispose();
    }

    public void StartNtripClient()
    {
        try
        {
            client = new TcpClient(server, port);
            stream = client.GetStream();

            // NTRIPリクエストの送信
            string request = $"GET /{mountPoint} HTTP/1.1\r\n" +
                             $"Host: {server}:{port}\r\n" +
                             "User-Agent: NTRIP UnityClient\r\n" +
                             $"Authorization: Basic {Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{password}"))}\r\n" +
                             "\r\n";
            byte[] requestBytes = System.Text.Encoding.ASCII.GetBytes(request);
            stream.Write(requestBytes, 0, requestBytes.Length);

            isRunning = true;
            readThread = new Thread(ReadData);
            readThread.Start();

            Debug.Log($"NTRIPクライアントが {server}:{port} に接続しました。");
        }
        catch (Exception e)
        {
            Debug.LogError($"NTRIPクライアントの接続に失敗しました: {e.Message}");
        }
    }

    private void ReadData()
    {
        try
        {
            while (isRunning && client.Connected)
            {
                if (stream.DataAvailable)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        string receivedData = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Debug.Log($"Ntrip Data Received: {receivedData}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ntrip Client Read Error: {e.Message}");
        }
    }


    public void Dispose()
    {
        isRunning = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join();
        }

        if (stream != null)
        {
            stream.Close();
        }

        if (client != null && client.Connected)
        {
            client.Close();
        }

        Debug.Log("NTRIPクライアントが停止しました。");
    }
}
