//	Altirra - Atari 800/800XL/5200 emulator
//	Compiler - LZ decompression module
//	Copyright (C) 2009-2012 Avery Lee
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

int cmd_lzunpack(int argc, const char *const *argv) {
	if (argc < 2)
		fail("lzunpack requires input and output filenames");

	FILE *fi = fopen(argv[1], "rb");
	if (!fi)
		fail("cannot open input file: %s", argv[1]);

	FILE *fo = fopen(argv[2], "wb");
	if (!fo)
		fail("cannot open output file: %s", argv[2]);

	uint8 window[16384] = {0};

	// discard length
	getc(fi);
	getc(fi);
	getc(fi);
	getc(fi);

	unsigned pos = 0;
	for(;;) {
		int c = getc(fi);

		if (c <= 0)
			break;

		if (c & 1) {
			int distm1 = getc(fi);
			int len;

			if (c & 2) {
				distm1 += (c & 0xfc) << 6;
				len = getc(fi);
			} else {
				distm1 += ((c & 0x1c) << 6);
				len = c >> 5;
			}

			len += 3;

			unsigned srcpos = (pos - distm1 - 1);
			do {
				uint8 d = window[srcpos++ & 0x3fff];
				window[pos++ & 0x3fff] = d;
				putc(d, fo);
			} while(--len);
		} else {
			c >>= 1;

			do {
				int d = getc(fi);
				window[pos++ & 0x3fff] = d;
				putc(d, fo);
			} while(--c);
		}
	}

	int packlen = ftell(fi);
	int len = ftell(fo);

	fclose(fi);
	fclose(fo);

	printf("%s(%d) -> %s(%d)\n", argv[1], packlen, argv[2], len);

	return 0;
}
