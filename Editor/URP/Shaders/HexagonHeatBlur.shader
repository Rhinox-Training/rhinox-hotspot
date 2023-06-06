Shader "Hidden/HeatHexagonBlur"
{
    Properties
    {
        _DensityTex ("Albedo (RGB)", 2D) = "white" {}
        _HeatTex("Heat", 2D) = "white" {}
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
            #include "Blur.cginc"

            sampler2D _DensityTex;
            sampler2D _HeatTex;
            int _MaxDensity;
            int _Radius;

            fixed4 frag(blur_frag_in i) : SV_Target
            {
                // Get the color of the center
                fixed4 col = tex2D(_DensityTex, i.uv);
                float lerp_val = col.r / (float)_MaxDensity;

                // For every corner
                for (int index = 0; index < 6; index++)
                {
                    float scaled_corner_cos = get_hexagon_cos(index) * i.pixel_size.x;
                    float scaled_corner_sin = get_hexagon_sin(index) * i.pixel_size.y;

                    // For every sample
                    for (int sample_idx = 1; sample_idx <= _Radius; sample_idx++)
                    {
                        // Calculate the current offset
                        float2 offset_uv = i.uv + float2(sample_idx * scaled_corner_cos,
                                                         sample_idx * scaled_corner_sin);

                        // Get the density in this pixel and clamp it
                        float vertex_density = tex2D(_DensityTex, offset_uv).r;
                        vertex_density = clamp(vertex_density, 0, 1);

                        // Add to the total
                        lerp_val += vertex_density;
                    }
                }

                // Normalize the final lerp value
                lerp_val /= 1.f + 6 * _Radius;

                // Set the final color
                col = tex2D(_HeatTex,float2(lerp_val,0));
                return float4(col.rgb, 1.f);
            }
            ENDCG
        }

    }
}