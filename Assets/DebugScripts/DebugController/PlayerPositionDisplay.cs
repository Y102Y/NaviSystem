// Assets/Scripts/PlayerPositionDisplay.cs

using UnityEngine;
using TMPro; // TextMeshProを使用する場合

public class PlayerPositionDisplay : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("プレイヤーの位置を表示するTextMeshProUGUIコンポーネントを割り当ててください。")]
    public TextMeshProUGUI positionText;

    [Header("Player Settings")]
    [Tooltip("プレイヤーのTransformを割り当ててください。")]
    public Transform playerTransform;

    void Start()
    {
        if (positionText == null)
        {
            Debug.LogError("PlayerPositionDisplay: Position Textが割り当てられていません。");
        }

        if (playerTransform == null)
        {
            Debug.LogError("PlayerPositionDisplay: Player Transformが割り当てられていません。");
        }
    }

    void Update()
    {
        if (positionText != null && playerTransform != null)
        {
            Vector3 playerPosition = playerTransform.position;
            positionText.text = $"Player Position:\nX: {playerPosition.x:F2}\nY: {playerPosition.y:F2}\nZ: {playerPosition.z:F2}";
        }
    }
}
