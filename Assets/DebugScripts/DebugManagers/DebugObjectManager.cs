// Assets/DebugScripts/DebugManagers/DebugObjectManager.cs

using UnityEngine;
using System.Collections.Generic;

public class DebugObjectManager : MonoBehaviour
{
    [Header("Target Object")]
    [Tooltip("ターゲットオブジェクト（プレイヤーキャラクターなど）")]
    public GameObject targetObject;

    // チェックポイントとゲートのリスト
    private List<GameObject> checkpoints = new List<GameObject>();
    private List<GameObject> gates = new List<GameObject>();

    void Start()
    {
        if (targetObject == null)
        {
            Logger.LogError("DebugObjectManager: Target Object が設定されていません。");
            return;
        }

        Logger.LogInfo("DebugObjectManager initialized successfully.");
    }

    /// <summary>
    /// チェックポイントを追加するメソッド
    /// </summary>
    /// <param name="checkpoint">チェックポイントオブジェクト</param>
    public void AddCheckpoint(GameObject checkpoint)
    {
        if (checkpoint != null)
        {
            checkpoints.Add(checkpoint);
            Logger.LogInfo($"DebugObjectManager: チェックポイント '{checkpoint.name}' を追加しました。");
        }
    }

    /// <summary>
    /// ゲートを追加するメソッド
    /// </summary>
    /// <param name="gate">ゲートオブジェクト</param>
    public void AddGate(GameObject gate)
    {
        if (gate != null)
        {
            gates.Add(gate);
            Logger.LogInfo($"DebugObjectManager: ゲート '{gate.name}' を追加しました。");
        }
    }

    /// <summary>
    /// チェックポイントのリストを取得するメソッド
    /// </summary>
    /// <returns>チェックポイントのリスト</returns>
    public List<GameObject> GetCheckpoints()
    {
        return checkpoints;
    }

    /// <summary>
    /// ゲートのリストを取得するメソッド
    /// </summary>
    /// <returns>ゲートのリスト</returns>
    public List<GameObject> GetGates()
    {
        return gates;
    }
}
