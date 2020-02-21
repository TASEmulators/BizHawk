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
#include <ctype.h>

int cmd_makeexports(int argc, const char *const *argv) {
	if (argc < 3)
		fail("makeexports requires three arguments: source.lab, output.inc, and base prefix to strip");

	FILE *fi = fopen(argv[1], "r");
	if (!fi)
		fail("cannot open input file: %s", argv[1]);

	FILE *fo = fopen(argv[2], "w");
	if (!fo)
		fail("cannot open output file: %s", argv[2]);

	const char *prefix = argv[3];
	size_t prefixlen = strlen(prefix);
	char linebuf[256];

	while(fgets(linebuf, 256, fi)) {
		char dummy;
		int labelpos;
		unsigned type;
		unsigned address;
		if (sscanf(linebuf, "%02X %04X %n%c", &type, &address, &labelpos, &dummy) == 3) {
			char *label = linebuf + labelpos;
			char *labelend = label + strlen(label);

			if (labelend != label && labelend[-1] == '\n')
				--labelend;

			*labelend = 0;

			if (!strncmp(label, prefix, prefixlen)) {
				char startchar = label[prefixlen];
				if (isalpha((unsigned char)startchar) || startchar=='_')
					fprintf(fo, "%s = $%04X\n", label + prefixlen, address);
			}
		}
	}

	fclose(fo);
	fclose(fi);
	return 0;
}
