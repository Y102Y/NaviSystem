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
        var parsedData = NmeaParser.Parse(nmeaData);

        if (parsedData != null)
        {
            latitude = parsedData.Latitude;
            longitude = parsedData.Longitude;
            altitude = parsedData.Altitude;

            Debug.Log($"Latitude: {latitude}, Longitude: {longitude}, Altitude: {altitude}");
        }
    }

    void Update()
    {
        // 必要であればUnityの座標系に変換して使用
    }
}
