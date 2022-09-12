//
// Memory Track cartridge emulation
//
// by James Hammons
// (C) 2016 Underground Software
//
// The Memory Track is just a large(-ish) EEPROM, holding 128K. We emulate the
// Atmel part, since it seems to be easier to deal with than the AMD part. The
// way it works is the 68K checks in its R/W functions to see if the MT is
// inserted, and, if so, call the R/W functions here. It also checks to see if
// the ROM width was changed to 32-bit; if not, then it reads the normal ROM in
// the ROM space like usual.
//
// The Atmel part reads/writes a single byte into a long space. So we have to
// adjust for that when reading from/writing to the NVRAM.
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -----------------------------------------------------------
// JLH  06/12/2016  Created this file ;-)
//

#include "memtrack.h"

#include <stdlib.h>
#include <string.h>
#include <log.h>
#include <settings.h>


#define MEMTRACK_FILENAME	"memtrack.eeprom"
//#define DEBUG_MEMTRACK

enum { MT_NONE, MT_PROD_ID, MT_RESET, MT_WRITE_ENABLE };
enum { MT_IDLE, MT_PHASE1, MT_PHASE2 };

uint8_t mtMem[0x20000];
uint8_t mtCommand = MT_NONE;
uint8_t mtState = MT_IDLE;
bool haveMT = false;
char mtFilename[MAX_PATH];

// Private function prototypes
void MTWriteFile(void);
void MTStateMachine(uint8_t reg, uint16_t data);


void MTInit(void)
{
	sprintf(mtFilename, "%s%s", vjs.EEPROMPath, MEMTRACK_FILENAME);
	FILE * fp = fopen(mtFilename, "rb");

	if (fp)
	{
		size_t ignored = fread(mtMem, 1, 0x20000, fp);
		fclose(fp);
		WriteLog("MT: Loaded NVRAM from %s\n", mtFilename);
		haveMT = true;
	}
	else
		WriteLog("MT: Could not open file \"%s\"!\n", mtFilename);
}


void MTReset(void)
{
	if (!haveMT)
		memset(mtMem, 0xFF, 0x20000);
}


void MTDone(void)
{
	MTWriteFile();
	WriteLog("MT: Done.\n");
}


void MTWriteFile(void)
{
	if (!haveMT)
		return;

	FILE * fp = fopen(mtFilename, "wb");

	if (fp)
	{
		fwrite(mtMem, 1, 0x20000, fp);
		fclose(fp);
	}
	else
		WriteLog("MT: Could not create file \"%s\"!", mtFilename);
}


//
// This is crappy, there doesn't seem to be a word interface to the NVRAM. But
// we'll keep this as a placeholder for now.
//
uint16_t MTReadWord(uint32_t addr)
{
	uint32_t value = MTReadLong(addr);

	if ((addr & 0x03) == 0)
		value >>= 16;
	else if ((addr & 0x03) == 2)
		value &= 0xFFFF;

#ifdef DEBUG_MEMTRACK
WriteLog("MT: Reading word @ $%06X: $%04X\n", addr, value);
#endif

	return (uint16_t)value;
}


uint32_t MTReadLong(uint32_t addr)
{
	uint32_t value = 0;

	if (mtCommand == MT_PROD_ID)
	{
		if (addr == 0x800000)
			value = 0x1F;
		else if (addr == 0x800004)
			value = 0xD5;
	}
	else
	{
		value = (uint32_t)mtMem[(addr & 0x7FFFC) >> 2];
	}

	// We do this because we're not sure how the real thing behaves; but it
	// seems reasonable on its face to do it this way. :-P So we turn off write
	// mode when reading the NVRAM.
	if (mtCommand == MT_WRITE_ENABLE)
		mtCommand = MT_NONE;

#ifdef DEBUG_MEMTRACK
WriteLog("MT: Reading long @ $%06X: $%08X\n", addr, value << 16);
#endif
	return value << 16;
}


void MTWriteWord(uint32_t addr, uint16_t data)
{
	// We don't care about writes to long offsets + 2
	if ((addr & 0x3) == 2)
		return;

#ifdef DEBUG_MEMTRACK
WriteLog("MT: Writing word @ $%06X: $%04X (Writing is %sabled)\n", addr, data, (mtCommand == MT_WRITE_ENABLE ? "en" : "dis"));
#endif

	// Write to the NVRAM if it's enabled...
	if (mtCommand == MT_WRITE_ENABLE)
	{
		mtMem[(addr & 0x7FFFC) >> 2] = (uint8_t)(data & 0xFF);
		return;
	}

	switch (addr)
	{
	case (0x800000 + (4 * 0x5555)):		// $815554
		MTStateMachine(0, data);
		break;
	case (0x800000 + (4 * 0x2AAA)):		// $80AAA8
		MTStateMachine(1, data);
		break;
	}
}


void MTWriteLong(uint32_t addr, uint32_t data)
{
	// Strip off lower 3 bits of the passed in address
	addr &= 0xFFFFFC;

	MTWriteWord(addr + 0, data & 0xFFFF);
	MTWriteWord(addr + 2, data >> 16);
}


void MTStateMachine(uint8_t reg, uint16_t data)
{
#ifdef DEBUG_MEMTRACK
WriteLog("MTStateMachine: reg = %u, data = $%02X, current state = %u\n", reg, data, mtState);
#endif
	switch (mtState)
	{
	case MT_IDLE:
		if ((reg == 0) && (data == 0xAA))
			mtState = MT_PHASE1;

		break;
	case MT_PHASE1:
		if ((reg == 1) && (data == 0x55))
			mtState = MT_PHASE2;
		else
			mtState = MT_IDLE;

		break;
	case MT_PHASE2:
		if (reg == 0)
		{
			if (data == 0x90)		// Product ID
				mtCommand = MT_PROD_ID;
			else if (data == 0xF0)	// Reset
				mtCommand = MT_NONE;
			else if (data == 0xA0)	// Write enagle
				mtCommand = MT_WRITE_ENABLE;
			else
				mtCommand = MT_NONE;
		}

		mtState = MT_IDLE;
		break;
	}
#ifdef DEBUG_MEMTRACK
WriteLog("                state = %u, cmd = %u\n", mtState, mtCommand);
#endif
}

