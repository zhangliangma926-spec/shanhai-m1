Shader "Shanhai/VertexColorLit"
{
    Properties { _Color ("Ground Tint", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        fixed4 _Color;
        struct Input { fixed4 color : COLOR; };
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.color.rgb * _Color.rgb;
            o.Metallic = 0.0;
            o.Smoothness = 0.12;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
