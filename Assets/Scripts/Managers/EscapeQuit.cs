#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class EscapeQuit : MonoBehaviour
{
    void Start()
    {
        // プレイ開始時に全画面に設定
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;

        // マウスカーソルを非表示に設定
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Escapeキーを押したときにゲームを終了
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // マウスカーソルを再表示し、ロックを解除
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Application.Quit();

            // Unityエディター上で動作確認するためのコード（エディター停止）
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
