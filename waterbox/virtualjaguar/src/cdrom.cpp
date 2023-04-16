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

void CDROMInit(void)
{
}

void CDROMReset(void)
{
	memset(cdRam, 0x00, sizeof(cdRam));
}

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

static uint8_t minTrack, maxTrack;

uint16_t CDROMReadWord(uint32_t offset, uint32_t who)
{
	offset &= 0xFF;

	uint16_t data = 0x0000;

	if (offset == UNKNOWN + 2)
		data = CDROMBusRead();
	else if (offset < FIFO_DATA || offset > FIFO_DATA + 7)
		data = GET16(cdRam, offset);

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
static uint16_t rxData;
static uint16_t rxDataBit;

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
					}
				}
				else
				{
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
