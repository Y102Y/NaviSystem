using UnityEngine;
using TMPro;

public class TriggerAreaManager : MonoBehaviour
{
    public Transform startArea; // スタートエリアの中心
    public Transform goalArea;  // ゴールエリアの中心
    public float triggerRadius = 2f; // スタート/ゴールエリアの半径
    public TextMeshProUGUI messageText; // メッセージ表示用のUI
    public TextMeshProUGUI timerText;  // 時間表示用のUI
    public Transform playerTransform; // プレイヤーのTransform
    public DebugRouteManager routeManager; // ルートマネージャーへの参照

    private bool isTiming = false;
    private bool hasLeftStartArea = false; // StartAreaを出たかどうかを判定するフラグ
    private float startTime;

    void Start()
    {
        // 既存のStartAreaを再利用または生成
        GameObject existingStartArea = GameObject.Find("StartArea");
        if (existingStartArea != null)
        {
            startArea = existingStartArea.transform;
            startArea.position = Vector3.zero; // 必ず原点に配置
            Debug.Log($"既存のStartAreaを再利用しました。位置: {startArea.position}");
        }
        else
        {
            GameObject defaultStartArea = new GameObject("StartArea");
            defaultStartArea.transform.position = Vector3.zero; // 原点
            startArea = defaultStartArea.transform;
            Debug.Log($"新しいStartAreaを生成しました。位置: {startArea.position}");
        }

        // ゴールエリアの再利用または生成
        GameObject existingGoalArea = GameObject.Find("GoalArea");
        if (existingGoalArea != null)
        {
            goalArea = existingGoalArea.transform;
            Vector3 goalPosition = CalculateGoalPosition();
            goalArea.position = goalPosition;
            Debug.Log($"既存のGoalAreaを再利用しました。新しい位置: {goalPosition}");
        }
        else
        {
            GameObject defaultGoalArea = new GameObject("GoalArea");
            goalArea = defaultGoalArea.transform;
            Vector3 goalPosition = CalculateGoalPosition();
            goalArea.position = goalPosition;
            Debug.Log($"新しいGoalAreaを生成しました。位置: {goalPosition}");
        }

        Debug.Log("スタートエリアとゴールエリアの設定が完了しました。");
    }


    private void Update()
    {
        if (playerTransform == null) return;

        Vector3 playerPosition = playerTransform.position;

        Debug.Log($"Player Position: {playerPosition}");
        Debug.Log($"Goal Position: {goalArea.position}");

        // スタートエリア判定
        if (startArea != null && !isTiming && Vector3.Distance(playerPosition, startArea.position) <= triggerRadius)
        {
            OnEnterStartArea();
        }
        else if (isTiming && !hasLeftStartArea && Vector3.Distance(playerPosition, startArea.position) > triggerRadius)
        {
            // StartAreaを出たタイミング
            OnLeaveStartArea();
            hasLeftStartArea = true; // フラグを更新
        }

        // ゴールエリア判定 (3次元距離を考慮)
        if (goalArea != null && isTiming)
        {
            float distance = Vector3.Distance(playerPosition, goalArea.position);
            Debug.Log($"Distance to Goal: {distance}, Trigger Radius: {triggerRadius}");

            if (distance <= triggerRadius)
            {
                OnEnterGoalArea();
            }
        }

        // タイマー更新
        if (isTiming)
        {
            float elapsed = Time.time - startTime;
            timerText.text = $"Time: {elapsed:F2} s";
        }
    }

    private void OnEnterStartArea()
    {
        isTiming = true;
        messageText.text = "Start!";
        Debug.Log("スタートエリアに入りました！");
    }

    private void OnLeaveStartArea()
    {
        // スタートエリアを出たタイミングでの処理
        startTime = Time.time;
        messageText.text = "Walking...";
        Debug.Log("スタートエリアを出ました！");
    }

    private void OnEnterGoalArea()
    {
        if (!isTiming) return; // 二重判定を防ぐ

        isTiming = false;
        float totalTime = Time.time - startTime;
        messageText.text = $"Goal!\nTime: {totalTime:F2} sec";
        timerText.text = $"Time: {totalTime:F2} s";
        Debug.Log($"ゴールエリアに到達！所要時間: {totalTime:F2} 秒");

        // デバッグログで判定の確認
        Debug.Log($"Final Player Position: {playerTransform.position}");
        Debug.Log($"Final Goal Position: {goalArea.position}");
    }

    private Vector3 CalculateGoalPosition()
    {
        if (routeManager.routes == null || routeManager.routes.Count == 0)
        {
            Debug.LogError("ルートデータがありません。");
            return Vector3.zero;
        }

        RouteData currentRoute = routeManager.routes[0]; // 仮に最初のルートを選択
        if (currentRoute.coordinates.Count == 0)
        {
            Debug.LogError("現在のルートにチェックポイントがありません。");
            return Vector3.zero;
        }

        Coordinate lastCheckpoint = currentRoute.coordinates[currentRoute.coordinates.Count - 1];

        Vector3 goalPosition = routeManager.ConvertGeographicToUnity(
            lastCheckpoint.Latitude,
            lastCheckpoint.Longitude,
            routeManager.originLatitude,
            routeManager.originLongitude
        );

        return new Vector3(goalPosition.x, 0f, goalPosition.z); // Y軸を地面に固定
    }

    private void OnDrawGizmos()
    {
        // スタートエリアの可視化
        if (startArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startArea.position, triggerRadius);
        }

        // ゴールエリアの可視化
        if (goalArea != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(goalArea.position, triggerRadius);
        }
    }
}
