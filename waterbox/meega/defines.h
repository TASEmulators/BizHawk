#pragma once

#include "uae/string.h"

#define debugtest(...)

// FIXME: THESE ARE HERE IN ORDER TO COMPILE blkdev.cpp
#define UAESCSI_CDEMU 0
#define UAESCSI_SPTI 1
#define UAESCSI_SPTISCAN 2
#define UAESCSI_ASPI_FIRST 3
#define UAESCSI_ADAPTECASPI 3
#define UAESCSI_NEROASPI 4
#define UAESCSI_FROGASPI 5
#define DRIVE_CDROM 0

int GetDriveType(TCHAR* vol);
#define _timezone timezone

// Some other places define it as unsigned long, but I think it was expected to be 32 bit
#ifndef ULONG
#define ULONG uint32_t
#endif
