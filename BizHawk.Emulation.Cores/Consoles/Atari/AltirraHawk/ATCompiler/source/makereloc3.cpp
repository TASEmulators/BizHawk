//	Altirra - Atari 800/800XL/5200 emulator
//	Compiler - relocatable module creator
//	Copyright (C) 2009-2014 Avery Lee
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

void write_offset(FILE *fo, uint32 offset) {
	fprintf(fo, "\tdta\t");

	while(offset >= 255) {
		offset -= 255;
		fprintf(fo, "$ff,");
	}

	fprintf(fo, "$%02x\n", offset);
}

int cmd_makereloc3(int argc, const char *const *argv) {
	if (argc < 6)
		fail("makereloc3 requires 2x input and 2x output filenames, and a base page");

	FILE *fi[2]={0};
	uint8 *objs[2]={0};

	uint32 seglen = 0;
	long srclen;

	for(int i=0; i<2; ++i) {
		fi[i] = fopen(argv[i+1], "rb");
		if (!fi[i])
			fail("cannot open input file: %s", argv[i+1]);

		fseek(fi[i], 0, SEEK_END);
		long len = ftell(fi[i]);
		fseek(fi[i], 0, SEEK_SET);

		if (len < 0)
			fail("cannot get length of: %s", argv[i+1]);

		if (i) {
			if (srclen != len)
				fail("source objects don't match in length: %d != %d", srclen != len);
		} else {
			srclen = len;
			seglen = (uint32)srclen;
		}

		objs[i] = (uint8 *)malloc(len);

		if (1 != fread(objs[i], len, 1, fi[i]))
			fail("cannot read source file: %s", argv[i+1]);
	}

	uint8 *segbin = (uint8 *)malloc(seglen);

	// open output files
	FILE *fo = fopen(argv[3], "w");
	if (!fo)
		fail("cannot open output file: %s", argv[3]);

	// close source files
	for(int i=0; i<2; ++i) {
		fclose(fi[i]);
		fi[i] = NULL;
	}

	fprintf(fo, "relocdata_begin:\n");

	const uint8 *srcbase = objs[0];
	const uint8 *srchigh = objs[1];

	// compute binary
	FILE *fo2 = fopen(argv[4], "wb");
	if (!fo2)
		fail("cannot open output file: %s", argv[4]);

	memcpy(segbin, srcbase, seglen);

	const uint8 basepage = (uint8)strtol(argv[5], NULL, 0);

	for(uint32 i=0; i<seglen; ++i) {
		if (srcbase[i] != srchigh[i])
			segbin[i] -= basepage;
	}

	fwrite(segbin, seglen, 1, fo2);
	fclose(fo2);

	// emit high-byte relocations
	int hirelocs = 0;
	fprintf(fo, "\n\t;high byte relocations\n");
	for(uint32 i=0, last=0; i<seglen; ++i) {
		if (srcbase[i] != srchigh[i]) {
			uint32 offset = i-last;

			write_offset(fo, offset);

			last = i;
			++hirelocs;
		}
	}
	fprintf(fo, "\tdta\t$00\n");

	fprintf(fo, "\nrelocdata_end:\n");

	fprintf(fo, "\nrelocbin_len = $%04x\n", seglen);

	// close output files
	fclose(fo);

	printf("%d bytes, %d hi relocs\n", seglen, hirelocs);

	return 0;
}
