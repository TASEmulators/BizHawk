//
// DAC.H: Header file
//

#ifndef __DAC_H__
#define __DAC_H__

#include "memory.h"

void DACInit(void);
void DACReset(void);
void DACPauseAudioThread(bool state = true);
void DACDone(void);
//int GetCalculatedFrequency(void);

// DAC memory access

void DACWriteByte(uint32_t offset, uint8_t data, uint32_t who = UNKNOWN);
void DACWriteWord(uint32_t offset, uint16_t data, uint32_t who = UNKNOWN);
uint8_t DACReadByte(uint32_t offset, uint32_t who = UNKNOWN);
uint16_t DACReadWord(uint32_t offset, uint32_t who = UNKNOWN);


// DAC defines

#define SMODE_INTERNAL		0x01
#define SMODE_MODE			0x02
#define SMODE_WSEN			0x04
#define SMODE_RISING		0x08
#define SMODE_FALLING		0x10
#define SMODE_EVERYWORD		0x20

#endif	// __DAC_H__
