using UnityEngine;
using TMPro;

public class GNSSReceiver : MonoBehaviour
{
    // プレイヤーのTransformと移動設定
    public Transform playerTransform;
    public Transform cameraTransform;
    public float smoothness = 0.1f; // プレイヤーの移動の滑らかさ

    private Vector3 targetPosition;

    // 緯度と経度を表示するUI要素
    public TextMeshProUGUI latitudeText; // 緯度を表示するTextMeshProのUI
    public TextMeshProUGUI longitudeText; // 経度を表示するTextMeshProのUI
    public TextMeshProUGUI azimuthText; // 方位角を表示するTextMeshProのUI
    public TextMeshProUGUI pitchText; // ピッチを表示するTextMeshProのUI
    public TextMeshProUGUI rollText; // ロールを表示するTextMeshProのUI

    // 原点（基準点）の緯度経度
    private readonly double originLatitude = 35.665573533488;
    private readonly double originLongitude = 140.071299281814;
    
    void Start()
    {
        // 初期のターゲット位置を(0, 1, 5)に設定
        targetPosition = new Vector3(0, 1, 5);
        // Debug.Log("Start targetPosition: " + targetPosition);
    }

    void Update()
    {
        UpdateCameraOrientation();
        UpdatePos(UnityTcpServer.latitude, UnityTcpServer.longitude);
        
        if (playerTransform != null)
        {
            playerTransform.position = Vector3.Lerp(playerTransform.position, targetPosition, smoothness);
            // Debug.Log("Update vector:" + playerTransform.position + targetPosition);
        }
        return;
    }

    public void UpdatePos(double latitude, double longitude)
    {
        // 無効な緯度・経度をチェック
        if (latitude == 0 || longitude == 0 || Mathf.Abs((float)(latitude - originLatitude)) > 1 || Mathf.Abs((float)(longitude - originLongitude)) > 1)
        {
            Debug.LogWarning("Invalid latitude or longitude, skipping position update.");
            return;
        }
        // 解析した緯度と経度に基づいてUnityの座標系に変換
        Vector3 newTargetPosition = ConvertToUnityCoordinates(latitude, longitude);

        // 距離差を計算
        float distanceDifference = Vector3.Distance(targetPosition, newTargetPosition);

        // 距離差に基づいて補間速度を計算（動的なスムーズ値）
        float dynamicSmoothness = Mathf.Lerp(30f, 0.1f, Mathf.Clamp01(distanceDifference / 100f));

        // 動的な補間速度で位置を更新
        targetPosition = Vector3.Lerp(targetPosition, newTargetPosition, Time.deltaTime * dynamicSmoothness);

        // 現在の緯度、経度をUIテキストに更新
        UpdateLocationUI(latitude, longitude);
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

        return new Vector3(x, 1.0f, z);
    }

    // カメラの方位角、ピッチ、ロールを更新
    void UpdateCameraOrientation()
    {
        float pitch = UnityTcpServer.pitch;
        float azimuth = UnityTcpServer.azimuth;
        float roll = UnityTcpServer.roll;

        if (cameraTransform != null)
        {
            Quaternion currentRotation = cameraTransform.rotation;
            Quaternion targetRotation = Quaternion.Euler(pitch, azimuth, roll);

            // 角度差を計算
            float angleDifference = Quaternion.Angle(currentRotation, targetRotation);

            // 角度差に基づいて補間速度を計算（動的なスムーズ値）
            float dynamicSmoothness = Mathf.Lerp(30f, 0.1f, Mathf.Clamp01(angleDifference / 180f));

            // 動的な補間速度で回転を更新
            cameraTransform.rotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * dynamicSmoothness);
        }

        // 方位角、ピッチ、ロールをUIに表示
        if (azimuthText != null)
        {
            azimuthText.text = "Azimuth: " + azimuth + "\u00b0";
            azimuthText.ForceMeshUpdate();
        }
        if (pitchText != null)
        {
            pitchText.text = "Pitch: " + pitch + "\u00b0";
            pitchText.ForceMeshUpdate();
        }
        if (rollText != null)
        {
            rollText.text = "Roll: " + roll + "\u00b0";
            rollText.ForceMeshUpdate();
        }
    }
}