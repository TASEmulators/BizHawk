extern Texture2D cubicpttex : register(ps, t1);
extern Texture2D filtertex : register(ps, t2);
extern SamplerState cubicptsamp : register(ps, s1);
extern SamplerState filtersamp : register(ps, s2);

void VP_StretchBltCubic(
		float3 pos : POSITION,
		float2 uv0 : TEXCOORD0,
		float2 uv1 : TEXCOORD1,
		float2 uv2 : TEXCOORD2,
		float2 uvfilt : TEXCOORD3,
		out float4 oPos : SV_Position,
		out float2 oT0 : TEXCOORD0,
		out float2 oT1 : TEXCOORD1,
		out float2 oT2 : TEXCOORD2,
		out float2 oT3 : TEXCOORD3)
{
	oPos = float4(pos.xyz, 1.0f);
	oT0 = uv0;
	oT1 = uv1;
	oT2 = uv2;
	oT3 = uvfilt;
	
	VP_APPLY_VIEWPORT(oPos);
}

half4 FP_StretchBltCubic(float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1, float2 uv2 : TEXCOORD2, float2 uvfilt : TEXCOORD3) : SV_Target {
	half4 weights = (half4)SAMPLE2D(filtertex, filtersamp, uvfilt);

	half4 p1 = (half4)SAMPLE2D(cubicpttex, cubicptsamp, uv0);
	half4 p2 = (half4)SAMPLE2D(srctex, srcsamp, uv1);
	half4 p3 = (half4)SAMPLE2D(cubicpttex, cubicptsamp, uv1);
	half4 p4 = (half4)SAMPLE2D(cubicpttex, cubicptsamp, uv2);
	
	weights.rg *= 0.25h;
	
	half4 c1 = (half4)lerp(p4, p1, weights.b);
	half4 c2 = (half4)lerp(p2, p3, weights.g);
	return (c2 - c1) * weights.r + c2;
}
