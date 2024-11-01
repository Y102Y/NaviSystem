// Assets/Scripts/Managers/ObjectManager.cs

using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    [Header("Target Object")]
    public GameObject targetObject;

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
            Logger.LogWarning("Target Object が設定されていません。");
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
            Logger.LogWarning("Target Object が設定されていません。");
        }
    }
}
