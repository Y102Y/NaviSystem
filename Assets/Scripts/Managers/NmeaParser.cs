// NmeaParser.cs
using System;
using UnityEngine;

public static class NmeaParser
{
    public static void ParseNmea(string line)
    {
        try
        {
            if (line.StartsWith("$GPGGA"))
            {
                var msg = ParseGga(line);
                if (msg != null)
                {
                    Logger.LogInfo($"GGA - Time: {msg.Time}, Lat: {msg.Latitude} {msg.LatDir}, Lon: {msg.Longitude} {msg.LonDir}, Fix Quality: {msg.FixQuality}, Satellites: {msg.NumSatellites}, Altitude: {msg.Altitude} {msg.AltitudeUnits}");
                }
            }
            else if (line.StartsWith("$GPRMC"))
            {
                var msg = ParseRmc(line);
                if (msg != null)
                {
                    Logger.LogInfo($"RMC - Time: {msg.Time}, Status: {msg.Status}, Lat: {msg.Latitude} {msg.LatDir}, Lon: {msg.Longitude} {msg.LonDir}, Speed: {msg.Speed} knots, Course: {msg.Course}, Date: {msg.Date}");
                }
            }
            // 必要に応じて他のNMEA文を処理
        }
        catch (Exception e)
        {
            Logger.LogError($"NMEA解析エラー: {e.Message}");
        }
    }

    private static GgaMessage ParseGga(string line)
    {
        // GGA文のパース
        // フォーマット: $GPGGA,time,lat,NS,lon,EW,quality,num_satellites,hdop,altitude,units,geoid_sep,units,...
        var parts = line.Split(',');

        if (parts.Length < 15) return null;

        try
        {
            GgaMessage msg = new GgaMessage
            {
                Time = parts[1],
                Latitude = Convert.ToDouble(parts[2]),
                LatDir = parts[3],
                Longitude = Convert.ToDouble(parts[4]),
                LonDir = parts[5],
                FixQuality = parts[6],
                NumSatellites = parts[7],
                Hdop = parts[8],
                Altitude = parts[9],
                AltitudeUnits = parts[10]
            };
            return msg;
        }
        catch
        {
            return null;
        }
    }

    private static RmcMessage ParseRmc(string line)
    {
        // RMC文のパース
        // フォーマット: $GPRMC,time,status,lat,NS,lon,EW,speed,course,date,mag_var,mag_var_dir,...
        var parts = line.Split(',');

        if (parts.Length < 12) return null;

        try
        {
            RmcMessage msg = new RmcMessage
            {
                Time = parts[1],
                Status = parts[2],
                Latitude = Convert.ToDouble(parts[3]),
                LatDir = parts[4],
                Longitude = Convert.ToDouble(parts[5]),
                LonDir = parts[6],
                Speed = parts[7],
                Course = parts[8],
                Date = parts[9]
            };
            return msg;
        }
        catch
        {
            return null;
        }
    }

    // メッセージクラスの定義
    public class GgaMessage
    {
        public string Time { get; set; }
        public double Latitude { get; set; }
        public string LatDir { get; set; }
        public double Longitude { get; set; }
        public string LonDir { get; set; }
        public string FixQuality { get; set; }
        public string NumSatellites { get; set; }
        public string Hdop { get; set; }
        public string Altitude { get; set; }
        public string AltitudeUnits { get; set; }
    }

    public class RmcMessage
    {
        public string Time { get; set; }
        public string Status { get; set; }
        public double Latitude { get; set; }
        public string LatDir { get; set; }
        public double Longitude { get; set; }
        public string LonDir { get; set; }
        public string Speed { get; set; }
        public string Course { get; set; }
        public string Date { get; set; }
    }
}
