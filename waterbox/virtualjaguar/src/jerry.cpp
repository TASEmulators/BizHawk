//
// JERRY Core
//
// Originally by David Raingeard
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Carwin Jones (BeOS)
// Cleanups/rewrites/fixes by James Hammons
//
// JLH = James Hammons <jlhamm@acm.org>
//
// WHO  WHEN        WHAT
// ---  ----------  -----------------------------------------------------------
// JLH  11/25/2009  Major rewrite of memory subsystem and handlers
//

#include "jerry.h"

#include <string.h>
#include "cdrom.h"
#include "dac.h"
#include "dsp.h"
#include "eeprom.h"
#include "event.h"
#include "jaguar.h"
#include "joystick.h"
#include "m68000/m68kinterface.h"
#include "memtrack.h"
#include "settings.h"
#include "tom.h"
#include "wavetable.h"
#include "cdhle.h"

uint8_t jerry_ram_8[0x10000];

// JERRY Registers (write, offset from $F10000)
#define JPIT1		0x00
#define JPIT2		0x02
#define JPIT3		0x04
#define JPIT4		0x08
#define CLK1		0x10
#define CLK2		0x12
#define CLK3		0x14
#define JINTCTRL	0x20
#define ASIDATA		0x30
#define ASICTRL		0x32
#define ASICLK		0x34
#define SCLK		0xA150
#define SMODE		0xA154

static uint32_t JERRYPIT1Prescaler;
static uint32_t JERRYPIT1Divider;
static uint32_t JERRYPIT2Prescaler;
static uint32_t JERRYPIT2Divider;
static int32_t jerry_timer_1_counter;
static int32_t jerry_timer_2_counter;

int32_t JERRYI2SInterruptTimer = -1;
static uint32_t jerryI2SCycles;

static uint16_t jerryInterruptMask = 0;
static uint16_t jerryPendingInterrupt = 0;

static void JERRYResetPIT1(void);
static void JERRYResetPIT2(void);
static void JERRYResetI2S(void);

static void JERRYPIT1Callback(void);
static void JERRYPIT2Callback(void);

void JERRYResetI2S(void)
{
	sclk = 8;
	JERRYI2SInterruptTimer = -1;
}

void JERRYResetPIT1(void)
{
	RemoveCallback(JERRYPIT1Callback);

	if (JERRYPIT1Prescaler | JERRYPIT1Divider)
	{
		double usecs = (float)(JERRYPIT1Prescaler + 1) * (float)(JERRYPIT1Divider + 1) * RISC_CYCLE_IN_USEC;
		SetCallbackTime(JERRYPIT1Callback, usecs);
	}
}

void JERRYResetPIT2(void)
{
	RemoveCallback(JERRYPIT2Callback);

	if (JERRYPIT1Prescaler | JERRYPIT1Divider)
	{
		double usecs = (float)(JERRYPIT2Prescaler + 1) * (float)(JERRYPIT2Divider + 1) * RISC_CYCLE_IN_USEC;
		SetCallbackTime(JERRYPIT2Callback, usecs);
	}
}

void JERRYPIT1Callback(void)
{
	if (TOMIRQEnabled(IRQ_DSP))
	{
		if (jerryInterruptMask & IRQ2_TIMER1)
		{
			jerryPendingInterrupt |= IRQ2_TIMER1;
			m68k_set_irq(2);
		}
	}

	DSPSetIRQLine(DSPIRQ_TIMER0, ASSERT_LINE);
	JERRYResetPIT1();
}

void JERRYPIT2Callback(void)
{
	if (TOMIRQEnabled(IRQ_DSP))
	{
		if (jerryInterruptMask & IRQ2_TIMER2)
		{
			jerryPendingInterrupt |= IRQ2_TIMER2;
			m68k_set_irq(2);
		}
	}

	DSPSetIRQLine(DSPIRQ_TIMER1, ASSERT_LINE);
	JERRYResetPIT2();
}

void JERRYI2SCallback(void)
{
	jerryI2SCycles = 32 * (2 * (sclk + 1));

	if (smode & SMODE_INTERNAL)
	{
		DSPSetIRQLine(DSPIRQ_SSI, ASSERT_LINE);
		double usecs = (float)jerryI2SCycles * (vjs.hardwareTypeNTSC ? RISC_CYCLE_IN_USEC : RISC_CYCLE_PAL_IN_USEC);
		SetCallbackTime(JERRYI2SCallback, usecs);
	}
	else
	{
		if (CDHLEJerryCallback())
		{
			DSPSetIRQLine(DSPIRQ_SSI, ASSERT_LINE);
		}
		SetCallbackTime(JERRYI2SCallback, 22.675737);
	}
}

void JERRYInit(void)
{
	JoystickInit();
	MTInit();
	memcpy(&jerry_ram_8[0xD000], waveTableROM, 0x1000);

	JERRYPIT1Prescaler = 0xFFFF;
	JERRYPIT2Prescaler = 0xFFFF;
	JERRYPIT1Divider = 0xFFFF;
	JERRYPIT2Divider = 0xFFFF;
	jerryInterruptMask = 0x0000;
	jerryPendingInterrupt = 0x0000;

	DACInit();
}

void JERRYReset(void)
{
	JoystickReset();
	EepromReset();
	MTReset();
	JERRYResetI2S();

	memset(jerry_ram_8, 0x00, 0xD000);
	JERRYPIT1Prescaler = 0xFFFF;
	JERRYPIT2Prescaler = 0xFFFF;
	JERRYPIT1Divider = 0xFFFF;
	JERRYPIT2Divider = 0xFFFF;
	jerry_timer_1_counter = 0;
	jerry_timer_2_counter = 0;
	jerryInterruptMask = 0x0000;
	jerryPendingInterrupt = 0x0000;

	DACReset();
}

bool JERRYIRQEnabled(int irq)
{
	return jerryInterruptMask & irq;
}

void JERRYSetPendingIRQ(int irq)
{
	jerryPendingInterrupt |= irq;
}

//
// JERRY byte access (read)
//
uint8_t JERRYReadByte(uint32_t offset, uint32_t who)
{
	if ((offset >= DSP_CONTROL_RAM_BASE) && (offset < DSP_CONTROL_RAM_BASE+0x20))
		return DSPReadByte(offset, who);
	else if ((offset >= DSP_WORK_RAM_BASE) && (offset < DSP_WORK_RAM_BASE+0x2000))
		return DSPReadByte(offset, who);
	else if (offset >= 0xF1A148 && offset <= 0xF1A153)
		return DACReadByte(offset, who);
	else if (offset >= 0xF14000 && offset <= 0xF14003)
	{
		uint16_t value = JoystickReadWord(offset & 0xFE);

		if (offset & 0x01)
			value &= 0xFF;
		else
			value >>= 8;

		return value | EepromReadByte(offset);
	}
	else if (offset >= 0xF14000 && offset <= 0xF1A0FF)
		return EepromReadByte(offset);

	return jerry_ram_8[offset & 0xFFFF];
}

//
// JERRY word access (read)
//
uint16_t JERRYReadWord(uint32_t offset, uint32_t who)
{
	if ((offset >= DSP_CONTROL_RAM_BASE) && (offset < DSP_CONTROL_RAM_BASE+0x20))
		return DSPReadWord(offset, who);
	else if (offset >= DSP_WORK_RAM_BASE && offset <= DSP_WORK_RAM_BASE + 0x1FFF)
		return DSPReadWord(offset, who);
	else if (offset >= 0xF1A148 && offset <= 0xF1A153)
		return DACReadWord(offset, who);
	else if (offset == 0xF10020)
		return jerryPendingInterrupt;
	else if (offset == 0xF14000)
		return (JoystickReadWord(offset) & 0xFFFE) | EepromReadWord(offset);
	else if ((offset >= 0xF14002) && (offset < 0xF14003))
		return JoystickReadWord(offset);
	else if ((offset >= 0xF14000) && (offset <= 0xF1A0FF))
		return EepromReadWord(offset);

	offset &= 0xFFFF;
	return ((uint16_t)jerry_ram_8[offset+0] << 8) | jerry_ram_8[offset+1];
}

//
// JERRY byte access (write)
//
void JERRYWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	if ((offset >= DSP_CONTROL_RAM_BASE) && (offset < DSP_CONTROL_RAM_BASE + 0x20))
	{
		DSPWriteByte(offset, data, who);
	}
	else if ((offset >= DSP_WORK_RAM_BASE) && (offset < DSP_WORK_RAM_BASE + 0x2000))
	{
		DSPWriteByte(offset, data, who);
	}
	else if (offset >= 0xF1A148 && offset <= 0xF1A157)
	{
		DACWriteByte(offset, data, who);
	}
	else if (offset >= 0xF10020 && offset <= 0xF10021)
	{
		if (offset == 0xF10020)
		{
			jerryPendingInterrupt &= ~data;
		}
		else if (offset == 0xF10021)
			jerryInterruptMask = data;
	}
	else if ((offset >= 0xF14000) && (offset <= 0xF14003))
	{
		JoystickWriteWord(offset & 0xFE, (uint16_t)data);
		EepromWriteByte(offset, data);
	}
	else if ((offset >= 0xF14000) && (offset <= 0xF1A0FF))
	{
		EepromWriteByte(offset, data);
	}

	if (offset >= 0xF1D000 && offset <= 0xF1DFFF)
		return;

	jerry_ram_8[offset & 0xFFFF] = data;
}

//
// JERRY word access (write)
//
void JERRYWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	if ((offset >= DSP_CONTROL_RAM_BASE) && (offset < DSP_CONTROL_RAM_BASE + 0x20))
	{
		DSPWriteWord(offset, data, who);
	}
	else if ((offset >= DSP_WORK_RAM_BASE) && (offset < DSP_WORK_RAM_BASE + 0x2000))
	{
		DSPWriteWord(offset, data, who);
	}
	else if (offset >= 0xF1A148 && offset <= 0xF1A156)
	{
		DACWriteWord(offset, data, who);
	}
	else if (offset >= 0xF10000 && offset <= 0xF10007)
	{
		switch(offset & 0x07)
		{
			case 0:
				JERRYPIT1Prescaler = data;
				JERRYResetPIT1();
				break;
			case 2:
				JERRYPIT1Divider = data;
				JERRYResetPIT1();
				break;
			case 4:
				JERRYPIT2Prescaler = data;
				JERRYResetPIT2();
				break;
			case 6:
				JERRYPIT2Divider = data;
				JERRYResetPIT2();
				break;
		}
	}
	else if (offset >= 0xF10020 && offset <= 0xF10022)
	{
		jerryInterruptMask = data & 0xFF;
		jerryPendingInterrupt &= ~(data >> 8);
	}
	else if (offset >= 0xF14000 && offset < 0xF14003)
	{
		JoystickWriteWord(offset, data);
		EepromWriteWord(offset, data);
	}
	else if (offset >= 0xF14000 && offset <= 0xF1A0FF)
	{
		EepromWriteWord(offset, data);
	}

	if (offset >= 0xF1D000 && offset <= 0xF1DFFF)
		return;

	jerry_ram_8[(offset+0) & 0xFFFF] = (data >> 8) & 0xFF;
	jerry_ram_8[(offset+1) & 0xFFFF] = data & 0xFF;
}
