Shader "Hidden/Heatmap"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "black" {}
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return fixed4(col.r, 0, 0, col.a); // invert the color
            }
            ENDCG
        }
    }
}