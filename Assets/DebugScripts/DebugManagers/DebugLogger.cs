// Assets/DebugScripts/DebugManagers/DebugLogger.cs

using UnityEngine;

public class DebugLogger : MonoBehaviour
{
    // シングルトンインスタンス
    public static DebugLogger Instance { get; private set; }

    void Awake()
    {
        // シングルトンの設定
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーン遷移時に破棄されないようにする
        }
        else
        {
            Destroy(gameObject); // 既に存在する場合は新しいインスタンスを破棄
        }
    }

    /// <summary>
    /// エラーログを出力するメソッド
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    public void LogError(string message)
    {
        Debug.LogError(message);
    }

    /// <summary>
    /// 情報ログを出力するメソッド
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    public void LogInfo(string message)
    {
        Debug.Log(message);
    }

    /// <summary>
    /// 警告ログを出力するメソッド
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    public void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }
}
