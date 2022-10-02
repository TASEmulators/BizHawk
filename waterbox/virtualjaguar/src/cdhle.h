#ifndef __CDHLE_H__
#define __CDHLE_H__

#include <stdint.h>

void CDHLEInit(void);
void CDHLEReset(void);

void CDHLEHook(uint32_t which);
bool CDHLEJerryCallback(void);

#endif	// __CDHLE_H__
