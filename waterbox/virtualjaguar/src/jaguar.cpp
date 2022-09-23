//
// JAGUAR.CPP
//
// Originally by David Raingeard (Cal2)
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Carwin Jones (BeOS)
// Cleanups and endian wrongness amelioration by James Hammons
// Note: Endian wrongness probably stems from the MAME origins of this emu and
//       the braindead way in which MAME handled memory when this was written. :-)
//
// JLH = James Hammons
//
// WHO  WHEN        WHAT
// ---  ----------  -----------------------------------------------------------
// JLH  11/25/2009  Major rewrite of memory subsystem and handlers
//

#include "jaguar.h"

#include <stdlib.h>
#include <time.h>
#include <string.h>
#include "blitter.h"
#include "cdhle.h"
#include "cdrom.h"
#include "dac.h"
#include "dsp.h"
#include "eeprom.h"
#include "event.h"
#include "gpu.h"
#include "jerry.h"
#include "joystick.h"
#include "m68000/m68kinterface.h"
#include "memtrack.h"
#include "settings.h"
#include "tom.h"

// Private function prototypes

unsigned jaguar_unknown_readbyte(unsigned address, uint32_t who = UNKNOWN);
unsigned jaguar_unknown_readword(unsigned address, uint32_t who = UNKNOWN);
void jaguar_unknown_writebyte(unsigned address, unsigned data, uint32_t who = UNKNOWN);
void jaguar_unknown_writeword(unsigned address, unsigned data, uint32_t who = UNKNOWN);

// External variables

// Really, need to include memory.h for this, but it might interfere with some stuff...
extern uint8_t jagMemSpace[];

// Internal variables

uint32_t jaguarMainROMCRC32, jaguarROMSize, jaguarRunAddress;
bool jaguarCartInserted = false;
bool lowerField = false;
bool jaguarCdInserted = false;

void M68KInstructionHook(void)
{
	if (jaguarCdInserted)
	{
		uint32_t pc = m68k_get_reg(NULL, M68K_REG_PC);
		if (pc >= 0x3000 && pc <= 0x306C)
		{
			CDHLEHook((pc - 0x3000) / 6);
			// return
			uint32_t sp = m68k_get_reg(NULL, M68K_REG_SP);
			m68k_set_reg(M68K_REG_PC, m68k_read_memory_32(sp));
			m68k_set_reg(M68K_REG_SP, sp + 4);
		}
	}

	if (__builtin_expect(!!TraceCallback, false))
	{
		uint32_t regs[18];
		for (uint32_t i = 0; i < 18; i++)
		{
			regs[i] = m68k_get_reg(NULL, (m68k_register_t)i);
		}
		TraceCallback(regs); 
	}

	MAYBE_CALLBACK(ExecuteCallback, m68k_get_reg(NULL, M68K_REG_PC));
}

//
// Custom UAE 68000 read/write/IRQ functions
//

int irq_ack_handler(int level)
{
	if (level == 2)
	{
		m68k_set_irq(0);
		return 64;
	}

	return M68K_INT_ACK_AUTOVECTOR;
}

unsigned int m68k_read_memory_8(unsigned int address)
{
	MAYBE_CALLBACK(ReadCallback, address);

	address &= 0x00FFFFFF;

	unsigned int retVal = 0;

	if ((address >= 0x000000) && (address <= 0x1FFFFF))
		retVal = jaguarMainRAM[address];
	else if ((address >= 0x800000) && (address <= 0xDFFEFF))
		retVal = jaguarMainROM[address - 0x800000];
	else if ((address >= 0xE00000) && (address <= 0xE3FFFF))
		retVal = jagMemSpace[address];
	else if ((address >= 0xDFFF00) && (address <= 0xDFFFFF))
		retVal = CDROMReadByte(address);
	else if ((address >= 0xF00000) && (address <= 0xF0FFFF))
		retVal = TOMReadByte(address, M68K);
	else if ((address >= 0xF10000) && (address <= 0xF1FFFF))
		retVal = JERRYReadByte(address, M68K);
	else
		retVal = jaguar_unknown_readbyte(address, M68K);

    return retVal;
}

unsigned int m68k_read_memory_16(unsigned int address)
{
	MAYBE_CALLBACK(ReadCallback, address);

	address &= 0x00FFFFFF;

    unsigned int retVal = 0;

	if ((address >= 0x000000) && (address <= 0x1FFFFE))
		retVal = GET16(jaguarMainRAM, address);
	else if ((address >= 0x800000) && (address <= 0xDFFEFE))
	{
		if (((TOMGetMEMCON1() & 0x0006) == (2 << 1)) && (jaguarMainROMCRC32 == 0xFDF37F47))
		{
			retVal = MTReadWord(address);
		}
		else
			retVal = (jaguarMainROM[address - 0x800000] << 8)
				| jaguarMainROM[address - 0x800000 + 1];
	}
	else if ((address >= 0xE00000) && (address <= 0xE3FFFE))
		retVal = (jagMemSpace[address] << 8) | jagMemSpace[address + 1];
	else if ((address >= 0xDFFF00) && (address <= 0xDFFFFE))
		retVal = CDROMReadWord(address, M68K);
	else if ((address >= 0xF00000) && (address <= 0xF0FFFE))
		retVal = TOMReadWord(address, M68K);
	else if ((address >= 0xF10000) && (address <= 0xF1FFFE))
		retVal = JERRYReadWord(address, M68K);
	else
		retVal = jaguar_unknown_readword(address, M68K);

    return retVal;
}

unsigned int m68k_read_memory_32(unsigned int address)
{
	MAYBE_CALLBACK(ReadCallback, address);

	address &= 0x00FFFFFF;

	uint32_t retVal = 0;

	if ((address >= 0x800000) && (address <= 0xDFFEFE))
	{
		if (((TOMGetMEMCON1() & 0x0006) == (2 << 1)) && (jaguarMainROMCRC32 == 0xFDF37F47))
			retVal = MTReadLong(address);
		else
			retVal = GET32(jaguarMainROM, address - 0x800000);

		return retVal;
	}

	return (m68k_read_memory_16(address) << 16) | m68k_read_memory_16(address + 2);
}

void m68k_write_memory_8(unsigned int address, unsigned int value)
{
	MAYBE_CALLBACK(WriteCallback, address);

	address &= 0x00FFFFFF;

	if ((address >= 0x000000) && (address <= 0x1FFFFF))
		jaguarMainRAM[address] = value;
	else if ((address >= 0xDFFF00) && (address <= 0xDFFFFF))
		CDROMWriteByte(address, value, M68K);
	else if ((address >= 0xF00000) && (address <= 0xF0FFFF))
		TOMWriteByte(address, value, M68K);
	else if ((address >= 0xF10000) && (address <= 0xF1FFFF))
		JERRYWriteByte(address, value, M68K);
	else
		jaguar_unknown_writebyte(address, value, M68K);
}

void m68k_write_memory_16(unsigned int address, unsigned int value)
{
	MAYBE_CALLBACK(WriteCallback, address);

	address &= 0x00FFFFFF;

	if ((address >= 0x000000) && (address <= 0x1FFFFE))
	{
		SET16(jaguarMainRAM, address, value);
	}
	else if ((address >= 0x800000) && (address <= 0x87FFFE))
	{
		if (((TOMGetMEMCON1() & 0x0006) == (2 << 1)) && (jaguarMainROMCRC32 == 0xFDF37F47))
			MTWriteWord(address, value);
	}
	else if ((address >= 0xDFFF00) && (address <= 0xDFFFFE))
		CDROMWriteWord(address, value, M68K);
	else if ((address >= 0xF00000) && (address <= 0xF0FFFE))
		TOMWriteWord(address, value, M68K);
	else if ((address >= 0xF10000) && (address <= 0xF1FFFE))
		JERRYWriteWord(address, value, M68K);
	else
	{
		jaguar_unknown_writeword(address, value, M68K);
	}
}

void m68k_write_memory_32(unsigned int address, unsigned int value)
{
	address &= 0x00FFFFFF;

	m68k_write_memory_16(address, value >> 16);
	m68k_write_memory_16(address + 2, value & 0xFFFF);
}

uint32_t JaguarGetHandler(uint32_t i)
{
	return JaguarReadLong(i * 4);
}

//
// Unknown read/write byte/word routines
//

void jaguar_unknown_writebyte(unsigned address, unsigned data, uint32_t who)
{
}

void jaguar_unknown_writeword(unsigned address, unsigned data, uint32_t who)
{
}

unsigned jaguar_unknown_readbyte(unsigned address, uint32_t who)
{
    return 0xFF;
}

unsigned jaguar_unknown_readword(unsigned address, uint32_t who)
{
    return 0xFFFF;
}

//
// Disassemble M68K instructions at the given offset
//

unsigned int m68k_read_disassembler_8(unsigned int address)
{
	return m68k_read_memory_8(address);
}

unsigned int m68k_read_disassembler_16(unsigned int address)
{
	return m68k_read_memory_16(address);
}

unsigned int m68k_read_disassembler_32(unsigned int address)
{
	return m68k_read_memory_32(address);
}

uint8_t JaguarReadByte(uint32_t offset, uint32_t who)
{
	uint8_t data = 0x00;
	offset &= 0xFFFFFF;

	if (offset < 0x800000)
		data = jaguarMainRAM[offset & 0x1FFFFF];
	else if ((offset >= 0x800000) && (offset < 0xDFFF00))
		data = jaguarMainROM[offset - 0x800000];
	else if ((offset >= 0xDFFF00) && (offset <= 0xDFFFFF))
		data = CDROMReadByte(offset, who);
	else if ((offset >= 0xE00000) && (offset < 0xE40000))
		data = jagMemSpace[offset];
	else if ((offset >= 0xF00000) && (offset < 0xF10000))
		data = TOMReadByte(offset, who);
	else if ((offset >= 0xF10000) && (offset < 0xF20000))
		data = JERRYReadByte(offset, who);
	else
		data = jaguar_unknown_readbyte(offset, who);

	return data;
}

uint16_t JaguarReadWord(uint32_t offset, uint32_t who)
{
	offset &= 0xFFFFFF;

	if (offset < 0x800000)
	{
		return (jaguarMainRAM[(offset+0) & 0x1FFFFF] << 8) | jaguarMainRAM[(offset+1) & 0x1FFFFF];
	}
	else if ((offset >= 0x800000) && (offset < 0xDFFF00))
	{
		offset -= 0x800000;
		return (jaguarMainROM[offset+0] << 8) | jaguarMainROM[offset+1];
	}
	else if ((offset >= 0xDFFF00) && (offset <= 0xDFFFFE))
		return CDROMReadWord(offset, who);
	else if ((offset >= 0xE00000) && (offset <= 0xE3FFFE))
		return (jagMemSpace[offset + 0] << 8) | jagMemSpace[offset + 1];
	else if ((offset >= 0xF00000) && (offset <= 0xF0FFFE))
		return TOMReadWord(offset, who);
	else if ((offset >= 0xF10000) && (offset <= 0xF1FFFE))
		return JERRYReadWord(offset, who);

	return jaguar_unknown_readword(offset, who);
}

void JaguarWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	offset &= 0xFFFFFF;

	if (offset < 0x800000)
	{
		jaguarMainRAM[offset & 0x1FFFFF] = data;
		return;
	}
	else if ((offset >= 0xDFFF00) && (offset <= 0xDFFFFF))
	{
		CDROMWriteByte(offset, data, who);
		return;
	}
	else if ((offset >= 0xF00000) && (offset <= 0xF0FFFF))
	{
		TOMWriteByte(offset, data, who);
		return;
	}
	else if ((offset >= 0xF10000) && (offset <= 0xF1FFFF))
	{
		JERRYWriteByte(offset, data, who);
		return;
	}

	jaguar_unknown_writebyte(offset, data, who);
}

void JaguarWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	offset &= 0xFFFFFF;

	if (offset <= 0x7FFFFE)
	{
		jaguarMainRAM[(offset+0) & 0x1FFFFF] = data >> 8;
		jaguarMainRAM[(offset+1) & 0x1FFFFF] = data & 0xFF;
		return;
	}
	else if (offset >= 0xDFFF00 && offset <= 0xDFFFFE)
	{
		CDROMWriteWord(offset, data, who);
		return;
	}
	else if (offset >= 0xF00000 && offset <= 0xF0FFFE)
	{
		TOMWriteWord(offset, data, who);
		return;
	}
	else if (offset >= 0xF10000 && offset <= 0xF1FFFE)
	{
		JERRYWriteWord(offset, data, who);
		return;
	}
	else if (offset >= 0x800000 && offset <= 0xEFFFFF)
		return;

	jaguar_unknown_writeword(offset, data, who);
}

uint32_t JaguarReadLong(uint32_t offset, uint32_t who)
{
	return (JaguarReadWord(offset, who) << 16) | JaguarReadWord(offset+2, who);
}

void JaguarWriteLong(uint32_t offset, uint32_t data, uint32_t who)
{
	JaguarWriteWord(offset, data >> 16, who);
	JaguarWriteWord(offset+2, data & 0xFFFF, who);
}

void JaguarSetScreenBuffer(uint32_t * buffer)
{
	screenBuffer = buffer;
}

void JaguarSetScreenPitch(uint32_t pitch)
{
	screenPitch = pitch;
}

//
// Jaguar console initialization
//
void JaguarInit(void)
{
	srand(time(NULL));

	for(uint32_t i=0; i<0x200000; i+=4)
		*((uint32_t *)(&jaguarMainRAM[i])) = rand();

	lowerField = false;
	memset(jaguarMainRAM + 0x804, 0xFF, 4);

	m68k_pulse_reset();
	GPUInit();
	DSPInit();
	TOMInit();
	JERRYInit();
	CDROMInit();
	CDHLEInit();
}

void HalflineCallback(void);
void RenderCallback(void);

void JaguarReset(void)
{
	for(uint32_t i=8; i<0x200000; i+=4)
		*((uint32_t *)(&jaguarMainRAM[i])) = rand();

	InitializeEventList();

	if (vjs.useJaguarBIOS && jaguarCartInserted && !vjs.hardwareTypeAlpine)
		memcpy(jaguarMainRAM, jagMemSpace + 0xE00000, 8);
	else
		SET32(jaguarMainRAM, 4, jaguarRunAddress);

	TOMReset();
	JERRYReset();
	GPUReset();
	DSPReset();
	CDROMReset();
	CDHLEReset();
	m68k_pulse_reset();

	lowerField = false;
	SetCallbackTime(HalflineCallback, (vjs.hardwareTypeNTSC ? 31.777777777 : 32.0));
}

void JaguarDone(void)
{
	CDHLEDone();
	CDROMDone();
	GPUDone();
	DSPDone();
	TOMDone();
	JERRYDone();
}

//
// New Jaguar execution stack
// This executes 1 frame's worth of code.
//
bool frameDone;
void JaguarExecuteNew(void)
{
	frameDone = false;

	do
	{
		double timeToNextEvent = GetTimeToNextEvent();

		m68k_execute(USEC_TO_M68K_CYCLES(timeToNextEvent));

		GPUExec(USEC_TO_RISC_CYCLES(timeToNextEvent));

		HandleNextEvent();
 	}
	while (!frameDone);
}

//
// The thing to keep in mind is that the VC is advanced every HALF line,
// regardless of whether the display is interlaced or not. The only difference
// with an interlaced display is that the high bit of VC will be set when the
// lower field is being rendered. (NB: The high bit of VC is ALWAYS set on the
// lower field, regardless of whether it's in interlace mode or not.
// NB2: Seems it doesn't always, not sure what the constraint is...)
//
// Normally, TVs will render a full frame in 1/30s (NTSC) or 1/25s (PAL) by
// rendering two fields that are slighty vertically offset from each other.
// Each field is created in 1/60s (NTSC) or 1/50s (PAL), and every other line
// is rendered in this mode so that each field, when overlaid on each other,
// will yield the final picture at the full resolution for the full frame.
//
// We execute a half frame in each timeslice (1/60s NTSC, 1/50s PAL).
// Since the number of lines in a FULL frame is 525 for NTSC, 625 for PAL,
// it will be half this number for a half frame. BUT, since we're counting
// HALF lines, we double this number and we're back at 525 for NTSC, 625 for
// PAL.
//
// Scanline times are 63.5555... μs in NTSC and 64 μs in PAL
// Half line times are, naturally, half of this. :-P
//
void HalflineCallback(void)
{
	uint16_t vc = TOMReadWord(0xF00006, JAGUAR);
	uint16_t vp = TOMReadWord(0xF0003E, JAGUAR) + 1;
	uint16_t vi = TOMReadWord(0xF0004E, JAGUAR);
	vc++;

	uint16_t numHalfLines = ((vjs.hardwareTypeNTSC ? 525 : 625) * 2) / 2;

	if ((vc & 0x7FF) >= numHalfLines)
	{
		lowerField = !lowerField;
		vc = (lowerField ? 0x0800 : 0x0000);
	}

	TOMWriteWord(0xF00006, vc, JAGUAR);

	if ((vc & 0x7FF) == vi && (vc & 0x7FF) > 0 && TOMIRQEnabled(IRQ_VIDEO))
	{
		TOMSetPendingVideoInt();
		m68k_set_irq(2);
	}

	TOMExecHalfline(vc, true);

	if ((vc & 0x7FF) == 0)
	{
		JoystickExec();
		frameDone = true;
	}

	SetCallbackTime(HalflineCallback, (vjs.hardwareTypeNTSC ? 31.777777777 : 32.0));
}
