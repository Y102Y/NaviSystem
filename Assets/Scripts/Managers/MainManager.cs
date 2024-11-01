// Assets/Scripts/Managers/MainManager.cs

using UnityEngine;
using System; // 必要な名前空間のインポート

public class MainManager : MonoBehaviour
{
    [Header("Serial Manager")]
    public SerialManager serialManager; // シリアルマネージャーの参照

    [Header("NTRIP Settings")]
    public string ntripServer = "ntrip.ales-corp.co.jp";
    public int ntripPort = 2101;
    public string mountPoint = "RTCM32M5S";
    public string username = "psu7f04d";
    public string password = "67q9bj";

    [Header("Object Manager")]
    public ObjectManager objectManager;

    private NtripClient ntripClient;

    void Start()
    {
        if (SerialManager.Instance == null)
        {
            Logger.LogError("SerialManager が存在しません。");
            return;
        }

        serialManager = SerialManager.Instance;

        // シリアルデータ受信イベントの登録
        serialManager.OnDataReceived += OnSerialDataReceived;

        // RTCMデータ受信イベントの登録
        serialManager.OnRTCMDataReceived += OnRTCMDataReceived;

        // NTRIPクライアントの初期化と開始
        ntripClient = new NtripClient(ntripServer, ntripPort, mountPoint, username, password, serialManager);
        ntripClient.OnRTCMDataReceived += OnRTCMDataReceivedFromNTRIP;
        ntripClient.Start();

        Logger.LogInfo("MainManager initialized successfully.");

        // ログファイルのパスを出力（テスト用）
        Logger.LogFilePath();

        // テストログの追加
        Logger.LogInfo("これはテストログです。log.txt に出力されるはずです。");
        Logger.LogDebug("これはデバッグテストログです。");
    }

    // シリアルポートからのNMEAデータ受信時の処理
    private void OnSerialDataReceived(string data)
    {
        Logger.LogDebug($"Received NMEA Data: {data}");

        // データを個別のNMEAセンテンスに分割
        string[] sentences = data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string sentence in sentences)
        {
            // NMEAセンテンスごとに解析
            NmeaParser.NmeaData parsedData = NmeaParser.Parse(sentence);
            if (parsedData != null)
            {
                Logger.LogInfo($"Parsed NMEA Data - Lat: {parsedData.Latitude}, Lon: {parsedData.Longitude}, Alt: {parsedData.Altitude}, Heading: {parsedData.Heading}");

                // オブジェクトの位置を更新
                if (objectManager != null)
                {
                    // 緯度経度をUnityの座標系に適切に変換する
                    Vector3 position = ConvertGeographicToUnity(parsedData.Latitude, parsedData.Longitude, parsedData.Altitude);
                    objectManager.UpdateObjectPosition(position);

                    // オブジェクトの向きをヘディングに基づいて更新
                    objectManager.UpdateObjectRotation(parsedData.Heading);
                }
                else
                {
                    Logger.LogWarning("ObjectManager がアタッチされていません。");
                }
            }
            else
            {
                Logger.LogDebug("NMEAデータの解析に失敗しました。");
            }
        }
    }

    // シリアルポートからのRTCMデータ受信時の処理
    private void OnRTCMDataReceived(byte[] rtcmData)
    {
        Logger.LogDebug($"Received RTCM data: {rtcmData.Length} bytes");

        if (rtcmData.Length > 0)
        {
            // データの一部をログに出力（バイナリデータなので、バイト値を16進数で表示）
            string hexData = BitConverter.ToString(rtcmData);
            Logger.LogDebug($"RTCM Data Hex: {hexData}");
        }

        // RTCMデータをNtripClientに送信して補正を適用
        if (ntripClient != null)
        {
            ntripClient.SendRTCMData(rtcmData);
        }
    }

    // NTRIPクライアントからのRTCMデータ受信時の処理
    private void OnRTCMDataReceivedFromNTRIP(byte[] rtcmData)
    {
        // RTCMデータをシリアルポートに送信
        if (serialManager.IsOpen)
        {
            serialManager.WriteBytes(rtcmData);
            Logger.LogDebug($"Sent RTCM data to serial port: {rtcmData.Length} bytes");
        }
        else
        {
            Logger.LogWarning("シリアルポートが開かれていません。RTCMデータを送信できません。");
        }
    }

    void OnDestroy()
    {
        // クリーンアップ
        if (ntripClient != null)
        {
            ntripClient.Dispose();
        }

        if (serialManager != null)
        {
            serialManager.Dispose();
        }
    }

    /// <summary>
    /// 地理座標（緯度、経度、高度）をUnityの座標系に変換します。
    /// </summary>
    /// <param name="latitude">緯度（度）。</param>
    /// <param name="longitude">経度（度）。</param>
    /// <param name="altitude">高度（メートル）。</param>
    /// <returns>Unityの座標系に変換されたVector3。</returns>
    private Vector3 ConvertGeographicToUnity(double latitude, double longitude, double altitude)
    {
        // ここでは、緯度をZ軸、経度をX軸、高度をY軸にマッピングします。
        // 実際の用途に応じてスケールや変換を調整してください。
        // 例えば、1度を1000ユニットとするなど

        float scale = 1000f; // スケール調整（例）

        float x = (float)(longitude * scale);
        float y = (float)altitude;
        float z = (float)(latitude * scale);

        return new Vector3(x, y, z);
    }
}
