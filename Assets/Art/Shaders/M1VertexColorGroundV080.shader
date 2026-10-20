Shader "Shanhai/VertexColorLitV080"
{
    // v0.8.0 地面：顶点色（草地 / 土路 / 山坡分区）× 细节贴图。
    // 细节贴图按两种尺度采样相乘：近处看得到草叶纹理，远处的大块明暗打破平铺重复感。
    Properties
    {
        _Color ("Ground Tint", Color) = (1,1,1,1)
        _MainTex ("Ground Detail (tileable)", 2D) = "white" {}
        _MacroScale ("Macro Scale", Float) = 0.13
        _MacroStrength ("Macro Strength", Range(0,1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half _MacroScale, _MacroStrength;
        struct Input { fixed4 color : COLOR; float2 uv_MainTex; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 fine = tex2D(_MainTex, IN.uv_MainTex).rgb;
            fixed macro = tex2D(_MainTex, IN.uv_MainTex * _MacroScale + float2(0.37, 0.71)).g;
            fixed3 detail = fine * lerp(1.0, 0.72 + macro * 0.42, _MacroStrength);
            o.Albedo = detail * IN.color.rgb * _Color.rgb;
            o.Metallic = 0.0;
            o.Smoothness = 0.06;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
