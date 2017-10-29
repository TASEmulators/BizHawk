#pragma once
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct
{
	uint32_t* VideoBuffer;
	int16_t* SoundBuffer;
	int64_t Cycles;
	int32_t Width;
	int32_t Height;
	int32_t Samples;
	int32_t Lagged;
} FrameInfo;

typedef struct
{
	void* Data;
	const char* Name;
	int64_t Size;
	int64_t Flags;
} MemoryArea;

#define MEMORYAREA_FLAGS_WRITABLE 1
#define MEMORYAREA_FLAGS_SAVERAMMABLE 2
#define MEMORYAREA_FLAGS_ONEFILLED 4
#define MEMORYAREA_FLAGS_PRIMARY 8
#define MEMORYAREA_FLAGS_YUGEENDIAN 16
#define MEMORYAREA_FLAGS_WORDSIZE1 32
#define MEMORYAREA_FLAGS_WORDSIZE2 64
#define MEMORYAREA_FLAGS_WORDSIZE4 128
#define MEMORYAREA_FLAGS_WORDSIZE8 256
#define MEMORYAREA_FLAGS_SWAPPED 512

#ifdef __cplusplus
}
#endif
