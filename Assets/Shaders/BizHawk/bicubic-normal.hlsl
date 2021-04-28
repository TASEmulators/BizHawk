/* COMPATIBILITY 
   - HLSL compilers
   - Cg   compilers
*/

/*
   Copyright (C) 2010 Team XBMC
   http://www.xbmc.org
   Copyright (C) 2011 Stefanos A.
   http://www.opentk.com

This Program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2, or (at your option)
any later version.

This Program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with XBMC; see the file COPYING.  If not, write to
the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
http://www.gnu.org/copyleft/gpl.html

    From this forum post:
        http://board.byuu.org/viewtopic.php?p=33488#p33488

*/


/* Default Vertex shader */
void main_vertex
(
 float4 position	: POSITION,
 //float4 color	: COLOR,
 float2 texCoord1 : TEXCOORD0,

 uniform float4x4 modelViewProj,

 out float4 oPosition : POSITION,
 //out float4 oColor    : COLOR,
 out float2 otexCoord : TEXCOORD
 )
{
   oPosition = mul(modelViewProj, position);
   //oColor = color;
   otexCoord = texCoord1;
}

struct output
{
   float4 color : COLOR;
};

struct input
{
  float2 video_size;
  float2 texture_size;
  float2 output_size;
  float frame_count;
  float frame_direction;
  float frame_rotation;
};

float weight(float x)
{
	float ax = abs(x);
	// Mitchel-Netravali coefficients.
	// Best psychovisual result.
	const float B = 1.0 / 3.0;
	const float C = 1.0 / 3.0;

	// Sharper version.
	// May look better in some cases.
	//const float B = 0.0;
	//const float C = 0.75;

	if (ax < 1.0)
	{
		return
			(
			 pow(x, 2.0) * ((12.0 - 9.0 * B - 6.0 * C) * ax + (-18.0 + 12.0 * B + 6.0 * C)) +
			 (6.0 - 2.0 * B)
			) / 6.0;
	}
	else if ((ax >= 1.0) && (ax < 2.0))
	{
		return
			(
			 pow(x, 2.0) * ((-B - 6.0 * C) * ax + (6.0 * B + 30.0 * C)) +
			 (-12.0 * B - 48.0 * C) * ax + (8.0 * B + 24.0 * C)
			) / 6.0;
	}
	else
	{
		return 0.0;
	}
}
	
float4 weight4(float x)
{
	return float4(
			weight(x - 2.0),
			weight(x - 1.0),
			weight(x),
			weight(x + 1.0));
}

float3 pixel(float xpos, float ypos, uniform sampler2D s_p)
{
	return tex2D(s_p, float2(xpos, ypos)).rgb;
}

float3 line_run(float ypos, float4 xpos, float4 linetaps, uniform sampler2D s_p)
{
	return
		pixel(xpos.r, ypos, s_p) * linetaps.r +
		pixel(xpos.g, ypos, s_p) * linetaps.g +
		pixel(xpos.b, ypos, s_p) * linetaps.b +
		pixel(xpos.a, ypos, s_p) * linetaps.a;
}


output main_fragment (float2 tex : TEXCOORD0, uniform input IN, uniform sampler2D s_p : TEXUNIT0)
{	
        float2 stepxy = float2(1.0/IN.texture_size.x, 1.0/IN.texture_size.y);
        float2 pos = tex.xy + stepxy * 0.5;
        float2 f = frac(pos / stepxy);
		
	float4 linetaps   = weight4(1.0 - f.x);
	float4 columntaps = weight4(1.0 - f.y);

	//make sure all taps added together is exactly 1.0, otherwise some (very small) distortion can occur
	linetaps /= linetaps.r + linetaps.g + linetaps.b + linetaps.a;
	columntaps /= columntaps.r + columntaps.g + columntaps.b + columntaps.a;

	float2 xystart = (-1.5 - f) * stepxy + pos;
	float4 xpos = float4(xystart.x, xystart.x + stepxy.x, xystart.x + stepxy.x * 2.0, xystart.x + stepxy.x * 3.0);


// final sum and weight normalization
   output OUT;
   OUT.color = float4(line_run(xystart.y                 , xpos, linetaps, s_p) * columntaps.r +
                      line_run(xystart.y + stepxy.y      , xpos, linetaps, s_p) * columntaps.g +
                      line_run(xystart.y + stepxy.y * 2.0, xpos, linetaps, s_p) * columntaps.b +
                      line_run(xystart.y + stepxy.y * 3.0, xpos, linetaps, s_p) * columntaps.a,1);
   return OUT;
}

