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
#include <at/atcore/devicemanager.h>
#include "audiooutput.h"
#include "simulator.h"
#include "uiaccessors.h"

extern ATSimulator g_sim;

void ATUIShowAudioOptionsDialog(VDGUIHandle hParent);

void OnCommandAudioToggleStereo() {
	g_sim.SetDualPokeysEnabled(!g_sim.IsDualPokeysEnabled());
}

void OnCommandAudioToggleMonitor() {
	g_sim.SetAudioMonitorEnabled(!g_sim.IsAudioMonitorEnabled());
}

void OnCommandAudioToggleMute() {
	IATAudioOutput *out = g_sim.GetAudioOutput();

	if (out)
		out->SetMute(!out->GetMute());
}

void OnCommandAudioOptionsDialog() {
	ATUIShowAudioOptionsDialog(ATUIGetNewPopupOwner());
}

void OnCommandAudioToggleNonlinearMixing() {
	ATPokeyEmulator& pokey = g_sim.GetPokey();

	pokey.SetNonlinearMixingEnabled(!pokey.IsNonlinearMixingEnabled());
}

void OnCommandAudioToggleSerialNoise() {
	ATPokeyEmulator& pokey = g_sim.GetPokey();

	pokey.SetSerialNoiseEnabled(!pokey.IsSerialNoiseEnabled());
}

void OnCommandAudioToggleChannel(int channel) {
	ATPokeyEmulator& pokey = g_sim.GetPokey();
	pokey.SetChannelEnabled(channel, !pokey.IsChannelEnabled(channel));
}

void OnCommandAudioToggleSlightSid() {
	auto& dm = *g_sim.GetDeviceManager();

	dm.ToggleDevice("slightsid");
}

void OnCommandAudioToggleCovox() {
	auto& dm = *g_sim.GetDeviceManager();

	dm.ToggleDevice("covox");
}
