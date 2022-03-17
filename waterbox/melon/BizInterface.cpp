#include "NDS.h"
#include "GPU.h"
#include "SPU.h"
#include "RTC.h"
#include "ARM.h"
#include "NDSCart.h"
#include "GBACart.h"
#include "DSi.h"
#include "DSi_NAND.h"
#include "Platform.h"
#include "BizConfig.h"
#include "types.h"
#include "frontend/mic_blow.h"

#include "emulibc.h"
#include "waterboxcore.h"

#include <cmath>
#include <algorithm>
#include <time.h>

#include <sstream>

#define EXPORT extern "C" ECL_EXPORT

static GPU::RenderSettings biz_render_settings { false, 1, false };
static bool biz_skip_fw;
static time_t biz_time;

static time_t BizRtcCallback()
{
	return biz_time;
}

typedef enum
{
	NONE = 0x00,
	USE_REAL_BIOS = 0x01,
	SKIP_FIRMWARE = 0x02,
	GBA_CART_PRESENT = 0x04,
	RESERVED_FLAG = 0x08,
	FIRMWARE_OVERRIDE = 0x10,
	IS_DSI = 0x20,
	LOAD_DSIWARE = 0x40,
	THREADED_RENDERING = 0x80,
} LoadFlags;

typedef struct
{
	u8* DsRomData;
	u32 DsRomLen;
	u8* GbaRomData;
	u32 GbaRomLen;
	u8* GbaRamData;
	u32 GbaRamLen;
	char* NandData;
	u32 NandLen;
	u8* TmdData;
	s32 AudioBitrate;
} LoadData;

typedef struct
{
	char* FirmwareUsername; // max 10 length (then terminator)
	s32 FirmwareUsernameLength;
	s32 FirmwareLanguage;
	s32 FirmwareBirthdayMonth;
	s32 FirmwareBirthdayDay;
	s32 FirmwareFavouriteColour;
	char* FirmwareMessage; // max 26 length (then terminator)
	s32 FirmwareMessageLength;
} FirmwareSettings;

extern std::stringstream* NANDFilePtr;

static bool LoadDSiWare(u8* TmdData)
{
	FILE* bios7i = Platform::OpenLocalFile(Config::DSiBIOS7Path, "rb");
	if (!reinterpret_cast<void*>(bios7i))
		return false;

	u8 es_keyY[16];
	fseek(bios7i, 0x8308, SEEK_SET);
	fread(es_keyY, 16, 1, bios7i);
	fclose(bios7i);

	FILE* curNAND = Platform::OpenLocalFile(Config::DSiNANDPath, "r+b");
	if (!curNAND)
		return false;

	if (!DSi_NAND::Init(curNAND, es_keyY))
		return false;

	bool ret = DSi_NAND::ImportTitle("dsiware.rom", TmdData, false);

	DSi_NAND::DeInit();
	return ret;
}

EXPORT bool Init(LoadFlags loadFlags, LoadData* loadData, FirmwareSettings* fwSettings)
{
	Config::ExternalBIOSEnable = !!(loadFlags & USE_REAL_BIOS);
	Config::AudioBitrate = loadData->AudioBitrate;
	Config::FirmwareOverrideSettings = !!(loadFlags & FIRMWARE_OVERRIDE);
	biz_skip_fw = !!(loadFlags & SKIP_FIRMWARE);
	bool isDsi = !!(loadFlags & IS_DSI);

	NDS::SetConsoleType(isDsi);
	// time calls are deterministic under wbx, so this will force the mac address to a constant value instead of relying on whatever is in the firmware
	// fixme: might want to allow the user to specify mac address?
	// edit: upstream has ability to set mac address, most work is in frontend now
	srand(time(NULL));
	Config::RandomizeMAC = true;
	biz_time = 0;
	RTC::RtcCallback = BizRtcCallback;

	if (Config::FirmwareOverrideSettings)
	{
		std::string fwUsername(fwSettings->FirmwareUsername, fwSettings->FirmwareUsernameLength);
		fwUsername += '\0';
		Config::FirmwareUsername = fwUsername;
		Config::FirmwareLanguage = fwSettings->FirmwareLanguage;
		Config::FirmwareBirthdayMonth = fwSettings->FirmwareBirthdayMonth;
		Config::FirmwareBirthdayDay = fwSettings->FirmwareBirthdayDay;
		Config::FirmwareFavouriteColour = fwSettings->FirmwareFavouriteColour;
		std::string fwMessage(fwSettings->FirmwareMessage, fwSettings->FirmwareMessageLength);
		fwMessage += '\0';
		Config::FirmwareMessage = fwMessage;
	}

	NANDFilePtr = isDsi ? new std::stringstream(std::string(loadData->NandData, loadData->NandLen), std::ios_base::in | std::ios_base::out | std::ios_base::binary) : nullptr;

	if (isDsi && (loadFlags & LOAD_DSIWARE))
	{
		if (!LoadDSiWare(loadData->TmdData))
			return false;
	}

	if (!NDS::Init()) return false;
	GPU::InitRenderer(false);
	biz_render_settings.Soft_Threaded = !!(loadFlags & THREADED_RENDERING);
	GPU::SetRenderSettings(false, biz_render_settings);
	NDS::LoadBIOS();
	if (!isDsi || !(loadFlags & LOAD_DSIWARE))
	{
		if (!NDS::LoadCart(loadData->DsRomData, loadData->DsRomLen, nullptr, 0))
			return false;
	}
	if (!isDsi && (loadFlags & GBA_CART_PRESENT))
	{
		if (!NDS::LoadGBACart(loadData->GbaRomData, loadData->GbaRomLen, loadData->GbaRamData, loadData->GbaRamLen))
			return false;
	}
	if (biz_skip_fw) NDS::SetupDirectBoot("");
	NDS::Start();
	Config::FirmwareOverrideSettings = false;
	return true;
}

namespace NDSCart { extern CartCommon* Cart; }
extern bool NdsSaveRamIsDirty;

EXPORT void PutSaveRam(u8* data, u32 len)
{
	NDS::LoadSave(data, len);
	NdsSaveRamIsDirty = false;
}

EXPORT void GetSaveRam(u8* data)
{
	if (NDSCart::Cart)
	{
		NDSCart::Cart->GetSaveData(data);
		NdsSaveRamIsDirty = false;
	}
}

EXPORT s32 GetSaveRamLength()
{
	return NDSCart::Cart ? NDSCart::Cart->GetSaveLen() : 0;
}

EXPORT bool SaveRamIsDirty()
{
	return NdsSaveRamIsDirty;
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
			Write(address++, *buffer++);
	}
	else
	{
		u8 (*Read)(u32) = NDS::ConsoleType == 1 ? DSi::ARM9Read8 : NDS::ARM9Read8;
		while (count--)
			*buffer++ = SafeToPeek<true>(address) ? Read(address) : 0, address++;
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

EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data = NDS::ARM9BIOS;
	m[0].Name = "ARM9 BIOS";
	m[0].Size = sizeof NDS::ARM9BIOS;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[1].Data = NDS::ARM7BIOS;
	m[1].Name = "ARM7 BIOS";
	m[1].Size = sizeof NDS::ARM7BIOS;
	m[1].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[2].Data = NDS::MainRAM;
	m[2].Name = "Main RAM";
	m[2].Size = NDS::MainRAMMaxSize;
	m[2].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

	m[3].Data = NDS::SharedWRAM;
	m[3].Name = "Shared WRAM";
	m[3].Size = NDS::SharedWRAMSize;
	m[3].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[4].Data = NDS::ARM7WRAM;
	m[4].Name = "ARM7 WRAM";
	m[4].Size = NDS::ARM7WRAMSize;
	m[4].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE;

	m[5].Data = (void*)ARM9Access;
	m[5].Name = "ARM9 System Bus";
	m[5].Size = 1ull << 32;
	m[5].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK;

	m[6].Data = (void*)ARM7Access;
	m[6].Name = "ARM7 System Bus";
	m[6].Size = 1ull << 32;
	m[6].Flags = MEMORYAREA_FLAGS_WORDSIZE4 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_FUNCTIONHOOK;

	// fixme: include more shit
}

struct MyFrameInfo : public FrameInfo
{
	s64 Time;
	u32 Keys;
	u8 TouchX;
	u8 TouchY;
	s8 MicVolume;
	s8 GBALightSensor;
};

static s16 biz_mic_input[735];

static bool ValidRange(s8 sensor)
{
	return (sensor >= 0) && (sensor <= 10);
}

static int sampPos = 0;

static void MicFeedNoise(s8 vol)
{
	int sampLen = sizeof mic_blow / sizeof mic_blow[0];

	for (int i = 0; i < 735; i++)
	{
		biz_mic_input[i] = std::round(mic_blow[sampPos++] * (vol / 100.0));
		if (sampPos >= sampLen) sampPos = 0;
	}
}

static bool RunningFrame = false;

EXPORT void FrameAdvance(MyFrameInfo* f)
{
	RunningFrame = true;

	if (f->Keys & 0x8000)
	{
		NDS::LoadBIOS();
		if (biz_skip_fw) NDS::SetupDirectBoot("");
		NDS::Start();
	}

	NDS::SetKeyMask(~f->Keys & 0xFFF);

	if (f->Keys & 0x1000)
	{
		NDS::TouchScreen(f->TouchX, f->TouchY);
	}
	else
	{
		NDS::ReleaseScreen();
	}

	if (f->Keys & 0x2000)
		NDS::SetLidClosed(false);
	else if (f->Keys & 0x4000)
		NDS::SetLidClosed(true);

	MicFeedNoise(f->MicVolume);

	NDS::MicInputFrame(biz_mic_input, 735);

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

	biz_time = f->Time;
	NDS::RunFrame();
	dynamic_cast<GPU3D::SoftRenderer*>(GPU3D::CurrentRenderer.get())->StopRenderThread();
	const u32 SingleScreenSize = 256 * 192;
	memcpy(f->VideoBuffer, GPU::Framebuffer[GPU::FrontBuffer][0], SingleScreenSize * sizeof (u32));
	memcpy(f->VideoBuffer + SingleScreenSize, GPU::Framebuffer[GPU::FrontBuffer][1], SingleScreenSize * sizeof (u32));
	f->Width = 256;
	f->Height = 384;
	f->Samples = SPU::GetOutputSize() / 2;
	SPU::ReadOutput(f->SoundBuffer, f->Samples);
	if (f->Samples < 547) // hack
	{
		memset(f->SoundBuffer + (f->Samples * 2), 0, ((547 * 2) - (f->Samples * 2)) * sizeof (u16));
		f->Samples = 547;
	}
	f->Cycles = NDS::GetSysClockCycles(2);
	f->Lagged = NDS::LagFrameFlag;

	RunningFrame = false;
}

void (*InputCallback)() = nullptr;

EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}

EXPORT void GetRegs(u32* regs)
{
	NDS::GetRegs(regs);
}

EXPORT void SetReg(s32 ncpu, s32 index, s32 val)
{
	NDS::SetReg(ncpu, index, val);
}

EXPORT u32 GetCallbackCycleOffset()
{
	return RunningFrame ? NDS::GetSysClockCycles(2) : 0;
}

void (*ReadCallback)(u32) = nullptr;
void (*WriteCallback)(u32) = nullptr;
void (*ExecuteCallback)(u32) = nullptr;

EXPORT void SetMemoryCallback(u32 which, void (*callback)(u32 addr))
{
	switch (which)
	{
		case 0: ReadCallback = callback; break;
		case 1: WriteCallback = callback; break;
		case 2: ExecuteCallback = callback; break;
	}
}

void (*TraceCallback)(u32, u32*, u32) = nullptr;

EXPORT void SetTraceCallback(void (*callback)(u32 cpu, u32* regs, u32 opcode))
{
	TraceCallback = callback;
}

namespace Platform
{
	extern uintptr_t FrameThreadProc;
	extern void (*ThreadStartCallback)();
}

EXPORT uintptr_t GetFrameThreadProc()
{
	return Platform::FrameThreadProc;
}

EXPORT void SetThreadStartCallback(void (*callback)())
{
	Platform::ThreadStartCallback = callback;
}

EXPORT u32 GetNANDSize()
{
	if (NANDFilePtr)
	{
		NANDFilePtr->seekg(0, std::ios::end);
		return NANDFilePtr->tellg();
	}

	return 0;
}

EXPORT void GetNANDData(char* buf)
{
	if (NANDFilePtr)
	{
		u32 sz = GetNANDSize();		
		NANDFilePtr->seekg(0);
		NANDFilePtr->read(buf, sz);
	}
}

namespace GPU { void ResetVRAMCache(); }

EXPORT void ResetCaches()
{
	GPU::ResetVRAMCache();
}
