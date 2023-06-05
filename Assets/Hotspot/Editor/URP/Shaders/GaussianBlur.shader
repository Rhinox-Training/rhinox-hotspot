Shader "Hidden/GaussianBlur"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "black" {}
        _Sigma("Sigma", float) = 1
        _Radius("Sample radius", int) = 5
    }
    SubShader
    {
        zwrite off
        ztest off
        zclip off
        Pass
        {

            name "Horizontal Gaussian Blur"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define PI 3.14159265

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
            float _Sigma;
            int _Radius;

            float gauss(float x)
            {
                return 1.0f / (2.0f * PI * _Sigma * _Sigma) * exp(-(x * x) / (2.0f * _Sigma * _Sigma));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // Get the size of 1 pixel
                float2 pixel_size = float2(_ScreenParams.z - 1, _ScreenParams.w - 1);

                fixed4 o = 0;
                float weight_sum = 0;

                for (int kernel_step = -_Radius / 2; kernel_step <= _Radius / 2; kernel_step += 2)
                {
                    float2 uv_offset = input.uv;
                    uv_offset.x += ((kernel_step + .5f) * pixel_size.x) * 1;

                    float weight = gauss(kernel_step) + gauss(kernel_step + 1);
                    o += tex2D(_MainTex, uv_offset) * weight;
                    weight_sum += weight;
                }
                o *= (1.0f / weight_sum);
                return o;
            }
            ENDCG
        }
        Pass
        {

            name "Vertical Gaussian Blur"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #define PI 3.14159265

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
            float _Sigma;
            int _Radius;

            float gauss(float x)
            {
                return 1.0f / (2.0f * PI * _Sigma * _Sigma) * exp(-(x * x) / (2.0f * _Sigma * _Sigma));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // Get the size of 1 pixel
                float2 pixel_size = float2(_ScreenParams.z - 1, _ScreenParams.w - 1);

                fixed4 o = 0;
                float weight_sum = 0;

                for (int kernel_step = -_Radius / 2; kernel_step <= _Radius / 2; kernel_step += 2)
                {
                    float2 uv_offset = input.uv;
                    uv_offset.y += ((kernel_step + .5f) * pixel_size.y) * 1;

                    float weight = gauss(kernel_step) + gauss(kernel_step + 1);
                    o += tex2D(_MainTex, uv_offset) * weight;
                    weight_sum += weight;
                }
                o *= (1.0f / weight_sum);
                return o;
            }
            ENDCG
        }
    }
}