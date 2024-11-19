using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;

public class NtripClient : MonoBehaviour, IDisposable
{
    [Header("NTRIP Settings")]
    [Tooltip("NTRIPサーバーのアドレス")]
    public string server = "ntrip.server.com";

    [Tooltip("NTRIPサーバーのポート")]
    public int port = 2101;

    [Tooltip("マウントポイント")]
    public string mountPoint = "RTCM32";

    [Tooltip("ユーザー名")]
    public string username = "yourUsername";

    [Tooltip("パスワード")]
    public string password = "yourPassword";

    public delegate void NMEADataReceivedHandler(string nmeaData);
    public event NMEADataReceivedHandler OnNMEADataReceived;

    public delegate void RTCMDataReceivedHandler(byte[] rtcmData);
    public event RTCMDataReceivedHandler OnRTCMDataReceived;

    private TcpClient client;
    private NetworkStream stream;
    private Thread readThread;
    private bool isRunning = false;

    void Start()
    {
        StartNtripClient();
    }

    void OnDestroy()
    {
        Dispose();
    }

    /// <summary>
    /// NTRIPクライアントを開始します。
    /// </summary>
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

    /// <summary>
    /// NTRIPサーバーからデータを読み取ります。
    /// </summary>
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
                        string nmeaSentence = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);

                        if (nmeaSentence.StartsWith("$"))
                        {
                            OnNMEADataReceived?.Invoke(nmeaSentence);
                        }
                        else
                        {
                            byte[] rtcmData = new byte[bytesRead];
                            Array.Copy(buffer, rtcmData, bytesRead);
                            OnRTCMDataReceived?.Invoke(rtcmData);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"NTRIPクライアントの読み取りエラー: {e.Message}");
        }
    }

    /// <summary>
    /// NTRIPクライアントを停止し、リソースを解放します。
    /// </summary>
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
