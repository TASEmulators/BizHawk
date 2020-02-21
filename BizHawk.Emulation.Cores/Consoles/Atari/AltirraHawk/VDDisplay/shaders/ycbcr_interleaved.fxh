#ifndef YCBCR_INTERLEAVED_FXH
#define YCBCR_INTERLEAVED_FXH

extern Texture2D ctex : register(t1);
extern SamplerState csamp : register(s1);

half4 FP_BlitUYVY(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace, uniform bool rbswap) : SV_Target {
	half4 pxY = (half4)SAMPLE2D(ytex, ysamp, uv0);
	half4 pxC = (half4)SAMPLE2D(ctex, csamp, uv1);
	
	half cb = rbswap ? pxC.b : pxC.r;
	half cr = rbswap ? pxC.r : pxC.b;
	half y = frac(flip.x) >= 0.5h ? pxY.a : pxY.g;
	
	return half4(ConvertYCbCrToRGB(y, cb, cr, colorSpace).rgb, 1);
}

half4 FP_BlitUYVY_601_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC601, false);
}

half4 FP_BlitUYVY_601_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC601_FR, false);
}

half4 FP_BlitUYVY_709_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC709, false);
}

half4 FP_BlitUYVY_709_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC709_FR, false);
}

half4 FP_BlitUYVYRBSwap_601_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC601, true);
}

half4 FP_BlitUYVYRBSwap_601_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC601_FR, true);
}

half4 FP_BlitUYVYRBSwap_709_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC709, true);
}

half4 FP_BlitUYVYRBSwap_709_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitUYVY(pos, uv0, uv1, flip, COLOR_SPACE_REC709_FR, true);
}

///////////////////////////////////////////////////////////////////////////

half4 FP_BlitYUYV(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace, uniform bool rbswap) : SV_Target {
	half4 pxY = (half4)SAMPLE2D(ytex, ysamp, uv0);
	half4 pxC = (half4)SAMPLE2D(ctex, csamp, uv1);
	
	half cb = pxC.g;
	half cr = pxC.a;
	half y = frac(flip.x) < 0.5h ? (rbswap ? pxY.b : pxY.r) : (rbswap ? pxY.r : pxY.b);
	
	return half4(ConvertYCbCrToRGB(y, cb, cr, colorSpace).rgb, 1);
}

half4 FP_BlitYUYV_601_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC601, false);
}

half4 FP_BlitYUYV_601_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC601_FR, false);
}

half4 FP_BlitYUYV_709_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC709, false);
}

half4 FP_BlitYUYV_709_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC709_FR, false);
}

half4 FP_BlitYUYVRBSwap_601_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC601, true);
}

half4 FP_BlitYUYVRBSwap_601_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC601_FR, true);
}

half4 FP_BlitYUYVRBSwap_709_LR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC709, true);
}

half4 FP_BlitYUYVRBSwap_709_FR(float4 pos : SV_Position, half2 uv0 : TEXCOORD0, half2 uv1 : TEXCOORD1, half2 flip : TEXCOORD2, uniform int colorSpace) : SV_Target {
	return FP_BlitYUYV(pos, uv0, uv1, flip, COLOR_SPACE_REC709_FR, true);
}

#endif
