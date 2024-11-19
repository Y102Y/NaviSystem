using UnityEngine;

public class RTCMHandler : MonoBehaviour
{
    public NtripClient ntripClient;

    void Start()
    {
        if (ntripClient != null)
        {
            ntripClient.OnRTCMDataReceived += HandleRTCMData;
        }
    }

    private void HandleRTCMData(byte[] data)
    {
        Debug.Log($"RTCM Data Received: {data.Length} bytes");
        // ここでRTCMデータを処理
    }

    void OnDestroy()
    {
        if (ntripClient != null)
        {
            ntripClient.OnRTCMDataReceived -= HandleRTCMData;
        }
    }
}
