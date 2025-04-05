struct vs_in
{
    float3 pos : POSITION;
    //float2 uv : TEXCOORD;
};

struct vs_out
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

vs_out vs_main(vs_in input)
{
    vs_out output =
    {
        float4(input.pos, 1),
        float2(0, 0),
    };
    
    return output;
}

float4 ps_main(vs_out input) : SV_TARGET
{
    return float4(1, 0, 0, 1);
}