//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2019 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include "stdafx.h"
#include <vd2/system/file.h>
#include <vd2/Dita/services.h>
#include <at/atio/cassetteimage.h>
#include <at/atui/uicommandmanager.h>
#include "cassette.h"
#include "cmdhelpers.h"
#include "simulator.h"
#include "uiaccessors.h"
#include "uifilefilters.h"

extern ATSimulator g_sim;

void ATUIShowTapeControlDialog(VDGUIHandle hParent, ATCassetteEmulator& cassette);

void OnCommandCassetteLoadNew() {
	g_sim.GetCassette().LoadNew();
}

void OnCommandCassetteLoad() {
	VDStringW fn(VDGetLoadFileName('cass', ATUIGetNewPopupOwner(), L"Load cassette tape", g_ATUIFileFilter_LoadTape, L"wav"));

	if (!fn.empty()) {
		ATCassetteEmulator& cas = g_sim.GetCassette();
		cas.Load(fn.c_str());
		cas.Play();
	}
}

void OnCommandCassetteUnload() {
	g_sim.GetCassette().Unload();
}

void OnCommandCassetteSave() {
	ATCassetteEmulator& cas = g_sim.GetCassette();
	if (!cas.IsLoaded())
		return;

	VDStringW fn(VDGetSaveFileName('cass', ATUIGetNewPopupOwner(), L"Save cassette tape", g_ATUIFileFilter_SaveTape, L"cas"));

	if (fn.empty())
		return;

	VDFileStream f(fn.c_str(), nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kSequential | nsVDFile::kCreateAlways);

	ATSaveCassetteImageCAS(f, cas.GetImage());
	cas.SetImageClean();
}

void OnCommandCassetteExportAudioTape() {
	ATCassetteEmulator& cas = g_sim.GetCassette();
	if (!cas.IsLoaded())
		return;

	VDStringW fn(VDGetSaveFileName('casa', ATUIGetNewPopupOwner(), L"Export cassette tape audio", g_ATUIFileFilter_SaveTapeAudio, L"wav"));

	if (fn.empty())
		return;

	VDFileStream f(fn.c_str(), nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kSequential | nsVDFile::kCreateAlways);

	ATSaveCassetteImageWAV(f, cas.GetImage());
	cas.SetImageClean();
}

void OnCommandCassetteTapeControlDialog() {
	ATUIShowTapeControlDialog(ATUIGetNewPopupOwner(), g_sim.GetCassette());
}

void OnCommandCassetteToggleSIOPatch() {
	g_sim.SetCassetteSIOPatchEnabled(!g_sim.IsCassetteSIOPatchEnabled());
}

void OnCommandCassetteToggleAutoBoot() {
	g_sim.SetCassetteAutoBootEnabled(!g_sim.IsCassetteAutoBootEnabled());
}

void OnCommandCassetteToggleAutoBasicBoot() {
	g_sim.SetCassetteAutoBasicBootEnabled(!g_sim.IsCassetteAutoBasicBootEnabled());
}

void OnCommandCassetteToggleAutoRewind() {
	g_sim.SetCassetteAutoRewindEnabled(!g_sim.IsCassetteAutoRewindEnabled());
}

void OnCommandCassetteToggleLoadDataAsAudio() {
	ATCassetteEmulator& cas = g_sim.GetCassette();

	cas.SetLoadDataAsAudioEnable(!cas.IsLoadDataAsAudioEnabled());
}

void OnCommandCassetteToggleRandomizeStartPosition() {
	g_sim.SetCassetteRandomizedStartEnabled(!g_sim.IsCassetteRandomizedStartEnabled());
}

void OnCommandCassetteTurboModeNone() {
	g_sim.GetCassette().SetTurboMode(kATCassetteTurboMode_None);
}

void OnCommandCassetteTurboModeCommandControl() {
	g_sim.GetCassette().SetTurboMode(kATCassetteTurboMode_CommandControl);
}

void OnCommandCassetteTurboModeProceedSense() {
	g_sim.GetCassette().SetTurboMode(kATCassetteTurboMode_ProceedSense);
}

void OnCommandCassetteTurboModeInterruptSense() {
	g_sim.GetCassette().SetTurboMode(kATCassetteTurboMode_InterruptSense);
}

void OnCommandCassetteTurboModeAlways() {
	g_sim.GetCassette().SetTurboMode(kATCassetteTurboMode_Always);
}

void OnCommandCassetteTogglePolarity() {
	auto& cas = g_sim.GetCassette();

	cas.SetPolarityMode(cas.GetPolarityMode() == kATCassettePolarityMode_Normal ? kATCassettePolarityMode_Inverted : kATCassettePolarityMode_Normal);
}

void OnCommandCassettePolarityModeNormal() {
	g_sim.GetCassette().SetPolarityMode(kATCassettePolarityMode_Normal);
}

void OnCommandCassettePolarityModeInverted() {
	g_sim.GetCassette().SetPolarityMode(kATCassettePolarityMode_Inverted);
}

void OnCommandCassetteDirectSenseNormal() {
	g_sim.GetCassette().SetDirectSenseMode(ATCassetteDirectSenseMode::Normal);
}

void OnCommandCassetteDirectSenseLowSpeed() {
	g_sim.GetCassette().SetDirectSenseMode(ATCassetteDirectSenseMode::LowSpeed);
}

void OnCommandCassetteDirectSenseHighSpeed() {
	g_sim.GetCassette().SetDirectSenseMode(ATCassetteDirectSenseMode::HighSpeed);
}

void OnCommandCassetteDirectSenseMaxSpeed() {
	g_sim.GetCassette().SetDirectSenseMode(ATCassetteDirectSenseMode::MaxSpeed);
}

namespace ATCommands {	
	bool IsCassetteLoaded() {
		return g_sim.GetCassette().IsLoaded();
	}

	bool IsCassetteLoadDataAsAudioEnabled() {
		return g_sim.GetCassette().IsLoadDataAsAudioEnabled();
	}

	bool IsCassetteRandomizeStartPositionEnabled() {
		return g_sim.IsCassetteRandomizedStartEnabled();
	}

	template<ATCassetteTurboMode T_TurboMode>
	bool IsCassetteTurboMode() {
		return g_sim.GetCassette().GetTurboMode() == T_TurboMode;
	}

	template<ATCassettePolarityMode T_PolarityMode>
	bool IsCassettePolarityMode() {
		return g_sim.GetCassette().GetPolarityMode() == T_PolarityMode;
	}

	static constexpr ATUICommand kATCommandsCassette[] = {
		{ "Cassette.LoadNew", OnCommandCassetteLoadNew, nullptr },
		{ "Cassette.Load", OnCommandCassetteLoad, nullptr },
		{ "Cassette.Unload", OnCommandCassetteUnload, IsCassetteLoaded },
		{ "Cassette.Save", OnCommandCassetteSave, IsCassetteLoaded },
		{ "Cassette.ExportAudioTape", OnCommandCassetteExportAudioTape, IsCassetteLoaded },
		{ "Cassette.TapeControlDialog", OnCommandCassetteTapeControlDialog, nullptr },
		{ "Cassette.ToggleSIOPatch", OnCommandCassetteToggleSIOPatch, nullptr, CheckedIf<SimTest<&ATSimulator::IsCassetteSIOPatchEnabled> > },
		{ "Cassette.ToggleAutoBoot", OnCommandCassetteToggleAutoBoot, nullptr, CheckedIf<SimTest<&ATSimulator::IsCassetteAutoBootEnabled> > },
		{ "Cassette.ToggleAutoBasicBoot", OnCommandCassetteToggleAutoBasicBoot, nullptr, CheckedIf<SimTest<&ATSimulator::IsCassetteAutoBasicBootEnabled> > },
		{ "Cassette.ToggleAutoRewind", OnCommandCassetteToggleAutoRewind, nullptr, CheckedIf<SimTest<&ATSimulator::IsCassetteAutoRewindEnabled> > },
		{ "Cassette.ToggleLoadDataAsAudio", OnCommandCassetteToggleLoadDataAsAudio, nullptr, CheckedIf<IsCassetteLoadDataAsAudioEnabled> },
		{ "Cassette.ToggleRandomizeStartPosition", OnCommandCassetteToggleRandomizeStartPosition, nullptr, CheckedIf<IsCassetteRandomizeStartPositionEnabled> },
		{ "Cassette.TurboModeNone", OnCommandCassetteTurboModeNone, nullptr, CheckedIf<IsCassetteTurboMode<kATCassetteTurboMode_None>> },
		{ "Cassette.TurboModeAlways", OnCommandCassetteTurboModeAlways, nullptr, CheckedIf<IsCassetteTurboMode<kATCassetteTurboMode_Always>> },
		{ "Cassette.TurboModeCommandControl", OnCommandCassetteTurboModeCommandControl, nullptr, CheckedIf<IsCassetteTurboMode<kATCassetteTurboMode_CommandControl>> },
		{ "Cassette.TurboModeProceedSense", OnCommandCassetteTurboModeProceedSense, nullptr, CheckedIf<IsCassetteTurboMode<kATCassetteTurboMode_ProceedSense>> },
		{ "Cassette.TurboModeInterruptSense", OnCommandCassetteTurboModeInterruptSense, nullptr, CheckedIf<IsCassetteTurboMode<kATCassetteTurboMode_InterruptSense>> },
		{ "Cassette.TogglePolarity", OnCommandCassetteTogglePolarity, Not<IsCassetteTurboMode<kATCassetteTurboMode_None>>, CheckedIf<IsCassettePolarityMode<kATCassettePolarityMode_Inverted>> },
		{ "Cassette.PolarityNormal", OnCommandCassettePolarityModeNormal, Not<IsCassetteTurboMode<kATCassetteTurboMode_None>>, CheckedIf<IsCassettePolarityMode<kATCassettePolarityMode_Normal>> },
		{ "Cassette.PolarityInverted", OnCommandCassettePolarityModeInverted, Not<IsCassetteTurboMode<kATCassetteTurboMode_None>>, CheckedIf<IsCassettePolarityMode<kATCassettePolarityMode_Inverted>> },
		{ "Cassette.DirectSenseNormal", OnCommandCassetteDirectSenseNormal, nullptr, [] { return ToChecked(g_sim.GetCassette().GetDirectSenseMode() == ATCassetteDirectSenseMode::Normal); } },
		{ "Cassette.DirectSenseLowSpeed", OnCommandCassetteDirectSenseLowSpeed, nullptr, [] { return ToChecked(g_sim.GetCassette().GetDirectSenseMode() == ATCassetteDirectSenseMode::LowSpeed); } },
		{ "Cassette.DirectSenseHighSpeed", OnCommandCassetteDirectSenseHighSpeed, nullptr, [] { return ToChecked(g_sim.GetCassette().GetDirectSenseMode() == ATCassetteDirectSenseMode::HighSpeed); } },
		{ "Cassette.DirectSenseMaxSpeed", OnCommandCassetteDirectSenseMaxSpeed, nullptr, [] { return ToChecked(g_sim.GetCassette().GetDirectSenseMode() == ATCassetteDirectSenseMode::MaxSpeed); } },
	};
}

void ATUIInitCommandMappingsCassette(ATUICommandManager& cmdMgr) {
	using namespace ATCommands;

	cmdMgr.RegisterCommands(kATCommandsCassette, vdcountof(kATCommandsCassette));
}
