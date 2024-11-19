using UnityEngine;

public class Controller : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform; // プレイヤーのTransform
    public float smoothness = 0.1f; // プレイヤーの移動の滑らかさ

    [Header("Ntrip Client")]
    public NtripClient ntripClient; // NtripClientをインスペクターでアタッチ

    private Vector3 targetPosition; // プレイヤーの目標位置

    private void Start()
    {
        if (ntripClient != null)
        {
            // NMEAデータ受信イベントの登録
            ntripClient.OnNMEADataReceived += OnNMEADataReceived;

            // 初期位置を設定
            targetPosition = playerTransform.position;
        }
        else
        {
            Debug.LogError("NtripClientがアタッチされていません。");
        }
    }

    private void Update()
    {
        // プレイヤーの位置を更新
        UpdatePlayerPosition();
    }

    private void OnNMEADataReceived(string nmeaSentence)
    {
        Debug.Log($"Received NMEA Data: {nmeaSentence}");

        // 必要に応じて座標を解析し、Unity座標に変換
        // TODO: NMEAデータから経緯度を解析して targetPosition に変換
    }

    private void UpdatePlayerPosition()
    {
        if (playerTransform != null)
        {
            // プレイヤーの位置を滑らかに補間
            playerTransform.position = Vector3.Lerp(playerTransform.position, targetPosition, smoothness);
        }
    }

    private void OnDestroy()
    {
        if (ntripClient != null)
        {
            ntripClient.OnNMEADataReceived -= OnNMEADataReceived;
        }
    }
}
