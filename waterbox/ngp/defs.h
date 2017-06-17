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

#include <emulibc.h>
#define EXPORT extern "C" ECL_EXPORT
#include <waterboxcore.h>

struct MyFrameInfo: public FrameInfo
{
	int64_t FrontendTime;
	int32_t SkipRendering;
	int32_t Buttons;
};


struct MDFN_Surface
{
	uint32 *pixels;
	int pitch32;
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
