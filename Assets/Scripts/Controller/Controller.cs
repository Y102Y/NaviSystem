using UnityEngine;

public class Controller : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform; // プレイヤーのTransform
    public float smoothness = 0.1f; // プレイヤーの移動の滑らかさ

    [Header("Ntrip Client")]
    public NtripClient ntripClient; // NtripClientをインスペクターでアタッチ

    // Unityの基準点（原点となる緯度経度高度）
    private double originLatitude = 35.665576580782; // 仮の緯度
    private double originLongitude = 140.071367856623; // 仮の経度
    private double originAltitude = 0.0; // 仮の高度

    private Vector3 targetPosition; // プレイヤーの目標位置

    private void Start()
    {
        if (ntripClient != null)
        {
            // NMEAデータ受信イベントの登録
            ntripClient.OnNMEADataReceived += OnNMEADataReceived;

            // NtripClientを開始
            ntripClient.StartNtripClient();
        }
        else
        {
            Debug.LogError("NtripClientがアタッチされていません。");
        }

        // 初期位置を設定
        targetPosition = playerTransform.position;
    }

    private void Update()
    {
        // プレイヤーの位置を更新
        UpdatePlayerPosition();
    }

    private void OnNMEADataReceived(string nmeaSentence)
    {
        Debug.Log($"Received NMEA Data: {nmeaSentence}");

        // NMEAデータを解析
        var parsedData = NmeaParser.Parse(nmeaSentence);

        if (parsedData != null)
        {
            Debug.Log($"Parsed Data - Latitude: {parsedData.Latitude}, Longitude: {parsedData.Longitude}, Altitude: {parsedData.Altitude}");

            // 緯度経度高度をUnity座標に変換
            targetPosition = ConvertGeographicToUnity(parsedData.Latitude, parsedData.Longitude, parsedData.Altitude);
        }
    }

    private void UpdatePlayerPosition()
    {
        if (playerTransform != null)
        {
            // プレイヤーの位置を滑らかに補間
            playerTransform.position = Vector3.Lerp(playerTransform.position, targetPosition, smoothness);
        }
    }

    /// <summary>
    /// 地理座標（緯度、経度、高度）をUnityの座標系に変換します。
    /// </summary>
    private Vector3 ConvertGeographicToUnity(double latitude, double longitude, double altitude)
    {
        const double EarthRadius = 6378137.0; // 地球の半径（メートル）

        // 緯度・経度の差分をラジアンに変換
        double latDiff = (latitude - originLatitude) * Mathf.Deg2Rad;
        double lonDiff = (longitude - originLongitude) * Mathf.Deg2Rad;

        // 緯度をラジアンに変換してコサインを計算
        double x = EarthRadius * lonDiff * Mathf.Cos((float)originLatitude * Mathf.Deg2Rad);
        double z = EarthRadius * latDiff;

        // 高度をY座標に反映
        float y = (float)(altitude - originAltitude);

        return new Vector3((float)x, y, (float)z);
    }

    private void OnDestroy()
    {
        if (ntripClient != null)
        {
            ntripClient.OnNMEADataReceived -= OnNMEADataReceived;
        }
    }
}
