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
	FrameInfo base;
	int16_t port1;
	int16_t port2;
} MyFrameInfo;
