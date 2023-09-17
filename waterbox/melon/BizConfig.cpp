#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "BizConfig.h"

namespace Config
{

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

}
