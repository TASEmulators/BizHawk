#include "mednafen/src/types.h"
#include <emulibc.h>
#include <waterboxcore.h>
#include <src/mednafen.h>
#include <stdint.h>
#include "mednafen/src/FileStream.h"
#include "nyma.h"
#include "NymaTypes_generated.h"

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

bool LagFlag;
void (*InputCallback)();
int64_t FrontendTime = 1555555555555;

ECL_EXPORT void PreInit()
{
	SetupMDFNGameInfo();
}

static void Setup()
{
	pixels = (uint32_t*)alloc_invisible(Game->fb_width * Game->fb_height * sizeof(*pixels));
	samples = (int16_t*)alloc_invisible(22050 * 2 * sizeof(*samples));
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
	int64_t FrontendTime;
};

ECL_EXPORT void FrameAdvance(MyFrameInfo& frame)
{
	FrontendTime = frame.FrontendTime;
	LagFlag = true;
	EES->skip = frame.SkipRendering;

	if (frame.Command)
		Game->DoSimpleCommand(frame.Command);
	
	memcpy(InputPortData, frame.InputPortData, sizeof(InputPortData));

	if (Game->TransformInput)
		Game->TransformInput();
	Game->Emulate(EES);

	EES->VideoFormatChanged = false;
	EES->SoundFormatChanged = false;
	frame.Cycles = EES->MasterCycles;
	frame.Lagged = LagFlag;
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
{
	InputCallback = cb;
}

ECL_EXPORT void SetInputDevices(const char** devices)
{
	for (unsigned port = 0; port < MAX_PORTS && devices[port]; port++)
	{
		std::string dev(devices[port]);
		Game->SetInput(port, dev.c_str(), &InputPortData[port * MAX_PORT_DATA]);
	}
}

namespace NymaTypes
{
#define MAYBENULL(y,x) if(x) y = x
ECL_EXPORT void DumpInputs()
{
	NPortsT ports;
	for (auto& x: Game->PortInfo)
	{
		std::unique_ptr<NPortInfoT> a(new NPortInfoT());
		MAYBENULL(a->ShortName, x.ShortName);
		MAYBENULL(a->FullName, x.FullName);
		MAYBENULL(a->DefaultDeviceShortName, x.DefaultDevice);
		for (auto& y: x.DeviceInfo)
		{
			std::unique_ptr<NDeviceInfoT> b(new NDeviceInfoT());
			MAYBENULL(b->ShortName, y.ShortName);
			MAYBENULL(b->FullName, y.FullName);
			MAYBENULL(b->Description, y.Description);
			b->Flags = (DeviceFlags)y.Flags;
			b->ByteLength = y.IDII.InputByteSize;
			for (auto& z: y.IDII)
			{
				std::unique_ptr<NInputInfoT> c(new NInputInfoT());
				MAYBENULL(c->SettingName, z.SettingName);
				MAYBENULL(c->Name, z.Name);
				c->ConfigOrder = z.ConfigOrder;
				c->BitOffset = z.BitOffset;
				c->Type = (InputType)z.Type;
				c->Flags = (AxisFlags)z.Flags;
				c->BitSize = z.BitSize;
				switch(z.Type)
				{
					case IDIT_BUTTON:
					case IDIT_BUTTON_CAN_RAPID:
					{
						auto p(new NButtonInfoT());
						MAYBENULL(p->ExcludeName, z.Button.ExcludeName);
						c->Extra.type = NInputExtra_Button;
						c->Extra.value = p;
						break;
					}
					case IDIT_SWITCH:
					{
						auto p(new NSwitchInfoT());
						p->DefaultPosition = z.Switch.DefPos;
						for (uint32_t i = 0; i < z.Switch.NumPos; i++)
						{
							auto& q = z.Switch.Pos[i];
							std::unique_ptr<NSwitchPositionT> d(new NSwitchPositionT());
							MAYBENULL(d->SettingName, q.SettingName);
							MAYBENULL(d->Name, q.Name);
							MAYBENULL(d->Description, q.Description);
							p->Positions.push_back(std::move(d));
						}
						c->Extra.type = NInputExtra_Switch;
						c->Extra.value = p;
						break;
					}
					case IDIT_STATUS:
					{
						auto p(new NStatusInfoT());
						for (uint32_t i = 0; i < z.Status.NumStates; i++)
						{
							auto& q = z.Status.States[i];
							std::unique_ptr<NStatusStateT> d(new NStatusStateT());
							MAYBENULL(d->ShortName, q.ShortName);
							MAYBENULL(d->Name, q.Name);
							d->Color = q.Color;
							p->States.push_back(std::move(d));
						}
						c->Extra.type = NInputExtra_Status;
						c->Extra.value = p;
						break;
					}
					case IDIT_AXIS:
					case IDIT_AXIS_REL:
					{
						auto p(new NAxisInfoT());
						MAYBENULL(p->SettingsNameNeg, z.Axis.sname_dir[0]);
						MAYBENULL(p->SettingsNamePos, z.Axis.sname_dir[1]);
						MAYBENULL(p->NameNeg, z.Axis.name_dir[0]);
						MAYBENULL(p->NamePos, z.Axis.name_dir[1]);
						c->Extra.type = NInputExtra_Axis;
						c->Extra.value = p;
						break;
					}
					default:
						// no extra data on these
						break;
				}
				b->Inputs.push_back(std::move(c));
			}
			a->Devices.push_back(std::move(b));
		}
		ports.Values.push_back(std::move(a));
	}

	flatbuffers::FlatBufferBuilder fbb;
	fbb.Finish(NPorts::Pack(fbb, &ports));

	// the file is initially empty, so inplace vs not doesn't make a difference, but we haven't implemented ftruncate(2)
	FileStream f("inputs", FileStream::MODE_WRITE_INPLACE, false);
	f.write(fbb.GetBufferPointer(), fbb.GetSize());
}

ECL_EXPORT void DumpSettings()
{
	SettingsT settings;
	for (auto a = Game->Settings; a->name; a++)
	{
		std::unique_ptr<SettingT> s(new SettingT());
		MAYBENULL(s->Name, a->description);
		MAYBENULL(s->Description, a->description_extra);
		MAYBENULL(s->SettingsKey, a->name);
		MAYBENULL(s->DefaultValue, a->default_value);
		MAYBENULL(s->Min, a->minimum);
		MAYBENULL(s->Max, a->maximum);
		s->Flags = (SettingsFlags)a->flags;
		s->Type = (SettingType)a->type;
		if (a->enum_list)
		{
			for (auto b = a->enum_list; b->string; b++)
			{
				std::unique_ptr<EnumValueT> e(new EnumValueT());
				MAYBENULL(e->Name, b->description);
				MAYBENULL(e->Description, b->description_extra);
				MAYBENULL(e->Value, b->string);
				s->SettingEnums.push_back(std::move(e));
			}
		}
		settings.Values.push_back(std::move(s));
	}
	flatbuffers::FlatBufferBuilder fbb;
	fbb.Finish(Settings::Pack(fbb, &settings));

	// the file is initially empty, so inplace vs not doesn't make a difference, but we haven't implemented ftruncate(2)
	FileStream f("settings", FileStream::MODE_WRITE_INPLACE, false);
	f.write(fbb.GetBufferPointer(), fbb.GetSize());
}
}
