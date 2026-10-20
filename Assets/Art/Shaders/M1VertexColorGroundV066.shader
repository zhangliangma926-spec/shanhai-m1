Shader "Shanhai/VertexColorLitV066"
{
    Properties
    {
        _Color ("Ground Tint", Color) = (1,1,1,1)
        _MainTex ("Ground Detail", 2D) = "white" {}
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
        struct Input { fixed4 color : COLOR; float2 uv_MainTex; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed3 detail = tex2D(_MainTex, IN.uv_MainTex).rgb;
            o.Albedo = detail * IN.color.rgb * _Color.rgb;
            o.Metallic = 0.0;
            o.Smoothness = 0.08;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
