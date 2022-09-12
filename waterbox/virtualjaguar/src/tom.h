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

// Virtual screen size stuff

// NB: This virtual width is for PWIDTH = 4
//#define VIRTUAL_SCREEN_WIDTH            320
//was:340, 330
#define VIRTUAL_SCREEN_WIDTH            326
#define VIRTUAL_SCREEN_HEIGHT_NTSC      240
#define VIRTUAL_SCREEN_HEIGHT_PAL       256

// 68000 Interrupt bit positions (enabled at $F000E0)

enum { IRQ_VIDEO = 0, IRQ_GPU, IRQ_OPFLAG, IRQ_TIMER, IRQ_DSP };

void TOMInit(void);
void TOMReset(void);
void TOMDone(void);

uint8_t TOMReadByte(uint32_t offset, uint32_t who = UNKNOWN);
uint16_t TOMReadWord(uint32_t offset, uint32_t who = UNKNOWN);
void TOMWriteByte(uint32_t offset, uint8_t data, uint32_t who = UNKNOWN);
void TOMWriteWord(uint32_t offset, uint16_t data, uint32_t who = UNKNOWN);

void TOMExecHalfline(uint16_t halfline, bool render);
uint32_t TOMGetVideoModeWidth(void);
uint32_t TOMGetVideoModeHeight(void);
uint8_t TOMGetVideoMode(void);
uint8_t * TOMGetRamPointer(void);
uint16_t TOMGetHDB(void);
uint16_t TOMGetVDB(void);
uint16_t TOMGetHC(void);
uint16_t TOMGetVP(void);
uint16_t TOMGetMEMCON1(void);
void TOMDumpIORegistersToLog(void);


int TOMIRQEnabled(int irq);
uint16_t TOMIRQControlReg(void);
void TOMSetIRQLatch(int irq, int enabled);
void TOMExecPIT(uint32_t cycles);
void TOMSetPendingJERRYInt(void);
void TOMSetPendingTimerInt(void);
void TOMSetPendingObjectInt(void);
void TOMSetPendingGPUInt(void);
void TOMSetPendingVideoInt(void);
void TOMResetPIT(void);

// Exported variables

extern uint32_t tomWidth;
extern uint32_t tomHeight;
extern uint8_t tomRam8[];
extern uint32_t tomTimerPrescaler;
extern uint32_t tomTimerDivider;
extern int32_t tomTimerCounter;

extern uint32_t screenPitch;
extern uint32_t * screenBuffer;

#endif	// __TOM_H__
