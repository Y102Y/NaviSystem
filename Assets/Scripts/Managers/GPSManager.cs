using UnityEngine;

public class GPSManager : MonoBehaviour
{
    public SerialPortManager serialPortManager;

    private double latitude;
    private double longitude;
    private double altitude;

    void OnEnable()
    {
        if (serialPortManager != null)
        {
            serialPortManager.OnNMEADataReceived += OnNMEADataReceived;
        }
    }

    void OnDisable()
    {
        if (serialPortManager != null)
        {
            serialPortManager.OnNMEADataReceived -= OnNMEADataReceived;
        }
    }

    private void OnNMEADataReceived(string nmeaData)
    {
        // 無効なデータをスキップ
        if (string.IsNullOrWhiteSpace(nmeaData) || !nmeaData.StartsWith("$"))
        {
            Debug.LogWarning("Invalid NMEA data received, skipping...");
            return;
        }

        // GGAセンテンスのみ処理
        if (!nmeaData.StartsWith("$GNGGA") && !nmeaData.StartsWith("$GPGGA"))
        {
            // 他のセンテンスのログを省略
            return;
        }

        // NMEAデータを解析
        var parsedData = NmeaParser.Parse(nmeaData);

        if (parsedData != null)
        {
            latitude = parsedData.Latitude;
            longitude = parsedData.Longitude;
            altitude = parsedData.Altitude;

            Debug.Log($"Valid GGA Data Parsed - Latitude: {latitude}, Longitude: {longitude}, Altitude: {altitude}");
        }
        else
        {
            Debug.LogWarning($"Failed to parse GGA sentence: {nmeaData}");
        }
    }

    void Update()
    {
        // 必要であればUnityの座標系に変換して使用
    }
}
