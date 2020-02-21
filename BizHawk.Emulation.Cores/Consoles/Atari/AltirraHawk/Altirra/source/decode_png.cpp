//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

#include "stdafx.h"
#include <vd2/system/error.h>
#include <vd2/system/vdtypes.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/zip.h>
#include <vd2/system/binary.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmaputils.h>
#include "decode_png.h"
#include "common_png.h"
#include <stdio.h>

using namespace nsVDPNG;

///////////////////////////////////////////////////////////////////////////

namespace {
	struct PNGHeader {
		uint32	width;
		uint32	height;
		uint8	depth;
		uint8	colortype;
		uint8	compression;
		uint8	filter;
		uint8	interlacing;
	};

	unsigned long PNGDecodeNetwork32(const uint8 *src) {
		return (src[0]<<24) + (src[1]<<16) + (src[2]<<8) + src[3];
	}

	int PNGPaethPredictor(int a, int b, int c) {
		int p  = a + b - c;
		int pa = abs(p - a);
		int pb = abs(p - b);
		int pc = abs(p - c);

		if (pa <= pb && pa <= pc)
			return a;
		else if (pb <= pc)
			return b;
		else
			return c;
	}

	void PNGPredictSub(uint8 *row, const uint8 *prevrow, int rowuint8s, int bpp) {
		for(int i=bpp; i<rowuint8s; ++i)
			row[i] += row[i-bpp];
	}

	void PNGPredictUp(uint8 *row, const uint8 *prevrow, int rowbytes, int bpp) {
		if (prevrow)
			for(int i=0; i<rowbytes; ++i)
				row[i] += prevrow[i];
	}

	void PNGPredictAverage(uint8 *row, const uint8 *prevrow, int rowbytes, int bpp) {
		if (prevrow) {
			for(int i=0; i<bpp; ++i)
				row[i] += prevrow[i]>>1;
			for(int j=bpp; j<rowbytes; ++j)
				row[j] += (prevrow[j] + row[j-bpp])>>1;
		} else {
			for(int j=bpp; j<rowbytes; ++j)
				row[j] += row[j-bpp]>>1;
		}
	}

	void PNGPredictPaeth(uint8 *row, const uint8 *prevrow, int rowbytes, int bpp) {
		if (prevrow) {
			for(int i=0; i<bpp; ++i)
				row[i] += PNGPaethPredictor(0, prevrow[i], 0);
			for(int j=bpp; j<rowbytes; ++j)
				row[j] += PNGPaethPredictor(row[j-bpp], prevrow[j], prevrow[j-bpp]);
		} else {
			for(int j=bpp; j<rowbytes; ++j)
				row[j] += PNGPaethPredictor(row[j-bpp], 0, 0);
		}
	}

	void PNGUnpackIndices1Bit(uint8 *dst, const uint8 *src, int w) {
		uint8 v;

		dst += (w-1) & ~7;
		src += (w+7)>>3;

		v = *--src;

		switch(w & 7) {
				while(w > 0) {
					v = *--src;
		case 0:		dst[7] = (v   )&1;
		case 7:		dst[6] = (v>>1)&1;
		case 6:		dst[5] = (v>>2)&1;
		case 5:		dst[4] = (v>>3)&1;
		case 4:		dst[3] = (v>>4)&1;
		case 3:		dst[2] = (v>>5)&1;
		case 2:		dst[1] = (v>>6)&1;
		case 1:		dst[0] = (v>>7)&1;
					dst -= 8;
					w -= 8;
				}
		}
	}

	void PNGUnpackIndices2Bit(uint8 *dst, const uint8 *src, int w) {
		uint8 v;

		dst += (w-1) & ~3;
		src += (w+3)>>2;

		v = *--src;

		switch(w & 3) {
				while(w > 0) {
					v = *--src;
		case 0:		dst[3] = (v   )&3;
		case 3:		dst[2] = (v>>2)&3;
		case 2:		dst[1] = (v>>4)&3;
		case 1:		dst[0] = (v>>6)&3;
					dst -= 4;
					w -= 4;
				}
		}
	}

	void PNGUnpackIndices4Bit(uint8 *dst, const uint8 *src, int w) {
		int x = w>>1;

		if (x)
			do {
				dst[0] = (src[0]>>4)&15;
				dst[1] = (src[0]   )&15;
				dst += 2;
				++src;
			} while(--x);

		if (w & 1)
			*dst = (*src >> 4) & 15;
	}

	void PNGUnpackIndices16Bit(uint8 *dst, const uint8 *src, int w) {
		while(w--) {
			*dst++ = *src;
			src += 2;
		}
	}
}

class VDImageDecoderPNG : public IVDImageDecoderPNG {
public:
	PNGDecodeError Decode(const void *src0, uint32 size);
	const VDPixmap& GetFrameBuffer() { return mFrameBuffer; }
	bool IsAlphaPresent() const { return mbAlphaPresent; }

protected:
	VDPixmapBuffer	mFrameBuffer;
	bool mbAlphaPresent;
};

IVDImageDecoderPNG *VDCreateImageDecoderPNG() {
	return new VDImageDecoderPNG;
}

PNGDecodeError VDImageDecoderPNG::Decode(const void *src0, uint32 size) {
	const uint8 *src = (const uint8 *)src0;
	const uint8 *const src_end = src+size;

	if (size < 8)
		return kPNGDecodeNotPNG;

	if (memcmp(src, kPNGSignature, 8))
		return kPNGDecodeNotPNG;

	src += 8;

	PNGHeader hdr;

	bool header_found = false;

	vdfastvector<uint8> packeddata;
	unsigned char pal[768];

	// decode chunks
	VDCRCTable table(VDCRCTable::kCRC32);
	VDCRCChecker checker(table);

	while(src < src_end) {
		if (src_end-src < 12)
			break;

		uint32 length = PNGDecodeNetwork32(src);

		if ((uint32)(src_end-src) < length+12)
			return kPNGDecodeTruncatedChunk;

		uint32 crc = PNGDecodeNetwork32(src + length + 8);

		// verify the crc
		checker.Init();
		checker.Process(src + 4, length + 4);
		if (checker.CRC() != crc)
			return kPNGDecodeChecksumFailed;

		uint32 type = PNGDecodeNetwork32(src + 4);

		if (type == 'IHDR') {
			if (length < 13)
				return kPNGDecodeBadHeader;

			hdr.width		= PNGDecodeNetwork32(src+8);
			hdr.height		= PNGDecodeNetwork32(src+12);
			hdr.depth		= src[16];
			hdr.colortype	= src[17];
			hdr.compression	= src[18];
			hdr.filter		= src[19];
			hdr.interlacing	= src[20];

			if (hdr.compression != 0)
				return kPNGDecodeUnsupportedCompressionAlgorithm;
			if (hdr.filter != 0)
				return kPNGDecodeUnsupportedFilterAlgorithm;
			if (hdr.interlacing > 1)
				return kPNGDecodeUnsupportedInterlacingAlgorithm;

			header_found = true;
		} else if (type == 'IDAT') {
			packeddata.resize(packeddata.size()+length);
			memcpy(&packeddata[packeddata.size()-length], src+8, length);
		} else if (type == 'PLTE') {
			if (length%3)
				return kPNGDecodeBadPalette;

			memcpy(pal, src+8, length<768?length:768);
		} else if (type == 'IEND') {
			break;
		} else if (src[0] & 0x20) {
			return kPNGDecodeUnknownRequiredChunk;
		}

		src += length+12;
	}

	if (!header_found)
		return kPNGDecodeBadHeader;

	if (packeddata.size() < 6)
		return kPNGDecodeDecompressionFailed;

	// if grayscale, initialize palette and make it paletted
	if (hdr.colortype == 0) {
		for(int i=0; i<256; ++i) {
			pal[i*3+0] = pal[i*3+1] = pal[i*3+2] = i;
		}

		hdr.colortype = 3;
	}

	unsigned bitsperpixel = hdr.depth;
	bool hasAlpha = false;

	switch(hdr.colortype) {
	case 2:		// RGB
		bitsperpixel *= 3;
		break;
	case 3:		// Paletted
		break;
	case 4:		// IA
		bitsperpixel *= 2;
		hasAlpha = true;
		break;
	case 6:		// RGBA
		bitsperpixel *= 4;
		hasAlpha = true;
		break;
	}

	mbAlphaPresent = hasAlpha;

	unsigned pitch = (hdr.width*3+3)&~3;
	unsigned pngrowbytes	= (hdr.width * bitsperpixel + 7) >> 3;
	unsigned pngbpp			= (bitsperpixel+7) >> 3;

	// decompress here
	vdblock<uint8> dstbuf((pngrowbytes + 1) * hdr.height);

	VDMemoryStream packedStream(&packeddata[2], packeddata.size() - 6);
	VDZipStream unpackedStream(&packedStream, packeddata.size() - 6, false);

	try {
		unpackedStream.Read(dstbuf.data(), dstbuf.size());
	} catch(const MyError&) {
		return kPNGDecodeDecompressionFailed;
	}

	// check image data
	uint32 adler32 = VDReadUnalignedBEU32(packeddata.data() + packeddata.size() - 4);
	if (adler32 != VDAdler32Checker::Adler32(dstbuf.data(), dstbuf.size()))
		return kPNGDecodeChecksumFailed;

	mFrameBuffer.init(hdr.width, hdr.height, hasAlpha ? nsVDPixmap::kPixFormat_XRGB8888 : nsVDPixmap::kPixFormat_RGB888);

	uint8 *srcp = &dstbuf[0];
	int x, y;

	vdblock<uint8> tempindices;

	if (hdr.colortype == 3 && hdr.depth != 8)	// If paletted and not 8bpp....
		tempindices.resize(hdr.width);

	const uint8 *srcprv = NULL;
	for(y=0; (uint32)y<hdr.height; ++y) {
		switch(*srcp++) {
		case 0:			break;
		case 1:			PNGPredictSub(srcp, srcprv, pngrowbytes, pngbpp);		break;
		case 2:			PNGPredictUp(srcp, srcprv, pngrowbytes, pngbpp);		break;
		case 3:			PNGPredictAverage(srcp, srcprv, pngrowbytes, pngbpp);	break;
		case 4:			PNGPredictPaeth(srcp, srcprv, pngrowbytes, pngbpp);		break;
		default:		return kPNGDecodeBadFilterMode;
		}

		const uint8 *rowsrc = srcp;
		      uint8 *rowdst = (uint8 *)vdptroffset(mFrameBuffer.data, mFrameBuffer.pitch * y);

		if (hdr.colortype == 2) {					// RGB: depths 8, 16
			if (hdr.depth == 8) {
				for(x=0; (uint32)x<hdr.width; ++x) {
					rowdst[x*3+0] = rowsrc[x*3+2];
					rowdst[x*3+1] = rowsrc[x*3+1];
					rowdst[x*3+2] = rowsrc[x*3+0];
				}
			} else if (hdr.depth == 16) {
				for(x=0; (uint32)x<hdr.width; ++x) {
					rowdst[x*3+0] = rowsrc[x*6+4];
					rowdst[x*3+1] = rowsrc[x*6+2];
					rowdst[x*3+2] = rowsrc[x*6+0];
				}
			} else {
				return kPNGDecodeUnsupported;
			}
		} else if (hdr.colortype == 6) {			// RGBA: depths 8, 16
			if (hdr.depth == 8) {
				for(x=0; (uint32)x<hdr.width; ++x) {
					rowdst[x*4+0] = rowsrc[x*4+2];
					rowdst[x*4+1] = rowsrc[x*4+1];
					rowdst[x*4+2] = rowsrc[x*4+0];
					rowdst[x*4+3] = rowsrc[x*4+3];
				}
			} else if (hdr.depth == 16) {
				for(x=0; (uint32)x<hdr.width; ++x) {
					rowdst[x*4+0] = rowsrc[x*8+4];
					rowdst[x*4+1] = rowsrc[x*8+2];
					rowdst[x*4+2] = rowsrc[x*8+0];
					rowdst[x*4+3] = rowsrc[x*8+6];
				}
			} else {
				return kPNGDecodeUnsupported;
			}
		} else if (hdr.colortype == 4) {			// grayscale with alpha: depths 8, 16
			if (hdr.depth == 8) {
				for(x=0; (uint32)x<hdr.width; ++x) {
					rowdst[x*4+0] = rowdst[x*4+1] = rowdst[x*4+2] = rowsrc[x*2];
					rowdst[x*4+3] = rowsrc[x*2+1];
				}
			} else if (hdr.depth == 16) {
				for(x=0; (uint32)x<hdr.width; ++x) {
					rowdst[x*4+0] =	rowdst[x*4+1] =	rowdst[x*4+2] = rowsrc[x*4];
					rowdst[x*4+3] = rowsrc[x*4+2];
				}
			} else {
				return kPNGDecodeUnsupported;
			}
		} else if (hdr.colortype == 3) {			// paletted: depths 1, 2, 4, 8
			if (hdr.depth != 8) {
				switch(hdr.depth) {
				case 1:		PNGUnpackIndices1Bit(tempindices.data(), rowsrc, hdr.width);	break;
				case 2:		PNGUnpackIndices2Bit(tempindices.data(), rowsrc, hdr.width);	break;
				case 4:		PNGUnpackIndices4Bit(tempindices.data(), rowsrc, hdr.width);	break;
				case 16:	PNGUnpackIndices16Bit(tempindices.data(), rowsrc, hdr.width);	break;
				default:
					return kPNGDecodeUnsupported;
				}

				rowsrc = tempindices.data();
			}

			for(x=0; (uint32)x<hdr.width; ++x) {
				unsigned idx = rowsrc[x];
				rowdst[x*3+0] = pal[idx*3+2];
				rowdst[x*3+1] = pal[idx*3+1];
				rowdst[x*3+2] = pal[idx*3+0];
			}
		}

		srcprv = srcp;
		srcp += pngrowbytes;
	}

	return kPNGDecodeOK;
}

const char *PNGGetErrorString(PNGDecodeError err) {
	switch(err) {
	case kPNGDecodeOK:									return "No error.";
	case kPNGDecodeNotPNG:								return "Not a PNG file.";
	case kPNGDecodeTruncatedChunk:						return "A chunk in the PNG file is damaged.";
	case kPNGDecodeBadHeader:							return "The PNG header is invalid.";
	case kPNGDecodeUnsupportedCompressionAlgorithm:		return "The compression algorithm used by the PNG file is not supported.";
	case kPNGDecodeUnsupportedInterlacingAlgorithm:		return "The interlacing algorithm used by the PNG file is not supported.";
	case kPNGDecodeUnsupportedFilterAlgorithm:			return "The filtering algorithm used by the PNG file is not supported.";
	case kPNGDecodeBadPalette:							return "The PNG palette structure is bad.";
	case kPNGDecodeDecompressionFailed:					return "A decompression error occurred while unpacking the raw PNG image data.";
	case kPNGDecodeBadFilterMode:						return "The image data specifies an unknown PNG filtering mode.";
	case kPNGDecodeUnknownRequiredChunk:				return "The PNG file contains data that is required to decode the image but which this decoder does not support.";
	case kPNGDecodeChecksumFailed:						return "A chunk in the PNG file is corrupted (bad checksum).";
	case kPNGDecodeUnsupported:							return "The PNG file appears to be valid, but uses an encoding mode that is not supported.";
	default:											return "?";
	}
}

bool VDDecodePNGHeader(const void *src0, uint32 len, int& w, int& h, bool& hasalpha) {
	const uint8 *src = (const uint8 *)src0;

	if (len < 29)
		return false;

	if (memcmp(src, kPNGSignature, 8))
		return false;

	// Next four bytes must be IHDR and length >= 13
	const uint32 hlen = PNGDecodeNetwork32(src + 8);
	const uint32 ckid = PNGDecodeNetwork32(src + 12);

	if (ckid != 'IHDR')
		return false;

	if (hlen < 13)
		return false;

	PNGHeader hdr;

	hdr.width		= PNGDecodeNetwork32(src+16);
	hdr.height		= PNGDecodeNetwork32(src+20);
	hdr.depth		= src[24];
	hdr.colortype	= src[25];
	hdr.compression	= src[26];
	hdr.filter		= src[27];
	hdr.interlacing	= src[28];

	switch(hdr.colortype) {
		case 2:		// RGB
		case 3:		// Paletted
		default:
			hasalpha = false;
			break;

		case 4:		// IA
		case 6:		// RGBA
			hasalpha = true;
			break;
	}

	w = hdr.width;
	h = hdr.height;

	return true;
}
