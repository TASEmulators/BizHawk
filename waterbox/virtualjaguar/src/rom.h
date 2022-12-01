//
// ROM.H
//
// ROM support
//

#ifndef __ROM_H__
#define __ROM_H__

#include <stdint.h>

// JST = Jaguar Software Type
enum { JST_NONE = 0, JST_ROM, JST_ALPINE, JST_ABS_TYPE1, JST_ABS_TYPE2, JST_JAGSERVER, JST_WTFOMGBBQ };

bool JaguarLoadROM(uint8_t * buffer, uint32_t size);
bool AlpineLoadROM(uint8_t * buffer, uint32_t size);
uint32_t ParseROMType(uint8_t * buffer, uint32_t size);

#endif	// __ROM_H__
