#ifndef __JAGUAR_H__
#define __JAGUAR_H__

#include <stdint.h>
#include "memory.h"							// For "UNKNOWN" enum

void JaguarSetScreenBuffer(uint32_t * buffer);
void JaguarSetScreenPitch(uint32_t pitch);
void JaguarInit(void);
void JaguarReset(void);
void JaguarDone(void);

uint8_t JaguarReadByte(uint32_t offset, uint32_t who = UNKNOWN);
uint16_t JaguarReadWord(uint32_t offset, uint32_t who = UNKNOWN);
uint32_t JaguarReadLong(uint32_t offset, uint32_t who = UNKNOWN);
void JaguarWriteByte(uint32_t offset, uint8_t data, uint32_t who = UNKNOWN);
void JaguarWriteWord(uint32_t offset, uint16_t data, uint32_t who = UNKNOWN);
void JaguarWriteLong(uint32_t offset, uint32_t data, uint32_t who = UNKNOWN);

bool JaguarInterruptHandlerIsValid(uint32_t i);
void JaguarDasm(uint32_t offset, uint32_t qt);

void JaguarExecuteNew(void);

// Exports from JAGUAR.CPP

extern int32_t jaguarCPUInExec;
extern uint32_t jaguarMainROMCRC32, jaguarROMSize, jaguarRunAddress;
extern char * jaguarEepromsPath;
extern bool jaguarCartInserted;
extern bool bpmActive;
extern uint32_t bpmAddress1;

// Various clock rates

#define M68K_CLOCK_RATE_PAL		13296950
#define M68K_CLOCK_RATE_NTSC	13295453
#define RISC_CLOCK_RATE_PAL		26593900
#define RISC_CLOCK_RATE_NTSC	26590906

// Stuff for IRQ handling

#define ASSERT_LINE		1
#define CLEAR_LINE		0

//Temp debug stuff (will go away soon, so don't depend on these)

void DumpMainMemory(void);
uint8_t * GetRamPtr(void);

#endif	// __JAGUAR_H__
