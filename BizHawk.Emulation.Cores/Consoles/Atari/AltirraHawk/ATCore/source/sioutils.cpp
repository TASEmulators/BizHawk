//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2011 Avery Lee
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
#include <at/atcore/sioutils.h>

uint8 ATComputeSIOChecksum(const uint8 *p, int len) {
	uint32 checksum = 0;
	for(int i=0; i<len; ++i) {
		checksum += p[i];
		checksum += (checksum >> 8);
		checksum &= 0xff;
	}

	return (uint8)checksum;
}

const char *ATDecodeSIOCommand(uint8 device, uint8 command, const uint8 *aux) {
	if (device >= 0x31 && device <= 0x3F) {		// D:
		switch(command) {
			case 0x21:	return "Disk: Format";
			case 0x22:	return "Disk: Format medium-density";
			case 0x28:	return "Disk: Happy head positioning test recalibrate";
			case 0x29:	return "Disk: Happy head positioning test seek";
			case 0x2D:	return "Disk: Happy RPM test";
			case 0x3F:	return "Disk: Get high-speed index";
			case 0x48:	return "Disk: Happy drive control";
			case 0x4E:	return "Disk: Read PERCOM block";
			case 0x4F:	return "Disk: Write PERCOM block";
			case 0x50:	return "Disk: Write sector";
			case 0x51:	return "Disk: Quiet";
			case 0x52:	return "Disk: Read sector";
			case 0x53:	return "Disk: Get status";
			case 0x54:	return "Disk: Happy RAM test";
			case 0x57:	return "Disk: Write sector with verify";
			case 0x58:	return "Disk: Execute code (Indus GT)";
			case 0x66:	return "Disk: Format skewed";

			case 0x70:	return "Disk: Write sector (Happy high speed)";
			case 0x72:	return "Disk: Read sector (Happy high speed)";
			case 0x77:	return "Disk: Write sector with verify (Happy high speed)";

			case 0xA1:	return "Disk: Format with high-speed skew";
			case 0xA2:	return "Disk: Format medium-density (high speed)";
			case 0xA3:	return "Disk: Format boot tracks with normal skew (Synchromesh)";
			case 0xD0:	return "Disk: Write sector (high speed)";
			case 0xD2:	return "Disk: Read sector (high speed)";
			case 0xD3:	return "Disk: Get status (high speed)";
			case 0xD7:	return "Disk: Write sector with verify (high speed)";
			case 0xE6:	return "Disk: Format skewed (high speed)";

			default:	return "Disk: ?";
		}
	} else if (device >= 0x40 && device <= 0x43) {		// P:
		switch(command) {
			case 0x53:	return "Printer: Get status";
			case 0x57:	return "Printer: Write";

			default:	return "Printer: ?";
		}
	} else if (device == 0x45) {		// APE
		switch(command) {
			case 0x93:	return "APE: Read clock";
			default:	return "APE: ?";
		}
	} else if (device == 0x46) {		// AspeQt
		switch(command) {
			case 0x93:	return "AspeQt: Read clock";
			default:	return "AspeQt: ?";
		}
	} else if (device == 0x4F) {
		if (command == 0x40) {
			if (aux[0] == aux[1]) {
				switch(aux[0]) {
					case 0x00:	return "Type 3 poll";
					case 0x4E:	return "Null poll";
					case 0x4F:	return "Poll reset";
				}
			}

			if (aux[1] >= 1 && aux[1] <= 9)
				return "Type 4 poll";
		}
	} else if (device >= 0x50 && device <= 0x53) {		// 850
		switch(command) {
			case 0x21:	return "850: Load relocator";
			case 0x26:	return "850: Load handler";
			case 0x41:	return "850: Control";
			case 0x42:	return "850: Configure";
			case 0x53:	return "850: Get status";
			case 0x57:	return "850: Write block";
			case 0x58:	return "850: Stream";
			default:	return "850: ?";
		}
	} else if (device == 0x58) {		// 1030
		switch(command) {
			case 0x3C:	return "1030: Get handler";
			default:	return "1030: ?";
		}
	} else if (device == 0x6F) {		// PCLink
		switch(command) {
			case 0x3F:	return "PCLink: Get high-speed index";
			case 0x50:	return "PCLink: Put";
			case 0x52:	return "PCLink: Read";
			case 0x53:	return "PCLink: Get status";

			case 0xD0:	return "PCLink: Put (high speed)";
			case 0xD2:	return "PCLink: Read (high speed)";
			case 0xD3:	return "PCLink: Get status (high speed)";
		}
	}

	return "?";
}
