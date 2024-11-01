// Assets/Scripts/Managers/NtripClient.cs

using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;

public class NtripClient : IDisposable
{
    private string server;
    private int port;
    private string mountPoint;
    private string username;
    private string password;
    private SerialManager serialManager;
    private TcpClient client;
    private NetworkStream stream;
    private Thread readThread;
    private bool isRunning = false;

    public delegate void RTCMDataReceivedHandler(byte[] data);
    public event RTCMDataReceivedHandler OnRTCMDataReceived;

    public NtripClient(string server, int port, string mountPoint, string username, string password, SerialManager serialManager)
    {
        this.server = server;
        this.port = port;
        this.mountPoint = mountPoint;
        this.username = username;
        this.password = password;
        this.serialManager = serialManager;
    }

    public void Start()
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

            Logger.LogInfo($"NTRIPクライアントが {server}:{port} に接続しました。");
        }
        catch (Exception e)
        {
            Logger.LogError($"NTRIPクライアントの接続に失敗しました: {e.Message}");
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
                        byte[] rtcmData = new byte[bytesRead];
                        Array.Copy(buffer, rtcmData, bytesRead);
                        OnRTCMDataReceived?.Invoke(rtcmData);
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
            Logger.LogError($"NTRIPクライアントの読み取りエラー: {e.Message}");
        }
    }

    public void SendRTCMData(byte[] data)
    {
        if (stream != null && stream.CanWrite)
        {
            try
            {
                stream.Write(data, 0, data.Length);
                Logger.LogDebug($"NTRIPクライアントにRTCMデータを送信: {data.Length} bytes");
            }
            catch (Exception e)
            {
                Logger.LogError($"NTRIPクライアントへのRTCMデータ送信エラー: {e.Message}");
            }
        }
        else
        {
            Logger.LogWarning("NTRIPクライアントのストリームが書き込み可能ではありません。");
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
    }
}
