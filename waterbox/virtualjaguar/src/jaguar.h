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

void JaguarExecuteNew(void);

// Exports from JAGUAR.CPP

extern uint32_t jaguarMainROMCRC32, jaguarROMSize, jaguarRunAddress;
extern bool jaguarCartInserted;

// Various clock rates

#define M68K_CLOCK_RATE_PAL		13296950
#define M68K_CLOCK_RATE_NTSC	13295453
#define RISC_CLOCK_RATE_PAL		26593900
#define RISC_CLOCK_RATE_NTSC	26590906

// Stuff for IRQ handling

#define ASSERT_LINE		1
#define CLEAR_LINE		0

// Callbacks

extern void (*InputCallback)();

extern void (*ReadCallback)(uint32_t);
extern void (*WriteCallback)(uint32_t);
extern void (*ExecuteCallback)(uint32_t);

extern void (*TraceCallback)(uint32_t*);

#define MAYBE_CALLBACK(callback, ...) do { if (__builtin_expect(!!callback, false)) callback(__VA_ARGS__); } while (0)

#endif	// __JAGUAR_H__
