// Assets/DebugScripts/DebugManager/GroundManager.cs

using UnityEngine;

public class GroundManager : MonoBehaviour
{
    [Header("Player Transform")]
    public Transform playerTransform;

    [Header("Ground Settings")]
    public Vector3 groundOffset = new Vector3(0, -0.5f, 0);
    public float repositionThreshold = 50f;

    private Vector3 lastPosition;

    void Start()
    {
        if (playerTransform == null)
        {
            Debug.LogError("GroundManager: Player Transform が設定されていません。");
            return;
        }

        lastPosition = playerTransform.position;
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(playerTransform.position, lastPosition);
        if (distance >= repositionThreshold)
        {
            // 地面をプレイヤーの現在位置に再配置
            transform.position = playerTransform.position + groundOffset;
            lastPosition = playerTransform.position;
        }
    }
}
