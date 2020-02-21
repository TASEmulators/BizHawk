//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2008 Avery Lee
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
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#ifndef DISPLAYDX9_PS1_FXH
#define DISPLAYDX9_PS1_FXH

////////////////////////////////////////////////////////////////////////////////////////////////////
//
//	Pixel shader 1.1 boxlinear path
//
////////////////////////////////////////////////////////////////////////////////////////////////////

struct VertexOutputBoxlinear1_1 {
	float4	pos		: POSITION;
	float2	uvfilt	: TEXCOORD0;
	float2	uvsrc	: TEXCOORD1;
};

VertexOutputBoxlinear1_1 VertexShaderBoxlinear1_1(VertexInput IN) {
	VertexOutputBoxlinear1_1 OUT;
	
	OUT.pos = IN.pos;
	OUT.uvfilt = IN.uv2 * vd_vpsize.xy * vd_interptexsize.wz;
	OUT.uvsrc = IN.uv + float2(0, vd_fieldinfo.y)*vd_texsize.wz;
	return OUT;
}

pixelshader PixelShaderBoxlinear1_1 = asm {
	ps_1_1
	tex t0
	texbem t1, t0
	mov r0, t1
};

technique boxlinear_1_1 {
	pass <
		string vd_bumpenvscale="vd_texsize";
	> {
		VertexShader = compile vs_1_1 VertexShaderBoxlinear1_1();
		PixelShader = <PixelShaderBoxlinear1_1>;
		
		Texture[0] = <vd_interptexture>;
		AddressU[0] = Clamp;
		AddressV[0] = Clamp;
		MipFilter[0] = None;
		MinFilter[0] = Point;
		MagFilter[0] = Point;

		Texture[1] = <vd_srctexture>;
		AddressU[1] = Clamp;
		AddressV[1] = Clamp;
		MipFilter[1] = None;
		MinFilter[1] = Linear;
		MagFilter[1] = Linear;
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////
//
//	Pal8 to RGB -- pixel shader 1.1
//
//	Note: Intel 965 Express chipsets are reported to cut corners on precision here, which prevents
//	this shader from working properly (colors 0 and 1 are indistinguishable). As a workaround, we
//	use a PS2.0 shader if available.
//
////////////////////////////////////////////////////////////////////////////////////////////////////

void VS_Pal8_to_RGB_1_1(
	float4 pos : POSITION,
	float2 uv : TEXCOORD0,
	float2 uv2 : TEXCOORD1,
	out float4 oPos : POSITION,
	out float2 oT0 : TEXCOORD0,
	out float3 oT1 : TEXCOORD1,
	out float3 oT2 : TEXCOORD2)
{
	oPos = pos;
	oT0 = uv;
	oT1 = float3(0, 255.75f / 256.0f, 0);
	oT2 = float3(0, 0, 0);
}
	

technique pal8_to_rgb_1_1 {
	pass < string vd_viewport = "unclipped,unclipped"; > {
		VertexShader = compile vs_1_1 VS_Pal8_to_RGB_1_1();
		PixelShader = asm {
			ps_1_1
			tex t0
			texm3x2pad t1, t0
			texm3x2tex t2, t0
			mov r0, t2
		};
		
		Texture[0] = <vd_srctexture>;
		AddressU[0] = Clamp;
		AddressV[0] = Clamp;
		MinFilter[0] = Point;
		MagFilter[0] = Point;
		
		Texture[2] = <vd_srcpaltexture>;
		AddressU[2] = Clamp;
		AddressV[2] = Clamp;
		MinFilter[2] = Point;
		MagFilter[2] = Point;
	}
}

#endif
