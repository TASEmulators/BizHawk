#pragma once

// system
#include <limits.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

// waterbox
#include "emulibc.h"
#include "waterboxcore.h"

#pragma pack(push, 1)
struct gamePad_t
{
	int up;
	int down;
	int left;
	int right;
	int start;
	int select;
	int buttonA;
	int buttonB;
	int buttonX;
	int buttonY;
	int buttonL;
	int buttonR;
};
#pragma pack(pop)

#pragma pack(push, 1)
struct mouse_t
{
	int posX;
	int posY;
	int dX;
	int dY;
	int leftButton;
	int middleButton;
	int rightButton;
	int fourthButton;
};
#pragma pack(pop)

#pragma pack(push, 1)
struct controllerData_t
{
	gamePad_t gamePad;
	mouse_t mouse;
};
#pragma pack(pop)

typedef struct
{
	FrameInfo base;
	controllerData_t port1;
	controllerData_t port2;
} MyFrameInfo;
