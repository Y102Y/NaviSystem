using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UnityTcpServer : MonoBehaviour
{
    // サーバーのポート
    private int PORT = 9999; // Androidアプリと一致させる

    private TcpListener server;
    private bool isRunning = false;
    private Thread serverThread;

    public static float pitch = 0.0f;
    public static float roll = 0.0f;
    public static float azimuth = 0.0f;
    public static double latitude = 0.0f;
    public static double longitude = 0.0f;

    // 受信データ用
    private StringBuilder buffer = new StringBuilder();

    // データ受信時に呼び出されるデリゲート
    public delegate void DataReceivedHandler(float azimuth, float pitch, float roll);
    public event DataReceivedHandler OnDataReceived;

    void Start()
    {
        StartServer();
    }

    void OnApplicationQuit()
    {
        StopServer();
    }

    public void StartServer()
    {
        if (isRunning)
            return;

        serverThread = new Thread(new ThreadStart(RunServer));
        serverThread.IsBackground = true;
        serverThread.Start();
        isRunning = true;
        Debug.Log($"サーバーを起動しました。ポート {PORT} で待機中...");
    }

    public void StopServer()
    {
        if (!isRunning)
            return;

        isRunning = false;
        server?.Stop();
        if (serverThread != null && serverThread.IsAlive)
        {
            serverThread.Join();
        }
        Debug.Log("サーバーを停止しました。");
    }

    private void RunServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, PORT);
            server.Start();
            Debug.Log("TCPサーバーが開始されました。");

            while (isRunning)
            {
                // クライアントからの接続をブロックして待機
                TcpClient client = server.AcceptTcpClient();
                Debug.Log($"接続されました: {client.Client.RemoteEndPoint}");

                // クライアントの処理を別スレッドで行う
                Thread clientThread = new Thread(() => HandleClient(client));
                clientThread.IsBackground = true;
                clientThread.Start();
            }
        }
        catch (SocketException se)
        {
            if (isRunning)
            {
                Debug.LogError($"ソケットエラー: {se.Message}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"エラー: {ex.Message}");
        }
        finally
        {
            server?.Stop();
            Debug.Log("TCPサーバーが停止されました。");
        }
    }

    private void HandleClient(TcpClient client)
    {
        try
        {
            using (client)
            {
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] dataBuffer = new byte[1024];
                    int bytesRead;

                    while (isRunning && (bytesRead = stream.Read(dataBuffer, 0, dataBuffer.Length)) != 0)
                    {
                        string receivedData = Encoding.UTF8.GetString(dataBuffer, 0, bytesRead);
                        // Debug.Log($"受信データ: {receivedData}");
                        ProcessData(receivedData);
                    }
                }
                Debug.Log("接続が切断されました。");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"クライアント処理中のエラー: {ex.Message}");
        }
    }

    private void ProcessData(string data)
    {
        buffer.Append(data);
        string bufferContent = buffer.ToString();

        int newlineIndex;
        while ((newlineIndex = bufferContent.IndexOf('\n')) >= 0)
        {
            string line = bufferContent.Substring(0, newlineIndex).Trim();
            buffer.Remove(0, newlineIndex + 1);
            bufferContent = buffer.ToString();

            if (!string.IsNullOrEmpty(line))
            {
                string[] parts = line.Split(',');
                if (parts.Length == 5)
                {
                    if (float.TryParse(parts[0], out float azimuth) &&
                        float.TryParse(parts[1], out float pitch) &&
                        float.TryParse(parts[2], out float roll) &&
                        double.TryParse(parts[3], out double latitude) &&
                        double.TryParse(parts[4], out double longitude))
                    {
                        // Debug.Log("L: " + parts[3] + " LO: " + parts[4]);
                        Debug.Log("2L: " + latitude + " 2LO: " + longitude);
                        UnityTcpServer.azimuth = azimuth;
                        UnityTcpServer.pitch = pitch;
                        UnityTcpServer.roll = roll;
                        UnityTcpServer.latitude = latitude;
                        UnityTcpServer.longitude = longitude;
                        // 必要に応じてイベントを発火
                        OnDataReceived?.Invoke(azimuth, pitch, roll);
                    }
                    else
                    {
                        Debug.LogWarning($"受信エラー（数値変換失敗）: {line}");
                    }
                }
                else
                {
                    Debug.LogWarning($"受信エラー（データ形式不正）: {line}");
                }
            }
        }
    }
}
