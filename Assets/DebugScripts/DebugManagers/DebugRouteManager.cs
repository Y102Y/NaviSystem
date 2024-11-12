// Assets/DebugScripts/DebugManagers/DebugRouteManager.cs

using UnityEngine;
using System.Collections.Generic;
using System; // Math クラスを使用するために追加

public class DebugRouteManager : MonoBehaviour
{
    [Header("Origin Coordinates")]
    [Tooltip("基準点の緯度（小数第13位まで）")]
    public double originLatitude = 35.665576580782;
    [Tooltip("基準点の経度（小数第13位まで）")]
    public double originLongitude = 140.071367856623;

    [Header("Routes Data")]
    [Tooltip("管理するルートデータのリスト")]
    public List<RouteData> routes = new List<RouteData>();

    [Header("Object Manager")]
    [Tooltip("DebugObjectManagerへの参照")]
    public DebugObjectManager objectManager;

    [Header("Navigation Settings")]
    [Tooltip("最大可視距離（メートル）")]
    public float maxVisibleDistance = 10.0f;

    [Header("Prefabs")]
    [Tooltip("矢印（Arrow）プレハブ")]
    public GameObject arrowPrefab;
    [Tooltip("ゲート（Gate）プレハブ")]
    public GameObject gatePrefab;

    [Header("Navigation Mode")]
    [Tooltip("ナビゲーションモードを選択してください。")]
    public NavigationMode navigationMode = NavigationMode.Arrow;

    [Header("Gate Position Settings")]
    [Tooltip("ゲートのY軸オフセット（メートル）")]
    public float gateYOffset = 2f;
    [Tooltip("ゲートを配置する前後の距離（メートル）")]
    public float gateDistance = 3f; // 前後3ｍ
    [Tooltip("ゲートを配置する間隔（メートル）")]
    public float gateInterval = 5f; // 間隔5ｍ

    // ナビゲーション変数
    private int currentCheckpointIndex = 0;
    private List<Vector3> checkpointPositions = new List<Vector3>();

    // ナビゲーションモードの列挙型
    public enum NavigationMode
    {
        Arrow,
        Gate
        // Bothを削除
    }

    void Start()
    {
        InitializeDependencies();
        InstantiateAllRoutes();
        DebugLogger.Instance?.LogInfo($"DebugRouteManager: {GetTotalCheckpointCount()} checkpoints と {GetTotalGateCount()} gates をインスタンス化しました。");
    }

    /// <summary>
    /// 依存関係の初期化と設定確認を行います。
    /// </summary>
    private void InitializeDependencies()
    {
        if (objectManager == null)
        {
            objectManager = FindObjectOfType<DebugObjectManager>();
            if (objectManager == null)
            {
                DebugLogger.Instance?.LogError("DebugRouteManager: DebugObjectManagerが見つかりません。");
                return;
            }
        }

        // プレハブの確認
        if (arrowPrefab == null && navigationMode != NavigationMode.Gate)
        {
            DebugLogger.Instance?.LogError("DebugRouteManager: ArrowPrefabがアサインされていません。");
        }

        if (gatePrefab == null && navigationMode != NavigationMode.Arrow)
        {
            DebugLogger.Instance?.LogError("DebugRouteManager: GatePrefabがアサインされていません。");
        }
    }

    /// <summary>
    /// すべてのルートをインスタンス化します。
    /// </summary>
    private void InstantiateAllRoutes()
    {
        foreach (RouteData route in routes)
        {
            InstantiateRoute(route);
        }
    }

    /// <summary>
    /// ルートをインスタンス化するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    public void InstantiateRoute(RouteData route)
    {
        if (route.coordinates.Count == 0)
        {
            DebugLogger.Instance?.LogWarning($"Route '{route.routeName}' に座標データがありません。");
            return;
        }

        // チェックポイントの配置
        for (int i = 0; i < route.coordinates.Count; i++)
        {
            Vector3 unityPosition = ConvertGeographicToUnity(
                route.coordinates[i].Latitude,
                route.coordinates[i].Longitude,
                originLatitude,
                originLongitude
            );

            // チェックポイントのインスタンス化（矢印関連の処理は既存のまま）
            if (arrowPrefab != null && (navigationMode == NavigationMode.Arrow))
            {
                GameObject arrowInstance = Instantiate(arrowPrefab, unityPosition, Quaternion.identity, transform);
                arrowInstance.name = $"{route.routeName}_Arrow_Checkpoint_{i + 1}";

                // 初期の矢印のみアクティブに
                if (i == 0)
                {
                    arrowInstance.SetActive(true);
                }
                else
                {
                    arrowInstance.SetActive(false);
                }

                // チェックポイントの向きを設定
                SetCheckpointRotation(route, i, arrowInstance, unityPosition);

                objectManager.AddCheckpoint(arrowInstance);
                DebugLogger.Instance?.LogInfo($"Instantiated {arrowInstance.name} at {unityPosition}");
            }
            else if (arrowPrefab == null && navigationMode == NavigationMode.Arrow)
            {
                DebugLogger.Instance?.LogError("DebugRouteManager: ArrowPrefabがアサインされていません。");
            }

            checkpointPositions.Add(unityPosition);
        }

        // ゲートの配置（各セグメントごと）
        if (navigationMode == NavigationMode.Gate)
        {
            PlaceGatesForRoute(route);
        }
    }

    /// <summary>
    /// ルート全体に対してゲートを配置するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    private void PlaceGatesForRoute(RouteData route)
    {
        for (int i = 0; i < route.coordinates.Count - 1; i++)
        {
            Vector3 startPos = checkpointPositions[i];
            Vector3 endPos = checkpointPositions[i + 1];
            Vector3 direction = (endPos - startPos).normalized;
            float segmentLength = Vector3.Distance(startPos, endPos);

            // 前方ゲートの配置
            Vector3 gateBeforePosition = startPos + direction * gateDistance;
            gateBeforePosition.y += gateYOffset;
            GameObject gateBefore = Instantiate(gatePrefab, gateBeforePosition, Quaternion.identity, transform);
            gateBefore.name = $"{route.routeName}_Gate_Before_{i + 1}";

            // ゲートの向きをチェックポイントに合わせる（y軸のみ回転）
            Vector3 directionToCheckpoint = (endPos - gateBeforePosition).normalized;
            directionToCheckpoint.y = 0; // y軸の成分を除去して水平に向ける
            if (directionToCheckpoint != Vector3.zero)
            {
                gateBefore.transform.rotation = Quaternion.LookRotation(directionToCheckpoint) * Quaternion.Euler(0f, 90f, 0f);
            }

            objectManager.AddGate(gateBefore);
            DebugLogger.Instance?.LogInfo($"Instantiated {gateBefore.name} at {gateBeforePosition}");

            // 後方ゲートの配置
            Vector3 gateAfterPosition = endPos - direction * gateDistance;
            gateAfterPosition.y += gateYOffset;
            GameObject gateAfter = Instantiate(gatePrefab, gateAfterPosition, Quaternion.identity, transform);
            gateAfter.name = $"{route.routeName}_Gate_After_{i + 1}";

            // ゲートの向きをチェックポイントに合わせる（y軸のみ回転）
            Vector3 directionFromCheckpoint = (gateAfterPosition - startPos).normalized;
            directionFromCheckpoint.y = 0; // y軸の成分を除去して水平に向ける
            if (directionFromCheckpoint != Vector3.zero)
            {
                gateAfter.transform.rotation = Quaternion.LookRotation(directionFromCheckpoint) * Quaternion.Euler(0f, 90f, 0f);
            }

            objectManager.AddGate(gateAfter);
            DebugLogger.Instance?.LogInfo($"Instantiated {gateAfter.name} at {gateAfterPosition}");

            // 間隔ゲートの配置
            PlaceGatesAtIntervals(route, i, startPos, endPos, direction, segmentLength);
        }
    }

    /// <summary>
    /// セグメントごとに間隔ゲートを配置するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    /// <param name="segmentIndex">セグメントのインデックス</param>
    /// <param name="startPos">セグメントの開始位置</param>
    /// <param name="endPos">セグメントの終了位置</param>
    /// <param name="direction">セグメントの方向ベクトル</param>
    /// <param name="segmentLength">セグメントの長さ</param>
    private void PlaceGatesAtIntervals(RouteData route, int segmentIndex, Vector3 startPos, Vector3 endPos, Vector3 direction, float segmentLength)
    {
        // ゲート配置可能な距離を計算（前後ゲートを除く）
        float availableDistance = segmentLength - 2 * gateDistance;
        if (availableDistance < gateInterval)
        {
            // 間隔ゲートを配置する余裕がない場合はスキップ
            return;
        }

        // 間隔ゲートの開始位置
        float firstGateOffset = gateDistance + gateInterval;
        if (firstGateOffset > segmentLength - gateDistance)
        {
            // 間隔ゲートの配置開始位置が後方ゲートの位置を超える場合はスキップ
            return;
        }

        // ゲートを配置する位置のリストを作成
        List<float> gateOffsets = new List<float>();
        float currentOffset = firstGateOffset;

        while (currentOffset <= segmentLength - gateDistance)
        {
            gateOffsets.Add(currentOffset);
            currentOffset += gateInterval;
        }

        // 間隔ゲートを配置
        foreach (float offset in gateOffsets)
        {
            Vector3 gatePosition = startPos + direction * offset;
            gatePosition.y += gateYOffset;

            GameObject gate = Instantiate(gatePrefab, gatePosition, Quaternion.identity, transform);
            gate.name = $"{route.routeName}_Gate_Interval_{segmentIndex + 1}_{offset}m";

            // ゲートの向きを進行方向に合わせる（y軸のみ回転）
            Vector3 gateDirection = (endPos - gatePosition).normalized;
            gateDirection.y = 0; // y軸の成分を除去して水平に向ける
            if (gateDirection != Vector3.zero)
            {
                gate.transform.rotation = Quaternion.LookRotation(gateDirection) * Quaternion.Euler(0f, 90f, 0f);
            }

            objectManager.AddGate(gate);
            DebugLogger.Instance?.LogInfo($"Instantiated {gate.name} at {gatePosition}");
        }
    }

    /// <summary>
    /// チェックポイントの回転を設定します。
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    /// <param name="index">現在のチェックポイントインデックス</param>
    /// <param name="obj">インスタンス化されたオブジェクト</param>
    /// <param name="currentPosition">現在のチェックポイントの位置</param>
    private void SetCheckpointRotation(RouteData route, int index, GameObject obj, Vector3 currentPosition)
    {
        if (index < route.coordinates.Count - 1)
        {
            Vector3 nextUnityPosition = ConvertGeographicToUnity(
                route.coordinates[index + 1].Latitude,
                route.coordinates[index + 1].Longitude,
                originLatitude,
                originLongitude
            );
            Vector3 direction = nextUnityPosition - currentPosition;
            if (direction != Vector3.zero)
            {
                obj.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f);
            }
        }
        else
        {
            if (index > 0)
            {
                Vector3 prevUnityPosition = ConvertGeographicToUnity(
                    route.coordinates[index - 1].Latitude,
                    route.coordinates[index - 1].Longitude,
                    originLatitude,
                    originLongitude
                );
                Vector3 direction = currentPosition - prevUnityPosition;
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
    }

    /// <summary>
    /// 緯度経度をUnityの座標系に変換するメソッド
    /// </summary>
    /// <param name="latitude">緯度</param>
    /// <param name="longitude">経度</param>
    /// <param name="originLat">基準点の緯度</param>
    /// <param name="originLon">基準点の経度</param>
    /// <param name="yOffset">Y軸のオフセット（メートル）</param>
    /// <returns>Unity座標</returns>
    private Vector3 ConvertGeographicToUnity(double latitude, double longitude, double originLat, double originLon, float yOffset = 0f)
    {
        const double EarthRadius = 6378137.0; // 地球の半径（メートル）

        double deltaLat = (latitude - originLat) * Math.PI / 180.0;
        double deltaLon = (longitude - originLon) * Math.PI / 180.0;

        double x = EarthRadius * deltaLon * Math.Cos(originLat * Math.PI / 180.0);
        double z = EarthRadius * deltaLat;

        return new Vector3((float)x, yOffset, (float)z);
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
            total += route.coordinates.Count;
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
            if (route.gateInterval <= 0f || gatePrefab == null) continue;

            float totalDistance = 0f;
            for (int i = 1; i < route.coordinates.Count; i++)
            {
                Vector3 startPos = ConvertGeographicToUnity(
                    route.coordinates[i - 1].Latitude,
                    route.coordinates[i - 1].Longitude,
                    originLatitude,
                    originLongitude
                );
                Vector3 endPos = ConvertGeographicToUnity(
                    route.coordinates[i].Latitude,
                    route.coordinates[i].Longitude,
                    originLatitude,
                    originLongitude
                );
                totalDistance += Vector3.Distance(startPos, endPos);
            }
            total += Mathf.FloorToInt(totalDistance / route.gateInterval);
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

            // チェックポイントにアタッチされた矢印を取得
            Transform checkpointTransform = GetCheckpointTransform(currentCheckpointIndex);
            if (checkpointTransform == null)
            {
                DebugLogger.Instance.LogWarning($"DebugRouteManager: チェックポイント {currentCheckpointIndex} に矢印がアタッチされていません。");
                return;
            }

            // 矢印が子オブジェクトとしてアタッチされていると仮定
            if (checkpointTransform.childCount == 0)
            {
                DebugLogger.Instance.LogWarning($"DebugRouteManager: チェックポイント {currentCheckpointIndex} に矢印が見つかりません。");
                return;
            }

            GameObject arrow = checkpointTransform.GetChild(0).gameObject;

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
                if (currentCheckpointIndex < checkpointPositions.Count)
                {
                    Transform nextCheckpointTransform = GetCheckpointTransform(currentCheckpointIndex);
                    if (nextCheckpointTransform != null && nextCheckpointTransform.childCount > 0)
                    {
                        GameObject nextArrow = nextCheckpointTransform.GetChild(0).gameObject;
                        nextArrow.SetActive(true);
                    }
                }
                else
                {
                    DebugLogger.Instance.LogInfo("DebugRouteManager: 目的地に到着しました。");
                }
            }
        }
    }

    /// <summary>
    /// 指定したチェックポイントインデックスのTransformを取得します。
    /// </summary>
    /// <param name="index">チェックポイントのインデックス</param>
    /// <returns>チェックポイントのTransform、存在しない場合はnull</returns>
    private Transform GetCheckpointTransform(int index)
    {
        // チェックポイントの名前を基に検索
        string checkpointName = $"RouteName_Arrow_Checkpoint_{index + 1}"; // 実際の命名規則に合わせてください
        GameObject checkpoint = GameObject.Find(checkpointName);
        return checkpoint != null ? checkpoint.transform : null;
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
