//	VirtualDub - Video processing and capture application
//	A/V interface library
//	Copyright (C) 1998-2007 Avery Lee
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

#ifndef f_VD2_RIZA_AVI_H
#define f_VD2_RIZA_AVI_H

#ifdef _MSC_VER
	#pragma once
#endif

#ifndef f_VD2_SYSTEM_VDTYPES_H
	#include <vd2/system/vdtypes.h>
#endif

#ifndef f_VD2_SYSTEM_BINARY_H
	#include <vd2/system/binary.h>
#endif

// This is a copy of the AVISTREAMINFO structure.
struct VDAVIStreamInfo {
	enum {
		kTypeVideo = VDMAKEFOURCC('v', 'i', 'd', 's'),
		kTypeAudio = VDMAKEFOURCC('a', 'u', 'd', 's')
	};

	uint32	fccType;
	uint32	fccHandler;
	uint32	dwFlags;
	uint32	dwCaps;
	uint16	wPriority;
	uint16	wLanguage;
	uint32	dwScale;
	uint32	dwRate;
	uint32	dwStart;
	uint32	dwLength;
	uint32	dwInitialFrames;
	uint32	dwSuggestedBufferSize;
	uint32	dwQuality;
	uint32	dwSampleSize;
	uint16	rcFrameLeft;
	uint16	rcFrameTop;
	uint16	rcFrameRight;
	uint16	rcFrameBottom;
};

// This is a copy of the BITMAPINFOHEADER structure.
struct VDAVIBitmapInfoHeader {
	enum {
		kCompressionRGB = 0,
		kCompressionRLE8 = 1,
		kCompressionRLE4 = 2,
		kCompressionBitfields = 3
	};
	uint32	biSize;
	sint32	biWidth;
	sint32	biHeight;
	uint16	biPlanes;
	uint16	biBitCount;
	uint32	biCompression;
	uint32	biSizeImage;
	sint32	biXPelsPerMeter;
	sint32	biYPelsPerMeter;
	uint32	biClrUsed;
	uint32	biClrImportant;
};

// This is a copy of the RGBQUAD structure.
struct VDAVIRGBQuad {
	uint8	rgbBlue;
	uint8	rgbGreen;
	uint8	rgbRed;
	uint8	rgbReserved;
};

#endif
