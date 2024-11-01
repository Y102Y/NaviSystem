// ObjectManager.cs
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : MonoBehaviour
{
    // プレハブへの参照
    public GameObject markerPrefab;

    // 生成されたオブジェクトのリスト
    private List<GameObject> markers = new List<GameObject>();

    // データ受信時に呼び出すメソッド
    public void CreateMarker(Vector3 position)
    {
        // プレハブからオブジェクトを生成
        GameObject marker = Instantiate(markerPrefab, position, Quaternion.identity);
        markers.Add(marker);
    }

    // 既存のマーカーを全て削除するメソッド（必要に応じて）
    public void ClearMarkers()
    {
        foreach (var marker in markers)
        {
            Destroy(marker);
        }
        markers.Clear();
    }
}
