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

#ifndef STDDEFS_FXH
#define STDDEFS_FXH

struct VertexInput {
	float4		pos		: POSITION;
	float2		uv		: TEXCOORD0;
	float2		uv2		: TEXCOORD1;
};

float4 vd_vpsize : register(c0);
float4 vd_srcsize : register(c1);
float4 vd_texsize : register(c2);
float4 vd_tex2size : register(c3);
float4 vd_tempsize : register(c4);
float4 vd_interphtexsize : register(c5);
float4 vd_interpvtexsize : register(c6);
float4 vd_interptexsize : register(c7);
float4 vd_fieldinfo : register(c8);
float2 vd_chromauvscale : register(c9);
float2 vd_chromauvoffset : register(c10);
float2 vd_pixelsharpness : register(c11);

texture vd_srctexture;
texture vd_src2atexture;
texture vd_src2btexture;
texture vd_src2ctexture;
texture vd_src2dtexture;
texture vd_srcpaltexture;
texture vd_temptexture;
texture vd_temp2texture;
texture vd_cubictexture;
texture vd_hevenoddtexture;
texture vd_dithertexture;
texture vd_interphtexture;
texture vd_interpvtexture;
texture vd_interptexture;

extern sampler samp0 : register(s0);
extern sampler samp1 : register(s1);
extern sampler samp2 : register(s2);
extern sampler samp3 : register(s3);
extern sampler samp4 : register(s4);

#endif
