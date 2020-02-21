
extern Texture2D paltex : register(t1);
extern SamplerState palsamp : register(s1);

////////////////////////////////////////////////////////////////////////////////////////////////////
//
//	Pal8 to RGB -- pixel shader 2.0
//
////////////////////////////////////////////////////////////////////////////////////////////////////

float4 FP_BlitPal8(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
	half index = (half)SAMPLE2D(srctex, srcsamp, (half2)uv).r;

	return SAMPLE2D(paltex, palsamp, half2(index * 255.0h/256.0h + 0.5h/256.0h, 0));
}

float4 FP_BlitPal8RBSwap(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target
{
	half index = (half)SAMPLE2D(srctex, srcsamp, (half2)uv).r;

	return SAMPLE2D(paltex, palsamp, half2(index * 255.0h/256.0h + 0.5h/256.0h, 0)).bgra;
}
