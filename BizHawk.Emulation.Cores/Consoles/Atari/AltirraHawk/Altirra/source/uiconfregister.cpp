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
#include <at/atcore/devicemanager.h>

bool ATUIConfDevHardDisk(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevBlackBox(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevModem(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevDragonCart(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevPCLink(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevHostFS(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDev1030(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDev850(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevSX212(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevVeronica(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevSoundBoard(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevPocketModem(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevCorvus(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevMyIDE2(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevDongle(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevKMKJZIDE(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevKMKJZIDE2(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevCovox(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevDiskDriveFull(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevATR8000(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevPercom(VDGUIHandle hParent, ATPropertySet& props);
bool ATUIConfDevVBXE(VDGUIHandle hParent, ATPropertySet& props);

void ATRegisterDeviceConfigurers(ATDeviceManager& dev) {
	dev.AddDeviceConfigurer("harddisk", ATUIConfDevHardDisk);
	dev.AddDeviceConfigurer("blackbox", ATUIConfDevBlackBox);
	dev.AddDeviceConfigurer("modem", ATUIConfDevModem);
	dev.AddDeviceConfigurer("dragoncart", ATUIConfDevDragonCart);
	dev.AddDeviceConfigurer("pclink", ATUIConfDevPCLink);
	dev.AddDeviceConfigurer("hostfs", ATUIConfDevHostFS);
	dev.AddDeviceConfigurer("1030", ATUIConfDev1030);
	dev.AddDeviceConfigurer("850", ATUIConfDev850);
	dev.AddDeviceConfigurer("sx212", ATUIConfDevSX212);
	dev.AddDeviceConfigurer("veronica", ATUIConfDevVeronica);
	dev.AddDeviceConfigurer("soundboard", ATUIConfDevSoundBoard);
	dev.AddDeviceConfigurer("pocketmodem", ATUIConfDevPocketModem);
	dev.AddDeviceConfigurer("corvus", ATUIConfDevCorvus);
	dev.AddDeviceConfigurer("myide2", ATUIConfDevMyIDE2);
	dev.AddDeviceConfigurer("dongle", ATUIConfDevDongle);
	dev.AddDeviceConfigurer("kmkjzide", ATUIConfDevKMKJZIDE);
	dev.AddDeviceConfigurer("kmkjzide2", ATUIConfDevKMKJZIDE2);
	dev.AddDeviceConfigurer("covox", ATUIConfDevCovox);
	dev.AddDeviceConfigurer("diskdriveatr8000", ATUIConfDevATR8000);
	dev.AddDeviceConfigurer("diskdrivepercom", ATUIConfDevPercom);
	dev.AddDeviceConfigurer("vbxe", ATUIConfDevVBXE);

	static const char *const kDiskDriveFullTypes[]={
		"diskdrive810",
		"diskdrive810archiver",
		"diskdrivehappy810",
		"diskdrive1050",
		"diskdriveusdoubler",
		"diskdrivespeedy1050",
		"diskdrivehappy1050",
		"diskdrivesuperarchiver",
		"diskdrivetoms1050",
		"diskdrive1050duplicator",
		"diskdrivetygrys1050",
		"diskdrive1050turbo",
		"diskdrive1050turboii",
		"diskdriveisplate",
		"diskdriveindusgt",
		"diskdrivexf551",
	};

	for(const char *s : kDiskDriveFullTypes) {
		dev.AddDeviceConfigurer(s, ATUIConfDevDiskDriveFull);
	}
}
