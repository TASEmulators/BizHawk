void VP_RenderBlit(
	float2 pos : POSITION,
	half4 c : COLOR0,
	float2 uv : TEXCOORD0,
	uniform float4 xform2d : register(c1),
	out float4 oPos : SV_Position,
	out half4 oD0 : COLOR0,
	out float2 oT0 : TEXCOORD0
)
{
	oPos = float4(pos.xy * xform2d.xy + xform2d.zw, 0.5, 1);
	oD0 = c;
	oT0 = uv;
	
	VP_APPLY_VIEWPORT(oPos);
}

void VP_RenderFill(
	float2 pos : POSITION,
	half4 c : COLOR0,
	uniform float4 xform2d : register(c1),
	out float4 oPos : SV_Position,
	out half4 oD0 : COLOR0
)
{
	oPos = float4(pos.xy * xform2d.xy + xform2d.zw, 0.5, 1);
	oD0 = c;
	
	VP_APPLY_VIEWPORT(oPos);
}

half4 FP_RenderFill(float4 pos : SV_Position,
		half4 c : COLOR0) : SV_Target
{	
	return c;
}

half4 FP_RenderBlit(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	return (half4)SAMPLE2D(srctex, srcsamp, (half2)uv) * c;
}

half4 FP_RenderBlitRBSwap(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	return (half4)SAMPLE2D(srctex, srcsamp, (half2)uv).bgra * c;
}

half4 FP_RenderBlitDirect(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	return (half4)SAMPLE2D(srctex, srcsamp, (half2)uv);
}

half4 FP_RenderBlitDirectRBSwap(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	return (half4)SAMPLE2D(srctex, srcsamp, (half2)uv).bgra * c;
}

half4 FP_RenderBlitStencil(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	half4 px = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv);
	
	px.a = px.b;
	px.rgb *= c.rgb;
	
	return px;
}

half4 FP_RenderBlitStencilRBSwap(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	half4 px = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv).bgra;
	
	px.a = px.b > 0 ? 255 : 0;
	px.rgb *= c.rgb;
	
	return px;
}

half4 FP_RenderBlitColor(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	half4 px = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv);
	
	return half4(px.rgb * c.rgb, c.a);
}

half4 FP_RenderBlitColorRBSwap(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	half4 px = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv).bgra;
	
	return half4(px.rgb * c.rgb, c.a);
}

half4 FP_RenderBlitColor2(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	half4 px1 = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv);
	half4 px2 = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv + half2(0.5, 0.0));
	half4 px = lerp(px1, px2, c.a);
	
	return half4(px.rgb * c.rgb, c.a);
}

half4 FP_RenderBlitColor2RBSwap(float4 pos : SV_Position,
		half4 c : COLOR0,
		float2 uv : TEXCOORD0) : SV_Target
{	
	half4 px1 = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv).bgra;
	half4 px2 = (half4)SAMPLE2D(srctex, srcsamp, (half2)uv + half2(0.5, 0.0)).bgra;
	half4 px = lerp(px1, px2, c.a);
	
	return half4(px.rgb * c.rgb, c.a);
}
