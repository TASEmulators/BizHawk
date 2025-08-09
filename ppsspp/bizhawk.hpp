#pragma once

// system
#include <limits.h>
#include <stddef.h>
#include <stdarg.h>
#include <stdio.h>
#include <cstdint>

#ifdef _WIN32
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __attribute__((visibility("default")))
#endif

struct gamePad_t
{
	int32_t up;
	int32_t down;
	int32_t left;
	int32_t right;
	int32_t start;
	int32_t select;
	int32_t square;
	int32_t triangle;
	int32_t circle;
	int32_t cross;
	int32_t ltrigger;
	int32_t rtrigger;
	int32_t rightAnalogX;
	int32_t rightAnalogY;
	int32_t leftAnalogX;
	int32_t leftAnalogY;
};

typedef struct
{
  gamePad_t gamePad;
} MyFrameInfo;

