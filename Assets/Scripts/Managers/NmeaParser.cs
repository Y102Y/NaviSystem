// Assets/Scripts/Managers/NmeaParser.cs

using UnityEngine;
using System;

public static class NmeaParser
{
    public class NmeaData
    {
        public double Latitude { get; set; }   // 緯度（度）
        public double Longitude { get; set; }  // 経度（度）
        public double Altitude { get; set; }   // 高度（メートル）
        public double Heading { get; set; }    // ヘディング（度）
    }

    /// <summary>
    /// NMEAセンテンスを解析し、NmeaDataオブジェクトを返します。
    /// </summary>
    public static NmeaData Parse(string nmeaSentence)
    {
        if (string.IsNullOrEmpty(nmeaSentence))
            return null;

        string[] parts = nmeaSentence.Split(',');

        // センテンスの種類を判定
        if (nmeaSentence.StartsWith("$GPGGA") || nmeaSentence.StartsWith("$GNGGA"))
        {
            // GGAセンテンスの解析
            return ParseGGA(parts);
        }
        else if (nmeaSentence.StartsWith("$GPRMC") || nmeaSentence.StartsWith("$GNRMC"))
        {
            // RMCセンテンスの解析
            return ParseRMC(parts);
        }
        else if (nmeaSentence.StartsWith("$GPVTG") || nmeaSentence.StartsWith("$GNVTG"))
        {
            // VTGセンテンスの解析
            return ParseVTG(parts);
        }

        // 解析対象外のセンテンスの場合はnullを返す
        return null;
    }

    private static NmeaData ParseGGA(string[] parts)
    {
        if (parts.Length < 15)
        {
            Debug.LogWarning("GGAセンテンスのフィールド数が不足しています。");
            return null;
        }

        // ステータスフィールドの確認
        if (!int.TryParse(parts[6], out int status) || status == 0)
        {
            Debug.LogWarning("GGAデータのステータスが無効です。");
            return null;
        }

        NmeaData data = new NmeaData();

        try
        {
            // 緯度の解析
            if (double.TryParse(parts[2], out double rawLat) && !string.IsNullOrEmpty(parts[3]))
            {
                string latDirection = parts[3];
                data.Latitude = ConvertToDecimalDegrees(rawLat, latDirection);
            }
            else
            {
                Debug.LogWarning("GGAセンテンスの緯度情報が無効です。");
                return null;
            }

            // 経度の解析
            if (double.TryParse(parts[4], out double rawLon) && !string.IsNullOrEmpty(parts[5]))
            {
                string lonDirection = parts[5];
                data.Longitude = ConvertToDecimalDegrees(rawLon, lonDirection);
            }
            else
            {
                Debug.LogWarning("GGAセンテンスの経度情報が無効です。");
                return null;
            }

            // 高度の解析
            if (double.TryParse(parts[9], out double altitude))
            {
                data.Altitude = altitude;
            }
            else
            {
                Debug.LogWarning("GGAセンテンスの高度情報が無効です。");
                data.Altitude = 0.0; // デフォルト値
            }

            // ヘディングは他のセンテンスから取得
        }
        catch (Exception e)
        {
            Debug.LogError($"GGA解析エラー: {e.Message}");
            return null;
        }

        return data;
    }

    private static NmeaData ParseRMC(string[] parts)
    {
        if (parts.Length < 12)
        {
            Debug.LogWarning("RMCセンテンスのフィールド数が不足しています。");
            return null;
        }

        // ステータスフィールドの確認
        if (parts[2] != "A")
        {
            Debug.LogWarning("RMCデータが無効です。");
            return null;
        }

        NmeaData data = new NmeaData();

        try
        {
            // 緯度の解析
            if (double.TryParse(parts[3], out double rawLat) && !string.IsNullOrEmpty(parts[4]))
            {
                string latDirection = parts[4];
                data.Latitude = ConvertToDecimalDegrees(rawLat, latDirection);
            }
            else
            {
                Debug.LogWarning("RMCセンテンスの緯度情報が無効です。");
                return null;
            }

            // 経度の解析
            if (double.TryParse(parts[5], out double rawLon) && !string.IsNullOrEmpty(parts[6]))
            {
                string lonDirection = parts[6];
                data.Longitude = ConvertToDecimalDegrees(rawLon, lonDirection);
            }
            else
            {
                Debug.LogWarning("RMCセンテンスの経度情報が無効です。");
                return null;
            }

            // ヘディングの解析
            if (double.TryParse(parts[8], out double heading))
            {
                data.Heading = heading;
            }
            else
            {
                Debug.LogWarning("RMCセンテンスのヘディング情報が無効です。");
                data.Heading = 0.0; // デフォルト値
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"RMC解析エラー: {e.Message}");
            return null;
        }

        return data;
    }

    private static NmeaData ParseVTG(string[] parts)
    {
        if (parts.Length < 9)
        {
            Debug.LogWarning("VTGセンテンスのフィールド数が不足しています。");
            return null;
        }

        NmeaData data = new NmeaData();

        try
        {
            // ヘディングの解析
            if (double.TryParse(parts[1], out double heading))
            {
                data.Heading = heading;
            }
            else
            {
                Debug.LogWarning("VTGセンテンスのヘディング情報が無効です。");
                data.Heading = 0.0; // デフォルト値
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"VTG解析エラー: {e.Message}");
            return null;
        }

        return data;
    }

    /// <summary>
    /// NMEAフォーマットの緯度・経度を度単位に変換します。
    /// </summary>
    private static double ConvertToDecimalDegrees(double rawDegrees, string direction)
    {
        int degrees = 0;
        double minutes = 0.0;

        if (direction == "N" || direction == "S" || direction == "E" || direction == "W")
        {
            if (direction == "N" || direction == "S")
            {
                degrees = (int)(rawDegrees / 100);
                minutes = rawDegrees - (degrees * 100);
            }
            else // E or W
            {
                degrees = (int)(rawDegrees / 100);
                minutes = rawDegrees - (degrees * 100);
            }

            double decimalDegrees = degrees + (minutes / 60);

            if (direction == "S" || direction == "W")
                decimalDegrees *= -1;

            return decimalDegrees;
        }
        else
        {
            Debug.LogWarning($"無効な方向指示子: {direction}");
            return 0.0;
        }
    }
}
