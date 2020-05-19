/*
(The MIT License)

Copyright (c) 2008-2016 by
David Etherton, Eric Anderton, Alec Bourque (Uze), Filipe Rinaldi,
Sandor Zsuga (Jubatian), Matt Pandina (Artcfox)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#pragma once

#include <vector>
#include <stdint.h>
#include <queue>
#include <cstring>

// Video: Offset of display on the emulator's surface
// Syncronized with the kernel, this value now results in the image
// being perfectly centered in both the emulator and a real TV
#define VIDEO_LEFT_EDGE 168U
// Video: Display width; the width of the emulator's output (before any
// scaling applied) and video capturing
#define VIDEO_DISP_WIDTH 720U

//Uzebox keyboard defines
#define KB_STOP 0
#define KB_TX_START 1
#define KB_TX_READY 2

#define KB_SEND_KEY 0x00
#define KB_SEND_END 0x01
#define KB_SEND_DEVICE_ID 0x02
#define KB_SEND_FIRMWARE_REV 0x03
#define KB_RESET 0x7f

// Joysticks
#define MAX_JOYSTICKS 2
#define NUM_JOYSTICK_BUTTONS 8
#define MAX_JOYSTICK_AXES 8
#define MAX_JOYSTICK_HATS 8

#define JOY_SNES_X 0
#define JOY_SNES_A 1
#define JOY_SNES_B 2
#define JOY_SNES_Y 3
#define JOY_SNES_LSH 6
#define JOY_SNES_RSH 7
#define JOY_SNES_SELECT 8
#define JOY_SNES_START 9

#define JOY_DIR_UP 1
#define JOY_DIR_RIGHT 2
#define JOY_DIR_DOWN 4
#define JOY_DIR_LEFT 8
#define JOY_DIR_COUNT 4
#define JOY_AXIS_UNUSED -1

#define JOY_MASK_UP 0x11111111
#define JOY_MASK_RIGHT 0x22222222
#define JOY_MASK_DOWN 0x44444444
#define JOY_MASK_LEFT 0x88888888

#ifndef JOY_ANALOG_DEADZONE
#define JOY_ANALOG_DEADZONE 4096
#endif

#if defined(_MSC_VER) && _MSC_VER >= 1400
// don't whine about sprintf and fopen.
// could switch to sprintf_s but that's not standard.
#pragma warning(disable : 4996)
#endif

// 644 Overview: http://www.atmel.com/dyn/resources/prod_documents/doc2593.pdf
// AVR8 insn set: http://www.atmel.com/dyn/resources/prod_documents/doc0856.pdf

enum
{
	NES_A,
	NES_B,
	PAD_SELECT,
	PAD_START,
	PAD_UP,
	PAD_DOWN,
	PAD_LEFT,
	PAD_RIGHT
};
enum
{
	SNES_B,
	SNES_Y,
	SNES_A = 8,
	SNES_X,
	SNES_LSH,
	SNES_RSH
};

#if 1 // 644P
const unsigned eepromSize = 2048;
const unsigned sramSize = 4096;
const unsigned progSize = 65536;
#else // 1284P
const unsigned eepromSize = 4096;
const unsigned sramSize = 16384;
const unsigned progSize = 131072;
#endif

#define IOBASE 32
#define SRAMBASE 256

namespace ports
{
enum
{
	PINA,
	DDRA,
	PORTA,
	PINB,
	DDRB,
	PORTB,
	PINC,
	DDRC,
	PORTC,
	PIND,
	DDRD,
	PORTD,
	res2C,
	res2D,
	res2E,
	res2F,
	res30,
	res31,
	res32,
	res33,
	res34,
	TIFR0,
	TIFR1,
	TIFR2,
	res38,
	res39,
	res3A,
	PCIFR,
	EIFR,
	EIMSK,
	GPIOR0,
	EECR,
	EEDR,
	EEARL,
	EEARH,
	GTCCR,
	TCCR0A,
	TCCR0B,
	TCNT0,
	OCR0A,
	OCR0B,
	res49,
	GPIOR1,
	GPIOR2,
	SPCR,
	SPSR,
	SPDR,
	res4f,
	ACSR,
	OCDR,
	res52,
	SMCR,
	MCUSR,
	MCUCR,
	res56,
	SPMCSR,
	res58,
	res59,
	res5A,
	res5B,
	res5C,
	SPL,
	SPH,
	SREG,
	WDTCSR,
	CLKPR,
	res62,
	res63,
	PRR,
	res65,
	OSCCAL,
	res67,
	PCICR,
	EICRA,
	res6a,
	PCMSK0,
	PCMSK1,
	PCMSK2,
	TIMSK0,
	TIMSK1,
	TIMSK2,
	res71,
	res72,
	PCMSK3,
	res74,
	res75,
	res76,
	res77,
	ADCL,
	ADCH,
	ADCSRA,
	ADCSRB,
	ADMUX,
	res7d,
	DIDR0,
	DIDR1,
	TCCR1A,
	TCCR1B,
	TCCR1C,
	res83,
	TCNT1L,
	TCNT1H,
	ICR1L,
	ICR1H,
	OCR1AL,
	OCR1AH,
	OCR1BL,
	OCR1BH,
	res8c,
	res8d,
	res8e,
	res8f,
	res90,
	res91,
	res92,
	res93,
	res94,
	res95,
	res96,
	res97,
	res98,
	res99,
	res9a,
	res9b,
	res9c,
	res9d,
	res9e,
	res9f,
	resa0,
	resa1,
	resa2,
	resa3,
	resa4,
	resa5,
	resa6,
	resa7,
	resa8,
	resa9,
	resaa,
	resab,
	resac,
	resad,
	resae,
	resaf,
	TCCR2A,
	TCCR2B,
	TCNT2,
	OCR2A,
	OCR2B,
	resb5,
	ASSR,
	resb7,
	TWBR,
	TWSR,
	TWAR,
	TWDR,
	TWCR,
	TWAMR,
	resbe,
	resbf,
	UCSR0A,
	UCSR0B,
	UCSR0C,
	resc3,
	UBRR0L,
	UBRR0H,
	UDR0,
	resc7,
	resc8,
	resc9,
	resca,
	rescb,
	rescc,
	rescd,
	resce,
	rescf,
	resd0,
	resd1,
	resd2,
	resd3,
	resd4,
	resd5,
	resd6,
	resd7,
	resd8,
	resd9,
	resda,
	resdb,
	resdc,
	resdd,
	resde,
	resdf,
	rese0,
	rese1,
	rese2,
	rese3,
	rese4,
	rese5,
	rese6,
	rese7,
	rese8,
	rese9,
	resea,
	reseb,
	resec,
	resed,
	resee,
	resef,
	resf0,
	resf1,
	resf2,
	resf3,
	resf4,
	resf5,
	resf6,
	resf7,
	resf8,
	resf9,
	resfa,
	resfb,
	resfc,
	resfd,
	resfe,
	resff
};
}

typedef uint8_t u8;
typedef int8_t s8;
typedef uint16_t u16;
typedef int16_t s16;
typedef uint32_t u32;
typedef int32_t s32;

typedef struct
{
	s16 arg2;
	u8 arg1;
	u8 opNum;
} __attribute__((packed)) instructionDecode_t;

typedef struct
{
	u8 opNum;
	char opName[32];
	u8 arg1Type;
	u8 arg1Mul;
	u8 arg1Offset;
	u8 arg1Neg;
	u8 arg2Type;
	u8 arg2Mul;
	u8 arg2Offset;
	u8 arg2Neg;
	u8 words;
	u8 clocks;
	u16 mask;
	u16 arg1Mask;
	u16 arg2Mask;
} instructionList_t;

using namespace std;

//SPI state machine states
enum
{
	SPI_IDLE_STATE,
	SPI_ARG_X_LO,
	SPI_ARG_X_HI,
	SPI_ARG_Y_LO,
	SPI_ARG_Y_HI,
	SPI_ARG_CRC,
	SPI_RESPOND_SINGLE,
	SPI_RESPOND_MULTI,
	SPI_READ_SINGLE_BLOCK,
	SPI_READ_MULTIPLE_BLOCK,
	SPI_WRITE_SINGLE,
	SPI_WRITE_SINGLE_BLOCK,
	SPI_RESPOND_R1,
	SPI_RESPOND_R1B,
	SPI_RESPOND_R2,
	SPI_RESPOND_R3,
	SPI_RESPOND_R7,
};

struct SDPartitionEntry
{
	u8 state;
	u8 startHead;
	u16 startCylinder;
	u8 type;
	u8 endHead;
	u16 endCylinder;
	u32 sectorOffset;
	u32 sectorCount;
};

struct avr8
{
	avr8() : /*Core*/
			 pc(0),
			 watchdogTimer(0), prevPortB(0), prevWDR(0),
			 dly_out(0), itd_TIFR1(0), elapsedCyclesSleep(0),
			 timer1_next(0), timer1_base(0), TCNT1(0),
			 //to align with AVR Simulator 2 since it has a bug that the first JMP
			 //at the reset vector takes only 2 cycles
			 cycleCounter(-1),

			 /*Video*/
			 video_buffer(nullptr),

			 /*Audio*/
			 enableSound(true),

			 /*Joystick*/
			 lagged(false),

			 /*Uzekeyboard*/
			 uzeKbState(0), uzeKbEnabled(false),

			 /*SPI Emulation*/
			 spiByte(0), spiClock(0), spiTransfer(0), spiState(SPI_IDLE_STATE), spiResponsePtr(0), spiResponseEnd(0)
	{
		memset(r, 0, sizeof(r));
		memset(io, 0, sizeof(io));
		memset(sram, 0, sizeof(sram));
		memset(eeprom, 0, sizeof(eeprom));
		memset(progmem, 0, progSize / 2 * sizeof(*progmem));
		memset(progmemDecoded, 0, progSize / 2 * sizeof(*progmemDecoded));
	}

	/*Core*/
	u16 progmem[progSize / 2];
	instructionDecode_t progmemDecoded[progSize / 2];
	u16 pc, currentPc;

  private:
	unsigned int cycleCounter;
	unsigned int elapsedCycles, prevCyclesCounter, elapsedCyclesSleep, lastCyclesSleep;
	unsigned int prevPortB, prevWDR;
	unsigned int watchdogTimer;
	unsigned int cycle_ctr_ins; // Used in update_hardware_ins to track elapsed cycles between calls
	// u8 eeClock; TODO: Only set at one location, never used. Maybe a never completed EEPROM timing code.
	unsigned int T16_latch;   // Latch for 16-bit timers (16 bits used)
	unsigned int TCNT1;		  // Timer 1 counter (used instead of TCNT1H:TCNT1L)
	unsigned int timer1_next; // Cycles remaining until next timer1 event
	unsigned int timer1_base; // Where the between-events timer started (to reproduce TCNT1)
	unsigned int itd_TIFR1;   // Interrupt delaying for TIFR1 (8 bits used)
	unsigned int dly_out;	 // Delayed output flags
	unsigned int dly_TCCR1B;  // Delayed Timer1 controls
	unsigned int dly_TCNT1L;  // Delayed Timer1 count (low)
	unsigned int dly_TCNT1H;  // Delayed Timer1 count (high)
  public:
	int randomSeed;
	u16 decodeArg(u16 flash, u16 argMask, u8 argNeg);
	void instructionDecode(u16 address);
	void decodeFlash(void);
	void decodeFlash(u16 address);

	struct
	{
		union {
			u8 r[32]; // Register file
			struct
			{
				u8 r0, r1, r2, r3, r4, r5, r6, r7, r8, r9, r10, r11, r12, r13, r14, r15;
				u8 r16, r17, r18, r19, r20, r21, r22, r23, r24, r25, XL, XH, YL, YH, ZL, ZH;
			};
		};
		union {
			u8 io[256]; // Direct-mapped I/O space
			struct
			{
				u8 PINA, DDRA, PORTA, PINB, DDRB, PORTB, PINC, DDRC;
				u8 PORTC, PIND, DDRD, PORTD, res2C, res2D, res2E, res2F;
				u8 res30, res31, res32, res33, res34, TIFR0, TIFR1, TIFR2;
				u8 res38, res39, res3A, PCIFR, EIFR, EIMSK, GPIOR0, EECR;
				u8 EEDR, EEARL, EEARH, GTCCR, TCCR0A, TCCR0B, TCNT0, OCR0A;
				u8 OCR0B, res49, GPIOR1, GPIOR2, SPCR, SPSR, SPDR, res4f;
				u8 ACSR, OCDR, res52, SMCR, MCUSR, MCUCR, res56, SPMCSR;
				u8 res58, res59, res5A, res5B, res5C, SPL, SPH, SREG;
				u8 WDTCSR, CLKPR, res62, res63, PRR, res65, OSCCAL, res67;
				u8 PCICR, EICRA, res6a, PCMSK0, PCMSK1, PCMSK2, TIMSK0, TIMSK1;
				u8 TIMSK2, res71, res72, PCMSK3, res74, res75, res76, res77;
				u8 ADCL, ADCH, ADCSRA, ADCSRB, ADMUX, res7d, DIDR0, DIDR1;
				u8 TCCR1A, TCCR1B, TCCR1C, res83, TCNT1L, TCNT1H, ICR1L, ICR1H;
				u8 OCR1AL, OCR1AH, OCR1BL, OCR1BH, res8c, res8d, res8e, res8f;
				u8 res90, res91, res92, res93, res94, res95, res96, res97;
				u8 res98, res99, res9a, res9b, res9c, res9d, res9e, res9f;
				u8 resa0, resa1, resa2, resa3, resa4, resa5, resa6, resa7;
				u8 resa8, resa9, resaa, resab, resac, resad, resae, resaf;
				u8 TCCR2A, TCCR2B, TCNT2, OCR2A, OCR2B, resb5, ASSR, resb7;
				u8 TWBR, TWSR, TWAR, TWDR, TWCR, TWAMR, resbe, resbf;
				u8 UCSR0A, UCSR0B, UCSR0C, resc3, UBRR0L, UBRR0H, UDR0, resc7;
				u8 resc8, resc9, resca, rescb, rescc, rescd, resce, rescf;
				u8 resd0, resd1, resd2, resd3, resd4, resd5, resd6, resd7;
				u8 resd8, resd9, resda, resdb, resdc, resdd, resde, resdf;
				u8 rese0, rese1, rese2, rese3, rese4, rese5, rese6, rese7;
				u8 rese8, rese9, resea, reseb, resec, resed, resee, resef;
				u8 resf0, resf1, resf2, resf3, resf4, resf5, resf6, resf7;
				u8 resf8, resf9, resfa, resfb, resfc, resfd, resfe, resff;
			};
		};
		u8 sram[sramSize];
	};
	u8 eeprom[eepromSize];

	int scanline_count;
	unsigned int left_edge_cycle;
	int scanline_top;
	unsigned int left_edge;
	u32 inset;
	u32 palette[256];
	u8 scanline_buf[2048]; // For collecting pixels from a single scanline
	u8 pixel_raw;		   // Raw (8 bit) input pixel

	u32 *video_buffer;

	/*Audio*/
	bool enableSound;

	/*Joystick*/
	// SNES bit order:  0 = B, Y, Select, Start, Up, Down, Left, Right, A, X, L, 11 = R
	// NES bit order: 0 = A, B, Select, Start, Up, Down, Left, 7 = Right
	u32 buttons[2], latched_buttons[2];
	bool lagged;

	/*Uzebox Keyboard*/
	u8 uzeKbState;
	u8 uzeKbDataOut;
	bool uzeKbEnabled;
	queue<u8> uzeKbScanCodeQueue;
	u8 uzeKbDataIn;
	u8 uzeKbClock;

	/*SPI Emulation*/
	u8 spiByte;
	u8 spiTransfer;
	u16 spiClock;
	u16 spiCycleWait;
	u8 spiState;
	u8 spiCommand;
	u8 spiCommandDelay;
	union {
		u32 spiArg;
		union {
			struct
			{
				u16 spiArgY;
				u16 spiArgX;
			};
			struct
			{
				u8 spiArgYlo;
				u8 spiArgYhi;
				u8 spiArgXlo;
				u8 spiArgXhi;
			};
		};
	};
	u32 spiByteCount;
	u8 spiResponseBuffer[12];
	u8 *spiResponsePtr;
	u8 *spiResponseEnd;

  private:
	void write_io(u8 addr, u8 value);
	u8 read_io(u8 addr);
	// Should not be called directly (see write_io)
	void write_io_x(u8 addr, u8 value);

	inline u8 read_progmem(u16 addr)
	{
		u16 word = progmem[addr >> 1];
		return (addr & 1) ? word >> 8 : word;
	}

	inline void write_sram(u16 addr, u8 value)
	{
		sram[(addr - SRAMBASE) & (sramSize - 1U)] = value;
	}

	void write_sram_io(u16 addr, u8 value)
	{
		if (addr >= SRAMBASE)
		{
			sram[(addr - SRAMBASE) & (sramSize - 1)] = value;
		}
		else if (addr >= IOBASE)
		{
			write_io(addr - IOBASE, value);
		}
		else
		{
			r[addr] = value; // Write a register
		}
	}

	inline u8 read_sram(u16 addr)
	{
		return sram[(addr - SRAMBASE) & (sramSize - 1U)];
	}

	u8 read_sram_io(u16 addr)
	{

		if (addr >= SRAMBASE)
		{
			return sram[(addr - SRAMBASE) & (sramSize - 1)];
		}
		else if (addr >= IOBASE)
		{
			return read_io(addr - IOBASE);
		}
		else
		{
			return r[addr]; // Read a register
		}
	}

	inline static unsigned int get_insn_size(unsigned int insn)
	{
		/* 41  LDS Rd,k (next word is rest of address)
		   82  STS k,Rr (next word is rest of address)
		   30  JMP k (next word is rest of address)
		   14  CALL k (next word is rest of address) */
		// This code is simplified by assuming upper k bits are zero on 644

		if (insn == 14 || insn == 30 || insn == 41 || insn == 82)
		{
			return 2U;
		}
		else
		{
			return 1U;
		}
	}

  public:
	bool init_gui();
	void draw_memorymap();
	void trigger_interrupt(unsigned int location);
	unsigned int exec();
	void spi_calculateClock();
	void update_hardware();
	void update_hardware_fast();
	void update_hardware_ins();
	void update_spi();
	u8 SDReadByte();
	void SDWriteByte(u8 value);
	void SDCommit();
	void LoadEEPROMFile(const char *filename);
	void shutdown(int errcode);
	void idle(void);
};

// undefine the following to disable SPI debug messages
#ifdef USE_SPI_DEBUG
#define SPI_DEBUG(...) printf(__VA_ARGS__)
#else
#define SPI_DEBUG(...)
#endif

#ifdef USE_EEPROM_DEBUG
#define EEPROM_DEBUG(...) printf(__VA_ARGS__)
#else
#define EEPROM_DEBUG(...)
#endif
