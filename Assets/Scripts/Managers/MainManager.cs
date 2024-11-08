// Assets/Scripts/Managers/MainManager.cs

using UnityEngine;
using System;
using System.Collections.Generic;

public class MainManager : MonoBehaviour
{
    [Header("Serial Manager")]
    [SerializeField]
    private MonoBehaviour serialManagerBehaviour; // シリアルマネージャーの参照（MonoBehaviour型）

    private ISerialManager serialManager; // インターフェース型の参照

    [Header("NTRIP Settings")]
    public string ntripServer = "ntrip.ales-corp.co.jp";
    public int ntripPort = 2101;
    public string mountPoint = "RTCM32M5S";
    public string username = "psu7f04d";
    public string password = "67q9bj";

    [Header("Routes Data")]
    public List<RouteData> routes = new List<RouteData>(); // 複数ルートを管理

    public RouteManager routeManager; // RouteManagerへの参照

    public float maxVisibleDistance = 50.0f; // 最大可視距離（メートル）

    // ユーザーの現在位置とヘディング
    private double currentLatitude;
    private double currentLongitude;
    private double currentAltitude;
    private double currentHeading;

    // 原点設定
    private bool originSet = false;
    private double originLatitude;
    private double originLongitude;
    private double originAltitude;

    private NtripClient ntripClient;

    void Start()
    {
        if (serialManagerBehaviour != null)
        {
            serialManager = serialManagerBehaviour as ISerialManager;
            if (serialManager == null)
            {
                Logger.LogError("Assigned SerialManager does not implement ISerialManager.");
                return;
            }
        }
        else
        {
            Logger.LogError("SerialManager が設定されていません。");
            return;
        }

        // シリアルデータ受信イベントの登録
        serialManager.OnDataReceived += OnSerialDataReceived;

        // RTCMデータ受信イベントの登録
        serialManager.OnRTCMDataReceived += OnRTCMDataReceived;

        // NTRIPクライアントの初期化と開始（RTCMデータの受信が未完成のため、一旦無視）
        // ntripClient = new NtripClient(ntripServer, ntripPort, mountPoint, username, password, serialManager);
        // ntripClient.OnRTCMDataReceived += OnRTCMDataReceivedFromNTRIP;
        // ntripClient.Start();

        Logger.LogInfo("MainManager initialized successfully.");

        // ログファイルのパスを出力（テスト用）
        Logger.LogFilePath();

        // RouteManager の参照を取得
        if (routeManager == null)
        {
            routeManager = FindObjectOfType<RouteManager>();
            if (routeManager == null)
            {
                Logger.LogError("MainManager: RouteManager がシーン内に存在しません。");
                return;
            }

            // RouteManager にルートデータを設定
            routeManager.routes = routes;
            // originCoordinates は RouteManager の Inspector から設定するか、以下のように固定値を設定
            // routeManager.originCoordinates = new Vector2((float)originLatitude, (float)originLongitude);
            // objectManager は RouteManager が自動的に取得するため、ここでは設定不要
        }

        // カメラの背景を透明に設定（透過型ARグラスの場合）
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0, 0, 0, 0);
    }

    void Update()
    {
        // シリアル通信を通じて受信したGPSデータを処理

        // 現在の位置とヘディングが更新された場合、RouteManagerに渡す
        if (routeManager != null)
        {
            Vector3 userPosition = ConvertGeographicToUnity(currentLatitude, currentLongitude, currentAltitude);
            float userHeading = (float)currentHeading;
            routeManager.UpdateNavigation(userPosition, userHeading);
        }
    }

    // シリアルポートからのNMEAデータ受信時の処理
    private void OnSerialDataReceived(string data)
    {
        Logger.LogDebug($"Received NMEA Data: {data}");

        // データを個別のNMEAセンテンスに分割
        string[] sentences = data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string sentence in sentences)
        {
            Logger.LogDebug($"Parsing NMEA Sentence: {sentence}"); // 追加

            // NMEAセンテンスごとに解析
            NmeaParser.NmeaData parsedData = NmeaParser.Parse(sentence);
            if (parsedData != null)
            {
                Logger.LogInfo($"Parsed NMEA Data - Lat: {parsedData.Latitude}, Lon: {parsedData.Longitude}, Alt: {parsedData.Altitude}, Heading: {parsedData.Heading}");

                // 現在の位置とヘディングを更新
                currentLatitude = parsedData.Latitude;
                currentLongitude = parsedData.Longitude;
                currentAltitude = parsedData.Altitude;
                currentHeading = parsedData.Heading;
            }
            else
            {
                Logger.LogDebug("NMEAデータの解析に失敗しました。");
            }
        }
    }

    // シリアルポートからのRTCMデータ受信時の処理（未使用の場合はコメントアウト）
    private void OnRTCMDataReceived(byte[] rtcmData)
    {
        // Logger.LogDebug($"Received RTCM data: {rtcmData.Length} bytes");
        // 必要に応じて処理を実装
    }

    // NTRIPクライアントからのRTCMデータ受信時の処理（未使用の場合はコメントアウト）
    private void OnRTCMDataReceivedFromNTRIP(byte[] rtcmData)
    {
        // 必要に応じて処理を実装
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
    private Vector3 ConvertGeographicToUnity(double latitude, double longitude, double altitude)
    {
        // 原点（基準点）を設定
        if (!originSet)
        {
            originLatitude = latitude;
            originLongitude = longitude;
            originAltitude = altitude;
            originSet = true;
        }

        // 地球半径（メートル）
        const double EarthRadius = 6378137.0;

        // 緯度・経度の差分をラジアンに変換
        double latDiff = ((float)(latitude - originLatitude)) * Mathf.Deg2Rad;
        double lonDiff = ((float)(longitude - originLongitude)) * Mathf.Deg2Rad;

        // 緯度をラジアンに変換してコサインを計算
        double x = EarthRadius * lonDiff * Math.Cos(((float)latitude) * Mathf.Deg2Rad);
        double z = EarthRadius * latDiff;

        float y = (float)(altitude - originAltitude);

        return new Vector3((float)x, y, (float)z);
    }
}
