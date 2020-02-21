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

#ifndef f_AT_SETTINGS_H
#define f_AT_SETTINGS_H

#include <vd2/system/vdstl.h>

class VDStringW;

enum ATSettingsCategory : uint32 {
	kATSettingsCategory_None			= 0x00000000,
	kATSettingsCategory_Hardware		= 0x00000001,
	kATSettingsCategory_Firmware		= 0x00000002,
	kATSettingsCategory_Acceleration	= 0x00000004,
	kATSettingsCategory_Debugging		= 0x00000008,
	kATSettingsCategory_Devices			= 0x00000010,
	kATSettingsCategory_StartupConfig	= 0x00000020,
	kATSettingsCategory_Environment		= 0x00000040,
	kATSettingsCategory_Color			= 0x00000080,
	kATSettingsCategory_View			= 0x00000100,
	kATSettingsCategory_InputMaps		= 0x00000200,
	kATSettingsCategory_Input			= 0x00000400,
	kATSettingsCategory_Speed			= 0x00000800,
	kATSettingsCategory_MountedImages	= 0x00001000,
	kATSettingsCategory_FullScreen		= 0x00002000,
	kATSettingsCategory_Sound			= 0x00004000,
	kATSettingsCategory_Boot			= 0x00008000,

	kATSettingsCategory_AllCategories	= 0x0000FFFF,

	kATSettingsCategory_Baseline =
		kATSettingsCategory_Hardware |
		kATSettingsCategory_Firmware |
		kATSettingsCategory_Acceleration |
		kATSettingsCategory_Debugging |
		kATSettingsCategory_Devices |
		kATSettingsCategory_StartupConfig |
		kATSettingsCategory_MountedImages,

	kATSettingsCategory_All				= 0xFFFFFFFF
};

enum ATDefaultProfile {
	kATDefaultProfile_800,
	kATDefaultProfile_1200XL,
	kATDefaultProfile_XL,
	kATDefaultProfile_XEGS,
	kATDefaultProfile_5200,
	kATDefaultProfileCount
};

enum : uint32 {
	kATProfileId_Invalid = 0xFFFFFFFF
};

bool ATLoadDefaultProfiles();
uint32 ATGetDefaultProfileId(ATDefaultProfile profile);
void ATSetDefaultProfileId(ATDefaultProfile profile, uint32 profileId);

void ATLoadSettings(ATSettingsCategory categories);
void ATSaveSettings(ATSettingsCategory categories);

void ATSettingsProfileEnum(vdfastvector<uint32>& profileIds);
bool ATSettingsIsValidProfile(uint32 profileId);
uint32 ATSettingsGenerateProfileId();

void ATSettingsProfileSetCategoryMask(uint32 profileId, ATSettingsCategory mask);
ATSettingsCategory ATSettingsProfileGetCategoryMask(uint32 profileId);

void ATSettingsProfileSetSavedCategoryMask(uint32 profileId, ATSettingsCategory mask);
ATSettingsCategory ATSettingsProfileGetSavedCategoryMask(uint32 profileId);

void ATSettingsProfileSetVisible(uint32 profileId, bool visible);
bool ATSettingsProfileGetVisible(uint32 profileId);

uint32 ATSettingsProfileGetParent(uint32 profileId);
void ATSettingsProfileSetParent(uint32 profileId, uint32 parentId);

void ATSettingsProfileDelete(uint32 profileId);

uint32 ATSettingsFindProfileByName(const wchar_t *name);

VDStringW ATSettingsProfileGetName(uint32 profileId);
void ATSettingsProfileSetName(uint32 profileId, const wchar_t *name);

uint32 ATSettingsGetCurrentProfileId();
bool ATSettingsIsCurrentProfileADefault();
void ATSettingsSwitchProfile(uint32 profileId);

void ATSettingsLoadProfile(uint32 profileId, ATSettingsCategory mask);
void ATSettingsLoadLastProfile(ATSettingsCategory mask);

bool ATSettingsGetTemporaryProfileMode();
void ATSettingsSetTemporaryProfileMode(bool temporary);

bool ATSettingsGetBootstrapProfileMode();
void ATSettingsSetBootstrapProfileMode(bool bootstrap);

// A scheduled reset causes all settings to be nuked on exit, or if something
// goes wrong, on the next startup.
void ATSettingsScheduleReset();
bool ATSettingsIsResetPending();

void ATSettingsSetInPortableMode(bool portable);
bool ATSettingsIsInPortableMode();

void ATSettingsScheduleMigration();
bool ATSettingsIsMigrationScheduled();

VDStringW ATSettingsGetDefaultPortablePath();

void ATSettingsMigrate();

#endif
