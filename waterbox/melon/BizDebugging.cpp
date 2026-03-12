#include "NDS.h"
#include "NDSCart.h"
#include "GBACart.h"
#include "DSi.h"
#include "ARM.h"
#include "SPI.h"

#include "BizTypes.h"
#include "dthumb.h"

#include <waterboxcore.h>

melonDS::NDS* CurrentNDS;

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

void TraceTrampoline(TraceMask_t type, u32* regs, u32 opcode, u32 cycleOffset)
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

	TraceCallback(type, opcode, regs, disasm, cycleOffset);
}

ECL_EXPORT void GetRegs(melonDS::NDS* nds, u32* regs)
{
	for (int i = 0; i < 16; i++)
	{
		*regs++ = nds->ARM9.R[i];
	}

	for (int i = 0; i < 16; i++)
	{
		*regs++ = nds->ARM7.R[i];
	}
}

ECL_EXPORT void SetReg(melonDS::NDS* nds, s32 ncpu, s32 index, s32 val)
{
	if (ncpu)
	{
		nds->ARM7.R[index] = val;
	}
	else
	{
		nds->ARM9.R[index] = val;
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

template <bool arm9>
static bool SafeToPeek(u32 addr)
{
	if (arm9)
	{
		// dsp io reads are not safe
		if ((addr & 0xFFFFFF00) == 0x04004200)
		{
			return false;
		}

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
		while (count--)
		{
			if (address < CurrentNDS->ARM9.ITCMSize)
			{
				CurrentNDS->ARM9.ITCM[address & (melonDS::ITCMPhysicalSize - 1)] = *buffer;
			}
			else if ((address & CurrentNDS->ARM9.DTCMMask) == CurrentNDS->ARM9.DTCMBase)
			{
				CurrentNDS->ARM9.DTCM[address & (melonDS::DTCMPhysicalSize - 1)] = *buffer;
			}
			else
			{
				CurrentNDS->ARM9Write8(address, *buffer);
			}

			address++;
			buffer++;
		}
	}
	else
	{
		while (count--)
		{
			if (address < CurrentNDS->ARM9.ITCMSize)
			{
				*buffer = CurrentNDS->ARM9.ITCM[address & (melonDS::ITCMPhysicalSize - 1)];
			}
			else if ((address & CurrentNDS->ARM9.DTCMMask) == CurrentNDS->ARM9.DTCMBase)
			{
				*buffer = CurrentNDS->ARM9.DTCM[address & (melonDS::DTCMPhysicalSize - 1)];
			}
			else
			{
				*buffer = SafeToPeek<true>(address) ? CurrentNDS->ARM9Read8(address) : 0;
			}

			address++;
			buffer++;
		}
	}
}

static void ARM7Access(u8* buffer, s64 address, s64 count, bool write)
{
	if (write)
	{
		while (count--)
		{
			CurrentNDS->ARM7Write8(address, *buffer);

			address++;
			buffer++;
		}
	}
	else
	{
		while (count--)
		{
			*buffer = SafeToPeek<false>(address) ? CurrentNDS->ARM7Read8(address) : 0;

			address++;
			buffer++;
		}
	}
}

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	int i = 0;
	#define ADD_MEMORY_DOMAIN(name, data, size, flags) do \
	{ \
		m[i].Data = reinterpret_cast<void*>(data); \
		m[i].Name = name; \
		m[i].Size = size; \
		m[i].Flags = flags; \
		i++; \
	} while (0)

	const auto mainRamSize = CurrentNDS->ConsoleType == 1 ? melonDS::MainRAMMaxSize : melonDS::MainRAMMaxSize / 4;
	ADD_MEMORY_DOMAIN("Main RAM", CurrentNDS->MainRAM, mainRamSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY);
	ADD_MEMORY_DOMAIN("Shared WRAM", CurrentNDS->SharedWRAM, melonDS::SharedWRAMSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	ADD_MEMORY_DOMAIN("ARM7 WRAM", CurrentNDS->ARM7WRAM, melonDS::ARM7WRAMSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	if (auto* ndsCart = CurrentNDS->GetNDSCart())
	{
		ADD_MEMORY_DOMAIN("SRAM", CurrentNDS->GetNDSSave(), CurrentNDS->GetNDSSaveLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("ROM", const_cast<u8*>(ndsCart->GetROM()), ndsCart->GetROMLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	}

	if (auto* gbaCart = CurrentNDS->GetGBACart())
	{
		ADD_MEMORY_DOMAIN("GBA SRAM", CurrentNDS->GetGBASave(), CurrentNDS->GetGBASaveLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("GBA ROM", const_cast<u8*>(gbaCart->GetROM()), gbaCart->GetROMLength(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	}

	ADD_MEMORY_DOMAIN("Instruction TCM", CurrentNDS->ARM9.ITCM, melonDS::ITCMPhysicalSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	ADD_MEMORY_DOMAIN("Data TCM", CurrentNDS->ARM9.DTCM, melonDS::DTCMPhysicalSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	ADD_MEMORY_DOMAIN("ARM9 BIOS", const_cast<u8*>(CurrentNDS->GetARM9BIOS().data()), melonDS::ARM9BIOSSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	ADD_MEMORY_DOMAIN("ARM7 BIOS", const_cast<u8*>(CurrentNDS->GetARM7BIOS().data()), melonDS::ARM7BIOSSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	ADD_MEMORY_DOMAIN("Firmware", CurrentNDS->GetFirmware().Buffer(), CurrentNDS->GetFirmware().Length(), MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

	if (CurrentNDS->ConsoleType == 1)
	{
		auto* dsi = static_cast<melonDS::DSi*>(CurrentNDS);
		ADD_MEMORY_DOMAIN("ARM9i BIOS", dsi->ARM9iBIOS.data(), melonDS::DSiBIOSSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("ARM7i BIOS", dsi->ARM7iBIOS.data(), melonDS::DSiBIOSSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);

		ADD_MEMORY_DOMAIN("NWRAM A", dsi->NWRAM_A, melonDS::NWRAMSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("NWRAM B", dsi->NWRAM_B, melonDS::NWRAMSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
		ADD_MEMORY_DOMAIN("NWRAM C", dsi->NWRAM_C, melonDS::NWRAMSize, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE);
	}

	ADD_MEMORY_DOMAIN("ARM9 System Bus", ARM9Access, 1ull << 32, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK);
	ADD_MEMORY_DOMAIN("ARM7 System Bus", ARM7Access, 1ull << 32, MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK);

	// fixme: include more shit
}
