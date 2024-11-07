// Assets/DebugScripts/DebugManagers/DebugMainManager.cs

using UnityEngine;
using System.Collections.Generic;

public class DebugMainManager : MonoBehaviour
{
    [Header("Player Controller")]
    [Tooltip("プレイヤーオブジェクトの PlayerController スクリプトへの参照")]
    public PlayerController playerController;

    [Header("Routes Data")]
    public List<RouteData> routes = new List<RouteData>(); // 複数ルートを管理

    public DebugRouteManager routeManager; // DebugRouteManagerへの参照

    public float maxVisibleDistance = 50.0f; // 最大可視距離（メートル）

    // ユーザーの現在位置とヘディング
    private Vector3 currentPosition;
    private float currentHeading;

    void Start()
    {
        // PlayerController の設定確認
        if (playerController == null)
        {
            Logger.LogError("DebugMainManager: PlayerController が設定されていません。");
            return;
        }

        // DebugRouteManager の設定確認
        if (routeManager == null)
        {
            routeManager = FindObjectOfType<DebugRouteManager>();
            if (routeManager == null)
            {
                Logger.LogError("DebugMainManager: DebugRouteManager がシーン内に存在しません。");
                return;
            }

            // DebugRouteManager にルートデータを設定
            routeManager.routes = routes;

            // DebugObjectManager の設定確認
            if (routeManager.objectManager == null)
            {
                DebugObjectManager objectManager = FindObjectOfType<DebugObjectManager>();
                if (objectManager != null)
                {
                    routeManager.objectManager = objectManager;
                }
                else
                {
                    Logger.LogError("DebugMainManager: DebugObjectManager がシーン内に存在しません。");
                }
            }
        }

        Logger.LogInfo("DebugMainManager initialized successfully.");

        // ログファイルのパスを出力（テスト用）
        Logger.LogFilePath();
    }

    void Update()
    {
        // プレイヤーの現在位置とヘディングを取得
        currentPosition = playerController.transform.position;
        currentHeading = playerController.transform.eulerAngles.y; // Y軸の回転をヘディングとする

        // DebugRouteManager に渡す
        if (routeManager != null)
        {
            routeManager.UpdateNavigation(currentPosition, currentHeading);
        }
    }

    void OnDestroy()
    {
        // クリーンアップが必要な場合はここに記述
    }
}
