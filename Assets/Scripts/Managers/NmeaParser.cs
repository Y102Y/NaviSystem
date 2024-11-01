// Assets/Scripts/Parsers/NmeaParser.cs

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
    /// <param name="nmeaSentence">解析するNMEAセンテンス。</param>
    /// <returns>解析結果のNmeaDataオブジェクト。解析に失敗した場合はnull。</returns>
    public static NmeaData Parse(string nmeaSentence)
    {
        if (string.IsNullOrEmpty(nmeaSentence))
            return null;

        string[] parts = nmeaSentence.Split(',');

        // センテンスの種類を判定
        if (nmeaSentence.StartsWith("$GPGGA") || nmeaSentence.StartsWith("$GNGGA"))
        {
            // GGAセンテンスの解析
            if (parts.Length < 15)
            {
                Debug.LogWarning("GGAセンテンスのフィールド数が不足しています。");
                return null;
            }

            // ステータスフィールドの確認
            // 6番目のフィールドがステータス（0 = 無効, 1 = GPS fix, 2 = DGPS fix）
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

                // ヘディングはGPRMCから取得するため、ここでは保持しない
            }
            catch (Exception e)
            {
                Debug.LogError($"GGA解析エラー: {e.Message}");
                return null;
            }

            return data;
        }
        else if (nmeaSentence.StartsWith("$GPRMC") || nmeaSentence.StartsWith("$GNRMC"))
        {
            // RMCセンテンスの解析
            if (parts.Length < 12)
            {
                Debug.LogWarning("RMCセンテンスのフィールド数が不足しています。");
                return null;
            }

            // ステータスフィールドの確認
            // 3番目のフィールドがステータス（A = 有効, V = 無効）
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

                // 速度の解析（ノットからメートル毎秒に変換）
                if (double.TryParse(parts[7], out double speedKnots))
                {
                    double speedMps = speedKnots * 0.514444; // 1ノット = 0.514444 m/s
                    // 現在のスクリプトでは速度は使用していませんが、必要に応じてデータに追加可能
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

                // 日付の解析（ここでは省略）
            }
            catch (Exception e)
            {
                Debug.LogError($"RMC解析エラー: {e.Message}");
                return null;
            }

            return data;
        }
        else if (nmeaSentence.StartsWith("$GPGSV") || nmeaSentence.StartsWith("$GLGSV") ||
                 nmeaSentence.StartsWith("$GAGSV") || nmeaSentence.StartsWith("$GBGSV") ||
                 nmeaSentence.StartsWith("$GNGSA") || nmeaSentence.StartsWith("$GPGSA"))
        {
            // GSVやGSAセンテンスの解析（必要に応じて拡張可能）
            // 現在のスクリプトでは位置情報に関与しないため、解析をスキップ
            return null;
        }

        // 解析対象外のセンテンスの場合はnullを返す
        return null;
    }

    /// <summary>
    /// NMEAフォーマットの緯度・経度を度単位に変換します。
    /// </summary>
    /// <param name="rawDegrees">NMEAフォーマットの緯度・経度（ddmm.mmmmまたはdddmm.mmmm）。</param>
    /// <param name="direction">方向（N, S, E, W）。</param>
    /// <returns>度単位の緯度・経度。</returns>
    private static double ConvertToDecimalDegrees(double rawDegrees, string direction)
    {
        // 緯度はddmm.mmmm、経度はdddmm.mmmm
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
