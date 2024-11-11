// Assets/DebugScripts/DebugManagers/DebugObjectManager.cs

using UnityEngine;
using System.Collections.Generic;

public class DebugObjectManager : MonoBehaviour
{
    // 管理するオブジェクトのリスト
    private List<GameObject> checkpoints = new List<GameObject>();
    private List<GameObject> gates = new List<GameObject>();

    /// <summary>
    /// チェックポイントを追加するメソッド
    /// </summary>
    /// <param name="checkpoint">追加するチェックポイントのGameObject</param>
    public void AddCheckpoint(GameObject checkpoint)
    {
        if (checkpoint != null)
        {
            checkpoints.Add(checkpoint);
            DebugLogger.Instance.LogInfo($"DebugObjectManager: チェックポイント {checkpoint.name} を追加しました。");
        }
        else
        {
            DebugLogger.Instance.LogError("DebugObjectManager: チェックポイントがnullです。");
        }
    }

    /// <summary>
    /// ゲートを追加するメソッド
    /// </summary>
    /// <param name="gate">追加するゲートのGameObject</param>
    public void AddGate(GameObject gate)
    {
        if (gate != null)
        {
            gates.Add(gate);
            DebugLogger.Instance.LogInfo($"DebugObjectManager: ゲート {gate.name} を追加しました。");
        }
        else
        {
            DebugLogger.Instance.LogError("DebugObjectManager: ゲートがnullです。");
        }
    }

    /// <summary>
    /// すべてのチェックポイントを削除するメソッド
    /// </summary>
    public void RemoveAllCheckpoints()
    {
        foreach (GameObject checkpoint in checkpoints)
        {
            if (checkpoint != null)
            {
                Destroy(checkpoint);
            }
        }
        checkpoints.Clear();
        DebugLogger.Instance.LogInfo("DebugObjectManager: すべてのチェックポイントを削除しました。");
    }

    /// <summary>
    /// すべてのゲートを削除するメソッド
    /// </summary>
    public void RemoveAllGates()
    {
        foreach (GameObject gate in gates)
        {
            if (gate != null)
            {
                Destroy(gate);
            }
        }
        gates.Clear();
        DebugLogger.Instance.LogInfo("DebugObjectManager: すべてのゲートを削除しました。");
    }

    /// <summary>
    /// 現在のチェックポイント数を取得するメソッド
    /// </summary>
    /// <returns>チェックポイントの数</returns>
    public int GetCheckpointCount()
    {
        return checkpoints.Count;
    }

    /// <summary>
    /// 現在のゲート数を取得するメソッド
    /// </summary>
    /// <returns>ゲートの数</returns>
    public int GetGateCount()
    {
        return gates.Count;
    }
}
