#ifndef __CPUEXTRA_H__
#define __CPUEXTRA_H__

#include "sysdeps.h"

typedef unsigned long cpuop_func(uint32_t);

struct cputbl
{
	cpuop_func * handler;
	int specific;
	uint16_t opcode;
};

extern uint16_t last_op_for_exception_3;
extern uint32_t last_addr_for_exception_3;
extern uint32_t last_fault_for_exception_3;

extern int OpcodeFamily;

extern int BusCyclePenalty;
extern int CurrentInstrCycles;

extern uint32_t get_disp_ea_000(uint32_t base, uint32_t dp);
extern void MakeSR(void);
extern void MakeFromSR(void);
extern void Exception(int, uint32_t, int);
extern int getDivu68kCycles(uint32_t dividend, uint16_t divisor);
extern int getDivs68kCycles(int32_t dividend, int16_t divisor);

#endif	// __CPUEXTRA_H__
