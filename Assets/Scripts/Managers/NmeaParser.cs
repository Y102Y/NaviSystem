using UnityEngine;

public static class NmeaParser
{
    public class NmeaData
    {
        public double Latitude { get; set; }    // 緯度
        public double Longitude { get; set; }   // 経度
        public double Altitude { get; set; }    // 高度
    }

    public static NmeaData Parse(string nmeaSentence)
    {
        // NMEAセンテンスが空か、"$"で始まらない場合は無視
        if (string.IsNullOrEmpty(nmeaSentence) || !nmeaSentence.StartsWith("$"))
        {
            Debug.LogWarning("Invalid NMEA sentence: Does not start with '$'");
            return null;
        }

        // センテンスをカンマで分割
        string[] parts = nmeaSentence.Split(',');

        // GGAセンテンスのみ処理
        if (nmeaSentence.StartsWith("$GPGGA") || nmeaSentence.StartsWith("$GNGGA"))
        {
            return ParseGGA(parts);
        }

        Debug.Log($"Non-relevant NMEA sentence received: {nmeaSentence}");
        return null; // 他のNMEAセンテンスは無視
    }

    private static NmeaData ParseGGA(string[] parts)
    {
        // フィールド数が足りない場合は無視
        if (parts.Length < 15)
        {
            Debug.LogWarning("Invalid GGA sentence: Not enough fields");
            return null;
        }

        var data = new NmeaData();

        try
        {
            // 緯度の解析
            if (double.TryParse(parts[2], out double rawLat) && !string.IsNullOrEmpty(parts[3]))
            {
                string latDir = parts[3];
                data.Latitude = ConvertToDecimalDegrees(rawLat, latDir);
            }
            else
            {
                Debug.LogWarning("Invalid latitude in GGA sentence");
                return null;
            }

            // 経度の解析
            if (double.TryParse(parts[4], out double rawLon) && !string.IsNullOrEmpty(parts[5]))
            {
                string lonDir = parts[5];
                data.Longitude = ConvertToDecimalDegrees(rawLon, lonDir);
            }
            else
            {
                Debug.LogWarning("Invalid longitude in GGA sentence");
                return null;
            }

            // 高度の解析
            if (double.TryParse(parts[9], out double altitude))
            {
                data.Altitude = altitude;
            }
            else
            {
                Debug.LogWarning("Invalid altitude in GGA sentence");
                data.Altitude = 0.0; // 高度がない場合はデフォルト値
            }
        }
        catch
        {
            Debug.LogError("Error parsing GGA sentence");
            return null;
        }

        return data;
    }

    private static double ConvertToDecimalDegrees(double rawDegrees, string direction)
    {
        int degrees = (int)(rawDegrees / 100);
        double minutes = rawDegrees - (degrees * 100);
        double decimalDegrees = degrees + (minutes / 60);

        if (direction == "S" || direction == "W")
            decimalDegrees *= -1;

        return decimalDegrees;
    }
}
