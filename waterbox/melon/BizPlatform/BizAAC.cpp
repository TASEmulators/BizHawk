#include "neaacdec.h"

#include "Platform.h"

// mostly a copy paste from melonDS/src/frontend/qt_sdl/Platform_AAC.cpp

namespace melonDS::Platform
{

struct AACDecoder
{
	NeAACDecHandle handle;
	bool configured;
};

AACDecoder* AAC_Init()
{
	AACDecoder* dec = new AACDecoder();
	dec->handle = NeAACDecOpen();

	if (!dec->handle)
	{
		delete dec;
		return nullptr;
	}

	auto* cfg = NeAACDecGetCurrentConfiguration(dec->handle);
	cfg->defObjectType = LC;
	cfg->outputFormat = FAAD_FMT_16BIT;

	if (!NeAACDecSetConfiguration(dec->handle, cfg))
	{
		NeAACDecClose(dec->handle);
		delete dec;
		return nullptr;
	}

	dec->configured = false;
	return dec;
}

void AAC_DeInit(AACDecoder* dec)
{
	NeAACDecClose(dec->handle);
	delete dec;
}

bool AAC_Configure(AACDecoder* dec, int frequency, int channels)
{
	// see get_sample_rate in faad2/libfaad/common.c
	constexpr u32 freqList[9] = { 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11025, 8000 };
	u8 freqNum = 3; // default to 48000
	for (u32 i = 0; i < 9; i++)
	{
		if (frequency == freqList[i])
		{
			freqNum = 3 + i;
			break;
		}
	}

	// Produce an MP4 ASC to configure the decoder
	// Basic MP4 ASC format is as follows:
	// 5 bits: Object Type Index (always LC (2) here)
	// 4 bits: Sample frequency index (DSi only supports up to 48000Hz)
	// 4 bits: Channel count
	// 1 bit: Frame Length flag (never true here)
	// 1 bit: Core coder delay present (never true here)
	// 1 bit: Extension flag (never true here)
	// 11 bits: Sync extension type (0x2B7 indicates possible SBR specification)
	// 5 bits: Extension audio object type (5 indicates SBR specification)
	// 1 bit: SBR present (never present on DSi, which doesn't support such)

	u8 asc[5];
	asc[0] = ((LC << 3) & 0b1111000) | ((freqNum >> 1) & 0b00000111);
	asc[1] = ((freqNum << 7) & 0b10000000) | ((channels << 3) & 0b01111000);
	asc[2] = (0x2B7 >> 3) & 0b11111111;
	asc[3] = ((0x2B7 << 5) & 0b11100000) | (0x05 & 0b00011111);
	asc[4] = (0x00 << 7) & 0b10000000;

	unsigned long freqOut;
	unsigned char chanOut;
	if (NeAACDecInit2(dec->handle, asc, sizeof(asc), &freqOut, &chanOut) != 0)
	{
		dec->configured = false;
		return false;
	}

	dec->configured = true;
	return true;
}

bool AAC_DecodeFrame(AACDecoder* dec, const void* input, int inputlen, void* output, int outputlen)
{
	if (!dec->configured)
	{
		return false;
	}

	NeAACDecFrameInfo frameInfo;
	NeAACDecDecode2(dec->handle, &frameInfo, (unsigned char*)input, inputlen, &output, outputlen);

	if (frameInfo.error != 0 || frameInfo.bytesconsumed != inputlen)
	{
		return false;
	}

	return true;
}

}
