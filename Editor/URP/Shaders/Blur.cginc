// This file contains struct, constants and functions for blur image effect shaders

//=============================================================================
// Structs
//=============================================================================
struct blur_app_data
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct blur_frag_in
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
    float2 pixel_size : TEXCOORD1;
};

//=============================================================================
// Defines
//=============================================================================
#define HEXAGON_ANGLE_RAD 1.04719755f
#define HEXAGON_ANGLE_DEG 60
#define PI 3.14159265

//=============================================================================
// Functions
//=============================================================================
float get_hexagon_cos(int corner_index)
{
    if (corner_index % 6 == 0)
        return 1;
    if (corner_index % 6 == 1)
        return 0.5;
    if (corner_index % 6 == 2)
        return -0.5;
    if (corner_index % 6 == 3)
        return -1;
    if (corner_index % 6 == 4)
        return -0.5;
    return 0.5;
}

float get_hexagon_sin(int corner_index)
{
    if (corner_index % 6 == 0)
        return 0;
    if (corner_index % 6 == 1)
        return 0.86603;
    if (corner_index % 6 == 2)
        return 0.86603;
    if (corner_index % 6 == 3)
        return 0;
    if (corner_index % 6 == 4)
        return -0.86603;
    return -0.86603;
}

blur_frag_in vert(blur_app_data v)
{
    blur_frag_in o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    o.pixel_size = _ScreenParams.zw - float2(1, 1);
    return o;
}


//=============================================================================
// Gaussian Blur Functions
//=============================================================================

float gauss_full(const float x, const float sigma)
{
    return 1.0f / (2.0f * PI * sigma * sigma) * exp(-(x * x) / (2.0f * sigma * sigma));
}

float gauss(const float x, const float sigma)
{
    return exp(-(x * x) / (2.0f * sigma * sigma));
}

fixed4 gauss_two_way(blur_frag_in input, const float sigma, const int radius, const sampler2D tex, float2 dir)
{
    fixed4 o = 0;
    float weight_sum = 0;
    float2 scaled_dir = input.pixel_size * dir;

    for (int kernel_step = -radius; kernel_step <= radius; kernel_step++)
    {
        float2 uv_offset = input.uv + scaled_dir * kernel_step;
        float weight = gauss(kernel_step, sigma);
        o += tex2D(tex, uv_offset) * weight;
        weight_sum += weight;
    }
    o /= weight_sum;
    return o;
}
