//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/registry.h>
#include <at/atcore/media.h>
#include <at/atcore/vfs.h>
#include <at/atio/blobimage.h>
#include "oshelper.h"
#include "compatdb.h"
#include "compatengine.h"
#include "disk.h"
#include "firmwaremanager.h"
#include "resource.h"
#include "simulator.h"
#include "options.h"
#include "cartridge.h"
#include "hleprogramloader.h"
#include "uiaccessors.h"
#include "uicommondialogs.h"

extern ATSimulator g_sim;

bool ATUISwitchKernel(VDGUIHandle h, uint64 kernelId);

ATCompatDBView g_ATCompatDBView;
ATCompatDBView g_ATCompatDBViewExt;
VDStringW g_ATCompatDBPath;
void *g_pATCompatDBExt;

void ATCompatInit() {
	size_t len;

	const void *data = ATLockResource(IDR_COMPATDB, len);

	if (data) {
		if (len < sizeof(ATCompatDBHeader))
			return;

		auto *hdr = (const ATCompatDBHeader *)data;
		if (!hdr->Validate(len))
			return;

		g_ATCompatDBView = ATCompatDBView(hdr);
	}

	ATOptionsAddUpdateCallback(true,
		[](ATOptions& opts, const ATOptions *prevOpts, void *) {
			if (!prevOpts || opts.mbCompatEnableExternalDB != prevOpts->mbCompatEnableExternalDB
				|| opts.mCompatExternalDBPath != prevOpts->mCompatExternalDBPath)
			{
				if (g_ATOptions.mbCompatEnableExternalDB && !g_ATOptions.mCompatExternalDBPath.empty()) {
					try {
						ATCompatLoadExtDatabase(g_ATOptions.mCompatExternalDBPath.c_str(), false);
					} catch(const MyError&) {
					}
				}
			}
		},
		nullptr);
}

void ATCompatShutdown() {
	if (g_pATCompatDBExt) {
		g_ATCompatDBViewExt = ATCompatDBView();

		free(g_pATCompatDBExt);
		g_pATCompatDBExt = nullptr;
	}
}

void ATCompatLoadExtDatabase(const wchar_t *path, bool testOnly) {
	vdrefptr<ATVFSFileView> fileView;
	ATVFSOpenFileView(path, false, ~fileView);

	auto& stream = fileView->GetStream();

	auto len = stream.Length();;

	if (len > 0x8000000)
		throw MyError("Compatibility engine '%ls' is too big (%llu bytes).", path, (unsigned long long)len);

	size_t lensz = (size_t)len;

	vdautoblockptr p(malloc(lensz));
	if (!p)
		throw MyMemoryError(lensz);

	stream.Read(p, (sint32)lensz);

	auto *hdr = (const ATCompatDBHeader *)p.get();
	if (lensz < sizeof(ATCompatDBHeader) || !hdr->Validate(lensz))
		throw MyError("'%ls' is not a valid compiled compatibility engine file.", path);

	if (!testOnly) {
		if (g_pATCompatDBExt)
			free(g_pATCompatDBExt);

		g_pATCompatDBExt = p.release();
		g_ATCompatDBViewExt = ATCompatDBView(hdr);
		g_ATCompatDBPath = path;
	}
}

void ATCompatReloadExtDatabase() {
	if (!g_ATCompatDBPath.empty() && g_pATCompatDBExt)
		ATCompatLoadExtDatabase(g_ATCompatDBPath.c_str(), false);
}

bool ATCompatIsAllMuted() {
	if (!g_ATOptions.mbCompatEnable)
		return true;

	return !g_ATOptions.mbCompatEnableInternalDB && !g_ATOptions.mbCompatEnableExternalDB;
}

void ATCompatSetAllMuted(bool mute) {
	if (g_ATOptions.mbCompatEnable == mute) {
		g_ATOptions.mbCompatEnable = !mute;

		ATOptionsSave();
	}
}

VDStringA ATCompatGetTitleMuteKeyName(const ATCompatDBTitle *title) {
	// Take everything alphanumeric up to 15 chars, then add hash of the whole original string
	VDStringA name;
	for(const char *s = title->mName.c_str(); *s; ++s) {
		const char c = *s;

		if ((c >= 0x30 && c <= 0x39) || (c >= 0x41 && c <= 0x5A) || (c >= 0x61 && c <= 0x7A)) {
			name += c;

			if (name.size() >= 15)
				break;
		}
	}

	name.append_sprintf("_%08X", (unsigned)vdhash<VDStringA>()(title->mName.c_str()));
	return name;
}

bool ATCompatIsTitleMuted(const ATCompatDBTitle *title) {
	if (ATCompatIsAllMuted())
		return true;

	VDRegistryAppKey key("Settings\\MutedCompatMessages", false);
	const auto& name = ATCompatGetTitleMuteKeyName(title);

	if (key.getInt(name.c_str()) & 1)
		return true;

	return false;
}

void ATCompatSetTitleMuted(const ATCompatDBTitle *title, bool mute) {
	VDRegistryAppKey key("Settings\\MutedCompatMessages", true);
	const auto& name = ATCompatGetTitleMuteKeyName(title);

	int flags = key.getInt(name.c_str());
	if (mute) {
		if (!(flags & 1)) {
			++flags;

			key.setInt(name.c_str(), flags);
		}
	} else {
		if (flags & 1) {
			--flags;

			if (flags)
				key.setInt(name.c_str(), flags);
			else
				key.removeValue(name.c_str());
		}
	}
}

void ATCompatUnmuteAllTitles() {
	VDRegistryAppKey key("Settings", true);

	key.removeKeyRecursive("MutedCompatMessages");
}

bool ATHasInternalBASIC(ATHardwareMode hwmode) {
	switch(hwmode) {
		case kATHardwareMode_800XL:
		case kATHardwareMode_130XE:
		case kATHardwareMode_XEGS:
			return true;

		default:
			return false;
	}
}

bool ATCompatIsTagApplicable(ATCompatKnownTag knownTag) {
	switch(knownTag) {
		case kATCompatKnownTag_BASIC:
			if (ATHasInternalBASIC(g_sim.GetHardwareMode())) {
				if (!g_sim.IsBASICEnabled())
					return true;
			} else {
				if (!g_sim.IsCartridgeAttached(0))
					return true;
			}
			break;

		case kATCompatKnownTag_BASICRevA:
			return g_sim.GetActualBasicId() != g_sim.GetFirmwareManager()->GetSpecificFirmware(kATSpecificFirmwareType_BASICRevA);

		case kATCompatKnownTag_BASICRevB:
			return g_sim.GetActualBasicId() != g_sim.GetFirmwareManager()->GetSpecificFirmware(kATSpecificFirmwareType_BASICRevB);

		case kATCompatKnownTag_BASICRevC:
			return g_sim.GetActualBasicId() != g_sim.GetFirmwareManager()->GetSpecificFirmware(kATSpecificFirmwareType_BASICRevC);

		case kATCompatKnownTag_NoBASIC:
			if (ATHasInternalBASIC(g_sim.GetHardwareMode())) {
				if (g_sim.IsBASICEnabled())
					return true;
			} else {
				if (g_sim.IsCartridgeAttached(0))
					return true;
			}
			break;

		case kATCompatKnownTag_OSA:
			return g_sim.GetActualKernelId() != g_sim.GetFirmwareManager()->GetSpecificFirmware(kATSpecificFirmwareType_OSA);

		case kATCompatKnownTag_OSB:
			return g_sim.GetActualKernelId() != g_sim.GetFirmwareManager()->GetSpecificFirmware(kATSpecificFirmwareType_OSB);

		case kATCompatKnownTag_XLOS:
			return g_sim.GetActualKernelId() != g_sim.GetFirmwareManager()->GetSpecificFirmware(kATSpecificFirmwareType_XLOSr2);

		case kATCompatKnownTag_AccurateDiskTiming:
			if (!g_sim.IsDiskAccurateTimingEnabled() || g_sim.IsDiskSIOPatchEnabled())
				return true;
			break;

		case kATCompatKnownTag_NoU1MB:
			if (g_sim.IsUltimate1MBEnabled())
				return true;
			break;

		case kATCompatKnownTag_Undocumented6502:
			{
				auto& cpu = g_sim.GetCPU();

				if (cpu.GetCPUMode() != kATCPUMode_6502 || !cpu.AreIllegalInsnsEnabled())
					return true;
			}
			break;

		case kATCompatKnownTag_No65C816HighAddressing:
			if (g_sim.GetHighMemoryBanks() >= 0 && g_sim.GetCPU().GetCPUMode() == kATCPUMode_65C816)
				return true;
			break;

		case kATCompatKnownTag_WritableDisk:
			if (!(g_sim.GetDiskInterface(0).GetWriteMode() & kATMediaWriteMode_AllowWrite))
				return true;
			break;

		case kATCompatKnownTag_NoFloatingDataBus:
			switch(g_sim.GetHardwareMode()) {
				case kATHardwareMode_130XE:
				case kATHardwareMode_XEGS:
					return true;
			}
			break;
	}

	return false;
}

bool ATCompatCheckTitleTags(ATCompatDBView& view, const ATCompatDBTitle *title, vdfastvector<ATCompatKnownTag>& tags) {
	for(const auto& tagId : title->mTagIds) {
		const ATCompatKnownTag knownTag = view.GetKnownTag(tagId);

		if (ATCompatIsTagApplicable(knownTag))
			tags.push_back(knownTag);
	}

	return !tags.empty();
}

const ATCompatDBTitle *ATCompatFindTitle(const ATCompatDBView& view, const vdvector_view<const ATCompatMarker>& markers, vdfastvector<ATCompatKnownTag>& tags) {
	vdfastvector<const ATCompatDBRule *> matchingRules;
	matchingRules.reserve(markers.size());

	for(const auto& marker : markers) {
		const auto *rule = view.FindMatchingRule(marker.mRuleType, marker.mValue);

		if (rule)
			matchingRules.push_back(rule);
	}

	const auto *title = view.FindMatchingTitle(matchingRules.data(), matchingRules.size());

	if (!title || ATCompatIsTitleMuted(title))
		return nullptr;

	for(const auto& tagId : title->mTagIds) {
		const ATCompatKnownTag knownTag = view.GetKnownTag(tagId);

		tags.push_back(knownTag);
	}

	return !tags.empty() ? title : nullptr;
}

const ATCompatDBTitle *ATCompatFindTitle(const vdvector_view<const ATCompatMarker>& markers, vdfastvector<ATCompatKnownTag>& tags) {
	if (ATCompatIsAllMuted())
		return nullptr;

	const ATCompatDBTitle *title;

	if (g_ATOptions.mbCompatEnableExternalDB && g_ATCompatDBView.IsValid()) {
		title = ATCompatFindTitle(g_ATCompatDBViewExt, markers, tags);
		if (title)
			return title;
	}

	if (g_ATOptions.mbCompatEnableInternalDB && g_ATCompatDBView.IsValid()) {
		title = ATCompatFindTitle(g_ATCompatDBView, markers, tags);
		if (title)
			return title;
	}

	return nullptr;
}

const ATCompatDBTitle *ATCompatCheckDB(ATCompatDBView& view, vdfastvector<ATCompatKnownTag>& tags) {
	const auto& diskIf = g_sim.GetDiskInterface(0);
	const auto *pImage = diskIf.GetDiskImage();

	if (pImage && !pImage->IsDynamic()) {
		uint64 checksum = pImage->GetImageChecksum();

		const auto *rule = view.FindMatchingRule(kATCompatRuleType_DiskChecksum, checksum);

		if (rule) {
			auto *title = view.FindMatchingTitle(&rule, 1);

			if (title && !ATCompatIsTitleMuted(title)) {
				if (ATCompatCheckTitleTags(view, title, tags))
					return title;
			}
		}
	}

	for(int i=0; i<2; ++i) {
		auto *cart = g_sim.GetCartridge(i);

		if (!cart)
			continue;

		uint64 checksum = cart->GetChecksum();

		const auto *rule = view.FindMatchingRule(kATCompatRuleType_CartChecksum, checksum);

		if (rule) {
			auto *title = view.FindMatchingTitle(&rule, 1);

			if (title && !ATCompatIsTitleMuted(title)) {
				if (ATCompatCheckTitleTags(view, title, tags))
					return title;
			}
		}
	}

	auto *programLoader = g_sim.GetProgramLoader();
	if (programLoader) {
		auto *pgimage = programLoader->GetCurrentImage();

		if (pgimage) {
			auto *rule = view.FindMatchingRule(kATCompatRuleType_ExeChecksum, pgimage->GetChecksum());

			if (rule) {
				auto *title = view.FindMatchingTitle(&rule, 1);

				if (title && !ATCompatIsTitleMuted(title)) {
					if (ATCompatCheckTitleTags(view, title, tags))
						return title;
				}
			}
		}
	}

	return nullptr;
}

const ATCompatDBTitle *ATCompatCheck(vdfastvector<ATCompatKnownTag>& tags) {
	if (ATCompatIsAllMuted())
		return nullptr;

	const ATCompatDBTitle *title;

	if (g_ATOptions.mbCompatEnableExternalDB && g_ATCompatDBView.IsValid()) {
		title = ATCompatCheckDB(g_ATCompatDBViewExt, tags);
		if (title)
			return title;
	}

	if (g_ATOptions.mbCompatEnableInternalDB && g_ATCompatDBView.IsValid()) {
		title = ATCompatCheckDB(g_ATCompatDBView, tags);
		if (title)
			return title;
	}

	return nullptr;
}

bool ATCompatSwitchToSpecificBASIC(ATSpecificFirmwareType specificType) {
	auto *fw = g_sim.GetFirmwareManager();

	const auto id = fw->GetSpecificFirmware(specificType);
	if (!id)
		return false;

	ATFirmwareInfo info;
	if (!fw->GetFirmwareInfo(id, info))
		return false;

	if (!ATIsSpecificFirmwareTypeCompatible(info.mType, specificType))
		return false;

	g_sim.SetBasic(id);
	return true;
}

bool ATCompatTrySwitchToSpecificKernel(VDGUIHandle h, ATSpecificFirmwareType specificType) {
	auto *fw = g_sim.GetFirmwareManager();

	const auto id = fw->GetSpecificFirmware(specificType);
	if (!id)
		return false;

	ATFirmwareInfo info;
	if (!fw->GetFirmwareInfo(id, info))
		return false;

	if (!ATIsSpecificFirmwareTypeCompatible(info.mType, specificType))
		return false;

	ATHardwareMode hardwareMode = g_sim.GetHardwareMode();

	switch(specificType) {
		case kATSpecificFirmwareType_OSA:
		case kATSpecificFirmwareType_OSB:
			hardwareMode = kATHardwareMode_800;
			break;

		case kATSpecificFirmwareType_XLOSr2:
		default:
			if (hardwareMode != kATHardwareMode_800XL && hardwareMode != kATHardwareMode_130XE)
				hardwareMode = kATHardwareMode_800XL;
			break;

		case kATSpecificFirmwareType_XLOSr4:
			hardwareMode = kATHardwareMode_XEGS;
			break;
	}

	return ATUISwitchHardwareMode(h, hardwareMode, true) && ATUISwitchKernel(h, id);
}

void ATCompatSwitchToSpecificKernel(VDGUIHandle h, ATSpecificFirmwareType specificType) {
	if (!ATCompatTrySwitchToSpecificKernel(h, specificType)) {
		ATUIShowWarning(
			h,
			L"The ROM image required by this program could not be found. If you have it, make sure it is set under \"Use For...\" in Firmware Images. (Rescan will do this automatically for any images it finds.)",
			L"Altirra Warning");
	}
}

void ATCompatAdjust(VDGUIHandle h, const ATCompatKnownTag *tags, size_t numTags) {
	bool basic = false;
	bool nobasic = false;

	while(numTags--) {
		switch(*tags++) {
			case kATCompatKnownTag_BASIC:
				// Handle this last, as it has to be done after the hardware mode is known.
				basic = true;
				break;

			case kATCompatKnownTag_BASICRevA:
				ATCompatSwitchToSpecificBASIC(kATSpecificFirmwareType_BASICRevA);
				break;

			case kATCompatKnownTag_BASICRevB:
				ATCompatSwitchToSpecificBASIC(kATSpecificFirmwareType_BASICRevB);
				break;

			case kATCompatKnownTag_BASICRevC:
				ATCompatSwitchToSpecificBASIC(kATSpecificFirmwareType_BASICRevC);
				break;

			case kATCompatKnownTag_NoBASIC:
				nobasic = true;
				break;

			case kATCompatKnownTag_OSA:
				ATCompatSwitchToSpecificKernel(h, kATSpecificFirmwareType_OSA);
				break;

			case kATCompatKnownTag_OSB:
				ATCompatSwitchToSpecificKernel(h, kATSpecificFirmwareType_OSB);
				break;

			case kATCompatKnownTag_XLOS:
				ATCompatSwitchToSpecificKernel(h, kATSpecificFirmwareType_XLOSr2);
				break;

			case kATCompatKnownTag_AccurateDiskTiming:
				g_sim.SetDiskAccurateTimingEnabled(true);
				g_sim.SetDiskSIOPatchEnabled(false);
				break;

			case kATCompatKnownTag_NoU1MB:
				g_sim.SetUltimate1MBEnabled(false);
				break;

			case kATCompatKnownTag_Undocumented6502:
				{
					auto& cpu = g_sim.GetCPU();
					g_sim.SetCPUMode(kATCPUMode_6502, 1);
					cpu.SetIllegalInsnsEnabled(true);
				}
				break;

			case kATCompatKnownTag_No65C816HighAddressing:
				g_sim.SetHighMemoryBanks(-1);
				break;

			case kATCompatKnownTag_WritableDisk:
				{
					ATDiskInterface& diskIf = g_sim.GetDiskInterface(0);
					auto writeMode = diskIf.GetWriteMode();

					if (!(writeMode & kATMediaWriteMode_AllowWrite))
						diskIf.SetWriteMode(kATMediaWriteMode_VRWSafe);
				}
				break;

			case kATCompatKnownTag_NoFloatingDataBus:
				switch(g_sim.GetHardwareMode()) {
					case kATHardwareMode_130XE:
					case kATHardwareMode_XEGS:
						g_sim.SetHardwareMode(kATHardwareMode_800XL);
						break;
				}
				break;

			case kATCompatKnownTag_Cart52008K:
			case kATCompatKnownTag_Cart520016KOneChip:
			case kATCompatKnownTag_Cart520016KTwoChip:
			case kATCompatKnownTag_Cart520032K:
				// We ignore these tags at compat checking time. They're used to feed the
				// mapper detection instead.
				break;
		}
	}

	if (basic || nobasic) {
		// Check if this is a machine type that has internal BASIC, or if we need to insert
		// the BASIC cartridge.
		bool internalBASIC = ATHasInternalBASIC(g_sim.GetHardwareMode());

		if (basic) {
			g_sim.SetBASICEnabled(true);

			if (!internalBASIC)
				g_sim.LoadCartridgeBASIC();
		} else if (nobasic) {
			g_sim.SetBASICEnabled(false);

			if (!internalBASIC)
				g_sim.UnloadCartridge(0);
		}
	}

	g_sim.ColdReset();
}
