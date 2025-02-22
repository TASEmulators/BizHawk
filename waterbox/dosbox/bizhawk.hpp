#pragma once

// system
#include <limits.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

// waterbox
#include "emulibc.h"
#include "waterboxcore.h"

const int KEY_COUNT = 0x65;

typedef struct
{
		int insertFloppyDisk;
		int insertCDROM;
		int insertHardDiskDrive;
} DriveActions;

typedef struct
{
	FrameInfo base;
	char Keys[KEY_COUNT];
	DriveActions driveActions;
} MyFrameInfo;
