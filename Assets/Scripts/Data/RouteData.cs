// Assets/Scripts/Data/RouteData.cs

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New RouteData", menuName = "Route Data", order = 51)]
public class RouteData : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("ルートの名前を設定してください。")]
    public string routeName;

    [Header("座標データ")]
    [Tooltip("ルートを構成する座標のリストを設定してください。")]
    public List<Coordinate> coordinates = new List<Coordinate>();

    [Tooltip("ゲート間の距離（メートル）を設定してください。")]
    public float gateInterval = 10.0f;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(routeName))
        {
            DebugLogger.Instance?.LogWarning("RouteData: routeName が設定されていません。");
        }

        if (gateInterval <= 0f)
        {
            gateInterval = 10.0f;
            DebugLogger.Instance?.LogWarning("RouteData: gateInterval は 0 より大きい値に設定してください。デフォルト値 10.0f にリセットしました。");
        }
    }
}
