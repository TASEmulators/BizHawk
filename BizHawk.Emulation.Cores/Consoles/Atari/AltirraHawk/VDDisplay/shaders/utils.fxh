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

#ifndef UTILS_FXH
#define UTILS_FXH

#define COLOR_SPACE_REC601		0
#define COLOR_SPACE_REC709		1
#define COLOR_SPACE_REC601_FR	2
#define COLOR_SPACE_REC709_FR	3

float4 ConvertYCbCrToRGB(float y, float cb, float cr, uniform int colorSpace) {
	float4 result;
	
	if (colorSpace == COLOR_SPACE_REC601) {
		const float3 kCoeffCr = { 1.596f, -0.813f, 0 };
		const float3 kCoeffCb = { 0, -0.391f, 2.018f };
		const float kCoeffY = 1.164f;
		const float kBiasY = -16.0f / 255.0f;
		const float kBiasC = -128.0f / 255.0f;

		result = y * kCoeffY;
		result.rgb += kCoeffCr * cr;
		result.rgb += kCoeffCb * cb;
		result.rgb += kCoeffY * kBiasY + (kCoeffCr + kCoeffCb) * kBiasC;	
	} else if (colorSpace == COLOR_SPACE_REC709) {
		const float3 kCoeffCr = { 1.793f, -0.533f, 0 };
		const float3 kCoeffCb = { 0, -0.213f, 2.112f };
		const float kCoeffY = 1.164f;
		const float kBiasY = -16.0f / 255.0f;
		const float kBiasC = -128.0f / 255.0f;

		result = y * kCoeffY;
		result.rgb += kCoeffCr * cr;
		result.rgb += kCoeffCb * cb;
		result.rgb += kCoeffY * kBiasY + (kCoeffCr + kCoeffCb) * kBiasC;	
		
		return result;
	} else if (colorSpace == COLOR_SPACE_REC601_FR) {
		const float3 kCoeffCr = { 1.402f, -0.7141363f, 0 };
		const float3 kCoeffCb = { 0, -0.3441363f, 1.772f };
		const float kCoeffY = 1.0f;
		const float kBiasY = 0.0f;
		const float kBiasC = -128.0f / 255.0f;

		result = y * kCoeffY;
		result.rgb += kCoeffCr * cr;
		result.rgb += kCoeffCb * cb;
		result.rgb += kCoeffY * kBiasY + (kCoeffCr + kCoeffCb) * kBiasC;	
	} else {
		const float3 kCoeffCr = { 1.5748f, -0.4681243f, 0 };
		const float3 kCoeffCb = { 0, -0.1873243f, 1.8556f };
		const float kCoeffY = 1.0f;
		const float kBiasY = 0.0f;
		const float kBiasC = -128.0f / 255.0f;

		result = y * kCoeffY;
		result.rgb += kCoeffCr * cr;
		result.rgb += kCoeffCb * cb;
		result.rgb += kCoeffY * kBiasY + (kCoeffCr + kCoeffCb) * kBiasC;	
	}
	
	return result;
}

#endif
