#ifndef DTHUMB_H
#define DTHUMB_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define DTHUMB_STRING_LENGTH 80

// only 32-bit legacy architectures with THUMB support
typedef enum 
{
	ARMv4T, //ARM v4, THUMB v1
	ARMv5TE, //ARM v5, THUMB v2
	ARMv6, //ARM v6, THUMB v3
} ARMARCH;

uint32_t Disassemble_thumb(uint32_t code, char str[DTHUMB_STRING_LENGTH], ARMARCH tv);
void Disassemble_arm(uint32_t code, char str[DTHUMB_STRING_LENGTH], ARMARCH av);

#ifdef __cplusplus
}
#endif

#endif
