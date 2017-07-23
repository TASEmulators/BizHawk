#ifndef z80_cpu_h
#define z80_cpu_h
#include "gb.h"

void GB_cpu_disassemble(GB_gameboy_t *gb, uint16_t pc, uint16_t count);
#ifdef GB_INTERNAL
void GB_cpu_run(GB_gameboy_t *gb);
#endif

#endif /* z80_cpu_h */
