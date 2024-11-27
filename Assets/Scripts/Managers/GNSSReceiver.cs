using System.IO.Ports;
using UnityEngine;
using TMPro;

public class GNSSReceiver : MonoBehaviour
{
    // シリアルポートの設定
    public string portName = "COM3";
    public int baudRate = 9600;
    private SerialPort serialPort;

    // プレイヤーのTransformと移動設定
    public Transform playerTransform;
    public float smoothness = 0.1f; // プレイヤーの移動の滑らかさ

    private Vector3 targetPosition;

    // 緯度と経度を表示するUI要素
    public TextMeshProUGUI latitudeText; // 緯度を表示するTextMeshProのUI
    public TextMeshProUGUI longitudeText; // 経度を表示するTextMeshProのUI
    public TextMeshProUGUI fixQualityText; // 位置解の種類を表示するTextMeshProのUI
    
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
    }

    void Update()
    {
        // シリアルポートが開いているか確認
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                // シリアルポートから1行のデータを読み取る
                string line = serialPort.ReadLine();
                // 行が "$GPRMC" または "$GNRMC" で始まるか確認（RMCメッセージかどうか）
                if (line.StartsWith("$GPRMC") || line.StartsWith("$GNRMC"))
                {
                    string[] data = line.Split(',');
                    if (data[2] == "A") // データが有効かどうか確認（ステータスフィールドが "A"）
                    {
                        // RMCメッセージから緯度と経度を解析
                        double latitude = ParseLatitude(data[3], data[4]);
                        double longitude = ParseLongitude(data[5], data[6]);

                        // 解析した緯度と経度に基づいてターゲット位置を更新
                        targetPosition = new Vector3((float)longitude, playerTransform.position.y, (float)latitude);

                        // 現在の緯度と経度をUIテキストに更新
                        if (latitudeText != null) latitudeText.text = "Latitude: " + latitude;
                        if (longitudeText != null) longitudeText.text = "Longitude: " + longitude;
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

                        // 解析した緯度と経度に基づいてターゲット位置を更新
                        targetPosition = new Vector3((float)longitude, playerTransform.position.y, (float)latitude);

                        // 現在の緯度、経度をUIテキストに更新
                        if (latitudeText != null) latitudeText.text = "Latitude: " + latitude;
                        if (longitudeText != null) longitudeText.text = "Longitude: " + longitude;

                        // 位置解の種類をUIテキストに更新
                        if (fixQualityText != null)
                        {
                            string fixQuality = GetFixQualityDescription(data[6]);
                            fixQualityText.text = "Fix Quality: " + fixQuality;
                        }
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

    void OnDestroy()
    {
        // シリアルポートが開いている場合は閉じる
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("シリアルポートを閉じました。");
        }
    }
}
