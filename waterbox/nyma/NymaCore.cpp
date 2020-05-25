#include "mednafen/src/types.h"
#include <emulibc.h>
#include <waterboxcore.h>
#include <mednafen/mednafen.h>
#include <stdint.h>
#include "mednafen/src/FileStream.h"
#include "nyma.h"

using namespace Mednafen;

#define Game MDFNGameInfo

static EmulateSpecStruct* EES;
static MDFN_Surface* Surf;
static uint32_t* pixels;
static int16_t* samples;

struct InitData
{
	const char* FileNameBase;
	const char* FileNameExt;
	const char* FileNameFull;
};

enum { MAX_PORTS = 16 };
enum { MAX_PORT_DATA = 16 };
static uint8_t InputPortData[(MAX_PORTS + 1) * MAX_PORT_DATA];

ECL_EXPORT void PreInit()
{
	SetupMDFNGameInfo();
}

static void Setup()
{
	pixels = new uint32_t[Game->fb_width * Game->fb_height];
	samples = new int16_t[22050 * 2];
	Surf = new MDFN_Surface(
		pixels, Game->fb_width, Game->fb_height, Game->fb_width,
		MDFN_PixelFormat(MDFN_COLORSPACE_RGB, 16, 8, 0, 24)
	);
	EES = new EmulateSpecStruct();
	EES->surface = Surf;
	EES->VideoFormatChanged = true;
	EES->LineWidths = new int32_t[Game->fb_height];
	memset(EES->LineWidths, 0xff, Game->fb_height * sizeof(int32_t));
	EES->SoundBuf = samples;
	EES->SoundBufMaxSize = 22050;
	EES->SoundFormatChanged = true;
	EES->SoundRate = 44100;
}

ECL_EXPORT bool InitRom(const InitData& data)
{
	try
	{
		Setup();

		std::unique_ptr<Stream> gamestream(new FileStream(data.FileNameFull, FileStream::MODE_READ, false));
		GameFile gf({
			&NVFS,
			"",
			gamestream.get(),
			data.FileNameExt,
			data.FileNameBase,
			&NVFS,
			"",
			data.FileNameBase
		});

		Game->Load(&gf);
	}
	catch(...)
	{
		return false;
	}
	return true;
}

void StartGameWithCds(int numdisks);

ECL_EXPORT bool InitCd(int numdisks)
{
	try
	{
		Setup();
		StartGameWithCds(numdisks);
	}
	catch(...)
	{
		return false;
	}
	return true;
}

struct MyFrameInfo: public FrameInfo
{
	// true to skip video rendering
	int16_t SkipRendering;
	int16_t SkipSoundening;
	// a single MDFN_MSC_* command to run at the start of this frame; 0 if none
	int32_t Command;
	// raw data for each input port, assumed to be MAX_PORTS * MAX_PORT_DATA long
	uint8_t* InputPortData;
};

ECL_EXPORT void FrameAdvance(MyFrameInfo& frame)
{
	EES->skip = frame.SkipRendering;

	if (frame.Command)
		Game->DoSimpleCommand(frame.Command);
	
	memcpy(InputPortData, frame.InputPortData, sizeof(InputPortData));

	Game->TransformInput();
	Game->Emulate(EES);

	EES->VideoFormatChanged = false;
	EES->SoundFormatChanged = false;
	frame.Cycles = EES->MasterCycles;
	if (!frame.SkipSoundening)
	{
		memcpy(frame.SoundBuffer, EES->SoundBuf, EES->SoundBufSize * 4);
		frame.Samples = EES->SoundBufSize;
	}
	if (!frame.SkipRendering)
	{
		int h = EES->DisplayRect.h;
		int lineStart = EES->DisplayRect.y;
		int lineEnd = lineStart + h;

		auto multiWidth = EES->LineWidths[0] != -1;
		int w;
		if (multiWidth)
		{
			w = 0;
			for (int line = lineStart; line < lineEnd; line++)
				w = std::max(w, EES->LineWidths[line]);
		}
		else
		{
			w = EES->DisplayRect.w;
		}

		frame.Width = w;
		frame.Height = h;
		int srcp = Game->fb_width;
		int dstp = w;
		uint32_t* src = pixels + EES->DisplayRect.x + EES->DisplayRect.y * srcp;
		uint32_t* dst = frame.VideoBuffer;
		for (int line = lineStart; line < lineEnd; line++)
		{
			memcpy(dst, src, (multiWidth ? EES->LineWidths[line] : w) * sizeof(uint32_t));
			src += srcp;
			dst += dstp;
		}
	}
}

struct SystemInfo
{
	int32_t MaxWidth;
	int32_t MaxHeight;
	int32_t NominalWidth;
	int32_t NominalHeight;
	int32_t VideoSystem;
	int32_t FpsFixed;
};
SystemInfo SI;

ECL_EXPORT SystemInfo* GetSystemInfo()
{
	SI.MaxWidth = Game->fb_width;
	SI.MaxHeight = Game->fb_height;
	SI.NominalWidth = Game->nominal_width;
	SI.NominalHeight = Game->nominal_height;
	SI.VideoSystem = Game->VideoSystem;
	SI.FpsFixed = Game->fps;
	return &SI;
} 

ECL_EXPORT const char* GetLayerData()
{
	return Game->LayerNames;
}

ECL_EXPORT void SetLayers(uint64_t layers)
{
	Game->SetLayerEnableMask(layers);
}

ECL_EXPORT void SetInputCallback(void (*cb)())
{}

// same information as PortInfo, but easier to marshal
struct NPortInfo
{
	const char* ShortName;
	const char* FullName;
	const char* DefaultDeviceShortName;
	uint32_t NumDevices;
};
struct NDeviceInfo
{
	const char* ShortName;
	const char* FullName;
	const char* Description;
	uint32_t Flags;
	uint32_t ByteLength;
	uint32_t NumInputs;
};
struct NInputInfo
{
	const char* SettingName;
	const char* Name;
	int16_t ConfigOrder;
	uint16_t BitOffset;
	InputDeviceInputType Type; // uint8_t
	uint8_t Flags;
	uint8_t BitSize;
};
struct NButtonInfo
{
	const char* ExcludeName;
};
struct NAxisInfo
{
	// negative, then positive
	const char* SettingName[2];
	const char* Name[2];	
};
struct NSwitchInfo
{
	uint32_t NumPositions;
	uint32_t DefaultPosition;
	struct Position
	{
		const char* SettingName;
		const char* Name;
		const char* Description;
	};
};
struct NStatusInfo
{
	uint32_t NumStates;
	struct State
	{
		const char* ShortName;
		const char* Name;
		int32_t Color; // (msb)0RGB(lsb), -1 for unused.
		int32_t _Padding;
	};
};

ECL_EXPORT uint32_t GetNumPorts()
{
	return Game->PortInfo.size();
}
ECL_EXPORT NPortInfo& GetPort(uint32_t port)
{
	auto& a = *(NPortInfo*)InputPortData;
	auto& x = Game->PortInfo[port];
	a.ShortName = x.ShortName;
	a.FullName = x.FullName;
	a.DefaultDeviceShortName = x.DefaultDevice;
	a.NumDevices = x.DeviceInfo.size();
	return a;
}
ECL_EXPORT NDeviceInfo& GetDevice(uint32_t port, uint32_t dev)
{
	auto& b = *(NDeviceInfo*)InputPortData;
	auto& y = Game->PortInfo[port].DeviceInfo[dev];
	b.ShortName = y.ShortName;
	b.FullName = y.FullName;
	b.Description = y.Description;
	b.Flags = y.Flags;
	b.ByteLength = y.IDII.InputByteSize;
	b.NumInputs = y.IDII.size();
	return b;
}
ECL_EXPORT NInputInfo& GetInput(uint32_t port, uint32_t dev, uint32_t input)
{
	auto& c = *(NInputInfo*)InputPortData;
	auto& z = Game->PortInfo[port].DeviceInfo[dev].IDII[input];
	c.SettingName = z.SettingName;
	c.Name = z.Name;
	c.ConfigOrder = z.ConfigOrder;
	c.BitOffset = z.BitOffset;
	c.Type = z.Type;
	c.Flags = z.Flags;
	c.BitSize = z.BitSize;
	return c;
}
ECL_EXPORT NButtonInfo& GetButton(uint32_t port, uint32_t dev, uint32_t input)
{
	auto& c = *(NButtonInfo*)InputPortData;
	auto& z = Game->PortInfo[port].DeviceInfo[dev].IDII[input].Button;
	c.ExcludeName = z.ExcludeName;
	return c;
}
ECL_EXPORT NSwitchInfo& GetSwitch(uint32_t port, uint32_t dev, uint32_t input)
{
	auto& c = *(NSwitchInfo*)InputPortData;
	auto& z = Game->PortInfo[port].DeviceInfo[dev].IDII[input].Switch;
	c.NumPositions = z.NumPos;
	c.DefaultPosition = z.DefPos;
	return c;
}
ECL_EXPORT NSwitchInfo::Position& GetSwitchPosition(uint32_t port, uint32_t dev, uint32_t input, int i)
{
	auto& c = *(NSwitchInfo::Position*)InputPortData;
	auto& z = Game->PortInfo[port].DeviceInfo[dev].IDII[input].Switch;
	c.SettingName = z.Pos[i].SettingName;
	c.Name = z.Pos[i].Name;
	c.Description = z.Pos[i].Description;
	return c;
}
ECL_EXPORT NStatusInfo& GetStatus(uint32_t port, uint32_t dev, uint32_t input)
{
	auto& c = *(NStatusInfo*)InputPortData;
	auto& z = Game->PortInfo[port].DeviceInfo[dev].IDII[input].Status;
	c.NumStates = z.NumStates;
	return c;
}
ECL_EXPORT NStatusInfo::State& GetStatusState(uint32_t port, uint32_t dev, uint32_t input, int i)
{
	auto& c = *(NStatusInfo::State*)InputPortData;
	auto& z = Game->PortInfo[port].DeviceInfo[dev].IDII[input].Status;
	c.ShortName = z.States[i].ShortName;
	c.Name = z.States[i].Name;
	c.Color = z.States[i].Color;
	return c;
}
ECL_EXPORT NAxisInfo& GetAxis(uint32_t port, uint32_t dev, uint32_t input)
{
	auto& c = *(NAxisInfo*)InputPortData;
	auto& z = Game->PortInfo[port].DeviceInfo[dev].IDII[input].Axis;
	c.SettingName[0] = z.sname_dir[0];
	c.SettingName[1] = z.sname_dir[1];
	c.Name[0] = z.name_dir[0];
	c.Name[1] = z.name_dir[1];
	return c;
}

ECL_EXPORT void SetInputDevices(const char** devices)
{
	for (unsigned port = 0; port < MAX_PORTS && devices[port]; port++)
	{
		std::string dev(devices[port]);
		Game->SetInput(port, dev.c_str(), &InputPortData[port * MAX_PORT_DATA]);
	}
}

struct NSetting
{
	const char* Name;
	const char* Description;
	const char* SettingsKey;
	const char* DefaultValue;
	const char* Min;
	const char* Max;
	uint32_t Flags;
	uint32_t Type;
};
struct NEnumValue
{
	const char* Name;
	const char* Description;
	const char* Value;
};

ECL_EXPORT void IterateSettings(int index, NSetting& s)
{
	auto& a = Game->Settings[index];
	if (a.name)
	{
		s.Name = a.description;
		s.Description = a.description_extra;
		s.SettingsKey = a.name;
		s.DefaultValue = a.default_value;
		s.Min = a.minimum;
		s.Max = a.maximum;
		s.Flags = a.flags;
		s.Type = a.type;
	}
}

ECL_EXPORT void IterateSettingEnums(int index, int enumIndex, NEnumValue& e)
{
	auto& a = Game->Settings[index].enum_list[enumIndex];
	if (a.string)
	{
		e.Name = a.description;
		e.Description = a.description_extra;
		e.Value = a.string;
	}
}
