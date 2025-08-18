#include "NDS.h"
#include "GPU.h"
#include "SPU.h"
#include "RTC.h"
#include "GBACart.h"

#include "BizPlatform/BizOGL.h"
#include "BizPlatform/BizUserData.h"
#include "BizGLPresenter.h"

#include <emulibc.h>
#include <waterboxcore.h>

static bool GLPresentation;

ECL_EXPORT const char* InitGL(BizOGL::LoadGLProc loadGLProc, int threeDeeRenderer, int scaleFactor, bool isWinApi)
{
	switch (threeDeeRenderer)
	{
		case 0:
			BizOGL::LoadGL(loadGLProc, BizOGL::LoadGLVersion::V3_1, isWinApi);
			break;
		case 1:
			BizOGL::LoadGL(loadGLProc, BizOGL::LoadGLVersion::V3_2, isWinApi);
			break;
		case 2:
			BizOGL::LoadGL(loadGLProc, BizOGL::LoadGLVersion::V4_3, isWinApi);
			break;
		default:
			return "Unknown 3DRenderer!";
	}

	GLPresenter::Init(threeDeeRenderer ? scaleFactor : 1);
	GLPresentation = true;
	return nullptr;
}

struct MyFrameInfo : public FrameInfo
{
	melonDS::NDS* NDS;
	u32 Keys;
	u8 TouchX;
	u8 TouchY;
	u8 MicVolume;
	u8 GBALightSensor;
	bool ConsiderAltLag;
	bool UseTouchInterpolation;
};

static bool RunningFrame = false;

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
	RunningFrame = true;

	f->NDS->SetKeyMask(~f->Keys & 0xFFF);

	if (f->Keys & 0x1000)
	{
		if (f->UseTouchInterpolation)
		{
			// move touch coords incrementally to our new touch point
			f->NDS->MoveTouch(f->TouchX, f->TouchY);
		}
		else
		{
			f->NDS->TouchScreen(f->TouchX, f->TouchY);
		}
	}
	else
	{
		f->NDS->ReleaseScreen();
	}

	if (f->Keys & 0x2000)
	{
		f->NDS->SetLidClosed(false);
	}
	else if (f->Keys & 0x4000)
	{
		f->NDS->SetLidClosed(true);
	}

	auto* bizUserData = static_cast<melonDS::Platform::BizUserData*>(f->NDS->UserData);
	bizUserData->MicVolume = f->MicVolume;

	if (auto* gbaCart = f->NDS->GetGBACart())
	{
		int sensor = gbaCart->SetInput(melonDS::GBACart::Input_SolarSensorDown, 1);
		if (sensor != -1)
		{
			if (f->GBALightSensor > 10) f->GBALightSensor = 10;

			if (sensor > f->GBALightSensor)
			{
				while (gbaCart->SetInput(melonDS::GBACart::Input_SolarSensorDown, 1) != f->GBALightSensor);
			}
			else if (sensor < f->GBALightSensor)
			{
				while (gbaCart->SetInput(melonDS::GBACart::Input_SolarSensorUp, 1) != f->GBALightSensor);
			}
		}
	}

	melonDS::NDS::Current = f->NDS;
	f->NDS->RunFrame();

	if (f->Keys & 0x1000)
	{
		if (f->UseTouchInterpolation)
		{
			// finalize touch after emulation finishes
			f->NDS->TouchScreen(f->TouchX, f->TouchY);
		}
	}

	auto& renderer3d = f->NDS->GetRenderer3D();
	if (!renderer3d.Accelerated)
	{
		auto& softRenderer = static_cast<melonDS::SoftRenderer&>(renderer3d);
		softRenderer.StopRenderThread();
	}

	if (GLPresentation)
	{
		std::tie(f->Width, f->Height) = GLPresenter::Present(f->NDS->GPU);
	}
	else
	{
		constexpr u32 SingleScreenSize = 256 * 192;

		auto& gpu = f->NDS->GPU;
		memcpy(f->VideoBuffer, gpu.Framebuffer[gpu.FrontBuffer][0].get(), SingleScreenSize * sizeof(u32));
		memcpy(f->VideoBuffer + SingleScreenSize, gpu.Framebuffer[gpu.FrontBuffer][1].get(), SingleScreenSize * sizeof(u32));

		f->Width = 256;
		f->Height = 384;
	}

	f->Samples = f->NDS->SPU.ReadOutput(f->SoundBuffer, 4096);
	if (f->Samples == 0) // hack when core decides to stop outputting audio altogether (power off)
	{
		memset(f->SoundBuffer, 0, 737 * 2 * sizeof(u16));
		f->Samples = 737;
	}

	f->Cycles = f->NDS->GetSysClockCycles(2);

	// if we want to consider other lag sources, use that lag flag if we haven't unlagged already 
	if (f->ConsiderAltLag && f->NDS->LagFrameFlag)
	{
		f->Lagged = f->NDS->AltLagFrameFlag;
	}
	else
	{
		f->Lagged = f->NDS->LagFrameFlag;
	}

	RunningFrame = false;
}

ECL_EXPORT u32 GetCallbackCycleOffset(melonDS::NDS* nds)
{
	return RunningFrame ? nds->GetSysClockCycles(2) : 0;
}

ECL_EXPORT void SetSoundConfig(melonDS::NDS* nds, int bitDepth, int interpolation)
{
	nds->SPU.SetDegrade10Bit(static_cast<melonDS::AudioBitDepth>(bitDepth));
	nds->SPU.SetInterpolation(static_cast<melonDS::AudioInterpolation>(interpolation));
}
