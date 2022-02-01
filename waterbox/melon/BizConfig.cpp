#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "BizConfig.h"

namespace Config
{

#ifdef JIT_ENABLED
bool JIT_Enable = false;
int JIT_MaxBlockSize = 32;
bool JIT_BranchOptimisations = true;
bool JIT_LiteralOptimisations = true;
bool JIT_FastMemory = true;
#endif

bool ExternalBIOSEnable;

std::string BIOS9Path = "bios9.rom";
std::string BIOS7Path = "bios7.rom";
std::string FirmwarePath = "firmware.bin";

std::string DSiBIOS9Path = "bios9i.rom";
std::string DSiBIOS7Path = "bios7i.rom";
std::string DSiFirmwarePath = "firmwarei.bin";
std::string DSiNANDPath = "nand.bin";

bool DLDIEnable = false;
std::string DLDISDPath = "";
int DLDISize = 0;
bool DLDIReadOnly = true;
bool DLDIFolderSync = false;
std::string DLDIFolderPath = "";

bool DSiSDEnable = false;
std::string DSiSDPath = "";
int DSiSDSize = 0;
bool DSiSDReadOnly = true;
bool DSiSDFolderSync = false;
std::string DSiSDFolderPath = "";

bool FirmwareOverrideSettings;
std::string FirmwareUsername;
int FirmwareLanguage;
int FirmwareBirthdayMonth;
int FirmwareBirthdayDay;
int FirmwareFavouriteColour;
std::string FirmwareMessage;
std::string FirmwareMAC;
bool RandomizeMAC;

int AudioBitrate;

bool FixedBootTime = true;
bool UseRealTime = false;
int TimeAtBoot = 0;

}
