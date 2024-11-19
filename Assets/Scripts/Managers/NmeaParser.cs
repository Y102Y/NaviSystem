using UnityEngine;

public static class NmeaParser
{
    public class NmeaData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
    }

    public static NmeaData Parse(string nmeaSentence)
    {
        if (string.IsNullOrEmpty(nmeaSentence))
            return null;

        string[] parts = nmeaSentence.Split(',');

        if (nmeaSentence.StartsWith("$GNGGA") || nmeaSentence.StartsWith("$GPGGA"))
        {
            return ParseGGA(parts);
        }

        return null;
    }

    private static NmeaData ParseGGA(string[] parts)
    {
        if (parts.Length < 15)
        {
            Debug.LogWarning("GGA sentence has insufficient fields.");
            return null;
        }

        NmeaData data = new NmeaData();

        if (double.TryParse(parts[2], out double rawLat) && parts[3] == "N")
        {
            data.Latitude = rawLat / 100.0;
        }
        else if (double.TryParse(parts[2], out rawLat) && parts[3] == "S")
        {
            data.Latitude = -(rawLat / 100.0);
        }

        if (double.TryParse(parts[4], out double rawLon) && parts[5] == "E")
        {
            data.Longitude = rawLon / 100.0;
        }
        else if (double.TryParse(parts[4], out rawLon) && parts[5] == "W")
        {
            data.Longitude = -(rawLon / 100.0);
        }

        if (double.TryParse(parts[9], out double altitude))
        {
            data.Altitude = altitude;
        }

        return data;
    }
}
