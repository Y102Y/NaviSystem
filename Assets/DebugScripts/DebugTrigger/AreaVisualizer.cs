using UnityEngine;

public class AreaVisualizer : MonoBehaviour
{
    public Transform center; // エリアの中心点
    public float radius = 2f; // エリアの半径
    public float offsetY = 0.1f; // 地面からのオフセット
    public Color lineColor = Color.green; // 線の色

    private LineRenderer lineRenderer;

    void Start()
    {
        // LineRendererを作成または取得
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 100; // 頂点数
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = true; // 閉じた形状にする
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // シンプルなシェーダー
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // 初期状態で円を描画
        DrawCircle();
    }

    private void DrawCircle()
    {
        if (center == null)
        {
            Debug.LogError("中心点が設定されていません！");
            return;
        }

        Vector3[] positions = new Vector3[lineRenderer.positionCount];
        float angleStep = 360f / lineRenderer.positionCount;

        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            float angle = Mathf.Deg2Rad * i * angleStep;
            positions[i] = center.position + new Vector3(Mathf.Cos(angle) * radius, offsetY, Mathf.Sin(angle) * radius);
        }

        lineRenderer.SetPositions(positions);
    }

    void Update()
    {
        // 中心点の移動に応じて円を更新
        DrawCircle();
    }
}
