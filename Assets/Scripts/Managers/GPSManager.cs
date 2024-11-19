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

        // 必要なセンテンス（GGAまたはRMC）以外をスキップ
        if (!(nmeaData.StartsWith("$GNGGA") || nmeaData.StartsWith("$GNRMC")))
        {
            Debug.Log($"Non-relevant NMEA sentence received: {nmeaData}");
            return;
        }

        // NMEAデータを解析
        var parsedData = NmeaParser.Parse(nmeaData);

        if (parsedData != null)
        {
            latitude = parsedData.Latitude;
            longitude = parsedData.Longitude;
            altitude = parsedData.Altitude;

            Debug.Log($"Valid NMEA Data Parsed - Latitude: {latitude}, Longitude: {longitude}, Altitude: {altitude}");
        }
        else
        {
            Debug.LogWarning($"Failed to parse NMEA sentence: {nmeaData}");
        }
    }


    void Update()
    {
        // 必要であればUnityの座標系に変換して使用
    }
}
