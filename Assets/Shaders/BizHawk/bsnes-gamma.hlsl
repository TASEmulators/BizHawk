//https://raw.githubusercontent.com/Themaister/Emulator-Shader-Pack/master/Cg/TV/gamma.cg

/*
   Author: Themaister
   License: Public domain
*/

// Shader that replicates gamma-ramp of bSNES/Higan.

void main_vertex
(
	float4 position : POSITION,
	float2 tex : TEXCOORD,

	uniform float4x4 modelViewProj,

	out float4 oPosition : POSITION,
	out float2 oTex : TEXCOORD
)
{
	oPosition = mul(modelViewProj, position);
	oTex = tex;
}

// Tweakables.
#define saturation 1.0
#define gamma 1.5
#define luminance 1.0

float3 grayscale(float3 col)
{
	// Non-conventional way to do grayscale,
	// but bSNES uses this as grayscale value.
	float v = dot(col, float3(0.3333,0.3333,0.3333));
	return float3(v,v,v);
}

float4 main_fragment(in float4 vpos : POSITION, in float2 tex : TEXCOORD, uniform sampler2D s0 : TEXUNIT0) : COLOR
{
	float3 res = tex2D(s0, tex).xyz;
	res = lerp(grayscale(res), res, saturation); // Apply saturation
	res = pow(res, float3(gamma,gamma,gamma)); // Apply gamma
	return float4(saturate(res * luminance), 1.0);
}

