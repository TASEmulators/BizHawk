
struct input
{
   float2 video_size;
   float2 texture_size;
   float2 output_size;
};

void main_vertex
(
   float4 position	: POSITION,
   out float4 oPosition : POSITION,
   uniform float4x4 modelViewProj,

   float2 tex : TEXCOORD,

   uniform input IN,
   out float2 oTexcoord : TEXCOORD
)
{
   oPosition = mul(modelViewProj, position);
	oTexcoord = tex;
}

uniform float uIntensity;

float4 main_fragment (in float2 texcoord : TEXCOORD, in float2 wpos : WPOS, uniform sampler2D s_p : TEXUNIT0) : COLOR
{
  float4 temp = tex2D(s_p,texcoord);
	if(floor(wpos.y/2) != floor(wpos.y)/2) temp.rgb *= uIntensity;
	return temp;
}
