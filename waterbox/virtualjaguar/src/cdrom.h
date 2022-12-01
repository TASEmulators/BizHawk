//
// CDROM.H
//

#ifndef __CDROM_H__
#define __CDROM_H__

#include "memory.h"

void CDROMInit(void);
void CDROMReset(void);

void BUTCHExec(uint32_t cycles);

uint8_t CDROMReadByte(uint32_t offset, uint32_t who = UNKNOWN);
uint16_t CDROMReadWord(uint32_t offset, uint32_t who = UNKNOWN);
void CDROMWriteByte(uint32_t offset, uint8_t data, uint32_t who = UNKNOWN);
void CDROMWriteWord(uint32_t offset, uint16_t data, uint32_t who = UNKNOWN);

#endif	// __CDROM_H__
