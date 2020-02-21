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

static void write_offset(FILE *fo, uint32 offset, const uint8 *extra) {
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

int cmd_makereloc2(int argc, const char *const *argv) {
	if (argc < 4)
		fail("makereloc2 requires input and 2x output filenames");

	typedef std::vector<uint32> Relocs;
	Relocs wordRelocs;
	Relocs lowRelocs;
	Relocs highRelocs;
	std::vector<uint8> highRelocOffsets;

	FILE *fi = fopen(argv[1], "rb");
	if (!fi)
		fail("cannot open input file: %s", argv[1]);

	std::vector<uint8> dataseg;

	for(;;) {
		uint16 startAddr;
		uint16 endAddr;

		if (1 != fread(&startAddr, 2, 1, fi)) {
			if (feof(fi))
				break;

			fail("failed reading start address: %s", argv[1]);
		}

		if (startAddr == 0xFFEF) {
			int segType = fgetc(fi);
			int numRelocs1 = fgetc(fi);
			int numRelocs2 = fgetc(fi);

			if (segType < 0 || numRelocs1 < 0 || numRelocs2 < 0)
				fail("failed reading reloc header: %s", argv[1]);

			int numRelocs = numRelocs1 + numRelocs2 * 256;

			if (segType == 'W') {
				while(numRelocs--) {
					int addrLo = fgetc(fi);
					int addrHi = fgetc(fi);

					if (addrLo < 0 || addrHi < 0)
						fail("failed reading reloc offset: %s", argv[1]);

					wordRelocs.push_back(addrLo + 256*addrHi);
				}
			} else if (segType == '<') {
				while(numRelocs--) {
					int addrLo = fgetc(fi);
					int addrHi = fgetc(fi);

					if (addrLo < 0 || addrHi < 0)
						fail("failed reading reloc offset: %s", argv[1]);

					lowRelocs.push_back(addrLo + 256*addrHi);
				}
			} else if (segType == '>') {
				while(numRelocs--) {
					int addrLo = fgetc(fi);
					int addrHi = fgetc(fi);
					int addrOffset = fgetc(fi);

					if (addrLo < 0 || addrHi < 0 || addrOffset < 0)
						fail("failed reading reloc offset: %s", argv[1]);

					highRelocs.push_back(addrLo + 256*addrHi);
					highRelocOffsets.push_back(addrOffset);
				}
			}
		} else if (startAddr == 0xFFFF) {
			struct MADSRelocHeader {
				uint16	mStartAddr;
				uint16	mEndAddr;
				uint16	mRelocHeader;
				uint8	mUnused;
				uint8	mConfig;
				uint16	mStackPointer;
				uint16	mStackAddress;
				uint16	mProcVarsAddr;
			} hdr;

			if (1 != fread(&hdr, sizeof hdr, 1, fi))
				fail("cannot read reloc header: %s", argv[1]);

			if (hdr.mStartAddr != 0 || hdr.mRelocHeader != 0x524D)
				fail("invalid MADS relocation header: %s", argv[1]);

			startAddr = hdr.mStartAddr;
			endAddr = hdr.mEndAddr;

			if (endAddr < startAddr)
				fail("invalid segment range: %04X-%04X", startAddr, endAddr);

			if (dataseg.size() <= endAddr)
				dataseg.resize(endAddr + 1, 0);

			if (1 != fread(&dataseg[startAddr], endAddr + 1 - startAddr, 1, fi))
				fail("cannot read segment data: %s", argv[1]);
		} else {
			fail("invalid segment in source file");
		}
	}

	// re-relocate from $0100 to $0000
#if 0
	for(Relocs::const_iterator it(wordRelocs.begin()), itEnd(wordRelocs.end()); it != itEnd; ++it) {
		if (*it + 1 >= dataseg.size())
			fail("invalid word relocation");
		
		--dataseg[*it + 1];
	}

	for(Relocs::const_iterator it(highRelocs.begin()), itEnd(highRelocs.end()); it != itEnd; ++it) {
		if (*it >= dataseg.size())
			fail("invalid word relocation");

		--dataseg[*it];
	}
#endif

	// open output files
	FILE *fobin = fopen(argv[2], "wb");
	if (!fobin)
		fail("cannot open output file: %s", argv[2]);

	fwrite(&dataseg[0], dataseg.size(), 1, fobin);
	fclose(fobin);

	FILE *forelocs = fopen(argv[3], "wb");
	if (!forelocs)
		fail("cannot open output file: %s", argv[3]);

	fprintf(forelocs, "relocdata_begin:\n");

	// emit low-byte relocations
	int lorelocs = 0;
	uint32 last = 0;
	fprintf(forelocs, "\n\t;low byte relocations\n");
	for(Relocs::const_iterator it(lowRelocs.begin()), itEnd(lowRelocs.end()); it != itEnd; ++it) {
		uint32 offset = *it-last;

		write_offset(forelocs, offset, NULL);

		last = *it;
		++lorelocs;
	}
	fprintf(forelocs, "\tdta\t$00\n");

	// emit word relocations
	int wordrelocs = 0;
	last = 0;
	fprintf(forelocs, "\n\t;word relocations\n");
	for(Relocs::const_iterator it(wordRelocs.begin()), itEnd(wordRelocs.end()); it != itEnd; ++it) {
		uint32 offset = *it-last;

		write_offset(forelocs, offset, NULL);

		last = *it;
		++wordrelocs;
	}
	fprintf(forelocs, "\tdta\t$00\n");

	// emit high-byte relocations
	int hirelocs = 0;
	last = 1;
	fprintf(forelocs, "\n\t;high byte relocations\n");
	std::vector<uint8>::const_iterator itExtra(highRelocOffsets.begin());
	for(Relocs::const_iterator it(highRelocs.begin()), itEnd(highRelocs.end()); it != itEnd; ++it) {
		uint32 offset = *it-last;

		write_offset(forelocs, offset, &*itExtra++);

		last = *it;
		++hirelocs;
	}
	fprintf(forelocs, "\tdta\t$00\n");

	fprintf(forelocs, "\nrelocdata_end:\n");

	// close output files
	fclose(forelocs);

	printf("%d bytes, %d lo relocs, %d word relocs, %d hi relocs\n", dataseg.size(), lorelocs, wordrelocs, hirelocs);

	return 0;
}
