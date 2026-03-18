Shader "Custom/BlackUnlit"
{
    Properties
    {
        _Color ("Main Color", Color) = (0,0,0,1) // デフォルトは黒
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            // ライティングを無視
            Lighting Off
            ZWrite On
            ZTest LEqual
            Cull Back

            // 色の設定
            Color [_Color]

            // メインの描画
            SetTexture [_MainTex] { combine primary }
        }
    }
}
