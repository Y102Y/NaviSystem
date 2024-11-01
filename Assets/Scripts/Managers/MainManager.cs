// MainManager.cs
using System;
using System.IO.Ports;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    // シリアルポートの設定
    public string serialPortName = "COM3";
    public int baudRate = 9600;
    public Parity parity = Parity.None;
    public int dataBits = 8;
    public StopBits stopBits = StopBits.One;

    // NTRIPサーバーの設定
    public string ntripServer = "ntrip.ales-corp.co.jp";
    public int ntripPort = 2101;
    public string mountPoint = "RTCM32M5S";
    public string username = "psu7f04d";
    public string password = "67q9bj";

    private SerialManager serialManager;
    private NtripClient ntripClient;

    void Start()
    {
        try
        {
            // シリアルマネージャーの初期化と開始
            serialManager = new SerialManager(serialPortName, baudRate, parity, dataBits, stopBits, OnSerialDataReceived);
            serialManager.Start();

            // NTRIPクライアントの初期化と開始
            ntripClient = new NtripClient(ntripServer, ntripPort, mountPoint, username, password, serialManager);
            ntripClient.Start();
        }
        catch (Exception e)
        {
            Logger.LogError($"初期化エラー: {e.Message}");
        }
    }

    void OnSerialDataReceived(string line)
    {
        NmeaParser.ParseNmea(line);
    }

    void OnApplicationQuit()
    {
        // クリーンアップ
        if (ntripClient != null)
        {
            ntripClient.Dispose();
        }

        if (serialManager != null)
        {
            serialManager.Dispose();
        }
    }
}
