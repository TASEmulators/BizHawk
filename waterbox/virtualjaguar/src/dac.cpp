//
// DAC (really, Synchronous Serial Interface) Handler
//
// Originally by David Raingeard
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Rewritten by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
// JLH  04/30/2012  Changed SDL audio handler to run JERRY
//

#include "dac.h"

#include "cdrom.h"
#include "dsp.h"
#include "event.h"
#include "jerry.h"
#include "jaguar.h"
#include "m68000/m68kinterface.h"
#include "settings.h"

#define DAC_AUDIO_RATE		44100.0				// Set the audio rate to 44.1 KHz

// Jaguar memory locations

#define LTXD			0xF1A148
#define RTXD			0xF1A14C
#define LRXD			0xF1A148
#define RRXD			0xF1A14C
#define SCLK			0xF1A150
#define SMODE			0xF1A154

static uint16_t * sampleBuffer;
static uint32_t bufferIndex;
static uint32_t maxSamples;
extern bool audioEnabled;

static void DSPSampleCallback(void);

void DACInit(void)
{
	sclk = 19;
	maxSamples = 2048; // bleh
	DACReset();
}

//
// Reset the sound buffer FIFOs
//
void DACReset(void)
{
	ltxd = rtxd = 0;
	RemoveCallback(DSPSampleCallback);
	SetCallbackTime(DSPSampleCallback, 1000000.0 / DAC_AUDIO_RATE);
}

// Call this every frame with your buffer or NULL
// Returns amount of samples last outputted
uint32_t DACResetBuffer(void * buffer)
{
	sampleBuffer = (uint16_t*)buffer;
	uint32_t ret = bufferIndex / 2;
	bufferIndex = 0;
	return ret;
}

static void DSPSampleCallback(void)
{
	if (bufferIndex != maxSamples)
	{
		sampleBuffer[bufferIndex + 0] = audioEnabled ? ltxd : 0;
		sampleBuffer[bufferIndex + 1] = audioEnabled ? rtxd : 0;
		bufferIndex += 2;
	}

	SetCallbackTime(DSPSampleCallback, 1000000.0 / DAC_AUDIO_RATE);
}

//
// LTXD/RTXD/SCLK/SMODE ($F1A148/4C/50/54)
//
void DACWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	if (offset == SCLK + 3)
		DACWriteWord(offset - 3, (uint16_t)data);
}

void DACWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	if (offset == LTXD + 2)
	{
		ltxd = data;
	}
	else if (offset == RTXD + 2)
	{
		rtxd = data;
	}
	else if (offset == SCLK + 2)
	{
		sclk = data & 0xFF;
		JERRYI2SInterruptTimer = -1;
		RemoveCallback(JERRYI2SCallback);
		JERRYI2SCallback();
	}
	else if (offset == SMODE + 2)
	{
		smode = data;
	}
}

//
// LRXD/RRXD/SSTAT ($F1A148/4C/50)
//
uint8_t DACReadByte(uint32_t offset, uint32_t who)
{
	return 0xFF;
}

uint16_t DACReadWord(uint32_t offset, uint32_t who)
{
	if (offset == LRXD || offset == RRXD)
		return 0x0000;
	else if (offset == LRXD + 2)
		return lrxd;
	else if (offset == RRXD + 2)
		return rrxd;

	return 0xFFFF;
}

