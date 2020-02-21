extern Texture2D rgbtex : register(t1);
extern SamplerState rgbsamp : register(s1);

////////////////////////////////////////////////////////////////////////////////////////////////////
//
//	Pal8 to RGB -- pixel shader 2.0
//
////////////////////////////////////////////////////////////////////////////////////////////////////

half4 FP_BlitRGB16_L8A8(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target {
	half4 px = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv) * (255.0h / 256.0h) + (0.5h / 256.0h);
	half4 c0 = (half4)SAMPLE2D(rgbtex, rgbsamp, half2(px.r, 0));
	half4 c1 = (half4)SAMPLE2D(rgbtex, rgbsamp, half2(px.a, 1));
	
	return c0 + c1;
}

half4 FP_BlitRGB16_R8G8(float4 pos : SV_Position, float2 uv : TEXCOORD0) : SV_Target {
	half4 px = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv) * (255.0h / 256.0h) + (0.5h / 256.0h);
	half4 c0 = (half4)SAMPLE2D(rgbtex, rgbsamp, half2(px.r, 0));
	half4 c1 = (half4)SAMPLE2D(rgbtex, rgbsamp, half2(px.g, 1));
	
	return c0 + c1;
}

half4 FP_BlitRGB24(float4 pos : SV_Position,
		float2 uv0 : TEXCOORD0,
		float2 uv1 : TEXCOORD1,
		float2 uv2 : TEXCOORD2) : SV_Target {
	half b = (half)SAMPLE2D(srctex, srcsamp, (half2)uv0);
	half g = (half)SAMPLE2D(srctex, srcsamp, (half2)uv1);
	half r = (half)SAMPLE2D(srctex, srcsamp, (half2)uv2);
	
	return half4(r, g, b, 1);
}
