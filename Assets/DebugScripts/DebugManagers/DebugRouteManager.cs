// Assets/DebugScripts/DebugManagers/DebugRouteManager.cs

using UnityEngine;
using System.Collections.Generic;
using System;

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
    [Tooltip("ライン（Line）プレハブ")]
    public GameObject linePrefab; // 新規追加

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

    [Header("Line Settings")]
    [Tooltip("ラインの幅（メートル）")]
    public float lineWidth = 0.5f;
    [Tooltip("ラインの高さ（Y軸オフセット、メートル）")]
    public float lineYOffset = 0.1f; // 地面に軽く浮かせる

    [Tooltip("カーブを適用する距離（メートル）")]
    public float curveDistance = 3f; // 各チェックポイント前後に3mのカーブを適用
    [Tooltip("カーブの高さ（メートル）")]
    public float curveHeight = 2f; // カーブの高さ

    // ナビゲーション変数
    private int currentCheckpointIndex = 0;
    private List<Vector3> checkpointPositions = new List<Vector3>();

    // ナビゲーションモードの列挙型
    public enum NavigationMode
    {
        Arrow,
        Gate,
        Line // 新規追加
    }

    void Start()
    {
        InitializeDependencies();
        InstantiateAllRoutes();
        DebugLogger.Instance?.LogInfo($"DebugRouteManager: {GetTotalCheckpointCount()} checkpoints と {GetTotalGateCount()} gates と {GetTotalLineCount()} lines をインスタンス化しました。");
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
        if (navigationMode == NavigationMode.Arrow && arrowPrefab == null)
        {
            DebugLogger.Instance?.LogError("DebugRouteManager: ArrowPrefabがアサインされていません。");
        }

        if (navigationMode == NavigationMode.Gate && gatePrefab == null)
        {
            DebugLogger.Instance?.LogError("DebugRouteManager: GatePrefabがアサインされていません。");
        }

        if (navigationMode == NavigationMode.Line && linePrefab == null)
        {
            DebugLogger.Instance?.LogError("DebugRouteManager: LinePrefabがアサインされていません。");
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

            checkpointPositions.Add(unityPosition);
        }

        // ナビゲーションモードに応じた配置処理
        if (navigationMode == NavigationMode.Arrow && arrowPrefab != null)
        {
            for (int i = 0; i < checkpointPositions.Count; i++)
            {
                InstantiateArrow(route, i, checkpointPositions[i]);
            }
        }
        else if (navigationMode == NavigationMode.Gate && gatePrefab != null)
        {
            PlaceGatesForRoute(route);
        }
        else if (navigationMode == NavigationMode.Line && linePrefab != null)
        {
            PlaceLineForRoute(route);
        }
    }

    /// <summary>
    /// 矢印（Arrow）のインスタンス化と設定を行います。
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    /// <param name="index">チェックポイントのインデックス</param>
    /// <param name="unityPosition">チェックポイントの位置</param>
    private void InstantiateArrow(RouteData route, int index, Vector3 unityPosition)
    {
        GameObject arrowInstance = Instantiate(arrowPrefab, unityPosition, Quaternion.identity, transform);
        arrowInstance.name = $"{route.routeName}_Arrow_Checkpoint_{index + 1}";

        // 初期の矢印のみアクティブに
        if (index == 0)
        {
            arrowInstance.SetActive(true);
        }
        else
        {
            arrowInstance.SetActive(false);
        }

        // チェックポイントの向きを設定
        SetCheckpointRotation(route, index, arrowInstance, unityPosition);

        objectManager.AddCheckpoint(arrowInstance);
        DebugLogger.Instance?.LogInfo($"Instantiated {arrowInstance.name} at {unityPosition}");
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
    /// ルート全体に対してラインを配置するメソッド
    /// 各チェックポイントの前後に3mずつカーブを適用し、その他は直線
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    private void PlaceLineForRoute(RouteData route)
    {
        if (linePrefab == null)
        {
            DebugLogger.Instance?.LogError("DebugRouteManager: LinePrefabがアサインされていません。");
            return;
        }

        for (int i = 0; i < checkpointPositions.Count - 1; i++)
        {
            Vector3 startPos = checkpointPositions[i];
            Vector3 endPos = checkpointPositions[i + 1];
            Vector3 direction = (endPos - startPos).normalized;
            float segmentLength = Vector3.Distance(startPos, endPos);

            // カーブを適用する距離
            float curveDist = curveDistance;

            // セグメントが十分な長さであるか確認
            if (segmentLength < 2 * curveDist)
            {
                // カーブを適用する余裕がない場合は直線のみ
                InstantiateStraightLine(startPos, endPos, route.routeName, i);
                continue;
            }

            // カーブ部分の開始位置と終了位置を計算
            Vector3 curveStartPos = startPos + direction * curveDist;
            Vector3 curveEndPos = endPos - direction * curveDist;

            // 直線部分を描画（startPos から curveStartPos）
            InstantiateStraightLine(startPos, curveStartPos, route.routeName, i);

            // カーブ部分を描画（curveStartPos から curveEndPos）
            InstantiateCurveLine(route, curveStartPos, curveEndPos, i);

            // 直線部分を描画（curveEndPos から endPos）
            InstantiateStraightLine(curveEndPos, endPos, route.routeName, i);
        }
    }

    /// <summary>
    /// 直線部分を描画するためのメソッド
    /// </summary>
    private void InstantiateStraightLine(Vector3 start, Vector3 end, string routeName, int segmentIndex)
    {
        GameObject lineInstance = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
        lineInstance.name = $"{routeName}_Line_Segment_{segmentIndex + 1}_Straight";

        LineRenderer lr = lineInstance.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(start.x, start.y + lineYOffset, start.z));
            lr.SetPosition(1, new Vector3(end.x, end.y + lineYOffset, end.z));
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
        }
        else
        {
            DebugLogger.Instance?.LogError($"DebugRouteManager: {lineInstance.name} に LineRenderer コンポーネントがアタッチされていません。");
            Destroy(lineInstance);
            return;
        }

        objectManager.AddLine(lineInstance);
        DebugLogger.Instance?.LogInfo($"Instantiated {lineInstance.name} from {start} to {end}");
    }

    /// <summary>
    /// カーブ部分を描画するためのメソッド（二次ベジェ曲線を使用）
    /// </summary>
    private void InstantiateCurveLine(RouteData route, Vector3 start, Vector3 end, int segmentIndex)
    {
        // 制御点を計算（カーブの高さを基に）
        Vector3 midPoint = (start + end) / 2f;
        Vector3 controlPoint = midPoint + Vector3.up * curveHeight;

        // ベジェ曲線の補間ポイントを生成
        List<Vector3> bezierPoints = GetQuadraticBezierPoints(start, controlPoint, end, 20);

        for (int i = 0; i < bezierPoints.Count - 1; i++)
        {
            Vector3 p0 = bezierPoints[i];
            Vector3 p1 = bezierPoints[i + 1];

            GameObject lineInstance = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
            lineInstance.name = $"{route.routeName}_Line_Segment_{segmentIndex + 1}_Curve_{i + 1}";

            LineRenderer lr = lineInstance.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(p0.x, p0.y + lineYOffset, p0.z));
                lr.SetPosition(1, new Vector3(p1.x, p1.y + lineYOffset, p1.z));
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
            }
            else
            {
                DebugLogger.Instance?.LogError($"DebugRouteManager: {lineInstance.name} に LineRenderer コンポーネントがアタッチされていません。");
                Destroy(lineInstance);
                continue;
            }

            objectManager.AddLine(lineInstance);
            DebugLogger.Instance?.LogInfo($"Instantiated {lineInstance.name} from {p0} to {p1}");
        }
    }

    /// <summary>
    /// 二次ベジェ曲線の補間ポイントを生成するメソッド
    /// </summary>
    private List<Vector3> GetQuadraticBezierPoints(Vector3 p0, Vector3 p1, Vector3 p2, int resolution)
    {
        List<Vector3> points = new List<Vector3>();
        for (int i = 0; i <= resolution; i++)
        {
            float t = i / (float)resolution;
            float u = 1 - t;
            Vector3 point = u * u * p0 + 2 * u * t * p1 + t * t * p2;
            points.Add(point);
        }
        return points;
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
            if (gateInterval <= 0f || gatePrefab == null) continue;

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
            total += Mathf.FloorToInt(totalDistance / gateInterval);
        }
        return total;
    }

    /// <summary>
    /// インスタンス化したラインの総数を取得するメソッド
    /// </summary>
    /// <returns>ラインの総数</returns>
    private int GetTotalLineCount()
    {
        int total = 0;
        foreach (RouteData route in routes)
        {
            if (linePrefab == null) continue;

            // 各セグメントに対して直線2本とカーブ1本をカウント
            total += (route.coordinates.Count - 1) * 3; // 各セグメントにつき直線2本 + カーブ1本
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

            // ナビゲーションモードがArrowの場合のみ矢印の処理を行う
            if (navigationMode == NavigationMode.Arrow)
            {
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
            // 他のナビゲーションモード（Gate, Line）は必要に応じて追加の処理を行う
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
        // ルート名が "Route1" と仮定
        string routeName = routes[0].routeName; // 複数ルートの場合は適宜変更
        string checkpointName = $"{routeName}_Arrow_Checkpoint_{index + 1}";
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
