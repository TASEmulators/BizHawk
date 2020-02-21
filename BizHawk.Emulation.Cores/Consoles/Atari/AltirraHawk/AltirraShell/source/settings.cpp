//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - global settings
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
#include <vd2/system/registry.h>
#include <at/atcore/devicemanager.h>
#include "serialconfig.h"
#include "settings.h"
#include "globals.h"
#include "events.h"
#include "broadcaster.h"

bool g_ATSDiskAccurateTimingEnabled;

void ATSLoadSettings() {
	VDRegistryAppKey settingsKey("Settings", false);

	g_ATSDiskAccurateTimingEnabled = settingsKey.getBool("Disk: Accurate Timing", g_ATSDiskAccurateTimingEnabled);

	VDRegistryKey serialSettingsKey(settingsKey, "Serial", false);

	ATSSerialConfig cfg;
	ATSLoadSerialConfig(cfg, serialSettingsKey);

	ATSSendEngineRequest([&]() { ATSSetSerialConfig(cfg); });

	VDStringW devices;
	if (settingsKey.getString("devices", devices)) {
		ATSSendEngineRequest([&]() { ATSGetDeviceManager()->DeserializeDevices(nullptr, nullptr, devices.c_str()); });
	}

	ATSRaiseEvent(ATSEventDevicesChanged());
}

void ATSSaveSettings() {
	VDRegistryAppKey settingsKey("Settings");
	settingsKey.setBool("Disk: Accurate Timing", g_ATSDiskAccurateTimingEnabled);

	VDRegistryKey serialSettingsKey(settingsKey, "Serial");

	ATSSerialConfig cfg;
	ATSSendEngineRequest([&]() { ATSGetSerialConfig(cfg); });

	ATSSaveSerialConfig(serialSettingsKey, cfg);

	VDStringW devices;
	ATSSendEngineRequest([&]() { ATSGetDeviceManager()->SerializeDevice(nullptr, devices); });
	settingsKey.setString("devices", devices.c_str());
}
