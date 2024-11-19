using UnityEngine;
using TMPro;

public class LocationDisplay : MonoBehaviour
{
    public TextMeshProUGUI locationText;
    public SerialPortManager serialPortManager;

    private void Start()
    {
        if (serialPortManager != null)
        {
            serialPortManager.OnNMEADataReceived += OnNMEADataReceived;
        }
    }

    private void OnNMEADataReceived(string nmeaSentence)
    {
        var parsedData = NmeaParser.Parse(nmeaSentence);

        if (parsedData != null)
        {
            string latitude = parsedData.Latitude.ToString("F6");
            string longitude = parsedData.Longitude.ToString("F6");
            string altitude = parsedData.Altitude.ToString("F2");

            locationText.text = $"Lat: {latitude}, Lon: {longitude}, Alt: {altitude} m";
        }
    }

    private void OnDestroy()
    {
        if (serialPortManager != null)
        {
            serialPortManager.OnNMEADataReceived -= OnNMEADataReceived;
        }
    }
}
