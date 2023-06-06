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
            #include "Blur.cginc"

            sampler2D _MainTex;
            float _Sigma;
            int _Radius;

            fixed4 frag(blur_frag_in input) : SV_Target
            {
                return gauss_two_way(input, _Sigma, _Radius, _MainTex, float2(1, 0));
            }
            ENDCG
        }
        Pass
        {
            name "Vertical Gaussian Blur"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Blur.cginc"

            sampler2D _MainTex;
            float _Sigma;
            int _Radius;

            fixed4 frag(blur_frag_in input) : SV_Target
            {
                return gauss_two_way(input, _Sigma, _Radius, _MainTex, float2(0, 1));
            }
            ENDCG
        }
    }
}