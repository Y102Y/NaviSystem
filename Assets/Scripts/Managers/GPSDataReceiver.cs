using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;
using TMPro;

public class GPSDataReceiver : MonoBehaviour
{
    // COMポートとボーレートの設定（実際のCOMポート番号に変更してください）
    SerialPort serialPort = new SerialPort("COM5", 9600);

    // 原点の緯度・経度
    public double originLatitude = 35.665573533488;
    public double originLongitude = 140.071299281814;

    // 緯度と経度を表示するUI要素
    public TextMeshProUGUI latitudeText; // 緯度を表示するTextMeshProのUI
    public TextMeshProUGUI longitudeText; // 経度を表示するTextMeshProのUI

    void Start()
    {
        // シリアルポートを開く
        try
        {
            serialPort.Open();
            serialPort.ReadTimeout = 100;
            Debug.Log("シリアルポートが開かれました");
        }
        catch (System.Exception e)
        {
            Debug.LogError("シリアルポートを開くことができませんでした: " + e.Message);
        }
    }

    void Update()
    {
        // シリアルポートからデータを読み取る
        if (serialPort.IsOpen)
        {
            try
            {
                string data = serialPort.ReadLine(); // NMEAデータを受信
                Debug.Log("Received NMEA: " + data);

                // NMEAデータを解析して、必要な情報をUnityで使用する（例：緯度・経度）
                ProcessNMEAData(data);
            }
            catch (System.Exception)
            {
                // タイムアウトや読み取りエラーが発生した場合
            }
        }
    }

    void OnApplicationQuit()
    {
        // アプリケーション終了時にシリアルポートを閉じる
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("シリアルポートを閉じました");
        }
    }

    void ProcessNMEAData(string nmea)
    {
        // NMEA文を解析し、緯度・経度の情報を抽出します。
        // 例：$GPGGA、$GPRMC、$GNGGA、$GNRMCメッセージを処理するコードを追加します。

        if (nmea.StartsWith("$GPGGA") || nmea.StartsWith("$GNGGA"))
        {
            string[] parts = nmea.Split(',');

            if (parts.Length > 5)
            {
                // 緯度と経度を取得
                double latitude = ConvertToDecimalDegrees(parts[2], parts[3]);
                double longitude = ConvertToDecimalDegrees(parts[4], parts[5]);

                // 緯度と経度をUIに表示
                if (latitudeText != null) latitudeText.text = "Latitude: " + latitude;
                if (longitudeText != null) longitudeText.text = "Longitude: " + longitude;

                // Unity上のオブジェクトに緯度・経度を反映する
                UpdateGameObjectPosition(latitude, longitude);
            }
        }
        else if (nmea.StartsWith("$GPRMC") || nmea.StartsWith("$GNRMC"))
        {
            string[] parts = nmea.Split(',');

            if (parts.Length > 5 && parts[3] != "" && parts[5] != "")
            {
                // 緯度と経度を取得
                double latitude = ConvertToDecimalDegrees(parts[3], parts[4]);
                double longitude = ConvertToDecimalDegrees(parts[5], parts[6]);

                // 緯度と経度をUIに表示
                if (latitudeText != null) latitudeText.text = "Latitude: " + latitude;
                if (longitudeText != null) longitudeText.text = "Longitude: " + longitude;

                // Unity上のオブジェクトに緯度・経度を反映する
                UpdateGameObjectPosition(latitude, longitude);
            }
        }
    }

    double ConvertToDecimalDegrees(string value, string direction)
    {
        // NMEA形式から10進式形式に変換
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(direction))
            return 0.0;

        double degrees = double.Parse(value.Substring(0, 2));
        double minutes = double.Parse(value.Substring(2)) / 60.0;
        double result = degrees + minutes;

        if (direction == "S" || direction == "W")
            result *= -1;

        return result;
    }

    void UpdateGameObjectPosition(double latitude, double longitude)
    {
        // 緯度と経度を原点からの相対座標に変換
        double deltaLatitude = latitude - originLatitude;
        double deltaLongitude = longitude - originLongitude;

        // 地球上の緯度・経度を適切にスケーリングしてUnity座標に変換（例として100000倍してメートル単位に）
        float x = (float)(deltaLongitude * 100000.0);
        float z = (float)(deltaLatitude * 100000.0);

        Debug.Log($"Updating position: Latitude: {latitude}, Longitude: {longitude}, X: {x}, Z: {z}");

        // プレイヤーオブジェクトを移動する
        GameObject playerObject = GameObject.Find("Player");
        if (playerObject != null)
        {
            playerObject.transform.position = new Vector3(x, 0, z);
        }
    }
}
