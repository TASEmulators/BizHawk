//	Altirra - Atari 800/800XL/5200 emulator
//	Compiler - relocatable module creator
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

void write_offset(FILE *fo, uint32 offset, const uint8 *extra) {
	fprintf(fo, "\tdta\t");

	while(offset >= 255) {
		offset -= 255;
		fprintf(fo, "$ff,");
	}

	if (extra)
		fprintf(fo, "$%02x,$%02x\n", offset, *extra);
	else
		fprintf(fo, "$%02x\n", offset);
}

int cmd_makereloc(int argc, const char *const *argv) {
	if (argc < 7)
		fail("makereloc requires 4x input and 2x output filenames");

	FILE *fi[4]={0};
	uint8 *objs[4]={0};

	static const uint32 kLoadAddrs[4]={
		0x2800,
		0x2801,
		0xa800,
		0x2800
	};

	uint32 initad = 0;
	uint32 seglen = 0;
	long srclen;

	for(int i=0; i<4; ++i) {
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
		}

		objs[i] = (uint8 *)malloc(len);

		if (1 != fread(objs[i], len, 1, fi[i]))
			fail("cannot read source file: %s", argv[i+1]);

		// validate object file
		const uint8 *obj = objs[i];
		bool valid = true;

		if (len < 12 || obj[0] != 0xFF || obj[1] != 0xFF)
			valid = false;

		if (valid) {
			uint32 loadaddr = obj[2] + 256*obj[3];

			if (loadaddr != kLoadAddrs[i])
				fail("incorrect load address for source #%d (%s): $%04X != $%04X", i+1, argv[i+1], loadaddr, kLoadAddrs[i]);

			uint32 endaddr = obj[4] + 256*obj[5];
			uint32 range = endaddr + 1 - loadaddr;
			if (endaddr < loadaddr || range + 12 > (uint32)len)
				valid = false;

			// check segment length
			if (valid) {
				if (i) {
					if (seglen != range)
						fail("segment lengths don't match between sources");
				} else
					seglen = range;
			}

			// check run address
			if (valid) {
				const uint8 *initseg = obj + range + 6;

				if (initseg[0] != 0xE2 || initseg[1] != 0x02
					|| initseg[2] != 0xE3 || initseg[3] != 0x02)
					valid = false;

				if (!i)
					initad = initseg[4] + 256*initseg[5];
			}
		}
	}

	uint8 *segbin = (uint8 *)malloc(seglen);

	// open output files
	FILE *fo = fopen(argv[6], "w");
	if (!fo)
		fail("cannot open output file: %s", argv[6]);

	// close source files
	for(int i=0; i<4; ++i) {
		fclose(fi[i]);
		fi[i] = NULL;
	}

	fprintf(fo, "relocdata_begin:\n");

	const uint8 *srcbase = objs[0] + 6;
	const uint8 *srclow = objs[1] + 6;
	const uint8 *srchigh = objs[2] + 6;
	const uint8 *srchighlow = objs[3] + 6;

	// compute binary
	FILE *fo2 = fopen(argv[5], "wb");
	if (!fo2)
		fail("cannot open output file: %s", argv[5]);

	memcpy(segbin, srcbase, seglen);

	for(uint32 i=0; i<seglen; ++i) {
		if ((srcbase[i] ^ srchigh[i]) & 0x80)
			segbin[i] -= 0x28;
	}

	fwrite(segbin, seglen, 1, fo2);
	fclose(fo2);

	// emit low-byte relocations
	int lorelocs = 0;
	fprintf(fo, "\n\t;low byte relocations\n");
	for(uint32 i=0, last=0; i<seglen; ++i) {
		if (srcbase[i] != srclow[i] && !((srcbase[i]^srchigh[i])&0x80) && !((srcbase[i+1]^srchigh[i+1])&0x80)) {
			uint32 offset = i-last;

			write_offset(fo, offset, NULL);

			last = i;
			++lorelocs;
		}
	}
	fprintf(fo, "\tdta\t$00\n");

	// emit word relocations
	int wordrelocs = 0;
	fprintf(fo, "\n\t;word relocations\n");
	for(uint32 i=0, last=0; i<seglen; ++i) {
		if (srcbase[i] != srclow[i] && ((srcbase[i+1]^srchigh[i+1])&0x80)) {
			uint32 offset = i-last;

			write_offset(fo, offset, NULL);

			last = i;
			++wordrelocs;
		}
	}
	fprintf(fo, "\tdta\t$00\n");

	// emit high-byte relocations
	int hirelocs = 0;
	fprintf(fo, "\n\t;high byte relocations\n");
	for(uint32 i=0, last=0; i<seglen; ++i) {
		if (srcbase[i] == srclow[i] && ((srcbase[i+1]^srchigh[i+1])&0x80)) {
			uint32 offset = i-last;

			write_offset(fo, offset, &srchighlow[i+1]);

			last = i;
			++hirelocs;
		}
	}
	fprintf(fo, "\tdta\t$00\n");

	fprintf(fo, "\nrelocdata_end:\n");

	fprintf(fo, "\nrelocbin_len = $%04x\n", seglen);

	// close output files
	fclose(fo);

	printf("%d bytes, %d lo relocs, %d word relocs, %d hi relocs\n", seglen, lorelocs, wordrelocs, hirelocs);

	return 0;
}
