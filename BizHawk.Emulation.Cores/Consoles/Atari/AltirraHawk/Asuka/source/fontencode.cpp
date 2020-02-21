//	Asuka - VirtualDub Build/Post-Mortem Utility
//	Copyright (C) 2005-2007 Avery Lee
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

#include <stdafx.h>

#define WINVER 0x0500
#define _WIN32_WINNT 0x0500
#include <vd2/system/memory.h>
#include <vd2/system/vectors.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/filesys.h>
#include <vd2/system/math.h>
#include <vd2/system/strutil.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <vector>
#include <algorithm>
#include <windows.h>

void VDNORETURN help_fontencode() {
	printf("usage: fontencode <.bmp file> <cell width> <cell height> <row width> <row height> <ascent> <advance> <line gap> <output file> <symbol name>\n");
	exit(5);
}

void tool_fontencode(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches) {
	if (args.size() < 10)
		help_fontencode();

	bool rawmode = false;

	for(vdfastvector<const char *>::const_iterator it = switches.begin(), itEnd = switches.end();
		it != itEnd;
		++it)
	{
		const char *sw = *it;

		if (!vdstricmp(sw, "raw"))
			rawmode = true;
	}

	const int cellWidth = atoi(args[1]);
	const int cellHeight = atoi(args[2]);
	const int gridWidth = atoi(args[3]);
	const int gridHeight = atoi(args[4]);
	const int cellAscent = atoi(args[5]);
	const int cellAdvance = atoi(args[6]);
	const int lineGap = atoi(args[7]);

	if (cellWidth < 1 || cellHeight < 1 || cellWidth > 1000 || cellHeight > 1000) {
		printf("Asuka: Invalid cell size %dx%d.\n", cellWidth, cellHeight);
		exit(10);
	}

	printf("Asuka: Extracting %dx%d bitmap font: %s -> %s.\n", cellWidth, cellHeight, args[0], args[8]);

	HBITMAP hbm = (HBITMAP)LoadImageA(NULL, args[0], IMAGE_BITMAP, 0, 0, LR_CREATEDIBSECTION | LR_LOADFROMFILE);
	BITMAP bm = {0};

	if (!hbm || !GetObject(hbm, sizeof bm, &bm)) {
		printf("Asuka: Unable to load font bitmap %s: error code %d.\n", args[0], (int)GetLastError());
		exit(10);
	}

	const int bmWidth = bm.bmWidth;
	const int bmHeight = bm.bmHeight;

	if (bmWidth < gridWidth * (cellWidth + 1) - 1 || bmHeight < gridHeight * (cellHeight + 1) - 1) {
		printf("Asuka: Can't extract %dx%d cells in %dx%d grid from %dx%d bitmap.\n"
			, cellWidth
			, cellHeight
			, gridWidth
			, gridHeight
			, bmWidth
			, bmHeight);

		exit(10);
	}

	vdfastvector<uint32> pixels(bmWidth * bmHeight);

	BITMAPINFO bi = {};
	bi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	bi.bmiHeader.biWidth = bmWidth;
	bi.bmiHeader.biHeight = bmHeight;
	bi.bmiHeader.biPlanes = 1;
	bi.bmiHeader.biCompression = BI_RGB;
	bi.bmiHeader.biBitCount = 32;
	bi.bmiHeader.biSizeImage = bmWidth * bmHeight * 4;
	bi.bmiHeader.biXPelsPerMeter = 0;
	bi.bmiHeader.biYPelsPerMeter = 0;
	bi.bmiHeader.biClrUsed = 0;
	bi.bmiHeader.biClrImportant = 0;
	HDC hdc = GetDC(NULL);
	if (!hdc || !GetDIBits(hdc, hbm, 0, bmHeight, pixels.data(), &bi, DIB_RGB_COLORS)) {
		printf("Asuka: Can't extract bitmap bits.\n");
		exit(10);
	}
	ReleaseDC(NULL, hdc);
	
	vdfastvector<uint8> outheap;
	vdfastvector<uint8> chardata(cellWidth * cellHeight);

	const bool bigfont = (cellWidth | cellHeight) >= 16;
	int maxCells = std::min<int>(256, gridWidth * gridHeight);
	vdfastvector<uint32> posdata;

	int startChar = 255;
	int emptyCharRun = 0;
	int endChar = 0;
	for(int i=0; i<maxCells; ++i) {
		int cx = i % gridWidth;
		int cy = i / gridWidth;
		int x = (cellWidth + 1) * cx;
		int y = (cellHeight + 1) * cy;

		uint8 *dst = chardata.data();
		bool empty = !rawmode;
		int x1 = 0;
		int x2 = cellWidth;

		const uint8 *srct = (const uint8 *)&pixels[bmWidth * (bmHeight - 1 - y) + x];

		if (!rawmode) {
			while(x2 > x1 && srct[4 * (x2 - 1)] > 192 && srct[4 * (x2 - 1) + 2] < 192)
				--x2;

			while(x1 < x2 && srct[4 * x1] > 192 && srct[4 * x1 + 2] < 192)
				++x1;
		}

		int cw = x2 - x1;

		for(int y2=0; y2<cellHeight; ++y2) {
			const uint8 *src = (const uint8 *)&pixels[bmWidth * (bmHeight - 1 - (y+y2)) + x + x1] + 1;

			for(int x2=0; x2<cw; ++x2) {
				if (src[0] < 128)
					empty = false;

				dst[x2] = src[0] < 128 ? 255 : 0;
				src += 4;
			}

			dst += cw;
		}

		if (empty && cw == cellWidth) {
			++emptyCharRun;
		} else {
			if (outheap.empty())
				startChar = i;
			else if (emptyCharRun)
				posdata.resize(posdata.size() + emptyCharRun, cellWidth);

			emptyCharRun = 0;
			if (bigfont)
				posdata.push_back(((outheap.size() / cellHeight + 1) << 8) + cw);
			else
				posdata.push_back(((outheap.size() / cellHeight + 1) << 4) + cw);
			outheap.insert(outheap.end(), chardata.data(), chardata.data() + cw * cellHeight);
			endChar = i;
		}
	}

	outheap.resize(((outheap.size() + 7) & ~7), 0);

	FILE *f = fopen(args[8], "w");
	if (!f) {
		printf("Asuka: Unable to open output file: %s\n", args[5]);
		exit(10);
	}

	fprintf(f, "// Created by Asuka from %s.  DO NOT EDIT!\n\n", VDFileSplitPath(args[0]));

	if (rawmode) {
		fprintf(f, "const uint8 %s[]={\n", args[9]);

		int datalen = outheap.size();
		for(int i=0; i<datalen; i+=8) {
			uint8 byte	= (outheap[i+0] & 0x80)
						+ (outheap[i+1] & 0x40)
						+ (outheap[i+2] & 0x20)
						+ (outheap[i+3] & 0x10)
						+ (outheap[i+4] & 0x08)
						+ (outheap[i+5] & 0x04)
						+ (outheap[i+6] & 0x02)
						+ (outheap[i+7] & 0x01);

			if (!(i & 127))
				fputc('\t', f);

			fprintf(f, "0x%02x,", byte);

			if ((i & 127) == 120)
				fputc('\n', f);
		}

		if (datalen & 127)
			fputc('\n', f);

		fprintf(f, "};\n\n");

		printf("Asuka: %d bytes.\n", (int)(datalen >> 3));
	} else {
		fprintf(f, "const uint8 %s_FontData[]={\n", args[9]);

		int datalen = outheap.size();
		for(int i=0; i<datalen; i+=8) {
			uint8 byte	= (outheap[i+0] & 0x80)
						+ (outheap[i+1] & 0x40)
						+ (outheap[i+2] & 0x20)
						+ (outheap[i+3] & 0x10)
						+ (outheap[i+4] & 0x08)
						+ (outheap[i+5] & 0x04)
						+ (outheap[i+6] & 0x02)
						+ (outheap[i+7] & 0x01);

			if (!(i & 127))
				fputc('\t', f);

			fprintf(f, "0x%02x,", byte);

			if ((i & 127) == 120)
				fputc('\n', f);
		}

		if (datalen & 127)
			fputc('\n', f);

		fprintf(f, "};\n\n");

		if (bigfont)
			fprintf(f, "const uint32 %s_PosData[]={\n", args[9]);
		else
			fprintf(f, "const uint16 %s_PosData[]={\n", args[9]);

		int poscount = posdata.size();
		for(int i=0; i<poscount; ++i) {
			if (!(i & 15))
				fputc('\t', f);

			if (bigfont)
				fprintf(f, "0x%08x,", posdata[i]);
			else
				fprintf(f, "0x%04x,", posdata[i]);

			if ((i & 15) == 15)
				fputc('\n', f);
		}

		if (poscount & 15)
			fputc('\n', f);

		fprintf(f, "};\n\n");

		// top-level data structure
		fprintf(f, "const VDBitmapFontInfo %s_FontInfo={\n", args[9]);
		fprintf(f, "\t%s_FontData,\n", args[9]);
		if (bigfont) {
			fprintf(f, "\tNULL,\n");
			fprintf(f, "\t%s_PosData,\n", args[9]);
		} else {
			fprintf(f, "\t%s_PosData,\n", args[9]);
			fprintf(f, "\tNULL,\n");
		}
		fprintf(f, "\t%d, %d,\n", startChar, endChar);
		fprintf(f, "\t%d, %d,\n", cellWidth, cellHeight);
		fprintf(f, "\t%d, %d,\n", cellAscent, cellAdvance);
		fprintf(f, "\t%d,\n", lineGap);
		fprintf(f, "};\n");

		printf("Asuka: %d bytes.\n", (int)((datalen >> 3) + (bigfont ? posdata.size() << 2 : posdata.size() << 1)));
	}
}
