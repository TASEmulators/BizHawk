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


typedef struct
{
  FrameInfo base;
  gamePad_t gamePad;
} MyFrameInfo;

