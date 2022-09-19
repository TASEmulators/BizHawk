//
// FILE.H
//
// File support
//

#ifndef __FILE_H__
#define __FILE_H__

#include <stdint.h>

enum FileType { FT_SOFTWARE=0, FT_EEPROM, FT_LABEL, FT_BOXART, FT_OVERLAY };
// JST = Jaguar Software Type
enum { JST_NONE = 0, JST_ROM, JST_ALPINE, JST_ABS_TYPE1, JST_ABS_TYPE2, JST_JAGSERVER, JST_WTFOMGBBQ };

bool JaguarLoadFile(uint8_t * buffer, uint32_t size);
bool AlpineLoadFile(uint8_t * buffer, uint32_t size);
uint32_t ParseFileType(uint8_t * buffer, uint32_t size);

#endif	// __FILE_H__
