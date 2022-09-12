//
// FILE.CPP
//
// File support
// by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  ------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
// JLH  02/28/2010  Added functions to look inside .ZIP files and handle
//                  contents
// JLH  06/01/2012  Added function to check ZIP file CRCs against file DB
//

#include "file.h"

#include <stdarg.h>
#include <string.h>
#include "crc32.h"
#include "filedb.h"
#include "eeprom.h"
#include "jaguar.h"
#include "log.h"
#include "memory.h"
#include "universalhdr.h"

// Private function prototypes
//static int ParseFileType(uint8_t header1, uint8_t header2, uint32_t size);

// Private variables/enums


//
// Jaguar file loading
// We do a more intelligent file analysis here instead of relying on (possible
// false) file extensions which people don't seem to give two shits about
// anyway. :-(
//
bool JaguarLoadFile(uint8_t * buffer, uint32_t size)
{
	jaguarROMSize = size;

	jaguarMainROMCRC32 = crc32_calcCheckSum(buffer, jaguarROMSize);
	WriteLog("CRC: %08X\n", (unsigned int)jaguarMainROMCRC32);
// TODO: Check for EEPROM file in ZIP file. If there is no EEPROM in the user's EEPROM
//       directory, copy the one from the ZIP file, if it exists.
	EepromInit();
	jaguarRunAddress = 0x802000;					// For non-BIOS runs, this is true
	int fileType = ParseFileType(buffer, jaguarROMSize);
	jaguarCartInserted = false;

	if (fileType == JST_ROM)
	{
		jaguarCartInserted = true;
		memcpy(jagMemSpace + 0x800000, buffer, jaguarROMSize);
// Checking something...
jaguarRunAddress = GET32(jagMemSpace, 0x800404);
WriteLog("FILE: Cartridge run address is reported as $%X...\n", jaguarRunAddress);
		return true;
	}
	else if (fileType == JST_ALPINE)
	{
		// File extension ".ROM": Alpine image that loads/runs at $802000
		WriteLog("FILE: Setting up Alpine ROM... Run address: 00802000, length: %08X\n", jaguarROMSize);
		memset(jagMemSpace + 0x800000, 0xFF, 0x2000);
		memcpy(jagMemSpace + 0x802000, buffer, jaguarROMSize);

// Maybe instead of this, we could try requiring the STUBULATOR ROM? Just a thought...
		// Try setting the vector to say, $1000 and putting an instruction there that loops forever:
		// This kludge works! Yeah!
		SET32(jaguarMainRAM, 0x10, 0x00001000);
		SET16(jaguarMainRAM, 0x1000, 0x60FE);		// Here: bra Here
		return true;
	}
	else if (fileType == JST_ABS_TYPE1)
	{
		// For ABS type 1, run address == load address
		uint32_t loadAddress = GET32(buffer, 0x16),
			codeSize = GET32(buffer, 0x02) + GET32(buffer, 0x06);
		WriteLog("FILE: Setting up homebrew (ABS-1)... Run address: %08X, length: %08X\n", loadAddress, codeSize);
		memcpy(jagMemSpace + loadAddress, buffer + 0x24, codeSize);
		jaguarRunAddress = loadAddress;
		return true;
	}
	else if (fileType == JST_ABS_TYPE2)
	{
		uint32_t loadAddress = GET32(buffer, 0x28), runAddress = GET32(buffer, 0x24),
			codeSize = GET32(buffer, 0x18) + GET32(buffer, 0x1C);
		WriteLog("FILE: Setting up homebrew (ABS-2)... Run address: %08X, length: %08X\n", runAddress, codeSize);
		memcpy(jagMemSpace + loadAddress, buffer + 0xA8, codeSize);
		jaguarRunAddress = runAddress;
		return true;
	}
	// NB: This is *wrong*
	/*
	Basically, if there is no "JAG" at position $1C, then the long there is the load/start
	address in LITTLE ENDIAN.
	If "JAG" is present, the the next character ("R" or "L") determines the size of the
	JagServer command (2 bytes vs. 4). Following that are the commands themselves;
	typically it will either be 2 (load) or 3 (load & run). Command headers go like so:
	2:
	Load address (long)
	Length (long)
	payload
	3:
	Load address (long)
	Length (long)
	Run address (long)
	payload
	5: (Reset)
	[command only]
	7: (Run at address)
	Run address (long)
	[no payload]
	9: (Clear memory)
	Start address (long)
	End address (long)
	[no payload]
	10: (Poll for commands)
	[command only]
	12: (Load & run user program)
	filname, terminated with NULL
	[no payload]
	$FFFF: (Halt)
	[no payload]
	*/
	else if (fileType == JST_JAGSERVER)
	{
		// This kind of shiaut should be in the detection code below...
		// (and now it is! :-)
//		if (buffer[0x1C] == 'J' && buffer[0x1D] == 'A' && buffer[0x1E] == 'G')
//		{
			// Still need to do some checking here for type 2 vs. type 3. This assumes 3
			// Also, JAGR vs. JAGL (word command size vs. long command size)
			uint32_t loadAddress = GET32(buffer, 0x22), runAddress = GET32(buffer, 0x2A);
			WriteLog("FILE: Setting up homebrew (Jag Server)... Run address: $%X, length: $%X\n", runAddress, jaguarROMSize - 0x2E);
			memcpy(jagMemSpace + loadAddress, buffer + 0x2E, jaguarROMSize - 0x2E);
			jaguarRunAddress = runAddress;

// Hmm. Is this kludge necessary?
SET32(jaguarMainRAM, 0x10, 0x00001000);		// Set Exception #4 (Illegal Instruction)
SET16(jaguarMainRAM, 0x1000, 0x60FE);		// Here: bra Here

			return true;
//		}
//		else // Special WTFOMGBBQ type here...
//		{
//			uint32_t loadAddress = (buffer[0x1F] << 24) | (buffer[0x1E] << 16) | (buffer[0x1D] << 8) | buffer[0x1C];
//			WriteLog("FILE: Setting up homebrew (GEMDOS WTFOMGBBQ type)... Run address: $%X, length: $%X\n", loadAddress, jaguarROMSize - 0x20);
//			memcpy(jagMemSpace + loadAddress, buffer + 0x20, jaguarROMSize - 0x20);
//			jaguarRunAddress = loadAddress;
//			return true;
//		}
	}
	else if (fileType == JST_WTFOMGBBQ)
	{
		uint32_t loadAddress = (buffer[0x1F] << 24) | (buffer[0x1E] << 16) | (buffer[0x1D] << 8) | buffer[0x1C];
		WriteLog("FILE: Setting up homebrew (GEMDOS WTFOMGBBQ type)... Run address: $%X, length: $%X\n", loadAddress, jaguarROMSize - 0x20);
		memcpy(jagMemSpace + loadAddress, buffer + 0x20, jaguarROMSize - 0x20);
		jaguarRunAddress = loadAddress;
		return true;
	}

	// We can assume we have JST_NONE at this point. :-P
	WriteLog("FILE: Failed to load headerless file.\n");
	return false;
}


//
// "Alpine" file loading
// Since the developers were coming after us with torches and pitchforks, we
// decided to allow this kind of thing. ;-) But ONLY FOR THE DEVS, DAMMIT! >:-U
// O_O
//
bool AlpineLoadFile(uint8_t * buffer, uint32_t size)
{
	jaguarROMSize = size;

	jaguarMainROMCRC32 = crc32_calcCheckSum(buffer, jaguarROMSize);
	WriteLog("FILE: CRC is %08X\n", (unsigned int)jaguarMainROMCRC32);
	EepromInit();

	jaguarRunAddress = 0x802000;

	WriteLog("FILE: Setting up Alpine ROM with non-standard length... Run address: 00802000, length: %08X\n", jaguarROMSize);

	memset(jagMemSpace + 0x800000, 0xFF, 0x2000);
	memcpy(jagMemSpace + 0x802000, buffer, jaguarROMSize);

// Maybe instead of this, we could try requiring the STUBULATOR ROM? Just a thought...
	// Try setting the vector to say, $1000 and putting an instruction there
	// that loops forever:
	// This kludge works! Yeah!
	SET32(jaguarMainRAM, 0x10, 0x00001000);		// Set Exception #4 (Illegal Instruction)
	SET16(jaguarMainRAM, 0x1000, 0x60FE);		// Here: bra Here

	return true;
}

//
// Parse the file type based upon file size and/or headers.
//
uint32_t ParseFileType(uint8_t * buffer, uint32_t size)
{
	// Check headers first...

	// ABS/COFF type 1
	if (buffer[0] == 0x60 && buffer[1] == 0x1B)
		return JST_ABS_TYPE1;

	// ABS/COFF type 2
	if (buffer[0] == 0x01 && buffer[1] == 0x50)
		return JST_ABS_TYPE2;

	// Jag Server & other old shite
	if (buffer[0] == 0x60 && buffer[1] == 0x1A)
	{
		if (buffer[0x1C] == 'J' && buffer[0x1D] == 'A' && buffer[0x1E] == 'G')
			return JST_JAGSERVER;
		else
			return JST_WTFOMGBBQ;
	}

	// And if that fails, try file sizes...

	// If the file size is divisible by 1M, we probably have an regular ROM.
	// We can also check our CRC32 against the internal ROM database to be sure.
	// (We also check for the Memory Track cartridge size here as well...)
	if ((size % 1048576) == 0 || size == 131072)
		return JST_ROM;

	// If the file size + 8192 bytes is divisible by 1M, we probably have an
	// Alpine format ROM.
	if (((size + 8192) % 1048576) == 0)
		return JST_ALPINE;

	// Headerless crap
	return JST_NONE;
}

//
// Check for universal header
//
bool HasUniversalHeader(uint8_t * rom, uint32_t romSize)
{
	// Sanity check
	if (romSize < 8192)
		return false;

	for(int i=0; i<8192; i++)
		if (rom[i] != universalCartHeader[i])
			return false;

	return true;
}

#if 0
// Misc. doco

/*
Stubulator ROM vectors...
handler 001 at $00E00008
handler 002 at $00E008DE
handler 003 at $00E008E2
handler 004 at $00E008E6
handler 005 at $00E008EA
handler 006 at $00E008EE
handler 007 at $00E008F2
handler 008 at $00E0054A
handler 009 at $00E008FA
handler 010 at $00000000
handler 011 at $00000000
handler 012 at $00E008FE
handler 013 at $00E00902
handler 014 at $00E00906
handler 015 at $00E0090A
handler 016 at $00E0090E
handler 017 at $00E00912
handler 018 at $00E00916
handler 019 at $00E0091A
handler 020 at $00E0091E
handler 021 at $00E00922
handler 022 at $00E00926
handler 023 at $00E0092A
handler 024 at $00E0092E
handler 025 at $00E0107A
handler 026 at $00E0107A
handler 027 at $00E0107A
handler 028 at $00E008DA
handler 029 at $00E0107A
handler 030 at $00E0107A
handler 031 at $00E0107A
handler 032 at $00000000

Let's try setting up the illegal instruction vector for a stubulated jaguar...

		SET32(jaguar_mainRam, 0x08, 0x00E008DE);
		SET32(jaguar_mainRam, 0x0C, 0x00E008E2);
		SET32(jaguar_mainRam, 0x10, 0x00E008E6);	// <-- Should be here (it is)...
		SET32(jaguar_mainRam, 0x14, 0x00E008EA);//*/

/*
ABS Format sleuthing (LBUGDEMO.ABS):

000000  60 1B 00 00 05 0C 00 04 62 C0 00 00 04 28 00 00
000010  12 A6 00 00 00 00 00 80 20 00 FF FF 00 80 25 0C
000020  00 00 40 00

DRI-format file detected...
Text segment size = 0x0000050c bytes
Data segment size = 0x000462c0 bytes
BSS Segment size = 0x00000428 bytes
Symbol Table size = 0x000012a6 bytes
Absolute Address for text segment = 0x00802000
Absolute Address for data segment = 0x0080250c
Absolute Address for BSS segment = 0x00004000

(CRZDEMO.ABS):
000000  01 50 00 03 00 00 00 00 00 03 83 10 00 00 05 3b
000010  00 1c 00 03 00 00 01 07 00 00 1d d0 00 03 64 98
000020  00 06 8b 80 00 80 20 00 00 80 20 00 00 80 3d d0

000030  2e 74 78 74 00 00 00 00 00 80 20 00 00 80 20 00 .txt (+36 bytes)
000040  00 00 1d d0 00 00 00 a8 00 00 00 00 00 00 00 00
000050  00 00 00 00 00 00 00 20
000058  2e 64 74 61 00 00 00 00 00 80 3d d0 00 80 3d d0 .dta (+36 bytes)
000068  00 03 64 98 00 00 1e 78 00 00 00 00 00 00 00 00
000078  00 00 00 00 00 00 00 40
000080  2e 62 73 73 00 00 00 00 00 00 50 00 00 00 50 00 .bss (+36 bytes)
000090  00 06 8b 80 00 03 83 10 00 00 00 00 00 00 00 00
0000a0  00 00 00 00 00 00 00 80

Header size is $A8 bytes...

BSD/COFF format file detected...
3 sections specified
Symbol Table offset = 230160				($00038310)
Symbol Table contains 1339 symbol entries	($0000053B)
The additional header size is 28 bytes		($001C)
Magic Number for RUN_HDR = 0x00000107
Text Segment Size = 7632					($00001DD0)
Data Segment Size = 222360					($00036498)
BSS Segment Size = 428928					($00068B80)
Starting Address for executable = 0x00802000
Start of Text Segment = 0x00802000
Start of Data Segment = 0x00803dd0
*/
#endif
