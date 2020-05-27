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
