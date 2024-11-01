// NtripClient.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class NtripClient : IDisposable
{
    private string server;
    private int port;
    private string mountpoint;
    private string username;
    private string password;
    private SerialManager serialManager;
    private Thread ntripThread;
    private bool isRunning;
    private string rtcmFilePath;

    public NtripClient(string server, int port, string mountpoint, string username, string password, SerialManager serialManager)
    {
        this.server = server;
        this.port = port;
        this.mountpoint = mountpoint;
        this.username = username;
        this.password = password;
        this.serialManager = serialManager;

        // RTCMファイルの保存先を設定
        rtcmFilePath = Path.Combine(Application.persistentDataPath, "rtcm_received.bin");
    }

    public void Start()
    {
        isRunning = true;
        ntripThread = new Thread(NtripLoop);
        ntripThread.Start();
    }

    private void NtripLoop()
    {
        int maxRetries = 5;
        int retryCount = 0;
        int waitTime = 5000; // ミリ秒

        while (isRunning && retryCount < maxRetries)
        {
            try
            {
                Logger.LogInfo("NTRIP接続を開始します...");
                using (TcpClient client = new TcpClient())
                {
                    client.ReceiveTimeout = 30000; // 30秒
                    client.SendTimeout = 30000;

                    client.Connect(server, port);
                    Logger.LogInfo($"NTRIPサーバー {server}:{port} に接続しました。");

                    using (NetworkStream stream = client.GetStream())
                    {
                        // 認証情報をBase64エンコード
                        string credentials = $"{username}:{password}";
                        string encodedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));

                        // NTRIPリクエストヘッダーの準備
                        StringBuilder requestBuilder = new StringBuilder();
                        requestBuilder.Append($"GET /{mountpoint} HTTP/1.1\r\n");
                        requestBuilder.Append($"Host: {server}:{port}\r\n");
                        requestBuilder.Append("User-Agent: NTRIP Unity Client/1.0\r\n");
                        requestBuilder.Append($"Authorization: Basic {encodedCredentials}\r\n");
                        requestBuilder.Append("Ntrip-Version: Ntrip/2.0\r\n");
                        requestBuilder.Append("Accept: */*\r\n");
                        requestBuilder.Append("Connection: close\r\n");
                        requestBuilder.Append("\r\n");

                        string request = requestBuilder.ToString();
                        byte[] requestBytes = Encoding.ASCII.GetBytes(request);
                        stream.Write(requestBytes, 0, requestBytes.Length);
                        Logger.LogDebug("NTRIPリクエストを送信しました。");

                        // レスポンスの受信
                        using (StreamReader reader = new StreamReader(stream, Encoding.ASCII))
                        {
                            string responseLine = reader.ReadLine();
                            Logger.LogDebug($"NTRIPサーバーからのレスポンス: {responseLine}");

                            if (!responseLine.Contains("200 OK"))
                            {
                                Logger.LogError($"NTRIPサーバーからの応答が異常です: {responseLine}");
                                throw new Exception("NTRIPサーバーからの応答が異常です。");
                            }

                            // ヘッダー終了まで読み飛ばす
                            while (!reader.EndOfStream)
                            {
                                string line = reader.ReadLine();
                                if (string.IsNullOrEmpty(line))
                                {
                                    break;
                                }
                            }
                        }

                        Logger.LogInfo("NTRIPサーバーに正常に接続しました。RTCMデータを送信します。");

                        // RTCMデータの受信とRTKデバイスへの送信
                        using (FileStream rtcmFile = new FileStream(rtcmFilePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            while (isRunning && (bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                // シリアルポートにデータを送信（public メソッドを使用）
                                serialManager.WriteData(buffer);

                                // RTCMデータをファイルに保存
                                rtcmFile.Write(buffer, 0, bytesRead);
                                rtcmFile.Flush();

                                Logger.LogDebug($"Wrote {bytesRead} bytes to rtcm_received.bin");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"NTRIP接続エラー: {e.Message}");
            }
            finally
            {
                try
                {
                    // socket のクローズ処理があればここで行う
                }
                catch (Exception e)
                {
                    Logger.LogError($"ソケットクローズ時のエラー: {e.Message}");
                }
            }

            retryCount++;
            Logger.LogInfo($"{retryCount} 回目の再接続を試みます...");
            Thread.Sleep(waitTime);
        }

        if (retryCount >= maxRetries)
        {
            Logger.LogError("最大再接続回数に達しました。NTRIPクライアントを停止します。");
            isRunning = false;
        }
    }

    public void Stop()
    {
        isRunning = false;
        if (ntripThread != null && ntripThread.IsAlive)
        {
            ntripThread.Join();
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
