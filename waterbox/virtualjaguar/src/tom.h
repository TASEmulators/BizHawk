//
// TOM Header file
//

#ifndef __TOM_H__
#define __TOM_H__

#include "memory.h"

#define VIDEO_MODE_16BPP_CRY	0
#define VIDEO_MODE_24BPP_RGB	1
#define VIDEO_MODE_16BPP_DIRECT 2
#define VIDEO_MODE_16BPP_RGB	3

#define VIRTUAL_SCREEN_WIDTH            326
#define VIRTUAL_SCREEN_HEIGHT_NTSC      240
#define VIRTUAL_SCREEN_HEIGHT_PAL       256

#define MAX_SCREEN_WIDTH                (VIRTUAL_SCREEN_WIDTH * 4)

// 68000 Interrupt bit positions (enabled at $F000E0)

enum { IRQ_VIDEO = 0, IRQ_GPU, IRQ_OPFLAG, IRQ_TIMER, IRQ_DSP };

void TOMInit(void);
void TOMReset(void);

uint8_t TOMReadByte(uint32_t offset, uint32_t who = UNKNOWN);
uint16_t TOMReadWord(uint32_t offset, uint32_t who = UNKNOWN);
void TOMWriteByte(uint32_t offset, uint8_t data, uint32_t who = UNKNOWN);
void TOMWriteWord(uint32_t offset, uint16_t data, uint32_t who = UNKNOWN);

void TOMExecHalfline(uint16_t halfline);
uint16_t TOMGetHC(void);
uint16_t TOMGetMEMCON1(void);

int TOMIRQEnabled(int irq);
void TOMSetPendingObjectInt(void);
void TOMSetPendingGPUInt(void);
void TOMSetPendingVideoInt(void);

void TOMStartFrame(void);
void TOMBlit(uint32_t * framebuffer, int32_t & width, int32_t & height);

extern uint8_t tomRam8[];

#endif	// __TOM_H__
