//
// CD handler
//
// Originally by David Raingeard
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Extensive rewrites/cleanups/fixes by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  ------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
//

#include "cdrom.h"

#include <string.h>
#include "cdintf.h"	
#include "dac.h"

// Private function prototypes

static void CDROMBusWrite(uint16_t);
static uint16_t CDROMBusRead(void);

#define BUTCH		0x00				// base of Butch == interrupt control register, R/W
#define DSCNTRL 	BUTCH + 0x04		// DSA control register, R/W
#define DS_DATA		BUTCH + 0x0A		// DSA TX/RX data, R/W
#define I2CNTRL		BUTCH + 0x10		// i2s bus control register, R/W
#define SBCNTRL		BUTCH + 0x14		// CD subcode control register, R/W
#define SUBDATA		BUTCH + 0x18		// Subcode data register A
#define SUBDATB		BUTCH + 0x1C		// Subcode data register B
#define SB_TIME		BUTCH + 0x20		// Subcode time and compare enable (D24)
#define FIFO_DATA	BUTCH + 0x24		// i2s FIFO data
#define I2SDAT2		BUTCH + 0x28		// i2s FIFO data (old)
#define UNKNOWN		BUTCH + 0x2C		// Seems to be some sort of I2S interface

uint8_t cdRam[0x100];
static uint16_t cdCmd = 0, cdPtr = 0;
static bool haveCDGoodness;
static uint32_t min, sec, frm, block;
static uint8_t cdBuf[2352 + 96];
static uint32_t cdBufPtr = 2352;

void CDROMInit(void)
{
	haveCDGoodness = CDIntfInit();
}

void CDROMReset(void)
{
	memset(cdRam, 0x00, 0x100);
	cdCmd = 0;
}

void CDROMDone(void)
{
	CDIntfDone();
}

//
// This approach is probably wrong, but let's do it for now.
// What's needed is a complete overhaul of the interrupt system so that
// interrupts are handled as they're generated--instead of the current
// scheme where they're handled on scanline boundaries.
//
void BUTCHExec(uint32_t cycles)
{
}

//
// CD-ROM memory access functions
//

uint8_t CDROMReadByte(uint32_t offset, uint32_t who)
{
	return cdRam[offset & 0xFF];
}

static uint8_t trackNum = 1, minTrack, maxTrack;

uint16_t CDROMReadWord(uint32_t offset, uint32_t who)
{
	offset &= 0xFF;

	uint16_t data = 0x0000;

	if (offset == BUTCH)
		data = 0x0000;
	else if (offset == BUTCH + 2)
		data = (haveCDGoodness ? cdRam[BUTCH + 3] << 8 : 0x0000);
	else if (offset == DS_DATA && haveCDGoodness)
	{
		if ((cdCmd & 0xFF00) == 0x0100)
		{
			cdPtr++;
			switch (cdPtr)
			{
				case 1:
					data = 0x0000;
					break;
				case 2:
					data = 0x0100;
					break;
				case 3:
					data = 0x0200;
					break;
				case 4:
					data = 0x0300;
					break;
				case 5:
					data = 0x0400;
			}
		}
		else if ((cdCmd & 0xFF00) == 0x0200)
		{
			data = 0x0400;
		}
		else if ((cdCmd & 0xFF00) == 0x0300)
		{
			data = CDIntfGetSessionInfo(cdCmd & 0xFF, cdPtr);
			if (data == 0xFF)
			{
				data = 0x0400;
			}
			else
			{
				data |= (0x20 | cdPtr++) << 8;
			}
		}
		else if ((cdCmd & 0xFF00) == 0x1000 || (cdCmd & 0xFF00) == 0x1100 || (cdCmd & 0xFF00) == 0x1200)
			data = 0x0100;
		else if ((cdCmd & 0xFF00) == 0x1400)
		{
			if (trackNum > maxTrack)
			{
				data = 0x400;
			}
			else
			{
				if (cdPtr < 0x62)
					data = (cdPtr << 8) | trackNum;
				else if (cdPtr < 0x65)
					data = (cdPtr << 8) | CDIntfGetTrackInfo(trackNum, (cdPtr - 2) & 0x0F);

				cdPtr++;
				if (cdPtr == 0x65)
					cdPtr = 0x60, trackNum++;
			}
		}
		else if ((cdCmd & 0xFF00) == 0x1500)
		{
			data = cdCmd | 0x0200;
		}
		else if ((cdCmd & 0xFF00) == 0x1800)
		{
			data = cdCmd;
		}
		else if ((cdCmd & 0xFF00) == 0x5400)
		{
			data = cdCmd | 0x00;
		}
		else if ((cdCmd & 0xFF00) == 0x7000)
		{
			data = cdCmd;
		}
		else
		{
			data = 0x0400;
		}
	}
	else if (offset == DS_DATA && !haveCDGoodness)
		data = 0x0400;
	else if (offset >= FIFO_DATA && offset <= FIFO_DATA + 3)
	{
	}
	else if (offset >= FIFO_DATA + 4 && offset <= FIFO_DATA + 7)
	{
	}
	else
		data = GET16(cdRam, offset);

	if (offset == UNKNOWN + 2)
		data = CDROMBusRead();

	return data;
}

void CDROMWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	offset &= 0xFF;
	cdRam[offset] = data;
}

void CDROMWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	offset &= 0xFF;
	SET16(cdRam, offset, data);

	if (offset == DS_DATA)
	{
		cdCmd = data;
		if ((data & 0xFF00) == 0x0200)
		{
			cdPtr = 0;
		}
		else if ((data & 0xFF00) == 0x0300)
		{
			cdPtr = 0;
		}
		else if ((data & 0xFF00) == 0x1000)
		{
			min = data & 0x00FF;
		}
		else if ((data & 0xFF00) == 0x1100)
		{
			sec = data & 0x00FF;
		}
		else if ((data & 0xFF00) == 0x1200)
		{
			frm = data & 0x00FF;
			block = (((min * 60) + sec) * 75) + frm;
			cdBufPtr = 2352;
		}
		else if ((data & 0xFF00) == 0x1400)
		{
			cdPtr = 0x60,
			minTrack = CDIntfGetSessionInfo(data & 0xFF, 0),
			maxTrack = CDIntfGetSessionInfo(data & 0xFF, 1);
			trackNum = minTrack;
		}
		else if ((data & 0xFF00) == 0x1500)	
		{
		}
		else if ((data & 0xFF00) == 0x1800)
		{
		}
		else if ((data & 0xFF00) == 0x5400)
		{
		}
		else if ((data & 0xFF00) == 0x7000)
		{
		}
	}

	if (offset == UNKNOWN + 2)
		CDROMBusWrite(data);
}

//
// State machine for sending/receiving data along a serial bus
//

enum ButchState { ST_INIT, ST_RISING, ST_FALLING };
static ButchState currentState = ST_INIT;
static uint16_t counter = 0;
static bool cmdTx = false;
static uint16_t busCmd;
static uint16_t rxData, txData;
static uint16_t rxDataBit;
static bool firstTime = false;

static void CDROMBusWrite(uint16_t data)
{
	switch (currentState)
	{
		case ST_INIT:
			currentState = ST_RISING;
			break;
		case ST_RISING:
			if (data & 0x0001)
			{
				cmdTx = true;
				counter = 0;
				busCmd = 0;
			}
			else
			{
				if (cmdTx)
				{
					busCmd <<= 1;
					busCmd |= (data & 0x04);
					counter++;

					if (counter == 9)
					{
						busCmd >>= 2;
						cmdTx = false;

						if (busCmd == 0x180)
							rxData = 0x0024;
						else if (busCmd == 0x181)
							rxData = 0x0004;
						else if (busCmd == 0x182)
							rxData = 0x0071;
						else if (busCmd == 0x183)
							rxData = 0xFF67;
						else if (busCmd == 0x184)
							rxData = 0xFFFF;
						else if (busCmd == 0x185)
							rxData = 0xFFFF;
						else
							rxData = 0x0001;

						counter = 0;
						firstTime = true;
						txData = 0;
					}
				}
				else
				{
					txData = (txData << 1) | ((data & 0x04) >> 2);

					rxDataBit = (rxData & 0x8000) >> 12;
					rxData <<= 1;
					counter++;
				}
			}

			currentState = ST_FALLING;
			break;
		case ST_FALLING:
			currentState = ST_INIT;
			break;
	}
}

static uint16_t CDROMBusRead(void)
{
	return rxDataBit;
}

static uint8_t cdBuf2[2532 + 96], cdBuf3[2532 + 96];

uint16_t GetWordFromButchSSI(uint32_t offset, uint32_t who)
{
	bool go = ((offset & 0x0F) == 0x0A || (offset & 0x0F) == 0x0E ? true : false);

	if (!go)
		return 0x000;

	cdBufPtr += 2;

	if (cdBufPtr >= 2352)
	{
		CDIntfReadBlock(block - 150, cdBuf2);
		CDIntfReadBlock(block - 149, cdBuf3);
		for(int i=0; i<2352-4; i+=4)
		{
			cdBuf[i+0] = cdBuf2[i+4];
			cdBuf[i+1] = cdBuf2[i+5];
			cdBuf[i+2] = cdBuf2[i+2];
			cdBuf[i+3] = cdBuf2[i+3];
		}
		cdBuf[2348] = cdBuf3[0];
		cdBuf[2349] = cdBuf3[1];
		cdBuf[2350] = cdBuf2[2350];
		cdBuf[2351] = cdBuf2[2351];

		block++, cdBufPtr = 0;
	}

	return (cdBuf[cdBufPtr + 1] << 8) | cdBuf[cdBufPtr + 0];
}

bool ButchIsReadyToSend(void)
{
	return (cdRam[I2CNTRL + 3] & 0x02 ? true : false);
}

void SetSSIWordsXmittedFromButch(void)
{
	cdBufPtr += 4;

	if (cdBufPtr >= 2352)
	{
		CDIntfReadBlock(block, cdBuf2);
		CDIntfReadBlock(block + 1, cdBuf3);
		memcpy(cdBuf, cdBuf2 + 2, 2350);
		cdBuf[2350] = cdBuf3[0];
		cdBuf[2351] = cdBuf3[1];

		block++, cdBufPtr = 0;
	}

	lrxd = (cdBuf[cdBufPtr + 3] << 8) | cdBuf[cdBufPtr + 2],
	rrxd = (cdBuf[cdBufPtr + 1] << 8) | cdBuf[cdBufPtr + 0];
}
