#include "Platform.h"
#include "frontend/mic_blow.h"

#include "BizUserData.h"

namespace melonDS::Platform
{

void Mic_Start(void* userdata)
{
}

void Mic_Stop(void* userdata)
{
}

int Mic_ReadInput(s16* data, int maxlength, void* userdata)
{
	auto* bizUserData = static_cast<BizUserData*>(userdata);
	constexpr int micBlowSampleLength = sizeof(mic_blow) / sizeof(*mic_blow);
	const double micVolume = bizUserData->MicVolume / 100.0;

	for (int i = 0; i < maxlength; i++)
	{
		data[i] = round((s16)mic_blow[bizUserData->MicSamplePos++] * micVolume);
		if (bizUserData->MicSamplePos >= micBlowSampleLength)
		{
			bizUserData->MicSamplePos = 0;
		}
	}

	return maxlength;
}

}
