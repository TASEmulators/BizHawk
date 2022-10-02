//
// Jaguar blitter implementation
//

#ifndef __BLITTER_H__
#define __BLITTER_H__

#include "memory.h"

void BlitterInit(void);
void BlitterReset(void);

uint8_t BlitterReadByte(uint32_t, uint32_t who = UNKNOWN);
uint16_t BlitterReadWord(uint32_t, uint32_t who = UNKNOWN);
uint32_t BlitterReadLong(uint32_t, uint32_t who = UNKNOWN);
void BlitterWriteByte(uint32_t, uint8_t, uint32_t who = UNKNOWN);
void BlitterWriteWord(uint32_t, uint16_t, uint32_t who = UNKNOWN);
void BlitterWriteLong(uint32_t, uint32_t, uint32_t who = UNKNOWN);

#endif	// __BLITTER_H__
