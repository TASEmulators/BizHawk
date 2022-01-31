#ifndef CONFIG_H
#define CONFIG_H

#include <string>

namespace Config
{

#ifdef JIT_ENABLED
extern bool JIT_Enable;
extern int JIT_MaxBlockSize;
extern bool JIT_BranchOptimisations;
extern bool JIT_LiteralOptimisations;
extern bool JIT_FastMemory;
#endif

extern bool ExternalBIOSEnable;

extern std::string BIOS9Path;
extern std::string BIOS7Path;
extern std::string FirmwarePath;

extern std::string DSiBIOS9Path;
extern std::string DSiBIOS7Path;
extern std::string DSiFirmwarePath;
extern std::string DSiNANDPath;

extern bool DLDIEnable;
extern std::string DLDISDPath;
extern int DLDISize;
extern bool DLDIReadOnly;
extern bool DLDIFolderSync;
extern std::string DLDIFolderPath;

extern bool DSiSDEnable;
extern std::string DSiSDPath;
extern int DSiSDSize;
extern bool DSiSDReadOnly;
extern bool DSiSDFolderSync;
extern std::string DSiSDFolderPath;

extern bool FirmwareOverrideSettings;
extern std::string FirmwareUsername;
extern int FirmwareLanguage;
extern int FirmwareBirthdayMonth;
extern int FirmwareBirthdayDay;
extern int FirmwareFavouriteColour;
extern std::string FirmwareMessage;
extern std::string FirmwareMAC;
extern bool RandomizeMAC;

extern int AudioBitrate;

extern bool FixedBootTime;
extern bool UseRealTime;
extern int TimeAtBoot;

}

#endif
