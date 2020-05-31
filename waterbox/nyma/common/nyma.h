#pragma once

#include <stdint.h>

// the one linked core should set the MDFNGameInfo global
void SetupMDFNGameInfo();

struct CheatArea
{
	void* data;
	uint32_t size;
};

// find a previously registered cheatmem area, or null if it does not exist
CheatArea* FindCheatArea(uint32_t address);

extern bool LagFlag;
extern void (*InputCallback)();
extern int64_t FrontendTime;

typedef void (*FrameCallback)();

// Register a callback to run each frame asynchronously
// Only one callback may be registered
// The callback may not call any C standard library functions, or otherwise trigger a syscall
// The callback must return before frame advance finishes
void RegisterFrameThreadProc(FrameCallback threadproc);
