#include "PsxCore.h"
#include "core.h"
#include <string.h>
#include <stdlib.h>
#include "emufile_hawk.h"


static FunctionRecord records[] = {
	FUNC("PsxCore.Construct", &PsxCore::Construct),
	FUNC("PsxCore.GetResolution", &PsxCore::GetResolution),
	FUNC("PsxCore.FrameAdvance", &PsxCore::FrameAdvance),
	FUNC("PsxCore.UpdateVideoBuffer", &PsxCore::UpdateVideoBuffer)
};


PsxCore::Size PsxCore::GetResolution()
{
	con->fprintf("in PsxCore::GetResolution\n");
	Size size = {256,256};
	return size;
}

int videoBuffer[256*256];

void PsxCore::FrameAdvance()
{
	for(int i=0;i<256*256;i++)
	{
		videoBuffer[i] = rand() | (rand()<<15) | 0xFF000000;
	}
}

void PsxCore::UpdateVideoBuffer(void* target)
{
	int* dest = (int*)target;
	int* src = (int*)videoBuffer;
	for(int i=0;i<256*256;i++)
		*dest++ = *src++;
}

