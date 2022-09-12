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

#include <string.h>									// For memset, etc.
//#include "jaguar.h"									// For GET32/SET32 macros
//#include "m68k.h"	//???
//#include "memory.h"
#include "cdintf.h"									// System agnostic CD interface functions
#include "log.h"
#include "dac.h"

//#define CDROM_LOG									// For CDROM logging, obviously

/*
BUTCH     equ  $DFFF00		; base of Butch=interrupt control register, R/W
DSCNTRL   equ  BUTCH+4		; DSA control register, R/W
DS_DATA   equ  BUTCH+$A		; DSA TX/RX data, R/W
I2CNTRL   equ  BUTCH+$10	; i2s bus control register, R/W
SBCNTRL   equ  BUTCH+$14	; CD subcode control register, R/W
SUBDATA   equ  BUTCH+$18	; Subcode data register A
SUBDATB   equ  BUTCH+$1C	; Subcode data register B
SB_TIME   equ  BUTCH+$20	; Subcode time and compare enable (D24)
FIFO_DATA equ  BUTCH+$24	; i2s FIFO data
I2SDAT1   equ  BUTCH+$24	; i2s FIFO data
I2SDAT2   equ  BUTCH+$28	; i2s FIFO data
          equ  BUTCH+$2C	; CD EEPROM interface

;
; Butch's hardware registers
;
;BUTCH     equ  $DFFF00		;base of Butch=interrupt control register, R/W
;
;  When written (Long):
;
;  bit0 - set to enable interrupts
;  bit1 - enable CD data FIFO half full interrupt
;  bit2 - enable CD subcode frame-time interrupt (@ 2x spped = 7ms.)
;  bit3 - enable pre-set subcode time-match found interrupt
;  bit4 - CD module command transmit buffer empty interrupt
;  bit5 - CD module command receive buffer full
;  bit6 - CIRC failure interrupt
;
;  bit7-31  reserved, set to 0
;
;  When read (Long):
;
;  bit0-8 reserved
;
;  bit9  - CD data FIFO half-full flag pending
;  bit10 - Frame pending
;  bit11 - Subcode data pending
;  bit12 - Command to CD drive pending (trans buffer empty if 1)
;  bit13 - Response from CD drive pending (rec buffer full if 1)
;  bit14 - CD uncorrectable data error pending
;
;   Offsets from BUTCH
;
O_DSCNTRL   equ  4		; DSA control register, R/W
O_DS_DATA   equ  $A		; DSA TX/RX data, R/W
;
O_I2CNTRL   equ  $10		; i2s bus control register, R/W
;
;  When read:
;
;  b0 - I2S data from drive is ON if 1
;  b1 - I2S path to Jerry is ON if 1
;  b2 - reserved
;  b3 - host bus width is 16 if 1, else 32
;  b4 - FIFO state is not empty if 1
;
O_SBCNTRL   equ  $14		; CD subcode control register, R/W
O_SUBDATA   equ  $18		; Subcode data register A
O_SUBDATB   equ  $1C		; Subcode data register B
O_SB_TIME   equ  $20		; Subcode time and compare enable (D24)
O_FIFODAT   equ  $24		; i2s FIFO data
O_I2SDAT2   equ  $28		; i2s FIFO data (old)
*/

/*
Commands sent through DS_DATA:

$01nn - ? Play track nn ? Seek to track nn ?
$0200 - Stop CD
$03nn - Read session nn TOC (short)
$0400 - Pause CD
$0500 - Unpause CD
$10nn - Goto (min?)
$11nn - Goto (sec?)
$12nn - Goto (frm?)
$14nn - Read session nn TOC (full)
$15nn - Set CD mode
$18nn - Spin up CD to session nn
$5000 - ?
$5100 - Mute CD (audio mode only)
$51FF - Unmute CD (audio mode only)
$5400 - Read # of sessions on CD
$70nn - Set oversampling mode

Commands send through serial bus:

$100 - ? Acknowledge ? (Erase/Write disable)
$130 - ? (Seems to always prefix the $14n commands) (Erase/Write enable)
$140 - Returns ACK (1) (Write to NVRAM?) (Write selected register)
$141 - Returns ACK (1)
$142 - Returns ACK (1)
$143 - Returns ACK (1)
$144 - Returns ACK (1)
$145 - Returns ACK (1)
$180 - Returns 16-bit value (NVRAM?) (read from EEPROM)
$181 - Returns 16-bit value
$182 - Returns 16-bit value
$183 - Returns 16-bit value
$184 - Returns 16-bit value
$185 - Returns 16-bit value

;  The BUTCH interface for the CD-ROM module is a long-word register,
;   where only the least signifigant 4 bits are used
;
eeprom	equ	$DFFF2c			;interface to CD-eeprom
;
;  bit3 - busy if 0 after write cmd, or Data In after read cmd 
;  bit2 - Data Out
;  bit1 - clock
;  bit0 - Chip Select (CS)
;
;
;   Commands specific to the National Semiconductor NM93C14
;
;
;  9-bit commands..
;			 876543210
eREAD	equ	%110000000		;read from EEPROM
eEWEN	equ	%100110000		;Erase/write Enable
eERASE	equ	%111000000		;Erase selected register
eWRITE	equ	%101000000		;Write selected register
eERAL	equ	%100100000		;Erase all registers
eWRAL	equ	%100010000		;Writes all registers
eEWDS	equ	%100000000		;Erase/Write disable (default)

So... are there $40 words of memory? 128 bytes?

*/

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

const char * BReg[12] = { "BUTCH", "DSCNTRL", "DS_DATA", "???", "I2CNTRL",
	"SBCNTRL", "SUBDATA", "SUBDATB", "SB_TIME", "FIFO_DATA", "I2SDAT2",
	"UNKNOWN" };
//extern const char * whoName[9];


static uint8_t cdRam[0x100];
static uint16_t cdCmd = 0, cdPtr = 0;
static bool haveCDGoodness;
static uint32_t min, sec, frm, block;
static uint8_t cdBuf[2352 + 96];
static uint32_t cdBufPtr = 2352;
//Also need to set up (save/restore) the CD's NVRAM


//extern bool GetRawTOC(void);
void CDROMInit(void)
{
	haveCDGoodness = CDIntfInit();

//GetRawTOC();
/*uint8_t buf[2448];
uint32_t sec = 18667 - 150;
memset(buf, 0, 2448);
if (!CDIntfReadBlock(sec, buf))
{
	WriteLog("CDROM: Attempt to read with subchannel data failed!\n");
	return;
}

//24x98+96
//96=4x24=4x4x6
WriteLog("\nCDROM: Read sector %u...\n\n", sec);
for(int i=0; i<98; i++)
{
	WriteLog("%04X: ", i*24);
	for(int j=0; j<24; j++)
	{
		WriteLog("%02X ", buf[j + (i*24)]);
	}
	WriteLog("\n");
}
WriteLog("\nRaw P-W subchannel data:\n\n");
for(int i=0; i<6; i++)
{
	WriteLog("%02X: ", i*16);
	for(int j=0; j<16; j++)
	{
		WriteLog("%02X ", buf[2352 + j + (i*16)]);
	}
	WriteLog("\n");
}
WriteLog("\nP subchannel data: ");
for(int i=0; i<96; i+=8)
{
	uint8_t b = 0;
	for(int j=0; j<8; j++)
		b |= ((buf[2352 + i + j] & 0x80) >> 7) << (7 - j);

	WriteLog("%02X ", b);
}
WriteLog("\nQ subchannel data: ");
for(int i=0; i<96; i+=8)
{
	uint8_t b = 0;
	for(int j=0; j<8; j++)
		b |= ((buf[2352 + i + j] & 0x40) >> 6) << (7 - j);

	WriteLog("%02X ", b);
}
WriteLog("\n\n");//*/
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
#if 1
// We're chickening out for now...
return;
#else
//	extern uint8_t * jerry_ram_8;					// Hmm.

	// For now, we just do the FIFO interrupt. Timing is also likely to be WRONG as well.
	uint32_t cdState = GET32(cdRam, BUTCH);

	if (!(cdState & 0x01))						// No BUTCH interrupts enabled
		return;

	if (!(cdState & 0x22))
		return;									// For now, we only handle FIFO/buffer full interrupts...

	// From what I can make out, it seems that each FIFO is 32 bytes long

//	DSPSetIRQLine(DSPIRQ_EXT, ASSERT_LINE);
//I'm *sure* this is wrong--prolly need to generate DSP IRQs as well!
	if (jerry_ram_8[0x23] & 0x3F)				// Only generate an IRQ if enabled!
		GPUSetIRQLine(GPUIRQ_DSP, ASSERT_LINE);
#endif
}


//
// CD-ROM memory access functions
//

uint8_t CDROMReadByte(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
#ifdef CDROM_LOG
	if ((offset & 0xFF) < 12 * 4)
		WriteLog("[%s] ", BReg[(offset & 0xFF) / 4]);
	WriteLog("CDROM: %s reading byte $%02X from $%08X [68K PC=$%08X]\n", whoName[who], offset, cdRam[offset & 0xFF], m68k_get_reg(NULL, M68K_REG_PC));
#endif
	return cdRam[offset & 0xFF];
}

static uint8_t trackNum = 1, minTrack, maxTrack;
//static uint8_t minutes[16] = {  0,  0,  2,  5,  7, 10, 12, 15, 17, 20, 22, 25, 27, 30, 32, 35 };
//static uint8_t seconds[16] = {  0,  0, 30,  0, 30,  0, 30,  0, 30,  0, 30,  0, 30,  0, 30,  0 };
//static uint8_t frames[16]  = {  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0 };
//static uint16_t sd = 0;
uint16_t CDROMReadWord(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	offset &= 0xFF;

	uint16_t data = 0x0000;

	if (offset == BUTCH)
		data = 0x0000;
	else if (offset == BUTCH + 2)
// We need to fix this so it's not as brain-dead as it is now--i.e., make it so that when
// a command is sent to the CDROM, we control here whether or not it succeeded or whether
// the command is still being carried out, etc.

// bit12 - Command to CD drive pending (trans buffer empty if 1)
// bit13 - Response from CD drive pending (rec buffer full if 1)
//		data = (haveCDGoodness ? 0x3000 : 0x0000);	// DSA RX Interrupt pending bit (0 = pending)
//This only returns ACKs for interrupts that are set:
//This doesn't work for the initial code that writes $180000 to BUTCH. !!! FIX !!!
		data = (haveCDGoodness ? cdRam[BUTCH + 3] << 8 : 0x0000);
//	else if (offset == SUBDATA + 2)
//		data = sd++ | 0x0010;						// Have no idea what this is...
	else if (offset == DS_DATA && haveCDGoodness)
	{
		if ((cdCmd & 0xFF00) == 0x0100)				// ???
		{
//Not sure how to acknowledge the ???...
//			data = 0x0400;//?? 0x0200;
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
			}//*/
			//WriteLog("CDROM: Reading DS_DATA (???), cdCmd=$%04X\n", cdCmd);
		}
		else if ((cdCmd & 0xFF00) == 0x0200)			// Stop CD
		{
//Not sure how to acknowledge the stop...
			data = 0x0400;//?? 0x0200;
/*			cdPtr++;
			switch (cdPtr)
			{
			case 1:
				data = 0x00FF;
				break;
			case 2:
				data = 0x01FF;
				break;
			case 3:
				data = 0x02FF;
				break;
			case 4:
				data = 0x03FF;
				break;
			case 5:
				data = 0x0400;
			}//*/
			WriteLog("CDROM: Reading DS_DATA (stop), cdCmd=$%04X\n", cdCmd);
		}
		else if ((cdCmd & 0xFF00) == 0x0300)		// Read session TOC (overview?)
		{

/*
TOC: [Sess] [adrCtl] [?] [point] [?] [?] [?] [?] [pmin] [psec] [pframe]
TOC: 1 10 00 a0 00:00:00 00 01:00:00
TOC: 1 10 00 a1 00:00:00 00 01:00:00
TOC: 1 10 00 a2 00:00:00 00 03:42:42
TOC: 1 10 00  1 00:00:00 00 00:02:00   <-- Track #1
TOC: 1 50 00 b0 06:12:42 02 79:59:74
TOC: 1 50 00 c0 128:00:32 00 97:18:06
TOC: 2 10 00 a0 00:00:00 00 02:00:00
TOC: 2 10 00 a1 00:00:00 00 11:00:00
TOC: 2 10 00 a2 00:00:00 00 54:32:18
TOC: 2 10 00  2 00:00:00 00 06:14:42   <-- Track #2
TOC: 2 10 00  3 00:00:00 00 06:24:42   <-- Track #3
TOC: 2 10 00  4 00:00:00 00 17:42:00   <-- Track #4
TOC: 2 10 00  5 00:00:00 00 22:26:15   <-- Track #5
TOC: 2 10 00  6 00:00:00 00 29:50:16   <-- Track #6
TOC: 2 10 00  7 00:00:00 00 36:01:49   <-- Track #7
TOC: 2 10 00  8 00:00:00 00 40:37:59   <-- Track #8
TOC: 2 10 00  9 00:00:00 00 45:13:70   <-- Track #9
TOC: 2 10 00  a 00:00:00 00 49:50:06   <-- Track #10
TOC: 2 10 00  b 00:00:00 00 54:26:17   <-- Track #11
*/

//Should do something like so:
//			data = GetSessionInfo(cdCmd & 0xFF, cdPtr);
			data = CDIntfGetSessionInfo(cdCmd & 0xFF, cdPtr);
			if (data == 0xFF)	// Failed...
			{
				data = 0x0400;
				WriteLog("CDROM: Requested invalid session #%u (or failed to load TOC, or bad cdPtr value)\n", cdCmd & 0xFF);
			}
			else
			{
				data |= (0x20 | cdPtr++) << 8;
				WriteLog("CDROM: Reading DS_DATA (session #%u TOC byte #%u): $%04X\n", cdCmd & 0xFF, cdPtr, data);
			}

/*			bool isValidSession = ((cdCmd & 0xFF) == 0 ? true : false);//Hardcoded... !!! FIX !!!
//NOTE: This should return error condition if the requested session doesn't exist! ($0400?)
			if (isValidSession)
			{
				cdPtr++;
				switch (cdPtr)
				{
				case 1:
					data = 0x2001;	// Min track for this session?
					break;
				case 2:
					data = 0x210A;	// Max track for this session?
					break;
				case 3:
					data = 0x2219;	// Max lead-out time, absolute minutes
					break;
				case 4:
					data = 0x2319;	// Max lead-out time, absolute seconds
					break;
				case 5:
					data = 0x2419;	// Max lead-out time, absolute frames
					break;
				default:
					data = 0xFFFF;

//;    +0 - unused, reserved (0)
//;    +1 - unused, reserved (0)
//;    +2 - minimum track number
//;    +3 - maximum track number
//;    +4 - total number of sessions
//;    +5 - start of last lead-out time, absolute minutes
//;    +6 - start of last lead-out time, absolute seconds
//;    +7 - start of last lead-out time, absolute frames

				}
				WriteLog("CDROM: Reading DS_DATA (session #%u TOC byte #%u): $%04X\n", cdCmd & 0xFF, cdPtr, data);
			}
			else
			{
				data = 0x0400;
				WriteLog("CDROM: Requested invalid session #%u\n", cdCmd & 0xFF);
			}*/
		}
		// Seek to m, s, or f position
		else if ((cdCmd & 0xFF00) == 0x1000 || (cdCmd & 0xFF00) == 0x1100 || (cdCmd & 0xFF00) == 0x1200)
			data = 0x0100;	// Success, though this doesn't take error handling into account.
			// Ideally, we would also set the bits in BUTCH to let the processor know that
			// this is ready to be read... !!! FIX !!!
		else if ((cdCmd & 0xFF00) == 0x1400)		// Read "full" session TOC
		{
//Need to be a bit more tricky here, since it's reading the "session" TOC instead of the
//full TOC--so we need to check for the min/max tracks for each session here... [DONE]

			if (trackNum > maxTrack)
			{
				data = 0x400;
WriteLog("CDROM: Requested invalid track #%u for session #%u\n", trackNum, cdCmd & 0xFF);
			}
			else
			{
				if (cdPtr < 0x62)
					data = (cdPtr << 8) | trackNum;
				else if (cdPtr < 0x65)
					data = (cdPtr << 8) | CDIntfGetTrackInfo(trackNum, (cdPtr - 2) & 0x0F);

WriteLog("CDROM: Reading DS_DATA (session #%u, full TOC byte #%u): $%04X\n", cdCmd & 0xFF, (cdPtr+1) & 0x0F, data);

				cdPtr++;
				if (cdPtr == 0x65)
					cdPtr = 0x60, trackNum++;
			}

			// Note that it seems to return track info in sets of 4 (or is it 5?)
/*
;    +0 - track # (must be non-zero)
;    +1 - absolute minutes (0..99), start of track
;    +2 - absolute seconds (0..59), start of track
;    +3 - absolute frames, (0..74), start of track
;    +4 - session # (0..99)
;    +5 - track duration minutes
;    +6 - track duration seconds
;    +7 - track duration frames
*/
			// Seems to be the following format: $60xx -> Track #xx
			//                                   $61xx -> min?   (trk?)
			//                                   $62xx -> sec?   (min?)
			//                                   $63xx -> frame? (sec?)
			//                                   $64xx -> ?      (frame?)
/*			cdPtr++;
			switch (cdPtr)
			{
			case 1:
				data = 0x6000 | trackNum;	// Track #
				break;
			case 2:
				data = 0x6100 | trackNum;	// Track # (again?)
				break;
			case 3:
				data = 0x6200 | minutes[trackNum];	// Minutes
				break;
			case 4:
				data = 0x6300 | seconds[trackNum];	// Seconds
				break;
			case 5:
				data = 0x6400 | frames[trackNum];		// Frames
				trackNum++;
				cdPtr = 0;
			}//*/
		}
		else if ((cdCmd & 0xFF00) == 0x1500)		// Read CD mode
		{
			data = cdCmd | 0x0200;	// ?? not sure ?? [Seems OK]
			WriteLog("CDROM: Reading DS_DATA (mode), cdCmd=$%04X\n", cdCmd);
		}
		else if ((cdCmd & 0xFF00) == 0x1800)		// Spin up session #
		{
			data = cdCmd;
			WriteLog("CDROM: Reading DS_DATA (spin up session), cdCmd=$%04X\n", cdCmd);
		}
		else if ((cdCmd & 0xFF00) == 0x5400)		// Read # of sessions
		{
			data = cdCmd | 0x00;	// !!! Hardcoded !!! FIX !!!
			WriteLog("CDROM: Reading DS_DATA (# of sessions), cdCmd=$%04X\n", cdCmd);
		}
		else if ((cdCmd & 0xFF00) == 0x7000)		// Read oversampling
		{
//NOTE: This setting will probably affect the # of DSP interrupts that need to happen. !!! FIX !!!
			data = cdCmd;
			WriteLog("CDROM: Reading DS_DATA (oversampling), cdCmd=$%04X\n", cdCmd);
		}
		else
		{
			data = 0x0400;
			WriteLog("CDROM: Reading DS_DATA, unhandled cdCmd=$%04X\n", cdCmd);
		}
	}
	else if (offset == DS_DATA && !haveCDGoodness)
		data = 0x0400;								// No CD interface present, so return error
	else if (offset >= FIFO_DATA && offset <= FIFO_DATA + 3)
	{
	}
	else if (offset >= FIFO_DATA + 4 && offset <= FIFO_DATA + 7)
	{
	}
	else
		data = GET16(cdRam, offset);

//Returning $00000008 seems to cause it to use the starfield. Dunno why.
// It looks like it's getting the CD_mode this way...
//Temp, for testing...
//Very interesting...! Seems to control sumthin' or other...
/*if (offset == 0x2C || offset == 0x2E)
	data = 0xFFFF;//*/
/*if (offset == 0x2C)
	data = 0x0000;
if (offset == 0x2E)
	data = 0;//0x0008;//*/
	if (offset == UNKNOWN + 2)
		data = CDROMBusRead();

#ifdef CDROM_LOG
	if ((offset & 0xFF) < 11 * 4)
		WriteLog("[%s] ", BReg[(offset & 0xFF) / 4]);
	if (offset != UNKNOWN && offset != UNKNOWN + 2)
		WriteLog("CDROM: %s reading word $%04X from $%08X [68K PC=$%08X]\n", whoName[who], data, offset, m68k_get_reg(NULL, M68K_REG_PC));
#endif
	return data;
}

void CDROMWriteByte(uint32_t offset, uint8_t data, uint32_t who/*=UNKNOWN*/)
{
	offset &= 0xFF;
	cdRam[offset] = data;

#ifdef CDROM_LOG
	if ((offset & 0xFF) < 12 * 4)
		WriteLog("[%s] ", BReg[(offset & 0xFF) / 4]);
	WriteLog("CDROM: %s writing byte $%02X at $%08X [68K PC=$%08X]\n", whoName[who], data, offset, m68k_get_reg(NULL, M68K_REG_PC));
#endif
}

void CDROMWriteWord(uint32_t offset, uint16_t data, uint32_t who/*=UNKNOWN*/)
{
	offset &= 0xFF;
	SET16(cdRam, offset, data);

	// Command register
//Lesse what this does... Seems to work OK...!
	if (offset == DS_DATA)
	{
		cdCmd = data;
		if ((data & 0xFF00) == 0x0200)				// Stop CD
		{
			cdPtr = 0;
			WriteLog("CDROM: Stopping CD\n", data & 0xFF);
		}
		else if ((data & 0xFF00) == 0x0300)			// Read session TOC (short? overview?)
		{
			cdPtr = 0;
			WriteLog("CDROM: Reading TOC for session #%u\n", data & 0xFF);
		}
//Not sure how these three acknowledge...
		else if ((data & 0xFF00) == 0x1000)			// Seek to minute position
		{
			min = data & 0x00FF;
		}
		else if ((data & 0xFF00) == 0x1100)			// Seek to second position
		{
			sec = data & 0x00FF;
		}
		else if ((data & 0xFF00) == 0x1200)			// Seek to frame position
		{
			frm = data & 0x00FF;
			block = (((min * 60) + sec) * 75) + frm;
			cdBufPtr = 2352;						// Ensure that SSI read will do so immediately
			WriteLog("CDROM: Seeking to %u:%02u:%02u [block #%u]\n", min, sec, frm, block);
		}
		else if ((data & 0xFF00) == 0x1400)			// Read "full" TOC for session
		{
			cdPtr = 0x60,
			minTrack = CDIntfGetSessionInfo(data & 0xFF, 0),
			maxTrack = CDIntfGetSessionInfo(data & 0xFF, 1);
			trackNum = minTrack;
			WriteLog("CDROM: Reading \"full\" TOC for session #%u (min=%u, max=%u)\n", data & 0xFF, minTrack, maxTrack);
		}
		else if ((data & 0xFF00) == 0x1500)			// Set CDROM mode
		{
			// Mode setting is as follows: bit 0 set -> single speed, bit 1 set -> double,
			// bit 3 set -> multisession CD, bit 3 unset -> audio CD
			WriteLog("CDROM: Setting mode $%02X\n", data & 0xFF);
		}
		else if ((data & 0xFF00) == 0x1800)			// Spin up session #
		{
			WriteLog("CDROM: Spinning up session #%u\n", data & 0xFF);
		}
		else if ((data & 0xFF00) == 0x5400)			// Read # of sessions
		{
			WriteLog("CDROM: Reading # of sessions\n", data & 0xFF);
		}
		else if ((data & 0xFF00) == 0x7000)			// Set oversampling rate
		{
			// 1 = none, 2 = 2x, 3 = 4x, 4 = 8x
			uint32_t rates[5] = { 0, 1, 2, 4, 8 };
			WriteLog("CDROM: Setting oversample rate to %uX\n", rates[(data & 0xFF)]);
		}
		else
			WriteLog("CDROM: Unknown command $%04X\n", data);
	}//*/

	if (offset == UNKNOWN + 2)
		CDROMBusWrite(data);

#ifdef CDROM_LOG
	if ((offset & 0xFF) < 11 * 4)
		WriteLog("[%s] ", BReg[(offset & 0xFF) / 4]);
	if (offset != UNKNOWN && offset != UNKNOWN + 2)
		WriteLog("CDROM: %s writing word $%04X at $%08X [68K PC=$%08X]\n", whoName[who], data, offset, m68k_get_reg(NULL, M68K_REG_PC));
#endif
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
//This is kinda lame. What we should do is check for a 0->1 transition on either bits 0 or 1...
//!!! FIX !!!

#ifdef CDROM_LOG
	if (data & 0xFFF0)
		WriteLog("CDROM: BusWrite write on unknown line: $%04X\n", data);
#endif

	switch (currentState)
	{
	case ST_INIT:
		currentState = ST_RISING;
		break;
	case ST_RISING:
		if (data & 0x0001)							// Command coming
		{
			cmdTx = true;
			counter = 0;
			busCmd = 0;
		}
		else
		{
			if (cmdTx)
			{
				busCmd <<= 1;						// Make room for next bit
				busCmd |= (data & 0x04);			// & put it in
				counter++;

				if (counter == 9)
				{
					busCmd >>= 2;					// Because we ORed bit 2, we need to shift right by 2
					cmdTx = false;

//What it looks like:
//It seems that the $18x series reads from NVRAM while the
//$130, $14x, $100 series writes values to NVRAM...
					if (busCmd == 0x180)
						rxData = 0x0024;//1234;
					else if (busCmd == 0x181)
						rxData = 0x0004;//5678;
					else if (busCmd == 0x182)
						rxData = 0x0071;//9ABC;
					else if (busCmd == 0x183)
						rxData = 0xFF67;//DEF0;
					else if (busCmd == 0x184)
						rxData = 0xFFFF;//892F;
					else if (busCmd == 0x185)
						rxData = 0xFFFF;//8000;
					else
						rxData = 0x0001;
//						rxData = 0x8349;//8000;//0F67;

					counter = 0;
					firstTime = true;
					txData = 0;
#ifdef CDROM_LOG
					WriteLog("CDROM: *** BusWrite got command $%04X\n", busCmd);
#endif
				}
			}
			else
			{
				txData = (txData << 1) | ((data & 0x04) >> 2);
//WriteLog("[%s]", data & 0x04 ? "1" : "0");

				rxDataBit = (rxData & 0x8000) >> 12;
				rxData <<= 1;
				counter++;
#ifdef CDROM_LOG
				if (counter == 16)
					WriteLog("CDROM: *** BusWrite got extra command $%04X\n", txData);
#endif
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
// It seems the counter == 0 simply waits for a single bit acknowledge-- !!! FIX !!!
// Or does it? Hmm. It still "pumps" 16 bits through above, so how is this special?
// Seems to be because it sits and looks at it as if it will change. Dunno!
#ifdef CDROM_LOG
	if ((counter & 0x0F) == 0)
	{
		if (counter == 0 && rxDataBit == 0)
		{
			if (firstTime)
			{
				firstTime = false;
				WriteLog("0...\n");
			}
		}
		else
			WriteLog("%s\n", rxDataBit ? "1" : "0");
	}
	else
		WriteLog("%s", rxDataBit ? "1" : "0");
#endif

	return rxDataBit;
}

//
// This simulates a read from BUTCH over the SSI to JERRY. Uses real reading!
//
//temp, until I can fix my CD image... Argh!
static uint8_t cdBuf2[2532 + 96], cdBuf3[2532 + 96];
uint16_t GetWordFromButchSSI(uint32_t offset, uint32_t who/*= UNKNOWN*/)
{
	bool go = ((offset & 0x0F) == 0x0A || (offset & 0x0F) == 0x0E ? true : false);

	if (!go)
		return 0x000;

// The problem comes in here. Really, we should generate the IRQ once we've stuffed
// our values into the DAC L/RRXD ports...
// But then again, the whole IRQ system needs an overhaul in order to make it more
// cycle accurate WRT to the various CPUs. Right now, it's catch-as-catch-can, which
// means that IRQs get serviced on scanline boundaries instead of when they occur.
	cdBufPtr += 2;

	if (cdBufPtr >= 2352)
	{
WriteLog("CDROM: %s reading block #%u...\n", whoName[who], block);
		//No error checking. !!! FIX !!!
//NOTE: We have to subtract out the 1st track start as well (in cdintf_foo.cpp)!
//		CDIntfReadBlock(block - 150, cdBuf);

//Crappy kludge for shitty shit. Lesse if it works!
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
		cdBuf[2351] = cdBuf2[2351];//*/

		block++, cdBufPtr = 0;
	}

/*extern bool doDSPDis;
if (block == 244968)
	doDSPDis = true;//*/

WriteLog("[%04X:%01X]", GET16(cdBuf, cdBufPtr), offset & 0x0F);
if (cdBufPtr % 32 == 30)
	WriteLog("\n");

//	return GET16(cdBuf, cdBufPtr);
//This probably isn't endian safe...
// But then again... It seems that even though the data on the CD is organized as
// LL LH RL RH the way it expects to see the data is RH RL LH LL.
// D'oh! It doesn't matter *how* the data comes in, since it puts each sample into
// its own left or right side queue, i.e. it reads them 32 bits at a time and puts
// them into their L/R channel queues. It does seem, though, that it expects the
// right channel to be the upper 16 bits and the left to be the lower 16.
	return (cdBuf[cdBufPtr + 1] << 8) | cdBuf[cdBufPtr + 0];
}

bool ButchIsReadyToSend(void)
{
#ifdef LOG_CDROM_VERBOSE
WriteLog("Butch is%s ready to send...\n", cdRam[I2CNTRL + 3] & 0x02 ? "" : " not");
#endif
	return (cdRam[I2CNTRL + 3] & 0x02 ? true : false);
}

//
// This simulates a read from BUTCH over the SSI to JERRY. Uses real reading!
//
void SetSSIWordsXmittedFromButch(void)
{

// The problem comes in here. Really, we should generate the IRQ once we've stuffed
// our values into the DAC L/RRXD ports...
// But then again, the whole IRQ system needs an overhaul in order to make it more
// cycle accurate WRT to the various CPUs. Right now, it's catch-as-catch-can, which
// means that IRQs get serviced on scanline boundaries instead of when they occur.

// NOTE: The CD BIOS uses the following SMODE:
//       DAC: M68K writing to SMODE. Bits: WSEN FALLING  [68K PC=00050D8C]
	cdBufPtr += 4;

	if (cdBufPtr >= 2352)
	{
WriteLog("CDROM: Reading block #%u...\n", block);
		//No error checking. !!! FIX !!!
//NOTE: We have to subtract out the 1st track start as well (in cdintf_foo.cpp)!
//		CDIntfReadBlock(block - 150, cdBuf);

//Crappy kludge for shitty shit. Lesse if it works!
//It does! That means my CD is WRONG! FUCK!

// But, then again, according to Belboz at AA the two zeroes in front *ARE* necessary...
// So that means my CD is OK, just this method is wrong!
// It all depends on whether or not the interrupt occurs on the RISING or FALLING edge
// of the word strobe... !!! FIX !!!

// When WS rises, left channel was done transmitting. When WS falls, right channel is done.
//		CDIntfReadBlock(block - 150, cdBuf2);
//		CDIntfReadBlock(block - 149, cdBuf3);
		CDIntfReadBlock(block, cdBuf2);
		CDIntfReadBlock(block + 1, cdBuf3);
		memcpy(cdBuf, cdBuf2 + 2, 2350);
		cdBuf[2350] = cdBuf3[0];
		cdBuf[2351] = cdBuf3[1];//*/

		block++, cdBufPtr = 0;

/*extern bool doDSPDis;
static int foo = 0;
if (block == 244968)
{
	foo++;
WriteLog("\n***** foo = %u, block = %u *****\n\n", foo, block);
	if (foo == 2)
		doDSPDis = true;
}//*/
	}


WriteLog("[%02X%02X %02X%02X]", cdBuf[cdBufPtr+1], cdBuf[cdBufPtr+0], cdBuf[cdBufPtr+3], cdBuf[cdBufPtr+2]);
if (cdBufPtr % 32 == 28)
	WriteLog("\n");

//This probably isn't endian safe...
// But then again... It seems that even though the data on the CD is organized as
// LL LH RL RH the way it expects to see the data is RH RL LH LL.
// D'oh! It doesn't matter *how* the data comes in, since it puts each sample into
// its own left or right side queue, i.e. it reads them 32 bits at a time and puts
// them into their L/R channel queues. It does seem, though, that it expects the
// right channel to be the upper 16 bits and the left to be the lower 16.

// This behavior is strictly a function of *where* the WS creates an IRQ. If the data
// is shifted by two zeroes (00 00 in front of the data file) then this *is* the
// correct behavior, since the left channel will be xmitted followed by the right

// Now we have definitive proof: The MYST CD shows a word offset. So that means we have
// to figure out how to make that work here *without* having to load 2 sectors, offset, etc.
// !!! FIX !!!
	lrxd = (cdBuf[cdBufPtr + 3] << 8) | cdBuf[cdBufPtr + 2],
	rrxd = (cdBuf[cdBufPtr + 1] << 8) | cdBuf[cdBufPtr + 0];
}

/*
[18667]
TOC for MYST

CDINTF: Disc summary
        # of sessions: 2, # of tracks: 10
        Session info:
        1: min track= 1, max track= 1, lead out= 1:36:67
        2: min track= 2, max track=10, lead out=55:24:71
        Track info:
         1: start= 0:02:00
         2: start= 4:08:67
         3: start= 4:16:65
         4: start= 4:29:19
         5: start=29:31:03
         6: start=33:38:50
         7: start=41:38:60
         8: start=44:52:18
         9: start=51:51:22
        10: start=55:18:73

CDROM: Read sector 18517 (18667 - 150)...

0000: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0018: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0030: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0048: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0060: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0078: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0090: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00A8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00C0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00D8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
00F0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0108: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0120: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0138: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0150: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0168: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0180: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0198: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
01B0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
01C8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
01E0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
01F8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0210: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0228: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0240: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0258: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0270: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0288: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
02A0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
02B8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
02D0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
02E8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0300: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0318: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0330: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0348: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0360: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0378: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0390: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
03A8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
03C0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
03D8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
03F0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0408: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0420: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0438: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0450: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0468: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0480: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0498: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
04B0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
04C8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
04E0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
04F8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0510: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0528: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0540: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0558: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0570: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0588: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
05A0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
05B8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
05D0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
05E8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0600: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0618: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0630: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0648: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0660: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0678: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0690: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
06A8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
06C0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
06D8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
06F0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0708: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0720: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0738: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0750: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0768: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0780: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
0798: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
07B0: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00
07C8: 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00[54 41 49 52]54 41
07E0: 49 52 54 41 49 52 54 41 49 52 54 41 49 52 54 41 49 52 54 41 49 52 54 41
07F8: 49 52 54 41 49 52 54 41 49 52 54 41 49 52 54 41 49 52 54 41 49 52 54 41
0810: 49 52 54 41 49 52[54 41 49 52]54 41 52 41 20 49 50 41 52 50 56 4F 44 45
0828: 44 20 54 41 20 41 45 48 44 41 52 45 41 20 52 54 20 49[00 00 00 50]01 00
0840: 80 83 FC 23 07 00 07 00 F0 00 0C 21 FC 23 07 00 07 00 F1 00 0C A1 FC 33
0858: FF FF F0 00 4E 00 7C 2E 1F 00 FC FF 00 61 08 00 F9 4E 00 00 00 51 E7 48
0870: 00 FE 39 30 F1 00 02 40 40 02 10 00 00 67 1C 00 79 42 01 00 8C D3 3C 34
0888: 37 03 3C 30 81 05 3C 3C 0A 01 3C 38 F1 00 00 60 1A 00 FC 33 01 00 01 00
08A0: 8C D3 3C 34 4B 03 3C 30 65 05 3C 3C 42 01 3C 38 1F 01 C0 33 01 00 88 D3
08B8: C4 33 01 00 8A D3 00 32 41 E2 41 94 7C D4 04 00 7C 92 01 00 41 00 00 04
08D0: C1 33 01 00 82 D3 C1 33 F0 00 3C 00 C2 33 01 00 80 D3 C2 33 F0 00 38 00
08E8: C2 33 F0 00 3A 00 06 3A 44 9A C5 33 01 00 84 D3 44 DC C6 33 01 00 86 D3
0900: F9 33 01 00 84 D3 F0 00 46 00 FC 33 FF FF F0 00 48 00 FC 23 00 00 00 00
0918: F0 00 2A 00 FC 33 00 00 F0 00 58 00 DF 4C 7F 00 75 4E 00 00 00 00 00 00

Raw P-W subchannel data:

00: 80 80 C0 80 80 80 80 C0 80 80 80 80 80 80 C0 80
10: 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80
20: 80 80 80 80 80 80 80 80 80 80 80 80 80 80 80 C0
30: 80 80 80 80 80 80 80 80 80 80 80 80 80 C0 80 80
40: 80 80 80 80 C0 80 80 80 80 C0 C0 80 80 C0 C0 80
50: C0 80 80 C0 C0 C0 80 80 C0 80 80 80 C0 80 80 80

P subchannel data: FF FF FF FF FF FF FF FF FF FF FF FF
Q subchannel data: 21 02 00 00 00 01 00 04 08 66 9C 88

Run address: $5000, Length: $18380
*/


/*
CD_read function from the CD BIOS: Note that it seems to direct the EXT1 interrupt
to the GPU--so that would mean *any* interrupt that BUTCH generates would be routed
to the GPU...

read:
		btst.l	#31,d0
		bne.w	.play
		subq.l	#4,a0		; Make up for ISR pre-increment
		move.l	d0,-(sp)
		move.l	BUTCH,d0
		and.l	#$ffff0000,d0
		move.l	d0,BUTCH	; NO INTERRUPTS!!!!!!!!!!!
		move.l	(sp)+,d0
;		move.l	#0,BUTCH

		move.w	#$101,J_INT

		move.l	d1,-(sp)
		move.l	I2CNTRL,d1	;Read I2S Control Register
		bclr	#2,d1		; Stop data
		move.l	d1,I2CNTRL
		move.l	(sp)+,d1

		move.l	PTRLOC,a2
		move.l	a0,(a2)+
		move.l	a1,(a2)+
		move.l	#0,(a2)+

		btst.b	#7,INITTYPE
		beq	.not_bad
		move.l	PTRLOC,a0
		asl.l	#5,d2

		move.l	d2,-(sp)

		or.l	#$089a3c1a,d2		; These instructions include the bclr
		move.l	d2,188(a0)

		move.l	(sp)+,d2

		swap	d2
		or.l	#$3c1a1838,d2		; These instructions include the bclr
		move.l	d2,196(a0)

		move.l	#16,(a2)+
		move.l	d1,(a2)

.not_bad:

		move.w	DS_DATA,d1			; Clear any pending DSARX states
		move.l	I2CNTRL,d1			; Clear any pending errors

; Drain the FIFO so that we don't get overloaded

.dump:
		move.l	FIFO_DATA,d1
		move.l	I2CNTRL,d1
		btst	#4,d1
		bne.b	.dump

.butch_go:
		move.l	BUTCH,d1
		and.l	#$FFFF0000,d1
		or.l	#%000100001,d1			 ;Enable DSARX interrupt
		move.l	d1,BUTCH
;		move.l	#%000100001,BUTCH		 ;Enable DSARX interrupt

; Do a play @

.play:	move.l	d0,d1		; mess with copy in d1
		lsr.l	#8,d1		; shift the byte over
		lsr.w	#8,d1
		or.w	#$1000,d1	; format it for goto
		move.w	d1,DS_DATA	; DSA tx
        bsr.b	DSA_tx

		move.l	d0,d1		; mess with copy in d1
		lsr.w	#8,d1
		or.w	#$1100,d1	; format it for goto
		move.w	d1,DS_DATA	; DSA tx
        bsr.b	DSA_tx

		move.l	d0,d1		; mess with copy in d1
		and.w	#$00FF,d1	; mask for minutes
		or.w	#$1200,d1	; format it for goto
		move.w	d1,DS_DATA	; DSA tx
        bsr.b	DSA_tx

		rts


****************************
* Here's the GPU interrupt *
****************************

JERRY_ISR:
	movei	#G_FLAGS,r30
	load	(r30),r29		;read the flags

	movei	#BUTCH,r24

make_ptr:
	move	pc,Ptrloc
	movei	#(make_ptr-PTRPOS),TEMP
	sub	TEMP,Ptrloc

HERE:
	move	pc,r25
	movei	#(EXIT_ISR-HERE),r27
	add	r27,r25

; Is this a DSARX interrupt?

 	load	(r24),r27		;check for DSARX int pending
	btst	#13,r27
	jr	z,fifo_read			; This should ALWAYS fall thru the first time

; Set the match bit, to allow data
;	moveq	#3,r26			; enable FIFO only
; Don't just jam a value
; Clear the DSARX and set FIFO
	bclr	#5,r27
	bset	#1,r27
	store	r27,(r24)
	addq	#$10,r24
	load	(r24),r27
	bset	#2,r27
	store	r27,(r24)		; Disable SUBCODE match

; Now we clear the DSARX interrupt in Butch

	subq	#12,r24			; does what the above says
	load	(r24),r26		;Clears DSA pending interrupt
	addq	#6,r24
	loadw	(r24),r27		; Read DSA response
	btst	#10,r27			; Check for error
	jr	nz,error
	or	r26,r26
	jump	(r25)
;	nop

fifo_read:
; Check for ERROR!!!!!!!!!!!!!!!!!!!!!
	btst	#14,r27
	jr	z,noerror
	bset	#31,r27
error:
	addq	#$10,r24
	load	(r24),TEMP
	or	TEMP,TEMP
	subq	#$10,r24
	load	(Ptrloc),TEMP
	addq	#8,Ptrloc
	store	TEMP,(Ptrloc)
	subq	#8,Ptrloc
noerror:
	load	(Ptrloc),Dataptr	;get pointer

; Check to see if we should stop
	addq	#4,Ptrloc
	load	(Ptrloc),TEMP
	subq	#4,Ptrloc
	cmp	Dataptr,TEMP
	jr	pl,notend
;	nop
	bclr	#0,r27
	store	r27,(r24)

notend:
	movei	#FIFO_DATA,CDdata
	move	CDdata,r25
	addq	#4,CDdata
loptop:
	load 	(CDdata),TEMP
	load	(r25),r30
	load	(CDdata),r21
	load	(r25),r22
	load	(CDdata),r24
	load	(r25),r20
	load	(CDdata),r19
	load	(r25),r18
	addq	#4,Dataptr
	store	TEMP,(Dataptr)
	addqt	#4,Dataptr
	store	r30,(Dataptr)
	addqt	#4,Dataptr
	store	r21,(Dataptr)
	addqt	#4,Dataptr
	store	r22,(Dataptr)
	addqt	#4,Dataptr
	store	r24,(Dataptr)
	addqt	#4,Dataptr
	store	r20,(Dataptr)
	addqt	#4,Dataptr
	store	r19,(Dataptr)
	addqt	#4,Dataptr
	store	r18,(Dataptr)

	store	Dataptr,(Ptrloc)

exit_isr:
	movei	#J_INT,r24	; Acknowledge in Jerry
	moveq	#1,TEMP
	bset	#8,TEMP
	storew	TEMP,(r24)

.if FLAG
; Stack r18
	load	(r31),r18
	addq	#4,r31

; Stack r19
	load	(r31),r19
	addq	#4,r31

; Stack r20
	load	(r31),r20
	addq	#4,r31

; Stack r21
	load	(r31),r21
	addq	#4,r31

; Stack r22
	load	(r31),r22
	addq	#4,r31

; Stack r23
	load	(r31),r23
	addq	#4,r31

; Stack r26
	load	(r31),r26
	addq	#4,r31

; Stack r27
	load	(r31),r27
	addq	#4,r31

; Stack r24
	load	(r31),r24
	addq	#4,r31

; Stack r25
	load	(r31),r25
	addq	#4,r31
.endif

	movei	#G_FLAGS,r30

;r29 already has flags
	bclr	#3,r29		;IMASK
	bset	#10,r29		;Clear DSP int bit in TOM

	load	(r31),r28	;Load return address


	addq	#2,r28		;Fix it up
	addq	#4,r31
	jump	(r28)		;Return
	store	r29,(r30)	;Restore broken flags


	align long

stackbot:
	ds.l	20
STACK:


*/

