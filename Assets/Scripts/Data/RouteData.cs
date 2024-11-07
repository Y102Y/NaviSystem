// Assets/Scripts/Data/RouteData.cs

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New RouteData", menuName = "Route Data", order = 51)]
public class RouteData : ScriptableObject
{
    public string routeName;
    public List<Coordinate> coordinates = new List<Coordinate>();
    public GameObject gatePrefab; // ゲートを使用する場合にアサイン
    public float gateInterval = 10.0f; // ゲート間隔（メートル）
    public NavigationType navigationType = NavigationType.Arrow; // 使用するナビゲーションタイプ
}
