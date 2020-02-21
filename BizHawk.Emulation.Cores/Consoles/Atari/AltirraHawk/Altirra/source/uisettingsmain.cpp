//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/filesys.h>
#include <at/atcore/media.h>
#include <at/atio/image.h>
#include "uisettingswindow.h"

#include "audiooutput.h"
#include "audiosampleplayer.h"
#include "simulator.h"
#include "gtia.h"
#include "pokey.h"
#include "disk.h"
#include "options.h"
#include "uitypes.h"
#include "uicommondialogs.h"
#include "uiaccessors.h"

extern ATSimulator g_sim;

void ATUISetOverscanMode(ATGTIAEmulator::OverscanMode mode);
void OnCommandOpen(bool forceColdBoot);
void OnCommandExit();

void ATAppendDiskDrivePath(VDStringW& s, const ATDiskEmulator& drive, const ATDiskInterface& diskIf) {
	if (drive.IsEnabled() || diskIf.GetClientCount() > 1) {
		if (diskIf.IsDiskLoaded()) {
			s += L" [";
			s += VDFileSplitPath(diskIf.GetPath());
			s += L']';
		} else {
			s += L" (No disk)";
		}
	}
}

class ATUIFutureSelectDiskImage : public ATUIFutureWithResult<bool> {
public:
	ATUIFutureSelectDiskImage(int index) : mDriveIndex(index) {}

	void RunInner() override {
		switch(mStage) {
			case 0:
				mFileResult = ATUIShowOpenFileDialog('disk', L"Browse for disk", L"All files (*.*)\0*.*\0");
				Wait(mFileResult);
				++mStage;
				break;

			case 1:
				MarkCompleted(false);

				if (mFileResult->mbAccepted) {
					ATImageLoadContext ctx = {};
					ctx.mLoadType = kATImageType_Disk;
					ctx.mLoadIndex = mDriveIndex;
					g_sim.Load(mFileResult->mPath.c_str(), kATMediaWriteMode_RO, &ctx);
				}
				break;
		}
	}

protected:
	vdrefptr<ATUIFileDialogResult> mFileResult;
	int mDriveIndex;
};

class ATUISettingsScreenDiskDrive : public vdrefcounted<IATUISettingsScreen> {
public:
	ATUISettingsScreenDiskDrive(int drive) : mDriveIndex(drive) {}

	void BuildSettings(ATUISettingsWindow *target) {
		const int driveIndex = mDriveIndex;
		vdautoptr<ATUIEnumSetting> es;

		target->SetCaption(VDStringW().sprintf(L"Drive D%d:", mDriveIndex + 1).c_str());

		es = new ATUIEnumSetting(
			L"Mode",
			{
				{ 0, L"Off" },
				{ 1, L"Read only" },
				{ 2, L"VirtRW Safe" },
				{ 3, L"Virtual read/write" },
				{ 4, L"Read/write" },
			}
		);

		es->SetValueDynamic();

		es->SetGetter(
			[driveIndex]() -> sint32 {
				auto& dd = g_sim.GetDiskDrive(driveIndex);

				if (!dd.IsEnabled())
					return 0;

				auto& diskIf = g_sim.GetDiskInterface(driveIndex);
				const auto writeMode = diskIf.GetWriteMode();
				if (!(writeMode & kATMediaWriteMode_AllowWrite))
					return 1;

				if (!(writeMode & kATMediaWriteMode_AllowFormat))
					return 2;

				if (!(writeMode & kATMediaWriteMode_AutoFlush))
					return 3;

				return 4;
			}
		);

		es->SetSetter(
			[driveIndex](sint32 v) {
				auto& dd = g_sim.GetDiskDrive(driveIndex);
				auto& diskIf = g_sim.GetDiskInterface(driveIndex);
				
				if (v) {
					if (diskIf.GetClientCount() < 2)
						dd.SetEnabled(true);
				} else {
					dd.SetEnabled(false);
				}

				switch(v) {
					case 1:
					default:
						diskIf.SetWriteMode(kATMediaWriteMode_RO);
						break;
					case 2:
						diskIf.SetWriteMode(kATMediaWriteMode_VRWSafe);
						break;
					case 3:
						diskIf.SetWriteMode(kATMediaWriteMode_VRW);
						break;
					case 4:
						diskIf.SetWriteMode(kATMediaWriteMode_RW);
						break;
				}
			}
		);

		target->AddSetting(es);
		es.release();

		vdautoptr<ATUIActionSetting> as;

		as = new ATUIActionSetting(L"");

		as->SetDynamicNameFn(
			[driveIndex](VDStringW& name) {
				name = L"Image";

				ATAppendDiskDrivePath(name, g_sim.GetDiskDrive(driveIndex), g_sim.GetDiskInterface(driveIndex));
			}
		);

		as->SetAsyncAction(
			[=]() { return vdrefptr<ATUIFutureWithResult<bool>>(new ATUIFutureSelectDiskImage(this->mDriveIndex)); }
		);
		target->AddSetting(as);
		as.release();

		as = new ATUIActionSetting(L"Eject");
		as->SetAction(
			[driveIndex]() -> bool {
				auto& diskIf = g_sim.GetDiskInterface(driveIndex);
				auto& drive = g_sim.GetDiskDrive(driveIndex);

				if (diskIf.IsDiskLoaded())
					diskIf.UnloadDisk();
				else
					drive.SetEnabled(false);
				return false;
			}
		);
		target->AddSetting(as);
		as.release();
	}

protected:
	const int mDriveIndex;
};

class ATUISettingsScreenDisk : public vdrefcounted<IATUISettingsScreen> {
public:
	void BuildSettings(ATUISettingsWindow *target) {
		vdautoptr<ATUIEnumSetting> es;
		vdautoptr<ATUIBoolSetting> bs;
		vdautoptr<ATUISubScreenSetting> ss;

		target->SetCaption(L"Disk drives");

		bs = new ATUIBoolSetting(L"SIO patch");
		bs->SetGetter([]() { return g_sim.IsDiskSIOPatchEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetDiskSIOPatchEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"SIO override detection");
		bs->SetGetter([]() { return g_sim.IsDiskSIOOverrideDetectEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetDiskSIOOverrideDetectEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Accurate sector timing");
		bs->SetGetter([]() { return g_sim.IsDiskAccurateTimingEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetDiskAccurateTimingEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Show sector counter");
		bs->SetGetter([]() { return g_sim.IsDiskSectorCounterEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetDiskSectorCounterEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		es = new ATUIEnumSetting(
			L"Emulation mode",
			{
				{ kATDiskEmulationMode_Generic,			L"Generic" },
				{ kATDiskEmulationMode_FastestPossible,	L"Fastest possible" },
				{ kATDiskEmulationMode_810,				L"810" },
				{ kATDiskEmulationMode_1050,			L"1050" },
				{ kATDiskEmulationMode_XF551,			L"XF551" },
				{ kATDiskEmulationMode_USDoubler,		L"US Doubler" },
				{ kATDiskEmulationMode_Speedy1050,		L"Speedy 1050" },
				{ kATDiskEmulationMode_IndusGT,			L"Indus GT" },
				{ kATDiskEmulationMode_Happy810,		L"Happy 810" },
				{ kATDiskEmulationMode_Happy1050,		L"Happy 1050" },
				{ kATDiskEmulationMode_1050Turbo,		L"1050 Turbo" },
				{ kATDiskEmulationMode_Generic57600,	L"Generic (57.6Kbaud)" },
			}
		);
		es->SetGetter([]() { return g_sim.GetDiskDrive(0).GetEmulationMode(); });
		es->SetImmediateSetter(
			[](sint32 v) {
				for(int i=0; i<15; ++i)
					g_sim.GetDiskDrive(i).SetEmulationMode((ATDiskEmulationMode)v);
			}
		);
		target->AddSetting(es);
		es.release();

		for(int i=0; i<15; ++i) {
			ATDiskEmulator& drive = g_sim.GetDiskDrive(i);

			VDStringW label;
			label.sprintf(L"Drive D%d:", i+1);

			ATAppendDiskDrivePath(label, drive, g_sim.GetDiskInterface(i));

			ss = new ATUISubScreenSetting(label.c_str(),
				[=](IATUISettingsScreen **screen) {
					*screen = new ATUISettingsScreenDiskDrive(i);
					(*screen)->AddRef();
				}
			);

			target->AddSetting(ss);
			ss.release();
		}
	}
};

class ATUISettingsScreenDisplay : public vdrefcounted<IATUISettingsScreen> {
public:
	void BuildSettings(ATUISettingsWindow *target) {
		vdautoptr<ATUIEnumSetting> es;
		vdautoptr<ATUIBoolSetting> bs;

		target->SetCaption(L"Display");

		bs = new ATUIBoolSetting(L"Full screen mode");
		bs->SetGetter([]() { return ATUIGetFullscreen(); });
		bs->SetImmediateSetter([](bool b) { ATSetFullscreen(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Show FPS");
		bs->SetGetter([]() { return ATUIGetShowFPS(); });
		bs->SetImmediateSetter([](bool b) { ATUISetShowFPS(b); });
		target->AddSetting(bs);
		bs.release();

		es = new ATUIEnumSetting(
			L"Stretch mode",
			{
				{ 	kATDisplayStretchMode_Unconstrained, L"Stretch" },
				{	kATDisplayStretchMode_PreserveAspectRatio, L"Aspect" },
				{	kATDisplayStretchMode_SquarePixels, L"Square" },
				{	kATDisplayStretchMode_Integral, L"Square (int.)" },
				{	kATDisplayStretchMode_IntegralPreserveAspectRatio, L"Aspect (int.)" },
			}
		);

		es->SetGetter([]() { return ATUIGetDisplayStretchMode(); });
		es->SetImmediateSetter([](sint32 v) { return ATUISetDisplayStretchMode((ATDisplayStretchMode)v); });
		target->AddSetting(es);
		es.release();

		es = new ATUIEnumSetting(
			L"Overscan mode",
			{
				{ ATGTIAEmulator::kOverscanOSScreen, L"OS Screen Only" },
				{ ATGTIAEmulator::kOverscanNormal, L"Normal" },
				{ ATGTIAEmulator::kOverscanExtended, L"Extended" },
				{ ATGTIAEmulator::kOverscanFull, L"Full" },
			}
		);

		es->SetGetter([]() { return g_sim.GetGTIA().GetOverscanMode(); });
		es->SetImmediateSetter([](sint32 v) { return ATUISetOverscanMode((ATGTIAEmulator::OverscanMode)v); });
		target->AddSetting(es);
		es.release();

		es = new ATUIEnumSetting(
			L"Filter mode",
			{
				{ kATDisplayFilterMode_Point, L"Point" },
				{ kATDisplayFilterMode_Bilinear, L"Bilinear" },
				{ kATDisplayFilterMode_SharpBilinear, L"Sharp bilinear" },
				{ kATDisplayFilterMode_Bicubic, L"Bicubic" },
			}
		);

		es->SetGetter([]() { return ATUIGetDisplayFilterMode(); });
		es->SetImmediateSetter([](sint32 val) { ATUISetDisplayFilterMode((ATDisplayFilterMode)val); });
		target->AddSetting(es);
		es.release();

		es = new ATUIEnumSetting(
			L"Sharpness",
			{
				{ -2, L"Softest" },
				{ -1, L"Softer" },
				{  0, L"Normal" },
				{ +1, L"Sharper" },
				{ +2, L"Sharpest" },
			}
		);

		es->SetGetter([]() { return ATUIGetViewFilterSharpness(); });
		es->SetImmediateSetter([](sint32 val) { ATUISetViewFilterSharpness(val); });
		target->AddSetting(es);
		es.release();
		
		bs = new ATUIBoolSetting(L"Frame blending");
		bs->SetGetter([]() { return g_sim.GetGTIA().IsBlendModeEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.GetGTIA().SetBlendModeEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Interlace");
		bs->SetGetter([]() { return g_sim.GetGTIA().IsInterlaceEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.GetGTIA().SetInterlaceEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Scanlines");
		bs->SetGetter([]() { return g_sim.GetGTIA().AreScanlinesEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.GetGTIA().SetScanlinesEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		es = new ATUIEnumSetting(
			L"Artifacting",
			{
				{ ATGTIAEmulator::kArtifactNone, L"Off" },
				{ ATGTIAEmulator::kArtifactNTSC, L"NTSC" },
				{ ATGTIAEmulator::kArtifactNTSCHi, L"NTSC High" },
				{ ATGTIAEmulator::kArtifactPAL, L"PAL" },
				{ ATGTIAEmulator::kArtifactPALHi, L"PAL High" },
				{ ATGTIAEmulator::kArtifactAuto, L"NTSC/PAL (auto-switch)" },
				{ ATGTIAEmulator::kArtifactAutoHi, L"NTSC/PAL High (auto-switch)" },
			}
		);

		es->SetGetter([]() { return g_sim.GetGTIA().GetArtifactingMode(); });
		es->SetImmediateSetter([](sint32 val) { g_sim.GetGTIA().SetArtifactingMode((ATGTIAEmulator::ArtifactMode)val); });
		target->AddSetting(es);
		es.release();

		bs = new ATUIBoolSetting(L"XEP-80 view");
		bs->SetGetter([]() { return ATUIGetXEPViewEnabled(); });
		bs->SetImmediateSetter([](bool b) { ATUISetXEPViewEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"XEP-80 view autoswitch");
		bs->SetGetter([]() { return ATUIGetXEPViewAutoswitchingEnabled(); });
		bs->SetImmediateSetter([](bool b) { ATUISetXEPViewAutoswitchingEnabled(b); });
		target->AddSetting(bs);
		bs.release();
	}
};

class ATUISettingsScreenSound : public vdrefcounted<IATUISettingsScreen> {
public:
	void BuildSettings(ATUISettingsWindow *target) {
		vdautoptr<ATUIEnumSetting> es;
		vdautoptr<ATUIBoolSetting> bs;
		vdautoptr<ATUIIntSetting> is;

		target->SetCaption(L"Sound");

		bs = new ATUIBoolSetting(L"Stereo");
		bs->SetGetter([]() { return g_sim.IsDualPokeysEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetDualPokeysEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Audio monitor");
		bs->SetGetter([]() { return g_sim.IsAudioMonitorEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetAudioMonitorEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		is = new ATUIIntSetting(L"Volume", 0, 200);
		is->SetGetter([]() { return MapAmplitudeToVolTick(g_sim.GetAudioOutput()->GetVolume()); });
		is->SetImmediateSetter([](sint32 tick) { g_sim.GetAudioOutput()->SetVolume(MapVolTickToAmplitude(tick)); });
		target->AddSetting(is);
		is.release();

		bs = new ATUIBoolSetting(L"Drive sounds");
		bs->SetGetter([]() { return ATUIGetDriveSoundsEnabled(); });
		bs->SetImmediateSetter([](bool b) { ATUISetDriveSoundsEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		is = new ATUIIntSetting(L"Drive volume", 0, 200);
		is->SetGetter([]() { return MapAmplitudeToVolTick(g_sim.GetAudioOutput()->GetMixLevel(kATAudioMix_Drive)); });
		is->SetImmediateSetter([](sint32 tick) { g_sim.GetAudioOutput()->SetMixLevel(kATAudioMix_Drive, MapVolTickToAmplitude(tick)); });
		target->AddSetting(is);
		is.release();
	}

protected:
	static sint32 MapAmplitudeToVolTick(float amplitude) {
		if (amplitude <= 0.01f)
			return 0;
		else if (amplitude >= 1.0f)
			return 200;

		sint32 tick = 200 + VDRoundToInt(100.0f * log10(amplitude));

		if (tick < 0)
			return 0;
		else if (tick > 200)
			return 200;
		else
			return tick;
	}

	static float MapVolTickToAmplitude(sint32 tick) {
		return powf(10.0f, (tick - 200) * 0.01f);
	}
};

class ATUISettingsScreenSystem : public vdrefcounted<IATUISettingsScreen> {
public:
	void BuildSettings(ATUISettingsWindow *target) {
		vdautoptr<ATUIEnumSetting> es;
		vdautoptr<ATUIBoolSetting> bs;

		target->SetCaption(L"System");

		es = new ATUIEnumSetting(
			L"Hardware mode",
			{
				{ kATHardwareMode_800, L"400/800" },
				{ kATHardwareMode_1200XL, L"1200XL" },
				{ kATHardwareMode_800XL, L"600/800XL" },
				{ kATHardwareMode_130XE, L"130XE" },
				{ kATHardwareMode_XEGS, L"XEGS" },
				{ kATHardwareMode_5200, L"5200" },
			}
		);

		es->SetGetter([]() { return g_sim.GetHardwareMode(); });
		es->SetSetter([](sint32 mode) { ATUISwitchHardwareMode(nullptr, (ATHardwareMode)mode, false); });
		target->AddSetting(es);
		es.release();

		es = new ATUIEnumSetting(
			L"Video standard",
			{
				{ kATVideoStandard_NTSC, L"NTSC" },
				{ kATVideoStandard_PAL, L"PAL" },
				{ kATVideoStandard_SECAM, L"SECAM" },
				{ kATVideoStandard_NTSC50, L"NTSC50" },
				{ kATVideoStandard_PAL60, L"PAL60" },
			}
		);

		es->SetGetter([]() { return g_sim.GetVideoStandard(); });
		es->SetSetter([](sint32 value) { ATSetVideoStandard((ATVideoStandard)value); g_sim.ColdReset(); });
		target->AddSetting(es);
		es.release();

		es = new ATUIEnumSetting(
			L"Memory config",
			{
				{ kATMemoryMode_8K, L"8K" },
				{ kATMemoryMode_16K, L"16K" },
				{ kATMemoryMode_24K, L"24K" },
				{ kATMemoryMode_32K, L"32K" },
				{ kATMemoryMode_40K, L"40K" },
				{ kATMemoryMode_48K, L"48K" },
				{ kATMemoryMode_52K, L"52K" },
				{ kATMemoryMode_64K, L"64K" },
				{ kATMemoryMode_128K, L"128K" },
				{ kATMemoryMode_256K, L"256K Rambo" },
				{ kATMemoryMode_320K, L"320K Rambo" },
				{ kATMemoryMode_320K_Compy, L"320K Compy" },
				{ kATMemoryMode_576K, L"576K" },
				{ kATMemoryMode_576K_Compy, L"576K Compy" },
				{ kATMemoryMode_1088K, L"1088K" },
			}
		);

		es->SetGetter([]() { return g_sim.GetMemoryMode(); });
		es->SetSetter([](sint32 value) { ATUISwitchMemoryMode(nullptr, (ATMemoryMode)value); g_sim.ColdReset(); });
		es->SetValueDynamic();
		target->AddSetting(es);
		es.release();

		bs = new ATUIBoolSetting(L"Built-in BASIC");
		bs->SetGetter([]() { return g_sim.IsBASICEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetBASICEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Fast boot");
		bs->SetGetter([]() { return g_sim.IsFastBootEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetFastBootEnabled(b); });
		target->AddSetting(bs);
		bs.release();

		bs = new ATUIBoolSetting(L"Fast math");
		bs->SetGetter([]() { return g_sim.IsFPPatchEnabled(); });
		bs->SetImmediateSetter([](bool b) { g_sim.SetFPPatchEnabled(b); });
		target->AddSetting(bs);
		bs.release();
	}
};

class ATUISettingsScreenUI : public vdrefcounted<IATUISettingsScreen> {
public:
	void BuildSettings(ATUISettingsWindow *target) {
		vdautoptr<ATUIEnumSetting> es;
		vdautoptr<ATUIBoolSetting> bs;

		target->SetCaption(L"UI");

		es = new ATUIEnumSetting(
			L"UI scale",
			{
				{ 100, L"100%" },
				{ 125, L"125%" },
				{ 150, L"150%" },
				{ 200, L"200%" },
			}
		);

		static const sint32 kFactors[]={
			100,
			125,
			150,
			200
		};

		es->SetGetter(
			[]() -> sint32 {
				return g_ATOptions.mThemeScale;
			}
		);

		es->SetSetter(
			[](sint32 mode) {
				if (mode >= 100 && mode <= 200) {
					ATOptions opts(g_ATOptions);

					g_ATOptions.mThemeScale = mode;
					g_ATOptions.mbDirty = true;

					ATOptionsSave();
					ATOptionsRunUpdateCallbacks(&opts);
				}
			}
		);
		target->AddSetting(es);
		es.release();
	}
};

class ATUISettingsScreenMain : public vdrefcounted<IATUISettingsScreen> {
public:
	void BuildSettings(ATUISettingsWindow *target) {
		target->SetCaption(L"Settings");

		vdautoptr<ATUIActionSetting> as;
		as = new ATUIActionSetting(L"On-screen keyboard",
			[]() -> bool {
				ATUIOpenOnScreenKeyboard();
				return true;
			}
		);
		target->AddSetting(as);
		as.release();

		as = new ATUIActionSetting(L"Boot image...");
		as->SetAction(
			[]() -> bool { OnCommandOpen(true); return true; }
		);
		target->AddSetting(as);
		as.release();

		target->AddSeparator();

		vdautoptr<ATUISubScreenSetting> ss;
		ss = new ATUISubScreenSetting(L"System...",
			[](IATUISettingsScreen **screen) {
				*screen = new ATUISettingsScreenSystem;
				(*screen)->AddRef();
			}
		);
		target->AddSetting(ss);
		ss.release();

		ss = new ATUISubScreenSetting(L"Disk drives...",
			[](IATUISettingsScreen **screen) {
				*screen = new ATUISettingsScreenDisk;
				(*screen)->AddRef();
			}
		);
		target->AddSetting(ss);
		ss.release();

		ss = new ATUISubScreenSetting(L"Display...",
			[](IATUISettingsScreen **screen) {
				*screen = new ATUISettingsScreenDisplay;
				(*screen)->AddRef();
			}
		);
		target->AddSetting(ss);
		ss.release();

		ss = new ATUISubScreenSetting(L"Sound...",
			[](IATUISettingsScreen **screen) {
				*screen = new ATUISettingsScreenSound;
				(*screen)->AddRef();
			}
		);
		target->AddSetting(ss);
		ss.release();

		target->AddSeparator();

		vdautoptr<ATUIBoolSetting> bs;

		bs = new ATUIBoolSetting(L"Warp speed");
		bs->SetGetter([]() { return ATUIGetTurbo(); });
		bs->SetImmediateSetter([](bool b) { ATUISetTurbo(b); });
		target->AddSetting(bs);
		bs.release();

		as = new ATUIActionSetting(L"Cold reset",
			[]() -> bool {
				g_sim.ColdReset();
				g_sim.Resume();
				return true;
			}
		);
		target->AddSetting(as);
		as.release();

		as = new ATUIActionSetting(L"Warm reset",
			[]() -> bool {
				g_sim.WarmReset();
				g_sim.Resume();
				return true;
			}
		);
		target->AddSetting(as);
		as.release();

		target->AddSeparator();

		ss = new ATUISubScreenSetting(L"UI options...",
			[](IATUISettingsScreen **screen) {
				*screen = new ATUISettingsScreenUI;
				(*screen)->AddRef();
			}
		);
		target->AddSetting(ss);
		ss.release();

		as = new ATUIActionSetting(L"Quit", []() -> bool { OnCommandExit(); return true; });
		target->AddSetting(as);
		as.release();
	}
};

void ATCreateUISettingsScreenMain(IATUISettingsScreen **screen) {
	*screen = new ATUISettingsScreenMain;
	(*screen)->AddRef();
}
