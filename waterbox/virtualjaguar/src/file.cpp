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
#include "eeprom.h"
#include "jaguar.h"
#include "memory.h"

bool JaguarLoadFile(uint8_t * buffer, uint32_t size)
{
	jaguarROMSize = size;

	jaguarMainROMCRC32 = crc32_calcCheckSum(buffer, jaguarROMSize);

	EepromInit();
	jaguarRunAddress = 0x802000;
	int fileType = ParseFileType(buffer, jaguarROMSize);
	jaguarCartInserted = false;

	if (fileType == JST_ROM)
	{
		jaguarCartInserted = true;
		memcpy(jagMemSpace + 0x800000, buffer, jaguarROMSize);
		jaguarRunAddress = GET32(jagMemSpace, 0x800404);
		return true;
	}
	else if (fileType == JST_ALPINE)
	{
		memset(jagMemSpace + 0x800000, 0xFF, 0x2000);
		memcpy(jagMemSpace + 0x802000, buffer, jaguarROMSize);

		SET32(jaguarMainRAM, 0x10, 0x00001000);
		SET16(jaguarMainRAM, 0x1000, 0x60FE);
		return true;
	}
	else if (fileType == JST_ABS_TYPE1)
	{
		uint32_t loadAddress = GET32(buffer, 0x16),
			codeSize = GET32(buffer, 0x02) + GET32(buffer, 0x06);

		memcpy(jagMemSpace + loadAddress, buffer + 0x24, codeSize);
		jaguarRunAddress = loadAddress;
		return true;
	}
	else if (fileType == JST_ABS_TYPE2)
	{
		uint32_t loadAddress = GET32(buffer, 0x28), runAddress = GET32(buffer, 0x24),
			codeSize = GET32(buffer, 0x18) + GET32(buffer, 0x1C);

		memcpy(jagMemSpace + loadAddress, buffer + 0xA8, codeSize);
		jaguarRunAddress = runAddress;
		return true;
	}
	else if (fileType == JST_JAGSERVER)
	{
		uint32_t loadAddress = GET32(buffer, 0x22), runAddress = GET32(buffer, 0x2A);
		memcpy(jagMemSpace + loadAddress, buffer + 0x2E, jaguarROMSize - 0x2E);
		jaguarRunAddress = runAddress;

		SET32(jaguarMainRAM, 0x10, 0x00001000);
		SET16(jaguarMainRAM, 0x1000, 0x60FE);

		return true;
	}
	else if (fileType == JST_WTFOMGBBQ)
	{
		uint32_t loadAddress = (buffer[0x1F] << 24) | (buffer[0x1E] << 16) | (buffer[0x1D] << 8) | buffer[0x1C];
		memcpy(jagMemSpace + loadAddress, buffer + 0x20, jaguarROMSize - 0x20);
		jaguarRunAddress = loadAddress;
		return true;
	}

	// We can assume we have JST_NONE at this point. :-P
	return false;
}

bool AlpineLoadFile(uint8_t * buffer, uint32_t size)
{
	jaguarROMSize = size;

	jaguarMainROMCRC32 = crc32_calcCheckSum(buffer, jaguarROMSize);
	EepromInit();

	jaguarRunAddress = 0x802000;

	memset(jagMemSpace + 0x800000, 0xFF, 0x2000);
	memcpy(jagMemSpace + 0x802000, buffer, jaguarROMSize);

	SET32(jaguarMainRAM, 0x10, 0x00001000);
	SET16(jaguarMainRAM, 0x1000, 0x60FE);

	return true;
}

uint32_t ParseFileType(uint8_t * buffer, uint32_t size)
{
	if (buffer[0] == 0x60 && buffer[1] == 0x1B)
		return JST_ABS_TYPE1;

	if (buffer[0] == 0x01 && buffer[1] == 0x50)
		return JST_ABS_TYPE2;

	if (buffer[0] == 0x60 && buffer[1] == 0x1A)
	{
		if (buffer[0x1C] == 'J' && buffer[0x1D] == 'A' && buffer[0x1E] == 'G')
			return JST_JAGSERVER;
		else
			return JST_WTFOMGBBQ;
	}

	if ((size % 1048576) == 0 || size == 131072)
		return JST_ROM;

	if (((size + 8192) % 1048576) == 0)
		return JST_ALPINE;

	return JST_NONE;
}
