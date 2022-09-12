//
// mmu.h
//
// Jaguar Memory Manager Unit
//
// by James L. Hammons
//

#ifndef __MMU_H__
#define __MMU_H__

//#include "types.h"
#include "memory.h"

void MMUWrite8(uint32_t address, uint8_t data, uint32_t who = UNKNOWN);
void MMUWrite16(uint32_t address, uint16_t data, uint32_t who = UNKNOWN);
void MMUWrite32(uint32_t address, uint32_t data, uint32_t who = UNKNOWN);
void MMUWrite64(uint32_t address, uint64_t data, uint32_t who = UNKNOWN);
uint8_t MMURead8(uint32_t address, uint32_t who = UNKNOWN);
uint16_t MMURead16(uint32_t address, uint32_t who = UNKNOWN);
uint32_t MMURead32(uint32_t address, uint32_t who = UNKNOWN);
uint64_t MMURead64(uint32_t address, uint32_t who = UNKNOWN);

#endif	// __MMU_H__
