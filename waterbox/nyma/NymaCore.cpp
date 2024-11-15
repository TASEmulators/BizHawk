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

enum { MAX_INPUT_DATA = 256 };
static uint8_t InputPortData[MAX_INPUT_DATA];

bool LagFlag;
void (*InputCallback)();
int64_t FrontendTime = 1555555555555;

ECL_EXPORT void PreInit()
{
	SetupMDFNGameInfo();
}

ECL_EXPORT void SetInitialTime(int64_t initialTime)
{
	FrontendTime = initialTime;
}

static void Setup()
{
	pixels = (uint32_t*)alloc_invisible(Game->fb_width * Game->fb_height * sizeof(*pixels));
	samples = (int16_t*)alloc_invisible(22050 * 2 * sizeof(*samples));
	Surf = new MDFN_Surface(
		pixels, Game->fb_width, Game->fb_height, Game->fb_width,
		MDFN_PixelFormat(MDFN_COLORSPACE_RGB, 4, 16, 8, 0, 24)
	);
	EES = new EmulateSpecStruct();
	EES->surface = Surf;
	EES->LineWidths = new int32_t[Game->fb_height];
	memset(EES->LineWidths, 0xff, Game->fb_height * sizeof(int32_t));
	EES->SoundBuf = samples;
	EES->SoundBufMaxSize = 22050;
	EES->SoundRate = 44100;

	if (Game->FormatsChanged)
		Game->FormatsChanged(EES);
}

ECL_EXPORT bool InitRom(const InitData& data)
{
	try
	{
		std::unique_ptr<Stream> gamestream(new FileStream(data.FileNameFull, FileStream::MODE_READ, false));
		GameFile gf({
			&NVFS,
			"",
			data.FileNameFull,
			gamestream.get(),
			data.FileNameExt,
			data.FileNameBase,
			{&NVFS,
			"",
			data.FileNameBase}
		});

		Game->Load(&gf);

		Setup();
	}
	catch(...)
	{
		return false;
	}
	return true;
}

ECL_EXPORT bool InitCd(int numdisks)
{
	try
	{
		StartGameWithCds(numdisks);
		Setup();
	}
	catch(...)
	{
		return false;
	}
	return true;
}

enum BizhawkFlags
{
	// skip video output
	SkipRendering = 1,
	// skip sound output
	SkipSoundening = 2,
	// render at LCM * LCM instead of raw
	RenderConstantSize = 4,
	// open disk tray, if possible
	OpenTray = 8,
	// close disk tray, if possible
	CloseTray = 16
};

struct MyFrameInfo: public FrameInfo
{
	int32_t BizhawkFlags;
	// a single MDFN_MSC_* command to run at the start of this frame; 0 if none
	int32_t Command;
	// raw data for each input port, assumed to be MAX_PORTS * MAX_PORT_DATA long
	uint8_t* InputPortData;
	int64_t FrontendTime;
	int32_t DiskIndex; // used on close tray
};

ECL_EXPORT void FrameAdvance(MyFrameInfo& frame)
{
	FrontendTime = frame.FrontendTime;
	LagFlag = true;
	EES->skip = !!(frame.BizhawkFlags & BizhawkFlags::SkipRendering);

	{
		auto open = !!(frame.BizhawkFlags & BizhawkFlags::OpenTray);
		auto close = !!(frame.BizhawkFlags & BizhawkFlags::CloseTray);
		if (open || close)
			SwitchCds(open, close, frame.DiskIndex);
	}

	if (frame.Command)
		Game->DoSimpleCommand(frame.Command);

	memcpy(InputPortData, frame.InputPortData, sizeof(InputPortData));

	if (Game->TransformInput)
		Game->TransformInput();
	Game->Emulate(EES);

	frame.Cycles = EES->MasterCycles;
	frame.Lagged = LagFlag;
	if (!(frame.BizhawkFlags & BizhawkFlags::SkipSoundening))
	{
		memcpy(frame.SoundBuffer, EES->SoundBuf, EES->SoundBufSize * 4);
		frame.Samples = EES->SoundBufSize;
	}
	if (!(frame.BizhawkFlags & BizhawkFlags::SkipRendering))
	{
		int h = EES->DisplayRect.h;
		int lineStart = EES->DisplayRect.y;
		int lineEnd = lineStart + h;
		auto multiWidth = EES->LineWidths[0] != -1;

		int srcp = Game->fb_width;
		uint32_t* src = pixels + EES->DisplayRect.x + EES->DisplayRect.y * srcp;
		uint32_t* dst = frame.VideoBuffer;

		if (!(frame.BizhawkFlags & BizhawkFlags::RenderConstantSize) || !multiWidth && Game->lcm_width == EES->DisplayRect.w && Game->lcm_height == h)
		{
			// simple non-resizing blitter
			// TODO: What does this do with true multiwidth?  Probably not anything good

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
			int dstp = w;

			for (int line = lineStart; line < lineEnd; line++)
			{
				auto lw = multiWidth ? EES->LineWidths[line] : w;
				if (MDFN_LIKELY(lw > 0))
				{
					memcpy(dst, src, lw * sizeof(uint32_t));
					if (!EES->InterlaceOn && lw < w)
					{
						memset(dst + lw, 0, (w - lw) * sizeof(uint32_t));
					}
					src += srcp;
					dst += dstp;
				}
			}
		}
		else
		{
			// resize to lcm_width * lcm_height

			frame.Width = Game->lcm_width;
			frame.Height = Game->lcm_height;
			int dstp = frame.Width;

			int hf = Game->lcm_height / h;
			for (int line = lineStart; line < lineEnd; line++)
			{
				int w = multiWidth ? EES->LineWidths[line] : EES->DisplayRect.w;
				auto srcNext = src + srcp;
				if (frame.Width == w)
				{
					memcpy(dst, src, w * sizeof(uint32_t));
					dst += dstp;
				}
				else if (MDFN_LIKELY(w > 0))
				{
					// stretch horizontal
					int wf = Game->lcm_width / w;
					auto dstNext = dst + dstp;
					for (int x = 0; x < w; x++)
					{
						for (int n = 0; n < wf; n++)
							*dst++ = *src; 
						src++;
					}
					while (dst < dstNext) // 1024 % 3 == 1, not quite "lcm"
						*dst++ = src[-1];
				}
				src = srcNext;
				for (int y = 1; y < hf; y++)
				{
					// stretch vertical
					memcpy(dst, dst - dstp, dstp * sizeof(uint32_t));
					dst += dstp;
				}
			}
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
	int32_t GameType;
	int32_t FpsFixed;
	int64_t MasterClock;
	int32_t LcmWidth;
	int32_t LcmHeight;
	int32_t PointerScaleX;
	int32_t PointerScaleY;
	int32_t PointerOffsetX;
	int32_t PointerOffsetY;
};
SystemInfo SI;

ECL_EXPORT SystemInfo* GetSystemInfo()
{
	SI.MaxWidth = Game->fb_width;
	SI.MaxHeight = Game->fb_height;
	SI.NominalWidth = Game->nominal_width;
	SI.NominalHeight = Game->nominal_height;
	SI.VideoSystem = Game->VideoSystem;
	SI.GameType = Game->GameType;
	SI.FpsFixed = Game->fps;
	SI.MasterClock = Game->MasterClock;
	SI.LcmWidth = Game->lcm_width;
	SI.LcmHeight = Game->lcm_height;
	SI.PointerScaleX = Game->mouse_scale_x;
	SI.PointerScaleY = Game->mouse_scale_y;
	SI.PointerOffsetX = Game->mouse_offs_x;
	SI.PointerOffsetY = Game->mouse_offs_y;
	return &SI;
}

ECL_EXPORT const char* GetInputDeviceOverride(uint32_t port)
{
	return Game->DesiredInput.size() > port ? Game->DesiredInput[port].device_name : nullptr;
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
	for (unsigned port = 0, dataStart = 0; devices[port]; port++)
	{
		unsigned dataSize = 0;
		for (auto const& device: Game->PortInfo[port].DeviceInfo)
		{
			if (strcmp(device.ShortName, devices[port]) == 0)
				dataSize = device.IDII.InputByteSize;
		}
		Game->SetInput(port, devices[port], &InputPortData[dataStart]);
		dataStart += dataSize;
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
		a->Flags = (PortFlags)x.Flags;
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

	FileStream f("inputs", FileStream::MODE_WRITE);
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

	FileStream f("settings", FileStream::MODE_WRITE);
	f.write(fbb.GetBufferPointer(), fbb.GetSize());
}
}

ECL_EXPORT void NotifySettingChanged(const char* name)
{
	for (auto a = Game->Settings; a->name; a++)
	{
		if (strcmp(a->name, name) == 0)
		{
			if (a->ChangeNotification)
				a->ChangeNotification(name);
			return;
		}
	}
}

static FrameCallback FrameThreadProc = nullptr;

void RegisterFrameThreadProc(FrameCallback threadproc)
{
	FrameThreadProc = threadproc;
}

ECL_EXPORT FrameCallback GetFrameThreadProc()
{
	return FrameThreadProc;
}
