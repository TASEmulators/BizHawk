//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
#include "cpu.h"
#include "uiaccessors.h"
#include "uiconfirm.h"
#include "simulator.h"

extern ATSimulator g_sim;

void ATSyncCPUHistoryState();

void OnCommandSystemCPUMode(ATCPUMode mode, uint32 subCycles) {
	bool doReset = false;

	if (!g_sim.IsCPUModeOverridden() && g_sim.GetCPUMode() != mode) {
		if (!ATUIConfirmDiscardMemory(ATUIGetNewPopupOwner(), L"Changing CPU type"))
			return;

		doReset = true;
	}

	g_sim.SetCPUMode(mode, subCycles);

	if (doReset)
		g_sim.ColdReset();
}

void OnCommandSystemCPUMode6502() { OnCommandSystemCPUMode(kATCPUMode_6502, 1); }
void OnCommandSystemCPUMode65C02() { OnCommandSystemCPUMode(kATCPUMode_65C02, 1); }
void OnCommandSystemCPUMode65C816() { OnCommandSystemCPUMode(kATCPUMode_65C816, 1); }
void OnCommandSystemCPUMode65C816x2() { OnCommandSystemCPUMode(kATCPUMode_65C816, 2); }
void OnCommandSystemCPUMode65C816x4() { OnCommandSystemCPUMode(kATCPUMode_65C816, 4); }
void OnCommandSystemCPUMode65C816x6() { OnCommandSystemCPUMode(kATCPUMode_65C816, 6); }
void OnCommandSystemCPUMode65C816x8() { OnCommandSystemCPUMode(kATCPUMode_65C816, 8); }
void OnCommandSystemCPUMode65C816x10() { OnCommandSystemCPUMode(kATCPUMode_65C816, 10); }
void OnCommandSystemCPUMode65C816x12() { OnCommandSystemCPUMode(kATCPUMode_65C816, 12); }

void OnCommandSystemCPUToggleHistory() {
	auto& cpu = g_sim.GetCPU();
	cpu.SetHistoryEnabled(!cpu.IsHistoryEnabled());
	ATSyncCPUHistoryState();
}

void OnCommandSystemCPUTogglePathTracing() {
	auto& cpu = g_sim.GetCPU();
	cpu.SetPathfindingEnabled(!cpu.IsPathfindingEnabled());
}

void OnCommandSystemCPUToggleIllegalInstructions() {
	auto& cpu = g_sim.GetCPU();
	cpu.SetIllegalInsnsEnabled(!cpu.AreIllegalInsnsEnabled());
}

void OnCommandSystemCPUToggleStopOnBRK() {
	auto& cpu = g_sim.GetCPU();
	cpu.SetStopOnBRK(!cpu.GetStopOnBRK());
}

void OnCommandSystemCPUToggleNMIBlocking() {
	auto& cpu = g_sim.GetCPU();
	cpu.SetNMIBlockingEnabled(!cpu.IsNMIBlockingEnabled());
}

void OnCommandSystemCPUToggleShadowROM() {
	g_sim.SetShadowROMEnabled(!g_sim.GetShadowROMEnabled());
}

void OnCommandSystemCPUToggleShadowCarts() {
	g_sim.SetShadowCartridgeEnabled(!g_sim.GetShadowCartridgeEnabled());
}
