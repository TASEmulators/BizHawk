//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#if ENGINE_D3D9
	#define SCREENFX_SAMPLE_SRC(uv)			(half4(tex2D(samp0, uv)))
	#define SCREENFX_SAMPLE_BASE(uv)		(half4(tex2D(samp1, uv)))
	#define SCREENFX_SAMPLE_GAMMA(uv)		(half4(tex2D(samp1, uv)))
	#define SCREENFX_SAMPLE_SCANLINE(uv)	(half4(tex2D(samp2, uv)))
	#define VP_APPLY_VIEWPORT(oPos)

	#define SCREENFX_REG(spc, reg)			: register(spc, reg)

	#define SV_Position POSITION
#else
	#define SCREENFX_SAMPLE_SRC(uv)			SAMPLE2D(srctex, srcsamp, uv)
	#define SCREENFX_SAMPLE_BASE(uv)		SAMPLE2D(basetex, basesamp, uv)
	#define SCREENFX_SAMPLE_GAMMA(uv)		SAMPLE2D(gammatex, gammasamp, uv)
	#define SCREENFX_SAMPLE_SCANLINE(uv)	SAMPLE2D(scanlinetex, scanlinesamp, uv)

	#define SCREENFX_REG(spc, reg)

	extern Texture2D scanlinetex : register(t1);
	extern SamplerState scanlinesamp : register(s1);

	extern Texture2D gammatex : register(t2);
	extern SamplerState gammasamp : register(s2);
#endif

///////////////////////////////////////////////////////////////////////////

#if ENGINE_D3D9
	extern float4 vsScanlineInfo : register(vs, c16);
	extern float4 vsDistortionInfo : register(vs, c17);
#else
	extern float4 vsScanlineInfo : register(vs, c1);
	extern float4 vsDistortionInfo : register(vs, c2);
#endif

cbuffer PS_ScreenFX {
	float4 psSharpnessInfo		SCREENFX_REG(ps, c16);
	float4 psDistortionScales	SCREENFX_REG(ps, c17);
	float4 psImageUVSize		SCREENFX_REG(ps, c18);
	float3x3 colorCorrectMatrix	SCREENFX_REG(ps, c19);
};

half4 FP_PALArtifacting(
	float4 pos : SV_Position,
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1) : SV_Target0
{
	// Apply PAL chroma blending.
	half3 c = SCREENFX_SAMPLE_SRC(uv0).rgb;
	half3 c2 = SCREENFX_SAMPLE_SRC(uv1).rgb;

	// blend chroma only
	half3 chromaBias = c2 - c;

	chromaBias -= dot(chromaBias, half3(0.30, 0.59, 0.11));

	c += chromaBias * 0.5h;

	// Expand signed palette from [-0.5, 1.5] to [0, 1].
	const float scale = 255.0f / 127.0f;
	const float bias = 64.0f / 255.0f;

	c = c * scale - bias * scale;

	return half4(c, 0);
}

void VP_ScreenFX(
	float2 pos : POSITION,
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1,
	out float4 oPos : SV_Position,
	out float2 oT0 : TEXCOORD0,
	out float2 oT1 : TEXCOORD1)
{
	oPos = float4(pos.xy, 0.5, 1);
	oT0 = uv0;
	oT1 = uv1 * vsDistortionInfo.x + vsDistortionInfo.y;
	
	VP_APPLY_VIEWPORT(oPos);
}

void VP_ScreenFXScanlines(
	float2 pos : POSITION,
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1,
	out float4 oPos : SV_Position,
	out float2 oT0 : TEXCOORD0,
	out float2 oT1 : TEXCOORD1,
	out float2 oTScanMask : TEXCOORD2)
{
	oPos = float4(pos.xy, 0.5, 1);
	oT0 = uv0;
	oT1 = uv1 * vsDistortionInfo.x + vsDistortionInfo.y;
	oTScanMask = float2(0, uv1.y * vsScanlineInfo.x);
	
	VP_APPLY_VIEWPORT(oPos);
}

half4 FP_ScreenFX(
	float4 pos,
	float2 uv0,
	float2 uv1,
	float2 uvScanMask,
	uniform bool doSharp,
	uniform bool doScanlines,
	uniform bool doGammaCorrection,
	uniform bool doColorCorrection,
	uniform bool doDistortion)
{
	half3 c;

	// distortion
	float2 uv = uv0;
	half distortionMask = 1;

	if (doDistortion) {
		// Reverse map from the screen to the source image by ray casting from the screen to
		// the surface of an ellipsoid and then reproject to the source image. See
		// screenfx.cpp for the full explanation.

		float2 v = uv1;
		float2 v2 = v * psDistortionScales.xy;
		float d = psDistortionScales.z - dot(v2, v2);

		v *= rsqrt(max(d, 1e-5f));

		float2 rawUV = v + 0.5f;

		distortionMask = 1 - any(rawUV - saturate(rawUV));

		uv = rawUV * psImageUVSize.xy;
		uv.y += psImageUVSize.z;
		uvScanMask.y = rawUV.y * psImageUVSize.w;
	}

	// do stretchblt
	if (doSharp) {
		float2 f = floor(uv + 0.5f);
		float2 d = (f - uv);
	
		uv = (f - saturate(d * psSharpnessInfo.xy + 0.5f) + 0.5f) * psSharpnessInfo.wz;
	}

	c = SCREENFX_SAMPLE_SRC(uv).rgb;

	c *= distortionMask;
	
	// Apply gamma correction or color correction.
	//
	// If we are doing a gamma correction, this is simply a dependent lookup of RGB
	// through the gamma ramp.
	//
	// If we're doing color correction, this involves conversion to linear space,
	// then a matrix multiplication, then conversion back to gamma space.

	if (doColorCorrection || doGammaCorrection) {
		if (doColorCorrection) {
			// Convert to linear space. Note that we are using a straight 2.2 curve here
			// for NTSC, not the 2.4 curve with linear section that would be used with
			// sRGB.

			c = saturate(c);
			c.r = pow(c.r, 2.2h);
			c.g = pow(c.g, 2.2h);
			c.b = pow(c.b, 2.2h);

			// transform color spaces
			c = mul(c, (half3x3)colorCorrectMatrix);
		}

		// Apply gamma correction, or linear->gamma for color correction. The lookup
		// texture is 256 texels wide with the endpoints being the first and last
		// texels, so the U coordinates need to be contracted slightly for an accurate
		// lookup.

		c.r = SCREENFX_SAMPLE_GAMMA(float2(c.r * (255.0h / 256.0h) + 0.5h / 256.0h, 0)).r;
		c.g = SCREENFX_SAMPLE_GAMMA(float2(c.g * (255.0h / 256.0h) + 0.5h / 256.0h, 0)).r;
		c.b = SCREENFX_SAMPLE_GAMMA(float2(c.b * (255.0h / 256.0h) + 0.5h / 256.0h, 0)).r;
	}

	// Apply scanlines.
	//
	// This mask is applied in linear lighting, but with the gamma conversions optimized out
	// as follows:
	//
	//		c' = (c^gamma * mask) ^ (1/gamma)
	//		   = (c^gamma)^(1/gamma) * mask^(1/gamma)
	//		   = c * mask^(1/gamma)
	//
	// The gamma correction is then baked into the mask texture.

	if (doScanlines) {
		half3 scanMask = SCREENFX_SAMPLE_SCANLINE(uvScanMask).rgb;

		c *= scanMask;
	}

	return half4(c, 1);
}

half4 FP_ScreenFX_PtLinear_NoScanlines_Linear(float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, false, false, false, false); }
half4 FP_ScreenFX_PtLinear_NoScanlines_Gamma (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, false, true,  false, false); }
half4 FP_ScreenFX_PtLinear_NoScanlines_CC    (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, false, false, true , false); }
half4 FP_ScreenFX_Sharp_NoScanlines_Linear   (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  false, false, false, false); }
half4 FP_ScreenFX_Sharp_NoScanlines_Gamma    (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  false, true,  false, false); }
half4 FP_ScreenFX_Sharp_NoScanlines_CC       (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  false, false, true , false); }

half4 FP_ScreenFX_PtLinear_Scanlines_Linear  (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1, float2 uvScanMask : TEXCOORD2) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, uvScanMask, false, true,  false, false, false); }
half4 FP_ScreenFX_PtLinear_Scanlines_Gamma   (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1, float2 uvScanMask : TEXCOORD2) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, uvScanMask, false, true,  true,  false, false); }
half4 FP_ScreenFX_PtLinear_Scanlines_CC      (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1, float2 uvScanMask : TEXCOORD2) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, uvScanMask, false, true,  false, true , false); }
half4 FP_ScreenFX_Sharp_Scanlines_Linear     (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1, float2 uvScanMask : TEXCOORD2) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, uvScanMask, true,  true,  false, false, false); }
half4 FP_ScreenFX_Sharp_Scanlines_Gamma      (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1, float2 uvScanMask : TEXCOORD2) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, uvScanMask, true,  true,  true,  false, false); }
half4 FP_ScreenFX_Sharp_Scanlines_CC         (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1, float2 uvScanMask : TEXCOORD2) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, uvScanMask, true,  true,  false, true , false); }

half4 FP_ScreenFX_Distort_PtLinear_NoScanlines_Linear(float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, false, false, false, true); }
half4 FP_ScreenFX_Distort_PtLinear_NoScanlines_Gamma (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, false, true,  false, true); }
half4 FP_ScreenFX_Distort_PtLinear_NoScanlines_CC    (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, false, false, true , true); }
half4 FP_ScreenFX_Distort_Sharp_NoScanlines_Linear   (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  false, false, false, true); }
half4 FP_ScreenFX_Distort_Sharp_NoScanlines_Gamma    (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  false, true,  false, true); }
half4 FP_ScreenFX_Distort_Sharp_NoScanlines_CC       (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  false, false, true , true); }

half4 FP_ScreenFX_Distort_PtLinear_Scanlines_Linear  (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, true,  false, false, true); }
half4 FP_ScreenFX_Distort_PtLinear_Scanlines_Gamma   (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, true,  true,  false, true); }
half4 FP_ScreenFX_Distort_PtLinear_Scanlines_CC      (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, false, true,  false, true , true); }
half4 FP_ScreenFX_Distort_Sharp_Scanlines_Linear     (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  true,  false, false, true); }
half4 FP_ScreenFX_Distort_Sharp_Scanlines_Gamma      (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  true,  true,  false, true); }
half4 FP_ScreenFX_Distort_Sharp_Scanlines_CC         (float4 pos : SV_Position, float2 uv0 : TEXCOORD0, float2 uv1 : TEXCOORD1) : SV_Target0 { return FP_ScreenFX(pos, uv0, uv1, 0, true,  true,  false, true , true); }

///////////////////////////////////////////////////////////////////////////

cbuffer VpBloom {
	float4 vpBloomViewport		SCREENFX_REG(vs, c16);
	float2 vpBloomUVOffset		SCREENFX_REG(vs, c17);
	float2 vpBloomBlurOffsets1	SCREENFX_REG(vs, c18);
	float4 vpBloomBlurOffsets2	SCREENFX_REG(vs, c19);
};

cbuffer FpBloom {
	float4 fpBloomWeights		SCREENFX_REG(ps, c16);
	float2 fpBloomThresholds	SCREENFX_REG(ps, c17);
	float2 fpBloomScales		SCREENFX_REG(ps, c18);
}

void VP_Bloom1(
	float2 pos : POSITION,
	float2 uv0 : TEXCOORD0,
	out float2 oT0 : TEXCOORD0,
	out float2 oT1 : TEXCOORD1,
	out float2 oT2 : TEXCOORD2,
	out float2 oT3 : TEXCOORD3,
	out float4 oPos : SV_Position)
{
	oPos = float4(pos.xy, 0.5, 1);
	oT0 = uv0 + vpBloomUVOffset*float2(-1,-1);
	oT1 = uv0 + vpBloomUVOffset*float2(+1,-1);
	oT2 = uv0 + vpBloomUVOffset*float2(-1,+1);
	oT3 = uv0 + vpBloomUVOffset*float2(+1,+1);
	
	VP_APPLY_VIEWPORT(oPos);
}

half4 FP_Bloom1(
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1,
	float2 uv2 : TEXCOORD2,
	float2 uv3 : TEXCOORD3) : SV_Target0
{
	half3 c;

	c = SCREENFX_SAMPLE_SRC(uv0).rgb * 0.25h
	  + SCREENFX_SAMPLE_SRC(uv1).rgb * 0.25h
	  + SCREENFX_SAMPLE_SRC(uv2).rgb * 0.25h
	  + SCREENFX_SAMPLE_SRC(uv3).rgb * 0.25h;

	return half4(c*c * fpBloomThresholds.x + fpBloomThresholds.y, 0);
}

half4 FP_Bloom1A(
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1,
	float2 uv2 : TEXCOORD2,
	float2 uv3 : TEXCOORD3) : SV_Target0
{
	half3 c;

	c = SCREENFX_SAMPLE_SRC(uv0).rgb * 0.25h
	  + SCREENFX_SAMPLE_SRC(uv1).rgb * 0.25h
	  + SCREENFX_SAMPLE_SRC(uv2).rgb * 0.25h
	  + SCREENFX_SAMPLE_SRC(uv3).rgb * 0.25h;

	return half4(c, 0);
}

void VP_Bloom2(
	float2 pos : POSITION,
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1,
	out float2 oT0 : TEXCOORD0,
	out float2 oT1 : TEXCOORD1,
	out float2 oT2 : TEXCOORD2,
	out float2 oT3 : TEXCOORD3,
	out float2 oT4 : TEXCOORD4,
	out float2 oT5 : TEXCOORD5,
	out float2 oT6 : TEXCOORD6,
	out float4 oPos : SV_Position)
{
	oPos = float4(pos.xy, 0.5, 1);
	oT0 = uv0;
	oT1 = uv0 + vpBloomBlurOffsets1;
	oT2 = uv0 - vpBloomBlurOffsets1;
	oT3 = uv0 + vpBloomBlurOffsets2.xy;
	oT4 = uv0 - vpBloomBlurOffsets2.xy;
	oT5 = uv0 + vpBloomBlurOffsets2.zw;
	oT6 = uv0 - vpBloomBlurOffsets2.zw;
	
	VP_APPLY_VIEWPORT(oPos);
}

half4 FP_Bloom2(
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1,
	float2 uv2 : TEXCOORD2,
	float2 uv3 : TEXCOORD3,
	float2 uv4 : TEXCOORD4,
	float2 uv5 : TEXCOORD5,
	float2 uv6 : TEXCOORD6) : SV_Target0
{
	half3 c;

	c  =  SCREENFX_SAMPLE_SRC(uv0).rgb * fpBloomWeights.x;
	c += (SCREENFX_SAMPLE_SRC(uv1).rgb + SCREENFX_SAMPLE_SRC(uv2).rgb) * fpBloomWeights.y;
	c += (SCREENFX_SAMPLE_SRC(uv3).rgb + SCREENFX_SAMPLE_SRC(uv4).rgb) * fpBloomWeights.z;
	c += (SCREENFX_SAMPLE_SRC(uv5).rgb + SCREENFX_SAMPLE_SRC(uv6).rgb) * fpBloomWeights.w;

	return half4(c, 0);
}

void VP_Bloom3(
	float2 pos : POSITION,
	float2 uv0 : TEXCOORD0,
	float2 uv1 : TEXCOORD1,
	out float2 oT0 : TEXCOORD0,
	out float2 oT1 : TEXCOORD1,
	out float4 oPos : SV_Position)
{
	oPos = float4(pos.xy, 0.5, 1);
	oT0 = uv0;
	oT1 = uv1;
	
	VP_APPLY_VIEWPORT(oPos);
}

half4 FP_Bloom3(
	float2 uvBase : TEXCOORD0,
	float2 uvBlur : TEXCOORD1,
	Texture2D basetex : register(t1),
	SamplerState basesamp : register(s1)) : SV_Target0
{
	half3 c = SCREENFX_SAMPLE_SRC(uvBlur).rgb;
	half3 d = SCREENFX_SAMPLE_BASE(uvBase).rgb;

	c = d*d * fpBloomScales.x + c * fpBloomScales.y;

	return half4(sqrt(c), 0);
}
