//
// memtrack.h: Header file
//

#include <stdint.h>

void MTInit(void);
void MTReset(void);
void MTDone(void);

uint16_t MTReadWord(uint32_t addr);
uint32_t MTReadLong(uint32_t addr);
void MTWriteWord(uint32_t addr, uint16_t data);
void MTWriteLong(uint32_t addr, uint32_t data);

