//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2015 Avery Lee
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
#include <vd2/system/bitmath.h>
#include <vd2/system/date.h>
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include <vd2/system/hash.h>
#include <vd2/system/math.h>
#include <vd2/system/registry.h>
#include <vd2/system/time.h>
#include <at/atcore/devicemanager.h>
#include <at/atcore/media.h>
#include <at/atio/image.h>
#include <at/atui/uimanager.h>
#include "audiosampleplayer.h"
#include "audiooutput.h"
#include "cartridge.h"
#include "cassette.h"
#include "debugger.h"
#include "disk.h"
#include "firmwaremanager.h"
#include "ide.h"
#include "idephysdisk.h"
#include "inputcontroller.h"
#include "inputmanager.h"
#include "joystick.h"
#include "settings.h"
#include "simulator.h"
#include "uiaccessors.h"
#include "uiconfirm.h"
#include "uikeyboard.h"
#include "uiportmenus.h"
#include "uitypes.h"

extern ATSimulator g_sim;
extern ATUIKeyboardOptions g_kbdOpts;
extern ATUIManager g_ATUIManager;

bool g_ATInPortableMode;
bool g_ATSettingsMigrationScheduled;

uint32 g_ATCurrentProfileId = 0;
bool g_ATProfileTemporary = false;
bool g_ATProfileBootstrap = false;

uint32 g_ATDefaultProfileIds[kATDefaultProfileCount];

void ATSyncCPUHistoryState();
void ATUIUpdateSpeedTiming();
void ATUIResizeDisplay();

///////////////////////////////////////////////////////////////////////////

namespace {
	const wchar_t *const kCategoryTagNames[]={
		L"hardware",
		L"firmware",
		L"acceleration",
		L"debugging",
		L"devices",
		L"startupconfig",
		L"environment",
		L"color",
		L"view",
		L"inputMaps",
		L"input",
		L"speed",
		L"mountedimages",
		L"fullscreen",
		L"sound",
		L"boot",
	};
}

VDStringW ATSettingsCategoryMaskToTagString(ATSettingsCategory mask) {
	if (mask == kATSettingsCategory_All)
		return VDStringW(L"all");

	VDStringW s;

	for(uint32 i=0; i<vdcountof(kCategoryTagNames); ++i) {
		if (mask & (1 << i)) {
			if (!s.empty())
				s += ',';

			s += kCategoryTagNames[i];
		}
	}

	return s;
}

ATSettingsCategory ATSettingsCategoryMaskFromTagString(const wchar_t *s) {
	VDStringRefW parser(s);
	if (parser == L"all")
		return kATSettingsCategory_All;

	VDStringRefW token;
	uint32 mask = 0;

	while(!parser.empty()) {
		if (!parser.split(L',', token)) {
			token = parser;
			parser.clear();
		}

		uint32 bit = 1;
		for(const wchar_t *tag : kCategoryTagNames) {
			if (token == tag) {
				mask |= bit;
				break;
			}

			bit <<= 1;
		}
	}

	if (mask == (1 << vdcountof(kCategoryTagNames)) - 1)
		mask = kATSettingsCategory_All;

	return ATSettingsCategory(mask);
}

namespace {
	class ProfileKey : public VDRegistryAppKey {
	public:
		ProfileKey(uint32 profileId, bool write);
	};

	ProfileKey::ProfileKey(uint32 profileId, bool write)
		: VDRegistryAppKey(VDStringA().sprintf("Profiles\\%08X", profileId).c_str(), write)
	{
	}
}

void ATSettingsProfileEnum(vdfastvector<uint32>& profileIds) {
	VDRegistryAppKey key("Profiles", false);
	VDRegistryKeyIterator it(key);

	while(const char *s = it.Next()) {
		if (strlen(s) != 8)
			continue;

		int i=0;
		for(; i<8; ++i) {
			if (!isxdigit((unsigned char)s[i]))
				break;
		}

		if (i < 8)
			continue;

		const uint32 profileId = (uint32)strtoul(s, nullptr, 16);

		if (!profileId)
			continue;

		profileIds.push_back(profileId);
	}

	std::sort(profileIds.begin(), profileIds.end());
	profileIds.erase(std::unique(profileIds.begin(), profileIds.end()), profileIds.end());
}

bool ATSettingsIsValidProfile(uint32 profileId) {
	ProfileKey key(profileId, false);

	return key.isReady();
}

uint32 ATSettingsGenerateProfileId() {
	// try to create a unique ID based on time
	uint64 v = VDGetPreciseTick() ^ VDGetCurrentDate().mTicks;
	uint32 id = (uint32)v + (uint32)(v >> 32);

	// get list of profile IDs
	vdfastvector<uint32> profileIds;
	ATSettingsProfileEnum(profileIds);

	// clear collisions by quadratic probing
	uint32 increment = 1;
	while(id == kATProfileId_Invalid || std::binary_search(profileIds.begin(), profileIds.end(), id)) {
		id += increment;
		increment += 2;
	}

	return id;
}

void ATSettingsProfileSetCategoryMask(uint32 profileId, ATSettingsCategory mask) {
	if (!profileId) {
		VDASSERT(false);
		return;
	}

	ProfileKey key(profileId, true);
	auto&& s = ATSettingsCategoryMaskToTagString(mask);

	key.setString("_Category Mask", s.c_str());

	// check if we need to narrow the saved category mask
	const uint32 savedMask = ATSettingsProfileGetSavedCategoryMask(profileId);

	if (savedMask & ~mask) {
		ATSettingsProfileSetSavedCategoryMask(profileId, (ATSettingsCategory)(savedMask & mask));
	}
}

ATSettingsCategory ATSettingsProfileGetCategoryMask(uint32 profileId) {
	if (!profileId)
		return kATSettingsCategory_All;

	ProfileKey key(profileId, false);
	VDStringW categories;

	key.getString("_Category Mask", categories);

	return ATSettingsCategoryMaskFromTagString(categories.c_str());
}

void ATSettingsProfileSetSavedCategoryMask(uint32 profileId, ATSettingsCategory mask) {
	if (!profileId) {
		VDASSERT(false);
		return;
	}

	ProfileKey key(profileId, true);
	auto&& s = ATSettingsCategoryMaskToTagString(mask);

	key.setString("_Saved Category Mask", s.c_str());
}

ATSettingsCategory ATSettingsProfileGetSavedCategoryMask(uint32 profileId) {
	if (!profileId)
		return kATSettingsCategory_All;

	ProfileKey key(profileId, false);
	VDStringW categories;

	key.getString("_Saved Category Mask", categories);

	return ATSettingsCategoryMaskFromTagString(categories.c_str());
}

void ATSettingsProfileSetVisible(uint32 profileId, bool visible) {
	ProfileKey key(profileId, true);

	key.setBool("_Visible", visible);
}

bool ATSettingsProfileGetVisible(uint32 profileId) {
	ProfileKey key(profileId, false);

	return key.getBool("_Visible");
}

uint32 ATSettingsProfileGetParent(uint32 profileId) {
	if (!profileId)
		return 0;

	ProfileKey key(profileId, false);

	return (uint32)key.getInt("_Parent");
}

void ATSettingsProfileSetParent(uint32 profileId, uint32 parentId) {
	if (!profileId) {
		VDASSERT(false);
		return;
	}

	ProfileKey key(profileId, true);

	key.setInt("_Parent", (int)parentId);
}

void ATSettingsProfileDelete(uint32 profileId) {
	if (!profileId) {
		VDASSERT(false);
		return;
	}

	// scan default profiles and push up to parent if any point the one we're deleting
	uint32 parentId = kATProfileId_Invalid;
	for(uint32& defaultProfileId : g_ATDefaultProfileIds) {
		if (defaultProfileId == profileId) {
			if (parentId == kATProfileId_Invalid)
				parentId = ATSettingsProfileGetParent(profileId);

			defaultProfileId = parentId;
		}
	}

	// delete the profile
	VDRegistryAppKey key("Profiles", true);

	key.removeKeyRecursive(VDStringA().sprintf("%08X", profileId).c_str());
}

uint32 ATSettingsFindProfileByName(const wchar_t *name) {
	if (!name || !*name)
		return 0;

	vdfastvector<uint32> profileIds;
	ATSettingsProfileEnum(profileIds);

	for(uint32 profileId : profileIds) {
		if (ATSettingsProfileGetName(profileId) == name)
			return profileId;
	}

	return kATProfileId_Invalid;
}

VDStringW ATSettingsProfileGetName(uint32 profileId) {
	if (!profileId)
		return VDStringW(L"Default");

	ProfileKey key(profileId, false);
	VDStringW name;

	key.getString("_Name", name);
	return name;
}

void ATSettingsProfileSetName(uint32 profileId, const wchar_t *name) {
	if (!profileId) {
		VDASSERT(false);
		return;
	}

	ProfileKey key(profileId, true);

	key.setString("_Name", name);
}

///////////////////////////////////////////////////////////////////////////

void ATSettingsExchangeBool(bool write, VDRegistryKey& key, const char *name, const vdfunction<bool()>& getter, const vdfunction<void(bool)>& setter) {
	if (write)
		key.setBool(name, getter());
	else
		setter(key.getBool(name, getter()));
}

void ATSettingsExchangeInt32(bool write, VDRegistryKey& key, const char *name, const vdfunction<sint32()>& getter, const vdfunction<void(sint32)>& setter) {
	if (write)
		key.setInt(name, getter());
	else
		setter(key.getInt(name, getter()));
}

template<class T>
void ATSettingsExchangeEnum(bool write, VDRegistryKey& key, const char *name, T count, const vdfunction<T()>& getter, const vdfunction<void(T)>& setter) {
	if (write)
		key.setInt(name, (int)getter());
	else
		setter((T)key.getEnumInt(name, count, (int)getter()));
}

float ATSettingsGetFloat(VDRegistryKey& key, const char *name, float defaultValue) {
	int defaultInt = VDGetFloatAsInt(defaultValue);
	int currentInt = key.getInt(name, defaultInt);
	float currentValue = VDGetIntAsFloat(currentInt);

	return std::isfinite(currentValue) ? currentValue : defaultValue;
}

///////////////////////////////////////////////////////////////////////////

void ATSettingsExchangeView(bool write, VDRegistryKey& key) {
	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	ATSettingsExchangeEnum<ATDisplayFilterMode>(write, key, "Display: Filter mode", kATDisplayFilterModeCount, ATUIGetDisplayFilterMode, ATUISetDisplayFilterMode);
	ATSettingsExchangeInt32(write, key, "Display: Filter sharpness", ATUIGetViewFilterSharpness, ATUISetViewFilterSharpness);
	ATSettingsExchangeEnum<ATDisplayStretchMode>(write, key, "Display: Stretch mode", kATDisplayStretchModeCount, ATUIGetDisplayStretchMode, ATUISetDisplayStretchMode);
	ATSettingsExchangeBool(write, key, "Display: Show indicators", ATUIGetDisplayIndicators, ATUISetDisplayIndicators);
	ATSettingsExchangeBool(write, key, "Display: Indicator margin", ATUIGetDisplayPadIndicators, ATUISetDisplayPadIndicators);

	if (write) {
		key.setString("Display: Custom effect path", g_ATUIManager.GetCustomEffectPath());
	} else {
		VDStringW path;
		key.getString("Display: Custom effect path", path);

		g_ATUIManager.SetCustomEffectPath(path.c_str(), false);
	}

	ATSettingsExchangeBool(write, key, "View: Show FPS", ATUIGetShowFPS, ATUISetShowFPS);
	ATSettingsExchangeBool(write, key, "View: Vertical sync", [&]() { return gtia.IsVsyncEnabled(); }, [&](bool en) { gtia.SetVsyncEnabled(en); });

	ATSettingsExchangeBool(write, key, "View: 80-column view enabled", ATUIGetXEPViewEnabled, ATUISetXEPViewEnabled);
	ATSettingsExchangeBool(write, key, "View: 80-column view autoswitching enabled", ATUIGetXEPViewAutoswitchingEnabled, ATUISetXEPViewAutoswitchingEnabled);

	ATSettingsExchangeEnum<ATGTIAEmulator::ArtifactMode>(write, key, "GTIA: Artifacting mode", ATGTIAEmulator::kArtifactCount,
		[&]() { return gtia.GetArtifactingMode(); },
		[&](ATGTIAEmulator::ArtifactMode mode) { gtia.SetArtifactingMode(mode); });

	ATSettingsExchangeEnum<ATGTIAEmulator::OverscanMode>(write, key, "GTIA: Overscan mode", ATGTIAEmulator::kOverscanCount,
		[&]() { return gtia.GetOverscanMode(); },
		[&](ATGTIAEmulator::OverscanMode mode) { gtia.SetOverscanMode(mode); });

	ATSettingsExchangeEnum<ATGTIAEmulator::VerticalOverscanMode>(write, key, "GTIA: Vertical overscan mode", ATGTIAEmulator::kVerticalOverscanCount,
		[&]() { return gtia.GetVerticalOverscanMode(); },
		[&](ATGTIAEmulator::VerticalOverscanMode mode) { gtia.SetVerticalOverscanMode(mode); });

	ATSettingsExchangeBool(write, key, "GTIA: PAL extended height",
		[&]() { return gtia.IsOverscanPALExtended(); },
		[&](bool en) { gtia.SetOverscanPALExtended(en); });

	ATSettingsExchangeBool(write, key, "GTIA: Frame blending",
		[&]() { return gtia.IsBlendModeEnabled(); },
		[&](bool en) { gtia.SetBlendModeEnabled(en); });

	ATSettingsExchangeBool(write, key, "GTIA: Interlace",
		[&]() { return gtia.IsInterlaceEnabled(); },
		[&](bool en) { gtia.SetInterlaceEnabled(en); });

	ATSettingsExchangeBool(write, key, "GTIA: Scanlines",
		[&]() { return gtia.AreScanlinesEnabled(); },
		[&](bool en) { gtia.SetScanlinesEnabled(en); });

	if (write) {
		key.setBool("Disk: Sector counter enabled", g_sim.IsDiskSectorCounterEnabled());

		const ATArtifactingParams& aparams = gtia.GetArtifactingParams();
		key.setInt("Scanline intensity", (int)(0.5f + aparams.mScanlineIntensity * 100.0f));
		key.setBool("ScreenFX: Bloom enable", aparams.mbEnableBloom);
		key.setBool("ScreenFX: Bloom scanline compensation", aparams.mbBloomScanlineCompensation);
		key.setInt("ScreenFX: Bloom threshold", (int)(0.5f + aparams.mBloomThreshold * 100.0f));
		key.setInt("ScreenFX: Bloom radius", (int)(0.5f + aparams.mBloomRadius * 10.0f));
		key.setInt("ScreenFX: Bloom direct intensity", (int)(0.5f + aparams.mBloomDirectIntensity * 100.0f));
		key.setInt("ScreenFX: Bloom indirect intensity", (int)(0.5f + aparams.mBloomIndirectIntensity * 100.0f));
		key.setInt("ScreenFX: Distortion X View Angle", (int)(0.5f + aparams.mDistortionViewAngleX));
		key.setInt("ScreenFX: Distortion Y Ratio", (int)(0.5f + aparams.mDistortionYRatio * 100.0f));
	} else {
		g_sim.SetDiskSectorCounterEnabled(key.getBool("Disk: Sector counter enabled", g_sim.IsDiskSectorCounterEnabled()));

		ATUIResizeDisplay();

		ATArtifactingParams aparams = gtia.GetArtifactingParams();
		int sli = key.getInt("Scanline intensity", -1);
		if (sli >= 0 && sli <= 100)
			aparams.mScanlineIntensity = (float)sli / 100.0f;
		
		aparams.mbEnableBloom = key.getBool("ScreenFX: Bloom enable", aparams.mbEnableBloom);
		aparams.mbBloomScanlineCompensation = key.getBool("ScreenFX: Bloom scanline compensation", aparams.mbBloomScanlineCompensation);

		int bt = key.getInt("ScreenFX: Bloom threshold", -1);
		if (bt >= 0 && bt <= 100)
			aparams.mBloomThreshold = (float)bt / 100.0f;

		int br = key.getInt("ScreenFX: Bloom radius", -1);
		if (br >= 1 && br < 1000)
			aparams.mBloomRadius = (float)br / 10.0f;
		int bdi = key.getInt("ScreenFX: Bloom direct intensity", -1);
		if (bdi >= 0 && bdi <= 200)
			aparams.mBloomDirectIntensity = (float)bdi / 100.0f;
		int bii = key.getInt("ScreenFX: Bloom indirect intensity", -1);
		if (bii >= 0 && bii <= 200)
			aparams.mBloomIndirectIntensity = (float)bii / 100.0f;

		int disX = key.getInt("ScreenFX: Distortion X View Angle", -1);
		if (disX >= 0 && disX <= 180)
			aparams.mDistortionViewAngleX = (float)disX;

		int disY = key.getInt("ScreenFX: Distortion Y Ratio", -1);
		if (disY >= 0 && disY <= 100)
			aparams.mDistortionYRatio = (float)disY / 100.0f;

		gtia.SetArtifactingParams(aparams);
	}
}

void ATSettingsExchangeSpeed(bool write, VDRegistryKey& key) {
	if (write) {
		key.setInt("Speed: Frame rate modifier", VDRoundToInt((ATUIGetSpeedModifier() + 1.0f) * 100.0f));
		key.setInt("Speed: Frame rate mode", ATUIGetFrameRateMode());
		key.setBool("Turbo mode", ATUIGetTurbo());
	} else {
		ATUISetSpeedModifier(key.getInt("Speed: Frame rate modifier", VDRoundToInt((ATUIGetSpeedModifier() + 1.0f) * 100.0f)) / 100.0f - 1.0f);
		ATUISetFrameRateMode((ATFrameRateMode)key.getEnumInt("Speed: Frame rate mode", kATFrameRateModeCount, ATUIGetFrameRateMode()));
		ATUISetTurbo(key.getBool("Turbo mode", ATUIGetTurbo()));
	}
}

void ATSettingsExchangeInput(bool write, VDRegistryKey& key) {
	// native mouse
	ATSettingsExchangeBool(write, key, "Mouse: Auto-capture", ATUIGetMouseAutoCapture, ATUISetMouseAutoCapture);

	// light pen
	ATLightPenPort *lpp = g_sim.GetLightPenPort();
	if (write) {
		key.setInt("Light Pen: Adjust X", lpp->GetAdjustX());
		key.setInt("Light Pen: Adjust Y", lpp->GetAdjustY());
	} else {
		lpp->SetAdjust(key.getInt("Light Pen: Adjust X", lpp->GetAdjustX()), key.getInt("Light Pen: Adjust Y", lpp->GetAdjustY()));
	}

	// keyboard
	if (write) {
		{
			vdfastvector<uint32> km;
			ATUIGetCustomKeyMap(km);

			key.setBinary("Keyboard: Custom Layout", (const char *)km.data(), (int)(km.size() * sizeof(km[0])));
		}

		key.setBool("Keyboard: Raw mode", g_kbdOpts.mbRawKeys);
		key.setBool("Keyboard: Full raw mode", g_kbdOpts.mbFullRawKeys);
		key.setInt("Keyboard: Arrow key mode", g_kbdOpts.mArrowKeyMode);
		key.setInt("Keyboard: Layout mode", g_kbdOpts.mLayoutMode);
		key.setBool("Keyboard: Allow shift on cold reset", g_kbdOpts.mbAllowShiftOnColdReset);
		key.setBool("Keyboard: Enable function keys", g_kbdOpts.mbEnableFunctionKeys);
		key.setBool("Keyboard: Allow input map overlap", g_kbdOpts.mbAllowInputMapOverlap);
	} else {
		{
			int kmlen = key.getBinaryLength("Keyboard: Custom Layout");

			if (!(kmlen & 3) && kmlen < 0x10000) {
				vdblock<uint32> km(kmlen >> 2);
				if (key.getBinary("Keyboard: Custom Layout", (char *)km.data(), kmlen))
					ATUISetCustomKeyMap(km.data(), km.size());
			}
		}

		g_kbdOpts.mbRawKeys = key.getBool("Keyboard: Raw mode", g_kbdOpts.mbRawKeys);
		g_kbdOpts.mbFullRawKeys = key.getBool("Keyboard: Full raw mode", g_kbdOpts.mbFullRawKeys);
		g_kbdOpts.mArrowKeyMode = (ATUIKeyboardOptions::ArrowKeyMode)key.getEnumInt("Keyboard: Arrow key mode", ATUIKeyboardOptions::kAKMCount, g_kbdOpts.mArrowKeyMode);
		g_kbdOpts.mLayoutMode = (ATUIKeyboardOptions::LayoutMode)key.getEnumInt("Keyboard: Layout mode", ATUIKeyboardOptions::kLMCount, g_kbdOpts.mLayoutMode);
		g_kbdOpts.mbAllowShiftOnColdReset = key.getBool("Keyboard: Allow shift on cold reset", g_kbdOpts.mbAllowShiftOnColdReset);
		g_kbdOpts.mbEnableFunctionKeys = key.getBool("Keyboard: Enable function keys", g_kbdOpts.mbEnableFunctionKeys);
		g_kbdOpts.mbAllowInputMapOverlap = key.getBool("Keyboard: Allow input map overlap", g_kbdOpts.mbAllowInputMapOverlap);
		ATUIInitVirtualKeyMap(g_kbdOpts);
	}

	// joystick
	if (write) {
		const auto& joyxforms = g_sim.GetJoystickManager()->GetTransforms();
		key.setInt("Input: Stick analog dead zone", joyxforms.mStickAnalogDeadZone);
		key.setInt("Input: Stick digital dead zone", joyxforms.mStickDigitalDeadZone);
		key.setInt("Input: Stick analog power", VDGetFloatAsInt(joyxforms.mStickAnalogPower));
		key.setInt("Input: Trigger analog dead zone", joyxforms.mTriggerAnalogDeadZone);
		key.setInt("Input: Trigger digital dead zone", joyxforms.mTriggerDigitalDeadZone);
		key.setInt("Input: Trigger analog power", VDGetFloatAsInt(joyxforms.mTriggerAnalogPower));
	} else {
		ATJoystickTransforms joyxforms = g_sim.GetJoystickManager()->GetTransforms();
		joyxforms.mStickAnalogDeadZone = key.getInt("Input: Stick analog dead zone", joyxforms.mStickAnalogDeadZone);
		joyxforms.mStickDigitalDeadZone = key.getInt("Input: Stick digital dead zone", joyxforms.mStickDigitalDeadZone);
		joyxforms.mStickAnalogPower = VDGetIntAsFloat(key.getInt("Input: Stick analog power", VDGetFloatAsInt(joyxforms.mStickAnalogPower)));
		joyxforms.mTriggerAnalogDeadZone = key.getInt("Input: Trigger analog dead zone", joyxforms.mTriggerAnalogDeadZone);
		joyxforms.mTriggerDigitalDeadZone = key.getInt("Input: Trigger digital dead zone", joyxforms.mTriggerDigitalDeadZone);
		joyxforms.mTriggerAnalogPower = VDGetIntAsFloat(key.getInt("Input: Trigger analog power", VDGetFloatAsInt(joyxforms.mTriggerAnalogPower)));
		g_sim.GetJoystickManager()->SetTransforms(joyxforms);
	}

	// input map selections
	if (write) { 
		g_sim.GetInputManager()->SaveSelections(key);
	} else {
		auto defaultController = kATInputControllerType_None;

		if (g_sim.GetHardwareMode() == kATHardwareMode_5200)
			defaultController = kATInputControllerType_5200Controller;

		g_sim.GetInputManager()->LoadSelections(key, defaultController);
	}
}

void ATSettingsExchangeInputMaps(bool write, VDRegistryKey& key) {
	if (write) { 
		VDRegistryKey imapKey(key, "Input maps", true);
		g_sim.GetInputManager()->SaveMaps(imapKey);
	} else {
		VDRegistryKey imapKey(key, "Input maps", false);
		g_sim.GetInputManager()->LoadMaps(imapKey);
	}
}

void ATSettingsExchangeHardware(bool write, VDRegistryKey& key) {
	ATGTIAEmulator& gtia = g_sim.GetGTIA();
	ATCPUEmulator& cpu = g_sim.GetCPU();

	if (write) {
		key.setInt("Hardware mode", g_sim.GetHardwareMode());

		struct {
			bool mbPAL;
			bool mbSECAM;
			bool mbMixed;
		} kVSModes[]={
			{ false, false, false },	// NTSC
			{ true,  false, false },	// PAL
			{ true,  true,  false },	// SECAM
			{ false, false, true },		// PAL60
			{ true,  false, true },		// NTSC50
		};

		VDASSERTCT(vdcountof(kVSModes) == kATVideoStandardCount);

		const auto& vsmode = kVSModes[g_sim.GetVideoStandard()];
		key.setBool("PAL mode", vsmode.mbPAL);
		key.setBool("SECAM mode", vsmode.mbSECAM);
		key.setBool("Mixed video mode", vsmode.mbMixed);

		key.setInt("Memory mode", g_sim.GetMemoryMode());
		key.setInt("Memory: Axlon size", g_sim.GetAxlonMemoryMode());
		key.setBool("Memory: Axlon aliasing", g_sim.GetAxlonAliasingEnabled());
		key.setInt("Memory: High banks", g_sim.GetHighMemoryBanks());
		key.setBool("Memory: MapRAM", g_sim.IsMapRAMEnabled());
		key.setBool("Memory: Ultimate1MB", g_sim.IsUltimate1MBEnabled());
		key.setBool("Memory: Floating IO bus", g_sim.IsFloatingIoBusEnabled());
		key.setBool("Memory: Preserve extRAM", g_sim.IsPreserveExtRAMEnabled());
		key.setInt("Memory: Cold start pattern", g_sim.GetMemoryClearMode());

		key.setBool("CPU: Allow NMI blocking", cpu.IsNMIBlockingEnabled());
		key.setBool("CPU: Allow illegal instructions", cpu.AreIllegalInsnsEnabled());
		key.setInt("CPU: Chip type", g_sim.GetCPUMode());
		key.setInt("CPU: Clock multiplier", g_sim.GetCPUSubCycles());

		key.setBool("CPU: Shadow ROMs", g_sim.GetShadowROMEnabled());
		key.setBool("CPU: Shadow cartridges", g_sim.GetShadowCartridgeEnabled());

		key.setBool("GTIA: CTIA mode", gtia.IsCTIAMode());

		key.setBool("Audio: Dual POKEYs enabled", g_sim.IsDualPokeysEnabled());
	} else {
		const ATHardwareMode hwmode = (ATHardwareMode)key.getEnumInt("Hardware mode", kATHardwareModeCount, g_sim.GetHardwareMode());
		g_sim.SetHardwareMode(hwmode);

		auto vs =  g_sim.GetVideoStandard();
		const bool isPAL = key.getBool("PAL mode", false);
		const bool isSECAM = key.getBool("SECAM mode", false);
		const bool isMixed = key.getBool("Mixed video mode", false);

		if (isSECAM)
			g_sim.SetVideoStandard(kATVideoStandard_SECAM);
		else if (isPAL) {
			if (isMixed)
				g_sim.SetVideoStandard(kATVideoStandard_NTSC50);
			else
				g_sim.SetVideoStandard(kATVideoStandard_PAL);
		} else {
			if (isMixed)
				g_sim.SetVideoStandard(kATVideoStandard_PAL60);
			else
				g_sim.SetVideoStandard(kATVideoStandard_NTSC);
		}

		ATMemoryMode defaultMemoryMode = kATMemoryMode_320K;
		switch(hwmode) {
			case kATHardwareMode_800:
				defaultMemoryMode = kATMemoryMode_48K;
				break;

			case kATHardwareMode_5200:
				defaultMemoryMode = kATMemoryMode_16K;
				break;
		}

		g_sim.SetMemoryMode((ATMemoryMode)key.getEnumInt("Memory mode", kATMemoryModeCount, defaultMemoryMode));
		g_sim.SetAxlonMemoryMode(key.getInt("Memory: Axlon size", 0));
		g_sim.SetAxlonAliasingEnabled(key.getBool("Memory: Axlon aliasing", false));
		g_sim.SetHighMemoryBanks(key.getInt("Memory: High banks", 0));
		g_sim.SetMapRAMEnabled(key.getBool("Memory: MapRAM", false));
		g_sim.SetUltimate1MBEnabled(key.getBool("Memory: Ultimate1MB", false));
		g_sim.SetFloatingIoBusEnabled(key.getBool("Memory: Floating IO bus", false));
		g_sim.SetPreserveExtRAMEnabled(key.getBool("Memory: Preserve extRAM", false));
		g_sim.SetMemoryClearMode((ATMemoryClearMode)key.getEnumInt("Memory: Cold start pattern", kATMemoryClearModeCount, kATMemoryClearMode_DRAM1));

		cpu.SetNMIBlockingEnabled(key.getBool("CPU: Allow NMI blocking", false));
		cpu.SetIllegalInsnsEnabled(key.getBool("CPU: Allow illegal instructions", true));

		g_sim.SetShadowROMEnabled(key.getBool("CPU: Shadow ROMs", true));
		g_sim.SetShadowCartridgeEnabled(key.getBool("CPU: Shadow cartridges", false));

		ATCPUMode cpuMode = (ATCPUMode)key.getEnumInt("CPU: Chip type", kATCPUModeCount, kATCPUMode_6502);
		uint32 cpuMultiplier = key.getInt("CPU: Clock multiplier", 1);
		g_sim.SetCPUMode(cpuMode, cpuMultiplier);

		gtia.SetCTIAMode(key.getBool("GTIA: CTIA mode", false));

		g_sim.SetDualPokeysEnabled(key.getBool("Audio: Dual POKEYs enabled", false));
	}
}

void ATSettingsExchangeFirmware(bool write, VDRegistryKey& key) {
	ATFirmwareManager& fwmgr = *g_sim.GetFirmwareManager();

	if (write) {
		const uint64 kernelId = g_sim.GetKernelId();

		key.setString("Kernel path", fwmgr.GetFirmwareRefString(kernelId).c_str());

		ATFirmwareInfo kernelFwInfo;
		kernelFwInfo.mType = kATFirmwareType_Unknown;
		g_sim.GetFirmwareManager()->GetFirmwareInfo(kernelId, kernelFwInfo);
		key.setString("Kernel type", ATGetFirmwareTypeName(kernelFwInfo.mType));

		key.setString("Basic path", fwmgr.GetFirmwareRefString(g_sim.GetBasicId()).c_str());
	} else {
		VDStringW kernelPath;
		key.getString("Kernel path", kernelPath);

		ATFirmwareManager& fwmgr = *g_sim.GetFirmwareManager();
		uint64 kernelId = 0;
		if (!kernelPath.empty()) {
			kernelId = fwmgr.GetFirmwareByRefString(kernelPath.c_str());

			ATFirmwareInfo info;
			if (!kernelId) {
				VDStringA kernelTypeName;
				key.getString("Kernel type", kernelTypeName);

				ATFirmwareType type = ATGetFirmwareTypeFromName(kernelTypeName.c_str());
				kernelId = g_sim.GetFirmwareManager()->GetFirmwareOfType(type, false);
				if (!kernelId)
					kernelId = kATFirmwareId_NoKernel;
			}
		}

		g_sim.SetKernel(kernelId);

		VDStringW basicPath;
		key.getString("Basic path", basicPath);
		g_sim.SetBasic(fwmgr.GetFirmwareByRefString(basicPath.c_str()));
	}
}

void ATSettingsExchangeAcceleration(bool write, VDRegistryKey& key) {
	ATDiskEmulator& disk = g_sim.GetDiskDrive(0);

	if (write) {
		key.setBool("Cassette: SIO patch enabled", g_sim.IsCassetteSIOPatchEnabled());
		key.setBool("Cassette: Auto-boot enabled", g_sim.IsCassetteAutoBootEnabled());
		key.setBool("Cassette: Auto BASIC boot enabled", g_sim.IsCassetteAutoBasicBootEnabled());
		key.setBool("Cassette: Auto-rewind enabled", g_sim.IsCassetteAutoRewindEnabled());
		key.setBool("Cassette: Randomize start position", g_sim.IsCassetteRandomizedStartEnabled());
		key.setString("Cassette: Turbo mode", ATEnumToString(g_sim.GetCassette().GetTurboMode()));
		key.setString("Cassette: Polarity mode", ATEnumToString(g_sim.GetCassette().GetPolarityMode()));
		key.setString("Cassette: Direct sense mode", ATEnumToString(g_sim.GetCassette().GetDirectSenseMode()));

		key.setBool("Kernel: Floating-point patch enabled", g_sim.IsFPPatchEnabled());
		key.setBool("Kernel: Fast boot enabled", g_sim.IsFastBootEnabled());

		key.setBool("Disk: SIO patch enabled", g_sim.IsDiskSIOPatchEnabled());
		key.setBool("Disk: SIO override detection enabled", g_sim.IsDiskSIOOverrideDetectEnabled());
		key.setBool("Disk: Burst transfers enabled", g_sim.GetDiskBurstTransfersEnabled());

		key.setInt("Video: Enhanced text mode", ATUIGetEnhancedTextMode());

		key.setBool("Devices: CIO burst transfers enabled", g_sim.GetDeviceCIOBurstTransfersEnabled());
		key.setBool("Devices: SIO burst transfers enabled", g_sim.GetDeviceSIOBurstTransfersEnabled());

		for(char c : { 'H', 'P', 'R', 'T' }) {
			VDStringA s;

			s.sprintf("Devices: CIO %c: patch enabled", c);
			key.setBool(s.c_str(), g_sim.GetCIOPatchEnabled(c));
		}

		key.setBool("Devices: SIO patch enabled", g_sim.GetDeviceSIOPatchEnabled());

		key.setBool("Devices: Accelerate with SIO patch", g_sim.IsSIOPatchEnabled());
		key.setBool("Devices: Accelerate with PBI patch", g_sim.IsPBIPatchEnabled());
	} else {
		g_sim.SetCassetteSIOPatchEnabled(key.getBool("Cassette: SIO patch enabled", g_sim.IsCassetteSIOPatchEnabled()));
		g_sim.SetCassetteAutoBootEnabled(key.getBool("Cassette: Auto-boot enabled", g_sim.IsCassetteAutoBootEnabled()));
		g_sim.SetCassetteAutoBasicBootEnabled(key.getBool("Cassette: Auto BASIC boot enabled", g_sim.IsCassetteAutoBasicBootEnabled()));
		g_sim.SetCassetteAutoRewindEnabled(key.getBool("Cassette: Auto-rewind enabled", g_sim.IsCassetteAutoRewindEnabled()));
		g_sim.SetCassetteRandomizedStartEnabled(key.getBool("Cassette: Randomize start position", g_sim.IsCassetteRandomizedStartEnabled()));

		auto& cassette = g_sim.GetCassette();

		VDStringA turboMode;
		key.getString("Cassette: Turbo mode", turboMode);
		cassette.SetTurboMode(ATParseEnum<ATCassetteTurboMode>(turboMode).mValue);

		VDStringA polarityMode;
		key.getString("Cassette: Polarity mode", polarityMode);
		cassette.SetPolarityMode(ATParseEnum<ATCassettePolarityMode>(polarityMode).mValue);

		VDStringA directSenseMode;
		key.getString("Cassette: Direct sense mode", directSenseMode);
		g_sim.GetCassette().SetDirectSenseMode(ATParseEnum<ATCassetteDirectSenseMode>(directSenseMode).mValue);

		g_sim.SetFPPatchEnabled(key.getBool("Kernel: Floating-point patch enabled", g_sim.IsFPPatchEnabled()));
		g_sim.SetFastBootEnabled(key.getBool("Kernel: Fast boot enabled", g_sim.IsFastBootEnabled()));

		g_sim.SetDiskSIOPatchEnabled(key.getBool("Disk: SIO patch enabled", g_sim.IsDiskSIOPatchEnabled()));
		g_sim.SetDiskSIOOverrideDetectEnabled(key.getBool("Disk: SIO override detection enabled", g_sim.IsDiskSIOOverrideDetectEnabled()));
		g_sim.SetDiskBurstTransfersEnabled(key.getBool("Disk: Burst transfers enabled", g_sim.GetDiskBurstTransfersEnabled()));

		ATUISetEnhancedTextMode((ATUIEnhancedTextMode)key.getEnumInt("Video: Enhanced text mode", kATUIEnhancedTextModeCount, ATUIGetEnhancedTextMode()));

		g_sim.SetDeviceCIOBurstTransfersEnabled(key.getBool("Devices: CIO burst transfers enabled", g_sim.GetDeviceCIOBurstTransfersEnabled()));
		g_sim.SetDeviceSIOBurstTransfersEnabled(key.getBool("Devices: SIO burst transfers enabled", g_sim.GetDeviceSIOBurstTransfersEnabled()));

		for(char c : { 'H', 'P', 'R', 'T' }) {
			VDStringA s;

			s.sprintf("Devices: CIO %c: patch enabled", c);
			g_sim.SetCIOPatchEnabled(c, key.getBool(s.c_str(), g_sim.GetCIOPatchEnabled(c)));
		}

		g_sim.SetDeviceSIOPatchEnabled(key.getBool("Devices: SIO patch enabled", g_sim.GetDeviceSIOPatchEnabled()));

		g_sim.SetSIOPatchEnabled(key.getBool("Devices: Accelerate with SIO patch", true));
		g_sim.SetPBIPatchEnabled(key.getBool("Devices: Accelerate with PBI patch", false));
	}
}

void LoadColorParams(VDRegistryKey& key, ATNamedColorParams& colpa) {
	colpa.mPresetTag.clear();
	const bool presetNameValid = key.getString("Preset Tag", colpa.mPresetTag);

	sint32 presetIndex = colpa.mPresetTag.empty() ? -1 : ATGetColorPresetIndexByTag(colpa.mPresetTag.c_str());

	if (presetIndex >= 0) {
		static_cast<ATColorParams&>(colpa) = ATGetColorPresetByIndex(presetIndex);
	} else {
		colpa.mHueStart = ATSettingsGetFloat(key, "Hue Start", colpa.mHueStart);
		colpa.mHueRange = ATSettingsGetFloat(key, "Hue Range", colpa.mHueRange);
		colpa.mBrightness = ATSettingsGetFloat(key, "Brightness", colpa.mBrightness);
		colpa.mContrast = ATSettingsGetFloat(key, "Contrast", colpa.mContrast);
		colpa.mSaturation = ATSettingsGetFloat(key, "Saturation", colpa.mSaturation);
		colpa.mGammaCorrect = ATSettingsGetFloat(key, "Gamma Correction2", colpa.mGammaCorrect);

		// Artifact hue is stored negated for compatibility reasons.
		colpa.mArtifactHue = -ATSettingsGetFloat(key, "Artifact Hue", -colpa.mArtifactHue);

		colpa.mArtifactSat = ATSettingsGetFloat(key, "Artifact Saturation", colpa.mArtifactSat);
		colpa.mArtifactSharpness = ATSettingsGetFloat(key, "Artifact Sharpness", colpa.mArtifactSharpness);

		colpa.mIntensityScale = ATSettingsGetFloat(key, "Intensity Scale", colpa.mIntensityScale);
		colpa.mRedShift = ATSettingsGetFloat(key, "Red Shift", colpa.mRedShift);
		colpa.mRedScale = ATSettingsGetFloat(key, "Red Scale", colpa.mRedScale);
		colpa.mGrnShift = ATSettingsGetFloat(key, "Green Shift", colpa.mGrnShift);
		colpa.mGrnScale = ATSettingsGetFloat(key, "Green Scale", colpa.mGrnScale);
		colpa.mBluShift = ATSettingsGetFloat(key, "Blue Shift", colpa.mBluShift);
		colpa.mBluScale = ATSettingsGetFloat(key, "Blue Scale", colpa.mBluScale);

		colpa.mbUsePALQuirks = key.getBool("PAL quirks", colpa.mbUsePALQuirks);
		colpa.mLumaRampMode = (ATLumaRampMode)key.getEnumInt("Luma ramp mode", (int)kATLumaRampModeCount, (int)colpa.mLumaRampMode);

		VDStringA s;
		key.getString("Color matching mode", s);
		colpa.mColorMatchingMode = ATParseEnum<ATColorMatchingMode>(s).mValue;

		if (!presetNameValid) {
			for(uint32 i=0, n = ATGetColorPresetCount(); i<n; ++i) {
				if (ATGetColorPresetByIndex(i).IsSimilar(colpa)) {
					colpa.mPresetTag = ATGetColorPresetTagByIndex(i);
					break;
				}
			}
		}
	}
}

void SaveColorParams(VDRegistryKey& key, const ATNamedColorParams& colpa) {
	key.setString("Preset Tag", colpa.mPresetTag.c_str());
	key.setInt("Hue Start", VDGetFloatAsInt(colpa.mHueStart));
	key.setInt("Hue Range", VDGetFloatAsInt(colpa.mHueRange));
	key.setInt("Brightness", VDGetFloatAsInt(colpa.mBrightness));
	key.setInt("Contrast", VDGetFloatAsInt(colpa.mContrast));
	key.setInt("Saturation", VDGetFloatAsInt(colpa.mSaturation));
	key.setInt("Gamma Correction2", VDGetFloatAsInt(colpa.mGammaCorrect));

	// Artifact hue is stored negated for compatibility reasons.
	key.setInt("Artifact Hue", VDGetFloatAsInt(-colpa.mArtifactHue));

	key.setInt("Artifact Saturation", VDGetFloatAsInt(colpa.mArtifactSat));
	key.setInt("Artifact Sharpness", VDGetFloatAsInt(colpa.mArtifactSharpness));

	key.setInt("Intensity Scale", VDGetFloatAsInt(colpa.mIntensityScale));
	key.setInt("Red Shift", VDGetFloatAsInt(colpa.mRedShift));
	key.setInt("Red Scale", VDGetFloatAsInt(colpa.mRedScale));
	key.setInt("Green Shift", VDGetFloatAsInt(colpa.mGrnShift));
	key.setInt("Green Scale", VDGetFloatAsInt(colpa.mGrnScale));
	key.setInt("Blue Shift", VDGetFloatAsInt(colpa.mBluShift));
	key.setInt("Blue Scale", VDGetFloatAsInt(colpa.mBluScale));

	key.setBool("PAL quirks", colpa.mbUsePALQuirks);
	key.setInt("Luma ramp mode", colpa.mLumaRampMode);

	key.setString("Color matching mode", ATEnumToString(colpa.mColorMatchingMode));
}

void ATSettingsExchangeColor(bool write, VDRegistryKey& key) {
	ATGTIAEmulator& gtia = g_sim.GetGTIA();

	if (write) {
		ATColorSettings cols(gtia.GetColorSettings());

		VDRegistryKey colKey(key, "Colors", true);
		VDRegistryKey ntscColKey(colKey, "NTSC");
		SaveColorParams(ntscColKey, cols.mNTSCParams);

		VDRegistryKey palColKey(colKey, "PAL");
		SaveColorParams(palColKey, cols.mPALParams);
		colKey.setBool("Use separate color profiles", cols.mbUsePALParams);
	} else {
		VDRegistryKey colKey(key, "Colors", false);
		ATColorSettings cols(gtia.GetColorSettings());

		VDRegistryKey ntscRegKey(colKey, "NTSC", false);
		LoadColorParams(ntscRegKey, cols.mNTSCParams);

		VDRegistryKey palRegKey(colKey, "PAL", false);
		LoadColorParams(palRegKey, cols.mPALParams);
		cols.mbUsePALParams = colKey.getBool("Use separate color profiles", cols.mbUsePALParams);
		gtia.SetColorSettings(cols);
	}
}

void ATSettingsExchangeSound(bool write, VDRegistryKey& key) {
	ATDiskInterface& diskIf = g_sim.GetDiskInterface(0);
	IATAudioOutput *audioOut = g_sim.GetAudioOutput();
	ATPokeyEmulator& pokey = g_sim.GetPokey();

	if (write) {
		key.setInt("Audio: Volume", VDGetFloatAsInt(audioOut->GetVolume()));
		key.setBool("Audio: Mute", audioOut->GetMute());
		key.setInt("Audio: Latency", audioOut->GetLatency());
		key.setInt("Audio: Extra buffer", audioOut->GetExtraBuffer());
		key.setInt("Audio: Api", audioOut->GetApi());
		key.setBool("Audio: Show debug info", audioOut->GetStatusRenderer() != NULL);

		key.setBool("Audio: Monitor enabled", g_sim.IsAudioMonitorEnabled());

		key.setBool("Audio: Non-linear mixing", pokey.IsNonlinearMixingEnabled());
		key.setBool("Audio: Serial noise enabled", pokey.IsSerialNoiseEnabled());

		key.setBool("Cassette: Load data as audio", g_sim.GetCassette().IsLoadDataAsAudioEnabled());

		key.setBool("Disk: Drive sounds", diskIf.AreDriveSoundsEnabled());

		key.setInt("Audio: Drive sounds volume", VDGetFloatAsInt(g_sim.GetAudioOutput()->GetMixLevel(kATAudioMix_Drive)));
		key.setInt("Audio: Covox volume", VDGetFloatAsInt(g_sim.GetAudioOutput()->GetMixLevel(kATAudioMix_Covox)));
	} else {
		float volume = VDGetIntAsFloat(key.getInt("Audio: Volume", VDGetFloatAsInt(0.5f)));
		if (!(volume >= 0.0f && volume <= 1.0f))
			volume = 0.5f;
		audioOut->SetVolume(volume);
		audioOut->SetMute(key.getBool("Audio: Mute", false));

		audioOut->SetLatency(key.getInt("Audio: Latency", 80));
		audioOut->SetExtraBuffer(key.getInt("Audio: Extra buffer", 100));
		audioOut->SetApi((ATAudioApi)key.getEnumInt("Audio: Api", kATAudioApiCount, kATAudioApi_WaveOut));

		if (key.getBool("Audio: Show debug info", false))
			audioOut->SetStatusRenderer(g_sim.GetUIRenderer());
		else
			audioOut->SetStatusRenderer(NULL);

		g_sim.SetAudioMonitorEnabled(key.getBool("Audio: Monitor enabled", false));

		pokey.SetNonlinearMixingEnabled(key.getBool("Audio: Non-linear mixing", pokey.IsNonlinearMixingEnabled()));
		pokey.SetSerialNoiseEnabled(key.getBool("Audio: Serial noise enabled", true));

		g_sim.GetCassette().SetLoadDataAsAudioEnable(key.getBool("Cassette: Load data as audio", g_sim.GetCassette().IsLoadDataAsAudioEnabled()));

		bool enableDriveSounds = key.getBool("Disk: Drive sounds", diskIf.AreDriveSoundsEnabled());

		for(int i=0; i<15; ++i) {
			g_sim.GetDiskInterface(i).SetDriveSoundsEnabled(enableDriveSounds);
		}

		audioOut->SetMixLevel(kATAudioMix_Drive, VDGetIntAsFloat(key.getInt("Audio: Drive sounds volume", VDGetFloatAsInt(0.8f))));
		audioOut->SetMixLevel(kATAudioMix_Covox, VDGetIntAsFloat(key.getInt("Audio: Covox volume", VDGetFloatAsInt(1.0f))));
	}
}

void ATSettingsExchangeEnvironment(bool write, VDRegistryKey& key) {
	ATPokeyEmulator& pokey = g_sim.GetPokey();

	if (write) {
		key.setBool("Pause when inactive", ATUIGetPauseWhenInactive());
		key.setInt("Auto-reset flags", ATUIGetResetFlags());
		key.setInt("Auto-reset flag mask", kATUIResetFlag_All);

		const char *wctemp = ATUIGetWindowCaptionTemplate();

		if (wctemp && *wctemp)
			key.setString("Window caption template", VDTextU8ToW(VDStringSpanA(wctemp)).c_str());
		else
			key.removeValue("Window caption template");
	} else {
		ATUISetPauseWhenInactive(key.getBool("Pause when inactive", ATUIGetPauseWhenInactive()));

		const uint32 resetFlags = key.getInt("Auto-reset flags");
		const uint32 resetFlagMask = key.getInt("Auto-reset flag mask");
		ATUISetResetFlags(kATUIResetFlag_Default ^ ((kATUIResetFlag_Default ^ resetFlags) & resetFlagMask));

		VDStringW s;
		key.getString("Window caption template", s);
		ATUISetWindowCaptionTemplate(VDTextWToU8(s).c_str());
	}
}

void ATSettingsExchangeBoot(bool write, VDRegistryKey& key) {
	ATPokeyEmulator& pokey = g_sim.GetPokey();

	if (write) {
		key.setInt("Unload on boot types", ATUIGetBootUnloadStorageMask());
		key.setInt("Unload on boot mask", kATStorageTypeMask_All);

		key.setString("ExeLoader: Mode", ATEnumToString(g_sim.GetHLEProgramLoadMode()));

	} else {
		const uint32 bootUnloadFlags = key.getInt("Unload on boot types");
		const uint32 bootUnloadFlagMask = key.getInt("Unload on boot mask");
		ATUISetBootUnloadStorageMask(kATStorageTypeMask_All ^ ((kATStorageTypeMask_All ^ bootUnloadFlags) & bootUnloadFlagMask));

		VDStringA loadMode;
		key.getString("ExeLoader: Mode", loadMode);
		g_sim.SetHLEProgramLoadMode(ATParseEnum<ATHLEProgramLoadMode>(loadMode).mValue);
	}
}

void ATSettingsExchangeDebugging(bool write, VDRegistryKey& key) {
	ATCPUEmulator& cpu = g_sim.GetCPU();

	if (write) {
		key.setBool("Memory: Randomize on EXE load", g_sim.IsRandomFillEXEEnabled());
		key.setBool("CPU: History enabled", cpu.IsHistoryEnabled());
		key.setBool("CPU: Pathfinding enabled", cpu.IsPathfindingEnabled());
		key.setBool("CPU: Stop on BRK", cpu.GetStopOnBRK());

		IATDebugger *dbg = ATGetDebugger();
		if (dbg) {
			key.setBool("Debugger: Break on EXE run address", dbg->IsBreakOnEXERunAddrEnabled());
			key.setString("Debugger: Pre-start symbol load mode", ATEnumToString(dbg->GetSymbolLoadMode(false)));
			key.setString("Debugger: Post-start symbol load mode", ATEnumToString(dbg->GetSymbolLoadMode(true)));
			key.setString("Debugger: Script auto-load mode", ATEnumToString(dbg->GetScriptAutoLoadMode()));
		}
	} else {
		g_sim.SetRandomFillEXEEnabled(key.getBool("Memory: Randomize on EXE load", g_sim.IsRandomFillEXEEnabled()));
		cpu.SetHistoryEnabled(key.getBool("CPU: History enabled", cpu.IsHistoryEnabled()));
		cpu.SetPathfindingEnabled(key.getBool("CPU: Pathfinding enabled", cpu.IsPathfindingEnabled()));
		cpu.SetStopOnBRK(key.getBool("CPU: Stop on BRK", cpu.GetStopOnBRK()));

		ATSyncCPUHistoryState();

		IATDebugger *dbg = ATGetDebugger();
		if (dbg) {
			dbg->SetBreakOnEXERunAddrEnabled(key.getBool("Debugger: Break on EXE run address", dbg->IsBreakOnEXERunAddrEnabled()));

			VDStringA s;
			key.getString("Debugger: Pre-start symbol load mode", s);
			dbg->SetSymbolLoadMode(false, ATParseEnum<ATDebuggerSymbolLoadMode>(s).mValue);

			s.clear();
			key.getString("Debugger: Post-start symbol load mode", s);
			dbg->SetSymbolLoadMode(true, ATParseEnum<ATDebuggerSymbolLoadMode>(s).mValue);

			s.clear();
			key.getString("Debugger: Script auto-load mode", s);
			dbg->SetScriptAutoLoadMode(ATParseEnum<ATDebuggerScriptAutoLoadMode>(s).mValue);
		}
	}
}

void ATSettingsExchangeDevices(bool write, VDRegistryKey& key) {
	auto *dm = g_sim.GetDeviceManager();
	if (write) {
		VDStringW devStr;
		dm->SerializeDevice(nullptr, devStr);
		key.setString("Devices", devStr.c_str());

		ATDiskEmulator& disk = g_sim.GetDiskDrive(0);
		key.setInt("Disk: Emulation mode", disk.GetEmulationMode());
		key.setBool("Disk: Accurate sector timing", g_sim.GetDiskInterface(0).IsAccurateSectorTimingEnabled());
	} else {
		VDStringW devStr;
		key.getString("Devices", devStr);
		dm->RemoveAllDevices(false);
		dm->DeserializeDevices(nullptr, nullptr, devStr.c_str());

		bool accurateSectorTiming = key.getBool("Disk: Accurate sector timing", g_sim.GetDiskInterface(0).IsAccurateSectorTimingEnabled());
		for(int i=0; i<15; ++i) {
			ATDiskEmulator& disk = g_sim.GetDiskDrive(i);
			disk.SetEmulationMode((ATDiskEmulationMode)key.getEnumInt("Disk: Emulation mode", kATDiskEmulationModeCount, disk.GetEmulationMode()));

			ATDiskInterface& diskIf = g_sim.GetDiskInterface(i);
			diskIf.SetAccurateSectorTimingEnabled(accurateSectorTiming);
		}
	}
}

void ATSettingsExchangeStartupConfig(bool write, VDRegistryKey& key) {
	if (write) {
		key.setBool("BASIC enabled", g_sim.IsBASICEnabled());
		key.setBool("Console: Keyboard present", g_sim.IsKeyboardPresent());
		key.setBool("Console: Force self test", g_sim.IsForcedSelfTest());
		key.setBool("Console: Cartridge switch", g_sim.GetCartridgeSwitch());
		key.setInt("System: Power-On Delay", g_sim.GetPowerOnDelay());
	} else {
		g_sim.SetBASICEnabled(key.getBool("BASIC enabled", g_sim.IsBASICEnabled()));
		g_sim.SetKeyboardPresent(key.getBool("Console: Keyboard present", g_sim.IsKeyboardPresent()));
		g_sim.SetForcedSelfTest(key.getBool("Console: Force self test", g_sim.IsForcedSelfTest()));
		g_sim.SetCartridgeSwitch(key.getBool("Console: Cartridge switch", g_sim.GetCartridgeSwitch()));
		g_sim.SetPowerOnDelay(key.getInt("System: Power-On Delay", g_sim.GetPowerOnDelay()));
	}
}

///////////////////////////////////////////////////////////////////////////

void SaveMountedImages(VDRegistryKey& rootKey) {
	VDRegistryKey key(rootKey, "Mounted Images", true);
	VDStringA name;
	VDStringW imagestr;

	for(int i=0; i<15; ++i) {
		ATDiskEmulator& disk = g_sim.GetDiskDrive(i);
		ATDiskInterface& diskIf = g_sim.GetDiskInterface(i);
		name.sprintf("Disk %d", i);


		if (disk.IsEnabled() || diskIf.GetClientCount() > 1) {
			const wchar_t *path = diskIf.GetPath();
			const auto writeMode = diskIf.GetWriteMode();
			
			wchar_t c = 'R';

			if (writeMode & kATMediaWriteMode_AllowWrite) {
				if (writeMode & kATMediaWriteMode_AutoFlush)
					c = 'W';
				else
					c = 'V';
			}

			if (path && diskIf.IsDiskBacked())
				imagestr.sprintf(L"%c%ls", c, path);
			else
				imagestr.sprintf(L"%c", c);

			key.setString(name.c_str(), imagestr.c_str());
		} else {
			key.removeValue(name.c_str());
		}
	}

	for(uint32 cartUnit = 0; cartUnit < 2; ++cartUnit) {
		ATCartridgeEmulator *cart = g_sim.GetCartridge(cartUnit);
		const wchar_t *cartPath = NULL;
		int cartMode = 0;
		if (cart) {
			cartPath = cart->GetPath();
			cartMode = cart->GetMode();
		}

		VDStringA keyName;
		VDStringA keyNameMode;
		keyName.sprintf("Cartridge %u", cartUnit);
		keyNameMode.sprintf("Cartridge %u Mode", cartUnit);

		if (cartPath && *cartPath) {
			key.setString(keyName.c_str(), cartPath);
			key.setInt(keyNameMode.c_str(), cartMode);
		} else if (cartMode == kATCartridgeMode_SuperCharger3D) {
			key.setString(keyName.c_str(), "special:sc3d");
			key.removeValue(keyNameMode.c_str());
		} else {
			key.removeValue(keyName.c_str());
			key.removeValue(keyNameMode.c_str());
		}
	}

	auto& cas = g_sim.GetCassette();
	if (cas.IsImagePersistent() && *cas.GetPath()) {
		key.setString("Cassette", cas.GetPath());
	} else {
		key.removeValue("Cassette");
	}

	key.removeValue("IDE: Hardware mode");
	key.removeValue("IDE: Hardware mode 2");
	key.removeValue("IDE: Image path");
	key.removeValue("IDE: Write enabled");
	key.removeValue("IDE: Image cylinders");
	key.removeValue("IDE: Image heads");
	key.removeValue("IDE: Image sectors per track");
}

void LoadMountedImages(VDRegistryKey& rootKey) {
	VDRegistryKey key(rootKey, "Mounted Images", false);
	VDStringA name;
	VDStringW imagestr;

	for(int i=0; i<15; ++i) {
		name.sprintf("Disk %d", i);

		ATDiskEmulator& disk = g_sim.GetDiskDrive(i);
		ATDiskInterface& diskIf = g_sim.GetDiskInterface(i);
		if (key.getString(name.c_str(), imagestr) && !imagestr.empty()) {

			wchar_t mode = imagestr[0];
			ATMediaWriteMode writeMode;

			if (mode == L'V') {
				writeMode = kATMediaWriteMode_VRW;
			} else if (mode == L'R') {
				writeMode = kATMediaWriteMode_RO;
			} else if (mode == L'W') {
				writeMode = kATMediaWriteMode_RW;
			} else if (mode == L'S') {
				writeMode = kATMediaWriteMode_VRWSafe;
			} else
				continue;

			if (imagestr.size() > 1) {
				try {
					const wchar_t *star = wcschr(imagestr.c_str(), L'*');
					if (star) {
						diskIf.LoadDisk(imagestr.c_str() + 1);

						if (diskIf.GetClientCount() < 2)
							disk.SetEnabled(true);
					} else {
						ATImageLoadContext ctx;
						ctx.mLoadType = kATImageType_Disk;
						ctx.mLoadIndex = i;

						g_sim.Load(imagestr.c_str() + 1, writeMode, &ctx);
					}
				} catch(const MyError&) {
				}
			} else {
				diskIf.SetWriteMode(writeMode);

				if (diskIf.GetClientCount() < 2)
					disk.SetEnabled(true);
			}
		} else {
			diskIf.UnloadDisk();
			disk.SetEnabled(false);
		}
	}

	const bool is5200 = g_sim.GetHardwareMode() == kATHardwareMode_5200;

	for(uint32 cartUnit = 0; cartUnit < 2; ++cartUnit) {
		VDStringA keyName;
		VDStringA keyNameMode;
		keyName.sprintf("Cartridge %u", cartUnit);
		keyNameMode.sprintf("Cartridge %u Mode", cartUnit);

		const bool need5200Default = is5200 && !cartUnit;

		if (key.getString(keyName.c_str(), imagestr)) {
			int cartMode = key.getInt(keyNameMode.c_str(), 0);

			try {
				ATCartLoadContext cartLoadCtx = {};
				cartLoadCtx.mbReturnOnUnknownMapper = false;
				cartLoadCtx.mCartMapper = cartMode;

				if (imagestr == L"special:sc3d")
					g_sim.LoadNewCartridge(kATCartridgeMode_SuperCharger3D);
				else if (imagestr == L"special:basic")
					g_sim.LoadCartridgeBASIC();
				else
					g_sim.LoadCartridge(cartUnit, imagestr.c_str(), &cartLoadCtx);
			} catch(const MyError&) {
				if (need5200Default)
					g_sim.LoadCartridge5200Default();
			}
		} else {
			if (need5200Default)
				g_sim.LoadCartridge5200Default();
			else
				g_sim.UnloadCartridge(cartUnit);
		}
	}

	try {
		auto& cas = g_sim.GetCassette();
		VDStringW casPath;
		key.getString("Cassette", casPath);

		if (!casPath.empty())
			cas.Load(casPath.c_str());
		else
			cas.Unload();
	} catch(const MyError&) {
	}
}

void ATSettingsExchangeMountedImages(bool write, VDRegistryKey& key) {
	if (write) {
		SaveMountedImages(key);
	} else {
		LoadMountedImages(key);
	}
}

///////////////////////////////////////////////////////////////////////////

namespace {
	static const char *kSettingNamesROMImages[]={
		"OS-A",
		"OS-B",
		"XL",
		"XEGS",
		"Other",
		"5200",
		"Basic",
		"Game",
		"KMKJZIDE",
		"KMKJZIDEV2",
		"KMKJZIDEV2_SDX",
		"SIDE_SDX",
		"1200XL",
		"MyIDEII",
		"Ultimate1MB"
	};
}

void LoadBaselineSettings() {
	g_sim.GetDeviceManager()->RemoveAllDevices(false);

	g_sim.SetKernel(0);

	const uint32 profileId = g_ATCurrentProfileId;

	if (profileId == ATGetDefaultProfileId(kATDefaultProfile_XL)) {
		g_sim.SetHardwareMode(kATHardwareMode_800XL);
		g_sim.SetMemoryMode(kATMemoryMode_320K);
	} else if (profileId == ATGetDefaultProfileId(kATDefaultProfile_XEGS)) {
		g_sim.SetHardwareMode(kATHardwareMode_XEGS);
		g_sim.SetMemoryMode(kATMemoryMode_64K);
	} else if (profileId == ATGetDefaultProfileId(kATDefaultProfile_800)) {
		g_sim.SetHardwareMode(kATHardwareMode_800);
		g_sim.SetMemoryMode(kATMemoryMode_320K);
	} else if (profileId == ATGetDefaultProfileId(kATDefaultProfile_1200XL)) {
		g_sim.SetHardwareMode(kATHardwareMode_1200XL);
		g_sim.SetMemoryMode(kATMemoryMode_64K);
	} else if (profileId == ATGetDefaultProfileId(kATDefaultProfile_5200)) {
		g_sim.SetHardwareMode(kATHardwareMode_5200);
		g_sim.SetMemoryMode(kATMemoryMode_16K);

		g_sim.LoadCartridge5200Default();
	} else {
		g_sim.SetHardwareMode(kATHardwareMode_800XL);
		g_sim.SetMemoryMode(kATMemoryMode_320K);
	}

	g_sim.SetBASICEnabled(false);
	g_sim.SetVideoStandard(kATVideoStandard_NTSC);
	g_sim.SetCassetteSIOPatchEnabled(true);
	g_sim.SetCassetteAutoBootEnabled(true);
	g_sim.SetFPPatchEnabled(false);
	g_sim.SetFastBootEnabled(true);
	g_sim.SetDiskSIOPatchEnabled(true);
	g_sim.SetDiskSIOOverrideDetectEnabled(false);
	g_sim.SetSIOPatchEnabled(true);
	g_sim.SetPBIPatchEnabled(false);

	for(int i=0; i<15; ++i) {
		ATDiskInterface& diskIf = g_sim.GetDiskInterface(i);
		diskIf.SetAccurateSectorTimingEnabled(false);
		diskIf.SetDriveSoundsEnabled(false);

		ATDiskEmulator& disk = g_sim.GetDiskDrive(i);
		disk.SetEmulationMode(kATDiskEmulationMode_Generic);
	}

	g_sim.SetDualPokeysEnabled(false);

	ATUISetEnhancedTextMode(kATUIEnhancedTextMode_None);

	g_sim.SetMemoryClearMode(kATMemoryClearMode_DRAM1);
	g_kbdOpts.mbRawKeys = true;
	g_sim.SetKeyboardPresent(true);
	g_sim.SetForcedSelfTest(false);
}

void ATSettingsExchangeFullScreen(bool write, VDRegistryKey& key) {
	if (write) {
		key.setBool("Display: Full screen", ATUIGetFullscreen());
	} else {
		ATSetFullscreen(key.getBool("Display: Full screen"));
	}
}

namespace {
	struct ProfileCategoryHandler {
		ATSettingsCategory mCategory;
		void (*mpHandler)(bool, VDRegistryKey&);
	} kHandlers[] = {
		{ kATSettingsCategory_Hardware,			ATSettingsExchangeHardware },
		{ kATSettingsCategory_Firmware,			ATSettingsExchangeFirmware },
		{ kATSettingsCategory_Acceleration,		ATSettingsExchangeAcceleration },
		{ kATSettingsCategory_Debugging,		ATSettingsExchangeDebugging },
		{ kATSettingsCategory_Devices,			ATSettingsExchangeDevices },
		{ kATSettingsCategory_StartupConfig,	ATSettingsExchangeStartupConfig },
		{ kATSettingsCategory_Environment,		ATSettingsExchangeEnvironment },
		{ kATSettingsCategory_Color,			ATSettingsExchangeColor },
		{ kATSettingsCategory_View,				ATSettingsExchangeView },
		{ kATSettingsCategory_InputMaps,		ATSettingsExchangeInputMaps },	// must be before input to load selections
		{ kATSettingsCategory_Input,			ATSettingsExchangeInput },
		{ kATSettingsCategory_Speed,			ATSettingsExchangeSpeed },
		{ kATSettingsCategory_MountedImages,	ATSettingsExchangeMountedImages },
		{ kATSettingsCategory_FullScreen,		ATSettingsExchangeFullScreen },
		{ kATSettingsCategory_Sound,			ATSettingsExchangeSound },
		{ kATSettingsCategory_Boot,				ATSettingsExchangeBoot },
	};
}

void ATExchangeSettings(bool write, ATSettingsCategory mask) {
	uint32 remainingMask = mask;
	if (!remainingMask)
		return;

	typedef std::pair<uint32, uint32> ProfileMaskPair;
	vdfastvector<ProfileMaskPair> profileMasks;

	vdfastvector<uint32> seenProfiles;

	uint32 profileId = g_ATCurrentProfileId;
	for(;;) {
		if (std::find(seenProfiles.begin(), seenProfiles.end(), profileId) != seenProfiles.end())
			break;

		seenProfiles.push_back(profileId);

		const uint32 prevSavedMask = ATSettingsProfileGetSavedCategoryMask(profileId);
		const uint32 enabledMask = ATSettingsProfileGetCategoryMask(profileId);
		uint32 exchangeMask = enabledMask & remainingMask;

		// If we are reading, only read categories actually saved from intermediate
		// profiles; if we are writing, mark down what actually got saved in case
		// the categories change later. Note that we may only be saving a subset due
		// to shadowing, so we need to merge into the saved mask instead of replacing
		// it.
		if (profileId) {
			if (write) {
				if (exchangeMask & ~prevSavedMask)
					ATSettingsProfileSetSavedCategoryMask(profileId, (ATSettingsCategory)(exchangeMask | prevSavedMask));
			} else {
				exchangeMask &= ATSettingsProfileGetSavedCategoryMask(profileId);
			}
		}

		if (exchangeMask)
			profileMasks.push_back({profileId, exchangeMask});

		remainingMask &= ~exchangeMask;

		if (!profileId || !remainingMask)
			break;

		profileId = ATSettingsProfileGetParent(profileId);
	}

	for(const auto& entry : kHandlers) {
		if (!(mask & entry.mCategory))
			continue;

		for(const auto& profileMask : profileMasks) {
			if (profileMask.second & entry.mCategory) {
				const uint32 profileId = profileMask.first;

				ProfileKey key(profileId, write);

				entry.mpHandler(write, key);
			}
		}
	}
}

void ATLoadSettings(ATSettingsCategory mask) {
	ATExchangeSettings(false, mask);

	ATReloadPortMenus();
	ATUIUpdateSpeedTiming();
}

void ATSaveSettings(ATSettingsCategory mask) {
	if (g_ATProfileTemporary || g_ATProfileBootstrap)
		return;

	ATExchangeSettings(true, mask);
}

uint32 ATSettingsGetCurrentProfileId() {
	return g_ATCurrentProfileId;
}

bool ATSettingsIsCurrentProfileADefault() {
	return std::find(std::begin(g_ATDefaultProfileIds), std::end(g_ATDefaultProfileIds), g_ATCurrentProfileId) != std::end(g_ATDefaultProfileIds);
}

void ATSettingsSwitchProfile(uint32 profileId) {
	if (g_ATCurrentProfileId == profileId)
		return;

	ATSaveSettings(kATSettingsCategory_All);
	g_ATCurrentProfileId = profileId;
	ATLoadSettings(kATSettingsCategory_All);

	g_sim.ColdReset();

	VDRegistryAppKey key("Profiles", true);
	key.setInt("Current profile", profileId);
}

void ATSettingsLoadProfile(uint32 profileId, ATSettingsCategory mask) {
	g_ATProfileTemporary = false;
	g_ATProfileBootstrap = false;
	g_ATCurrentProfileId = profileId;

	ATLoadSettings(mask);
}

void ATSettingsLoadLastProfile(ATSettingsCategory mask) {
	VDRegistryAppKey key("Profiles", false);
	ATSettingsLoadProfile(key.getInt("Current profile", 0), mask);
}

bool ATSettingsGetTemporaryProfileMode() {
	return g_ATProfileTemporary;
}

void ATSettingsSetTemporaryProfileMode(bool temporary) {
	g_ATProfileTemporary = temporary;
}

bool ATSettingsGetBootstrapProfileMode() {
	return g_ATProfileBootstrap;
}

void ATSettingsSetBootstrapProfileMode(bool temporary) {
	g_ATProfileBootstrap = temporary;
}

///////////////////////////////////////////////////////////////////////////

namespace {
	const char *const kDefaultProfileTags[]={
		"800",
		"1200XL",
		"XL",
		"XEGS",
		"5200",
	};

	const wchar_t *const kDefaultProfileNames[]={
		L"400/800 Computer",
		L"1200XL Computer",
		L"XL/XE Computer",
		L"XEGS Console",
		L"5200 Console",
	};

	const ATHardwareMode kDefaultProfileHwModes[]={
		kATHardwareMode_800,
		kATHardwareMode_1200XL,
		kATHardwareMode_800XL,
		kATHardwareMode_XEGS,
		kATHardwareMode_5200,
	};

	static_assert(vdcountof(kDefaultProfileNames) == kATDefaultProfileCount, "Default profile table is out of sync");
	static_assert(vdcountof(kDefaultProfileHwModes) == kATDefaultProfileCount, "Default profile table is out of sync");
}

void ATInitStockProfiles() {
	for(size_t i=0; i<kATDefaultProfileCount; ++i) {
		const uint32 profileId = VDHashString32(kDefaultProfileTags[i]);
		ATSettingsProfileSetName(profileId, kDefaultProfileNames[i]);
		ATSettingsProfileSetVisible(profileId, true);

		ATSettingsCategory categoryMask;
		if (i != kATDefaultProfile_5200)
			categoryMask = (ATSettingsCategory)(kATSettingsCategory_Hardware | kATSettingsCategory_Firmware);
		else
			categoryMask = (ATSettingsCategory)(kATSettingsCategory_Hardware
				| kATSettingsCategory_Firmware
				| kATSettingsCategory_Input
				| kATSettingsCategory_InputMaps
				| kATSettingsCategory_Acceleration
				| kATSettingsCategory_StartupConfig
				| kATSettingsCategory_Devices
				| kATSettingsCategory_MountedImages
				);

		ATSettingsProfileSetCategoryMask(profileId, categoryMask);
		ATSettingsProfileSetSavedCategoryMask(profileId, categoryMask);
		ATSetDefaultProfileId((ATDefaultProfile)i, profileId);

		ProfileKey key(profileId, true);
		key.setInt("Hardware mode", kDefaultProfileHwModes[i]);
	}
}

bool ATLoadDefaultProfiles() {
	VDRegistryAppKey key("Profiles", true);
	VDRegistryKey defaultsKey(key, "Defaults", false);

	for(size_t i=0; i<kATDefaultProfileCount; ++i)
		g_ATDefaultProfileIds[i] = defaultsKey.getInt(kDefaultProfileTags[i]);

	if (!key.getBool("Defaults inited")) {
		ATInitStockProfiles();

		key.setBool("Defaults inited", true);

		key.setInt("Current profile", g_ATDefaultProfileIds[kATDefaultProfile_XL]);
		return true;
	}

	return false;
}

uint32 ATGetDefaultProfileId(ATDefaultProfile profile) {
	return g_ATDefaultProfileIds[profile];
}

void ATSetDefaultProfileId(ATDefaultProfile profile, uint32 profileId) {
	if (g_ATDefaultProfileIds[profile] != profileId) {
		g_ATDefaultProfileIds[profile] = profileId;

		VDRegistryAppKey key("Profiles\\Defaults", true);
		key.setInt(kDefaultProfileTags[profile], profileId);
	}
}

void ATSettingsScheduleReset() {
	VDRegistryAppKey key;

	key.setBool("Reset all pending", true);

}

bool ATSettingsIsResetPending() {
	VDRegistryAppKey key("", false);

	return key.getBool("Reset all pending");
}

void ATSettingsSetInPortableMode(bool portable) {
	g_ATInPortableMode = portable;
}

bool ATSettingsIsInPortableMode() {
	return g_ATInPortableMode;
}

void ATSettingsScheduleMigration() {
	g_ATSettingsMigrationScheduled = true;
}

bool ATSettingsIsMigrationScheduled() {
	return g_ATSettingsMigrationScheduled;
}

VDStringW ATSettingsGetDefaultPortablePath() {
	return VDMakePath(VDGetProgramPath().c_str(), L"Altirra.ini");
}
