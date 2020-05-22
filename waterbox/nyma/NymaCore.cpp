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
static uint8_t InputPortData[MAX_PORTS * MAX_PORT_DATA];

ECL_EXPORT bool Init(const InitData& data)
{
	try
	{
		SetupMDFNGameInfo();

		pixels = new uint32_t[Game->fb_width * Game->fb_height];
		samples = new int16_t[22050 * 2];
		Surf = new MDFN_Surface(
			pixels, Game->fb_width, Game->fb_height, Game->fb_width,
			MDFN_PixelFormat(MDFN_COLORSPACE_RGB, 0, 8, 16, 24)
		);
		EES = new EmulateSpecStruct();
		EES->surface = Surf;
		EES->VideoFormatChanged = true;
		EES->LineWidths = new int32_t[Game->fb_height];
		EES->SoundBuf = samples;
		EES->SoundBufMaxSize = 22050;
		EES->SoundFormatChanged = true;
		EES->SoundRate = 44100;

		GameFile gf({
			&NVFS,
			"",
			std::unique_ptr<Stream>(new FileStream(data.FileNameFull, FileStream::MODE_READ, false)).get(),
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

struct MyFrameInfo: public FrameInfo
{
	// true to skip video rendering
	int32_t SkipRendering;
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
	frame.Cycles = EES->MasterCycles; // TODO: Was this supposed to be total or delta?
	memcpy(frame.SoundBuffer, EES->SoundBuf, EES->SoundBufSize * 4);
	frame.Samples = EES->SoundBufSize;

	// TODO: Use linewidths
	int w = EES->DisplayRect.w;
	int h = EES->DisplayRect.h;
	frame.Width = w;
	frame.Height = h;
	int srcp = Game->fb_width;
	int dstp = Game->fb_height;
	uint32_t* src = pixels + EES->DisplayRect.x + EES->DisplayRect.y * srcp;
	uint32_t* dst = pixels;
	for (int line = 0; line < h; line++)
	{
		memcpy(dst, src, w * 4);
		src += srcp;
		dst += dstp;
	}
}

ECL_EXPORT void SetLayers(uint64_t layers)
{
	Game->SetLayerEnableMask(layers);
}

ECL_EXPORT void GetMemoryAreas(MemoryArea* m)
{}

ECL_EXPORT void SetInputCallback(void (*cb)())
{}

// same information as PortInfo, but easier to marshal
struct NPortInfo
{
	const char *ShortName;
	const char *FullName;
	const char *DefaultDeviceShortName;
	struct NDeviceInfo
	{
		const char *ShortName;
		const char *FullName;
		const char *Description;
		uint32_t Flags;
		uint32_t ByteLength;
		struct NInput
		{
			const char *SettingName;
			const char *Name;
			int16_t ConfigOrder;
			uint16_t BitOffset;
			InputDeviceInputType Type; // uint8_t
			uint8_t Flags;
			uint8_t BitSize;
			uint8_t _Padding;
			union
			{
				struct
				{
					const char* ExcludeName;
				} Button;
				struct
				{
					// negative, then positive
					const char* SettingName[2];
					const char* Name[2];
				} Axis;
				struct
				{
					struct
					{
						const char* SettingName;
						const char* Name;
						const char* Description;
					}* Positions;
					uint32_t NumPositions;
					uint32_t DefaultPosition;
				} Switch;
				struct
				{
					struct
					{
						const char* ShortName;
						const char* Name;
						int32_t Color; // (msb)0RGB(lsb), -1 for unused.
						int32_t _Padding;
					}* States;
					uint32_t NumStates;
				} Status;
			};
		} Inputs[256];
	} Devices[32];
};

NPortInfo PortInfos[MAX_PORTS] = {};

ECL_EXPORT NPortInfo* GetInputDevices()
{
	for (unsigned port = 0; port < MAX_PORTS && port < Game->PortInfo.size(); port++)
	{
		auto& a = PortInfos[port];
		auto& x = Game->PortInfo[port];
		a.ShortName = x.ShortName;
		a.FullName = x.FullName;
		a.DefaultDeviceShortName = x.DefaultDevice;
		for (unsigned dev = 0; dev < 32 && dev < x.DeviceInfo.size(); dev++)
		{
			auto& b = a.Devices[dev];
			auto& y = x.DeviceInfo[dev];
			b.ShortName = y.ShortName;
			b.FullName = y.FullName;
			b.Description = y.Description;
			b.Flags = y.Flags;
			b.ByteLength = y.IDII.InputByteSize;
			for (unsigned input = 0; input < 256 && input < y.IDII.size(); input++)
			{
				auto& c = b.Inputs[input];
				auto& z = y.IDII[input];
				c.SettingName = z.SettingName;
				c.Name = z.Name;
				c.ConfigOrder = z.ConfigOrder;
				c.BitOffset = z.BitOffset;
				c.Type = z.Type;
				c.Flags = z.Flags;
				c.BitSize = z.BitSize;
				switch (z.Type)
				{
					case IDIT_BUTTON:
					case IDIT_BUTTON_CAN_RAPID:
						c.Button.ExcludeName = z.Button.ExcludeName;
						break;
					case IDIT_SWITCH:
						c.Switch.NumPositions = z.Switch.NumPos;
						c.Switch.DefaultPosition = z.Switch.DefPos;
						c.Switch.Positions = (decltype(c.Switch.Positions))calloc(z.Switch.NumPos, sizeof(*c.Switch.Positions));
						for (unsigned i = 0; i < z.Switch.NumPos; i++)
						{
							c.Switch.Positions[i].SettingName = z.Switch.Pos[i].SettingName;
							c.Switch.Positions[i].Name = z.Switch.Pos[i].Name;
							c.Switch.Positions[i].Description = z.Switch.Pos[i].Description;
						}
						break;
					case IDIT_STATUS:
						c.Status.NumStates = z.Status.NumStates;
						c.Status.States = (decltype(c.Status.States))calloc(z.Status.NumStates, sizeof(*c.Status.States));
						for (unsigned i = 0; i < z.Status.NumStates; i++)
						{
							c.Status.States[i].ShortName = z.Status.States[i].ShortName;
							c.Status.States[i].Name = z.Status.States[i].Name;
							c.Status.States[i].Color = z.Status.States[i].Color;
						}
						break;
					case IDIT_AXIS:
					case IDIT_AXIS_REL:
						c.Axis.SettingName[0] = z.Axis.sname_dir[0];
						c.Axis.SettingName[1] = z.Axis.sname_dir[1];
						c.Axis.Name[0] = z.Axis.name_dir[0];
						c.Axis.Name[1] = z.Axis.name_dir[1];
						break;
					default:
						// other types have no extended information
						break;
				}
			}
		}
	}
	return PortInfos;
}

ECL_EXPORT void SetInputDevices(const char** devices)
{
	for (unsigned port = 0; port < MAX_PORTS && devices[port]; port++)
	{
		Game->SetInput(port, devices[port], &InputPortData[port * MAX_PORT_DATA]);
	}
}
