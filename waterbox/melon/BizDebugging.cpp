#include "NDS.h"
#include "NDSCart.h"
#include "GBACart.h"
#include "DSi.h"
#include "ARM.h"
#include "SPI.h"

#include "dthumb.h"

#include <waterboxcore.h>

void (*InputCallback)() = nullptr;

ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}

void (*ReadCallback)(u32) = nullptr;
void (*WriteCallback)(u32) = nullptr;
void (*ExecuteCallback)(u32) = nullptr;

ECL_EXPORT void SetMemoryCallback(u32 which, void (*callback)(u32 addr))
{
	switch (which)
	{
		case 0: ReadCallback = callback; break;
		case 1: WriteCallback = callback; break;
		case 2: ExecuteCallback = callback; break;
	}
}

TraceMask_t TraceMask = TRACE_NONE;
static void (*TraceCallback)(TraceMask_t, u32, u32*, char*, u32) = nullptr;

ECL_EXPORT void SetTraceCallback(void (*callback)(TraceMask_t mask, u32 opcode, u32* regs, char* disasm, u32 cyclesOff), TraceMask_t mask)
{
	TraceCallback = callback;
	TraceMask = callback ? mask : TRACE_NONE;
}

ECL_EXPORT void GetDisassembly(TraceMask_t type, u32 opcode, char* ret)
{
	static char disasm[DTHUMB_STRING_LENGTH];

	memset(disasm, 0, sizeof disasm);
	switch (type)
	{
		case TRACE_ARM7_THUMB: Disassemble_thumb(opcode, disasm, ARMv4T); break;
		case TRACE_ARM7_ARM: Disassemble_arm(opcode, disasm, ARMv4T); break;
		case TRACE_ARM9_THUMB: Disassemble_thumb(opcode, disasm, ARMv5TE); break;
		case TRACE_ARM9_ARM: Disassemble_arm(opcode, disasm, ARMv5TE); break;
		default: __builtin_unreachable();
	}

	memcpy(ret, disasm, DTHUMB_STRING_LENGTH);
}

void TraceTrampoline(TraceMask_t type, u32* regs, u32 opcode)
{
	static char disasm[DTHUMB_STRING_LENGTH];

	memset(disasm, 0, sizeof(disasm));
	switch (type)
	{
		case TRACE_ARM7_THUMB: Disassemble_thumb(opcode, disasm, ARMv4T); break;
		case TRACE_ARM7_ARM: Disassemble_arm(opcode, disasm, ARMv4T); break;
		case TRACE_ARM9_THUMB: Disassemble_thumb(opcode, disasm, ARMv5TE); break;
		case TRACE_ARM9_ARM: Disassemble_arm(opcode, disasm, ARMv5TE); break;
		default: __builtin_unreachable();
	}

	TraceCallback(type, opcode, regs, disasm, NDS::GetSysClockCycles(2));
}

ECL_EXPORT void GetRegs(u32* regs)
{
	for (int i = 0; i < 16; i++)
	{
		*regs++ = NDS::ARM9->R[i];
	}

	for (int i = 0; i < 16; i++)
	{
		*regs++ = NDS::ARM7->R[i];
	}
}

ECL_EXPORT void SetReg(s32 ncpu, s32 index, s32 val)
{
	if (ncpu)
	{
		NDS::ARM7->R[index] = val;
	}
	else
	{
		NDS::ARM9->R[index] = val;
	}
}

/* excerpted from gbatek

NDS9 Memory Map

  00000000h  Instruction TCM (32KB) (not moveable) (mirror-able to 1000000h)
  0xxxx000h  Data TCM        (16KB) (moveable)
  02000000h  Main Memory     (4MB)
  03000000h  Shared WRAM     (0KB, 16KB, or 32KB can be allocated to ARM9)
  04000000h  ARM9-I/O Ports
  05000000h  Standard Palettes (2KB) (Engine A BG/OBJ, Engine B BG/OBJ)
  06000000h  VRAM - Engine A, BG VRAM  (max 512KB)
  06200000h  VRAM - Engine B, BG VRAM  (max 128KB)
  06400000h  VRAM - Engine A, OBJ VRAM (max 256KB)
  06600000h  VRAM - Engine B, OBJ VRAM (max 128KB)
  06800000h  VRAM - "LCDC"-allocated (max 656KB)
  07000000h  OAM (2KB) (Engine A, Engine B)
  08000000h  GBA Slot ROM (max 32MB)
  0A000000h  GBA Slot RAM (max 64KB)
  FFFF0000h  ARM9-BIOS (32KB) (only 3K used)

NDS7 Memory Map

  00000000h  ARM7-BIOS (16KB)
  02000000h  Main Memory (4MB)
  03000000h  Shared WRAM (0KB, 16KB, or 32KB can be allocated to ARM7)
  03800000h  ARM7-WRAM (64KB)
  04000000h  ARM7-I/O Ports
  04800000h  Wireless Communications Wait State 0 (8KB RAM at 4804000h)
  04808000h  Wireless Communications Wait State 1 (I/O Ports at 4808000h)
  06000000h  VRAM allocated as Work RAM to ARM7 (max 256K)
  08000000h  GBA Slot ROM (max 32MB)
  0A000000h  GBA Slot RAM (max 64KB)

Further Memory (not mapped to ARM9/ARM7 bus)

  3D Engine Polygon RAM (52KBx2)
  3D Engine Vertex RAM (72KBx2)
  Firmware (256KB) (built-in serial flash memory)
  GBA-BIOS (16KB) (not used in NDS mode)
  NDS Slot ROM (serial 8bit-bus, max 4GB with default protocol)
  NDS Slot FLASH/EEPROM/FRAM (serial 1bit-bus)

*/

template<bool arm9>
static bool SafeToPeek(u32 addr)
{
	if (arm9)
	{
		switch (addr)
		{
			case 0x04000130:
			case 0x04000131:
			case 0x04000600:
			case 0x04000601:
			case 0x04000602:
			case 0x04000603:
				return false;
		}
	}
	else // arm7
	{
		if (addr >= 0x04800000 && addr <= 0x04810000)
		{
			if (addr & 1) addr--;
			addr &= 0x7FFE;
			if (addr == 0x044 || addr == 0x060)
				return false;
		}
	}

	return true;
}

static void ARM9Access(u8* buffer, s64 address, s64 count, bool write)
{
	if (write)
	{
		void (*Write)(u32, u8) = NDS::ConsoleType == 1 ? DSi::ARM9Write8 : NDS::ARM9Write8;
		while (count--)
		{
			if (address < NDS::ARM9->ITCMSize)
			{
				NDS::ARM9->ITCM[address++ & (ITCMPhysicalSize - 1)] = *buffer++;
			}
			else if ((address & NDS::ARM9->DTCMMask) == NDS::ARM9->DTCMBase)
			{
				NDS::ARM9->DTCM[address++ & (DTCMPhysicalSize - 1)] = *buffer++;
			}
			else
			{
				Write(address++, *buffer++);
			}
		}
	}
	else
	{
		u8 (*Read)(u32) = NDS::ConsoleType == 1 ? DSi::ARM9Read8 : NDS::ARM9Read8;
		while (count--)
		{
			if (address < NDS::ARM9->ITCMSize)
			{
				*buffer++ = NDS::ARM9->ITCM[address & (ITCMPhysicalSize - 1)];
			}
			else if ((address & NDS::ARM9->DTCMMask) == NDS::ARM9->DTCMBase)
			{
				*buffer++ = NDS::ARM9->DTCM[address & (DTCMPhysicalSize - 1)];
			}
			else
			{
				*buffer++ = SafeToPeek<true>(address) ? Read(address) : 0;
			}

			address++;
		}
	}
}

static void ARM7Access(u8* buffer, s64 address, s64 count, bool write)
{
	if (write)
	{
		void (*Write)(u32, u8) = NDS::ConsoleType == 1 ? DSi::ARM7Write8 : NDS::ARM7Write8;
		while (count--)
			Write(address++, *buffer++);
	}
	else
	{
		u8 (*Read)(u32) = NDS::ConsoleType == 1 ? DSi::ARM7Read8 : NDS::ARM7Read8;
		while (count--)
			*buffer++ = SafeToPeek<true>(address) ? Read(address) : 0, address++;
	}
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	int i = 0;
	#define ADD_MEMORY_DOMAIN(name, data, size, flags) do \
	{ \
		m[i].Data = (void*)data; \
		m[i].Name = name; \
		m[i].Size = size; \
		m[i].Flags = flags; \
		i++; \
	} while (0)

	ADD_MEMORY_DOMAIN("Main RAM", NDS::MainRAM, NDS::ConsoleType == 1 ? NDS::MainRAMMaxSize : NDS::MainRAMMaxSize / 4, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY);
	ADD_MEMORY_DOMAIN("Shared WRAM", NDS::SharedWRAM, NDS::SharedWRAMSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	ADD_MEMORY_DOMAIN("ARM7 WRAM", NDS::ARM7WRAM, NDS::ARM7WRAMSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	if (NDSCart::Cart)
	{
		ADD_MEMORY_DOMAIN("SRAM", NDSCart::GetSaveMemory(), NDSCart::GetSaveMemoryLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("ROM", NDSCart::Cart->GetROM(), NDSCart::Cart->GetROMLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	}

	if (GBACart::Cart)
	{
		ADD_MEMORY_DOMAIN("GBA SRAM", GBACart::GetSaveMemory(), GBACart::GetSaveMemoryLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("GBA ROM", GBACart::Cart->GetROM(), GBACart::Cart->GetROMLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	}

	ADD_MEMORY_DOMAIN("Instruction TCM", NDS::ARM9->ITCM, ITCMPhysicalSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	ADD_MEMORY_DOMAIN("Data TCM", NDS::ARM9->DTCM, DTCMPhysicalSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	ADD_MEMORY_DOMAIN("ARM9 BIOS", NDS::ARM9BIOS, sizeof(NDS::ARM9BIOS), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	ADD_MEMORY_DOMAIN("ARM7 BIOS", NDS::ARM7BIOS, sizeof(NDS::ARM7BIOS), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	ADD_MEMORY_DOMAIN("Firmware", SPI_Firmware::GetFirmware()->Buffer(), SPI_Firmware::GetFirmware()->Length(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	if (NDS::ConsoleType == 1)
	{
		ADD_MEMORY_DOMAIN("ARM9i BIOS", DSi::ARM9iBIOS, sizeof(DSi::ARM9iBIOS), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("ARM7i BIOS", DSi::ARM7iBIOS, sizeof(DSi::ARM7iBIOS), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	}

	ADD_MEMORY_DOMAIN("ARM9 System Bus", ARM9Access, 1ull << 32, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK);
	ADD_MEMORY_DOMAIN("ARM7 System Bus", ARM7Access, 1ull << 32, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK);

	// fixme: include more shit
}
