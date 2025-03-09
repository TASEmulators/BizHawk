#pragma once

// system
#include <limits.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

// waterbox
#include "emulibc.h"
#include "waterboxcore.h"

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
	FrameInfo base;
	JoystickButtons joy1;
	JoystickButtons joy2;
} MyFrameInfo;
