// Assets/DebugScripts/DebugManagers/DebugRouteManager.cs

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// ルートデータを管理し、ナビゲーション用のオブジェクトをインスタンス化するクラス
/// </summary>
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
    public GameObject linePrefab; // ラインプレハブ

    [Header("Navigation Mode")]
    [Tooltip("ナビゲーションモードを選択してください。")]
    public NavigationMode navigationMode = NavigationMode.Arrow;

    // 矢印の設定
    [Header("Arrow Settings")]
    [Tooltip("矢印のY軸オフセット（メートル）")]
    public float arrowYOffset = 1.5f; // デフォルトのオフセット値

    [Header("Gate Position Settings")]
    [Tooltip("ゲートのY軸オフセット（メートル）")]
    public float gateYOffset = 2f;
    [Tooltip("ゲートを配置する前後の距離（メートル）")]
    public float gateDistance = 3f; // 前後3ｍ
    // gateIntervalはRouteDataから取得

    [Header("Line Settings")]
    [Tooltip("ラインの幅（メートル）")]
    public float lineWidth = 0.2f; // ライン幅
    [Tooltip("ラインの高さ（Y軸オフセット、メートル）")]
    public float lineYOffset = 0.1f; // 地面に軽く浮かせる

    [Tooltip("カーブの高さ（メートル）")]
    public float curveHeight = 2f; // カーブの高さ

    [Header("Debug Settings")]
    [Tooltip("全ての矢印を常に表示する")]
    public bool showAllArrows = false; // デバッグ用フラグ

    // ナビゲーション変数
    private int currentCheckpointIndex = 0;
    private List<List<Vector3>> allCheckpointPositions = new List<List<Vector3>>();
    private List<Vector3> checkpointPositions = new List<Vector3>();
    private RouteData currentRouteData;

    // ナビゲーションモードの列挙型
    public enum NavigationMode
    {
        Arrow,
        Gate,
        Line // ラインモード
    }

    void Start()
    {
        InitializeDependencies();
        InstantiateAllRoutes();
        DebugLogger.Instance?.LogInfo($"DebugRouteManager: {GetTotalCheckpointCount()} checkpoints と {GetTotalGateCount()} gates と {GetTotalLineCount()} lines をインスタンス化しました。");
    }

    void Update()
    {
        // ユーザーの位置とヘディングを取得（仮実装）
        Vector3 userPosition = GetUserPosition();
        float userHeading = GetUserHeading();

        // ナビゲーションを更新
        UpdateNavigation(userPosition, userHeading);
    }

    /// <summary>
    /// ユーザーの位置を取得するメソッド（仮実装）
    /// </summary>
    private Vector3 GetUserPosition()
    {
        // 実際のユーザーの位置取得ロジックを実装
        // ここではカメラの位置を使用
        return Camera.main.transform.position;
    }

    /// <summary>
    /// ユーザーのヘディングを取得するメソッド（仮実装）
    /// </summary>
    private float GetUserHeading()
    {
        // 実際のユーザーのヘディング取得ロジックを実装
        // ここではカメラのY軸の回転を使用
        return Camera.main.transform.eulerAngles.y;
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

        List<Vector3> checkpointPositions = new List<Vector3>();

        // チェックポイントの配置
        foreach (Coordinate coord in route.coordinates)
        {
            Vector3 unityPosition = ConvertGeographicToUnity(
                coord.Latitude,
                coord.Longitude,
                originLatitude,
                originLongitude
            );

            checkpointPositions.Add(unityPosition);
        }

        allCheckpointPositions.Add(checkpointPositions); // 複数ルート対応

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
            PlaceGatesForRoute(route, checkpointPositions);
        }
        else if (navigationMode == NavigationMode.Line && linePrefab != null)
        {
            PlaceLineForRoute(route, checkpointPositions);
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
        Vector3 arrowPosition = unityPosition;
        arrowPosition.y += arrowYOffset; // オフセットを追加

        GameObject arrowInstance = Instantiate(arrowPrefab, unityPosition, Quaternion.identity, null);
        arrowInstance.name = $"{route.routeName}_Arrow_Checkpoint_{index + 1}";

        Debug.Log($"Arrow instantiated at position: {arrowPosition}");

        // 矢印のアクティブ状態を設定
        arrowInstance.SetActive(showAllArrows || index == 0);

        // チェックポイントの向きを設定
        SetCheckpointRotation(route, index, arrowInstance, unityPosition);

        objectManager.AddCheckpoint(arrowInstance);
        DebugLogger.Instance?.LogInfo($"Instantiated {arrowInstance.name} at {unityPosition}");
    }

    /// <summary>
    /// ルート全体に対してゲートを配置するメソッド
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    /// <param name="checkpointPositions">チェックポイントの位置リスト</param>

    private void PlaceGatesForRoute(RouteData route, List<Vector3> checkpointPositions)
    {
        float gateInterval = route.gateInterval;

        for (int i = 0; i < checkpointPositions.Count - 1; i++)
        {
            Vector3 startPos = checkpointPositions[i];
            Vector3 endPos = checkpointPositions[i + 1];
            Vector3 direction = (endPos - startPos).normalized;
            float segmentLength = Vector3.Distance(startPos, endPos);

            // デバッグログで座標情報を確認
            Debug.Log($"startPos: {startPos}, endPos: {endPos}, direction: {direction}, segmentLength: {segmentLength}");

            // 1つ目のチェックポイントの前方ゲートをスキップ
            if (i > 0)
            {
                // 前方ゲートの配置
                Vector3 gateBeforePosition = startPos + direction * gateDistance;
                gateBeforePosition.y += gateYOffset;

                GameObject gateBefore = Instantiate(gatePrefab, gateBeforePosition, Quaternion.identity, transform);
                gateBefore.name = $"{route.routeName}_Gate_Before_{i + 1}";

                Vector3 directionToCheckpoint = (endPos - gateBeforePosition).normalized;
                directionToCheckpoint.y = 0;
                if (directionToCheckpoint != Vector3.zero)
                {
                    gateBefore.transform.rotation = Quaternion.LookRotation(directionToCheckpoint) * Quaternion.Euler(0f, 90f, 0f);
                }

                objectManager.AddGate(gateBefore);
                DebugLogger.Instance?.LogInfo($"Instantiated {gateBefore.name} at {gateBeforePosition}");
            }

            // 後方ゲートの配置（全てのチェックポイントで実行）
            Vector3 gateAfterPosition = endPos - direction * gateDistance;
            gateAfterPosition.y += gateYOffset;

            Debug.Log($"Calculated Gate_After Position: {gateAfterPosition}, Direction: {direction}, GateDistance: {gateDistance}");

            GameObject gateAfter = Instantiate(gatePrefab, gateAfterPosition, Quaternion.identity, transform);
            gateAfter.name = $"{route.routeName}_Gate_After_{i + 1}";

            Vector3 directionFromCheckpoint = (gateAfterPosition - startPos).normalized;
            directionFromCheckpoint.y = 0;
            if (directionFromCheckpoint != Vector3.zero)
            {
                gateAfter.transform.rotation = Quaternion.LookRotation(directionFromCheckpoint) * Quaternion.Euler(0f, 90f, 0f);
            }

            objectManager.AddGate(gateAfter);
            DebugLogger.Instance?.LogInfo($"Instantiated {gateAfter.name} at {gateAfterPosition}");

            // 間隔ゲートの配置
            PlaceGatesAtIntervals(route, i, startPos, endPos, direction, segmentLength, gateInterval);
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
    /// <param name="gateInterval">ゲート間隔（メートル）</param>
    private void PlaceGatesAtIntervals(RouteData route, int segmentIndex, Vector3 startPos, Vector3 endPos, Vector3 direction, float segmentLength, float gateInterval)
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
    /// 各チェックポイントで自然なカーブを実現
    /// 単一の LineRenderer を使用
    /// </summary>
    /// <param name="route">RouteDataアセット</param>
    /// <param name="checkpointPositions">チェックポイントの位置リスト</param>
    private void PlaceLineForRoute(RouteData route, List<Vector3> checkpointPositions)
    {
        if (linePrefab == null)
        {
            DebugLogger.Instance?.LogError("DebugRouteManager: LinePrefabがアサインされていません。");
            return;
        }

        // ルートごとに新しい GameObject を作成し、その中に LineRenderer を追加
        GameObject lineObject = Instantiate(linePrefab, Vector3.zero, Quaternion.identity, transform);
        lineObject.name = $"{route.routeName}_Line";

        // ラインオブジェクトをX軸方向に90度回転
        lineObject.transform.Rotate(90f, 0f, 0f); // ここで回転を適用

        LineRenderer lr = lineObject.GetComponent<LineRenderer>();
        if (lr == null)
        {
            DebugLogger.Instance?.LogError($"DebugRouteManager: {lineObject.name} に LineRenderer コンポーネントがアタッチされていません。");
            Destroy(lineObject);
            return;
        }

        lr.material = new Material(Shader.Find("Sprites/Default")); // 適切なシェーダーとマテリアルを選択
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View; // Local から View に変更
        lr.sortingOrder = 1;

        // デバッグログでライン幅を確認
        DebugLogger.Instance?.LogInfo($"LineRenderer Width - Start: {lr.startWidth}, End: {lr.endWidth}");

        List<Vector3> finalPoints = new List<Vector3>();

        // **スタート地点（原点）の追加**: 原点を最初のポイントとして追加
        Vector3 startPoint = ConvertGeographicToUnity(originLatitude, originLongitude, originLatitude, originLongitude);
        startPoint.y += lineYOffset; // Yオフセットを追加
        finalPoints.Add(startPoint);

        if (checkpointPositions.Count > 0)
        {
            // 原点から最初のチェックポイントへの直線を追加
            Vector3 firstCheckpoint = checkpointPositions[0];
            finalPoints.Add(new Vector3(firstCheckpoint.x, firstCheckpoint.y + lineYOffset, firstCheckpoint.z));
        }

        for (int i = 0; i < checkpointPositions.Count - 1; i++)
        {
            Vector3 currentPos = checkpointPositions[i];
            Vector3 nextPos = checkpointPositions[i + 1];
            Vector3 direction = (nextPos - currentPos).normalized;

            // 前後のセグメントの方向を取得
            Vector3 incomingDir = (i > 0) ? (currentPos - checkpointPositions[i - 1]).normalized : direction;
            Vector3 outgoingDir = (i < checkpointPositions.Count - 2) ? (checkpointPositions[i + 2] - nextPos).normalized : direction;

            // バイセクター（角の中間）を計算
            Vector3 bisector = (incomingDir + outgoingDir).normalized;
            if (bisector == Vector3.zero)
            {
                bisector = direction;
            }

            // 制御点の位置を計算
            Vector3 controlPoint = currentPos + bisector * curveHeight;

            // ベジェ曲線の補間ポイントを生成
            List<Vector3> bezierPoints = GetQuadraticBezierPoints(
                new Vector3(currentPos.x, currentPos.y + lineYOffset, currentPos.z),
                new Vector3(controlPoint.x, controlPoint.y + lineYOffset, controlPoint.z),
                new Vector3(nextPos.x, nextPos.y + lineYOffset, nextPos.z),
                50 // 解像度
            );

            // ベジェ曲線のポイントを追加
            foreach (Vector3 point in bezierPoints)
            {
                finalPoints.Add(point);
                // デバッグログでポイントを確認（必要に応じてコメント解除）
                // DebugLogger.Instance?.LogInfo($"Added point: {point}");
            }
        }

        // LineRenderer にポイントを設定
        lr.positionCount = finalPoints.Count;
        lr.SetPositions(finalPoints.ToArray());

        objectManager.AddLine(lineObject);
        DebugLogger.Instance?.LogInfo($"Instantiated {lineObject.name} with {finalPoints.Count} points");
    }


    /// <summary>
    /// 二次ベジェ曲線の補間ポイントを生成するメソッド
    /// </summary>
    /// <param name="p0">始点</param>
    /// <param name="p1">制御点</param>
    /// <param name="p2">終点</param>
    /// <param name="resolution">解像度（ポイント数）</param>
    /// <returns>補間されたポイントのリスト</returns>
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
            float gateInterval = route.gateInterval;
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

            // 各ルートごとに1つのLineRendererを使用
            total += 1;
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
        Camera.main.transform.rotation = Quaternion.Euler(0, -userHeading, 0);

        if (currentCheckpointIndex < checkpointPositions.Count)
        {
            Vector3 checkpointPosition = checkpointPositions[currentCheckpointIndex];
            float distance = Vector3.Distance(userPosition, checkpointPosition);

            // チェックポイントのTransformを取得
            Transform checkpointTransform = GetCheckpointTransform(currentRouteData, currentCheckpointIndex);
            if (checkpointTransform == null || checkpointTransform.childCount == 0)
            {
                DebugLogger.Instance.LogWarning($"DebugRouteManager: チェックポイント {currentCheckpointIndex} に矢印がアタッチされていません。");
                return;
            }

            GameObject arrow = checkpointTransform.GetChild(0).gameObject;

            if (distance > maxVisibleDistance)
            {
                Vector3 direction = (checkpointPosition - userPosition).normalized;
                arrow.transform.position = userPosition + direction * maxVisibleDistance;
                arrow.transform.position += Vector3.up * arrowYOffset; // Yオフセット追加
            }
            else
            {
                arrow.transform.position = checkpointPosition;
                arrow.transform.position += Vector3.up * arrowYOffset; // Yオフセット追加
            }

            arrow.transform.LookAt(userPosition);

            if (distance < 5.0f)
            {
                arrow.SetActive(false);
                currentCheckpointIndex++;
                if (currentCheckpointIndex < checkpointPositions.Count)
                {
                    Transform nextCheckpointTransform = GetCheckpointTransform(currentRouteData, currentCheckpointIndex);
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
    /// 指定したルートとチェックポイントインデックスのTransformを取得します。
    /// </summary>
    /// <param name="route">対象のRouteData</param>
    /// <param name="checkpointIndex">チェックポイントのインデックス</param>
    /// <returns>チェックポイントのTransform、存在しない場合はnull</returns>
    private Transform GetCheckpointTransform(RouteData route, int checkpointIndex)
    {
        if (route == null)
        {
            DebugLogger.Instance.LogWarning("DebugRouteManager: ルートデータがnullです。");
            return null;
        }

        string checkpointName = $"{route.routeName}_Arrow_Checkpoint_{checkpointIndex + 1}";
        GameObject checkpoint = GameObject.Find(checkpointName);
        return checkpoint != null ? checkpoint.transform : null;
    }

    void OnDrawGizmos()
    {
        if (allCheckpointPositions == null) return;
        Gizmos.color = Color.red;
        foreach (var routeCheckpoints in allCheckpointPositions)
        {
            foreach (var pos in routeCheckpoints)
            {
                Gizmos.DrawSphere(pos, 0.5f);
            }
        }
    }
}
