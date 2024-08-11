void main_vertex
(
	float4 position	: POSITION,
	float2 tex : TEXCOORD0,

	uniform float4x4 modelViewProj,

	out float4 oPosition : POSITION,
	out float2 oTex0 : TEXCOORD0,
	out float oTex1 : TEXCOORD1
)
{
	oPosition = mul(modelViewProj, position);
	oTex0 = tex;
	oTex1 = position.y;
}

uniform float uIntensity;

float4 main_fragment (in float4 vpos : POSITION, in float2 tex0 : TEXCOORD0, in float tex1 : TEXCOORD1, uniform sampler2D s_p : TEXUNIT0) : COLOR
{
	float4 temp = tex2D(s_p, tex0);
	if (floor(tex1 / 2) != floor(tex1) / 2) temp.rgb *= uIntensity;
	return temp;
}
