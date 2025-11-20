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
  int buttonX;
  int buttonP;
  int buttonA;
  int buttonB;
  int buttonC;
  int buttonL;
  int buttonR;
};

struct mouse_t
{
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

struct lightGun_t
{
  int trigger;
  int select;
  int reload;
  int isOffScreen;
  int screenX;
  int screenY;
};

struct arcadeLightGun_t
{
  int trigger;
  int select;
  int start;
  int reload;
  int auxA;
  int isOffScreen;
  int screenX;
  int screenY;
};

struct OrbatakTrackball_t
{
  int dX;
  int dY;
  int startP1;
  int startP2;
  int coinP1;
  int coinP2;
  int service;
};

struct controllerData_t
{
  gamePad_t gamePad;
  mouse_t mouse;
  flightStick_t flightStick;
  lightGun_t lightGun;
  arcadeLightGun_t arcadeLightGun;
  OrbatakTrackball_t orbatakTrackball;
};

typedef struct
{
  FrameInfo base;
  controllerData_t port1;
  controllerData_t port2;
  int isReset;
} MyFrameInfo;

