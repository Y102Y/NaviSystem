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
    public float gateDistance = 3f; // 1メートルから3メートルに変更

    // ナビゲーション変数
    private int currentCheckpointIndex = 0;
    private List<Vector3> checkpointPositions = new List<Vector3>();

    // ナビゲーションモードの列挙型
    public enum NavigationMode
    {
        Arrow,
        Gate,
        Both
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

        // ルート全体の後方ゲートを配置
        foreach (RouteData route in routes)
        {
            PlaceAfterGates(route);
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
            if (arrowPrefab != null)
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
            else
            {
                DebugLogger.Instance?.LogError("DebugRouteManager: ArrowPrefabがアサインされていません。");
            }

            checkpointPositions.Add(unityPosition);

            // 各チェックポイントの前方にゲートを配置
            if (navigationMode == NavigationMode.Gate || navigationMode == NavigationMode.Both)
            {
                PlaceGatesAroundCheckpoint(route, i);
            }
        }

        // 間隔ゲートの配置
        if (navigationMode == NavigationMode.Gate || navigationMode == NavigationMode.Both)
        {
            PlaceGatesAtIntervals(route);
        }
    }

    /// <summary>
    /// チェックポイントの前方にゲートを配置するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    /// <param name="checkpointIndex">現在のチェックポイントのインデックス</param>
    private void PlaceGatesAroundCheckpoint(RouteData route, int checkpointIndex)
    {
        Vector3 checkpointPosition = checkpointPositions[checkpointIndex];

        // 前のチェックポイントが存在するか確認
        bool hasPrevious = checkpointIndex > 0;

        // ゲートのYオフセット
        float currentGateYOffset = gateYOffset;

        // 前方ゲートの配置
        if (hasPrevious)
        {
            Vector3 previousPosition = checkpointPositions[checkpointIndex - 1];
            Vector3 directionToCheckpoint = (checkpointPosition - previousPosition).normalized;
            Vector3 gateBeforePosition = checkpointPosition - directionToCheckpoint * gateDistance; // 3メートル前
            gateBeforePosition.y += currentGateYOffset; // Y座標を調整

            GameObject gateBefore = Instantiate(gatePrefab, gateBeforePosition, Quaternion.identity, transform);
            gateBefore.name = $"{route.routeName}_Gate_Before_{checkpointIndex + 1}";

            // ゲートの向きをチェックポイントに合わせる（y軸のみ回転）
            Vector3 direction = checkpointPosition - gateBeforePosition;
            direction.y = 0; // y軸の成分を除去して水平に向ける
            if (direction != Vector3.zero)
            {
                gateBefore.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f); // Y軸周りに90度回転
            }

            objectManager.AddGate(gateBefore);
            DebugLogger.Instance?.LogInfo($"Instantiated {gateBefore.name} at {gateBeforePosition}");
        }
    }

    /// <summary>
    /// ルート全体の後方ゲートを配置するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    private void PlaceAfterGates(RouteData route)
    {
        for (int i = 0; i < checkpointPositions.Count; i++)
        {
            Vector3 checkpointPosition = checkpointPositions[i];

            // 次のチェックポイントが存在するか確認
            bool hasNext = i < route.coordinates.Count - 1;

            if (hasNext)
            {
                Vector3 nextPosition = checkpointPositions[i + 1];
                Vector3 directionFromCheckpoint = (nextPosition - checkpointPosition).normalized;
                Vector3 gateAfterPosition = checkpointPosition + directionFromCheckpoint * gateDistance; // 3メートル後
                gateAfterPosition.y += gateYOffset; // Y座標を調整

                GameObject gateAfter = Instantiate(gatePrefab, gateAfterPosition, Quaternion.identity, transform);
                gateAfter.name = $"{route.routeName}_Gate_After_{i + 1}";

                // ゲートの向きをチェックポイントに合わせる（y軸のみ回転）
                Vector3 direction = checkpointPosition - gateAfterPosition;
                direction.y = 0; // y軸の成分を除去して水平に向ける
                if (direction != Vector3.zero)
                {
                    gateAfter.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f); // Y軸周りに90度回転
                }

                objectManager.AddGate(gateAfter);
                DebugLogger.Instance?.LogInfo($"Instantiated {gateAfter.name} at {gateAfterPosition}");
            }
        }
    }

    /// <summary>
    /// ゲートを一定間隔で配置するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    private void PlaceGatesAtIntervals(RouteData route)
    {
        if (route.gateInterval <= 0f)
        {
            DebugLogger.Instance?.LogWarning($"Route '{route.routeName}': GateInterval が無効です。");
            return;
        }

        float accumulatedDistance = 0f;
        int lastGateIndex = 0;

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
            float segmentDistance = Vector3.Distance(startPos, endPos);
            accumulatedDistance += segmentDistance;

            while (accumulatedDistance >= route.gateInterval)
            {
                accumulatedDistance -= route.gateInterval;
                float t = 1f - (accumulatedDistance / segmentDistance);
                Vector3 gatePosition = Vector3.Lerp(startPos, endPos, t);

                // ゲートのY座標を調整
                gatePosition.y += gateYOffset;

                // ゲートをインスタンス化
                GameObject gate = Instantiate(gatePrefab, gatePosition, Quaternion.identity, transform);
                gate.name = $"{route.routeName}_Gate_Interval_{lastGateIndex + 1}";

                // ゲートの向きを進行方向に合わせる（y軸のみ回転）
                Vector3 direction = endPos - gatePosition;
                direction.y = 0; // y軸の成分を除去して水平に向ける
                if (direction != Vector3.zero)
                {
                    gate.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, 90f, 0f); // Y軸周りに90度回転
                }

                objectManager.AddGate(gate);
                lastGateIndex++;
            }
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
