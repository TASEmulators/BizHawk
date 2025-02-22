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
		int up;
		int down;
		int left;
		int right;
		int button1;
		int button2;
} JoystickButtons;

typedef struct
{
	int posX;
	int posY;
	int leftButton;
	int middleButton;
	int rightButton;
} MouseInput;

typedef struct
{
	FrameInfo base;
	char Keys[KEY_COUNT];
	DriveActions driveActions;
	JoystickButtons joy1;
	JoystickButtons joy2;
	MouseInput mouse;
} MyFrameInfo;
