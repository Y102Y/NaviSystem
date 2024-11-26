using UnityEngine;

public class CylinderFollowPlayer : MonoBehaviour
{
    public Transform player; // プレイヤーのTransformを指定

    void Update()
    {
        if (player != null)
        {
            // シリンダーをプレイヤーの位置に追従させる
            transform.position = player.position;
        }
    }
}
