// Assets/Scripts/Managers/Logger.cs

using UnityEngine;
using System.IO;
using System; // Exception クラスなどを使用する場合に必要

public static class Logger
{
    private static string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
    private static string logFilePath = Path.Combine(logDirectory, "log.txt");
    private static readonly object fileLock = new object(); // ロックオブジェクトの追加
    private static long maxFileSize = 5 * 1024 * 1024; // 5MB

    public enum LogType
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    private static LogLevel currentLogLevel = LogLevel.Debug; // デフォルトはデバッグレベル

    /// <summary>
    /// ログレベルを設定します。
    /// </summary>
    /// <param name="level">設定するログレベル。</param>
    public static void SetLogLevel(LogLevel level)
    {
        currentLogLevel = level;
    }

    public static void Log(string message, LogType type = LogType.Info)
    {
        // 現在のログレベルに基づいてログ出力を制御
        if (!ShouldLog(type))
            return;

        string logMessage = $"{System.DateTime.Now:yyyy/MM/dd HH:mm:ss}: {type.ToString().ToUpper()}: {message}";
        switch (type)
        {
            case LogType.Info:
                Debug.Log(logMessage);
                break;
            case LogType.Warning:
                Debug.LogWarning(logMessage);
                break;
            case LogType.Error:
                Debug.LogError(logMessage);
                break;
            case LogType.Debug:
                Debug.Log(logMessage);
                break;
        }
        WriteToFile(logMessage);
    }

    public static void LogInfo(string message)
    {
        Log(message, LogType.Info);
    }

    public static void LogWarning(string message)
    {
        Log(message, LogType.Warning);
    }

    public static void LogError(string message)
    {
        Log(message, LogType.Error);
    }

    public static void LogDebug(string message)
    {
        Log(message, LogType.Debug);
    }

    private static bool ShouldLog(LogType type)
    {
        switch (currentLogLevel)
        {
            case LogLevel.Debug:
                return true; // すべてのログを出力
            case LogLevel.Info:
                return type != LogType.Debug;
            case LogLevel.Warning:
                return type == LogType.Warning || type == LogType.Error;
            case LogLevel.Error:
                return type == LogType.Error;
            default:
                return false;
        }
    }

    private static void WriteToFile(string message)
    {
        lock (fileLock) // 排他制御の適用
        {
            try
            {
                // ログディレクトリが存在しない場合は作成
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // ログファイルのサイズをチェックし、最大サイズを超えている場合はローテーション
                if (File.Exists(logFilePath) && new FileInfo(logFilePath).Length > maxFileSize)
                {
                    string archiveFilePath = Path.Combine(logDirectory, $"log_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    File.Move(logFilePath, archiveFilePath);
                }

                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(message);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("ログファイルへの書き込みに失敗しました: " + e.Message);
            }
        }
    }

    /// <summary>
    /// ログファイルのパスをコンソールに出力します。
    /// テスト目的で使用してください。
    /// </summary>
    public static void LogFilePath()
    {
        LogInfo($"Log file path: {logFilePath}");
    }
}
