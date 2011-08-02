#include "PsxCore.h"
#include "core.h"
#include <string.h>
#include <stdlib.h>
#include "emufile_hawk.h"
#include "DiscInterface.h"


static FunctionRecord records[] = {
	REG("PsxCore.Construct", &PsxCore::Construct),
	REG("PsxCore.GetResolution", &PsxCore::GetResolution),
	REG("PsxCore.FrameAdvance", &PsxCore::FrameAdvance),
	REG("PsxCore.UpdateVideoBuffer", &PsxCore::UpdateVideoBuffer)
};

PsxCore::PsxCore(void* _opaque)
	: opaque(_opaque)
{
	discInterface = (DiscInterface*)ClientSignal(NULL,opaque,"GetDiscInterface",NULL);
}

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
	DiscInterface::TrackInfo ti = discInterface->GetTrack(0,0);
	con->fprintf("lba len: %d\n",ti.length_lba);
}

void PsxCore::UpdateVideoBuffer(void* target)
{
	int* dest = (int*)target;
	int* src = (int*)videoBuffer;
	for(int i=0;i<256*256;i++)
		*dest++ = *src++;
}

