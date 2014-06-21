/*
compiler types and configuration
*/

#pragma once

#include <stdlib.h>

typedef __int8 s8;
typedef __int16 s16;
typedef __int32 s32;
typedef __int64 s64;
typedef unsigned __int8 u8;
typedef unsigned __int16 u16;
typedef unsigned __int32 u32;
typedef unsigned __int64 u64;

#define ABORT(message) { printf("%s\n",message); exit(0); }
#define ARRAY_SIZE(a) (sizeof(a) / sizeof((a)[0]))
#ifndef CTASSERT
#define	CTASSERT(x)		typedef char __assert ## y[(x) ? 1 : -1]
#endif
