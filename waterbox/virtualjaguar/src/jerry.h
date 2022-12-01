//
// JERRY.H: Header file
//

#ifndef __JERRY_H__
#define __JERRY_H__

#include "memory.h"

void JERRYInit(void);
void JERRYReset(void);

uint8_t JERRYReadByte(uint32_t offset, uint32_t who = UNKNOWN);
uint16_t JERRYReadWord(uint32_t offset, uint32_t who = UNKNOWN);
void JERRYWriteByte(uint32_t offset, uint8_t data, uint32_t who = UNKNOWN);
void JERRYWriteWord(uint32_t offset, uint16_t data, uint32_t who = UNKNOWN);

// 68000 Interrupt bit positions (enabled at $F10020)

enum { IRQ2_EXTERNAL=0x01, IRQ2_DSP=0x02, IRQ2_TIMER1=0x04, IRQ2_TIMER2=0x08, IRQ2_ASI=0x10, IRQ2_SSI=0x20 };

bool JERRYIRQEnabled(int irq);
void JERRYSetPendingIRQ(int irq);
void JERRYI2SCallback(void);

// External variables

extern int32_t JERRYI2SInterruptTimer;

#endif
