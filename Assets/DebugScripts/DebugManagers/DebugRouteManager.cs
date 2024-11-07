// Assets/DebugScripts/DebugManagers/DebugRouteManager.cs

using UnityEngine;
using System.Collections.Generic;

public class DebugRouteManager : MonoBehaviour
{
    [Header("Origin Coordinates")]
    [Tooltip("基準点の緯度（小数第13位まで）")]
    public double originLatitude = 35.665576580782; // 新しい緯度

    [Tooltip("基準点の経度（小数第13位まで）")]
    public double originLongitude = 140.071367856623; // 新しい経度

    [Header("Routes Data")]
    public List<RouteData> routes = new List<RouteData>();

    public DebugObjectManager objectManager; // DebugObjectManagerへの参照

    [Header("Navigation Settings")]
    public float maxVisibleDistance = 10.0f; // 最大可視距離（メートル）

    // Prefabs references
    [Header("Prefabs")]
    [Tooltip("矢印（Arrow）プレハブ")]
    public GameObject arrowPrefab;

    [Tooltip("ゲート（Gate）プレハブ")]
    public GameObject gatePrefab;

    // Navigation variables
    private int currentCheckpointIndex = 0;
    private List<Vector3> checkpointPositions = new List<Vector3>();
    //private List<GameObject> arrowInstances = new List<GameObject>(); // 使用しないためコメントアウト

    void Start()
    {
        if (objectManager == null)
        {
            objectManager = FindObjectOfType<DebugObjectManager>();
            if (objectManager == null)
            {
                Debug.LogError("DebugRouteManager: DebugObjectManagerが見つかりません。");
                return;
            }
        }

        foreach (RouteData route in routes)
        {
            InstantiateRoute(route);
        }

        Debug.Log($"DebugRouteManager: {GetTotalCheckpointCount()} checkpoints と {GetTotalGateCount()} gates をインスタンス化しました。");

        // Initialize first checkpoint arrow
        /*
        if (arrowInstances.Count > 0)
        {
            arrowInstances[0].SetActive(true);
        }
        */
    }

    /// <summary>
    /// ルートをインスタンス化するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    public void InstantiateRoute(RouteData route)
    {
        if (route.coordinates.Count == 0)
        {
            Debug.LogWarning($"Route '{route.routeName}' に座標データがありません。");
            return;
        }

        // ナビゲーションタイプに応じてプレハブを選択
        GameObject prefabToInstantiate = null;
        if (route.navigationType == NavigationType.Gate)
        {
            prefabToInstantiate = gatePrefab;
            if (prefabToInstantiate == null)
            {
                Debug.LogWarning($"Route '{route.routeName}' にゲートプレハブがアサインされていません。");
                return;
            }
        }
        else if (route.navigationType == NavigationType.Arrow)
        {
            prefabToInstantiate = arrowPrefab;
            if (prefabToInstantiate == null)
            {
                Debug.LogError("DebugRouteManager: ArrowPrefabがアサインされていません。");
                return;
            }
        }

        // チェックポイントの配置
        for (int i = 0; i < route.coordinates.Count; i++)
        {
            Vector3 unityPosition = ConvertGeographicToUnity(route.coordinates[i].Latitude, route.coordinates[i].Longitude);

            // 矢印プレハブの場合、Y座標を1.5m上に調整
            if (route.navigationType == NavigationType.Arrow)
            {
                unityPosition.y += 2.5f; // 矢印を1.5m上に配置
            }

            GameObject obj = Instantiate(prefabToInstantiate, unityPosition, Quaternion.identity);
            obj.name = $"{route.routeName}_{route.navigationType}_Checkpoint_{i + 1}";

            // チェックポイントの向きを次のポイントに向ける（時計回りに90度回転）
            if (i < route.coordinates.Count - 1)
            {
                Vector3 nextUnityPosition = ConvertGeographicToUnity(route.coordinates[i + 1].Latitude, route.coordinates[i + 1].Longitude);
                Vector3 direction = nextUnityPosition - unityPosition;
                if (direction != Vector3.zero)
                {
                    obj.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f);
                }
            }
            else
            {
                // 最後のチェックポイントの場合、前のポイントを向く
                if (i > 0)
                {
                    Vector3 prevUnityPosition = ConvertGeographicToUnity(route.coordinates[i - 1].Latitude, route.coordinates[i - 1].Longitude);
                    Vector3 direction = unityPosition - prevUnityPosition;
                    if (direction != Vector3.zero)
                    {
                        obj.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f);
                    }
                }
                else
                {
                    // チェックポイントが1つだけの場合はデフォルトの向きに90度回転
                    obj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                }
            }

            // オブジェクトの種類に応じて追加処理
            if (route.navigationType == NavigationType.Gate)
            {
                objectManager.AddGate(obj);
            }
            else if (route.navigationType == NavigationType.Arrow)
            {
                objectManager.AddCheckpoint(obj);
                /*
                // arrowInstance のインスタンス化を削除
                GameObject arrowInstance = Instantiate(arrowPrefab, unityPosition, obj.transform.rotation);
                arrowInstance.SetActive(false); // 初期は非表示
                arrowInstances.Add(arrowInstance);
                */
            }

            checkpointPositions.Add(unityPosition);
            Debug.Log($"Instantiated {obj.name} at {unityPosition}");
        }

        // ゲートの配置が必要な場合
        if (route.navigationType == NavigationType.Gate)
        {
            PlaceGates(route);
        }
    }

    /// <summary>
    /// ゲートを一定間隔で配置するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    private void PlaceGates(RouteData route)
    {
        float accumulatedDistance = 0f;
        int lastGateIndex = 0;

        for (int i = 1; i < route.coordinates.Count; i++)
        {
            Vector3 startPos = ConvertGeographicToUnity(route.coordinates[i - 1].Latitude, route.coordinates[i - 1].Longitude);
            Vector3 endPos = ConvertGeographicToUnity(route.coordinates[i].Latitude, route.coordinates[i].Longitude);
            float segmentDistance = Vector3.Distance(startPos, endPos);
            accumulatedDistance += segmentDistance;

            while (accumulatedDistance >= route.gateInterval)
            {
                accumulatedDistance -= route.gateInterval;
                float t = 1f - (accumulatedDistance / segmentDistance);
                Vector3 gatePosition = Vector3.Lerp(startPos, endPos, t);

                // ゲートプレハブの場合、Y座標を1.5m上に調整（必要に応じて）
                gatePosition.y += 1.5f; // ゲートを1.5m上に配置（必要に応じて調整）

                GameObject gate = Instantiate(gatePrefab, gatePosition, Quaternion.identity);
                gate.name = $"{route.routeName}_Gate_{lastGateIndex + 1}";

                // ゲートの向きを進行方向に合わせる（時計回りに90度回転）
                Vector3 direction = endPos - gatePosition;
                if (direction != Vector3.zero)
                {
                    gate.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f);
                }

                objectManager.AddGate(gate);
                lastGateIndex++;
            }
        }
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

        // Y座標（高度）は0と仮定
        float y = 0f;

        return new Vector3((float)x, y, (float)z);
    }

    /// <summary>
    /// インスタンス化したチェックポイントの総数を取得するメソッド
    /// </summary>
    /// <returns>チェックポイントの総数</returns>
    private int GetTotalCheckpointCount()
    {
        int total = 0;
        foreach (RouteData route in routes)
        {
            if (route.navigationType == NavigationType.Arrow)
            {
                total += route.coordinates.Count;
            }
        }
        return total;
    }

    /// <summary>
    /// インスタンス化したゲートの総数を取得するメソッド
    /// </summary>
    /// <returns>ゲートの総数</returns>
    private int GetTotalGateCount()
    {
        int total = 0;
        foreach (RouteData route in routes)
        {
            if (route.navigationType == NavigationType.Gate && route.gatePrefab != null)
            {
                float totalDistance = 0f;
                for (int i = 1; i < route.coordinates.Count; i++)
                {
                    Vector3 startPos = ConvertGeographicToUnity(route.coordinates[i - 1].Latitude, route.coordinates[i - 1].Longitude);
                    Vector3 endPos = ConvertGeographicToUnity(route.coordinates[i].Latitude, route.coordinates[i].Longitude);
                    totalDistance += Vector3.Distance(startPos, endPos);
                }
                total += Mathf.FloorToInt(totalDistance / route.gateInterval);
            }
        }
        return total;
    }

    /// <summary>
    /// ナビゲーションの更新処理
    /// </summary>
    /// <param name="userPosition">ユーザーの現在位置</param>
    /// <param name="userHeading">ユーザーの現在のヘディング</param>
    public void UpdateNavigation(Vector3 userPosition, float userHeading)
    {
        // カメラの回転を更新
        Camera.main.transform.rotation = Quaternion.Euler(0, -userHeading, 0);

        // 現在のチェックポイントの処理
        if (currentCheckpointIndex < checkpointPositions.Count)
        {
            Vector3 checkpointPosition = checkpointPositions[currentCheckpointIndex];
            float distance = Vector3.Distance(userPosition, checkpointPosition);

            /*
            if (currentCheckpointIndex >= arrowInstances.Count)
            {
                Debug.LogWarning("DebugRouteManager: arrowInstancesのインデックスがチェックポイント数を超えています。");
                return;
            }

            GameObject arrow = arrowInstances[currentCheckpointIndex];

            if (distance > maxVisibleDistance)
            {
                // 矢印をユーザーの前方に配置
                Vector3 direction = (checkpointPosition - userPosition).normalized;
                arrow.transform.position = userPosition + direction * maxVisibleDistance;
            }
            else
            {
                // 矢印をチェックポイントの位置に配置
                arrow.transform.position = checkpointPosition;
            }

            // 矢印がユーザーの方を向くように回転
            arrow.transform.LookAt(userPosition);

            // 一定距離内に入ったら次のチェックポイントへ
            if (distance < 5.0f)
            {
                arrow.SetActive(false);
                currentCheckpointIndex++;
                if (currentCheckpointIndex < arrowInstances.Count)
                {
                    arrowInstances[currentCheckpointIndex].SetActive(true);
                }
                else
                {
                    Debug.Log("DebugRouteManager: 目的地に到着しました。");
                }
            }
            */
        }
    }

    void OnDrawGizmos()
    {
        if (checkpointPositions == null) return;
        Gizmos.color = Color.red;
        foreach (var pos in checkpointPositions)
        {
            Gizmos.DrawSphere(pos, 0.5f);
        }
    }
}
