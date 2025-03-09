#include "bizhawk.hpp"
#include <stdlib.h>

ECL_EXPORT bool Init()
{
	return true;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
}

uint8_t dummyMem[512];
ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	int memAreaIdx = 0;

	m[memAreaIdx].Data  = dummyMem;
	m[memAreaIdx].Name  = "Work RAM";
	m[memAreaIdx].Size  = 512;
	m[memAreaIdx].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;
	memAreaIdx++;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}
