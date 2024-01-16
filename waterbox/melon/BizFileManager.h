#ifndef BIZFILEMANAGER_H
#define BIZFILEMANAGER_H

namespace FileManager
{

struct FirmwareSettings
{
	bool OverrideSettings;
	int UsernameLength;
	char16_t Username[10];
	int Language;
	int BirthdayMonth;
	int BirthdayDay;
	int Color;
	int MessageLength;
	char16_t Message[26];
};

const char* InitNDSBIOS();
const char* InitFirmware(FirmwareSettings& fwSettings);
const char* InitDSiBIOS();
const char* InitNAND(FirmwareSettings& fwSettings, bool clearNand, bool dsiWare);
const char* InitCarts(bool gba);
void SetupDirectBoot();

}

#endif
