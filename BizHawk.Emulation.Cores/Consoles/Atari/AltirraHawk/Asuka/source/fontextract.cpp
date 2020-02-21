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
#include <vd2/Kasumi/pixmaputils.h>
#include <vector>
#include <algorithm>
#include <windows.h>

void VDNORETURN help_fontextract() {
	printf("usage: fontextract <.ttf file> <font name> <start char> <end char> <output file> <symbol prefix>\n");
	exit(5);
}

namespace {
	struct GlyphInfo {
		ABCFLOAT mWidths;
		int mPointStart;
		int mCommandStart;

		GlyphInfo()
			: mPointStart(0)
			, mCommandStart(0)
		{
			mWidths.abcfA = 0;
			mWidths.abcfB = 0;
			mWidths.abcfC = 0;
		}
	};
}

void tool_fontextract(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches) {
	if (args.size() < 6)
		help_fontextract();

	printf("Asuka: Extracting font: %s -> %s.\n", args[0], args[4]);
	
	int startChar;
	int endChar;

	if (1 != sscanf(args[2], "%d", &startChar) || 1 != sscanf(args[3], "%d", &endChar))
		help_fontextract();

	if (startChar < 0 || endChar > 0xFFFF || endChar <= startChar) {
		printf("Asuka: Invalid character range %d-%d\n", startChar, endChar);
		exit(10);
	}

	int fontsAdded = AddFontResourceExA(args[0], FR_NOT_ENUM | FR_PRIVATE, 0);

	if (!fontsAdded) {
		printf("Asuka: Unable to load font file: %s\n", args[0]);
		exit(10);
	}

	HFONT hfont = CreateFontA(10, 0, 0, 0, 0, FALSE, FALSE, FALSE, ANSI_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE, args[1]);
	if (!hfont) {
		printf("Asuka: Unable to instantiate font: %s\n", args[1]);
		exit(10);
	}

	HDC hdc = CreateDCW(L"DISPLAY", 0, 0, 0);
	HGDIOBJ hfontOld = SelectObject(hdc, hfont);
	MAT2 m = { {0,1},{0,0},{0,0},{0,1} };

	union {
		OUTLINETEXTMETRICW m;
		char buf[2048];
	} metrics;

	if (!GetOutlineTextMetricsW(hdc, sizeof metrics, &metrics.m)) {
		printf("Asuka: Unable to retrieve outline text metrics.\n");
		exit(10);
	}

	SelectObject(hdc, hfontOld);
	DeleteObject(hfont);
	
	hfont = CreateFontA(metrics.m.otmEMSquare, 0, 0, 0, 0, FALSE, FALSE, FALSE, ANSI_CHARSET, OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, DEFAULT_QUALITY, DEFAULT_PITCH | FF_DONTCARE, args[1]);
	if (!hfont) {
		printf("Asuka: Unable to instantiate font: %s\n", args[1]);
		exit(10);
	}

	hfontOld = SelectObject(hdc, hfont);

	if (!GetOutlineTextMetricsW(hdc, sizeof metrics, &metrics.m)) {
		printf("Asuka: Unable to retrieve outline text metrics.\n");
		exit(10);
	}

	FILE *f = fopen(args[4], "w");
	if (!f) {
		printf("Asuka: Unable to open output file: %s\n", args[4]);
		exit(10);
	}

	std::vector<GlyphInfo> glyphs(endChar - startChar + 1);

	sint32 minX = 0x7FFFFFFF;
	sint32 minY = 0x7FFFFFFF;
	sint32 maxX = -0x7FFFFFFF - 1;
	sint32 maxY = -0x7FFFFFFF - 1;

	vdfastvector<sint32> points;
	vdfastvector<uint8> commands;

	for(int c=startChar; c<endChar; ++c) {
		GlyphInfo& info = glyphs[c - startChar];
		info.mPointStart = points.size() >> 1;
		info.mCommandStart = commands.size();

		GetCharABCWidthsFloatW(hdc, c, c, &info.mWidths);

		// encode glyph outline
		GLYPHMETRICS gm;

		if (GDI_ERROR == GetGlyphOutlineW(hdc, c, GGO_METRICS, &gm, 0, NULL, &m)) {
			printf("Asuka: Unable to obtain glyph metrics for char %d.\n", c);
			exit(20);
		}

		DWORD dwBytes = GetGlyphOutlineW(hdc, c, GGO_NATIVE | GGO_UNHINTED, &gm, 0, NULL, &m);
		if (dwBytes == 0xFFFFFFFF) {
			printf("Asuka: Unable to obtain glyph outline for char %d.\n", c);
			exit(20);
		}

		// GetGlyphOutline() doesn't like providing results for spaces.
		if (gm.gmBlackBoxX <= 0 || gm.gmBlackBoxY <= 0)
			continue;

		void *buf = malloc(dwBytes);
		GetGlyphOutlineW(hdc, c, GGO_NATIVE | GGO_UNHINTED, &gm, dwBytes, buf, &m);

		void *limit = (char *)buf + dwBytes;
		TTPOLYGONHEADER *pHdr = (TTPOLYGONHEADER *)buf;

		sint32 lastCode = 0;

		while(pHdr != limit) {
			VDASSERT(pHdr->dwType == TT_POLYGON_TYPE);

			sint32 x = *(const sint32 *)&pHdr->pfxStart.x;
			sint32 y = *(const sint32 *)&pHdr->pfxStart.y;

			if (minX > x)
				minX = x;
			if (minY > y)
				minY = y;
			if (maxX < x)
				maxX = x;
			if (maxY < y)
				maxY = y;

			points.push_back(x);
			points.push_back(y);

			TTPOLYCURVE *pCurve = (TTPOLYCURVE *)(pHdr + 1);
			TTPOLYCURVE *pCurveEnd = (TTPOLYCURVE *)((char *)pHdr + pHdr->cb);

			while(pCurve != pCurveEnd) {
				VDASSERT(pCurve->wType == TT_PRIM_QSPLINE || pCurve->wType == TT_PRIM_LINE);

				POINTFX *ppfx = pCurve->apfx;

				for(int i=0; i<pCurve->cpfx; ++i) {
					sint32 x = *(const sint32 *)&ppfx->x;
					sint32 y = *(const sint32 *)&ppfx->y;

					if (minX > x)
						minX = x;
					if (minY > y)
						minY = y;
					if (maxX < x)
						maxX = x;
					if (maxY < y)
						maxY = y;

					points.push_back(x);
					points.push_back(y);
					++ppfx;
				}

				if (pCurve->wType == TT_PRIM_LINE) {
					if ((lastCode & 3) != 2) {
						if (lastCode)
							commands.push_back(lastCode);

						lastCode = 2 - 4;
					}

					for(int i=0; i<pCurve->cpfx; ++i) {
						if (lastCode >= 0x7C) {
							commands.push_back(lastCode);
							lastCode = 2 - 4;
						}

						lastCode += 4;
					}
				} else {
					VDASSERT(pCurve->wType == TT_PRIM_QSPLINE);
					VDASSERT(!(pCurve->cpfx % 2));

					if ((lastCode & 3) != 3) {
						if (lastCode)
							commands.push_back(lastCode);

						lastCode = 3 - 4;
					}

					for(int i=0; i<pCurve->cpfx; i+=2) {
						if (lastCode >= 0x7C) {
							commands.push_back(lastCode);
							lastCode = 3 - 4;
						}

						lastCode += 4;
					}
				}

				pCurve = (TTPOLYCURVE *)ppfx;
			}

			if (lastCode) {
				commands.push_back(lastCode | 0x80);
				lastCode = 0;
			}

			vdptrstep(pHdr, pHdr->cb);
		}

		free(buf);
	}

	GlyphInfo& lastInfo = glyphs.back();

	lastInfo.mPointStart = points.size() >> 1;
	lastInfo.mCommandStart = commands.size();

	// write points
	fprintf(f, "// Created by Asuka from %s.  DO NOT EDIT!\n\n", VDFileSplitPath(args[0]));
	fprintf(f, "const uint16 %s_FontPointArray[]={\n", args[5]);

	float scaleX = (maxX > minX) ? 1.0f / (maxX - minX) : 0.0f;
	float scaleY = (maxY > minY) ? 1.0f / (maxY - minY) : 0.0f;

	int pointElementCount = points.size();

	for(int i=0; i<pointElementCount; i+=2) {
		uint8 x = VDClampedRoundFixedToUint8Fast((float)(points[i+0] - minX) * scaleX);
		uint8 y = VDClampedRoundFixedToUint8Fast((float)(points[i+1] - minY) * scaleY);
		uint16 pt = ((uint16)y << 8) + x;

		fprintf(f, "0x%04x,", pt);

		if ((i & 30) == 30)
			putc('\n', f);
	}

	if (pointElementCount & 30)
		putc('\n', f);

	fprintf(f, "};\n\n");

	// write commands
	fprintf(f, "const uint8 %s_FontCommandArray[]={\n", args[5]);

	int commandElementCount = commands.size();

	for(int i=0; i<commandElementCount; ++i) {
		fprintf(f, "0x%02x,", commands[i]);

		if ((i & 15) == 15)
			putc('\n', f);
	}

	if (commandElementCount & 15)
		putc('\n', f);

	fprintf(f, "};\n\n");

	// glyph data structures
	fprintf(f, "const VDOutlineFontGlyphInfo %s_FontGlyphArray[]={\n", args[5]);
	for(int i=startChar; i<=endChar; ++i) {
		const GlyphInfo& info = glyphs[i - startChar];
		fprintf(f, "{ %d, %d, %d, %d, %d },\n", info.mPointStart, info.mCommandStart, VDRoundToInt(info.mWidths.abcfA), VDRoundToInt(info.mWidths.abcfB), VDRoundToInt(info.mWidths.abcfC));
	}
	fprintf(f, "};\n\n");

	// top-level data structure
	fprintf(f, "const VDOutlineFontInfo %s_FontInfo={\n", args[5]);
	fprintf(f, "\t%s_FontPointArray,\n", args[5]);
	fprintf(f, "\t%s_FontCommandArray,\n", args[5]);
	fprintf(f, "\t%s_FontGlyphArray,\n", args[5]);
	fprintf(f, "\t%d, %d,\n", startChar, endChar);
	fprintf(f, "\t%d, %d, %d, %d,\t// bounds (16:16)\n", minX, minY, maxX, maxY);
	fprintf(f, "\t%d,\t// em square\n", metrics.m.otmEMSquare);
	fprintf(f, "\t%d,\t// ascent\n", metrics.m.otmAscent);
	fprintf(f, "\t%d,\t// descent\n", metrics.m.otmDescent);
	fprintf(f, "\t%d,\t// line gap\n", metrics.m.otmLineGap);
	fprintf(f, "};\n");

	printf("Asuka: %d point bytes, %d command bytes, %d glyph info bytes.\n", (int)points.size(), (int)commands.size(), (endChar - startChar + 1) * 10);

	SelectObject(hdc, hfontOld);
	DeleteDC(hdc);

	DeleteObject(hfont);
}
