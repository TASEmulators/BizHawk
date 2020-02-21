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

void VDNORETURN help_fontrender() {
	printf("usage: fontrender <fontname> <width> <height> <aa> <weight> <output .bmp file>\n");
	exit(5);
}

void tool_fontrender(const vdfastvector<const char *>& args, const vdfastvector<const char *>& switches) {
	if (args.size() != 6)
		help_fontrender();

	const char *familyname = args[0];
	const int width = atoi(args[1]);
	const int height = atoi(args[2]);
	const int aa = atoi(args[3]);
	const int weight = atoi(args[4]);
	const char *outname = args[5];

	HDC hdc0 = CreateDCW(L"DISPLAY", NULL, NULL, NULL);
	HDC hdc = CreateCompatibleDC(hdc0);
	DeleteDC(hdc0);

	HFONT hfont = CreateFontA(
		-MulDiv(height, GetDeviceCaps(hdc, LOGPIXELSY), 72),
		MulDiv(width, GetDeviceCaps(hdc, LOGPIXELSX), 72),
		0,
		0,
		weight,
		FALSE,
		FALSE,
		FALSE,
		DEFAULT_CHARSET,
		OUT_DEFAULT_PRECIS,
		CLIP_DEFAULT_PRECIS,
		aa ? ANTIALIASED_QUALITY : NONANTIALIASED_QUALITY,
		DEFAULT_PITCH | FF_DONTCARE,
		familyname);

	if (!hfont) {
		printf("Unable to create font: %s %d", args[0], height);
		exit(10);
	}

	SelectObject(hdc, hfont);

	TEXTMETRICW tm;
	GetTextMetricsW(hdc, &tm);

	printf("Font height:   %d\n", tm.tmHeight);
	printf("Font ascent:   %d\n", tm.tmAscent);
	printf("Font descent:  %d\n", tm.tmDescent);
	printf("Average width: %d\n", tm.tmAveCharWidth);
	printf("Max width:     %d\n", tm.tmMaxCharWidth);

	int maxWidth = 0;
	int maxHeight = tm.tmAscent + tm.tmDescent;

	for(int i=0; i<256; ++i) {
		WCHAR c = i;
		SIZE sz = {};

		GetTextExtentPoint32W(hdc, &c, 1, &sz);

		if (maxWidth < sz.cx)
			maxWidth = sz.cx;
	}

	printf("Max width for 8-bit: %d\n", maxWidth);

	BITMAPINFO bi = {};
	bi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
	bi.bmiHeader.biWidth = (maxWidth + 1) * 16;
	bi.bmiHeader.biHeight = (maxHeight + 1) * 16;
	bi.bmiHeader.biPlanes = 1;
	bi.bmiHeader.biCompression = BI_RGB;
	bi.bmiHeader.biBitCount = 32;
	bi.bmiHeader.biSizeImage = bi.bmiHeader.biWidth * bi.bmiHeader.biHeight * 4;

	void *dibbits;
	HBITMAP hbm = CreateDIBSection(hdc, &bi, DIB_RGB_COLORS, &dibbits, NULL, 0);
	if (!hbm) {
		printf("Unable to create DIB section.\n");
		exit(20);
	}

	DeleteObject(SelectObject(hdc, hbm));

	SetTextAlign(hdc, TA_TOP | TA_LEFT);
	SetBkMode(hdc, OPAQUE);

	SetBkColor(hdc, RGB(0, 0, 255));

	RECT r = {0, 0, bi.bmiHeader.biWidth, bi.bmiHeader.biHeight };
	ExtTextOutW(hdc, 0, 0, ETO_OPAQUE | ETO_IGNORELANGUAGE, &r, L"", 0, NULL);

	SetBkColor(hdc, RGB(255, 255, 255));
	SetTextColor(hdc, RGB(0, 0, 0));

	for(int i=0; i<256; ++i) {
		WCHAR c = i;

		ExtTextOutW(hdc, (maxWidth + 1) * (i & 15), (maxHeight + 1) * (i >> 4), ETO_OPAQUE | ETO_IGNORELANGUAGE, NULL, &c, 1, NULL);
	}

	FILE *fo = fopen(outname, "wb");
	if (!fo) {
		printf("Unable to open output file.\n");
		exit(20);
	}

	BITMAPFILEHEADER fh = { 0x4D42, sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) + bi.bmiHeader.biSizeImage, 0, 0, sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER) };
	fwrite(&fh, sizeof fh, 1, fo);
	fwrite(&bi.bmiHeader, sizeof bi.bmiHeader, 1, fo);
	fwrite(dibbits, bi.bmiHeader.biSizeImage, 1, fo);
	fclose(fo);
}
