Shader "Hidden/HeatHexagonBlur"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "black" {}
        _DensityTex ("Albedo (RGB)", 2D) = "white" {}
        _MaxDensity("Max Density", int) = 1
        _Radius("Sample radius", int) = 17
    }
    SubShader
    {
        zwrite off
        ztest off
        zclip off
        Pass
        {

            name "Hexagon blur"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define HEXAGON_ANGLE 1.04719755f

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
            int _Radius;


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Get the size of 1 pixel
                float2 pixel_size = float2(_ScreenParams.z - 1, _ScreenParams.w - 1);

                // Get the color of the center
                fixed4 col = tex2D(_DensityTex, i.uv);
                float lerp_val = col.r / (float)_MaxDensity;

                // For every corner
                for (int index = 0; index < 6; index++)
                {
                    // For every sample
                    for (int sample_idx = 1; sample_idx <= _Radius; sample_idx++)
                    {
                        // Calculate the current offset
                        float2 offset = float2(sample_idx * pixel_size.x * cos(index * HEXAGON_ANGLE),
                                               sample_idx * pixel_size.y * sin(index * HEXAGON_ANGLE));

                        // Get the density in this pixel and clamp it
                        float vertex_density = tex2D(_DensityTex, i.uv + offset) / (float)_MaxDensity;
                        vertex_density = clamp(vertex_density, 0, 1);

                        // Add to the total
                        lerp_val += vertex_density;
                    }
                }

                // Normalize the final lerp value
                lerp_val /= 1.f + 6 * _Radius;

                // Set the final color
                col = lerp(fixed4(0, 0, 1, 1),fixed4(1, 0, 0, 1), lerp_val);
                return float4(col.rgb, 1.f);
            }
            ENDCG
        }

    }
}