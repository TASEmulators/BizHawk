#ifndef BIZUSERDATA_H
#define BIZUSERDATA_H

#include "types.h"

namespace melonDS::Platform
{

struct BizUserData
{
	bool NdsSaveRamIsDirty;
	bool GbaSaveRamIsDirty;
	u8 MicVolume;
	int MicSamplePos;
};

}

#endif
