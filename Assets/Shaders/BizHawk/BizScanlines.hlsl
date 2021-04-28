void main_vertex
(
   float4 position	: POSITION,
   float2 tex : TEXCOORD0,

   uniform float4x4 modelViewProj,

   out float4 oPosition : POSITION,
   out float2 oTexcoord : TEXCOORD0
)
{
	oPosition = mul(modelViewProj, position);
	oTexcoord = tex;
}

uniform float uIntensity;

float4 main_fragment (in float2 texcoord : TEXCOORD0, in float2 wpos : VPOS, uniform sampler2D s_p : TEXUNIT0) : COLOR
{
  float4 temp = tex2D(s_p,texcoord);
	if(floor(wpos.y/2) != floor(wpos.y)/2) temp.rgb *= uIntensity;
	return temp;
}
