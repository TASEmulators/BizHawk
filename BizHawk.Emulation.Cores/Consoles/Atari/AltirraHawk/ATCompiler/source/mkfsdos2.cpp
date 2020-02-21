// Altirra Acid800 test suite
// Build utility
// Copyright (C) 2010-2013 Avery Lee, All Rights Reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE. 

#include <stdafx.h>
#include <ctype.h>

int cmd_mkfsdos2(int argc, const char *const *argv) {
	if (argc < 3)
		fail("mkfsdos2 requires input, base, and output filenames");

	FILE *fi = fopen(argv[1], "r");
	if (!fi)
		fail("cannot open input file: %s", argv[1]);

	char *disk = (char *)malloc(720 * 128);
	memset(disk, 0, 720*128);

	char fnbuf[512];
	char fnbuf2[512];
	char secbuf[125];
	bool boot = false;

	// initialize VTOC
	char *vtoc = disk + (360 - 1) * 128;
	vtoc[0] = 0x02;
	vtoc[1] = (char)0xC3;		// total count
	vtoc[2] = 0x02;

	// free all sectors (720 bits)
	memset(vtoc + 10, 0xFF, 90);

	// allocate sectors 0-3
	vtoc[10] = 0x0F;

	// allocate sectors 360-368
	vtoc[55] = 0;
	vtoc[56] = 0x7F;

	int freesecs = 707;
	int sec = 4;
	int fileid = 0;
	while(fgets(fnbuf, 511, fi)) {
		fnbuf[511] = 0;
		char *s = fnbuf;
		bool text = false;
		if (*s == '>') {
			text = true;
			++s;
		}

		char *t = fnbuf + strlen(s);

		while(*s && isspace((unsigned char)*s))
			++s;

		while(s != t && isspace((unsigned char)t[-1]))
			--t;
		*t = 0;

		char *altname = s;
		char *split = strchr(s, ' ');
		if (split) {
			*split++ = 0;

			while(*split == ' ')
				++split;

			if (split[0] == '-' && split[1] == '>') {
				split += 2;

				while(*split == ' ')
					++split;

				if (*split)
					altname = split;
			}
		}

		if (fileid >= 64)
			fail("exceeded file count -- cannot fit file: %s", s);

		// build composite filename
		if (strlen(argv[2]) + strlen(s) + 1 > sizeof fnbuf2)
			fail("path length exceeded");

		sprintf(fnbuf2, "%s%s", argv[2], s);

		FILE *ff = fopen(fnbuf2, text ? "r" : "rb");
		if (!ff)
			fail("unable to open file: %s", s);

		if (!boot) {
			fread(disk, 384, 1, ff);
			boot = true;
		}

		int firstsec = 0;
		int prevsec = 0;
		int seccount = 0;
		for(;;) {
			int actual = fread(secbuf, 1, 125, ff);

			if (actual < 0)
				fail("read error: %s", s);

			// we must always write at least one data sector
			if (actual == 0 && prevsec)
				break;

			if (sec >= 720)
				fail("out of space writing file: %s", s);

			if (!firstsec)
				firstsec = sec;

			char *dst = disk + (sec - 1)*128;

			if (text) {
				for(int i=0; i<actual; ++i) {
					char c = secbuf[i];

					if (c == '\n')
						c = (char)0x9B;

					dst[i] = c;
				}
			} else {
				memcpy(dst, secbuf, actual);
			}

			dst[125] = (fileid << 2);
			dst[127] = actual;

			if (prevsec) {
				char *prevdst = disk + (prevsec - 1)*128;

				prevdst[125] += (sec >> 8);
				prevdst[126] = (char)(sec & 0xff);
			}

			// allocate sector in VTOC
			vtoc[10 + (sec >> 3)] &= ~(0x80 >> (sec & 7));
			--freesecs;

			prevsec = sec;
			++sec;
			if (sec == 360)
				sec = 369;
			++seccount;
		}

		fclose(ff);

		// write directory entry
		char *dirent = &disk[128*360 + 16*fileid];
		dirent[0] = 0x42;		// in-use, DOS 2
		dirent[1] = (char)(seccount & 0xff);
		dirent[2] = (char)(seccount >> 8);
		dirent[3] = (char)(firstsec & 0xff);
		dirent[4] = (char)(firstsec >> 8);

		const char *fn = t;
		while(fn != altname && fn[-1] != '/')
			--fn;

		if (!*fn || !isalpha((unsigned char)*fn))
			fail("invalid filename: %s", altname);

		int fnlen = 0;

		while(fnlen < 8) {
			char c = *fn;

			if (!c || c == '.')
				break;

			++fn;

			if (!isalnum((unsigned char)c))
				fail("invalid filename: %s", altname);

			if (islower((unsigned char)c))
				c = toupper((unsigned char)c);

			dirent[fnlen + 5] = c;
			++fnlen;
		}

		while(fnlen < 8)
			dirent[fnlen++ + 5] = ' ';

		if (*fn == '.') {
			int extlen = 0;

			++fn;
			while(extlen < 8) {
				char c = *fn;

				if (!c)
					break;

				++fn;

				if (!isalnum((unsigned char)c))
					fail("invalid filename: %s", altname);

				if (islower((unsigned char)c))
					c = toupper((unsigned char)c);

				dirent[extlen + 13] = c;
				++extlen;
			}

			while(extlen < 3)
				dirent[extlen++ + 13] = ' ';
		} else if (*fn) {
			fail("invalid filename: %s", altname);
		}

		++fileid;
	}

	fclose(fi);

	// set free sector count in VTOC
	vtoc[3] = (char)(freesecs & 0xff);
	vtoc[4] = (char)(freesecs >> 8);

	char header[16] = {0};
	header[0] = (char)0x96;
	header[1] = (char)0x02;
	header[2] = (char)((720 * 8) & 0xff);
	header[3] = (char)((720 * 8) >> 8);
	header[4] = (char)0x80;
	header[5] = (char)0x00;

	FILE *fo = fopen(argv[3], "wb");
	if (!fo)
		fail("cannot open output file: %s", argv[3]);

	fwrite(header, 16, 1, fo);
	fwrite(disk, 720 * 128, 1, fo);
	free(disk);

	fclose(fo);

	return 0;
}
