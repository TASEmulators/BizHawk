#pragma once

#include <cstdint>
#include <cstddef>
#include <algorithm>
#include <cassert>
#include <cstring>
#include <cstdlib>

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;
typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

//#define MDFN_FASTCALL
#define INLINE inline
#define MDFN_COLD
#define NO_INLINE
//#define MDFN_ASSUME_ALIGNED(p, align) ((decltype(p))__builtin_assume_aligned((p), (align)))
#define MDFN_ASSUME_ALIGNED(p, align) (p)
#define trio_snprintf snprintf
#define TRUE true
#define FALSE false
#ifndef __alignas_is_defined
#define alignas(p)
#endif

struct MDFN_Surface
{
	uint32 *pixels;
	int pitch32;
};

struct EmulateSpecStruct
{
	// Pitch(32-bit) must be equal to width and >= the "fb_width" specified in the MDFNGI struct for the emulated system.
	// Height must be >= to the "fb_height" specified in the MDFNGI struct for the emulated system.
	// The framebuffer pointed to by surface->pixels is written to by the system emulation code.
	uint32 *pixels;

	// Pointer to sound buffer, set by the driver code, that the emulation code should render sound to.
	// Guaranteed to be at least 500ms in length, but emulation code really shouldn't exceed 40ms or so.  Additionally, if emulation code
	// generates >= 100ms,
	// DEPRECATED: Emulation code may set this pointer to a sound buffer internal to the emulation module.
	int16 *SoundBuf;

	// Number of cycles that this frame consumed, using MDFNGI::MasterClock as a time base.
	// Set by emulation code.
	int64 MasterCycles;

	// unix time for RTC
	int64 FrontendTime;

	// Maximum size of the sound buffer, in frames.  Set by the driver code.
	int32 SoundBufMaxSize;

	// Number of frames currently in internal sound buffer.  Set by the system emulation code, to be read by the driver code.
	int32 SoundBufSize;

	// true to skip rendering
	int32 skip;

	int32 Buttons;

	// set by core, true if lagged
	int32 Lagged;
};

#define MDFN_printf(...)
#define MDFN_PrintError(...)
#define require assert

#include "endian.h"

inline char* strdup(const char* p)
{
    char* ret = (char*)malloc(strlen(p) + 1);
    if (ret)
        strcpy(ret, p);
    return ret;
}

#include <emulibc.h>
#define EXPORT extern "C" ECL_EXPORT
