#include "NDS.h"
#include "GPU.h"
#include "SPU.h"
#include "RTC.h"
#include "GBACart.h"
#include "frontend/mic_blow.h"

#include "BizPlatform/BizConfig.h"
#include "BizPlatform/BizFile.h"
#include "BizPlatform/BizLog.h"
#include "BizPlatform/BizOGL.h"

#include "BizFileManager.h"
#include "BizGLPresenter.h"

#include <emulibc.h>
#include <waterboxcore.h>

static bool SkipFW;
static bool GLPresentation;

struct NDSTime
{
	int Year; // 0-99
	int Month; // 1-12
	int Day; // 1-(28/29/30/31 depending on month/year)
	int Hour; // 0-23
	int Minute; // 0-59
	int Second; // 0-59
};

struct InitConfig
{
	bool SkipFW;
	bool HasGBACart;
	bool DSi;
	bool ClearNAND;
	bool LoadDSiWare;
	bool IsWinApi;
	int ThreeDeeRenderer;
	GPU::RenderSettings RenderSettings;
	NDSTime StartTime;
	FileManager::FirmwareSettings FirmwareSettings;
};

ECL_EXPORT const char* Init(InitConfig* initConfig,
	Platform::ConfigCallbackInterface* configCallbackInterface,
	Platform::FileCallbackInterface* fileCallbackInterface,
	Platform::LogCallback_t logCallback,
	BizOGL::LoadGLProc loadGLProc)
{
	Platform::SetConfigCallbacks(*configCallbackInterface);
	Platform::SetFileCallbacks(*fileCallbackInterface);
	Platform::SetLogCallback(logCallback);

	SkipFW = initConfig->SkipFW;
	NDS::SetConsoleType(initConfig->DSi);

	if (const char* error = FileManager::InitNDSBIOS())
	{
		return error;
	}

	if (const char* error = FileManager::InitFirmware(initConfig->FirmwareSettings))
	{
		return error;
	}

	if (initConfig->DSi)
	{
		if (const char* error = FileManager::InitDSiBIOS())
		{
			return error;
		}

		if (const char* error = FileManager::InitNAND(initConfig->FirmwareSettings, initConfig->ClearNAND, initConfig->LoadDSiWare))
		{
			return error;
		}
	}

	if (!NDS::Init())
	{
		return "Failed to init core!";
	}

	RTC::SetDateTime(initConfig->StartTime.Year, initConfig->StartTime.Month, initConfig->StartTime.Day,
		initConfig->StartTime.Hour, initConfig->StartTime.Minute, initConfig->StartTime.Second);

	if (loadGLProc)
	{
		switch (initConfig->ThreeDeeRenderer)
		{
			case 0:
				BizOGL::LoadGL(loadGLProc, BizOGL::LoadGLVersion::V3_1, initConfig->IsWinApi);
				break;
			case 1:
				BizOGL::LoadGL(loadGLProc, BizOGL::LoadGLVersion::V3_2, initConfig->IsWinApi);
				break;
#if false // OpenGL Compute Renderer isn't released yet
			case 2:
				BizOGL::LoadGL(loadGLProc, BizOGL::LoadGLVersion::V4_3, initConfig->IsWinApi);
				break;
#endif
			default:
				return "Unknown 3DRenderer!";
		}

		GLPresenter::Init(initConfig->ThreeDeeRenderer ? initConfig->RenderSettings.GL_ScaleFactor : 1);
		GLPresentation = true;
	}
	else
	{
		GLPresentation = false;
	}

	GPU::InitRenderer(initConfig->ThreeDeeRenderer);
	GPU::SetRenderSettings(initConfig->ThreeDeeRenderer, initConfig->RenderSettings);

	NDS::Reset();

	if (!initConfig->LoadDSiWare)
	{
		if (const char* error = FileManager::InitCarts(initConfig->HasGBACart))
		{
			return error;
		}
	}

	if (SkipFW || NDS::NeedsDirectBoot())
	{
		FileManager::SetupDirectBoot();
	}

	NDS::Start();

	return nullptr;
}

struct MyFrameInfo : public FrameInfo
{
	u32 Keys;
	u8 TouchX;
	u8 TouchY;
	s8 MicVolume;
	s8 GBALightSensor;
	bool ConsiderAltLag;
};

static s16 biz_mic_input[735];

static bool ValidRange(s8 sensor)
{
	return sensor >= 0 && sensor <= 10;
}

static int sampPos = 0;

static void MicFeedNoise(s8 vol)
{
	int sampLen = sizeof(mic_blow) / sizeof(*mic_blow);

	for (int i = 0; i < 735; i++)
	{
		biz_mic_input[i] = round(mic_blow[sampPos++] * (vol / 100.0));
		if (sampPos >= sampLen) sampPos = 0;
	}
}

static bool RunningFrame = false;

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	RunningFrame = true;

	if (f->Keys & 0x8000)
	{
		NDS::Reset();
		if (SkipFW || NDS::NeedsDirectBoot())
		{
			FileManager::SetupDirectBoot();
		}

		NDS::Start();
	}

	NDS::SetKeyMask(~f->Keys & 0xFFF);

	if (f->Keys & 0x1000)
	{
		// move touch coords incrementally to our new touch point
		NDS::MoveTouch(f->TouchX, f->TouchY);
	}
	else
	{
		NDS::ReleaseScreen();
	}

	if (f->Keys & 0x2000)
	{
		NDS::SetLidClosed(false);
	}
	else if (f->Keys & 0x4000)
	{
		NDS::SetLidClosed(true);
	}

	MicFeedNoise(f->MicVolume);
	NDS::MicInputFrame(biz_mic_input, 735);

	int sensor = GBACart::SetInput(0, 1);
	if (sensor != -1 && ValidRange(f->GBALightSensor))
	{
		if (sensor > f->GBALightSensor)
		{
			while (GBACart::SetInput(0, 1) != f->GBALightSensor);
		}
		else if (sensor < f->GBALightSensor)
		{
			while (GBACart::SetInput(1, 1) != f->GBALightSensor);
		}
	}

	NDS::RunFrame();

	if (f->Keys & 0x1000)
	{
		// finalize touch after emulation finishes
		NDS::TouchScreen(f->TouchX, f->TouchY);
	}

	if (!GPU3D::CurrentRenderer->Accelerated)
	{
		auto softRenderer = reinterpret_cast<GPU3D::SoftRenderer*>(GPU3D::CurrentRenderer.get());
		softRenderer->StopRenderThread();
	}

	if (GLPresentation)
	{
		std::tie(f->Width, f->Height) = GLPresenter::Present();
	}
	else
	{
		constexpr u32 SingleScreenSize = 256 * 192;
		memcpy(f->VideoBuffer, GPU::Framebuffer[GPU::FrontBuffer][0], SingleScreenSize * sizeof(u32));
		memcpy(f->VideoBuffer + SingleScreenSize, GPU::Framebuffer[GPU::FrontBuffer][1], SingleScreenSize * sizeof(u32));

		f->Width = 256;
		f->Height = 384;
	}

	f->Samples = SPU::ReadOutput(f->SoundBuffer);
	if (f->Samples == 0) // hack when core decides to stop outputting audio altogether (lid closed or power off)
	{
		memset(f->SoundBuffer, 0, 737 * 2 * sizeof(u16));
		f->Samples = 737;
	}

	f->Cycles = NDS::GetSysClockCycles(2);

	// if we want to consider other lag sources, use that lag flag if we haven't unlagged already 
	if (f->ConsiderAltLag && NDS::LagFrameFlag)
	{
		f->Lagged = NDS::AltLagFrameFlag;
	}
	else
	{
		f->Lagged = NDS::LagFrameFlag;
	}

	RunningFrame = false;
}

ECL_EXPORT u32 GetCallbackCycleOffset()
{
	return RunningFrame ? NDS::GetSysClockCycles(2) : 0;
}
