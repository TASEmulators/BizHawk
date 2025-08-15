#include "Platform.h"

namespace melonDS::Platform
{

struct AACDecoder
{
};

AACDecoder* AAC_Init()
{
	return nullptr;
}

void AAC_DeInit(AACDecoder* dec)
{
	delete dec;
}

bool AAC_Configure(AACDecoder* dec, int frequency, int channels)
{
	return false;
}

bool AAC_DecodeFrame(AACDecoder* dec, const void* input, int inputlen, void* output, int outputlen)
{
	return false;
}

}
