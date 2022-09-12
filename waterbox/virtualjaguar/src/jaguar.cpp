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

#include <time.h>
#include <string.h>
#include "blitter.h"
#include "cdrom.h"
#include "dac.h"
#include "dsp.h"
#include "eeprom.h"
#include "event.h"
#include "foooked.h"
#include "gpu.h"
#include "jerry.h"
#include "joystick.h"
#include "log.h"
#include "m68000/m68kinterface.h"
//#include "memory.h"
#include "memtrack.h"
#include "mmu.h"
#include "settings.h"
#include "tom.h"

#define CPU_DEBUG
//Do this in makefile??? Yes! Could, but it's easier to define here...
//#define LOG_UNMAPPED_MEMORY_ACCESSES
//#define ABORT_ON_UNMAPPED_MEMORY_ACCESS
//#define ABORT_ON_ILLEGAL_INSTRUCTIONS
//#define ABORT_ON_OFFICIAL_ILLEGAL_INSTRUCTION
//#define CPU_DEBUG_MEMORY
//#define LOG_CD_BIOS_CALLS
//#define CPU_DEBUG_TRACING
#define ALPINE_FUNCTIONS

// Private function prototypes

unsigned jaguar_unknown_readbyte(unsigned address, uint32_t who = UNKNOWN);
unsigned jaguar_unknown_readword(unsigned address, uint32_t who = UNKNOWN);
void jaguar_unknown_writebyte(unsigned address, unsigned data, uint32_t who = UNKNOWN);
void jaguar_unknown_writeword(unsigned address, unsigned data, uint32_t who = UNKNOWN);
void M68K_show_context(void);

// External variables

#ifdef CPU_DEBUG_MEMORY
extern bool startMemLog;							// Set by "e" key
extern int effect_start;
extern int effect_start2, effect_start3, effect_start4, effect_start5, effect_start6;
#endif

// Really, need to include memory.h for this, but it might interfere with some stuff...
extern uint8_t jagMemSpace[];

// Internal variables

uint32_t jaguar_active_memory_dumps = 0;

uint32_t jaguarMainROMCRC32, jaguarROMSize, jaguarRunAddress;
bool jaguarCartInserted = false;
bool lowerField = false;

#ifdef CPU_DEBUG_MEMORY
uint8_t writeMemMax[0x400000], writeMemMin[0x400000];
uint8_t readMem[0x400000];
uint32_t returnAddr[4000], raPtr = 0xFFFFFFFF;
#endif

uint32_t pcQueue[0x400];
uint32_t a0Queue[0x400];
uint32_t a1Queue[0x400];
uint32_t a2Queue[0x400];
uint32_t a3Queue[0x400];
uint32_t a4Queue[0x400];
uint32_t a5Queue[0x400];
uint32_t a6Queue[0x400];
uint32_t a7Queue[0x400];
uint32_t d0Queue[0x400];
uint32_t d1Queue[0x400];
uint32_t d2Queue[0x400];
uint32_t d3Queue[0x400];
uint32_t d4Queue[0x400];
uint32_t d5Queue[0x400];
uint32_t d6Queue[0x400];
uint32_t d7Queue[0x400];
uint32_t srQueue[0x400];
uint32_t pcQPtr = 0;
bool startM68KTracing = false;

// Breakpoint on memory access vars (exported)
bool bpmActive = false;
uint32_t bpmAddress1;


//
// Callback function to detect illegal instructions
//
void GPUDumpDisassembly(void);
void GPUDumpRegisters(void);
static bool start = false;

void M68KInstructionHook(void)
{
	uint32_t m68kPC = m68k_get_reg(NULL, M68K_REG_PC);
// Temp, for comparing...
{
/*	static char buffer[2048];//, mem[64];
	m68k_disassemble(buffer, m68kPC, M68K_CPU_TYPE_68000);
	printf("%08X: %s\n", m68kPC, buffer);//*/
}
//JaguarDasm(m68kPC, 1);
//Testing Hover Strike...
#if 0
//Dasm(regs.pc, 1);
static int hitCount = 0;
static int inRoutine = 0;
static int instSeen;

//if (regs.pc == 0x80340A)
if (m68kPC == 0x803416)
{
	hitCount++;
	inRoutine = 1;
	instSeen = 0;
	printf("%i: $80340A start. A0=%08X, A1=%08X ", hitCount, m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_A1));
}
else if (m68kPC == 0x803422)
{
	inRoutine = 0;
	printf("(%i instructions)\n", instSeen);
}

if (inRoutine)
	instSeen++;
#endif

// For code tracing...
#ifdef CPU_DEBUG_TRACING
	if (startM68KTracing)
	{
		static char buffer[2048];

		m68k_disassemble(buffer, m68kPC, 0);
		WriteLog("%06X: %s\n", m68kPC, buffer);
	}
#endif

// For tracebacks...
// Ideally, we'd save all the registers as well...
	pcQueue[pcQPtr] = m68kPC;
	a0Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A0);
	a1Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A1);
	a2Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A2);
	a3Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A3);
	a4Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A4);
	a5Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A5);
	a6Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A6);
	a7Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_A7);
	d0Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D0);
	d1Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D1);
	d2Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D2);
	d3Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D3);
	d4Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D4);
	d5Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D5);
	d6Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D6);
	d7Queue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_D7);
	srQueue[pcQPtr] = m68k_get_reg(NULL, M68K_REG_SR);
	pcQPtr++;
	pcQPtr &= 0x3FF;

	if (m68kPC & 0x01)		// Oops! We're fetching an odd address!
	{
		/*WriteLog("M68K: Attempted to execute from an odd address!\n\nBacktrace:\n\n");

		static char buffer[2048];
		for(int i=0; i<0x400; i++)
		{
//			WriteLog("[A2=%08X, D0=%08X]\n", a2Queue[(pcQPtr + i) & 0x3FF], d0Queue[(pcQPtr + i) & 0x3FF]);
			WriteLog("[A0=%08X, A1=%08X, A2=%08X, A3=%08X, A4=%08X, A5=%08X, A6=%08X, A7=%08X, D0=%08X, D1=%08X, D2=%08X, D3=%08X, D4=%08X, D5=%08X, D6=%08X, D7=%08X, SR=%04X]\n", a0Queue[(pcQPtr + i) & 0x3FF], a1Queue[(pcQPtr + i) & 0x3FF], a2Queue[(pcQPtr + i) & 0x3FF], a3Queue[(pcQPtr + i) & 0x3FF], a4Queue[(pcQPtr + i) & 0x3FF], a5Queue[(pcQPtr + i) & 0x3FF], a6Queue[(pcQPtr + i) & 0x3FF], a7Queue[(pcQPtr + i) & 0x3FF], d0Queue[(pcQPtr + i) & 0x3FF], d1Queue[(pcQPtr + i) & 0x3FF], d2Queue[(pcQPtr + i) & 0x3FF], d3Queue[(pcQPtr + i) & 0x3FF], d4Queue[(pcQPtr + i) & 0x3FF], d5Queue[(pcQPtr + i) & 0x3FF], d6Queue[(pcQPtr + i) & 0x3FF], d7Queue[(pcQPtr + i) & 0x3FF], srQueue[(pcQPtr + i) & 0x3FF]);
			m68k_disassemble(buffer, pcQueue[(pcQPtr + i) & 0x3FF], 0);//M68K_CPU_TYPE_68000);
			WriteLog("\t%08X: %s\n", pcQueue[(pcQPtr + i) & 0x3FF], buffer);
		}
		WriteLog("\n");

		uint32_t topOfStack = m68k_get_reg(NULL, M68K_REG_A7);
		WriteLog("M68K: Top of stack: %08X. Stack trace:\n", JaguarReadLong(topOfStack));
		for(int i=0; i<10; i++)
			WriteLog("%06X: %08X\n", topOfStack - (i * 4), JaguarReadLong(topOfStack - (i * 4)));
		WriteLog("Jaguar: VBL interrupt is %s\n", ((TOMIRQEnabled(IRQ_VIDEO)) && (JaguarInterruptHandlerIsValid(64))) ? "enabled" : "disabled");
		M68K_show_context();
		LogDone();
		exit(0);*/
	}

	// Disassemble everything
/*	{
		static char buffer[2048];
		m68k_disassemble(buffer, m68kPC, M68K_CPU_TYPE_68000);
		WriteLog("%08X: %s", m68kPC, buffer);
		WriteLog("\t\tA0=%08X, A1=%08X, D0=%08X, D1=%08X\n",
			m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_A1),
			m68k_get_reg(NULL, M68K_REG_D0), m68k_get_reg(NULL, M68K_REG_D1));
	}//*/
/*	if (m68kPC >= 0x807EC4 && m68kPC <= 0x807EDB)
	{
		static char buffer[2048];
		m68k_disassemble(buffer, m68kPC, M68K_CPU_TYPE_68000);
		WriteLog("%08X: %s", m68kPC, buffer);
		WriteLog("\t\tA0=%08X, A1=%08X, D0=%08X, D1=%08X\n",
			m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_A1),
			m68k_get_reg(NULL, M68K_REG_D0), m68k_get_reg(NULL, M68K_REG_D1));
	}//*/
/*	if (m68kPC == 0x8D0E48 && effect_start5)
	{
		WriteLog("\nM68K: At collision detection code. Exiting!\n\n");
		GPUDumpRegisters();
		GPUDumpDisassembly();
		log_done();
		exit(0);
	}//*/
/*	uint16_t opcode = JaguarReadWord(m68kPC);
	if (opcode == 0x4E75)	// RTS
	{
		if (startMemLog)
//			WriteLog("Jaguar: Returning from subroutine to %08X\n", JaguarReadLong(m68k_get_reg(NULL, M68K_REG_A7)));
		{
			uint32_t addr = JaguarReadLong(m68k_get_reg(NULL, M68K_REG_A7));
			bool found = false;
			if (raPtr != 0xFFFFFFFF)
			{
				for(uint32_t i=0; i<=raPtr; i++)
				{
					if (returnAddr[i] == addr)
					{
						found = true;
						break;
					}
				}
			}

			if (!found)
				returnAddr[++raPtr] = addr;
		}
	}//*/

//Flip Out! debugging...
//805F46, 806486
/*
00805FDC: movea.l #$9c6f8, A0 		D0=00100010, A0=00100000
00805FE2: move.w  #$10, (A0)+ 		D0=00100010, A0=0009C6F8
00805FE6: cmpa.l  #$c96f8, A0 		D0=00100010, A0=0009C6FA
00805FEC: bne     805fe2 		D0=00100010, A0=0009C6FA

0080603A: move.l  #$11ed7c, $100.w 		D0=61700080, A0=000C96F8, D1=00000000, A1=000040D8

0012314C: move.l  (A0)+, (A1)+ 		D0=61700080, A0=00124174, D1=00000000, A1=00F03FFC
0012314E: cmpa.l  #$f04000, A1 		D0=61700080, A0=00124178, D1=00000000, A1=00F04000
00123154: blt     12314c 		D0=61700080, A0=00124178, D1=00000000, A1=00F04000
00123156: move.l  #$0, $f035d0.l 		D0=61700080, A0=00124178, D1=00000000, A1=00F04000
00123160: move.l  #$f03000, $f02110.l 		D0=61700080, A0=00124178, D1=00000000, A1=00F04000
0012316A: move.l  #$1, $f02114.l 		D0=61700080, A0=00124178, D1=00000000, A1=00F04000
00123174: rts 		D0=61700080, A0=00124178, D1=00000000, A1=00F04000
*/
/*	static char buffer[2048];
//if (m68kPC > 0x805F48) start = true;
//if (m68kPC > 0x806486) start = true;
//if (m68kPC == 0x805FEE) start = true;
//if (m68kPC == 0x80600C)// start = true;
if (m68kPC == 0x802058) start = true;
//{
//	GPUDumpRegisters();
//	GPUDumpDisassembly();
//
//	M68K_show_context();
//	log_done();
//	exit(0);
//}
	if (start)
	{
	m68k_disassemble(buffer, m68kPC, M68K_CPU_TYPE_68000);
	WriteLog("%08X: %s \t\tD0=%08X, A0=%08X, D1=%08X, A1=%08X\n", m68kPC, buffer, m68k_get_reg(NULL, M68K_REG_D0), m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_D1), m68k_get_reg(NULL, M68K_REG_A1));
	}//*/

/*	if (m68kPC == 0x803F16)
	{
		WriteLog("M68K: Registers found at $803F16:\n");
		WriteLog("\t68K PC=%06X\n", m68k_get_reg(NULL, M68K_REG_PC));
		for(int i=M68K_REG_D0; i<=M68K_REG_D7; i++)
			WriteLog("\tD%i = %08X\n", i-M68K_REG_D0, m68k_get_reg(NULL, (m68k_register_t)i));
		WriteLog("\n");
		for(int i=M68K_REG_A0; i<=M68K_REG_A7; i++)
			WriteLog("\tA%i = %08X\n", i-M68K_REG_A0, m68k_get_reg(NULL, (m68k_register_t)i));
	}*/
//Looks like the DSP is supposed to return $12345678 when it finishes its validation routine...
// !!! Investigate !!!
/*extern bool doDSPDis;
	static bool disgo = false;
	if (m68kPC == 0x50222)
	{
		// CD BIOS hacking
//		WriteLog("M68K: About to stuff $12345678 into $F1B000 (=%08X)...\n", DSPReadLong(0xF1B000, M68K));
//		DSPWriteLong(0xF1B000, 0x12345678, M68K);
//		disgo = true;
	}
	if (m68kPC == 0x5000)
//		doDSPDis = true;
		disgo = true;
	if (disgo)
	{
		static char buffer[2048];
		m68k_disassemble(buffer, m68kPC, M68K_CPU_TYPE_68000);
		WriteLog("%08X: %s", m68kPC, buffer);
		WriteLog("\t\tA0=%08X, A1=%08X, D0=%08X, D1=%08X, D2=%08X\n",
			m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_A1),
			m68k_get_reg(NULL, M68K_REG_D0), m68k_get_reg(NULL, M68K_REG_D1), m68k_get_reg(NULL, M68K_REG_D2));
	}//*/
/*	if (m68kPC == 0x82E1A)
	{
		static char buffer[2048];
		m68k_disassemble(buffer, m68kPC, 0);//M68K_CPU_TYPE_68000);
		WriteLog("--> [Routine start] %08X: %s", m68kPC, buffer);
		WriteLog("\t\tA0=%08X, A1=%08X, D0=%08X(cmd), D1=%08X(# bytes), D2=%08X\n",
			m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_A1),
			m68k_get_reg(NULL, M68K_REG_D0), m68k_get_reg(NULL, M68K_REG_D1), m68k_get_reg(NULL, M68K_REG_D2));
	}//*/
/*	if (m68kPC == 0x82E58)
		WriteLog("--> [Routine end]\n");
	if (m68kPC == 0x80004)
	{
		WriteLog("--> [Calling BusWrite2] D2: %08X\n", m68k_get_reg(NULL, M68K_REG_D2));
//		m68k_set_reg(M68K_REG_D2, 0x12345678);
	}//*/

#ifdef LOG_CD_BIOS_CALLS
/*
CD_init::	-> $3000
BIOS_VER::	-> $3004
CD_mode::	-> $3006
CD_ack::	-> $300C
CD_jeri::	-> $3012
CD_spin::	-> $3018
CD_stop::	-> $301E
CD_mute::	-> $3024
CD_umute::	-> $302A
CD_paus::	-> $3030
CD_upaus::	-> $3036
CD_read::	-> $303C
CD_uread::	-> $3042
CD_setup::	-> $3048
CD_ptr::	-> $304E
CD_osamp::	-> $3054
CD_getoc::	-> $305A
CD_initm::	-> $3060
CD_initf::	-> $3066
CD_switch::	-> $306C
*/
	if (m68kPC == 0x3000)
		WriteLog("M68K: CD_init\n");
	else if (m68kPC == 0x3006 + (6 * 0))
		WriteLog("M68K: CD_mode\n");
	else if (m68kPC == 0x3006 + (6 * 1))
		WriteLog("M68K: CD_ack\n");
	else if (m68kPC == 0x3006 + (6 * 2))
		WriteLog("M68K: CD_jeri\n");
	else if (m68kPC == 0x3006 + (6 * 3))
		WriteLog("M68K: CD_spin\n");
	else if (m68kPC == 0x3006 + (6 * 4))
		WriteLog("M68K: CD_stop\n");
	else if (m68kPC == 0x3006 + (6 * 5))
		WriteLog("M68K: CD_mute\n");
	else if (m68kPC == 0x3006 + (6 * 6))
		WriteLog("M68K: CD_umute\n");
	else if (m68kPC == 0x3006 + (6 * 7))
		WriteLog("M68K: CD_paus\n");
	else if (m68kPC == 0x3006 + (6 * 8))
		WriteLog("M68K: CD_upaus\n");
	else if (m68kPC == 0x3006 + (6 * 9))
		WriteLog("M68K: CD_read\n");
	else if (m68kPC == 0x3006 + (6 * 10))
		WriteLog("M68K: CD_uread\n");
	else if (m68kPC == 0x3006 + (6 * 11))
		WriteLog("M68K: CD_setup\n");
	else if (m68kPC == 0x3006 + (6 * 12))
		WriteLog("M68K: CD_ptr\n");
	else if (m68kPC == 0x3006 + (6 * 13))
		WriteLog("M68K: CD_osamp\n");
	else if (m68kPC == 0x3006 + (6 * 14))
		WriteLog("M68K: CD_getoc\n");
	else if (m68kPC == 0x3006 + (6 * 15))
		WriteLog("M68K: CD_initm\n");
	else if (m68kPC == 0x3006 + (6 * 16))
		WriteLog("M68K: CD_initf\n");
	else if (m68kPC == 0x3006 + (6 * 17))
		WriteLog("M68K: CD_switch\n");

	if (m68kPC >= 0x3000 && m68kPC <= 0x306C)
		WriteLog("\t\tA0=%08X, A1=%08X, D0=%08X, D1=%08X, D2=%08X\n",
			m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_A1),
			m68k_get_reg(NULL, M68K_REG_D0), m68k_get_reg(NULL, M68K_REG_D1), m68k_get_reg(NULL, M68K_REG_D2));
#endif

#ifdef ABORT_ON_ILLEGAL_INSTRUCTIONS
	if (!m68k_is_valid_instruction(m68k_read_memory_16(m68kPC), 0))//M68K_CPU_TYPE_68000))
	{
#ifndef ABORT_ON_OFFICIAL_ILLEGAL_INSTRUCTION
		if (m68k_read_memory_16(m68kPC) == 0x4AFC)
		{
			// This is a kludge to let homebrew programs work properly (i.e., let the other processors
			// keep going even when the 68K dumped back to the debugger or what have you).
//dis no wok right!
//			m68k_set_reg(M68K_REG_PC, m68kPC - 2);
// Try setting the vector to the illegal instruction...
//This doesn't work right either! Do something else! Quick!
//			SET32(jaguar_mainRam, 0x10, m68kPC);

			return;
		}
#endif

		WriteLog("\nM68K encountered an illegal instruction at %08X!!!\n\nAborting!\n", m68kPC);
		uint32_t topOfStack = m68k_get_reg(NULL, M68K_REG_A7);
		WriteLog("M68K: Top of stack: %08X. Stack trace:\n", JaguarReadLong(topOfStack));
		uint32_t address = topOfStack - (4 * 4 * 3);

		for(int i=0; i<10; i++)
		{
			WriteLog("%06X:", address);

			for(int j=0; j<4; j++)
			{
				WriteLog(" %08X", JaguarReadLong(address));
				address += 4;
			}

			WriteLog("\n");
		}

		WriteLog("Jaguar: VBL interrupt is %s\n", ((TOMIRQEnabled(IRQ_VIDEO)) && (JaguarInterruptHandlerIsValid(64))) ? "enabled" : "disabled");
		M68K_show_context();

//temp
//	WriteLog("\n\n68K disasm\n\n");
//	jaguar_dasm(0x802000, 0x50C);
//	WriteLog("\n\n");
//endoftemp

		LogDone();
		exit(0);
	}//*/
#endif
}

#if 0
Now here be dragons...
Here is how memory ranges are defined in the CoJag driver.
Note that we only have to be concerned with 3 entities read/writing anything:
The main CPU, the GPU, and the DSP. Everything else is unnecessary. So we can keep our main memory
checking in jaguar.cpp, gpu.cpp and dsp.cpp. There should be NO checking in TOM, JERRY, etc. other than
things that are entirely internal to those modules. This way we should be able to get a handle on all
this crap which is currently scattered over Hells Half Acre(tm).

Also: We need to distinguish whether or not we need .b, .w, and .dw versions of everything, or if there
is a good way to collapse that shit (look below for inspiration). Current method works, but is error prone.

/*************************************
 *
 *  Main CPU memory handlers
 *
 *************************************/

static ADDRESS_MAP_START( m68020_map, ADDRESS_SPACE_PROGRAM, 32 )
	AM_RANGE(0x000000, 0x7fffff) AM_RAM AM_BASE(&jaguar_shared_ram) AM_SHARE(1)
	AM_RANGE(0x800000, 0x9fffff) AM_ROM AM_REGION(REGION_USER1, 0) AM_BASE(&rom_base)
	AM_RANGE(0xa00000, 0xa1ffff) AM_RAM
	AM_RANGE(0xa20000, 0xa21fff) AM_READWRITE(eeprom_data_r, eeprom_data_w) AM_BASE(&generic_nvram32) AM_SIZE(&generic_nvram_size)
	AM_RANGE(0xa30000, 0xa30003) AM_WRITE(watchdog_reset32_w)
	AM_RANGE(0xa40000, 0xa40003) AM_WRITE(eeprom_enable_w)
	AM_RANGE(0xb70000, 0xb70003) AM_READWRITE(misc_control_r, misc_control_w)
	AM_RANGE(0xc00000, 0xdfffff) AM_ROMBANK(2)
	AM_RANGE(0xe00000, 0xe003ff) AM_DEVREADWRITE(IDE_CONTROLLER, "ide",  ide_controller32_r, ide_controller32_w)
	AM_RANGE(0xf00000, 0xf003ff) AM_READWRITE(jaguar_tom_regs32_r, jaguar_tom_regs32_w)
	AM_RANGE(0xf00400, 0xf007ff) AM_RAM AM_BASE(&jaguar_gpu_clut) AM_SHARE(2)
	AM_RANGE(0xf02100, 0xf021ff) AM_READWRITE(gpuctrl_r, gpuctrl_w)
	AM_RANGE(0xf02200, 0xf022ff) AM_READWRITE(jaguar_blitter_r, jaguar_blitter_w)
	AM_RANGE(0xf03000, 0xf03fff) AM_MIRROR(0x008000) AM_RAM AM_BASE(&jaguar_gpu_ram) AM_SHARE(3)
	AM_RANGE(0xf10000, 0xf103ff) AM_READWRITE(jaguar_jerry_regs32_r, jaguar_jerry_regs32_w)
	AM_RANGE(0xf16000, 0xf1600b) AM_READ(cojag_gun_input_r)	// GPI02
	AM_RANGE(0xf17000, 0xf17003) AM_READ(status_r)			// GPI03
//  AM_RANGE(0xf17800, 0xf17803) AM_WRITE(latch_w)  // GPI04
	AM_RANGE(0xf17c00, 0xf17c03) AM_READ(jamma_r)			// GPI05
	AM_RANGE(0xf1a100, 0xf1a13f) AM_READWRITE(dspctrl_r, dspctrl_w)
	AM_RANGE(0xf1a140, 0xf1a17f) AM_READWRITE(jaguar_serial_r, jaguar_serial_w)
	AM_RANGE(0xf1b000, 0xf1cfff) AM_RAM AM_BASE(&jaguar_dsp_ram) AM_SHARE(4)
ADDRESS_MAP_END

/*************************************
 *
 *  GPU memory handlers
 *
 *************************************/

static ADDRESS_MAP_START( gpu_map, ADDRESS_SPACE_PROGRAM, 32 )
	AM_RANGE(0x000000, 0x7fffff) AM_RAM AM_SHARE(1)
	AM_RANGE(0x800000, 0xbfffff) AM_ROMBANK(8)
	AM_RANGE(0xc00000, 0xdfffff) AM_ROMBANK(9)
	AM_RANGE(0xe00000, 0xe003ff) AM_DEVREADWRITE(IDE_CONTROLLER, "ide", ide_controller32_r, ide_controller32_w)
	AM_RANGE(0xf00000, 0xf003ff) AM_READWRITE(jaguar_tom_regs32_r, jaguar_tom_regs32_w)
	AM_RANGE(0xf00400, 0xf007ff) AM_RAM AM_SHARE(2)
	AM_RANGE(0xf02100, 0xf021ff) AM_READWRITE(gpuctrl_r, gpuctrl_w)
	AM_RANGE(0xf02200, 0xf022ff) AM_READWRITE(jaguar_blitter_r, jaguar_blitter_w)
	AM_RANGE(0xf03000, 0xf03fff) AM_RAM AM_SHARE(3)
	AM_RANGE(0xf10000, 0xf103ff) AM_READWRITE(jaguar_jerry_regs32_r, jaguar_jerry_regs32_w)
ADDRESS_MAP_END

/*************************************
 *
 *  DSP memory handlers
 *
 *************************************/

static ADDRESS_MAP_START( dsp_map, ADDRESS_SPACE_PROGRAM, 32 )
	AM_RANGE(0x000000, 0x7fffff) AM_RAM AM_SHARE(1)
	AM_RANGE(0x800000, 0xbfffff) AM_ROMBANK(8)
	AM_RANGE(0xc00000, 0xdfffff) AM_ROMBANK(9)
	AM_RANGE(0xf10000, 0xf103ff) AM_READWRITE(jaguar_jerry_regs32_r, jaguar_jerry_regs32_w)
	AM_RANGE(0xf1a100, 0xf1a13f) AM_READWRITE(dspctrl_r, dspctrl_w)
	AM_RANGE(0xf1a140, 0xf1a17f) AM_READWRITE(jaguar_serial_r, jaguar_serial_w)
	AM_RANGE(0xf1b000, 0xf1cfff) AM_RAM AM_SHARE(4)
	AM_RANGE(0xf1d000, 0xf1dfff) AM_READ(jaguar_wave_rom_r) AM_BASE(&jaguar_wave_rom)
ADDRESS_MAP_END
*/
#endif

//#define EXPERIMENTAL_MEMORY_HANDLING
// Experimental memory mappage...
// Dunno if this is a good approach or not, but it seems to make better
// sense to have all this crap in one spot intstead of scattered all over
// the place the way it is now.
#ifdef EXPERIMENTAL_MEMORY_HANDLING
// Needed defines...
#define NEW_TIMER_SYSTEM

/*
uint8_t jaguarMainRAM[0x400000];						// 68K CPU RAM
uint8_t jaguarMainROM[0x600000];						// 68K CPU ROM
uint8_t jaguarBootROM[0x040000];						// 68K CPU BIOS ROM--uses only half of this!
uint8_t jaguarCDBootROM[0x040000];					// 68K CPU CD BIOS ROM
bool BIOSLoaded = false;
bool CDBIOSLoaded = false;

uint8_t cdRAM[0x100];
uint8_t tomRAM[0x4000];
uint8_t jerryRAM[0x10000];
static uint16_t eeprom_ram[64];

// NOTE: CD BIOS ROM is read from cartridge space @ $802000 (it's a cartridge, after all)
*/

enum MemType { MM_NOP = 0, MM_RAM, MM_ROM, MM_IO };

// M68K Memory map/handlers
uint32_t 	{
	{ 0x000000, 0x3FFFFF, MM_RAM, jaguarMainRAM },
	{ 0x800000, 0xDFFEFF, MM_ROM, jaguarMainROM },
// Note that this is really memory mapped I/O region...
//	{ 0xDFFF00, 0xDFFFFF, MM_RAM, cdRAM },
	{ 0xDFFF00, 0xDFFF03, MM_IO,  cdBUTCH }, // base of Butch == interrupt control register, R/W
	{ 0xDFFF04, 0xDFFF07, MM_IO,  cdDSCNTRL }, // DSA control register, R/W
	{ 0xDFFF0A, 0xDFFF0B, MM_IO,  cdDS_DATA }, // DSA TX/RX data, R/W
	{ 0xDFFF10, 0xDFFF13, MM_IO,  cdI2CNTRL }, // i2s bus control register, R/W
	{ 0xDFFF14, 0xDFFF17, MM_IO,  cdSBCNTRL }, // CD subcode control register, R/W
	{ 0xDFFF18, 0xDFFF1B, MM_IO,  cdSUBDATA }, // Subcode data register A
	{ 0xDFFF1C, 0xDFFF1F, MM_IO,  cdSUBDATB }, // Subcode data register B
	{ 0xDFFF20, 0xDFFF23, MM_IO,  cdSB_TIME }, // Subcode time and compare enable (D24)
	{ 0xDFFF24, 0xDFFF27, MM_IO,  cdFIFO_DATA }, // i2s FIFO data
	{ 0xDFFF28, 0xDFFF2B, MM_IO,  cdI2SDAT2 }, // i2s FIFO data (old)
	{ 0xDFFF2C, 0xDFFF2F, MM_IO,  cdUNKNOWN }, // Seems to be some sort of I2S interface

	{ 0xE00000, 0xE3FFFF, MM_ROM, jaguarBootROM },

//	{ 0xF00000, 0xF0FFFF, MM_IO,  TOM_REGS_RW },
	{ 0xF00050, 0xF00051, MM_IO,  tomTimerPrescaler },
	{ 0xF00052, 0xF00053, MM_IO,  tomTimerDivider },
	{ 0xF00400, 0xF005FF, MM_RAM, tomRAM }, // CLUT A&B: How to link these? Write to one writes to the other...
	{ 0xF00600, 0xF007FF, MM_RAM, tomRAM }, // Actually, this is a good approach--just make the reads the same as well
	//What about LBUF writes???
	{ 0xF02100, 0xF0211F, MM_IO,  GPUWriteByte }, // GPU CONTROL
	{ 0xF02200, 0xF0229F, MM_IO,  BlitterWriteByte }, // BLITTER
	{ 0xF03000, 0xF03FFF, MM_RAM, GPUWriteByte }, // GPU RAM

	{ 0xF10000, 0xF1FFFF, MM_IO,  JERRY_REGS_RW },

/*
	EEPROM:
	{ 0xF14001, 0xF14001, MM_IO_RO, eepromFOO }
	{ 0xF14801, 0xF14801, MM_IO_WO, eepromBAR }
	{ 0xF15001, 0xF15001, MM_IO_RW, eepromBAZ }

	JOYSTICK:
	{ 0xF14000, 0xF14003, MM_IO,  joystickFoo }
	0 = pad0/1 button values (4 bits each), RO(?)
	1 = pad0/1 index value (4 bits each), WO
	2 = unused, RO
	3 = NTSC/PAL, certain button states, RO

JOYSTICK    $F14000               Read/Write
            15.....8  7......0
Read        fedcba98  7654321q    f-1    Signals J15 to J1
                                  q      Cartridge EEPROM  output data
Write       exxxxxxm  76543210    e      1 = enable  J7-J0 outputs
                                         0 = disable J7-J0 outputs
                                  x      don't care
                                  m      Audio mute
                                         0 = Audio muted (reset state)
                                         1 = Audio enabled
                                  7-4    J7-J4 outputs (port 2)
                                  3-0    J3-J0 outputs (port 1)
JOYBUTS     $F14002               Read Only
            15.....8  7......0
Read        xxxxxxxx  rrdv3210    x      don't care
                                  r      Reserved
                                  d      Reserved
                                  v      1 = NTSC Video hardware
                                         0 = PAL  Video hardware
                                  3-2    Button inputs B3 & B2 (port 2)
                                  1-0    Button inputs B1 & B0 (port 1)

J4 J5 J6 J7  Port 2    B2     B3    J12  J13   J14  J15
J3 J2 J1 J0  Port 1    B0     B1    J8   J9    J10  J11
 0  0  0  0
 0  0  0  1
 0  0  1  0
 0  0  1  1
 0  1  0  0
 0  1  0  1
 0  1  1  0
 0  1  1  1  Row 3     C3   Option  #     9     6     3
 1  0  0  0
 1  0  0  1
 1  0  1  0
 1  0  1  1  Row 2     C2      C    0     8     5     2
 1  1  0  0
 1  1  0  1  Row 1     C1      B    *     7     4     1
 1  1  1  0  Row 0   Pause     A    Up  Down  Left  Right
 1  1  1  1

0 bit read in any position means that button is pressed.
C3 = C2 = 1 means std. Jag. cntrlr. or nothing attached.
*/
};

void WriteByte(uint32_t address, uint8_t byte, uint32_t who/*=UNKNOWN*/)
{
	// Not sure, but I think the system only has 24 address bits...
	address &= 0x00FFFFFF;

	// RAM			($000000 - $3FFFFF)		4M
	if (address <= 0x3FFFFF)
		jaguarMainRAM[address] = byte;
	// hole			($400000 - $7FFFFF)		4M
	else if (address <= 0x7FFFFF)
		;	// Do nothing
	// GAME ROM		($800000 - $DFFEFF)		6M - 256 bytes
	else if (address <= 0xDFFEFF)
		;	// Do nothing
	// CDROM		($DFFF00 - $DFFFFF)		256 bytes
	else if (address <= 0xDFFFFF)
	{
		cdRAM[address & 0xFF] = byte;
#ifdef CDROM_LOG
		if ((address & 0xFF) < 12 * 4)
			WriteLog("[%s] ", BReg[(address & 0xFF) / 4]);
		WriteLog("CDROM: %s writing byte $%02X at $%08X [68K PC=$%08X]\n", whoName[who], data, offset, m68k_get_reg(NULL, M68K_REG_PC));
#endif
	}
	// BIOS ROM		($E00000 - $E3FFFF)		256K
	else if (address <= 0xE3FFFF)
		;	// Do nothing
	// hole			($E40000 - $EFFFFF)		768K
	else if (address <= 0xEFFFFF)
		;	// Do nothing
	// TOM			($F00000 - $F0FFFF)		64K
	else if (address <= 0xF0FFFF)
//		;	// Do nothing
	{
		if (address == 0xF00050)
		{
			tomTimerPrescaler = (tomTimerPrescaler & 0x00FF) | ((uint16_t)byte << 8);
			TOMResetPIT();
			return;
		}
		else if (address == 0xF00051)
		{
			tomTimerPrescaler = (tomTimerPrescaler & 0xFF00) | byte;
			TOMResetPIT();
			return;
		}
		else if (address == 0xF00052)
		{
			tomTimerDivider = (tomTimerDivider & 0x00FF) | ((uint16_t)byte << 8);
			TOMResetPIT();
			return;
		}
		else if (address == 0xF00053)
		{
			tomTimerDivider = (tomTimerDivider & 0xFF00) | byte;
			TOMResetPIT();
			return;
		}
		else if (address >= 0xF00400 && address <= 0xF007FF)	// CLUT (A & B)
		{
			// Writing to one CLUT writes to the other
			address &= 0x5FF;		// Mask out $F00600 (restrict to $F00400-5FF)
			tomRAM[address] = tomRAM[address + 0x200] = byte;
			return;
		}
		//What about LBUF writes???
		else if ((address >= 0xF02100) && (address <= 0xF0211F))	// GPU CONTROL
		{
			GPUWriteByte(address, byte, who);
			return;
		}
		else if ((address >= 0xF02200) && (address <= 0xF0229F))	// BLITTER
		{
			BlitterWriteByte(address, byte, who);
			return;
		}
		else if ((address >= 0xF03000) && (address <= 0xF03FFF))	// GPU RAM
		{
			GPUWriteByte(address, byte, who);
			return;
		}

		tomRAM[address & 0x3FFF] = byte;
	}
	// JERRY		($F10000 - $F1FFFF)		64K
	else if (address <= 0xF1FFFF)
//		;	// Do nothing
	{
#ifdef JERRY_DEBUG
		WriteLog("jerry: writing byte %.2x at 0x%.6x\n", byte, address);
#endif
		if ((address >= DSP_CONTROL_RAM_BASE) && (address < DSP_CONTROL_RAM_BASE+0x20))
		{
			DSPWriteByte(address, byte, who);
			return;
		}
		else if ((address >= DSP_WORK_RAM_BASE) && (address < DSP_WORK_RAM_BASE+0x2000))
		{
			DSPWriteByte(address, byte, who);
			return;
		}
		// SCLK ($F1A150--8 bits wide)
//NOTE: This should be taken care of in DAC...
		else if ((address >= 0xF1A152) && (address <= 0xF1A153))
		{
//		WriteLog("JERRY: Writing %02X to SCLK...\n", data);
			if ((address & 0x03) == 2)
				JERRYI2SInterruptDivide = (JERRYI2SInterruptDivide & 0x00FF) | ((uint32_t)byte << 8);
			else
				JERRYI2SInterruptDivide = (JERRYI2SInterruptDivide & 0xFF00) | (uint32_t)byte;

			JERRYI2SInterruptTimer = -1;
#ifndef NEW_TIMER_SYSTEM
			jerry_i2s_exec(0);
#else
			RemoveCallback(JERRYI2SCallback);
			JERRYI2SCallback();
#endif
//			return;
		}
		// LTXD/RTXD/SCLK/SMODE $F1A148/4C/50/54 (really 16-bit registers...)
		else if (address >= 0xF1A148 && address <= 0xF1A157)
		{
			DACWriteByte(address, byte, who);
			return;
		}
		else if (address >= 0xF10000 && address <= 0xF10007)
		{
#ifndef NEW_TIMER_SYSTEM
			switch (address & 0x07)
			{
			case 0:
				JERRYPIT1Prescaler = (JERRYPIT1Prescaler & 0x00FF) | (byte << 8);
				JERRYResetPIT1();
				break;
			case 1:
				JERRYPIT1Prescaler = (JERRYPIT1Prescaler & 0xFF00) | byte;
				JERRYResetPIT1();
				break;
			case 2:
				JERRYPIT1Divider = (JERRYPIT1Divider & 0x00FF) | (byte << 8);
				JERRYResetPIT1();
				break;
			case 3:
				JERRYPIT1Divider = (JERRYPIT1Divider & 0xFF00) | byte;
				JERRYResetPIT1();
				break;
			case 4:
				JERRYPIT2Prescaler = (JERRYPIT2Prescaler & 0x00FF) | (byte << 8);
				JERRYResetPIT2();
				break;
			case 5:
				JERRYPIT2Prescaler = (JERRYPIT2Prescaler & 0xFF00) | byte;
				JERRYResetPIT2();
				break;
			case 6:
				JERRYPIT2Divider = (JERRYPIT2Divider & 0x00FF) | (byte << 8);
				JERRYResetPIT2();
				break;
			case 7:
				JERRYPIT2Divider = (JERRYPIT2Divider & 0xFF00) | byte;
				JERRYResetPIT2();
			}
#else
WriteLog("JERRY: Unhandled timer write (BYTE) at %08X...\n", address);
#endif
			return;
		}
/*	else if ((offset >= 0xF10010) && (offset <= 0xF10015))
	{
		clock_byte_write(offset, byte);
		return;
	}//*/
	// JERRY -> 68K interrupt enables/latches (need to be handled!)
		else if (address >= 0xF10020 && address <= 0xF10023)
		{
WriteLog("JERRY: (68K int en/lat - Unhandled!) Tried to write $%02X to $%08X!\n", byte, address);
		}
/*	else if ((offset >= 0xF17C00) && (offset <= 0xF17C01))
	{
		anajoy_byte_write(offset, byte);
		return;
	}*/
		else if ((address >= 0xF14000) && (address <= 0xF14003))
		{
			JoystickWriteByte(address, byte);
			EepromWriteByte(address, byte);
			return;
		}
		else if ((address >= 0xF14004) && (address <= 0xF1A0FF))
		{
			EepromWriteByte(address, byte);
			return;
		}
//Need to protect write attempts to Wavetable ROM (F1D000-FFF)
		else if (address >= 0xF1D000 && address <= 0xF1DFFF)
			return;

		jerryRAM[address & 0xFFFF] = byte;
	}
	// hole			($F20000 - $FFFFFF)		1M - 128K
	else
		;	// Do nothing
}


void WriteWord(uint32_t adddress, uint16_t word)
{
}


void WriteDWord(uint32_t adddress, uint32_t dword)
{
}


uint8_t ReadByte(uint32_t adddress)
{
}


uint16_t ReadWord(uint32_t adddress)
{
}


uint32_t ReadDWord(uint32_t adddress)
{
}
#endif


void ShowM68KContext(void)
{
	printf("\t68K PC=%06X\n", m68k_get_reg(NULL, M68K_REG_PC));

	for(int i=M68K_REG_D0; i<=M68K_REG_D7; i++)
	{
		printf("D%i = %08X ", i-M68K_REG_D0, m68k_get_reg(NULL, (m68k_register_t)i));

		if (i == M68K_REG_D3 || i == M68K_REG_D7)
			printf("\n");
	}

	for(int i=M68K_REG_A0; i<=M68K_REG_A7; i++)
	{
		printf("A%i = %08X ", i-M68K_REG_A0, m68k_get_reg(NULL, (m68k_register_t)i));

		if (i == M68K_REG_A3 || i == M68K_REG_A7)
			printf("\n");
	}

	uint32_t currpc = m68k_get_reg(NULL, M68K_REG_PC);
	uint32_t disPC = currpc - 30;
	char buffer[128];

	do
	{
		uint32_t oldpc = disPC;
		disPC += m68k_disassemble(buffer, disPC, 0);
		printf("%s%08X: %s\n", (oldpc == currpc ? ">" : " "), oldpc, buffer);
	}
	while (disPC < (currpc + 10));
}


//
// Custom UAE 68000 read/write/IRQ functions
//

#if 0
IRQs:
=-=-=

      IPL         Name           Vector            Control
   ---------+---------------+---------------+---------------
       2      VBLANK IRQ         $100         INT1 bit #0 
       2      GPU IRQ            $100         INT1 bit #1
       2      HBLANK IRQ         $100         INT1 bit #2
       2      Timer IRQ          $100         INT1 bit #3

   Note: Both timer interrupts (JPIT && PIT) are on the same INT1 bit.
         and are therefore indistinguishable.

   A typical way to install a LEVEL2 handler for the 68000 would be 
   something like this, you gotta supply "last_line" and "handler".
   Note that the interrupt is auto vectored thru $100 (not $68)


   V_AUTO   = $100
   VI       = $F004E
   INT1     = $F00E0
   INT2     = $F00E2
   
   IRQS_HANDLED=$909                ;; VBLANK and TIMER

         move.w   #$2700,sr         ;; no IRQs please
         move.l   #handler,V_AUTO   ;; install our routine

         move.w   #last_line,VI     ;; scanline where IRQ should occur
                                    ;; should be 'odd' BTW
         move.w   #IRQS_HANDLE&$FF,INT1  ;; enable VBLANK + TIMER
         move.w   #$2100,sr         ;; enable IRQs on the 68K
         ...

handler:
         move.w   d0,-(a7)
         move.w   INT1,d0
         btst.b   #0,d0
         bne.b    .no_blank

         ...

.no_blank:
         btst.b   #3,d0
         beq.b    .no_timer
      
         ...

.no_timer:
         move.w   #IRQS_HANDLED,INT1      ; clear latch, keep IRQ alive
         move.w   #0,INT2                 ; let GPU run again
         move.w   (a7)+,d0
         rte

   As you can see, if you have multiple INT1 interrupts coming in,
   you need to check the lower byte of INT1, to see which interrupt
   happened.
#endif
int irq_ack_handler(int level)
{
#ifdef CPU_DEBUG_TRACING
	if (startM68KTracing)
	{
		WriteLog("irq_ack_handler: M68K PC=%06X\n", m68k_get_reg(NULL, M68K_REG_PC));
	}
#endif

	// Tracing the IPL lines on the Jaguar schematic yields the following:
	// IPL1 is connected to INTL on TOM (OUT to 68K)
	// IPL0-2 are also tied to Vcc via 4.7K resistors!
	// (DINT on TOM goes into DINT on JERRY (IN Tom from Jerry))
	// There doesn't seem to be any other path to IPL0 or 2 on the schematic,
	// which means that *all* IRQs to the 68K are routed thru TOM at level 2.
	// Which means they're all maskable.

	// The GPU/DSP/etc are probably *not* issuing an NMI, but it seems to work
	// OK...
	// They aren't, and this causes problems with a, err, specific ROM. :-D

	if (level == 2)
	{
		m68k_set_irq(0);						// Clear the IRQ (NOTE: Without this, the BIOS fails)...
		return 64;								// Set user interrupt #0
	}

	return M68K_INT_ACK_AUTOVECTOR;
}


//#define USE_NEW_MMU

unsigned int m68k_read_memory_8(unsigned int address)
{
#ifdef ALPINE_FUNCTIONS
	// Check if breakpoint on memory is active, and deal with it
	if (bpmActive && address == bpmAddress1)
		M68KDebugHalt();
#endif

	// Musashi does this automagically for you, UAE core does not :-P
	address &= 0x00FFFFFF;
#ifdef CPU_DEBUG_MEMORY
	// Note that the Jaguar only has 2M of RAM, not 4!
	if ((address >= 0x000000) && (address <= 0x1FFFFF))
	{
		if (startMemLog)
			readMem[address] = 1;
	}
#endif
//WriteLog("[RM8] Addr: %08X\n", address);
//; So, it seems that it stores the returned DWORD at $51136 and $FB074.
/*	if (address == 0x51136 || address == 0x51138 || address == 0xFB074 || address == 0xFB076
		|| address == 0x1AF05E)
		WriteLog("[RM8  PC=%08X] Addr: %08X, val: %02X\n", m68k_get_reg(NULL, M68K_REG_PC), address, jaguar_mainRam[address]);//*/
#ifndef USE_NEW_MMU
	unsigned int retVal = 0;

	// Note that the Jaguar only has 2M of RAM, not 4!
	if ((address >= 0x000000) && (address <= 0x1FFFFF))
		retVal = jaguarMainRAM[address];
//	else if ((address >= 0x800000) && (address <= 0xDFFFFF))
	else if ((address >= 0x800000) && (address <= 0xDFFEFF))
		retVal = jaguarMainROM[address - 0x800000];
	else if ((address >= 0xE00000) && (address <= 0xE3FFFF))
//		retVal = jaguarBootROM[address - 0xE00000];
//		retVal = jaguarDevBootROM1[address - 0xE00000];
		retVal = jagMemSpace[address];
	else if ((address >= 0xDFFF00) && (address <= 0xDFFFFF))
		retVal = CDROMReadByte(address);
	else if ((address >= 0xF00000) && (address <= 0xF0FFFF))
		retVal = TOMReadByte(address, M68K);
	else if ((address >= 0xF10000) && (address <= 0xF1FFFF))
		retVal = JERRYReadByte(address, M68K);
	else
		retVal = jaguar_unknown_readbyte(address, M68K);

//if (address >= 0x2800 && address <= 0x281F)
//	WriteLog("M68K: Read byte $%02X at $%08X [PC=%08X]\n", retVal, address, m68k_get_reg(NULL, M68K_REG_PC));
//if (address >= 0x8B5E4 && address <= 0x8B5E4 + 16)
//	WriteLog("M68K: Read byte $%02X at $%08X [PC=%08X]\n", retVal, address, m68k_get_reg(NULL, M68K_REG_PC));
    return retVal;
#else
	return MMURead8(address, M68K);
#endif
}


void gpu_dump_disassembly(void);
void gpu_dump_registers(void);

unsigned int m68k_read_memory_16(unsigned int address)
{
#ifdef ALPINE_FUNCTIONS
	// Check if breakpoint on memory is active, and deal with it
	if (bpmActive && address == bpmAddress1)
		M68KDebugHalt();
#endif

	// Musashi does this automagically for you, UAE core does not :-P
	address &= 0x00FFFFFF;
#ifdef CPU_DEBUG_MEMORY
/*	if ((address >= 0x000000) && (address <= 0x3FFFFE))
	{
		if (startMemLog)
			readMem[address] = 1, readMem[address + 1] = 1;
	}//*/
/*	if (effect_start && (address >= 0x8064FC && address <= 0x806501))
	{
		return 0x4E71;	// NOP
	}
	if (effect_start2 && (address >= 0x806502 && address <= 0x806507))
	{
		return 0x4E71;	// NOP
	}
	if (effect_start3 && (address >= 0x806512 && address <= 0x806517))
	{
		return 0x4E71;	// NOP
	}
	if (effect_start4 && (address >= 0x806524 && address <= 0x806527))
	{
		return 0x4E71;	// NOP
	}
	if (effect_start5 && (address >= 0x80653E && address <= 0x806543)) //Collision detection!
	{
		return 0x4E71;	// NOP
	}
	if (effect_start6 && (address >= 0x806544 && address <= 0x806547))
	{
		return 0x4E71;	// NOP
	}//*/
#endif
//WriteLog("[RM16] Addr: %08X\n", address);
/*if (m68k_get_reg(NULL, M68K_REG_PC) == 0x00005FBA)
//	for(int i=0; i<10000; i++)
	WriteLog("[M68K] In routine #6!\n");//*/
//if (m68k_get_reg(NULL, M68K_REG_PC) == 0x00006696) // GPU Program #4
//if (m68k_get_reg(NULL, M68K_REG_PC) == 0x00005B3C)	// GPU Program #2
/*if (m68k_get_reg(NULL, M68K_REG_PC) == 0x00005BA8)	// GPU Program #3
{
	WriteLog("[M68K] About to run GPU! (Addr:%08X, data:%04X)\n", address, TOMReadWord(address));
	gpu_dump_registers();
	gpu_dump_disassembly();
//	for(int i=0; i<10000; i++)
//		WriteLog("[M68K] About to run GPU!\n");
}//*/
//WriteLog("[WM8  PC=%08X] Addr: %08X, val: %02X\n", m68k_get_reg(NULL, M68K_REG_PC), address, value);
/*if (m68k_get_reg(NULL, M68K_REG_PC) >= 0x00006696 && m68k_get_reg(NULL, M68K_REG_PC) <= 0x000066A8)
{
	if (address == 0x000066A0)
	{
		gpu_dump_registers();
		gpu_dump_disassembly();
	}
	for(int i=0; i<10000; i++)
		WriteLog("[M68K] About to run GPU! (Addr:%08X, data:%04X)\n", address, TOMReadWord(address));
}//*/
//; So, it seems that it stores the returned DWORD at $51136 and $FB074.
/*	if (address == 0x51136 || address == 0x51138 || address == 0xFB074 || address == 0xFB076
		|| address == 0x1AF05E)
		WriteLog("[RM16  PC=%08X] Addr: %08X, val: %04X\n", m68k_get_reg(NULL, M68K_REG_PC), address, GET16(jaguar_mainRam, address));//*/
#ifndef USE_NEW_MMU
    unsigned int retVal = 0;

	// Note that the Jaguar only has 2M of RAM, not 4!
	if ((address >= 0x000000) && (address <= 0x1FFFFE))
//		retVal = (jaguar_mainRam[address] << 8) | jaguar_mainRam[address+1];
		retVal = GET16(jaguarMainRAM, address);
//	else if ((address >= 0x800000) && (address <= 0xDFFFFE))
	else if ((address >= 0x800000) && (address <= 0xDFFEFE))
	{
		// Memory Track reading...
		if (((TOMGetMEMCON1() & 0x0006) == (2 << 1)) && (jaguarMainROMCRC32 == 0xFDF37F47))
		{
			retVal = MTReadWord(address);
		}
		else
			retVal = (jaguarMainROM[address - 0x800000] << 8)
				| jaguarMainROM[address - 0x800000 + 1];
	}
	else if ((address >= 0xE00000) && (address <= 0xE3FFFE))
//		retVal = (jaguarBootROM[address - 0xE00000] << 8) | jaguarBootROM[address - 0xE00000 + 1];
//		retVal = (jaguarDevBootROM1[address - 0xE00000] << 8) | jaguarDevBootROM1[address - 0xE00000 + 1];
		retVal = (jagMemSpace[address] << 8) | jagMemSpace[address + 1];
	else if ((address >= 0xDFFF00) && (address <= 0xDFFFFE))
		retVal = CDROMReadWord(address, M68K);
	else if ((address >= 0xF00000) && (address <= 0xF0FFFE))
		retVal = TOMReadWord(address, M68K);
	else if ((address >= 0xF10000) && (address <= 0xF1FFFE))
		retVal = JERRYReadWord(address, M68K);
	else
		retVal = jaguar_unknown_readword(address, M68K);

//if (address >= 0xF1B000 && address <= 0xF1CFFF)
//	WriteLog("M68K: Read word $%04X at $%08X [PC=%08X]\n", retVal, address, m68k_get_reg(NULL, M68K_REG_PC));
//if (address >= 0x2800 && address <= 0x281F)
//	WriteLog("M68K: Read word $%04X at $%08X [PC=%08X]\n", retVal, address, m68k_get_reg(NULL, M68K_REG_PC));
//$8B3AE -> Transferred from $F1C010
//$8B5E4 -> Only +1 read at $808AA
//if (address >= 0x8B5E4 && address <= 0x8B5E4 + 16)
//	WriteLog("M68K: Read word $%04X at $%08X [PC=%08X]\n", retVal, address, m68k_get_reg(NULL, M68K_REG_PC));
    return retVal;
#else
	return MMURead16(address, M68K);
#endif
}


unsigned int m68k_read_memory_32(unsigned int address)
{
#ifdef ALPINE_FUNCTIONS
	// Check if breakpoint on memory is active, and deal with it
	if (bpmActive && address == bpmAddress1)
		M68KDebugHalt();
#endif

	// Musashi does this automagically for you, UAE core does not :-P
	address &= 0x00FFFFFF;
//; So, it seems that it stores the returned DWORD at $51136 and $FB074.
/*	if (address == 0x51136 || address == 0xFB074 || address == 0x1AF05E)
		WriteLog("[RM32  PC=%08X] Addr: %08X, val: %08X\n", m68k_get_reg(NULL, M68K_REG_PC), address, (m68k_read_memory_16(address) << 16) | m68k_read_memory_16(address + 2));//*/

//WriteLog("--> [RM32]\n");
#ifndef USE_NEW_MMU
	uint32_t retVal = 0;

	if ((address >= 0x800000) && (address <= 0xDFFEFE))
	{
		// Memory Track reading...
		if (((TOMGetMEMCON1() & 0x0006) == (2 << 1)) && (jaguarMainROMCRC32 == 0xFDF37F47))
			retVal = MTReadLong(address);
		else
			retVal = GET32(jaguarMainROM, address - 0x800000);

		return retVal;
	}

	return (m68k_read_memory_16(address) << 16) | m68k_read_memory_16(address + 2);
#else
	return MMURead32(address, M68K);
#endif
}


void m68k_write_memory_8(unsigned int address, unsigned int value)
{
#ifdef ALPINE_FUNCTIONS
	// Check if breakpoint on memory is active, and deal with it
	if (bpmActive && address == bpmAddress1)
		M68KDebugHalt();
#endif

	// Musashi does this automagically for you, UAE core does not :-P
	address &= 0x00FFFFFF;
#ifdef CPU_DEBUG_MEMORY
	// Note that the Jaguar only has 2M of RAM, not 4!
	if ((address >= 0x000000) && (address <= 0x1FFFFF))
	{
		if (startMemLog)
		{
			if (value > writeMemMax[address])
				writeMemMax[address] = value;
			if (value < writeMemMin[address])
				writeMemMin[address] = value;
		}
	}
#endif
/*if (address == 0x4E00)
	WriteLog("M68K: Writing %02X at %08X, PC=%08X\n", value, address, m68k_get_reg(NULL, M68K_REG_PC));//*/
//if ((address >= 0x1FF020 && address <= 0x1FF03F) || (address >= 0x1FF820 && address <= 0x1FF83F))
//	WriteLog("M68K: Writing %02X at %08X\n", value, address);
//WriteLog("[WM8  PC=%08X] Addr: %08X, val: %02X\n", m68k_get_reg(NULL, M68K_REG_PC), address, value);
/*if (effect_start)
	if (address >= 0x18FA70 && address < (0x18FA70 + 8000))
		WriteLog("M68K: Byte %02X written at %08X by 68K\n", value, address);//*/
//$53D0
/*if (address >= 0x53D0 && address <= 0x53FF)
	printf("M68K: Writing byte $%02X at $%08X, PC=$%08X\n", value, address, m68k_get_reg(NULL, M68K_REG_PC));//*/
//Testing AvP on UAE core...
//000075A0: FFFFF80E B6320220 (BITMAP)
/*if (address == 0x75A0 && value == 0xFF)
	printf("M68K: (8) Tripwire hit...\n");//*/

#ifndef USE_NEW_MMU
	// Note that the Jaguar only has 2M of RAM, not 4!
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
#else
	MMUWrite8(address, value, M68K);
#endif
}


void m68k_write_memory_16(unsigned int address, unsigned int value)
{
#ifdef ALPINE_FUNCTIONS
	// Check if breakpoint on memory is active, and deal with it
	if (bpmActive && address == bpmAddress1)
		M68KDebugHalt();
#endif

	// Musashi does this automagically for you, UAE core does not :-P
	address &= 0x00FFFFFF;
#ifdef CPU_DEBUG_MEMORY
	// Note that the Jaguar only has 2M of RAM, not 4!
	if ((address >= 0x000000) && (address <= 0x1FFFFE))
	{
		if (startMemLog)
		{
			uint8_t hi = value >> 8, lo = value & 0xFF;

			if (hi > writeMemMax[address])
				writeMemMax[address] = hi;
			if (hi < writeMemMin[address])
				writeMemMin[address] = hi;

			if (lo > writeMemMax[address+1])
				writeMemMax[address+1] = lo;
			if (lo < writeMemMin[address+1])
				writeMemMin[address+1] = lo;
		}
	}
#endif
/*if (address == 0x4E00)
	WriteLog("M68K: Writing %02X at %08X, PC=%08X\n", value, address, m68k_get_reg(NULL, M68K_REG_PC));//*/
//if ((address >= 0x1FF020 && address <= 0x1FF03F) || (address >= 0x1FF820 && address <= 0x1FF83F))
//	WriteLog("M68K: Writing %04X at %08X\n", value, address);
//WriteLog("[WM16 PC=%08X] Addr: %08X, val: %04X\n", m68k_get_reg(NULL, M68K_REG_PC), address, value);
//if (address >= 0xF02200 && address <= 0xF0229F)
//	WriteLog("M68K: Writing to blitter --> %04X at %08X\n", value, address);
//if (address >= 0x0E75D0 && address <= 0x0E75E7)
//	WriteLog("M68K: Writing %04X at %08X, M68K PC=%08X\n", value, address, m68k_get_reg(NULL, M68K_REG_PC));
/*extern uint32_t totalFrames;
if (address == 0xF02114)
	WriteLog("M68K: Writing to GPU_CTRL (frame:%u)... [M68K PC:%08X]\n", totalFrames, m68k_get_reg(NULL, M68K_REG_PC));
if (address == 0xF02110)
	WriteLog("M68K: Writing to GPU_PC (frame:%u)... [M68K PC:%08X]\n", totalFrames, m68k_get_reg(NULL, M68K_REG_PC));//*/
//if (address >= 0xF03B00 && address <= 0xF03DFF)
//	WriteLog("M68K: Writing %04X to %08X...\n", value, address);

/*if (address == 0x0100)//64*4)
	WriteLog("M68K: Wrote word to VI vector value %04X...\n", value);//*/
/*if (effect_start)
	if (address >= 0x18FA70 && address < (0x18FA70 + 8000))
		WriteLog("M68K: Word %04X written at %08X by 68K\n", value, address);//*/
/*	if (address == 0x51136 || address == 0x51138 || address == 0xFB074 || address == 0xFB076
		|| address == 0x1AF05E)
		WriteLog("[WM16  PC=%08X] Addr: %08X, val: %04X\n", m68k_get_reg(NULL, M68K_REG_PC), address, value);//*/
//$53D0
/*if (address >= 0x53D0 && address <= 0x53FF)
	printf("M68K: Writing word $%04X at $%08X, PC=$%08X\n", value, address, m68k_get_reg(NULL, M68K_REG_PC));//*/
//Testing AvP on UAE core...
//000075A0: FFFFF80E B6320220 (BITMAP)
/*if (address == 0x75A0 && value == 0xFFFF)
{
	printf("\nM68K: (16) Tripwire hit...\n");
	ShowM68KContext();
}//*/

#ifndef USE_NEW_MMU
	// Note that the Jaguar only has 2M of RAM, not 4!
	if ((address >= 0x000000) && (address <= 0x1FFFFE))
	{
/*		jaguar_mainRam[address] = value >> 8;
		jaguar_mainRam[address + 1] = value & 0xFF;*/
		SET16(jaguarMainRAM, address, value);
	}
	// Memory Track device writes....
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
#ifdef LOG_UNMAPPED_MEMORY_ACCESSES
		WriteLog("\tA0=%08X, A1=%08X, D0=%08X, D1=%08X\n",
			m68k_get_reg(NULL, M68K_REG_A0), m68k_get_reg(NULL, M68K_REG_A1),
			m68k_get_reg(NULL, M68K_REG_D0), m68k_get_reg(NULL, M68K_REG_D1));
#endif
	}
#else
	MMUWrite16(address, value, M68K);
#endif
}


void m68k_write_memory_32(unsigned int address, unsigned int value)
{
#ifdef ALPINE_FUNCTIONS
	// Check if breakpoint on memory is active, and deal with it
	if (bpmActive && address == bpmAddress1)
		M68KDebugHalt();
#endif

	// Musashi does this automagically for you, UAE core does not :-P
	address &= 0x00FFFFFF;
/*if (address == 0x4E00)
	WriteLog("M68K: Writing %02X at %08X, PC=%08X\n", value, address, m68k_get_reg(NULL, M68K_REG_PC));//*/
//WriteLog("--> [WM32]\n");
/*if (address == 0x0100)//64*4)
	WriteLog("M68K: Wrote dword to VI vector value %08X...\n", value);//*/
/*if (address >= 0xF03214 && address < 0xF0321F)
	WriteLog("M68K: Writing DWORD (%08X) to GPU RAM (%08X)...\n", value, address);//*/
//M68K: Writing DWORD (88E30047) to GPU RAM (00F03214)...
/*extern bool doGPUDis;
if (address == 0xF03214 && value == 0x88E30047)
//	start = true;
	doGPUDis = true;//*/
/*	if (address == 0x51136 || address == 0xFB074)
		WriteLog("[WM32  PC=%08X] Addr: %08X, val: %02X\n", m68k_get_reg(NULL, M68K_REG_PC), address, value);//*/
//Testing AvP on UAE core...
//000075A0: FFFFF80E B6320220 (BITMAP)
/*if (address == 0x75A0 && (value & 0xFFFF0000) == 0xFFFF0000)
{
	printf("\nM68K: (32) Tripwire hit...\n");
	ShowM68KContext();
}//*/

#ifndef USE_NEW_MMU
	m68k_write_memory_16(address, value >> 16);
	m68k_write_memory_16(address + 2, value & 0xFFFF);
#else
	MMUWrite32(address, value, M68K);
#endif
}


uint32_t JaguarGetHandler(uint32_t i)
{
	return JaguarReadLong(i * 4);
}


bool JaguarInterruptHandlerIsValid(uint32_t i) // Debug use only...
{
	uint32_t handler = JaguarGetHandler(i);
	return (handler && (handler != 0xFFFFFFFF) ? true : false);
}


void M68K_show_context(void)
{
	WriteLog("68K PC=%06X\n", m68k_get_reg(NULL, M68K_REG_PC));

	for(int i=M68K_REG_D0; i<=M68K_REG_D7; i++)
	{
		WriteLog("D%i = %08X ", i-M68K_REG_D0, m68k_get_reg(NULL, (m68k_register_t)i));

		if (i == M68K_REG_D3 || i == M68K_REG_D7)
			WriteLog("\n");
	}

	for(int i=M68K_REG_A0; i<=M68K_REG_A7; i++)
	{
		WriteLog("A%i = %08X ", i-M68K_REG_A0, m68k_get_reg(NULL, (m68k_register_t)i));

		if (i == M68K_REG_A3 || i == M68K_REG_A7)
			WriteLog("\n");
	}

	WriteLog("68K disasm\n");
//	jaguar_dasm(s68000readPC()-0x1000,0x20000);
	JaguarDasm(m68k_get_reg(NULL, M68K_REG_PC) - 0x80, 0x200);
//	jaguar_dasm(0x5000, 0x14414);

//	WriteLog("\n.......[Cart start]...........\n\n");
//	jaguar_dasm(0x192000, 0x1000);//0x200);

	WriteLog("..................\n");

	if (TOMIRQEnabled(IRQ_VIDEO))
	{
		WriteLog("video int: enabled\n");
		JaguarDasm(JaguarGetHandler(64), 0x200);
	}
	else
		WriteLog("video int: disabled\n");

	WriteLog("..................\n");

	for(int i=0; i<256; i++)
	{
		WriteLog("handler %03i at ", i);//$%08X\n", i, (unsigned int)JaguarGetHandler(i));
		uint32_t address = (uint32_t)JaguarGetHandler(i);

		if (address == 0)
			WriteLog(".........\n");
		else
			WriteLog("$%08X\n", address);
	}
}


//
// Unknown read/write byte/word routines
//

// It's hard to believe that developers would be sloppy with their memory
// writes, yet in some cases the developers screwed up royal. E.g., Club Drive
// has the following code:
//
// 807EC4: movea.l #$f1b000, A1
// 807ECA: movea.l #$8129e0, A0
// 807ED0: move.l  A0, D0
// 807ED2: move.l  #$f1bb94, D1
// 807ED8: sub.l   D0, D1
// 807EDA: lsr.l   #2, D1
// 807EDC: move.l  (A0)+, (A1)+
// 807EDE: dbra    D1, 807edc
//
// The problem is at $807ED0--instead of putting A0 into D0, they really meant
// to put A1 in. This mistake causes it to try and overwrite approximately
// $700000 worth of address space! (That is, unless the 68K causes a bus
// error...)

void jaguar_unknown_writebyte(unsigned address, unsigned data, uint32_t who/*=UNKNOWN*/)
{
#ifdef LOG_UNMAPPED_MEMORY_ACCESSES
	WriteLog("Jaguar: Unknown byte %02X written at %08X by %s (M68K PC=%06X)\n", data, address, whoName[who], m68k_get_reg(NULL, M68K_REG_PC));
#endif
#ifdef ABORT_ON_UNMAPPED_MEMORY_ACCESS
//	extern bool finished;
	finished = true;
//	extern bool doDSPDis;
	if (who == DSP)
		doDSPDis = true;
#endif
}


void jaguar_unknown_writeword(unsigned address, unsigned data, uint32_t who/*=UNKNOWN*/)
{
#ifdef LOG_UNMAPPED_MEMORY_ACCESSES
	WriteLog("Jaguar: Unknown word %04X written at %08X by %s (M68K PC=%06X)\n", data, address, whoName[who], m68k_get_reg(NULL, M68K_REG_PC));
#endif
#ifdef ABORT_ON_UNMAPPED_MEMORY_ACCESS
//	extern bool finished;
	finished = true;
//	extern bool doDSPDis;
	if (who == DSP)
		doDSPDis = true;
#endif
}


unsigned jaguar_unknown_readbyte(unsigned address, uint32_t who/*=UNKNOWN*/)
{
#ifdef LOG_UNMAPPED_MEMORY_ACCESSES
	WriteLog("Jaguar: Unknown byte read at %08X by %s (M68K PC=%06X)\n", address, whoName[who], m68k_get_reg(NULL, M68K_REG_PC));
#endif
#ifdef ABORT_ON_UNMAPPED_MEMORY_ACCESS
//	extern bool finished;
	finished = true;
//	extern bool doDSPDis;
	if (who == DSP)
		doDSPDis = true;
#endif
    return 0xFF;
}


unsigned jaguar_unknown_readword(unsigned address, uint32_t who/*=UNKNOWN*/)
{
#ifdef LOG_UNMAPPED_MEMORY_ACCESSES
	WriteLog("Jaguar: Unknown word read at %08X by %s (M68K PC=%06X)\n", address, whoName[who], m68k_get_reg(NULL, M68K_REG_PC));
#endif
#ifdef ABORT_ON_UNMAPPED_MEMORY_ACCESS
//	extern bool finished;
	finished = true;
//	extern bool doDSPDis;
	if (who == DSP)
		doDSPDis = true;
#endif
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


void JaguarDasm(uint32_t offset, uint32_t qt)
{
#ifdef CPU_DEBUG
	static char buffer[2048];//, mem[64];
	int pc = offset, oldpc;

	for(uint32_t i=0; i<qt; i++)
	{
/*		oldpc = pc;
		for(int j=0; j<64; j++)
			mem[j^0x01] = jaguar_byte_read(pc + j);

		pc += Dasm68000((char *)mem, buffer, 0);
		WriteLog("%08X: %s\n", oldpc, buffer);//*/
		oldpc = pc;
		pc += m68k_disassemble(buffer, pc, 0);//M68K_CPU_TYPE_68000);
		WriteLog("%08X: %s\n", oldpc, buffer);//*/
	}
#endif
}


uint8_t JaguarReadByte(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	uint8_t data = 0x00;
	offset &= 0xFFFFFF;

	// First 2M is mirrored in the $0 - $7FFFFF range
	if (offset < 0x800000)
		data = jaguarMainRAM[offset & 0x1FFFFF];
	else if ((offset >= 0x800000) && (offset < 0xDFFF00))
		data = jaguarMainROM[offset - 0x800000];
	else if ((offset >= 0xDFFF00) && (offset <= 0xDFFFFF))
		data = CDROMReadByte(offset, who);
	else if ((offset >= 0xE00000) && (offset < 0xE40000))
//		data = jaguarBootROM[offset & 0x3FFFF];
//		data = jaguarDevBootROM1[offset & 0x3FFFF];
		data = jagMemSpace[offset];
	else if ((offset >= 0xF00000) && (offset < 0xF10000))
		data = TOMReadByte(offset, who);
	else if ((offset >= 0xF10000) && (offset < 0xF20000))
		data = JERRYReadByte(offset, who);
	else
		data = jaguar_unknown_readbyte(offset, who);

	return data;
}


uint16_t JaguarReadWord(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	offset &= 0xFFFFFF;

	// First 2M is mirrored in the $0 - $7FFFFF range
	if (offset < 0x800000)
	{
		return (jaguarMainRAM[(offset+0) & 0x1FFFFF] << 8) | jaguarMainRAM[(offset+1) & 0x1FFFFF];
	}
	else if ((offset >= 0x800000) && (offset < 0xDFFF00))
	{
		offset -= 0x800000;
		return (jaguarMainROM[offset+0] << 8) | jaguarMainROM[offset+1];
	}
//	else if ((offset >= 0xDFFF00) && (offset < 0xDFFF00))
	else if ((offset >= 0xDFFF00) && (offset <= 0xDFFFFE))
		return CDROMReadWord(offset, who);
	else if ((offset >= 0xE00000) && (offset <= 0xE3FFFE))
//		return (jaguarBootROM[(offset+0) & 0x3FFFF] << 8) | jaguarBootROM[(offset+1) & 0x3FFFF];
//		return (jaguarDevBootROM1[(offset+0) & 0x3FFFF] << 8) | jaguarDevBootROM1[(offset+1) & 0x3FFFF];
		return (jagMemSpace[offset + 0] << 8) | jagMemSpace[offset + 1];
	else if ((offset >= 0xF00000) && (offset <= 0xF0FFFE))
		return TOMReadWord(offset, who);
	else if ((offset >= 0xF10000) && (offset <= 0xF1FFFE))
		return JERRYReadWord(offset, who);

	return jaguar_unknown_readword(offset, who);
}


void JaguarWriteByte(uint32_t offset, uint8_t data, uint32_t who/*=UNKNOWN*/)
{
/*	if ((offset & 0x1FFFFF) >= 0xE00 && (offset & 0x1FFFFF) < 0xE18)
	{
		WriteLog("JWB: Byte %02X written at %08X by %s\n", data, offset, whoName[who]);
	}//*/
/*	if (offset >= 0x4E00 && offset < 0x4E04)
		WriteLog("JWB: Byte %02X written at %08X by %s\n", data, offset, whoName[who]);//*/
//Need to check for writes in the range of $18FA70 + 8000...
/*if (effect_start)
	if (offset >= 0x18FA70 && offset < (0x18FA70 + 8000))
		WriteLog("JWB: Byte %02X written at %08X by %s\n", data, offset, whoName[who]);//*/

	offset &= 0xFFFFFF;

	// First 2M is mirrored in the $0 - $7FFFFF range
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


uint32_t starCount;
void JaguarWriteWord(uint32_t offset, uint16_t data, uint32_t who/*=UNKNOWN*/)
{
/*	if ((offset & 0x1FFFFF) >= 0xE00 && (offset & 0x1FFFFF) < 0xE18)
	{
		WriteLog("JWW: Word %04X written at %08X by %s\n", data, offset, whoName[who]);
		WriteLog("     GPU PC = $%06X\n", GPUReadLong(0xF02110, DEBUG));
	}//*/
/*	if (offset >= 0x4E00 && offset < 0x4E04)
		WriteLog("JWW: Word %04X written at %08X by %s\n", data, offset, whoName[who]);//*/
/*if (offset == 0x0100)//64*4)
	WriteLog("M68K: %s wrote word to VI vector value %04X...\n", whoName[who], data);
if (offset == 0x0102)//64*4)
	WriteLog("M68K: %s wrote word to VI vector+2 value %04X...\n", whoName[who], data);//*/
//TEMP--Mirror of F03000? Yes, but only 32-bit CPUs can do it (i.e., NOT the 68K!)
// PLUS, you would handle this in the GPU/DSP WriteLong code! Not here!
//Need to check for writes in the range of $18FA70 + 8000...
/*if (effect_start)
	if (offset >= 0x18FA70 && offset < (0x18FA70 + 8000))
		WriteLog("JWW: Word %04X written at %08X by %s\n", data, offset, whoName[who]);//*/
/*if (offset >= 0x2C00 && offset <= 0x2CFF)
	WriteLog("Jaguar: Word %04X written to TOC+%02X by %s\n", data, offset-0x2C00, whoName[who]);//*/

	offset &= 0xFFFFFF;

	// First 2M is mirrored in the $0 - $7FFFFF range
	if (offset <= 0x7FFFFE)
	{
/*
GPU Table (CD BIOS)

1A 69 F0 ($0000) -> Starfield
1A 73 C8 ($0001) -> Final clearing blit & bitmap blit?
1A 79 F0 ($0002)
1A 88 C0 ($0003)
1A 8F E8 ($0004) -> "Jaguar" small color logo?
1A 95 20 ($0005)
1A 9F 08 ($0006)
1A A1 38 ($0007)
1A AB 38 ($0008)
1A B3 C8 ($0009)
1A B9 C0 ($000A)
*/

//This MUST be done by the 68K!
/*if (offset == 0x670C)
	WriteLog("Jaguar: %s writing to location $670C...\n", whoName[who]);*/

/*extern bool doGPUDis;
//if ((offset == 0x100000 + 75522) && who == GPU)	// 76,226 -> 75522
if ((offset == 0x100000 + 128470) && who == GPU)	// 107,167 -> 128470 (384 x 250 screen size 16BPP)
//if ((offset >= 0x100000 && offset <= 0x12C087) && who == GPU)
	doGPUDis = true;//*/
/*if (offset == 0x100000 + 128470) // 107,167 -> 128470 (384 x 250 screen size 16BPP)
	WriteLog("JWW: Writing value %04X at %08X by %s...\n", data, offset, whoName[who]);
if ((data & 0xFF00) != 0x7700)
	WriteLog("JWW: Writing value %04X at %08X by %s...\n", data, offset, whoName[who]);//*/
/*if ((offset >= 0x100000 && offset <= 0x147FFF) && who == GPU)
	return;//*/
/*if ((data & 0xFF00) != 0x7700 && who == GPU)
	WriteLog("JWW: Writing value %04X at %08X by %s...\n", data, offset, whoName[who]);//*/
/*if ((offset >= 0x100000 + 0x48000 && offset <= 0x12C087 + 0x48000) && who == GPU)
	return;//*/
/*extern bool doGPUDis;
if (offset == 0x120216 && who == GPU)
	doGPUDis = true;//*/
/*extern uint32_t gpu_pc;
if (who == GPU && (gpu_pc == 0xF03604 || gpu_pc == 0xF03638))
{
	uint32_t base = offset - (offset > 0x148000 ? 0x148000 : 0x100000);
	uint32_t y = base / 0x300;
	uint32_t x = (base - (y * 0x300)) / 2;
	WriteLog("JWW: Writing starfield star %04X at %08X (%u/%u) [%s]\n", data, offset, x, y, (gpu_pc == 0xF03604 ? "s" : "L"));
}//*/
/*
JWW: Writing starfield star 775E at 0011F650 (555984/1447)
*/
//if (offset == (0x001E17F8 + 0x34))
/*if (who == GPU && offset == (0x001E17F8 + 0x34))
	data = 0xFE3C;//*/
//	WriteLog("JWW: Write at %08X written to by %s.\n", 0x001E17F8 + 0x34, whoName[who]);//*/
/*extern uint32_t gpu_pc;
if (who == GPU && (gpu_pc == 0xF03604 || gpu_pc == 0xF03638))
{
	extern int objectPtr;
//	if (offset > 0x148000)
//		return;
	starCount++;
	if (starCount > objectPtr)
		return;

//	if (starCount == 1)
//		WriteLog("--> Drawing 1st star...\n");
//
//	uint32_t base = offset - (offset > 0x148000 ? 0x148000 : 0x100000);
//	uint32_t y = base / 0x300;
//	uint32_t x = (base - (y * 0x300)) / 2;
//	WriteLog("JWW: Writing starfield star %04X at %08X (%u/%u) [%s]\n", data, offset, x, y, (gpu_pc == 0xF03604 ? "s" : "L"));

//A star of interest...
//-->JWW: Writing starfield star 77C9 at 0011D31A (269/155) [s]
//1st trail +3(x), -1(y) -> 272, 154 -> 0011D020
//JWW: Blitter writing echo 77B3 at 0011D022...
}//*/
//extern bool doGPUDis;
/*if (offset == 0x11D022 + 0x48000 || offset == 0x11D022)// && who == GPU)
{
//	doGPUDis = true;
	WriteLog("JWW: %s writing echo %04X at %08X...\n", whoName[who], data, offset);
//	LogBlit();
}
if (offset == 0x11D31A + 0x48000 || offset == 0x11D31A)
	WriteLog("JWW: %s writing star %04X at %08X...\n", whoName[who], data, offset);//*/

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
	// Don't bomb on attempts to write to ROM
	else if (offset >= 0x800000 && offset <= 0xEFFFFF)
		return;

	jaguar_unknown_writeword(offset, data, who);
}


// We really should re-do this so that it does *real* 32-bit access... !!! FIX !!!
uint32_t JaguarReadLong(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	return (JaguarReadWord(offset, who) << 16) | JaguarReadWord(offset+2, who);
}


// We really should re-do this so that it does *real* 32-bit access... !!! FIX !!!
void JaguarWriteLong(uint32_t offset, uint32_t data, uint32_t who/*=UNKNOWN*/)
{
/*	extern bool doDSPDis;
	if (offset < 0x400 && !doDSPDis)
	{
		WriteLog("JLW: Write to %08X by %s... Starting DSP log!\n\n", offset, whoName[who]);
		doDSPDis = true;
	}//*/
/*if (offset == 0x0100)//64*4)
	WriteLog("M68K: %s wrote dword to VI vector value %08X...\n", whoName[who], data);//*/

	JaguarWriteWord(offset, data >> 16, who);
	JaguarWriteWord(offset+2, data & 0xFFFF, who);
}


void JaguarSetScreenBuffer(uint32_t * buffer)
{
	// This is in TOM, but we set it here...
	screenBuffer = buffer;
}


void JaguarSetScreenPitch(uint32_t pitch)
{
	// This is in TOM, but we set it here...
	screenPitch = pitch;
}


//
// Jaguar console initialization
//
void JaguarInit(void)
{
	// For randomizing RAM
	srand(time(NULL));

	// Contents of local RAM are quasi-stable; we simulate this by randomizing RAM contents
	for(uint32_t i=0; i<0x200000; i+=4)
		*((uint32_t *)(&jaguarMainRAM[i])) = rand();

#ifdef CPU_DEBUG_MEMORY
	memset(readMem, 0x00, 0x400000);
	memset(writeMemMin, 0xFF, 0x400000);
	memset(writeMemMax, 0x00, 0x400000);
#endif
//	memset(jaguarMainRAM, 0x00, 0x200000);
//	memset(jaguar_mainRom, 0xFF, 0x200000);	// & set it to all Fs...
//	memset(jaguar_mainRom, 0x00, 0x200000);	// & set it to all 0s...
//NOTE: This *doesn't* fix FlipOut...
//Or does it? Hmm...
//Seems to want $01010101... Dunno why. Investigate!
//	memset(jaguarMainROM, 0x01, 0x600000);	// & set it to all 01s...
//	memset(jaguar_mainRom, 0xFF, 0x600000);	// & set it to all Fs...
	lowerField = false;							// Reset the lower field flag
//temp, for crappy crap that sux
memset(jaguarMainRAM + 0x804, 0xFF, 4);

	m68k_pulse_reset();							// Need to do this so UAE disasm doesn't segfault on exit
	GPUInit();
	DSPInit();
	TOMInit();
	JERRYInit();
	CDROMInit();
}


//New timer based code stuffola...
void HalflineCallback(void);
void RenderCallback(void);
void JaguarReset(void)
{
	// Only problem with this approach: It wipes out RAM loaded files...!
	// Contents of local RAM are quasi-stable; we simulate this by randomizing RAM contents
	for(uint32_t i=8; i<0x200000; i+=4)
		*((uint32_t *)(&jaguarMainRAM[i])) = rand();

	// New timer base code stuffola...
	InitializeEventList();
//Need to change this so it uses the single RAM space and load the BIOS
//into it somewhere...
//Also, have to change this here and in JaguarReadXX() currently
	// Only use the system BIOS if it's available...! (it's always available now!)
	// AND only if a jaguar cartridge has been inserted.
	if (vjs.useJaguarBIOS && jaguarCartInserted && !vjs.hardwareTypeAlpine)
		memcpy(jaguarMainRAM, jagMemSpace + 0xE00000, 8);
	else
		SET32(jaguarMainRAM, 4, jaguarRunAddress);

//	WriteLog("jaguar_reset():\n");
	TOMReset();
	JERRYReset();
	GPUReset();
	DSPReset();
	CDROMReset();
    m68k_pulse_reset();								// Reset the 68000
	WriteLog("Jaguar: 68K reset. PC=%06X SP=%08X\n", m68k_get_reg(NULL, M68K_REG_PC), m68k_get_reg(NULL, M68K_REG_A7));

	lowerField = false;								// Reset the lower field flag
//	SetCallbackTime(ScanlineCallback, 63.5555);
//	SetCallbackTime(ScanlineCallback, 31.77775);
	SetCallbackTime(HalflineCallback, (vjs.hardwareTypeNTSC ? 31.777777777 : 32.0));
}


void JaguarDone(void)
{
#ifdef CPU_DEBUG_MEMORY
/*	WriteLog("\nJaguar: Memory Usage Stats (return addresses)\n\n");

	for(uint32_t i=0; i<=raPtr; i++)
	{
		WriteLog("\t%08X\n", returnAddr[i]);
		WriteLog("M68000 disassembly at $%08X...\n", returnAddr[i] - 16);
		jaguar_dasm(returnAddr[i] - 16, 16);
		WriteLog("\n");
	}
	WriteLog("\n");//*/

/*	int start = 0, end = 0;
	bool endTriggered = false, startTriggered = false;
	for(int i=0; i<0x400000; i++)
	{
		if (readMem[i] && writeMemMin[i] != 0xFF && writeMemMax != 0x00)
		{
			if (!startTriggered)
				startTriggered = true, endTriggered = false, start = i;

			WriteLog("\t\tMin/Max @ %06X: %u/%u\n", i, writeMemMin[i], writeMemMax[i]);
		}
		else
		{
			if (!endTriggered)
			{
				end = i - 1, endTriggered = true, startTriggered = false;
				WriteLog("\tMemory range accessed: %06X - %06X\n", start, end);
			}
		}
	}
	WriteLog("\n");//*/
#endif
//#ifdef CPU_DEBUG
//	for(int i=M68K_REG_A0; i<=M68K_REG_A7; i++)
//		WriteLog("\tA%i = 0x%.8x\n", i-M68K_REG_A0, m68k_get_reg(NULL, (m68k_register_t)i));
	int32_t topOfStack = m68k_get_reg(NULL, M68K_REG_A7);
	WriteLog("M68K: Top of stack: %08X -> (%08X). Stack trace:\n", topOfStack, JaguarReadLong(topOfStack));
#if 0
	for(int i=-2; i<9; i++)
		WriteLog("%06X: %08X\n", topOfStack + (i * 4), JaguarReadLong(topOfStack + (i * 4)));
#else
	uint32_t address = topOfStack - (4 * 4 * 3);

	for(int i=0; i<10; i++)
	{
		WriteLog("%06X:", address);

		for(int j=0; j<4; j++)
		{
			WriteLog(" %08X", JaguarReadLong(address));
			address += 4;
		}

		WriteLog("\n");
	}
#endif

/*	WriteLog("\nM68000 disassembly at $802288...\n");
	jaguar_dasm(0x802288, 3);
	WriteLog("\nM68000 disassembly at $802200...\n");
	jaguar_dasm(0x802200, 500);
	WriteLog("\nM68000 disassembly at $802518...\n");
	jaguar_dasm(0x802518, 100);//*/

/*	WriteLog("\n\nM68000 disassembly at $803F00 (look @ $803F2A)...\n");
	jaguar_dasm(0x803F00, 500);
	WriteLog("\n");//*/

/*	WriteLog("\n\nM68000 disassembly at $802B00 (look @ $802B5E)...\n");
	jaguar_dasm(0x802B00, 500);
	WriteLog("\n");//*/

/*	WriteLog("\n\nM68000 disassembly at $809900 (look @ $8099F8)...\n");
	jaguar_dasm(0x809900, 500);
	WriteLog("\n");//*/
//8099F8
/*	WriteLog("\n\nDump of $8093C8:\n\n");
	for(int i=0x8093C8; i<0x809900; i+=4)
		WriteLog("%06X: %08X\n", i, JaguarReadLong(i));//*/
/*	WriteLog("\n\nM68000 disassembly at $90006C...\n");
	jaguar_dasm(0x90006C, 500);
	WriteLog("\n");//*/
/*	WriteLog("\n\nM68000 disassembly at $1AC000...\n");
	jaguar_dasm(0x1AC000, 6000);
	WriteLog("\n");//*/

//	WriteLog("Jaguar: CD BIOS version %04X\n", JaguarReadWord(0x3004));
	WriteLog("Jaguar: Interrupt enable = $%02X\n", TOMReadByte(0xF000E1, JAGUAR) & 0x1F);
	WriteLog("Jaguar: Video interrupt is %s (line=%u)\n", ((TOMIRQEnabled(IRQ_VIDEO))
		&& (JaguarInterruptHandlerIsValid(64))) ? "enabled" : "disabled", TOMReadWord(0xF0004E, JAGUAR));
	M68K_show_context();
//#endif

	CDROMDone();
	GPUDone();
	DSPDone();
	TOMDone();
	JERRYDone();

	// temp, until debugger is in place
//00802016: jsr     $836F1A.l
//0080201C: jsr     $836B30.l
//00802022: jsr     $836B18.l
//00802028: jsr     $8135F0.l
//00813C1E: jsr     $813F76.l
//00802038: jsr     $836D00.l
//00802098: jsr     $8373A4.l
//008020A2: jsr     $83E24A.l
//008020BA: jsr     $83E156.l
//008020C6: jsr     $83E19C.l
//008020E6: jsr     $8445E8.l
//008020EC: jsr     $838C20.l
//0080211A: jsr     $838ED6.l
//00802124: jsr     $89CA56.l
//0080212A: jsr     $802B48.l
#if 0
	WriteLog("-------------------------------------------\n");
	JaguarDasm(0x8445E8, 0x200);
	WriteLog("-------------------------------------------\n");
	JaguarDasm(0x838C20, 0x200);
	WriteLog("-------------------------------------------\n");
	JaguarDasm(0x838ED6, 0x200);
	WriteLog("-------------------------------------------\n");
	JaguarDasm(0x89CA56, 0x200);
	WriteLog("-------------------------------------------\n");
	JaguarDasm(0x802B48, 0x200);
	WriteLog("\n\nM68000 disassembly at $802000...\n");
	JaguarDasm(0x802000, 6000);
	WriteLog("\n");//*/
#endif
/*	WriteLog("\n\nM68000 disassembly at $6004...\n");
	JaguarDasm(0x6004, 10000);
	WriteLog("\n");//*/
//	WriteLog("\n\nM68000 disassembly at $802000...\n");
//	JaguarDasm(0x802000, 0x1000);
//	WriteLog("\n\nM68000 disassembly at $4100...\n");
//	JaguarDasm(0x4100, 200);
//	WriteLog("\n\nM68000 disassembly at $800800...\n");
//	JaguarDasm(0x800800, 0x1000);
}


// Temp debugging stuff

void DumpMainMemory(void)
{
	FILE * fp = fopen("./memdump.bin", "wb");

	if (fp == NULL)
		return;

	fwrite(jaguarMainRAM, 1, 0x200000, fp);
	fclose(fp);
}


uint8_t * GetRamPtr(void)
{
	return jaguarMainRAM;
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
//WriteLog("JEN: Time to next event (%u) is %f usec (%u RISC cycles)...\n", nextEvent, timeToNextEvent, USEC_TO_RISC_CYCLES(timeToNextEvent));

		m68k_execute(USEC_TO_M68K_CYCLES(timeToNextEvent));

		if (vjs.GPUEnabled)
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
//	uint16_t vbb = TOMReadWord(0xF00040, JAGUAR);
	vc++;

	// Each # of lines is for a full frame == 1/30s (NTSC), 1/25s (PAL).
	// So we cut the number of half-lines in a frame in half. :-P
	uint16_t numHalfLines = ((vjs.hardwareTypeNTSC ? 525 : 625) * 2) / 2;

	if ((vc & 0x7FF) >= numHalfLines)
	{
		lowerField = !lowerField;
		// If we're rendering the lower field, set the high bit (#11, counting
		// from 0) of VC
		vc = (lowerField ? 0x0800 : 0x0000);
	}

//WriteLog("HLC: Currently on line %u (VP=%u)...\n", vc, vp);
	TOMWriteWord(0xF00006, vc, JAGUAR);

	// Time for Vertical Interrupt?
	if ((vc & 0x7FF) == vi && (vc & 0x7FF) > 0 && TOMIRQEnabled(IRQ_VIDEO))
	{
		// We don't have to worry about autovectors & whatnot because the Jaguar
		// tells you through its HW registers who sent the interrupt...
		TOMSetPendingVideoInt();
		m68k_set_irq(2);
	}

	TOMExecHalfline(vc, true);

//Change this to VBB???
//Doesn't seem to matter (at least for Flip Out & I-War)
	if ((vc & 0x7FF) == 0)
//	if (vc == vbb)
	{
		JoystickExec();
		frameDone = true;
	}//*/

	SetCallbackTime(HalflineCallback, (vjs.hardwareTypeNTSC ? 31.777777777 : 32.0));
}

