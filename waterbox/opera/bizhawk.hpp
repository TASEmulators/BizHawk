#pragma once

// system
#include <limits.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>

// waterbox
#include "emulibc.h"
#include "waterboxcore.h"

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

struct flightStick_t
{
	int up;
	int down;
	int left;
	int right;
	int fire;
	int buttonA;
	int buttonB;
	int buttonC;
	int buttonX;
	int buttonP;
	int leftTrigger;
	int rightTrigger;
	int horizontalAxis;
	int verticalAxis;
	int altitudeAxis;
};

struct controllerData_t
{
	gamePad_t gamePad;
	mouse_t mouse;
	flightStick_t flightStick;
};

typedef struct
{
	FrameInfo base;
	controllerData_t port1;
	controllerData_t port2;
} MyFrameInfo;
