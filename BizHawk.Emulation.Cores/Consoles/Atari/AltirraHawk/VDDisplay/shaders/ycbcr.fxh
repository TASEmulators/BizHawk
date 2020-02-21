#ifndef YCBCR_FXH
#define YCBCR_FXH

extern Texture2D ytex : register(t0);
extern Texture2D cbtex : register(t1);
extern Texture2D crtex : register(t2);
extern SamplerState ysamp : register(s0);
extern SamplerState cbsamp : register(s1);
extern SamplerState crsamp : register(s2);

half4 FP_BlitYCbCr(
	float4 pos : SV_Position,
	half2 uv0 : TEXCOORD0,
	half2 uv1 : TEXCOORD1,
	uniform int colorSpace) : SV_Target
{
	half y = (half)SAMPLE2D(ytex, ysamp, uv0).r;
	half cb = (half)SAMPLE2D(cbtex, cbsamp, uv1).r;
	half cr = (half)SAMPLE2D(crtex, crsamp, uv1).r;
	
	return half4(ConvertYCbCrToRGB(y, cb, cr, colorSpace).rgb, 1);
}

half4 FP_BlitYCbCr_601_LR(float4 pos : SV_Position,
	half2 uv0 : TEXCOORD0,
	half2 uv1 : TEXCOORD1) : SV_Target
{
	return FP_BlitYCbCr(pos, uv0, uv1, COLOR_SPACE_REC601);
}

half4 FP_BlitYCbCr_601_FR(float4 pos : SV_Position,
	half2 uv0 : TEXCOORD0,
	half2 uv1 : TEXCOORD1) : SV_Target
{
	return FP_BlitYCbCr(pos, uv0, uv1, COLOR_SPACE_REC601_FR);
}

half4 FP_BlitYCbCr_709_LR(float4 pos : SV_Position,
	half2 uv0 : TEXCOORD0,
	half2 uv1 : TEXCOORD1) : SV_Target
{
	return FP_BlitYCbCr(pos, uv0, uv1, COLOR_SPACE_REC709);
}

half4 FP_BlitYCbCr_709_FR(float4 pos : SV_Position,
	half2 uv0 : TEXCOORD0,
	half2 uv1 : TEXCOORD1) : SV_Target
{
	return FP_BlitYCbCr(pos, uv0, uv1, COLOR_SPACE_REC709_FR);
}

half4 FP_BlitY_LR(float4 pos : SV_Position,
	half2 uv0 : TEXCOORD0) : SV_Target
{
	half y = (half)SAMPLE2D(ytex, ysamp, uv0).r;
	half rgb = (y - (16.0h / 255.0h)) * (255.0h / 219.0h);
	
	return half4(rgb.rrr, 1);
}

half4 FP_BlitY_FR(float4 pos : SV_Position,
	half2 uv0 : TEXCOORD0) : SV_Target
{
	half y = (half)SAMPLE2D(ytex, ysamp, uv0).r;
	
	return half4(y.rrr, 1);
}

#endif
