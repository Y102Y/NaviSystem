using UnityEngine;
using TMPro;
using System.IO;

public class TriggerAreaManager : MonoBehaviour
{
    public Transform startArea; // スタートエリアの中心
    public Transform goalArea;  // ゴールエリアの中心
    public float triggerRadius = 2f; // スタート/ゴールエリアの半径
    public TextMeshProUGUI messageText; // メッセージ表示用のUI
    public TextMeshProUGUI timerText;  // 時間表示用のUI
    public Transform playerTransform; // プレイヤーのTransform
    public RouteManager routeManager; // ルートマネージャーへの参照

    private bool isTiming = false;
    private bool hasEnteredStartArea = false; // スタートエリアに一度入ったかを判定するフラグ
    private bool hasLeftStartArea = false;  // スタートエリアを出たかを判定するフラグ
    private float startTime;

    void Start()
    {
        // スタートエリアとゴールエリアの設定を確認または生成
        InitializeAreas();
    }

    private void Update()
    {
        if (playerTransform == null) return;

        Vector3 playerPosition = playerTransform.position;
        playerPosition.y = 1.0f; // プレイヤーのY座標を1.0に固定

        // スタートエリア判定
        if (startArea != null && !hasEnteredStartArea && Vector3.Distance(playerPosition, startArea.position) <= triggerRadius)
        {
            OnEnterStartArea();
        }
        else if (hasEnteredStartArea && !hasLeftStartArea && Vector3.Distance(playerPosition, startArea.position) > triggerRadius)
        {
            OnLeaveStartArea();
        }

        // ゴールエリア判定 (3次元距離を考慮)
        if (goalArea != null && isTiming)
        {
            float distance = Vector3.Distance(playerPosition, goalArea.position);
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
        hasEnteredStartArea = true;
        messageText.text = "You have entered the Start Area!";
        Debug.Log("スタートエリアに入りました！");
    }

    private void OnLeaveStartArea()
    {
        hasLeftStartArea = true;
        isTiming = true;
        startTime = Time.time;
        messageText.text = "Timing Started!";
        Debug.Log("スタートエリアを出ました！タイム計測を開始します。");
    }

    private void OnEnterGoalArea()
    {
        if (!isTiming) return; // 二重判定を防ぐ

        isTiming = false;
        float totalTime = Time.time - startTime;
        messageText.text = $"Goal!\nTime: {totalTime:F2} sec";
        timerText.text = $"Time: {totalTime:F2} s";
        Debug.Log($"ゴールエリアに到達！所要時間: {totalTime:F2} 秒");

        SaveTimeToFile(totalTime);
    }

    private void SaveTimeToFile(float totalTime)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "timelog.csv");
        string logEntry = $"{System.DateTime.Now}, {totalTime:F2}\n";

        try
        {
            File.AppendAllText(filePath, logEntry);
            Debug.Log($"タイムをファイルに保存しました: {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"タイムの保存に失敗しました: {ex.Message}");
        }
    }

    private void InitializeAreas()
    {
        // スタートエリアの設定
        GameObject existingStartArea = GameObject.Find("StartArea");
        if (existingStartArea != null)
        {
            startArea = existingStartArea.transform;
            startArea.position = Vector3.zero;
        }
        else
        {
            GameObject defaultStartArea = new GameObject("StartArea");
            startArea = defaultStartArea.transform;
            startArea.position = Vector3.zero;
        }

        // ゴールエリアの設定
        GameObject existingGoalArea = GameObject.Find("GoalArea");
        if (existingGoalArea != null)
        {
            goalArea = existingGoalArea.transform;
            goalArea.position = CalculateGoalPosition();
        }
        else
        {
            GameObject defaultGoalArea = new GameObject("GoalArea");
            goalArea = defaultGoalArea.transform;
            goalArea.position = CalculateGoalPosition();
        }
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

        return new Vector3(goalPosition.x, 0f, goalPosition.z);
    }

    private void OnDrawGizmos()
    {
        if (startArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startArea.position, triggerRadius);
        }

        if (goalArea != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(goalArea.position, triggerRadius);
        }
    }
}
