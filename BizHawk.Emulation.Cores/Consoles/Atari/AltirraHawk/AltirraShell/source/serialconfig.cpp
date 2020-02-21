//	Altirra - Atari 800/800XL/5200 emulator
//	Native device emulator - serial emulation configuration
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
#include "serialconfig.h"

void ATSSaveSerialConfig(VDRegistryKey& key, const ATSSerialConfig& cfg) {
	key.setString("Device Path", cfg.mSerialPath.c_str());
	key.setInt("HS baud rate", cfg.mHSBaudRate);
	key.setInt("HS POKEY divisor", cfg.mHSPokeyDivisor);
	key.setInt("HS mode", cfg.mHighSpeedMode);
}

void ATSLoadSerialConfig(ATSSerialConfig& cfg, const VDRegistryKey& key) {
	key.getString("Device Path", cfg.mSerialPath);
	cfg.mHSBaudRate = key.getInt("HS baud rate", cfg.mHSBaudRate);
	cfg.mHSPokeyDivisor = key.getInt("HS POKEY divisor", cfg.mHSPokeyDivisor);
	cfg.mHighSpeedMode = (ATSSerialConfig::HighSpeedMode)key.getEnumInt("HS mode", cfg.kHighSpeed_PokeyDivisor + 1, cfg.mHighSpeedMode);
}
