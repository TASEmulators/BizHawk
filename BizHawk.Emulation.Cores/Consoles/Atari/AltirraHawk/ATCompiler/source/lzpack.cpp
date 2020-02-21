//	Altirra - Atari 800/800XL/5200 emulator
//	Compiler - LZ compression module
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

// The format we are compressing here:
//	00000000: end of stream
//	xxxxxxx0: x literal bytes
//	cccddd01 dddddddd: short LZ run <d+1, c+3>
//	dddddd11 dddddddd cccccccc: long LZ run <d+1, c+3>

int cmd_lzpack(int argc, const char *const *argv) {
	if (argc < 2)
		fail("lzpack requires input and output filenames");

	FILE *fi = fopen(argv[1], "rb");
	if (!fi)
		fail("cannot open input file: %s", argv[1]);

	FILE *fo = fopen(argv[2], "wb");
	if (!fo)
		fail("cannot open output file: %s", argv[2]);

	int htchain[16384];
	int ht[256];

	for(int i=0; i<256; ++i)
		ht[i] = -1;

	for(int i=0; i<16384; ++i)
		htchain[i] = -1;

	fseek(fi, 0, SEEK_END);
	size_t len = ftell(fi);
	fseek(fi, 0, SEEK_SET);

	uint8 *const mem = new uint8[len + 259];
	memset(mem, 0, len + 259);

	if (1 != fread(mem, len, 1, fi))
		fail("cannot read input file: %s", argv[1]);

	fclose(fi);

	uint32 pos = 0;
	uint32 lenm2 = len > 2 ? len - 2 : 0;
	uint8 hc = mem[0] + mem[1] + mem[2];

	uint8 dstbuf[129];
	int literals = 0;

	dstbuf[0] = (uint8)(len >> 0);
	dstbuf[1] = (uint8)(len >> 8);
	dstbuf[2] = (uint8)(len >> 16);
	dstbuf[3] = (uint8)(len >> 24);
	fwrite(dstbuf, 4, 1, fo);

	while(pos < lenm2) {
		// search hash chain
		int minpos = pos > 16384 ? (int)pos - 16384 : 0;
		const uint8 *curptr = mem + pos;
		int bestlen = 2;
		int bestdist = 0;
		int maxmatch = len - pos;

		int testpos = ht[hc];
		while(testpos >= minpos) {
			uint8 *testptr = mem + testpos;

			if (testptr[bestlen] == curptr[bestlen]) {
				int matchlen = 0;

				while(matchlen < 258 && testptr[matchlen] == curptr[matchlen])
					++matchlen;

				if (matchlen >= 3) {
					if (matchlen > maxmatch)
						matchlen = maxmatch;

					int dist = (pos - testpos) - 1;
					if (matchlen > bestlen && (matchlen > 3 || dist < 2048)) {
						bestlen = matchlen;
						bestdist = dist;

						if (matchlen == 258)
							break;
					}
				}
			}

			int nextpos = htchain[testpos & 0x3fff];
			if (nextpos >= testpos)
				break;

			testpos = nextpos;
		}

		if (bestlen >= 3) {
			if (literals) {
				dstbuf[0] = literals*2;
				fwrite(dstbuf, literals+1, 1, fo);
				literals = 0;
			}

			if (bestdist > 2047 || bestlen > 10) {
				putc(3 + ((bestdist & 0x3f00) >> 6), fo);
				putc(bestdist & 0xff, fo);
				putc(bestlen - 3, fo);
			} else {
				putc(1 + ((bestlen - 3) << 5) + ((bestdist & 0x700) >> 6), fo);
				putc(bestdist & 0xff, fo);
			}
		} else {
			if (literals >= 127) {
				dstbuf[0] = literals*2;
				fwrite(dstbuf, literals+1, 1, fo);
				literals = 0;
			}

			++literals;
			dstbuf[literals] = *curptr;
			bestlen = 1;
		}

		do {
			htchain[pos & 0x3fff] = ht[hc];
			ht[hc] = pos;

			hc -= mem[pos];
			hc += mem[pos + 3];
			++pos;
		} while(--bestlen);
	}

	while(pos < len) {
		if (literals >= 127) {
			dstbuf[0] = literals*2;
			fwrite(dstbuf, literals+1, 1, fo);
			literals = 0;
		}

		++literals;
		dstbuf[literals] = mem[pos++];
	}

	if (literals) {
		dstbuf[0] = literals*2;
		fwrite(dstbuf, literals+1, 1, fo);
		literals = 0;
	}

	putc(0, fo);

	printf("%s(%d) -> %s(%d)\n", argv[1], len, argv[2], (int)ftell(fo));

	fclose(fo);

	return 0;
}
