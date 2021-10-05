#include "melonds/src/NDS.h"
#include "melonds/src/DSi.h"
#include "melonds/src/GPU.h"
#include "melonds/src/SPU.h"
#include "melonds/src/GBACart.h"
#include "melonds/src/Platform.h"
#include "melonds/src/Config.h"
#include "melonds/src/types.h"

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

#define EXPORT extern "C" ECL_EXPORT

static GPU::RenderSettings biz_render_settings { false, 1, false };
static bool biz_direct_boot;
static bool biz_gba_cart_present;

typedef enum
{
	NONE = 0x00,
	USE_DSI = 0x01,
	USE_REAL_DS_BIOS = 0x02,
	SKIP_FIRMWARE = 0x04,
	SD_CARD_ENABLE = 0x08,
	GBA_CART_PRESENT = 0x10,
	ACCURATE_AUDIO_BITRATE = 0x20,
	FIRMWARE_OVERRIDE = 0x40,
} LoadFlags;

static const char* bios9_path = "bios9.rom";
static const char* bios7_path = "bios7.rom";
static const char* firmware_path = "firmware.bin";
static const char* sd_path = "sd.bin";
static const char* bios9i_path = "bios9i.rom";
static const char* bios7i_path = "bios7i.rom";
static const char* nand_path = "nand.bin";
static const char* no_path = "";

static const char* rom_path = "game.rom"
static const char* sram_path = "save.ram"
static const char* gba_rom_path = "gba.rom"
static const char* gba_sram_path = "gba.ram"

typedef struct
{
	char FirmwareUsername[64];
	int FirmwareLanguage;
	int FirmwareBirthdayMonth;
	int FirmwareBirthdayDay;
	int FirmwareFavouriteColour;
	char FirmwareMessage[1024];
} FirmwareSettings;

EXPORT bool Init(LoadFlags flags, FirmwareSettings fwSettings)
{
	Config::ExternalBIOSEnable = !!(flags & USE_REAL_DS_BIOS);
	strncpy(Config::BIOS9Path, Config::ExternalBIOSEnable ? bios9_path : no_path, 1023);
	Config::BIOS9Path[1023] = '\0';
	strncpy(Config::BIOS7Path, Config::ExternalBIOSEnable ? bios7_path : no_path, 1023);
	Config::BIOS7Path[1023] = '\0';

	bool dsi = !!(flags & USE_DSI);
	NDS::SetConsoleType(dsi);
	if (dsi)
	{
		strncpy(Config::FirmwarePath, no_path, 1023);
		Config::FirmwarePath[1023] = '\0';
		Config::DLDIEnable = false; // i think this is ds only?
		strncpy(Config::DLDISDPath, no_path, 1023);
		Config::DLDISDPath[1023] = '\0';
		strncpy(Config::DSiBIOS9Path, bios9i_path, 1023);
		Config::DSiBIOS9Path[1023] = '\0';
		strncpy(Config::DSiBIOS7Path, bios7i_path, 1023);
		Config::DSiBIOS7Path[1023] = '\0';
		strncpy(Config::DSiFirmwarePath, firmware_path, 1023);
		Config::DSiFirmwarePath[1023] = '\0';
		strncpy(Config::DSiNANDPath, nand_path, 1023);
		Config::DSiNANDPath[1023] = '\0';
		Config::DSiSDEnable = !!(flags & SD_CARD_ENABLE);
		strncpy(Config::DSiSDPath, Config::DSiSDEnable ? sd_path : no_path, 1023);
		Config::DSiSDPath[1023] = '\0';
	}
	else
	{
		strncpy(Config::FirmwarePath, firmware_path, 1023);
		Config::FirmwarePath[1023] = '\0';
		Config::DLDIEnable = !!(flags & SD_CARD_ENABLE);
		strncpy(Config::DLDISDPath, Config::DLDIEnable ? sd_path : no_path, 1023);
		Config::DLDISDPath[1023] = '\0';
		strncpy(Config::DSiBIOS9Path, no_path, 1023);
		Config::DSiBIOS9Path[1023] = '\0';
		strncpy(Config::DSiBIOS7Path, no_path, 1023);
		Config::DSiBIOS7Path[1023] = '\0';
		strncpy(Config::DSiFirmwarePath, no_path, 1023);
		Config::DSiFirmwarePath[1023] = '\0';
		strncpy(Config::DSiNANDPath, no_path, 1023);
		Config::DSiNANDPath[1023] = '\0';
		Config::DSiSDEnable = false;
		strncpy(Config::DSiSDPath, no_path, 1023);
		Config::DSiSDPath[1023] = '\0';
	}
	DSi::SDMMCFilePath = Config::DSiNANDPath;
	DSi::SDIOFilePath = Config::DSiSDPath;
	// rand calls are deterministic under wbx, so this will force the mac address to a constant value instead of relying on whatever is in the firmware
	// fixme: might want to allow the user to specify mac address?
	Config::RandomizeMAC = !!(flags & FIRMWARE_OVERRIDE);
	Config::AudioBitrate = !!(flags & ACCURATE_AUDIO_BITRATE) ? 10 : 16;
	// slight misnomer, this just means frontend time will always be used on boot, not a set one.
	// wbx core handles setting inital time already, so we don't need to deal with the configs for that
	Config::UseRealTime = true;

	if (!NDS::Init()) return false;
	GPU::InitRenderer(false);
	GPU::SetRenderSettings(false, biz_render_settings);
	biz_skip_fw = !!(flags & SKIP_FIRMWARE);
	if (!NDS::LoadROM(rom_path, no_path, biz_skip_fw)) return false;
	if (flags & GBA_CART_PRESENT) { if (!NDS::LoadGBAROM(gba_rom_path, gba_sram_path)) return false; }
	return true;
}

EXPORT void SetFileOpenCallback(void (*callback)(const char* path))
{
	Platform::SetFileOpenCallback(callback);
}

EXPORT void SetFileCloseCallback(void (*callback)(const char* path))
{
	Platform::SetFileCloseCallback(callback);
}

EXPORT bool PutSaveRam(u8* data, u32 len)
{
	return NDS::ImportSRAM(data, len) != 0;
}

EXPORT void GetSaveRam()
{
	NDS::RelocateSave(sram_path, true);
}

EXPORT bool SaveRamIsDirty()
{
	return NDS::SRAMIsDirty();
}

EXPORT void Reset()
{
	NDS::LoadROM(rom_path, sram_path, biz_skip_fw);
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

static void ARM9Access(u8* buffer, s64 address, s64 count, bool write)
{
	if (write)
		while (count--) ARM9Write8(address++, *buffer++);
	else
		while (count--) *buffer++ = ARM7Peek8(address++);
}

static void ARM7Access(u8* buffer, s64 address, s64 count, bool write)
{
	if (write)
		while (count--) ARM7Write8(address++, *buffer++);
	else
		while (count--) *buffer++ = ARM7Peek8(address++);
}

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = ARM9BIOS;
	m[0].Name = "ARM9 BIOS";
	m[0].Size = sizeof ARM9BIOS;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[1].Data = ARM7BIOS;
	m[1].Name = "ARM7 BIOS";
	m[1].Size = sizeof ARM7BIOS;
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[2].Data = MainRAM;
	m[2].Name = "Main RAM";
	m[2].Size = MainRAMMaxSize;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

	m[3].Data = SharedWRAM;
	m[3].Name = "Shared WRAM";
	m[3].Size = SharedWRAMSize;
	m[3].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[4].Data = ARM7WRAM;
	m[4].Name = "ARM7 WRAM";
	m[4].Size = ARM7WRAMSize;
	m[4].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[5].Data = ARM9Access;
	m[5].Name = "ARM9 System Bus";
	m[5].Size = 1 << 32;
	m[5].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK;

	m[6].Data = ARM7Access;
	m[6].Name = "ARM7 System Bus";
	m[6].Size = 1 << 32;
	m[6].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK;

	// fixme: include more shit
}

struct MyFrameInfo : public FrameInfo
{
	s64 Time;
	u32 Keys;
	s8 TouchX;
	s8 TouchY;
	s8 GBALightSensor;
};

typedef enum
{
	A = 0x0001,
	B = 0x0002,
	SELECT = 0x0004,
	START = 0x0008,
	RIGHT = 0x0010,
	LEFT = 0x0020,
	UP = 0x0040,
	DOWN = 0x0080,
	R = 0x0100,
	L = 0x0200,
	X = 0x0400,
	Y = 0x0800,
	TOUCH = 0x1000,
	LIDOPEN = 0x2000,
	LIDCLOSE = 0x4000,
	POWER = 0x8000, // handled by frontend
} Buttons;

static bool ValidRange(s8 sensor)
{
	return (sensor >= 0) && (sensor <= 10);
}

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	if (f->Keys & Buttons.TOUCH)
		NDS::TouchScreen(f->TouchX, f->TouchY);
	else
		NDS::ReleaseScreen();

	NDS::SetKeyMask(~f->Keys & 0xFFF);

	if (f->Keys & Buttons.LIDOPEN)
		NDS::SetLidClosed(false);
	else if (f->Keys & Buttons.LIDCLOSE)
		NDS::SetLidClosed(true);

	int sensor = GBACart::SetInput(0, 1);
	if (sensor != -1 && ValidRange(f->GBALightSensor))
	{
		if (sensor > f->GBALightSensor)
		{
			while (GBACart::SetInput(0, 1) != f->GBALightSensor) {}
		}
		else if (sensor < f->GBALightSensor)
		{
			while (GBACart::SetInput(1, 1) != f->GBALightSensor) {}
		}
	}

	Platform::SetFrontendTime(f->Time);
	NDS::RunFrame();
	for (int i = 0; i < (256 * 192); i++)
	{
		f->VideoBuffer[i] = GPU::Framebuffer[GPU::FrontBuffer][1];
		f->VideoBuffer[(256 * 192) + i] = GPU::Framebuffer[GPU::FrontBuffer][1]; 
	}
	f->Width = 256;
	f->Height = 384;
	f->Samples = SPU::GetOutputSize();
	SPU::ReadOutput(f->SoundBuffer, f->Samples / 2);
	f->Cycles = NDS::GetSysClockCycles(2);
	f->Lagged = NDS::LagFrameFlag;
}

void (*InputCallback)();

EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}