// Assets/Scripts/Managers/ObjectManager.cs

using UnityEngine;
using System.Collections.Generic;

public class ObjectManager : MonoBehaviour
{
    [Header("Target Object")]
    public GameObject targetObject;

    [Header("チェックポイント管理")]
    public List<GameObject> activeCheckpoints = new List<GameObject>();

    [Header("ゲート管理")]
    public List<GameObject> activeGates = new List<GameObject>();

    /// <summary>
    /// チェックポイントを追加するメソッド
    /// </summary>
    /// <param name="checkpoint">追加するチェックポイントオブジェクト。</param>
    public void AddCheckpoint(GameObject checkpoint)
    {
        if (checkpoint != null)
        {
            activeCheckpoints.Add(checkpoint);
            Logger.LogInfo($"ObjectManager: チェックポイントを追加 - {checkpoint.name}");
        }
        else
        {
            Logger.LogWarning("ObjectManager: 追加しようとしたチェックポイントが null です。");
        }
    }

    /// <summary>
    /// ゲートを追加するメソッド
    /// </summary>
    /// <param name="gate">追加するゲートオブジェクト。</param>
    public void AddGate(GameObject gate)
    {
        if (gate != null)
        {
            activeGates.Add(gate);
            Logger.LogInfo($"ObjectManager: ゲートを追加 - {gate.name}");
        }
        else
        {
            Logger.LogWarning("ObjectManager: 追加しようとしたゲートが null です。");
        }
    }

    /// <summary>
    /// オブジェクトの位置を更新します。
    /// </summary>
    /// <param name="position">更新する位置。</param>
    public void UpdateObjectPosition(Vector3 position)
    {
        if (targetObject != null)
        {
            targetObject.transform.position = position;
        }
        else
        {
            Logger.LogWarning("ObjectManager: Target Object が設定されていません。");
        }
    }

    /// <summary>
    /// オブジェクトの向きを更新します。
    /// </summary>
    /// <param name="heading">ヘディング（方位）を度単位で指定。</param>
    public void UpdateObjectRotation(double heading)
    {
        if (targetObject != null)
        {
            // ヘディングをY軸回転に変換
            targetObject.transform.rotation = Quaternion.Euler(0, (float)heading, 0);
        }
        else
        {
            Logger.LogWarning("ObjectManager: Target Object が設定されていません。");
        }
    }

    /// <summary>
    /// 全てのチェックポイントとゲートを削除するメソッド
    /// </summary>
    public void ClearAllObjects()
    {
        foreach (GameObject checkpoint in activeCheckpoints)
        {
            Destroy(checkpoint);
        }
        activeCheckpoints.Clear();

        foreach (GameObject gate in activeGates)
        {
            Destroy(gate);
        }
        activeGates.Clear();

        Logger.LogInfo("ObjectManager: 全てのオブジェクトを削除しました。");
    }
}
