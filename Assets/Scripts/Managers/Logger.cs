// Logger.cs
using System;
using System.IO;
using UnityEngine;

public static class Logger
{
    private static readonly string logFilePath = Path.Combine(Application.persistentDataPath, "rtk_ntrip_debug.log");
    private static readonly object lockObj = new object();

    public static void LogInfo(string message)
    {
        Log(message, "INFO");
    }

    public static void LogError(string message)
    {
        Log(message, "ERROR");
    }

    public static void LogDebug(string message)
    {
#if UNITY_EDITOR
        Debug.Log($"DEBUG: {message}");
#endif
        Log(message, "DEBUG");
    }

    private static void Log(string message, string level)
    {
        string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {level} - {message}";
        Debug.Log(logMessage);
        lock (lockObj)
        {
            try
            {
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.LogError($"ログファイルへの書き込みに失敗しました: {e.Message}");
            }
        }
    }
}
