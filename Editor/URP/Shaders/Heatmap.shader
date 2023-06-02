Shader "Hidden/Heatmap"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "black" {}
        _DensityTex ("Albedo (RGB)", 2D) = "white" {}
        _MaxDensity("Max Density", int) = 1
    }
    SubShader
    {
        zwrite off
        ztest off
        zclip off
        Pass
        {
            name "Invert Color"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _DensityTex;
            int _MaxDensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_DensityTex, i.uv);
                float lerp_val = col.r / (float)_MaxDensity;
                col = lerp(fixed4(0, 0, 1, 1),fixed4(1, 0, 0, 1), lerp_val);
                return col;
            }
            ENDCG
        }
    }
}