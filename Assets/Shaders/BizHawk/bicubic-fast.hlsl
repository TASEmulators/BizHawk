/* COMPATIBILITY 
   - HLSL compilers
   - Cg   compilers
*/

/*
   bicubic-fast Shader

   Copyright (C) 2011-2015 Hyllian - sergiogdb@gmail.com

   Permission is hereby granted, free of charge, to any person obtaining a copy
   of this software and associated documentation files (the "Software"), to deal
   in the Software without restriction, including without limitation the rights
   to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
   copies of the Software, and to permit persons to whom the Software is
   furnished to do so, subject to the following conditions:

   The above copyright notice and this permission notice shall be included in
   all copies or substantial portions of the Software.

   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
   IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
   FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
   AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
   LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
   OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
   THE SOFTWARE.

*/

const static float4x4 invX = float4x4(-1.0/6.0,  0.5, -1.0/3.0, 0.0,
                                           0.5, -1.0,     -0.5, 1.0,
                                          -0.5,  0.5,      1.0, 0.0,
                                       1.0/6.0,  0.0, -1.0/6.0, 0.0);


// Can't init statically from variable, even though it's const ...
//const static float4x4 invY = transpose(invX);
const static float4x4 invY = float4x4(-1.0/6.0,  0.5, -0.5,  1.0/6.0,
                                           0.5, -1.0,  0.5,      0.0,
                                      -1.0/3.0, -0.5,  1.0, -1.0/6.0,
                                           0.0,  1.0,  0.0,      0.0);

struct input
{
	float2 video_size;
	float2 texture_size;
	float2 output_size;
        float  frame_count;
        float  frame_direction;
	float frame_rotation;
};


struct out_vertex {
	float2 texCoord : TEXCOORD0;
	float4 t1       : TEXCOORD1;
	float4 t2       : TEXCOORD2;
	float4 t3       : TEXCOORD3;
	float4 t4       : TEXCOORD4;
	float4 t5       : TEXCOORD5;
	float4 t6       : TEXCOORD6;
	float4 t7       : TEXCOORD7;
	float2 t8       : COLOR0;
};

/*    VERTEX_SHADER    */
out_vertex main_vertex
(
	float4 position	: POSITION,
   out float4 oPosition : POSITION,
	float2 texCoord1 : TEXCOORD0,

   	uniform float4x4 modelViewProj,
	uniform input IN
)
{
	float2 ps = float2(1.0/IN.texture_size.x, 1.0/IN.texture_size.y);
	float dx = ps.x;
	float dy = ps.y;

   oPosition = mul(modelViewProj, position);

	// This line fix a bug in ATI cards.
	float2 tex = texCoord1 + float2(0.0000001, 0.0000001);

	out_vertex OUT = { 
		tex,
		float4(tex,tex) + float4(   -dx,    -dy,    0.0,    -dy), 
		float4(tex,tex) + float4(    dx,    -dy, 2.0*dx,    -dy),
		float4(tex,tex) + float4(   -dx,    0.0,     dx,    0.0), 
		float4(tex,tex) + float4(2.0*dx,    0.0,    -dx,     dy),
		float4(tex,tex) + float4(   0.0,     dy,     dx,     dy), 
		float4(tex,tex) + float4(2.0*dx,     dy,    -dx, 2.0*dy),
		float4(tex,tex) + float4(   0.0, 2.0*dy,     dx, 2.0*dy),
		tex             + float2(2.0*dx, 2.0*dy)
	};


	return OUT;
}


float4 main_fragment(in out_vertex VAR, uniform sampler2D s_p : TEXUNIT0, uniform input IN) : COLOR
{

  float2 fp = frac(VAR.texCoord*IN.texture_size);
  float3 c00 = tex2D(s_p, VAR.t1.xy).xyz;
  float3 c01 = tex2D(s_p, VAR.t1.zw).xyz;
  float3 c02 = tex2D(s_p, VAR.t2.xy).xyz;
  float3 c03 = tex2D(s_p, VAR.t2.zw).xyz;
  float3 c10 = tex2D(s_p, VAR.t3.xy).xyz;
  float3 c11 = tex2D(s_p, VAR.texCoord).xyz;
  float3 c12 = tex2D(s_p, VAR.t3.zw).xyz;
  float3 c13 = tex2D(s_p, VAR.t4.xy).xyz;
  float3 c20 = tex2D(s_p, VAR.t4.zw).xyz;
  float3 c21 = tex2D(s_p, VAR.t5.xy).xyz;
  float3 c22 = tex2D(s_p, VAR.t5.zw).xyz;
  float3 c23 = tex2D(s_p, VAR.t6.xy).xyz;
  float3 c30 = tex2D(s_p, VAR.t6.zw).xyz;
  float3 c31 = tex2D(s_p, VAR.t7.xy).xyz;
  float3 c32 = tex2D(s_p, VAR.t7.zw).xyz;
  float3 c33 = tex2D(s_p, VAR.t8.xy).xyz;


  float4x4   red_matrix = float4x4(c00.x, c01.x, c02.x, c03.x,
                                   c10.x, c11.x, c12.x, c13.x,
                                   c20.x, c21.x, c22.x, c23.x,
                                   c30.x, c31.x, c32.x, c33.x);

  float4x4 green_matrix = float4x4(c00.y, c01.y, c02.y, c03.y,
                                   c10.y, c11.y, c12.y, c13.y,
                                   c20.y, c21.y, c22.y, c23.y,
                                   c30.y, c31.y, c32.y, c33.y);

  float4x4  blue_matrix = float4x4(c00.z, c01.z, c02.z, c03.z,
                                   c10.z, c11.z, c12.z, c13.z,
                                   c20.z, c21.z, c22.z, c23.z,
                                   c30.z, c31.z, c32.z, c33.z);


  float4x1 invX_Px = mul(invX, float4x1(fp.x*fp.x*fp.x, fp.x*fp.x, fp.x, 1.0));
  float1x4 Py_invY = mul(float1x4(fp.y*fp.y*fp.y, fp.y*fp.y, fp.y, 1.0), invY);


  float red   = mul(Py_invY, mul(  red_matrix, invX_Px));
  float green = mul(Py_invY, mul(green_matrix, invX_Px));
  float blue  = mul(Py_invY, mul( blue_matrix, invX_Px));

  return float4(red, green, blue, 1.0);
}

