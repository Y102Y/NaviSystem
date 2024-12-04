using System.IO.Ports;
using UnityEngine;
using TMPro;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

public class GNSSReceiver : MonoBehaviour
{
    // シリアルポートの設定
    public string portName = "COM3";
    public int baudRate = 115200;
    private SerialPort serialPort;
    private StringBuilder buffer = new StringBuilder();

    // プレイヤーのTransformと移動設定
    public Transform playerTransform;
    public Transform cameraTransform;
    public float smoothness = 0.1f; // プレイヤーの移動の滑らかさ

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    // 緯度と経度を表示するUI要素
    public TextMeshProUGUI latitudeText; // 緯度を表示するTextMeshProのUI
    public TextMeshProUGUI longitudeText; // 経度を表示するTextMeshProのUI
    public TextMeshProUGUI fixQualityText; // 位置解の種類を表示するTextMeshProのUI
    public TextMeshProUGUI headingText; // 方位角を表示するTextMeshProのUI

    // 原点（基準点）の緯度経度
    private readonly double originLatitude = 35.665573533488;
    private readonly double originLongitude = 140.071299281814;

    // NMEAメッセージの正規表現パターン
    private readonly Regex nmeaRegex = new Regex(@"^\$(GNRMC|GNGGA),.*\*[0-9A-Fa-f]{2}$");
    
    void Start()
    {
        // 指定されたポート名とボーレートでシリアルポートを初期化
        serialPort = new SerialPort(portName, baudRate);
        try
        {
            // シリアルポートを開く
            serialPort.Open();
            Debug.Log("シリアルポートが正常に開かれました。");
        }
        catch (System.Exception e)
        {
            // シリアルポートが開けない場合はエラーログを出力
            Debug.LogError("シリアルポートのオープンに失敗しました: " + e.Message);
        }
        // 初期のターゲット位置をプレイヤーの現在位置に設定
        targetPosition = playerTransform.position;
        targetRotation = playerTransform.rotation;
    }

    void Update()
    {
        // シリアルポートが開いているか確認
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                while (serialPort.BytesToRead > 0)
                {
                    char receivedChar = (char)serialPort.ReadChar();
                    if (receivedChar == '\n')
                    {
                        string line = buffer.ToString().Trim();
                        buffer.Clear();

                        // NMEAメッセージの形式を確認
                        if (nmeaRegex.IsMatch(line) && ValidateChecksum(line))
                        {
                            Debug.Log("受信したデータ: " + line);
                            ProcessNMEALine(line);
                        }
                        else
                        {
                            Debug.LogWarning("無効なNMEAメッセージを破棄しました: " + line);
                        }
                    }
                    else
                    {
                        buffer.Append(receivedChar);
                    }
                }
            }
            catch (System.Exception e)
            {
                // シリアルポートからの読み取りに失敗した場合は警告ログを出力
                Debug.LogWarning("シリアルポートからの読み取りに失敗しました: " + e.Message);
            }
        }

        // プレイヤーの位置をターゲット位置に滑らかに更新
        if (playerTransform != null)
        {
            playerTransform.position = Vector3.Lerp(playerTransform.position, targetPosition, smoothness);
            playerTransform.rotation = Quaternion.Lerp(playerTransform.rotation, targetRotation, smoothness);
        }

        // カメラの回転もプレイヤーの回転に合わせて更新（必要に応じて）
        if (cameraTransform != null)
        {
            cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, targetRotation, smoothness);
        }
    }

    bool ValidateChecksum(string nmeaLine)
    {
        // チェックサムの計算
        int asteriskIndex = nmeaLine.IndexOf('*');
        if (asteriskIndex < 0 || asteriskIndex + 2 >= nmeaLine.Length)
        {
            return false;
        }

        string checksumString = nmeaLine.Substring(asteriskIndex + 1, 2);
        if (!byte.TryParse(checksumString, System.Globalization.NumberStyles.HexNumber, null, out byte expectedChecksum))
        {
            return false;
        }

        byte calculatedChecksum = 0;
        for (int i = 1; i < asteriskIndex; i++)
        {
            calculatedChecksum ^= (byte)nmeaLine[i];
        }

        return calculatedChecksum == expectedChecksum;
    }

    void ProcessNMEALine(string line)
    {
        // 行が "$GPRMC" または "$GNRMC" で始まるか確認（RMCメッセージかどうか）
        if (line.StartsWith("$GPRMC") || line.StartsWith("$GNRMC"))
        {
            Debug.Log("RMCメッセージを受信しました: " + line);
            string[] data = line.Split(',');
            if (data[2] == "A") // データが有効かどうか確認（ステータスフィールドが "A"）
            {
                // RMCメッセージから緯度と経度を解析
                double latitude = ParseLatitude(data[3], data[4]);
                double longitude = ParseLongitude(data[5], data[6]);

                // 解析した緯度と経度に基づいてUnityの座標系に変換
                targetPosition = ConvertToUnityCoordinates(latitude, longitude);

                // 現在の緯度と経度をUIテキストに更新
                UpdateLocationUI(latitude, longitude);

                // ヘディングを解析して反映
                if (!string.IsNullOrEmpty(data[8])) // ヘディング情報が含まれているか確認
                {
                    float heading = float.Parse(data[8]);
                    targetRotation = Quaternion.Euler(0, heading, 0);
                    if (headingText != null) headingText.text = "Heading: " + heading + "°";
                }
            }
        }
        // 行が "$GNGGA" で始まるか確認（GGAメッセージかどうか）
        else if (line.StartsWith("$GNGGA"))
        {
            string[] data = line.Split(',');
            if (data[6] != "0") // データが有効かどうか確認（品質フィールドが "0" でない）
            {
                // GGAメッセージから緯度、経度を解析
                double latitude = ParseLatitude(data[2], data[3]);
                double longitude = ParseLongitude(data[4], data[5]);

                // 解析した緯度と経度に基づいてUnityの座標系に変換
                targetPosition = ConvertToUnityCoordinates(latitude, longitude);

                // 現在の緯度、経度をUIテキストに更新
                UpdateLocationUI(latitude, longitude);

                // 位置解の種類をUIテキストに更新
                if (fixQualityText != null)
                {
                    string fixQuality = GetFixQualityDescription(data[6]);
                    fixQualityText.text = "Fix Quality: " + fixQuality;
                }
            }
        }
    }

    // 緯度と経度を更新するUIのメソッド
    void UpdateLocationUI(double latitude, double longitude)
    {
        if (latitudeText != null)
        {
            latitudeText.text = "Latitude: " + latitude;
            latitudeText.ForceMeshUpdate(); // テキストの即時更新を強制
        }

        if (longitudeText != null)
        {
            longitudeText.text = "Longitude: " + longitude;
            longitudeText.ForceMeshUpdate(); // テキストの即時更新を強制
        }
    }

    // NMEAデータから緯度を解析してfloat値に変換
    double ParseLatitude(string value, string direction)
    {
        // 緯度の値から度と分を抽出
        double degrees = double.Parse(value.Substring(0, 2));
        double minutes = double.Parse(value.Substring(2)) / 60.0;
        double latitude = degrees + minutes;
        // 方向が南の場合は負の値を返す
        return (direction == "S") ? -latitude : latitude;
    }

    // NMEAデータから経度を解析してfloat値に変換
    double ParseLongitude(string value, string direction)
    {
        // 経度の値から度と分を抽出
        double degrees = double.Parse(value.Substring(0, 3));
        double minutes = double.Parse(value.Substring(3)) / 60.0;
        double longitude = degrees + minutes;
        // 方向が西の場合は負の値を返す
        return (direction == "W") ? -longitude : longitude;
    }

    // 緯度経度をUnityの座標系に変換
    Vector3 ConvertToUnityCoordinates(double latitude, double longitude)
    {
        // 緯度・経度の差分を計算
        double deltaLatitude = latitude - originLatitude;
        double deltaLongitude = longitude - originLongitude;

        // メートル単位に変換（おおよその地球の円周から計算）
        double metersPerDegreeLatitude = 111320.0;
        double metersPerDegreeLongitude = 111320.0 * Mathf.Cos((float)(originLatitude * Mathf.Deg2Rad));

        float x = (float)(deltaLongitude * metersPerDegreeLongitude);
        float z = (float)(deltaLatitude * metersPerDegreeLatitude);

        return new Vector3(x, playerTransform.position.y, z);
    }

    // 位置解の種類を取得
    string GetFixQualityDescription(string qualityIndicator)
    {
        switch (qualityIndicator)
        {
            case "1":
                return "GPS Fix";
            case "2":
                return "DGPS Fix";
            case "4":
                return "RTK Fixed";
            case "5":
                return "RTK Float";
            default:
                return "No Fix";
        }
    }
}
