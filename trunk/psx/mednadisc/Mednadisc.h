#pragma once

#include "emuware/emuware.h"

class MednaDisc;

EW_EXPORT void* mednadisc_LoadCD(const char* fname);
EW_EXPORT int32 mednadisc_ReadSector(MednaDisc* disc, int lba, void* buf2448);
EW_EXPORT void mednadisc_CloseCD(MednaDisc* disc);