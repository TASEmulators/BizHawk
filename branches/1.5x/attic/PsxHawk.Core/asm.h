#pragma once

#include "types.h"

//jtype
#define OPCODE_J 2
#define OPCODE_JAL 3

//rtype
#define FUNCTION_JR 8
#define FUNCTION_BREAK 13

//itype
#define OPCODE_ORI 13
#define OPCODE_LUI 15

inline u32 ASM_JTYPE(const u32 opcode, const u32 target) 
{
	assert((target&3)==0);
	assert(opcode<64);
	const u32 _target = target & ~0xF0000000;
	return (opcode<<26)|(_target>>2);
}
inline u32 ASM_JAL(const u32 target) { return ASM_JTYPE(OPCODE_JAL,target); }

inline u32 ASM_RTYPE(const u32 function, const u32 rs, const u32 rt, const u32 rd, const u32 sa)
{
	assert(function<64);
	assert(rs<32);
	assert(rt<32);
	assert(rd<32);
	assert(sa<32);
	return (rs<<21)|(rt<<16)|(rd<<11)|(sa<<6)|function;
}
inline u32 ASM_JR(const u32 rs) { return ASM_RTYPE(FUNCTION_JR, rs, 0, 0, 0); }
inline u32 ASM_NOP() { return 0; }
inline u32 ASM_BREAK(const u32 code)
{
	assert(code<(1<<20));
	return (code<<6)|FUNCTION_BREAK;
}

inline u32 ASM_ITYPE(const u32 opcode, const u32 rs, const u32 rt, const u32 immediate)
{
	assert(opcode<64);
	assert(rs<32);
	assert(rt<32);
	assert(immediate<65536);
	return (opcode<<26)|(rs<<21)|(rt<<16)|immediate;
}

inline u32 ASM_LUI(const u32 rt, const u32 immediate) { return ASM_ITYPE(OPCODE_LUI,0,rt,immediate); }
inline u32 ASM_ORI(const u32 rt, const u32 rs, const u32 immediate)  { return ASM_ITYPE(OPCODE_ORI,rs,rt,immediate); }