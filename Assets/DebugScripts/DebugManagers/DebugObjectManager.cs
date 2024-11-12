// Assets/DebugScripts/DebugManagers/DebugObjectManager.cs

using UnityEngine;
using System.Collections.Generic;

public class DebugObjectManager : MonoBehaviour
{
    private List<GameObject> checkpoints = new List<GameObject>();
    private List<GameObject> gates = new List<GameObject>();
    private List<GameObject> lines = new List<GameObject>(); // ライン用リストを追加

    /// <summary>
    /// チェックポイントをリストに追加します。
    /// </summary>
    /// <param name="checkpoint">追加するチェックポイントのGameObject</param>
    public void AddCheckpoint(GameObject checkpoint)
    {
        if (checkpoint != null && !checkpoints.Contains(checkpoint))
        {
            checkpoints.Add(checkpoint);
        }
    }

    /// <summary>
    /// ゲートをリストに追加します。
    /// </summary>
    /// <param name="gate">追加するゲートのGameObject</param>
    public void AddGate(GameObject gate)
    {
        if (gate != null && !gates.Contains(gate))
        {
            gates.Add(gate);
        }
    }

    /// <summary>
    /// ラインをリストに追加します。
    /// </summary>
    /// <param name="line">追加するラインのGameObject</param>
    public void AddLine(GameObject line)
    {
        if (line != null && !lines.Contains(line))
        {
            lines.Add(line);
        }
    }

    /// <summary>
    /// すべてのオブジェクトを削除します。
    /// </summary>
    public void ClearAllObjects()
    {
        foreach (GameObject checkpoint in checkpoints)
        {
            if (checkpoint != null)
            {
                Destroy(checkpoint);
            }
        }
        checkpoints.Clear();

        foreach (GameObject gate in gates)
        {
            if (gate != null)
            {
                Destroy(gate);
            }
        }
        gates.Clear();

        foreach (GameObject line in lines)
        {
            if (line != null)
            {
                Destroy(line);
            }
        }
        lines.Clear();
    }

    /// <summary>
    /// チェックポイントの数を取得します。
    /// </summary>
    /// <returns>チェックポイントの数</returns>
    public int GetCheckpointCount()
    {
        return checkpoints.Count;
    }

    /// <summary>
    /// ゲートの数を取得します。
    /// </summary>
    /// <returns>ゲートの数</returns>
    public int GetGateCount()
    {
        return gates.Count;
    }

    /// <summary>
    /// ラインの数を取得します。
    /// </summary>
    /// <returns>ラインの数</returns>
    public int GetLineCount()
    {
        return lines.Count;
    }
}
