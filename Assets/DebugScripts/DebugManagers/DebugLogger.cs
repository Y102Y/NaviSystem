// Assets/DebugScripts/DebugManagers/DebugLogger.cs

using UnityEngine;
using System.IO;

public class DebugLogger : MonoBehaviour
{
    private string logFilePath;

    void Awake()
    {
        // ログファイルのパスを設定
        logFilePath = Path.Combine(Application.persistentDataPath, "debug_log.txt");
        Logger.LogInfo("DebugLogger: ログファイルのパスを設定しました。");
    }

    /// <summary>
    /// ログメッセージをファイルに書き込むメソッド
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    public void WriteLog(string message)
    {
        try
        {
            File.AppendAllText(logFilePath, $"{System.DateTime.Now}: {message}\n");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"DebugLogger: ログの書き込みに失敗しました。 {ex.Message}");
        }
    }

    /// <summary>
    /// ログファイルのパスを取得するメソッド
    /// </summary>
    /// <returns>ログファイルのパス</returns>
    public string GetLogFilePath()
    {
        return logFilePath;
    }
}
