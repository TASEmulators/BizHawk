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

/*
Revision Log
------------
7/8/2013 V1.16 Added emulation for Timer1 Overflow interrupt

More info at uzebox.org

*/

#include <algorithm>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <iostream>
#include <queue>

#include "avr8.h"

using namespace std;

#define X ((XL) | (XH << 8))
#define DEC_X (XL-- || XH--)
#define INC_X (++XL || ++XH)

#define Y ((YL) | (YH << 8))
#define DEC_Y (YL-- || YH--)
#define INC_Y (++YL || ++YH)

#define Z ((ZL) | (ZH << 8))
#define DEC_Z (ZL-- || ZH--)
#define INC_Z (++ZL || ++ZH)

#define SP (SPL | (SPH << 8))
#define DEC_SP (SPL-- || SPH--)
#define INC_SP (++SPL || ++SPH)

#define SREG_I 7
#define SREG_T 6
#define SREG_H 5
#define SREG_S 4 // == N ^ V
#define SREG_V 3
#define SREG_N 2
#define SREG_Z 1
#define SREG_C 0

#define EEPM1 0x20
#define EEPM0 0x10
#define EERIE 0x08
#define EEMPE 0x04
#define EEPE 0x02
#define EERE 0x01

//Interrupts vector adresses
#define INT_RESET 0x00
#define WDT 0x10
#define TIMER1_COMPA 0x1A
#define TIMER1_COMPB 0x1C
#define TIMER1_OVF 0x1E
#define SPI_STC 0x26

#define REG_TCNT1L 0x84

//TIFR1 flags
#define TOV1 1
#define OCF1A 2
#define OCF1B 4

//TIMSK1 flags
#define TOIE1 1
#define OCIE1A 2
#define OCIE1B 4
#define ICIE1 32

//TCCR1B flags
#define CS10 1
#define WGM12 8

//Watchdog flags
#define WDE 8
#define WDIE 64
#define WDIF 128

#define DELAY16MS 457142	  //in cpu cycles
#define HSYNC_HALF_PERIOD 910 //in cpu cycles
#define HSYNC_PERIOD 1820	 //in cpu cycles

#define SD_ENABLED() false

/*
#define D3	((insn >> 4) & 7)
#define R3	(insn & 7)
#define D4	((insn >> 4) & 15)
#define R4	(insn & 15)
#define R5	((insn & 15) | ((insn >> 5) & 0x10))
#define D5	((insn >> 4) & 31)
#define K8	(((insn >> 4) & 0xF0) | (insn & 0xF))
#define k7	((s16)(insn<<6)>>9)
#define k12	((s16)(insn<<4)>>4)
*/

#define BIT(x, b) (((x) >> (b)) & 1)
#define C BIT(SREG, SREG_C)

// Delayed output flags. If either is set, there is a delayed out waiting to
// be written on the end of update_hardware, so the next cycle it can have
// effect. The "dly_out" variable contains these flags, and the "dly_xxx"
// variables the values to be written out.
#define DLY_TCCR1B 0x0001U
#define DLY_TCNT1 0x0002U

// Masks for SREG bits, use to combine them
#define SREG_IM (1U << SREG_I)
#define SREG_TM (1U << SREG_T)
#define SREG_HM (1U << SREG_H)
#define SREG_SM (1U << SREG_S)
#define SREG_VM (1U << SREG_V)
#define SREG_NM (1U << SREG_N)
#define SREG_ZM (1U << SREG_Z)
#define SREG_CM (1U << SREG_C)

// Clears bits. Use this on the bits which should change processing
// the given instruction.
inline static void clr_bits(u8 &dest, unsigned int bits)
{
	dest = dest & (~bits);
}
// Inverse set bit: Sets if value is zero. Mostly for Z flag
inline static void set_bit_inv(u8 &dest, unsigned int bit, unsigned int value)
{
	// Assume at most 16 bits input on value, makes it 0 or 1, the latter
	// if the input was nonzero. The "& 1U" part is usually thrown away by
	// the compiler (32 bits). The "& 0xFFFFU" part might also be thrown
	// away depending on the input.
	value = ((value & 0xFFFFU) - 1U) >> 31;
	dest = dest | ((value & 1U) << bit);
}
// Set bit using only the lowest bit of 'value': if 1, sets the bit.
inline static void set_bit_1(u8 &dest, unsigned int bit, unsigned int value)
{
	// The "& 1" on 'value' might be thrown away for suitable input.
	dest = dest | ((value & 1U) << bit);
}
// Store bit (either set or clear) using only the lowest bit of 'value'.
inline static void store_bit_1(u8 &dest, unsigned int bit, unsigned int value)
{
	// The "& 1" on 'value' might be thrown away for suitable input
	// If 'bit' is constant (inlining), it folds up well on optimizing.
	dest = dest & (~(1U << bit));
	dest = dest | ((0U - (value & 1U)) & (1U << bit));
}

// This computes both the half-carry (bit3) and full carry (bit7)
#define BORROWS (~Rd & Rr) | (Rr & R) | (R & ~Rd)
#define CARRIES ((Rd & Rr) | (Rr & ~R) | (~R & Rd))

#define UPDATE_HC_SUB                           \
	CH = BORROWS;                               \
	set_bit_1(SREG, SREG_H, (CH & 0x08U) >> 3); \
	set_bit_1(SREG, SREG_C, (CH & 0x80U) >> 7);
#define UPDATE_HC_ADD                           \
	CH = CARRIES;                               \
	set_bit_1(SREG, SREG_H, (CH & 0x08U) >> 3); \
	set_bit_1(SREG, SREG_C, (CH & 0x80U) >> 7);

#define UPDATE_H set_bit_1(SREG, SREG_H, (CARRIES & 0x8) >> 3)
#define UPDATE_Z set_bit_inv(SREG, SREG_Z, R)
#define UPDATE_V_ADD set_bit_1(SREG, SREG_V, (((Rd & Rr & ~R) | (~Rd & ~Rr & R)) & 0x80) >> 7)
#define UPDATE_V_SUB set_bit_1(SREG, SREG_V, (((Rd & ~Rr & ~R) | (~Rd & Rr & R)) & 0x80) >> 7)
#define UPDATE_N set_bit_1(SREG, SREG_N, (R & 0x80) >> 7)
#define UPDATE_S set_bit_1(SREG, SREG_S, BIT(SREG, SREG_N) ^ BIT(SREG, SREG_V))

#define UPDATE_SVN_SUB \
	UPDATE_V_SUB;      \
	UPDATE_N;          \
	UPDATE_S
#define UPDATE_SVN_ADD \
	UPDATE_V_ADD;      \
	UPDATE_N;          \
	UPDATE_S

// Simplified version for logical insns.
// sreg_clr on S, V, and N should be called before this.
// If 7th bit of R is set:
//     Sets N, sets S, clears V.
// If 7th bit of R is clear:
//     Clears N, clears S, clears V.
#define UPDATE_SVN_LOGICAL \
	SREG |= ((0x7FU - (unsigned int)(R)) >> 8) & (SREG_SM | SREG_NM);

#define UPDATE_CZ_MUL(x)                         \
	set_bit_1(SREG, SREG_C, (x & 0x8000) >> 15); \
	set_bit_inv(SREG, SREG_Z, x)

// UPDATE_CLEAR_Z: Updates Z flag by clearing if result is nonzero. This
// should be used if the previous Z flag state is meant to be preserved (such
// as in CPC), so don't include Z in a clr_bits then.
#define UPDATE_CLEAR_Z (SREG &= ~(((0U - (unsigned int)(R)) >> 8) & SREG_ZM))

#define SET_C (SREG |= (1 << SREG_C))

#define ILLEGAL_OP                                              \
	fprintf(stderr, "invalid insn at address %x\n", currentPc); \
	shutdown(1);

#if defined(_DEBUG)
#define DISASM 1
#define DIS(fmt, ...)                     \
	sprintf(insnBuf, fmt, ##__VA_ARGS__); \
	if (disasmOnly)                       \
	break
#else
#define DISASM 0
#define DIS(fmt, ...)
#endif

void avr8::spi_calculateClock()
{
	// calculate the number of cycles before the write completes
	u16 spiClockDivider;
	switch (SPCR & 0x03)
	{
	case 0:
		spiClockDivider = 4;
		break;
	case 1:
		spiClockDivider = 16;
		break;
	case 2:
		spiClockDivider = 64;
		break;
	case 3:
		spiClockDivider = 128;
		break;
	}
	if (SPSR & 0x01)
	{
		spiClockDivider = spiClockDivider >> 1; // double the speed
	}
	spiCycleWait = spiClockDivider * 8;
	SPI_DEBUG("SPI divider set to : %d (%d cycles per byte)\n", spiClockDivider, spiCycleWait);
}

// Renders a line into a 32 bit output buffer.
// Performs a shrink by 2
static inline void render_line(u32 *dest, u8 const *src, unsigned int spos, u32 const *pal)
{
	for (unsigned int i = 0; i < VIDEO_DISP_WIDTH; i++)
		dest[i] = pal[src[((i << 1) + spos) & 0x7FFU]];
}

inline void avr8::write_io(u8 addr, u8 value)
{
	// Pixel output ideally should inline, it is performed about 2 - 3
	// million times per second in a Uzebox game.
	if (addr == ports::PORTC)
	{
		pixel_raw = value & DDRC;
	}
	else
	{
		write_io_x(addr, value);
	}
}

extern void SampleCallback(uint8_t val);

// Should not be called directly, use write_io instead (pixel output!)
void avr8::write_io_x(u8 addr, u8 value)
{
	u8 changed;
	u8 went_low;

	switch (addr)
	{
	case (ports::OCR2A):
		if (enableSound && TCCR2B)
		{
			SampleCallback(value);
		}
		break;

	case (ports::PORTD):
		// write value with respect to DDRD register
		io[addr] = value & DDRD;
		break;

	case (ports::PORTB):
		if (value & 1)
		{
			elapsedCycles = cycleCounter - prevCyclesCounter;

			if (scanline_count == -999 && elapsedCycles >= HSYNC_HALF_PERIOD - 10 && elapsedCycles <= HSYNC_HALF_PERIOD + 10)
			{
				scanline_count = scanline_top;
			}
			else if (scanline_count != -999)
			{

				if (scanline_count >= 0)
				{
					render_line(video_buffer + scanline_count * VIDEO_DISP_WIDTH,
								&scanline_buf[0],
								left_edge + left_edge_cycle,
								palette);
				}

				scanline_count++;
				left_edge_cycle = cycleCounter;

				if (scanline_count == 224)
				{
					scanline_count = -999;
				}
			}

			prevCyclesCounter = cycleCounter;
		}
		break;

	case (ports::PORTA):
		if ((value & 8) > (io[addr] & 8)) // clock rising
		{
			for (int i = 0; i < 2; i++)
				latched_buttons[i] >>= 1;
		}
		if (value & 4) // latch high
		{
			for (int i = 0; i < 2; i++)
				latched_buttons[i] = buttons[i];
		}
		PINA = latched_buttons[0] & 1 | latched_buttons[1] << 1 & 2;

		changed = value ^ io[addr];
		went_low = changed & io[addr];

		//Uzebox keyboard (always on P2 port)
		switch (uzeKbState)
		{
		case KB_STOP:
			//check uzekeyboard start condition: clock=low & latch=high simultaneously
			if ((value & 0x0c) == 0x04)
			{
				uzeKbState = KB_TX_START;
				uzeKbEnabled = true; //enable keyboard capture for Uzebox Keyboard
			}
			break;

		case KB_TX_START:
			//check start condition pulse completed: clock=high & latch=low (normal state)
			if ((value & 0x0c) == 0x08)
			{
				uzeKbState = KB_TX_READY;
				uzeKbClock = 8;
			}
			break;

		case KB_TX_READY:
			if (went_low == (1 << 3)) // CLOCK
			{
				if (uzeKbClock == 8)
				{
					uzeKbDataOut = 0;
					//returns only keys (no commands response yet)
					if (uzeKbScanCodeQueue.empty())
					{
						uzeKbDataIn = 0;
					}
					else
					{
						uzeKbDataIn = uzeKbScanCodeQueue.front();
						uzeKbScanCodeQueue.pop();
					}
				}

				//shift data out to keyboard
				//latch pin is used as "Data Out"
				uzeKbDataOut <<= 1;
				if (value & 0x04)
				{ //latch pin=1?
					uzeKbDataOut |= 1;
				}

				//shift data in from keyboard
				if (uzeKbDataIn & 0x80)
				{
					PINA |= (0x02); //set P2 data bit
				}
				else
				{
					PINA &= ~(0x02); //clear P2 data bit
				}
				uzeKbDataIn <<= 1;

				uzeKbClock--;
				if (uzeKbClock == 0)
				{
					if (uzeKbDataOut == KB_SEND_END)
					{
						uzeKbState = KB_STOP;
					}
					else
					{
						uzeKbClock = 8;
					}
				}
			}
			break;
		}

		io[addr] = value;
		break;

	case (ports::TCNT1H):
		// p106 in 644 manual; 16-bit values are latched
		T16_latch = value;
		break;

	case (ports::TCNT1L):
		dly_TCNT1L = value;
		dly_TCNT1H = T16_latch;
		dly_out |= DLY_TCNT1;
		break;

	case (ports::SPDR):
		if ((SPCR & 0x40) && SD_ENABLED())
		{ // only if SPI is enabled and card is present
			spiByte = value;
			//TODO: flag collision if x-fer in progress
			spiClock = spiCycleWait;
			spiTransfer = 1;
			SPSR ^= 0x80; // clear interrupt
						  //SPI_DEBUG("spiClock: %0.2X\n",spiClock);
		}
		// SPI_DEBUG("SPDR: %0.2X\n",value);
		io[addr] = value;
		break;

	case (ports::SPCR):
		SPI_DEBUG("SPCR: %02X\n", value);
		io[addr] = value;
		if (SD_ENABLED())
			spi_calculateClock();
		break;

	case (ports::SPSR):
		SPI_DEBUG("SPSR: %02X\n", value);
		io[addr] = value;
		if (SD_ENABLED())
			spi_calculateClock();
		break;

	case (ports::EECR):
		//printf("writing to port %s (%x) pc = %x\n",port_name(addr),value,pc-1);
		//EEPROM can only be put into either read or write mode, and the master bit must be set

		if (value & EERE)
		{
			if (io[addr] & EEPE)
			{
				io[addr] = value ^ EERE; // programming in progress, don't allow this to be set
			}
			else
			{
				io[addr] = value;
			}
		}
		else if (value & EEPE)
		{
			if ((io[addr] & EERE) || !(io[addr] & EEMPE))
			{							 // need master program enabled first
				io[addr] = value ^ EEPE; // read in progress, don't allow this to be set
			}
			else
			{
				io[addr] = value;
			}
		}
		if (value & EEMPE)
		{
			io[addr] = value;
			// eeClock = 4; TODO: This was only set here, never used. Maybe a never completed EEPROM timing code.
		}
		else
		{
			io[addr] = value;
		}
		break;

	// Note: This was commented out in the original code. If needed,
	// integrate in the switch.
	//else if(addr == ports::EEARH || addr == ports::EEARL || addr == ports::EEDR){
	//	io[addr] = value;

	case (ports::TIFR1):
		//clear flags by writing logical one
		io[addr] &= ~(value);
		break;

	case (ports::TCCR1B):
		dly_TCCR1B = value;
		dly_out |= DLY_TCCR1B;
		break;

	case (ports::OCR1AH):
	case (ports::OCR1AL):
	case (ports::OCR1BH):
	case (ports::OCR1BL):
		// TODO: These should also be latched by the Atmel docs, maybe
		// implement it later.
		io[addr] = value;
		TCNT1 += timer1_base - timer1_next;
		timer1_base = 0U;
		timer1_next = 0U; // Force timer state recalculation (update_hardware)
		break;

	case (ports::res3A):
		// emulator-only whisper support
		printf("%c", value);
		break;

	case (ports::res39):
		// emulator-only whisper support
		printf("%02x", value);
		break;

	default:
		io[addr] = value;
		break;
	}
}

extern void (*InputCallback)();

u8 avr8::read_io(u8 addr)
{
	if (addr == ports::PINA)
	{
		lagged = false;
		if (InputCallback)
			InputCallback();
	}

	// p106 in 644 manual; 16-bit values are latched
	if (addr == ports::TCNT1L)
	{
		unsigned int curr_timer = TCNT1 + timer1_base - timer1_next;
		T16_latch = (curr_timer >> 8) & 0xFFU;
		return curr_timer & 0xFFU;
	}
	else if (addr == ports::TCNT1H)
	{
		return T16_latch;
	}
	else
	{
		return io[addr];
	}
}

// Inline variation of update_hardware, to be used with frequent multi-cycle
// instructions.
inline void avr8::update_hardware_fast()
{
	if (timer1_next == 0U)
	{
		update_hardware();
		return;
	}

	cycleCounter++;
	timer1_next--;
	scanline_buf[cycleCounter & 0x7FFU] = pixel_raw;
}

// Performs hardware updates which have to be calculated at cycle precision
void avr8::update_hardware()
{
	cycleCounter++;

	// timer1_next stores the cycles remaining until the next event on the
	// Timer1 16 bit timer. It can be cleared to zero whenever the timer's
	// state is changed (port writes). Locking it to zero should have no
	// effect, causing the timer to re-calculate its state proper on every
	// update_hardware call. It should only improve performance.

	if (timer1_next == 0U)
	{

		// Apply time elapsed between full timer processings

		TCNT1 += timer1_base;

		// Apply delayed timer interrupt flags

		TIFR1 |= itd_TIFR1;
		itd_TIFR1 = 0U;

		// Process timer

		if ((TCCR1B & 7U) != 0U) // If timer 1 is started
		{

			unsigned int OCR1A = OCR1AL | ((unsigned int)(OCR1AH) << 8);
			unsigned int OCR1B = OCR1BL | ((unsigned int)(OCR1BH) << 8);

			if (TCCR1B & WGM12) // Timer in CTC mode: count up to OCRnA then resets to zero
			{

				if (TCNT1 == 0xFFFFU)
				{
					itd_TIFR1 |= TOV1;
				}

				if (TCNT1 == OCR1B)
				{
					itd_TIFR1 |= OCF1B;
				}

				if (TCNT1 == OCR1A)
				{
					TCNT1 = 0U;
					itd_TIFR1 |= OCF1A;
				}
				else
				{
					TCNT1 = (TCNT1 + 1U) & 0xFFFFU;
				}

				// Calculate next timer event

				if (itd_TIFR1 == 0U)
				{
					timer1_next = 0xFFFFU - TCNT1;
					if ((TCNT1 <= OCR1B) &&
						(timer1_next > (OCR1B - TCNT1)))
					{
						timer1_next = (OCR1B - TCNT1);
					}
					if ((TCNT1 <= OCR1A) &&
						(timer1_next > (OCR1A - TCNT1)))
					{
						timer1_next = (OCR1A - TCNT1);
					}
				}
			}
			else
			{ //timer in normal mode: counts up to 0xffff then rolls over

				if (TCNT1 == 0xFFFFU)
				{
					itd_TIFR1 |= TOV1;
				}
				TCNT1 = (TCNT1 + 1U) & 0xFFFFU;

				// Calculate next timer event

				if (itd_TIFR1 == 0U)
				{
					timer1_next = 0xFFFFU - TCNT1;
				}
			}
		}

		// Set timer base to be able to reproduce TCNT1 outside full timer
		// processing

		timer1_base = timer1_next;
	}
	else
	{
		timer1_next--;
	}

	// Draw pixel on scanline

	scanline_buf[cycleCounter & 0x7FFU] = pixel_raw;
}

// Performs hardware updates which can be done at instruction precision
// Also process interrupt requests
inline void avr8::update_hardware_ins()
{
	// Apply delayed outputs
	//
	// Notes: This can be here since as of now, it looks like all
	// instructions writing IO registers perform that in their last
	// cycle, so a delayed output may only be produced then, that is,
	// after the instruction. If this doesn't hold up, this will have
	// to be placed in the update_hardware call.

	if (dly_out != 0U)
	{
		if ((dly_out & DLY_TCCR1B) != 0U)
		{
			TCCR1B = dly_TCCR1B;
			TCNT1 += timer1_base - timer1_next;
			timer1_base = 0U;
			timer1_next = 0U; // Timer state changes
		}
		if ((dly_out & DLY_TCNT1) != 0U)
		{
			TCNT1 = (dly_TCNT1H << 8) | dly_TCNT1L;
			timer1_base = 0U;
			timer1_next = 0U; // Timer state changes
		}
		dly_out = 0U;
	}

	// Get cycle count to emulate

	unsigned int cycles = cycleCounter - cycle_ctr_ins;
	cycle_ctr_ins = cycleCounter;

	// Notes:
	//
	// From this point if further cycles are required to be consumed,
	// those should be consumed using update_hardware(). This won't
	// increase this run's cycle count (cycles), but will show in the
	// next run proper.

	// Watchdog notes:
	//
	// This is a bare minimum implementation to make the Uzebox kernel's
	// seed generator operational (used for seeding a PRNG).

	if (WDTCSR & WDE)
	{ //if watchdog enabled
		watchdogTimer += cycles;
		if (watchdogTimer >= DELAY16MS && (WDTCSR & WDIE))
		{
			WDTCSR |= WDIF; //watchdog interrupt
			//reset watchdog
			//watchdog is based on a RC oscillator
			//so add some random variation to simulate entropy
			watchdogTimer = rand() % 1024;
		}
	}

	// clock the SPI hardware.
	if ((SPCR & 0x40) && SD_ENABLED())
	{ // only if SPI is enabled
		//TODO: test for master/slave modes (assume master for now)
		// TODO: factor in clock divider

		if (spiTransfer)
		{
			if (spiClock <= cycles)
			{
				//SPI_DEBUG("SPI transfer complete\n");
				update_spi();
				spiClock = 0;
				spiTransfer = 0;
				SPSR |= 0x80; // set interrupt
			}
			else
			{
				spiClock -= cycles;
			}
		}
		/*
        //HACK: instantaneous SPI access
        if(spiTransfer && spiClock > 0){
            update_spi();
            SPSR |= 0x80; // set interrupt
            spiTransfer = 0;
            spiClock = 0;
        }*/

		// test for interrupt (enable and interrupt flag for SPI)
		// TODO (Jubatian): Verify that the move is OK, if not, try to fix
		// it there (where the other interrupts are)
	}

	//clock the EEPROM hardware
	/*
    1. Wait until EEPE becomes zero.
    2. Wait until SELFPRGEN in SPMCSR becomes zero.
    3. Write new EEPROM address to EEAR (optional).
    4. Write new EEPROM data to EEDR (optional).
    5. Write a logical one to the EEMPE bit while writing a zero to EEPE in EECR.
    6. Within four clock cycles after setting EEMPE, write a logical one to EEPE.
    The EEPROM can not be programmed during a CPU write to the Flash memory.
    */
	// are we attempting to program?

	// TODO (Jubatian):
	//
	// cycleCounter is incremented here by 4, but this has no effect on at
	// least Timer 1 and the video output, maybe even more. Not like
	// writing to EEPROM would be a common task when drawing the video
	// frame, though.

	if (EECR & (EEPE | EERE))
	{
		if (EECR & EEPE)
		{
			//printf("attempting write of EEPROM\n");
			cycleCounter += 4; // writes take four cycles
			int addr = (EEARH << 8) | EEARL;
			if (addr < eepromSize)
				eeprom[addr] = EEDR;
			EECR ^= (EEMPE | EEPE); // clear program bits

			// interrupt?
			//if((EECR & EERIE) && BIT(SREG,SREG_I)){
			//    SPSR ^= 0x80; // clear the interrupt
			//    trigger_interrupt(SPI_STC); // execute the vector
			//}
		}
		// are we attempting to read?
		else if (EECR & EERE)
		{
			// printf("attempting read of EEPROM\n");
			cycleCounter += 4; // eeprom reads take 4 additonal cycles
			int addr = (EEARH << 8) | EEARL;
			if (addr < eepromSize)
				EEDR = eeprom[addr];
			EECR ^= EERE; // clear read  bit

			// interrupt?
			//if((EECR & EERIE) && BIT(SREG,SREG_I)){
			//    SPSR ^= 0x80; // clear the interrupt
			//    trigger_interrupt(SPI_STC); // execute the vector
			//}
		}
	}

	// Process interrupts in order of priority

	if (SREG & (1 << SREG_I))
	{
		// Note (Jubatian):
		// The SD card's SPI interrupt trigger was within the SPI
		// handling part in update_hardware, however it belongs to
		// interrupt triggers. Priority order might be broken (but
		// essentially the emulator behaved according to this order
		// prior to this move).
		if ((SPCR & 0x80) && (SPSR & 0x80))
		{
			// TODO: verify that SPI is dependent upon the global interrupt flag
			SPSR ^= 0x80; // Clear the interrupt
			trigger_interrupt(SPI_STC);
		}
		else if ((WDTCSR & (WDIF | WDIE)) == (WDIF | WDIE))
		{
			WDTCSR &= ~WDIF; // Clear watchdog flag
			trigger_interrupt(WDT);
		}
		else if ((TIFR1 & (OCF1A | OCF1B | TOV1)) && (TIMSK1 & (OCIE1A | OCIE1B | TOIE1)))
		{
			if ((TIFR1 & OCF1A) && (TIMSK1 & OCIE1A))
			{
				TIFR1 &= ~OCF1A; // Clear CTC match flag
				trigger_interrupt(TIMER1_COMPA);
			}
			else if ((TIFR1 & OCF1B) && (TIMSK1 & OCIE1B))
			{
				TIFR1 &= ~OCF1B; // Clear CTC match flag
				trigger_interrupt(TIMER1_COMPB);
			}
			else if ((TIFR1 & TOV1) && (TIMSK1 & TOIE1))
			{
				TIFR1 &= ~TOV1; // Clear TOV1 flag
				trigger_interrupt(TIMER1_OVF);
			}
		}
	}
}

static const instructionList_t instructionList[] = {

	{1, "ADC    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0001110000000000, 0b0000000111110000, 0b0000001000001111},
	{2, "ADD    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0000110000000000, 0b0000000111110000, 0b0000001000001111},
	{3, "ADIW   r%d, %d ", 1, 2, 24, 0, 3, 1, 0, 0, 1, 2, 0b1001011000000000, 0b0000000000110000, 0b0000000011001111},
	{4, "AND    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0010000000000000, 0b0000000111110000, 0b0000001000001111},
	{5, "ANDI   r%d, %d ", 1, 1, 16, 0, 3, 1, 0, 0, 1, 1, 0b0111000000000000, 0b0000000011110000, 0b0000111100001111},
	{6, "ASR    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000000101, 0b0000000111110000, 0b0000000000000000},
	{7, "BCLR   %d ", 7, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010010001000, 0b0000000001110000, 0b0000000000000000},
	{8, "BLD    r%d, %d ", 1, 1, 0, 0, 6, 1, 0, 0, 1, 1, 0b1111100000000000, 0b0000000111110000, 0b0000000000000111},
	{9, "BRBC   %d, %d ", 7, 1, 0, 0, 3, 1, 0, 1, 1, 2, 0b1111010000000000, 0b0000000000000111, 0b0000001111111000},
	{10, "BRBS   %d, %d ", 7, 1, 0, 0, 3, 1, 0, 1, 1, 2, 0b1111000000000000, 0b0000000000000111, 0b0000001111111000},
	{11, "BREAK ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010110011000, 0b0000000000000000, 0b0000000000000000},
	{12, "BSET   %d ", 7, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000001000, 0b0000000001110000, 0b0000000000000000},
	{13, "BST    r%d, %d ", 1, 1, 0, 0, 6, 1, 0, 0, 1, 1, 0b1111101000000000, 0b0000000111110000, 0b0000000000000111},
	{14, "CALL   %d (+ next word) ", 0, 1, 0, 0, 3, 1, 0, 0, 2, 4, 0b1001010000001110, 0b0000000000000000, 0b0000000111110001},
	{15, "CBI    io%d, %d ", 8, 1, 0, 0, 6, 1, 0, 0, 1, 2, 0b1001100000000000, 0b0000000011111000, 0b0000000000000111},
	{16, "COM    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000000000, 0b0000000111110000, 0b0000000000000000},
	{17, "CP     r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0001010000000000, 0b0000000111110000, 0b0000001000001111},
	{18, "CPC    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0000010000000000, 0b0000000111110000, 0b0000001000001111},
	{19, "CPI    r%d, %d ", 1, 1, 16, 0, 3, 1, 0, 0, 1, 1, 0b0011000000000000, 0b0000000011110000, 0b0000111100001111},
	{20, "CPSE   r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 3, 0b0001000000000000, 0b0000000111110000, 0b0000001000001111},
	{21, "DEC    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000001010, 0b0000000111110000, 0b0000000000000000},
	{22, "EOR    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0010010000000000, 0b0000000111110000, 0b0000001000001111},
	{23, "FMUL   r%d, r%d ", 1, 1, 16, 0, 2, 1, 16, 0, 1, 2, 0b0000001100001000, 0b0000000001110000, 0b0000000000000111},
	{24, "FMULS  r%d, r%d ", 1, 1, 16, 0, 2, 1, 16, 0, 1, 2, 0b0000001110000000, 0b0000000001110000, 0b0000000000000111},
	{25, "FMULSU r%d, r%d ", 1, 1, 16, 0, 2, 1, 16, 0, 1, 2, 0b0000001110001000, 0b0000000001110000, 0b0000000000000111},
	{26, "ICALL ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001010100001001, 0b0000000000000000, 0b0000000000000000},
	{27, "IJMP ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001010000001001, 0b0000000000000000, 0b0000000000000000},
	{28, "IN     r%d, io%d ", 1, 1, 0, 0, 8, 1, 0, 0, 1, 1, 0b1011000000000000, 0b0000000111110000, 0b0000011000001111},
	{29, "INC    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000000011, 0b0000000111110000, 0b0000000000000000},
	{30, "JMP    %d (+ next word) ", 0, 1, 0, 0, 3, 1, 0, 0, 2, 3, 0b1001010000001100, 0b0000000000000000, 0b0000000111110001},
	{31, "LD     r%d, -X ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000001110, 0b0000000111110000, 0b0000000000000000},
	{32, "LD     r%d, -Y ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000001010, 0b0000000111110000, 0b0000000000000000},
	{33, "LD     r%d, -Z ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000000010, 0b0000000111110000, 0b0000000000000000},
	{34, "LD     r%d, X ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000001100, 0b0000000111110000, 0b0000000000000000},
	{35, "LD     r%d, X+ ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000001101, 0b0000000111110000, 0b0000000000000000},
	{36, "LD     r%d, Y+ ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000001001, 0b0000000111110000, 0b0000000000000000},
	{37, "LD     r%d, Y+%d ", 1, 1, 0, 0, 5, 1, 0, 0, 1, 3, 0b1000000000001000, 0b0000000111110000, 0b0010110000000111},
	{38, "LD     r%d, Z+ ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000000001, 0b0000000111110000, 0b0000000000000000},
	{39, "LD     r%d, Z+%d ", 1, 1, 0, 0, 5, 1, 0, 0, 1, 3, 0b1000000000000000, 0b0000000111110000, 0b0010110000000111},
	{40, "LDI    r%d, %d ", 1, 1, 16, 0, 3, 1, 0, 0, 1, 1, 0b1110000000000000, 0b0000000011110000, 0b0000111100001111},
	{41, "LDS    r%d, %d (+next word) ", 1, 1, 0, 0, 0, 1, 0, 0, 2, 2, 0b1001000000000000, 0b0000000111110000, 0b0000000000000000},
	{42, "LPM ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001010111001000, 0b0000000000000000, 0b0000000000000000},
	{43, "LPM    r%d, Z ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000000100, 0b0000000111110000, 0b0000000000000000},
	{44, "LPM    r%d, Z+ ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 3, 0b1001000000000101, 0b0000000111110000, 0b0000000000000000},
	{45, "LSR    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000000110, 0b0000000111110000, 0b0000000000000000},
	{46, "MOV    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0010110000000000, 0b0000000111110000, 0b0000001000001111},
	{47, "MOVW   r%d, r%d ", 1, 2, 0, 0, 2, 2, 0, 0, 1, 1, 0b0000000100000000, 0b0000000011110000, 0b0000000000001111},
	{48, "MUL    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 2, 0b1001110000000000, 0b0000000111110000, 0b0000001000001111},
	{49, "MULS   r%d, r%d ", 1, 1, 16, 0, 2, 1, 16, 0, 1, 2, 0b0000001000000000, 0b0000000011110000, 0b0000000000001111},
	{50, "MULSU  r%d, r%d ", 1, 1, 16, 0, 2, 1, 16, 0, 1, 2, 0b0000001100000000, 0b0000000001110000, 0b0000000000000111},
	{51, "NEG    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000000001, 0b0000000111110000, 0b0000000000000000},
	{52, "NOP ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b0000000000000000, 0b0000000000000000, 0b0000000000000000},
	{53, "OR     r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0010100000000000, 0b0000000111110000, 0b0000001000001111},
	{54, "ORI    r%d, %d ", 1, 1, 16, 0, 3, 1, 0, 0, 1, 1, 0b0110000000000000, 0b0000000011110000, 0b0000111100001111},
	{55, "OUT    io%d, r%d ", 8, 1, 0, 0, 1, 1, 0, 0, 1, 1, 0b1011100000000000, 0b0000011000001111, 0b0000000111110000},
	{56, "POP    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001000000001111, 0b0000000111110000, 0b0000000000000000},
	{57, "PUSH   r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000001111, 0b0000000111110000, 0b0000000000000000},
	{58, "RCALL  %d ", 0, 1, 0, 0, 3, 1, 0, 1, 1, 3, 0b1101000000000000, 0b0000000000000000, 0b0000111111111111},
	{59, "RET ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 4, 0b1001010100001000, 0b0000000000000000, 0b0000000000000000},
	{60, "RETI ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 4, 0b1001010100011000, 0b0000000000000000, 0b0000000000000000},
	{61, "RJMP   %d ", 0, 1, 0, 0, 3, 1, 0, 1, 1, 2, 0b1100000000000000, 0b0000000000000000, 0b0000111111111111},
	{62, "ROR    r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000000111, 0b0000000111110000, 0b0000000000000000},
	{63, "SBC    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0000100000000000, 0b0000000111110000, 0b0000001000001111},
	{64, "SBCI   r%d, %d ", 1, 1, 16, 0, 3, 1, 0, 0, 1, 1, 0b0100000000000000, 0b0000000011110000, 0b0000111100001111},
	{65, "SBI    io%d, %d ", 8, 1, 0, 0, 6, 1, 0, 0, 1, 2, 0b1001101000000000, 0b0000000011111000, 0b0000000000000111},
	{66, "SBIC   io%d, %d ", 8, 1, 0, 0, 6, 1, 0, 0, 1, 3, 0b1001100100000000, 0b0000000011111000, 0b0000000000000111},
	{67, "SBIS   io%d, %d ", 8, 1, 0, 0, 6, 1, 0, 0, 1, 3, 0b1001101100000000, 0b0000000011111000, 0b0000000000000111},
	{68, "SBIW   r%d, %d ", 1, 2, 24, 0, 3, 1, 0, 0, 1, 2, 0b1001011100000000, 0b0000000000110000, 0b0000000011001111},
	{69, "SBRC   r%d, %d ", 2, 1, 0, 0, 6, 1, 0, 0, 1, 3, 0b1111110000000000, 0b0000000111110000, 0b0000000000000111},
	{70, "SBRS   r%d, %d ", 2, 1, 0, 0, 6, 1, 0, 0, 1, 3, 0b1111111000000000, 0b0000000111110000, 0b0000000000000111},
	{71, "SLEEP ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010110001000, 0b0000000000000000, 0b0000000000000000},
	{72, "SPM    z+ ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010111101000, 0b0000000000000000, 0b0000000000000000},
	{73, "ST     -x, r%d ", 2, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000001110, 0b0000000111110000, 0b0000000000000000},
	{74, "ST     -y, r%d ", 2, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000001010, 0b0000000111110000, 0b0000000000000000},
	{75, "ST     -z, r%d ", 2, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000000010, 0b0000000111110000, 0b0000000000000000},
	{76, "ST     x, r%d ", 2, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000001100, 0b0000000111110000, 0b0000000000000000},
	{77, "ST     x+, r%d ", 2, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000001101, 0b0000000111110000, 0b0000000000000000},
	{78, "ST     y+, r%d ", 2, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000001001, 0b0000000111110000, 0b0000000000000000},
	{79, "ST     y+q, r%d (q=%d) ", 1, 1, 0, 0, 5, 1, 0, 0, 1, 2, 0b1000001000001000, 0b0000000111110000, 0b0010110000000111},
	{80, "ST     z+, r%d ", 2, 1, 0, 0, 0, 1, 0, 0, 1, 2, 0b1001001000000001, 0b0000000111110000, 0b0000000000000000},
	{81, "ST     z+q, r%d (q=%d) ", 1, 1, 0, 0, 5, 1, 0, 0, 1, 2, 0b1000001000000000, 0b0000000111110000, 0b0010110000000111},
	{82, "STS    k, r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 2, 2, 0b1001001000000000, 0b0000000111110000, 0b0000000000000000},
	{83, "SUB    r%d, r%d ", 1, 1, 0, 0, 2, 1, 0, 0, 1, 1, 0b0001100000000000, 0b0000000111110000, 0b0000001000001111},
	{84, "SUBI   r%d, %d ", 1, 1, 16, 0, 3, 1, 0, 0, 1, 1, 0b0101000000000000, 0b0000000011110000, 0b0000111100001111},
	{85, "SWAP   r%d ", 1, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010000000010, 0b0000000111110000, 0b0000000000000000},
	{86, "WDR ", 0, 1, 0, 0, 0, 1, 0, 0, 1, 1, 0b1001010110101000, 0b0000000000000000, 0b0000000000000000},

	{0, "END", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0b0000000000000000, 0b0000000000000000, 0b0000000000000000}

};

unsigned int avr8::exec()
{
	currentPc = pc;
	const instructionDecode_t insnDecoded = progmemDecoded[pc];
	const u8 opNum = insnDecoded.opNum;
	const u8 arg1_8 = insnDecoded.arg1;
	const s16 arg2_8 = insnDecoded.arg2;

	const unsigned int startcy = cycleCounter;
	u8 Rd, Rr, R, CH;
	u16 uTmp, Rd16, R16;
	s16 sTmp;

	//Program counter must be incremented *after* GDB
	pc++;

	// Instruction decoder notes:
	//
	// The instruction's timing is determined by how many update_hardware
	// calls are executed during its decoding. One update_hardware call is
	// placed after the instruction decoder since all instructions take at
	// least one cycle (there is at least one cycle after the last read /
	// write effect), so only multi-cycle instructions need to perform
	// extra calls to update_hardware.
	//
	// The read_sram and write_sram calls only access the sram.
	// TODO: I am not sure whether the instructions calling these only do
	// so on the real thing, but doing otherwise is unlikely, and may even
	// be buggy then (the behavior of things like having the stack over IO
	// area...). This solution is at least fast for these instructions.

	switch (opNum)
	{

	case 1: // 0001 11rd dddd rrrr		(1) ADC Rd,Rr (ROL is ADC Rd,Rd)
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd + Rr + C;
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_ADD;
		UPDATE_SVN_ADD;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 2: // 0000 11rd dddd rrrr		(1) ADD Rd,Rr (LSL is ADD Rd,Rd)
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd + Rr;
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_ADD;
		UPDATE_SVN_ADD;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 3: // 1001 0110 KKdd KKKK		(2) ADIW Rd+1:Rd,K   (16-bit add to upper four register pairs)
		Rd = arg1_8;
		Rr = arg2_8;
		Rd16 = r[Rd] | (r[Rd + 1] << 8);
		R16 = Rd16 + Rr;
		r[Rd] = (u8)R16;
		r[Rd + 1] = (u8)(R16 >> 8);
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		set_bit_1(SREG, SREG_V, ((~Rd16 & R16) & 0x8000) >> 15);
		set_bit_1(SREG, SREG_N, (R16 & 0x8000) >> 15);
		UPDATE_S;
		set_bit_inv(SREG, SREG_Z, R16);
		set_bit_1(SREG, SREG_C, ((~R16 & Rd16) & 0x8000) >> 15);
		update_hardware();
		break;

	case 4: // 0010 00rd dddd rrrr		(1) AND Rd,Rr (TST is AND Rd,Rd)
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd & Rr;
		clr_bits(SREG, SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_SVN_LOGICAL;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 5: // 0111 KKKK dddd KKKK		(1) ANDI Rd,K (CBR is ANDI with K complemented)
		Rd = r[arg1_8];
		Rr = arg2_8;
		R = Rd & Rr;
		clr_bits(SREG, SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_SVN_LOGICAL;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 6: // 1001 010d dddd 0101		(1) ASR Rd
		Rd = r[arg1_8];
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		set_bit_1(SREG, SREG_C, Rd & 1);
		r[arg1_8] = R = (Rd >> 1) | (Rd & 0x80);
		UPDATE_N;
		set_bit_1(SREG, SREG_V, (R >> 7) ^ (Rd & 1));
		UPDATE_S;
		UPDATE_Z;
		break;

	case 8: // 1111 100d dddd 0bbb		(1) BLD Rd,b
		Rd = arg1_8;
		store_bit_1(r[Rd], arg2_8, (SREG >> SREG_T) & 1U);
		break;

	case 7: // 1001 0100 1sss 1000		(1) BCLR s (CLC, etc are aliases with sss implicit)
		Rd = arg1_8;
		SREG &= ~(1U << Rd);
		break;

	case 9: // 1111 01kk kkkk ksss		(1/2) BRBC s,k (BRCC, etc are aliases for this with sss implicit)
		if (!(SREG & (1 << (arg1_8))))
		{
			update_hardware();
			pc += arg2_8;
		}
		break;

	case 10: // 1111 00kk kkkk ksss		(1/2) BRBS s,k (same here)
		if (SREG & (1 << (arg1_8)))
		{
			update_hardware();
			pc += arg2_8;
		}
		break;

	case 11: // 1001 0101 1001 1000		(?) BREAK
		// no operation
		break;

	case 12: // 1001 0100 0sss 1000		(1) BSET s (SEC, etc are aliases with sss implicit)
		Rd = arg1_8;
		SREG |= (1U << Rd);
		break;

	case 13: // 1111 101d dddd 0bbb		(1) BST Rd,b
		Rd = r[arg1_8];
		store_bit_1(SREG, SREG_T, (Rd >> (arg2_8)) & 1U);
		break;

	case 14: // 1001 010k kkkk 111k		(4) CALL k (next word is rest of address)
		// Note: 64K progmem, so 'k' in first insn word is unused
		update_hardware();
		update_hardware();
		update_hardware();
		write_sram(SP, (pc + 1));
		DEC_SP;
		write_sram(SP, (pc + 1) >> 8);
		DEC_SP;
		pc = arg2_8;
		break;

	case 15: // 1001 1000 AAAA Abbb		(2) CBI A,b
		update_hardware();
		Rd = arg1_8;
		write_io(Rd, read_io(Rd) & ~(1 << (arg2_8)));
		break;

	case 16: // 1001 010d dddd 0000		(1) COM Rd
		r[arg1_8] = R = ~r[arg1_8];
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_SVN_LOGICAL;
		UPDATE_Z;
		SET_C;
		break;

	case 17: // 0001 01rd dddd rrrr		(1) CP Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd - Rr;
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_Z;
		break;

	case 18: // 0000 01rd dddd rrrr		(1) CPC Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd - Rr - C;
		clr_bits(SREG, SREG_CM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_CLEAR_Z;
		break;

	case 19: // 0011 KKKK dddd KKKK		(1) CPI Rd,K
		Rd = r[arg1_8];
		Rr = arg2_8;
		R = Rd - Rr;
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_Z;
		break;

	case 20: // 0001 00rd dddd rrrr		(1/2/3) CPSE Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		if (Rd == Rr)
		{
			unsigned int icc = get_insn_size(progmemDecoded[pc].opNum);
			pc += icc;
			while (icc != 0U)
			{
				update_hardware();
				icc--;
			}
		}
		break;

	case 21: // 1001 010d dddd 1010		(1) DEC Rd
		R = --r[arg1_8];
		clr_bits(SREG, SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_N;
		set_bit_inv(SREG, SREG_V, (unsigned int)(R)-0x7FU);
		UPDATE_S;
		UPDATE_Z;
		break;

	case 22: // 0010 01rd dddd rrrr		(1) EOR Rd,Rr (CLR is EOR Rd,Rd)
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd ^ Rr;
		clr_bits(SREG, SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_SVN_LOGICAL;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 23: // 0000 0011 0ddd 1rrr		(2) FMUL Rd,Rr (registers are in 16-23 range)
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		uTmp = (u8)Rd * (u8)Rr;
		r0 = (u8)(uTmp << 1);
		r1 = (u8)(uTmp >> 7);
		clr_bits(SREG, SREG_CM | SREG_ZM);
		UPDATE_CZ_MUL(uTmp);
		update_hardware();
		break;

	case 24: // 0000 0011 1ddd 0rrr		(2) FMULS Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		sTmp = (s8)Rd * (s8)Rr;
		r0 = (u8)(sTmp << 1);
		r1 = (u8)(sTmp >> 7);
		clr_bits(SREG, SREG_CM | SREG_ZM);
		UPDATE_CZ_MUL(sTmp);
		update_hardware();
		break;

	case 25: // 0000 0011 1ddd 1rrr		(2) FMULSU Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		sTmp = (s8)Rd * (u8)Rr;
		r0 = (u8)(sTmp << 1);
		r1 = (u8)(sTmp >> 7);
		clr_bits(SREG, SREG_CM | SREG_ZM);
		UPDATE_CZ_MUL(sTmp);
		update_hardware();
		break;

	case 26: // 1001 0101 0000 1001		(3) ICALL (call thru Z register)
		update_hardware();
		update_hardware();
		write_sram(SP, u8(pc));
		DEC_SP;
		write_sram(SP, (pc) >> 8);
		DEC_SP;
		pc = Z;
		break;

	case 27: // 1001 0100 0000 1001		(2) IJMP (jump thru Z register)
		update_hardware_fast();
		pc = Z;
		break;

	case 28: // 1011 0AAd dddd AAAA		(1) IN Rd,A
		Rd = arg1_8;
		Rr = arg2_8;
		r[Rd] = read_io(Rr);
		break;

	case 29: // 1001 010d dddd 0011		(1) INC Rd
		R = ++r[arg1_8];
		clr_bits(SREG, SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_N;
		set_bit_inv(SREG, SREG_V, (unsigned int)(R)-0x80U);
		UPDATE_S;
		UPDATE_Z;
		break;

	case 30: // 1001 010k kkkk 110k		(3) JMP k (next word is rest of address)
		// Note: 64K progmem, so 'k' in first insn word is unused
		update_hardware();
		update_hardware();
		pc = arg2_8;
		break;

	case 31: // 1001 000d dddd 1110		(2) LD rd,-X
		update_hardware();
		DEC_X;
		r[arg1_8] = read_sram_io(X);
		break;

	case 32: // 1001 000d dddd 1010		(2) LD Rd,-Y
		update_hardware();
		DEC_Y;
		r[arg1_8] = read_sram_io(Y);
		break;

	case 33: // 1001 000d dddd 0010		(2) LD Rd,-Z
		update_hardware();
		DEC_Z;
		r[arg1_8] = read_sram_io(Z);
		break;

	case 34: // 1001 000d dddd 1100		(2) LD rd,X
		update_hardware();
		r[arg1_8] = read_sram_io(X);
		break;

	case 35: // 1001 000d dddd 1101		(2) LD rd,X+
		update_hardware();
		r[arg1_8] = read_sram_io(X);
		INC_X;
		break;

	case 36: // 1001 000d dddd 1001		(2) LD Rd,Y+
		update_hardware();
		r[arg1_8] = read_sram_io(Y);
		INC_Y;
		break;

	case 37: // 10q0 qq0d dddd 1qqq		(2) LDD Rd,Y+q
		update_hardware();
		Rd = arg1_8;
		Rr = arg2_8;
		r[Rd] = read_sram_io(Y + Rr);
		break;

	case 38: // 1001 000d dddd 0001		(2) LD Rd,Z+
		update_hardware();
		r[arg1_8] = read_sram_io(Z);
		INC_Z;
		break;

	case 39: // 10q0 qq0d dddd 0qqq		(2) LDD Rd,Z+q
		update_hardware();
		Rd = arg1_8;
		Rr = arg2_8;
		r[Rd] = read_sram_io(Z + Rr);
		break;

	case 40: // 1110 KKKK dddd KKKK		(1) LDI Rd,K (SER is just LDI Rd,255)
		r[arg1_8] = arg2_8;
		break;

	case 41: // 1001 000d dddd 0000		(2) LDS Rd,k (next word is rest of address)
		update_hardware();
		r[arg1_8] = read_sram_io(arg2_8);
		pc++;
		break;

	case 42: // 1001 0101 1100 1000		(3) LPM (r0 implied, why is this special?)
		update_hardware();
		update_hardware();
		r0 = read_progmem(Z);
		break;

	case 43: // 1001 000d dddd 0100		(3) LPM Rd,Z
		update_hardware_fast();
		update_hardware_fast();
		r[arg1_8] = read_progmem(Z);
		break;

	case 44: // 1001 000d dddd 0101		(3) LPM Rd,Z+
		update_hardware_fast();
		update_hardware_fast();
		r[arg1_8] = read_progmem(Z);
		INC_Z;
		break;

	case 45: // 1001 010d dddd 0110		(1) LSR Rd
		Rd = r[arg1_8];
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		set_bit_1(SREG, SREG_C, Rd & 1);
		r[arg1_8] = R = (Rd >> 1);
		UPDATE_N;
		set_bit_1(SREG, SREG_V, Rd & 1);
		UPDATE_S;
		UPDATE_Z;
		break;

	case 46: // 0010 11rd dddd rrrr		(1) MOV Rd,Rr
		r[arg1_8] = r[arg2_8];
		break;

	case 47: // 0000 0001 dddd rrrr		(1) MOVW Rd+1:Rd,Rr+1:R
		Rd = arg1_8;
		Rr = arg2_8;
		r[Rd] = r[Rr];
		r[Rd + 1] = r[Rr + 1];
		break;

	case 48: // 1001 11rd dddd rrrr		(2) MUL Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		uTmp = Rd * Rr;
		r0 = (u8)uTmp;
		r1 = (u8)(uTmp >> 8);
		clr_bits(SREG, SREG_CM | SREG_ZM);
		UPDATE_CZ_MUL(uTmp);
		update_hardware_fast();
		break;

	case 49: // 0000 0010 dddd rrrr		(2) MULS Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		sTmp = (s8)Rd * (s8)Rr;
		r0 = (u8)sTmp;
		r1 = (u8)(sTmp >> 8);
		clr_bits(SREG, SREG_CM | SREG_ZM);
		UPDATE_CZ_MUL(sTmp);
		update_hardware();
		break;

	case 50: // 0000 0011 0ddd 0rrr		(2) MULSU Rd,Rr (registers are in 16-23 range)
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		sTmp = (s8)Rd * (u8)Rr;
		r0 = (u8)sTmp;
		r1 = (u8)(sTmp >> 8);
		clr_bits(SREG, SREG_CM | SREG_ZM);
		UPDATE_CZ_MUL(sTmp);
		update_hardware();
		break;

	case 51: // 1001 010d dddd 0001		(1) NEG Rd
		Rr = r[arg1_8];
		Rd = 0;
		r[arg1_8] = R = Rd - Rr;
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_Z;
		break;

	case 52: // 0000 0000 0000 0000		(1) NOP
		break;

	case 53: // 0010 10rd dddd rrrr		(1) OR Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd | Rr;
		clr_bits(SREG, SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_SVN_LOGICAL;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 54: // 0110 KKKK dddd KKKK		(1) ORI Rd,K (same as SBR insn)
		Rd = r[arg1_8];
		Rr = arg2_8;
		R = Rd | Rr;
		clr_bits(SREG, SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		UPDATE_SVN_LOGICAL;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 55: // 1011 1AAd dddd AAAA		(1) OUT A,Rd
		Rd = arg2_8;
		Rr = arg1_8;
		write_io(Rr, r[Rd]);
		break;

	case 56: // 1001 000d dddd 1111		(2) POP Rd
		update_hardware();
		INC_SP;
		r[arg1_8] = read_sram(SP);
		break;

	case 57: // 1001 001d dddd 1111		(2) PUSH Rd
		update_hardware();
		write_sram(SP, r[arg1_8]);
		DEC_SP;
		break;

	case 58: // 1101 kkkk kkkk kkkk		(3) RCALL k
		update_hardware();
		update_hardware();
		write_sram(SP, (u8)pc);
		DEC_SP;
		write_sram(SP, pc >> 8);
		DEC_SP;
		pc += arg2_8;
		break;

	case 59: // 1001 0101 0000 1000		(4) RET
		update_hardware();
		update_hardware();
		update_hardware();
		INC_SP;
		pc = read_sram(SP) << 8;
		INC_SP;
		pc |= read_sram(SP);
		break;

	case 60: // 1001 0101 0001 1000		(4) RETI
		update_hardware();
		update_hardware();
		update_hardware();
		INC_SP;
		pc = read_sram(SP) << 8;
		INC_SP;
		pc |= read_sram(SP);
		SREG |= (1 << SREG_I);
		//--interruptLevel;
		break;

	case 61: // 1100 kkkk kkkk kkkk		(2) RJMP k
		update_hardware_fast();
		pc += arg2_8;
		break;

	case 62: // 1001 010d dddd 0111		(1) ROR Rd
		Rd = r[arg1_8];
		r[arg1_8] = R = (Rd >> 1) | ((SREG & 1) << 7);
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		set_bit_1(SREG, SREG_C, Rd & 1);
		UPDATE_N;
		set_bit_1(SREG, SREG_V, (R >> 7) ^ (Rd & 1));
		UPDATE_S;
		UPDATE_Z;
		break;

	case 63: // 0000 10rd dddd rrrr		(1) SBC Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd - Rr - C;
		clr_bits(SREG, SREG_CM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_CLEAR_Z;
		r[arg1_8] = R;
		break;

	case 64: // 0100 KKKK dddd KKKK		(1) SBCI Rd,K
		Rd = r[arg1_8];
		Rr = arg2_8;
		R = Rd - Rr - C;
		clr_bits(SREG, SREG_CM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_CLEAR_Z;
		r[arg1_8] = R;
		break;

	case 65: // 1001 1010 AAAA Abbb		(2) SBI A,b
		update_hardware();
		Rd = arg1_8;
		write_io(Rd, read_io(Rd) | (1 << (arg2_8)));
		break;

	case 66: // 1001 1001 AAAA Abbb		(1/2/3) SBIC A,b
		Rd = arg1_8;
		if (!(read_io(Rd) & (1 << (arg2_8))))
		{
			unsigned int icc = get_insn_size(progmemDecoded[pc].opNum);
			pc += icc;
			while (icc != 0U)
			{
				update_hardware();
				icc--;
			}
		}
		break;

	case 67: // 1001 1011 AAAA Abbb		(1/2/3) SBIS A,b
		Rd = arg1_8;
		if (read_io(Rd) & (1 << (arg2_8)))
		{
			unsigned int icc = get_insn_size(progmemDecoded[pc].opNum);
			pc += icc;
			while (icc != 0U)
			{
				update_hardware();
				icc--;
			}
		}
		break;

	case 68: // 1001 0111 KKdd KKKK		(2) SBIW Rd+1:Rd,K
		Rd = arg1_8;
		Rr = arg2_8;
		Rd16 = r[Rd] | (r[Rd + 1] << 8);
		R16 = Rd16 - Rr;
		r[Rd] = (u8)R16;
		r[Rd + 1] = (u8)(R16 >> 8);
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM);
		set_bit_1(SREG, SREG_V, ((Rd16 & ~R16) & 0x8000) >> 15);
		set_bit_1(SREG, SREG_N, (R16 & 0x8000) >> 15);
		UPDATE_S;
		set_bit_inv(SREG, SREG_Z, R16);
		set_bit_1(SREG, SREG_C, ((R16 & ~Rd16) & 0x8000) >> 15);
		update_hardware();
		break;

	case 69: // 1111 110r rrrr 0bbb		(1/2/3) SBRC Rr,b
		Rd = r[arg1_8];
		if (((Rd >> (arg2_8)) & 1U) == 0)
		{
			unsigned int icc = get_insn_size(progmemDecoded[pc].opNum);
			pc += icc;
			while (icc != 0U)
			{
				update_hardware();
				icc--;
			}
		}
		break;

	case 70: // 1111 111r rrrr 0bbb		(1/2/3) SBRS Rr,b
		Rd = r[arg1_8];
		if (((Rd >> (arg2_8)) & 1U) == 1)
		{
			unsigned int icc = get_insn_size(progmemDecoded[pc].opNum);
			pc += icc;
			while (icc != 0U)
			{
				update_hardware();
				icc--;
			}
		}
		break;

	case 71: // 1001 0101 1000 1000		(?) SLEEP
		elapsedCyclesSleep = cycleCounter - lastCyclesSleep;
		lastCyclesSleep = cycleCounter;
		break;

	case 72: // 1001 0101 1110 1000		(?) SPM Z (writes R1:R0)
		update_hardware();
		update_hardware(); // Cycle count undocumented?!?!?
		update_hardware(); // (4 cycles emulated)
		if (Z >= progSize / 2)
		{
			fprintf(stderr, "illegal write to progmem addr %x\n", Z);
			shutdown(1);
		}
		else
		{
			progmem[Z] = r0 | (r1 << 8);
			decodeFlash(Z - 1);
			decodeFlash(Z);
		}
		break;

	case 73: // 1001 001r rrrr 1110		(2) ST -X,Rr
		update_hardware();
		DEC_X;
		write_sram_io(X, r[arg1_8]);
		break;

	case 74: // 1001 001r rrrr 1010		(2) ST -Y,Rr
		update_hardware();
		DEC_Y;
		write_sram_io(Y, r[arg1_8]);
		break;

	case 75: // 1001 001r rrrr 0010		(2) ST -Z,Rr
		update_hardware();
		DEC_Z;
		write_sram_io(Z, r[arg1_8]);
		break;

	case 76: // 1001 001r rrrr 1100		(2) ST X,Rr
		update_hardware();
		write_sram_io(X, r[arg1_8]);
		break;

	case 77: // 1001 001r rrrr 1101		(2) ST X+,Rr
		update_hardware();
		write_sram_io(X, r[arg1_8]);
		INC_X;
		break;

	case 78: // 1001 001r rrrr 1001		(2) ST Y+,Rr
		update_hardware();
		write_sram_io(Y, r[arg1_8]);
		INC_Y;
		break;

	case 79: // 10q0 qq1d dddd 1qqq		(2) STD Y+q,Rd
		Rd = arg1_8;
		Rr = arg2_8;
		update_hardware();
		write_sram_io(Y + Rr, r[Rd]);
		break;

	case 80: // 1001 001r rrrr 0001		(2) ST Z+,Rr
		update_hardware();
		write_sram_io(Z, r[arg1_8]);
		INC_Z;
		break;

	case 81: // 10q0 qq1d dddd 0qqq		(2) STD Z+q,Rd
		Rd = arg1_8;
		Rr = arg2_8;
		update_hardware();
		write_sram_io(Z + Rr, r[Rd]);
		break;

	case 82: // 1001 001d dddd 0000		(2) STS k,Rr (next word is rest of address)
		update_hardware();
		write_sram_io(arg2_8, r[arg1_8]);
		pc++;
		break;

	case 83: // 0001 10rd dddd rrrr		(1) SUB Rd,Rr
		Rd = r[arg1_8];
		Rr = r[arg2_8];
		R = Rd - Rr;
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 84: // 0101 KKKK dddd KKKK		(1) SUBI Rd,K
		Rd = r[arg1_8];
		Rr = arg2_8;
		R = Rd - Rr;
		clr_bits(SREG, SREG_CM | SREG_ZM | SREG_NM | SREG_VM | SREG_SM | SREG_HM);
		UPDATE_HC_SUB;
		UPDATE_SVN_SUB;
		UPDATE_Z;
		r[arg1_8] = R;
		break;

	case 85: // 1001 010d dddd 0010		(1) SWAP Rd
		Rd = r[arg1_8];
		r[arg1_8] = (Rd >> 4) | (Rd << 4);
		break;

	case 86: // 1001 0101 1010 1000		(1) WDR
		//watchdog is based on a RC oscillator
		//so add some random variation to simulate entropy
		watchdogTimer = rand() % 1024;
		if (prevWDR)
		{
			printf("WDR measured %u cycles\n", cycleCounter - prevWDR);
			prevWDR = 0;
		}
		else
		{
			prevWDR = cycleCounter + 1;
		}
		break;

	default: // Illegal op.
		ILLEGAL_OP;
		break;
	}

	// Process hardware for the last instruction cycle

	update_hardware_fast();

	// Run instruction precise emulation tasks

	update_hardware_ins();

	// Done, return cycles consumed during the processing of this instruction.

	return cycleCounter - startcy;
}

u16 avr8::decodeArg(u16 flash, u16 argMask, u8 argNeg)
{
	u16 argMaskShift = 0x0001;
	u16 decodeShift = 0x0001;
	u16 arg = 0x0000;

	while (argMaskShift != 0x4000)
	{ //0x4000 is highest bit in argMask that can be set
		if ((argMaskShift & argMask) != 0)
		{
			if ((argMaskShift & flash) != 0)
			{
				arg = arg | decodeShift;
			}
			decodeShift = decodeShift << 1;
		}
		argMaskShift = argMaskShift << 1;
	}
	decodeShift = decodeShift >> 1;

	if ((argNeg == 1) && ((decodeShift & arg) != 0))
	{
		while (decodeShift != 0x8000)
		{
			decodeShift = decodeShift << 1;
			arg = arg | decodeShift;
		}
	}

	return (arg);
}

void avr8::instructionDecode(u16 address)
{
	int i = 0;
	u16 rawFlash;
	u16 thisMask;
	u16 arg1;
	u16 arg2;

	rawFlash = progmem[address];

	instructionDecode_t thisInst;

	thisInst.opNum = 0;
	thisInst.arg1 = 0;
	thisInst.arg2 = 0;

	while (instructionList[i].opNum != 0)
	{
		thisMask = ~(instructionList[i].arg1Mask | instructionList[i].arg2Mask);

		if ((rawFlash & thisMask) == instructionList[i].mask)
		{

			arg1 = (decodeArg(rawFlash, instructionList[i].arg1Mask, instructionList[i].arg1Neg) * instructionList[i].arg1Mul) + instructionList[i].arg1Offset;
			arg2 = (decodeArg(rawFlash, instructionList[i].arg2Mask, instructionList[i].arg2Neg) * instructionList[i].arg2Mul) + instructionList[i].arg2Offset;

			if (instructionList[i].words == 2)
			{ // the 2 word instructions have k16 as the 2nd word of total 32bit instruction
				arg2 = progmem[address + 1];
			}

			//fprintf(stdout, instructionList[i].opName, arg1, arg2);
			//fprintf(stdout, "\n");

			thisInst.opNum = instructionList[i].opNum;
			thisInst.arg1 = arg1;
			thisInst.arg2 = arg2;

			progmemDecoded[address] = thisInst;
			return;
		}
		i++;
	}
	return;
}

void avr8::decodeFlash(void)
{
	for (u16 i = 0; i < (progSize / 2); i++)
	{
		decodeFlash(i);
	}
}
void avr8::decodeFlash(u16 address)
{
	if (address < (progSize / 2))
	{
		instructionDecode(address);
	}
}

void avr8::trigger_interrupt(unsigned int location)
{

	// clear interrupt flag
	store_bit_1(SREG, SREG_I, 0);

	// push current PC
	write_sram(SP, (u8)pc);
	DEC_SP;
	write_sram(SP, pc >> 8);
	DEC_SP;

	// jump to new location (which jumps to the real handler)
	pc = location;

	// bill the cycles consumed (3 cycles).
	// Note  that there is an error in the Atmega644 datasheet where
	// it specifies the IRQ cycles as 5.
	// see: http://www.avrfreaks.net/forum/interrupt-timing-conundrum
	update_hardware();
	update_hardware();
	update_hardware();
}

bool avr8::init_gui()
{
	left_edge_cycle = cycleCounter;
	scanline_top = -33 - 5;
	scanline_count = -999;
	//Syncronized with the kernel, this value now results in the image
	//being perfectly centered in both the emulator and a real TV
	left_edge = VIDEO_LEFT_EDGE;

	latched_buttons[0] = buttons[0] = ~0;
	latched_buttons[1] = buttons[1] = ~0;

	// Precompute final palette for speed.
	// Should build some NTSC compensation magic in here too.
	for (int i = 0; i < 256; i++)
	{
		int red = (((i >> 0) & 7) * 255) / 7;
		int green = (((i >> 3) & 7) * 255) / 7;
		int blue = (((i >> 6) & 3) * 255) / 3;
		palette[i] = 0xff000000 | red << 16 | green << 8 | blue;
	}

	return true;
}

// TODO:


// keydown: see keyboard.h
// uzeKbScanCodeQueue.push(...);
// keup: uzeKbScanCodeQueue.push(0xf0); uzeKbScanCodeQueue.push(...);

#ifdef SPI_DEBUG
char ascii(unsigned char ch)
{
	if (ch >= 32 && ch <= 127)
	{
		return ch;
	}
	return '.';
}

#endif

void avr8::update_spi()
{
	// SPI state machine
	switch (spiState)
	{
	case SPI_IDLE_STATE:
		if (spiByte == 0xff)
		{
			//SPI_DEBUG("Idle->0xff\n");
			SPDR = 0xff; // echo back that we're ready
			break;
		}
		spiCommand = spiByte;
		SPDR = 0x00;
		spiState = SPI_ARG_X_HI;
		break;
	case SPI_ARG_X_HI:
		SPI_DEBUG("x hi: %02X\n", spiByte);
		spiArgXhi = spiByte;
		SPDR = 0x00;
		spiState = SPI_ARG_X_LO;
		break;
	case SPI_ARG_X_LO:
		SPI_DEBUG("x lo: %02X\n", spiByte);
		spiArgXlo = spiByte;
		SPDR = 0x00;
		spiState = SPI_ARG_Y_HI;
		break;
	case SPI_ARG_Y_HI:
		SPI_DEBUG("y hi: %02X\n", spiByte);
		spiArgYhi = spiByte;
		SPDR = 0x00;
		spiState = SPI_ARG_Y_LO;
		break;
	case SPI_ARG_Y_LO:
		SPI_DEBUG("y lo: %02X\n", spiByte);
		spiArgYlo = spiByte;
		SPDR = 0x00;
		spiState = SPI_ARG_CRC;
		break;
	case SPI_ARG_CRC:
		SPI_DEBUG("SPI - CMD%d (%02X) X:%04X Y:%04X CRC: %02X\n", spiCommand ^ 0x40, spiCommand, spiArgX, spiArgY, spiByte);
		// ignore CRC and process commands
		switch (spiCommand)
		{
		case 0x40: //CMD0 =  RESET / GO_IDLE_STATE
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0xff; // 8 clock wait
			spiResponseBuffer[1] = 0x01; // send command response R1->idle flag
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 2;
			spiByteCount = 0;
			break;
		case 0x41: //CMD1 =  INIT / SEND_OP_COND
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0x00; // 8-clock wait
			spiResponseBuffer[1] = 0x00; // no error
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 2;
			spiByteCount = 0;
			break;

		case 0x48: //CMD8 =  INIT / SEND_IF_COND
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0xff;	  // 8-clock wait
			spiResponseBuffer[1] = 0x01;	  // return command response R7
			spiResponseBuffer[2] = 0x00;	  // return command response R7
			spiResponseBuffer[3] = 0x00;	  // return command response R7
			spiResponseBuffer[4] = 0x01;	  // return command response R7 voltage accepted
			spiResponseBuffer[5] = spiArgYlo; // return command response R7 check pattern
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 6;
			spiByteCount = 0;
			break;

		case 0x4c: //CMD12 =  STOP_TRANSMISSION
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0xff; //stuff byte
			spiResponseBuffer[1] = 0xff; //stuff byte
			spiResponseBuffer[2] = 0x00; // card is ready //in "trans" state
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 3;
			spiByteCount = 0;
			break;

		case 0x51: //CMD17 =  READ_BLOCK
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0xFF; // 8-clock wait
			spiResponseBuffer[1] = 0x00; // no error
			spiResponseBuffer[2] = 0xFE; // start block
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 3;
			//SDSeekToOffset(spiArg);
			spiByteCount = 512;
			break;
		case 0x52: //CMD18 =  MULTI_READ_BLOCK
			SPDR = 0x00;
			spiState = SPI_RESPOND_MULTI;
			spiResponseBuffer[0] = 0xFF; // 8-clock wait
			spiResponseBuffer[1] = 0x00; // no error
			spiResponseBuffer[2] = 0xFE; // start block
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 3;
			spiCommandDelay = 0;
			//SDSeekToOffset(spiArg);
			spiByteCount = 0;
			break;
		case 0x58: //CMD24 =  WRITE_BLOCK
			SPDR = 0x00;
			spiState = SPI_WRITE_SINGLE;
			spiResponseBuffer[0] = 0x00; // 8-clock wait
			spiResponseBuffer[1] = 0x00; // no error
			spiResponseBuffer[2] = 0xFE; // start block
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 3;
			//SDSeekToOffset(spiArg);
			spiByteCount = 512;
			break;

		case 0x69: //ACMD41 =  SD_SEND_OP_COND  (ACMD<n> is the command sequence of CMD55-CMD<n>)
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0xff; // 8 clock wait
			spiResponseBuffer[1] = 0x00; // send command response R1->OK
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 2;
			spiByteCount = 0;
			break;

		case 0x77: //CMD55 =  APP_CMD  (ACMD<n> is the command sequence of CMD55-CMD<n>)
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0xff; // 8 clock wait
			spiResponseBuffer[1] = 0x01; // send command response R1->idle flag
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 2;
			spiByteCount = 0;
			break;

		case 0x7A: //CMD58 =  READ_OCR
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0xff; // 8 clock wait
			spiResponseBuffer[1] = 0x00; // send command response R1->ok
			spiResponseBuffer[2] = 0x80; // return command response R3
			spiResponseBuffer[3] = 0xff; // return command response R3
			spiResponseBuffer[4] = 0x80; // return command response R3
			spiResponseBuffer[5] = 0x00; // return command response R3
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 6;
			spiByteCount = 0;
			break;

		default:
			printf("Unknown SPI command: %d\n", spiCommand);
			SPDR = 0x00;
			spiState = SPI_RESPOND_SINGLE;
			spiResponseBuffer[0] = 0x02; // data accepted
			spiResponseBuffer[1] = 0x05; //i illegal command
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 2;
			break;
		}
		break;

	case SPI_RESPOND_SINGLE:
		SPDR = *spiResponsePtr;
		SPI_DEBUG("SPI - Respond: %02X\n", SPDR);
		spiResponsePtr++;
		if (spiResponsePtr == spiResponseEnd)
		{
			if (spiByteCount != 0)
			{
				spiState = SPI_READ_SINGLE_BLOCK;
			}
			else
			{
				spiState = SPI_IDLE_STATE;
			}
		}
		break;

	case SPI_RESPOND_MULTI:
		if (spiCommandDelay != 0)
		{
			spiCommandDelay--;
			SPDR = 0xff;
			break;
		}

		SPDR = *spiResponsePtr;
		SPI_DEBUG("SPI - Respond: %02X\n", SPDR);
		spiResponsePtr++;

		if (SPDR == 0 && spiByteCount == 0)
		{
			spiCommandDelay = 250; //average delay based on a sample of cards
		}

		if (spiResponsePtr == spiResponseEnd)
		{
			spiState = SPI_READ_MULTIPLE_BLOCK;
			spiByteCount = 512;
		}
		break;

	case SPI_READ_SINGLE_BLOCK:
		SPDR = 0; //SDReadByte();
#ifdef USE_SPI_DEBUG
		{
			// output a nice display to see sector data
			int i = 512 - spiByteCount;
			int ofs = i & 0x000F;
			static unsigned char buf[16];
			if (i > 0 && (ofs == 0))
			{
				printf("%04X: ", i - 16);
				for (int j = 0; j < 16; j++)
					printf("%02X ", buf[j]);
				printf("| ");
				for (int j = 0; j < 16; j++)
					printf("%c", ascii(buf[j]));
				SPI_DEBUG("\n");
			}
			buf[ofs] = SPDR;
		}
#endif
		spiByteCount--;
		if (spiByteCount == 0)
		{
			spiResponseBuffer[0] = 0x00; //CRC
			spiResponseBuffer[1] = 0x00; //CRC
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 2;
			spiState = SPI_RESPOND_SINGLE;
		}
		break;

	case SPI_READ_MULTIPLE_BLOCK:
		if (SPDR == 0x4C)
		{ //CMD12 - stop multiple read transmission
			spiState = SPI_RESPOND_SINGLE;

			spiCommand = 0x4C;
			SPDR = 0x00;
			spiState = SPI_ARG_X_HI;
			spiByteCount = 0;
			break;
		}
		else
		{
			SPDR = 0; //SDReadByte();
		}
		SPI_DEBUG("SPI - Data[%d]: %02X\n", 512 - spiByteCount, SPDR);
		spiByteCount--;
		//inter-sector
		//NOTE: Current MoviePlayer.hex does not work with two delay bytes after the CRC. It has
		//been coded to work with a MicroSD card. These cards usually have only 1 delay byte after the CRC.
		//Uzem uses two delay bytes after the CRC since it is what regular SD cards does
		//and we want to emulate the "worst" case.
		if (spiByteCount == 0)
		{
			spiResponseBuffer[0] = 0x00; //CRC
			spiResponseBuffer[1] = 0x00; //CRC
			spiResponseBuffer[2] = 0xff; //delay
			spiResponseBuffer[3] = 0xff; //delay
			spiResponseBuffer[4] = 0xFE; // start block
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 5;
			spiArg += 512; // automatically move to next block
			//SDSeekToOffset(spiArg);
			spiByteCount = 512;
			spiState = SPI_RESPOND_MULTI;
		}
		break;

	case SPI_WRITE_SINGLE:
		SPDR = *spiResponsePtr;
		SPI_DEBUG("SPI - Respond: %02X\n", SPDR);
		spiResponsePtr++;
		if (spiResponsePtr == spiResponseEnd)
		{
			if (spiByteCount != 0)
			{
				spiState = SPI_WRITE_SINGLE_BLOCK;
			}
			else
			{
				spiState = SPI_IDLE_STATE;
			}
		}
		break;
	case SPI_WRITE_SINGLE_BLOCK:
		//SDWriteByte(SPDR);
		SPI_DEBUG("SPI - Data[%d]: %02X\n", spiByteCount, SPDR);
		SPDR = 0xFF;
		spiByteCount--;
		if (spiByteCount == 0)
		{
			spiResponseBuffer[0] = 0x00; //CRC
			spiResponseBuffer[1] = 0x00; //CRC
			spiResponsePtr = spiResponseBuffer;
			spiResponseEnd = spiResponsePtr + 2;
			spiState = SPI_RESPOND_SINGLE;
			//SDCommit();
		}
		break;
	}
}
