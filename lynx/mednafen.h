#ifndef MEDNAFEN_H
#define MEDNAFEN_H

#include <assert.h>
#include <inttypes.h>

typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;

#define MDFN_COLD
#define FALSE 0
#define TRUE 1
#define INLINE

typedef struct
{
	union
	{
		struct
		{
#ifdef MSB_FIRST
			uint8   High;
			uint8   Low;
#else
			uint8   Low;
			uint8   High;
#endif
		} Union8;
		uint16 Val16;
	};
} Uuint16;

typedef struct
{
	union
	{
		struct
		{
#ifdef MSB_FIRST
			Uuint16   High;
			Uuint16   Low;
#else
			Uuint16   Low;
			Uuint16   High;
#endif
		} Union16;
		uint32  Val32;
	};
} Uuint32;

#endif
