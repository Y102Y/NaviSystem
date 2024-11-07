// Assets/DebugScripts/Controllers/PlayerResetter.cs

using UnityEngine;

public class PlayerResetter : MonoBehaviour
{
    [Header("Player Settings")]
    [Tooltip("リセット対象のプレイヤーのTransformを割り当ててください。")]
    public Transform playerTransform; // プレイヤーのTransformをInspectorで割り当て

    [Header("Origin Coordinates")]
    [Tooltip("基準点の緯度（小数第13位まで）")]
    public double originLatitude = 35.665576580782; // 更新された緯度

    [Tooltip("基準点の経度（小数第13位まで）")]
    public double originLongitude = 140.071367856623; // 更新された経度

    [Header("Offset Settings")]
    [Tooltip("基準点からのY座標のオフセット（メートル）")]
    public float yOffset = 0f; // 必要に応じて調整

    void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("PlayerResetter: Player Transform が設定されていません。");
            return;
        }

        Vector3 originPosition = ConvertGeographicToUnity(originLatitude, originLongitude) + new Vector3(0f, yOffset, 0f);

        playerTransform.position = originPosition;

        Debug.Log($"PlayerResetter: プレイヤーの位置を {originPosition} にリセットしました。");
    }

    /// <summary>
    /// 緯度経度をUnityの座標系に変換するメソッド
    /// </summary>
    /// <param name="latitude">緯度</param>
    /// <param name="longitude">経度</param>
    /// <returns>Unity座標</returns>
    private Vector3 ConvertGeographicToUnity(double latitude, double longitude)
    {
        // 基準点（Origin）の緯度経度
        double originLat = originLatitude;
        double originLon = originLongitude;

        // 地球の半径（メートル）
        double earthRadius = 6378137.0;

        // 緯度経度の差分をラジアンに変換
        double deltaLat = (latitude - originLat) * Mathf.Deg2Rad;
        double deltaLon = (longitude - originLon) * Mathf.Deg2Rad;

        // X座標（東方向）
        double x = earthRadius * deltaLon * Mathf.Cos((float)(originLat * Mathf.Deg2Rad));

        // Z座標（北方向）
        double z = earthRadius * deltaLat;

        // Y座標（高さ）はオフセットを加える
        float y = yOffset;

        return new Vector3((float)x, y, (float)z);
    }
}
