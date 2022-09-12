//
// DSP core
//
// Originally by David Raingeard
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Extensive cleanups/rewrites by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
// JLH  11/26/2011  Added fixes for LOAD/STORE alignment issues
//

#include "dsp.h"

#include <stdlib.h>
#include "dac.h"
#include "gpu.h"
#include "jagdasm.h"
#include "jaguar.h"
#include "jerry.h"
#include "log.h"
#include "m68000/m68kinterface.h"
//#include "memory.h"


// Seems alignment in loads & stores was off...
#define DSP_CORRECT_ALIGNMENT
//#define DSP_CORRECT_ALIGNMENT_STORE

//#define DSP_DEBUG
//#define DSP_DEBUG_IRQ
//#define DSP_DEBUG_PL2
//#define DSP_DEBUG_STALL
//#define DSP_DEBUG_CC
#define NEW_SCOREBOARD

// Disassembly definitions

#if 0
#define DSP_DIS_ABS
#define DSP_DIS_ADD
#define DSP_DIS_ADDC
#define DSP_DIS_ADDQ
#define DSP_DIS_ADDQMOD
#define DSP_DIS_ADDQT
#define DSP_DIS_AND
#define DSP_DIS_BCLR
#define DSP_DIS_BSET
#define DSP_DIS_BTST
#define DSP_DIS_CMP
#define DSP_DIS_CMPQ
#define DSP_DIS_IMACN
#define DSP_DIS_IMULT
#define DSP_DIS_IMULTN
#define DSP_DIS_ILLEGAL
#define DSP_DIS_JR
#define DSP_DIS_JUMP
#define DSP_DIS_LOAD
#define DSP_DIS_LOAD14I
#define DSP_DIS_LOAD14R
#define DSP_DIS_LOAD15I
#define DSP_DIS_LOAD15R
#define DSP_DIS_LOADB
#define DSP_DIS_LOADW
#define DSP_DIS_MOVE
#define DSP_DIS_MOVEI
#define DSP_DIS_MOVEQ
#define DSP_DIS_MOVEFA
#define DSP_DIS_MOVEPC								// Pipeline only!
#define DSP_DIS_MOVETA
#define DSP_DIS_MULT
#define DSP_DIS_NEG
#define DSP_DIS_NOP
#define DSP_DIS_NOT
#define DSP_DIS_OR
#define DSP_DIS_RESMAC
#define DSP_DIS_ROR
#define DSP_DIS_RORQ
#define DSP_DIS_SHARQ
#define DSP_DIS_SHLQ
#define DSP_DIS_SHRQ
#define DSP_DIS_STORE
#define DSP_DIS_STORE14I
#define DSP_DIS_STORE15I
#define DSP_DIS_STOREB
#define DSP_DIS_STOREW
#define DSP_DIS_SUB
#define DSP_DIS_SUBC
#define DSP_DIS_SUBQ
#define DSP_DIS_SUBQT
#define DSP_DIS_XOR
//*/
bool doDSPDis = false;
//bool doDSPDis = true;
#endif
bool doDSPDis = false;
//#define DSP_DIS_JR
//#define DSP_DIS_JUMP

/*
No dis yet:
+	subqt 4560
+	mult 1472
+	imultn 395024
+	resmac 395024
+	imacn 395024
+	addqmod 93328

dsp opcodes use:
+	add 1672497
+	addq 4366576
+	addqt 44405640
+	sub 94833
+	subq 111769
+	and 47416
+	btst 94521
+	bset 2277826
+	bclr 3223372
+	mult 47104
+	imult 237080
+	shlq 365464
+	shrq 141624
+	sharq 318368
+	cmp 45175078
+	move 2238994
+	moveq 335305
+	moveta 19
+	movefa 47406440
+	movei 1920664
+	loadb 94832
+	load 4031281
+	load_r15_indexed 284500
+	store 2161732
+	store_r15_indexed 47416
+	jump 3872424
+	jr 46386967
+	nop 3300029
+	load_r14_ri 1229448
*/

// Pipeline structures

const bool affectsScoreboard[64] =
{
	 true,  true,  true,  true,
	 true,  true,  true,  true,
	 true,  true,  true,  true,
	 true, false,  true,  true,

	 true,  true, false,  true,
	false,  true,  true,  true,
	 true,  true,  true,  true,
	 true,  true, false, false,

	 true,  true,  true,  true,
	false,  true,  true,  true,
	 true,  true,  true,  true,
	 true, false, false, false,

	 true, false, false,  true,
	false, false,  true,  true,
	 true, false,  true,  true,
	false, false, false,  true
};

struct PipelineStage
{
	uint16_t instruction;
	uint8_t opcode, operand1, operand2;
	uint32_t reg1, reg2, areg1, areg2;
	uint32_t result;
	uint8_t writebackRegister;
	// General memory store...
	uint32_t address;
	uint32_t value;
	uint8_t type;
};

#define TYPE_BYTE			0
#define TYPE_WORD			1
#define TYPE_DWORD			2
#define PIPELINE_STALL		64						// Set to # of opcodes + 1
#ifndef NEW_SCOREBOARD
bool scoreboard[32];
#else
uint8_t scoreboard[32];
#endif
uint8_t plPtrFetch, plPtrRead, plPtrExec, plPtrWrite;
PipelineStage pipeline[4];
bool IMASKCleared = false;

// DSP flags (old--have to get rid of this crap)

#define CINT0FLAG			0x00200
#define CINT1FLAG			0x00400
#define CINT2FLAG			0x00800
#define CINT3FLAG			0x01000
#define CINT4FLAG			0x02000
#define CINT04FLAGS			(CINT0FLAG | CINT1FLAG | CINT2FLAG | CINT3FLAG | CINT4FLAG)
#define CINT5FLAG			0x20000		/* DSP only */

// DSP_FLAGS bits

#define ZERO_FLAG		0x00001
#define CARRY_FLAG		0x00002
#define NEGA_FLAG		0x00004
#define IMASK			0x00008
#define INT_ENA0		0x00010
#define INT_ENA1		0x00020
#define INT_ENA2		0x00040
#define INT_ENA3		0x00080
#define INT_ENA4		0x00100
#define INT_CLR0		0x00200
#define INT_CLR1		0x00400
#define INT_CLR2		0x00800
#define INT_CLR3		0x01000
#define INT_CLR4		0x02000
#define REGPAGE			0x04000
#define DMAEN			0x08000
#define INT_ENA5		0x10000
#define INT_CLR5		0x20000

// DSP_CTRL bits

#define DSPGO			0x00001
#define CPUINT			0x00002
#define DSPINT0			0x00004
#define SINGLE_STEP		0x00008
#define SINGLE_GO		0x00010
// Bit 5 is unused!
#define INT_LAT0		0x00040
#define INT_LAT1		0x00080
#define INT_LAT2		0x00100
#define INT_LAT3		0x00200
#define INT_LAT4		0x00400
#define BUS_HOG			0x00800
#define VERSION			0x0F000
#define INT_LAT5		0x10000

extern uint32_t jaguar_mainRom_crc32;

// Is opcode 62 *really* a NOP? Seems like it...
static void dsp_opcode_abs(void);
static void dsp_opcode_add(void);
static void dsp_opcode_addc(void);
static void dsp_opcode_addq(void);
static void dsp_opcode_addqmod(void);
static void dsp_opcode_addqt(void);
static void dsp_opcode_and(void);
static void dsp_opcode_bclr(void);
static void dsp_opcode_bset(void);
static void dsp_opcode_btst(void);
static void dsp_opcode_cmp(void);
static void dsp_opcode_cmpq(void);
static void dsp_opcode_div(void);
static void dsp_opcode_imacn(void);
static void dsp_opcode_imult(void);
static void dsp_opcode_imultn(void);
static void dsp_opcode_jr(void);
static void dsp_opcode_jump(void);
static void dsp_opcode_load(void);
static void dsp_opcode_loadb(void);
static void dsp_opcode_loadw(void);
static void dsp_opcode_load_r14_indexed(void);
static void dsp_opcode_load_r14_ri(void);
static void dsp_opcode_load_r15_indexed(void);
static void dsp_opcode_load_r15_ri(void);
static void dsp_opcode_mirror(void);
static void dsp_opcode_mmult(void);
static void dsp_opcode_move(void);
static void dsp_opcode_movei(void);
static void dsp_opcode_movefa(void);
static void dsp_opcode_move_pc(void);
static void dsp_opcode_moveq(void);
static void dsp_opcode_moveta(void);
static void dsp_opcode_mtoi(void);
static void dsp_opcode_mult(void);
static void dsp_opcode_neg(void);
static void dsp_opcode_nop(void);
static void dsp_opcode_normi(void);
static void dsp_opcode_not(void);
static void dsp_opcode_or(void);
static void dsp_opcode_resmac(void);
static void dsp_opcode_ror(void);
static void dsp_opcode_rorq(void);
static void dsp_opcode_xor(void);
static void dsp_opcode_sat16s(void);
static void dsp_opcode_sat32s(void);
static void dsp_opcode_sh(void);
static void dsp_opcode_sha(void);
static void dsp_opcode_sharq(void);
static void dsp_opcode_shlq(void);
static void dsp_opcode_shrq(void);
static void dsp_opcode_store(void);
static void dsp_opcode_storeb(void);
static void dsp_opcode_storew(void);
static void dsp_opcode_store_r14_indexed(void);
static void dsp_opcode_store_r14_ri(void);
static void dsp_opcode_store_r15_indexed(void);
static void dsp_opcode_store_r15_ri(void);
static void dsp_opcode_sub(void);
static void dsp_opcode_subc(void);
static void dsp_opcode_subq(void);
static void dsp_opcode_subqmod(void);
static void dsp_opcode_subqt(void);
static void dsp_opcode_illegal(void);

/*uint8_t dsp_opcode_cycles[64] =
{
	3,  3,  3,  3,  3,  3,  3,  3,
	3,  3,  3,  3,  3,  3,  3,  3,
	3,  3,  1,  3,  1, 18,  3,  3,
	3,  3,  3,  3,  3,  3,  3,  3,
	3,  3,  2,  2,  2,  2,  3,  4,
	5,  4,  5,  6,  6,  1,  1,  1,
	1,  2,  2,  2,  1,  1,  9,  3,
	3,  1,  6,  6,  2,  2,  3,  3
};//*/
//Here's a QnD kludge...
//This is wrong, wrong, WRONG, but it seems to work for the time being...
//(That is, it fixes Flip Out which relies on GPU timing rather than semaphores. Bad developers! Bad!)
//What's needed here is a way to take pipeline effects into account (including pipeline stalls!)...
// Yup, without cheating like this, the sound in things like Rayman, FACTS, &
// Tripper Getem get starved for time and sounds like crap. So we have to figure
// out how to fix that. :-/
uint8_t dsp_opcode_cycles[64] =
{
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  9,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  2,
	2,  2,  2,  3,  3,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  4,  1,
	1,  1,  3,  3,  1,  1,  1,  1
};//*/

void (* dsp_opcode[64])() =
{
	dsp_opcode_add,					dsp_opcode_addc,				dsp_opcode_addq,				dsp_opcode_addqt,
	dsp_opcode_sub,					dsp_opcode_subc,				dsp_opcode_subq,				dsp_opcode_subqt,
	dsp_opcode_neg,					dsp_opcode_and,					dsp_opcode_or,					dsp_opcode_xor,
	dsp_opcode_not,					dsp_opcode_btst,				dsp_opcode_bset,				dsp_opcode_bclr,
	dsp_opcode_mult,				dsp_opcode_imult,				dsp_opcode_imultn,				dsp_opcode_resmac,
	dsp_opcode_imacn,				dsp_opcode_div,					dsp_opcode_abs,					dsp_opcode_sh,
	dsp_opcode_shlq,				dsp_opcode_shrq,				dsp_opcode_sha,					dsp_opcode_sharq,
	dsp_opcode_ror,					dsp_opcode_rorq,				dsp_opcode_cmp,					dsp_opcode_cmpq,
	dsp_opcode_subqmod,				dsp_opcode_sat16s,				dsp_opcode_move,				dsp_opcode_moveq,
	dsp_opcode_moveta,				dsp_opcode_movefa,				dsp_opcode_movei,				dsp_opcode_loadb,
	dsp_opcode_loadw,				dsp_opcode_load,				dsp_opcode_sat32s,				dsp_opcode_load_r14_indexed,
	dsp_opcode_load_r15_indexed,	dsp_opcode_storeb,				dsp_opcode_storew,				dsp_opcode_store,
	dsp_opcode_mirror,				dsp_opcode_store_r14_indexed,	dsp_opcode_store_r15_indexed,	dsp_opcode_move_pc,
	dsp_opcode_jump,				dsp_opcode_jr,					dsp_opcode_mmult,				dsp_opcode_mtoi,
	dsp_opcode_normi,				dsp_opcode_nop,					dsp_opcode_load_r14_ri,			dsp_opcode_load_r15_ri,
	dsp_opcode_store_r14_ri,		dsp_opcode_store_r15_ri,		dsp_opcode_illegal,				dsp_opcode_addqmod,
};

uint32_t dsp_opcode_use[65];

const char * dsp_opcode_str[65]=
{
	"add",				"addc",				"addq",				"addqt",
	"sub",				"subc",				"subq",				"subqt",
	"neg",				"and",				"or",				"xor",
	"not",				"btst",				"bset",				"bclr",
	"mult",				"imult",			"imultn",			"resmac",
	"imacn",			"div",				"abs",				"sh",
	"shlq",				"shrq",				"sha",				"sharq",
	"ror",				"rorq",				"cmp",				"cmpq",
	"subqmod",			"sat16s",			"move",				"moveq",
	"moveta",			"movefa",			"movei",			"loadb",
	"loadw",			"load",				"sat32s",			"load_r14_indexed",
	"load_r15_indexed",	"storeb",			"storew",			"store",
	"mirror",			"store_r14_indexed","store_r15_indexed","move_pc",
	"jump",				"jr",				"mmult",			"mtoi",
	"normi",			"nop",				"load_r14_ri",		"load_r15_ri",
	"store_r14_ri",		"store_r15_ri",		"illegal",			"addqmod",
	"STALL"
};

uint32_t dsp_pc;
static uint64_t dsp_acc;								// 40 bit register, NOT 32!
static uint32_t dsp_remain;
static uint32_t dsp_modulo;
static uint32_t dsp_flags;
static uint32_t dsp_matrix_control;
static uint32_t dsp_pointer_to_matrix;
static uint32_t dsp_data_organization;
uint32_t dsp_control;
static uint32_t dsp_div_control;
static uint8_t dsp_flag_z, dsp_flag_n, dsp_flag_c;
static uint32_t * dsp_reg = NULL, * dsp_alternate_reg = NULL;
uint32_t dsp_reg_bank_0[32], dsp_reg_bank_1[32];

static uint32_t dsp_opcode_first_parameter;
static uint32_t dsp_opcode_second_parameter;

#define DSP_RUNNING			(dsp_control & 0x01)

#define RM					dsp_reg[dsp_opcode_first_parameter]
#define RN					dsp_reg[dsp_opcode_second_parameter]
#define ALTERNATE_RM		dsp_alternate_reg[dsp_opcode_first_parameter]
#define ALTERNATE_RN		dsp_alternate_reg[dsp_opcode_second_parameter]
#define IMM_1				dsp_opcode_first_parameter
#define IMM_2				dsp_opcode_second_parameter

#define CLR_Z				(dsp_flag_z = 0)
#define CLR_ZN				(dsp_flag_z = dsp_flag_n = 0)
#define CLR_ZNC				(dsp_flag_z = dsp_flag_n = dsp_flag_c = 0)
#define SET_Z(r)			(dsp_flag_z = ((r) == 0))
#define SET_N(r)			(dsp_flag_n = (((uint32_t)(r) >> 31) & 0x01))
#define SET_C_ADD(a,b)		(dsp_flag_c = ((uint32_t)(b) > (uint32_t)(~(a))))
#define SET_C_SUB(a,b)		(dsp_flag_c = ((uint32_t)(b) > (uint32_t)(a)))
#define SET_ZN(r)			SET_N(r); SET_Z(r)
#define SET_ZNC_ADD(a,b,r)	SET_N(r); SET_Z(r); SET_C_ADD(a,b)
#define SET_ZNC_SUB(a,b,r)	SET_N(r); SET_Z(r); SET_C_SUB(a,b)

uint32_t dsp_convert_zero[32] = {
	32, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
	17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
};

uint8_t dsp_branch_condition_table[32 * 8];
static uint16_t mirror_table[65536];
static uint8_t dsp_ram_8[0x2000];

#define BRANCH_CONDITION(x)		dsp_branch_condition_table[(x) + ((jaguar_flags & 7) << 5)]

static uint32_t dsp_in_exec = 0;
static uint32_t dsp_releaseTimeSlice_flag = 0;

FILE * dsp_fp;

#ifdef DSP_DEBUG_CC
// Comparison core vars (used only for core comparison! :-)
static uint64_t count = 0;
static uint8_t ram1[0x2000], ram2[0x2000];
static uint32_t regs1[64], regs2[64];
static uint32_t ctrl1[14], ctrl2[14];
#endif

// Private function prototypes

void DSPDumpRegisters(void);
void DSPDumpDisassembly(void);
void FlushDSPPipeline(void);


void dsp_reset_stats(void)
{
	for(int i=0; i<65; i++)
		dsp_opcode_use[i] = 0;
}


void DSPReleaseTimeslice(void)
{
//This does absolutely nothing!!! !!! FIX !!!
	dsp_releaseTimeSlice_flag = 1;
}


void dsp_build_branch_condition_table(void)
{
	// Fill in the mirror table
	for(int i=0; i<65536; i++)
	{
		mirror_table[i] = ((i >> 15) & 0x0001) | ((i >> 13) & 0x0002)
			| ((i >> 11) & 0x0004) | ((i >> 9)  & 0x0008)
			| ((i >> 7)  & 0x0010) | ((i >> 5)  & 0x0020)
			| ((i >> 3)  & 0x0040) | ((i >> 1)  & 0x0080)
			| ((i << 1)  & 0x0100) | ((i << 3)  & 0x0200)
			| ((i << 5)  & 0x0400) | ((i << 7)  & 0x0800)
			| ((i << 9)  & 0x1000) | ((i << 11) & 0x2000)
			| ((i << 13) & 0x4000) | ((i << 15) & 0x8000);
	}

	// Fill in the condition table
	for(int i=0; i<8; i++)
	{
		for(int j=0; j<32; j++)
		{
			int result = 1;

			if ((j & 1) && (i & ZERO_FLAG))
				result = 0;

			if ((j & 2) && (!(i & ZERO_FLAG)))
				result = 0;

			if ((j & 4) && (i & (CARRY_FLAG << (j >> 4))))
				result = 0;

			if ((j & 8) && (!(i & (CARRY_FLAG << (j >> 4)))))
				result = 0;

			dsp_branch_condition_table[i * 32 + j] = result;
		}
	}
}


uint8_t DSPReadByte(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	if (offset >= 0xF1A000 && offset <= 0xF1A0FF)
		WriteLog("DSP: ReadByte--Attempt to read from DSP register file by %s!\n", whoName[who]);
// battlemorph
//	if ((offset==0xF1CFE0)||(offset==0xF1CFE2))
//		return(0xffff);
	// mutant penguin
/*	if ((jaguar_mainRom_crc32==0xbfd751a4)||(jaguar_mainRom_crc32==0x053efaf9))
	{
		if (offset==0xF1CFE0)
			return(0xff);
	}*/
	if (offset >= DSP_WORK_RAM_BASE && offset <= (DSP_WORK_RAM_BASE + 0x1FFF))
		return dsp_ram_8[offset - DSP_WORK_RAM_BASE];

	if (offset >= DSP_CONTROL_RAM_BASE && offset <= (DSP_CONTROL_RAM_BASE + 0x1F))
	{
		uint32_t data = DSPReadLong(offset & 0xFFFFFFFC, who);

		if ((offset & 0x03) == 0)
			return (data >> 24);
		else if ((offset & 0x03) == 1)
			return ((data >> 16) & 0xFF);
		else if ((offset & 0x03) == 2)
			return ((data >> 8) & 0xFF);
		else if ((offset & 0x03) == 3)
			return (data & 0xFF);
	}

	return JaguarReadByte(offset, who);
}


uint16_t DSPReadWord(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	if (offset >= 0xF1A000 && offset <= 0xF1A0FF)
		WriteLog("DSP: ReadWord--Attempt to read from DSP register file by %s!\n", whoName[who]);
	//???
	offset &= 0xFFFFFFFE;

	if (offset >= DSP_WORK_RAM_BASE && offset <= DSP_WORK_RAM_BASE+0x1FFF)
	{
		offset -= DSP_WORK_RAM_BASE;
/*		uint16_t data = (((uint16_t)dsp_ram_8[offset])<<8)|((uint16_t)dsp_ram_8[offset+1]);
		return data;*/
		return GET16(dsp_ram_8, offset);
	}
	else if ((offset>=DSP_CONTROL_RAM_BASE)&&(offset<DSP_CONTROL_RAM_BASE+0x20))
	{
		uint32_t data = DSPReadLong(offset & 0xFFFFFFFC, who);

		if (offset & 0x03)
			return data & 0xFFFF;
		else
			return data >> 16;
	}

	return JaguarReadWord(offset, who);
}


uint32_t DSPReadLong(uint32_t offset, uint32_t who/*=UNKNOWN*/)
{
	if (offset >= 0xF1A000 && offset <= 0xF1A0FF)
		WriteLog("DSP: ReadLong--Attempt to read from DSP register file by %s!\n", whoName[who]);

	// ??? WHY ???
	offset &= 0xFFFFFFFC;
/*if (offset == 0xF1BCF4)
{
	WriteLog("DSPReadLong: Reading from 0xF1BCF4... -> %08X [%02X %02X %02X %02X][%04X %04X]\n", GET32(dsp_ram_8, 0x0CF4), dsp_ram_8[0x0CF4], dsp_ram_8[0x0CF5], dsp_ram_8[0x0CF6], dsp_ram_8[0x0CF7], JaguarReadWord(0xF1BCF4, DSP), JaguarReadWord(0xF1BCF6, DSP));
	DSPDumpDisassembly();
}*/
	if (offset >= DSP_WORK_RAM_BASE && offset <= DSP_WORK_RAM_BASE + 0x1FFF)
	{
		offset -= DSP_WORK_RAM_BASE;
		return GET32(dsp_ram_8, offset);
	}
//NOTE: Didn't return DSP_ACCUM!!!
//Mebbe it's not 'spose to! Yes, it is!
	if (offset >= DSP_CONTROL_RAM_BASE && offset <= DSP_CONTROL_RAM_BASE + 0x23)
	{
		offset &= 0x3F;
		switch (offset)
		{
		case 0x00:
			dsp_flags = (dsp_flags & 0xFFFFFFF8) | (dsp_flag_n << 2) | (dsp_flag_c << 1) | dsp_flag_z;
			return dsp_flags & 0xFFFFC1FF;
		case 0x04: return dsp_matrix_control;
		case 0x08: return dsp_pointer_to_matrix;
		case 0x0C: return dsp_data_organization;
		case 0x10: return dsp_pc;
		case 0x14: return dsp_control;
		case 0x18: return dsp_modulo;
		case 0x1C: return dsp_remain;
		case 0x20:
			return (int32_t)((int8_t)(dsp_acc >> 32));	// Top 8 bits of 40-bit accumulator, sign extended
		}
		// unaligned long read-- !!! FIX !!!
		return 0xFFFFFFFF;
	}

	return JaguarReadLong(offset, who);
}


void DSPWriteByte(uint32_t offset, uint8_t data, uint32_t who/*=UNKNOWN*/)
{
	if (offset >= 0xF1A000 && offset <= 0xF1A0FF)
		WriteLog("DSP: WriteByte--Attempt to write to DSP register file by %s!\n", whoName[who]);

	if ((offset >= DSP_WORK_RAM_BASE) && (offset < DSP_WORK_RAM_BASE + 0x2000))
	{
		offset -= DSP_WORK_RAM_BASE;
		dsp_ram_8[offset] = data;
//This is rather stupid! !!! FIX !!!
/*		if (dsp_in_exec == 0)
		{
			m68k_end_timeslice();
			dsp_releaseTimeslice();
		}*/
		return;
	}
	if ((offset >= DSP_CONTROL_RAM_BASE) && (offset < DSP_CONTROL_RAM_BASE + 0x20))
	{
		uint32_t reg = offset & 0x1C;
		int bytenum = offset & 0x03;

		if ((reg >= 0x1C) && (reg <= 0x1F))
			dsp_div_control = (dsp_div_control & (~(0xFF << (bytenum << 3)))) | (data << (bytenum << 3));
		else
		{
//This looks funky. !!! FIX !!!
			uint32_t old_data = DSPReadLong(offset&0xFFFFFFC, who);
			bytenum = 3 - bytenum; // convention motorola !!!
			old_data = (old_data & (~(0xFF << (bytenum << 3)))) | (data << (bytenum << 3));
			DSPWriteLong(offset & 0xFFFFFFC, old_data, who);
		}
		return;
	}
//	WriteLog("dsp: writing %.2x at 0x%.8x\n",data,offset);
//Should this *ever* happen??? Shouldn't we be saying "unknown" here???
// Well, yes, it can. There are 3 MMU users after all: 68K, GPU & DSP...!
	JaguarWriteByte(offset, data, who);
}


void DSPWriteWord(uint32_t offset, uint16_t data, uint32_t who/*=UNKNOWN*/)
{
	if (offset >= 0xF1A000 && offset <= 0xF1A0FF)
		WriteLog("DSP: WriteWord--Attempt to write to DSP register file by %s!\n", whoName[who]);
	offset &= 0xFFFFFFFE;
/*if (offset == 0xF1BCF4)
{
	WriteLog("DSPWriteWord: Writing to 0xF1BCF4... %04X -> %04X\n", GET16(dsp_ram_8, 0x0CF4), data);
}*/
//	WriteLog("dsp: writing %.4x at 0x%.8x\n",data,offset);
	if ((offset >= DSP_WORK_RAM_BASE) && (offset < DSP_WORK_RAM_BASE+0x2000))
	{
/*if (offset == 0xF1B2F4)
{
	WriteLog("DSP: %s is writing %04X at location 0xF1B2F4 (DSP_PC: %08X)...\n", whoName[who], data, dsp_pc);
}//*/
		offset -= DSP_WORK_RAM_BASE;
		dsp_ram_8[offset] = data >> 8;
		dsp_ram_8[offset+1] = data & 0xFF;
//This is rather stupid! !!! FIX !!!
/*		if (dsp_in_exec == 0)
		{
//			WriteLog("dsp: writing %.4x at 0x%.8x\n",data,offset+DSP_WORK_RAM_BASE);
			m68k_end_timeslice();
			dsp_releaseTimeslice();
		}*/
//CC only!
#ifdef DSP_DEBUG_CC
SET16(ram1, offset, data),
SET16(ram2, offset, data);
#endif
//!!!!!!!!
		return;
	}
	else if ((offset >= DSP_CONTROL_RAM_BASE) && (offset < DSP_CONTROL_RAM_BASE+0x20))
	{
		if ((offset & 0x1C) == 0x1C)
		{
			if (offset & 0x03)
				dsp_div_control = (dsp_div_control & 0xFFFF0000) | (data & 0xFFFF);
			else
				dsp_div_control = (dsp_div_control & 0xFFFF) | ((data & 0xFFFF) << 16);
		}
		else
		{
			uint32_t old_data = DSPReadLong(offset & 0xFFFFFFC, who);

			if (offset & 0x03)
				old_data = (old_data & 0xFFFF0000) | (data & 0xFFFF);
			else
				old_data = (old_data & 0xFFFF) | ((data & 0xFFFF) << 16);

			DSPWriteLong(offset & 0xFFFFFFC, old_data, who);
		}

		return;
	}

	JaguarWriteWord(offset, data, who);
}


//bool badWrite = false;
void DSPWriteLong(uint32_t offset, uint32_t data, uint32_t who/*=UNKNOWN*/)
{
	if (offset >= 0xF1A000 && offset <= 0xF1A0FF)
		WriteLog("DSP: WriteLong--Attempt to write to DSP register file by %s!\n", whoName[who]);
	// ??? WHY ???
	offset &= 0xFFFFFFFC;
/*if (offset == 0xF1BCF4)
{
	WriteLog("DSPWriteLong: Writing to 0xF1BCF4... %08X -> %08X\n", GET32(dsp_ram_8, 0x0CF4), data);
}*/
//	WriteLog("dsp: writing %.8x at 0x%.8x\n",data,offset);
	if (offset >= DSP_WORK_RAM_BASE && offset <= DSP_WORK_RAM_BASE + 0x1FFF)
	{
/*if (offset == 0xF1BE2C)
{
	WriteLog("DSP: %s is writing %08X at location 0xF1BE2C (DSP_PC: %08X)...\n", whoName[who], data, dsp_pc - 2);
}//*/
		offset -= DSP_WORK_RAM_BASE;
		SET32(dsp_ram_8, offset, data);
//CC only!
#ifdef DSP_DEBUG_CC
SET32(ram1, offset, data),
SET32(ram2, offset, data);
#endif
//!!!!!!!!
		return;
	}
	else if (offset >= DSP_CONTROL_RAM_BASE && offset <= (DSP_CONTROL_RAM_BASE + 0x1F))
	{
		offset &= 0x1F;
		switch (offset)
		{
		case 0x00:
		{
#ifdef DSP_DEBUG
			WriteLog("DSP: Writing %08X to DSP_FLAGS by %s (REGPAGE is %sset)...\n", data, whoName[who], (dsp_flags & REGPAGE ? "" : "not "));
#endif
//			bool IMASKCleared = (dsp_flags & IMASK) && !(data & IMASK);
			IMASKCleared = (dsp_flags & IMASK) && !(data & IMASK);
			// NOTE: According to the JTRM, writing a 1 to IMASK has no effect; only the
			//       IRQ logic can set it. So we mask it out here to prevent problems...
			dsp_flags = data & (~IMASK);
			dsp_flag_z = dsp_flags & 0x01;
			dsp_flag_c = (dsp_flags >> 1) & 0x01;
			dsp_flag_n = (dsp_flags >> 2) & 0x01;
			DSPUpdateRegisterBanks();
			dsp_control &= ~((dsp_flags & CINT04FLAGS) >> 3);
			dsp_control &= ~((dsp_flags & CINT5FLAG) >> 1);
			break;
		}
		case 0x04:
			dsp_matrix_control = data;
			break;
		case 0x08:
			// According to JTRM, only lines 2-11 are addressable, the rest being
			// hardwired to $F1Bxxx.
			dsp_pointer_to_matrix = 0xF1B000 | (data & 0x000FFC);
			break;
		case 0x0C:
			dsp_data_organization = data;
			break;
		case 0x10:
			dsp_pc = data;
#ifdef DSP_DEBUG
			WriteLog("DSP: Setting DSP PC to %08X by %s%s\n", dsp_pc, whoName[who], (DSP_RUNNING ? " (DSP is RUNNING!)" : ""));//*/
#endif
//CC only!
#ifdef DSP_DEBUG_CC
if (who != DSP)
	ctrl1[0] = ctrl2[0] = data;
#endif
//!!!!!!!!
			break;
		case 0x14:
		{
//#ifdef DSP_DEBUG
WriteLog("Write to DSP CTRL by %s: %08X (DSP PC=$%08X)\n", whoName[who], data, dsp_pc);
//#endif
			bool wasRunning = DSP_RUNNING;
//			uint32_t dsp_was_running = DSP_RUNNING;
			// Check for DSP -> CPU interrupt
			if (data & CPUINT)
			{
#ifdef DSP_DEBUG
				WriteLog("DSP: DSP -> CPU interrupt\n");
#endif

#warning "!!! DSP IRQs that go to the 68K have to be routed thru TOM !!! FIX !!!"
				if (JERRYIRQEnabled(IRQ2_DSP))
				{
					JERRYSetPendingIRQ(IRQ2_DSP);
					DSPReleaseTimeslice();
					m68k_set_irq(2);			// Set 68000 IPL 2...
				}
				data &= ~CPUINT;
			}
			// Check for CPU -> DSP interrupt
			if (data & DSPINT0)
			{
#ifdef DSP_DEBUG
				WriteLog("DSP: CPU -> DSP interrupt\n");
#endif
				m68k_end_timeslice();
				DSPReleaseTimeslice();
				DSPSetIRQLine(DSPIRQ_CPU, ASSERT_LINE);
				data &= ~DSPINT0;
			}
			// single stepping
			if (data & SINGLE_STEP)
			{
//				WriteLog("DSP: Asked to perform a single step (single step is %senabled)\n", (data & 0x8 ? "" : "not "));
			}

			// Protect writes to VERSION and the interrupt latches...
			uint32_t mask = VERSION | INT_LAT0 | INT_LAT1 | INT_LAT2 | INT_LAT3 | INT_LAT4 | INT_LAT5;
			dsp_control = (dsp_control & mask) | (data & ~mask);
//CC only!
#ifdef DSP_DEBUG_CC
if (who != DSP)
	ctrl1[8] = ctrl2[8] = dsp_control;
#endif
//!!!!!!!!

			// if dsp wasn't running but is now running
			// execute a few cycles
//This is just plain wrong, wrong, WRONG!
#ifndef DSP_SINGLE_STEPPING
/*			if (!dsp_was_running && DSP_RUNNING)
			{
				DSPExec(200);
			}*/
#else
//This is WRONG! !!! FIX !!!
			if (dsp_control & 0x18)
				DSPExec(1);
#endif
#ifdef DSP_DEBUG
if (DSP_RUNNING)
	WriteLog(" --> Starting to run at %08X by %s...", dsp_pc, whoName[who]);
else
	WriteLog(" --> Stopped by %s! (DSP PC: %08X)", whoName[who], dsp_pc);
WriteLog("\n");
#endif	// DSP_DEBUG
//This isn't exactly right either--we don't know if it was the M68K or the DSP writing here...
// !!! FIX !!! [DONE]
			if (DSP_RUNNING)
			{
				if (who == M68K)
					m68k_end_timeslice();
				else if (who == DSP)
					DSPReleaseTimeslice();

				if (!wasRunning)
					FlushDSPPipeline();
//DSPDumpDisassembly();
			}
			break;
		}
		case 0x18:
WriteLog("DSP: Modulo data %08X written by %s.\n", data, whoName[who]);
			dsp_modulo = data;
			break;
		case 0x1C:
			dsp_div_control = data;
			break;
//		default:   // unaligned long read
				   //__asm int 3
		}
		return;
	}

//We don't have to break this up like this! We CAN do 32 bit writes!
//	JaguarWriteWord(offset, (data>>16) & 0xFFFF, DSP);
//	JaguarWriteWord(offset+2, data & 0xFFFF, DSP);
//if (offset > 0xF1FFFF)
//	badWrite = true;
	JaguarWriteLong(offset, data, who);
}


//
// Update the DSP register file pointers depending on REGPAGE bit
//
void DSPUpdateRegisterBanks(void)
{
	int bank = (dsp_flags & REGPAGE);

	if (dsp_flags & IMASK)
		bank = 0;							// IMASK forces main bank to be bank 0

	if (bank)
		dsp_reg = dsp_reg_bank_1, dsp_alternate_reg = dsp_reg_bank_0;
	else
		dsp_reg = dsp_reg_bank_0, dsp_alternate_reg = dsp_reg_bank_1;

#ifdef DSP_DEBUG_IRQ
	WriteLog("DSP: Register bank #%s active.\n", (bank ? "1" : "0"));
#endif
}


//
// Check for and handle any asserted DSP IRQs
//
void DSPHandleIRQs(void)
{
	if (dsp_flags & IMASK) 							// Bail if we're already inside an interrupt
		return;

	// Get the active interrupt bits (latches) & interrupt mask (enables)
	uint32_t bits = ((dsp_control >> 10) & 0x20) | ((dsp_control >> 6) & 0x1F),
		mask = ((dsp_flags >> 11) & 0x20) | ((dsp_flags >> 4) & 0x1F);

//	WriteLog("dsp: bits=%.2x mask=%.2x\n",bits,mask);
	bits &= mask;

	if (!bits)										// Bail if nothing is enabled
		return;

	int which = 0;									// Determine which interrupt

	if (bits & 0x01)
		which = 0;
	if (bits & 0x02)
		which = 1;
	if (bits & 0x04)
		which = 2;
	if (bits & 0x08)
		which = 3;
	if (bits & 0x10)
		which = 4;
	if (bits & 0x20)
		which = 5;

#ifdef DSP_DEBUG_IRQ
	WriteLog("DSP: Generating interrupt #%i...", which);
#endif
//temp... !!!!!
//if (which == 0)	doDSPDis = true;

	// NOTE: Since the actual Jaguar hardware injects the code sequence below
	//       directly into the pipeline, it has the side effect of ensuring that the
	//       instruction interrupted also gets to do its writeback. We simulate that
	//       behavior here.
/*	if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
	{
		if (pipeline[plPtrWrite].writebackRegister != 0xFF)
			dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;

		if (affectsScoreboard[pipeline[plPtrWrite].opcode])
			scoreboard[pipeline[plPtrWrite].operand2] = false;
	}//*/
//This should be execute (or should it?--not sure now!)
//Actually, the way this is called now, this should be correct (i.e., the plPtrs advance,
//and what just executed is now in the Write position...). So why didn't it do the
//writeback into register 0?
#ifdef DSP_DEBUG_IRQ
WriteLog("--> Pipeline dump [DSP_PC=%08X]...\n", dsp_pc);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister, dsp_opcode_str[pipeline[plPtrWrite].opcode]);
#endif
	if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
	{
		if (pipeline[plPtrWrite].writebackRegister != 0xFF)
		{
			if (pipeline[plPtrWrite].writebackRegister != 0xFE)
				dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;
			else
			{
				if (pipeline[plPtrWrite].type == TYPE_BYTE)
					JaguarWriteByte(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
				else if (pipeline[plPtrWrite].type == TYPE_WORD)
					JaguarWriteWord(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
				else
					JaguarWriteLong(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
			}
		}

#ifndef NEW_SCOREBOARD
		if (affectsScoreboard[pipeline[plPtrWrite].opcode])
			scoreboard[pipeline[plPtrWrite].operand2] = false;
#else
//Yup, sequential MOVEQ # problem fixing (I hope!)...
		if (affectsScoreboard[pipeline[plPtrWrite].opcode])
			if (scoreboard[pipeline[plPtrWrite].operand2])
				scoreboard[pipeline[plPtrWrite].operand2]--;
#endif
	}

	dsp_flags |= IMASK;
//CC only!
#ifdef DSP_DEBUG_CC
ctrl2[4] = dsp_flags;
#endif
//!!!!!!!!
	DSPUpdateRegisterBanks();
#ifdef DSP_DEBUG_IRQ
//	WriteLog(" [PC will return to %08X, R31 = %08X]\n", dsp_pc, dsp_reg[31]);
	WriteLog(" [PC will return to %08X, R31 = %08X]\n", dsp_pc - (pipeline[plPtrExec].opcode == 38 ? 6 : (pipeline[plPtrExec].opcode == PIPELINE_STALL ? 0 : 2)), dsp_reg[31]);
#endif

	// subqt  #4,r31		; pre-decrement stack pointer
	// move   pc,r30		; address of interrupted code
	// store  r30,(r31)     ; store return address
	dsp_reg[31] -= 4;
//CC only!
#ifdef DSP_DEBUG_CC
regs2[31] -= 4;
#endif
//!!!!!!!!
//This might not come back to the right place if the instruction was MOVEI #. !!! FIX !!!
//But, then again, JTRM says that it adds two regardless of what the instruction was...
//It missed the place that it was supposed to come back to, so this is WRONG!
//
// Look at the pipeline when an interrupt occurs (instructions of foo, bar, baz):
//
// R -> baz		(<- PC points here)
// E -> bar		(when it should point here!)
// W -> foo
//
// 'Foo' just completed executing as per above. PC is pointing to the instruction 'baz'
// which means (assuming they're all 2 bytes long) that the code below will come back on
// instruction 'baz' instead of 'bar' which is the next instruction to execute in the
// instruction stream...

//	DSPWriteLong(dsp_reg[31], dsp_pc - 2, DSP);
	DSPWriteLong(dsp_reg[31], dsp_pc - 2 - (pipeline[plPtrExec].opcode == 38 ? 6 : (pipeline[plPtrExec].opcode == PIPELINE_STALL ? 0 : 2)), DSP);
//CC only!
#ifdef DSP_DEBUG_CC
SET32(ram2, regs2[31] - 0xF1B000, dsp_pc - 2 - (pipeline[plPtrExec].opcode == 38 ? 6 : (pipeline[plPtrExec].opcode == PIPELINE_STALL ? 0 : 2)));
#endif
//!!!!!!!!

	// movei  #service_address,r30  ; pointer to ISR entry
	// jump  (r30)					; jump to ISR
	// nop
	dsp_pc = dsp_reg[30] = DSP_WORK_RAM_BASE + (which * 0x10);
//CC only!
#ifdef DSP_DEBUG_CC
ctrl2[0] = regs2[30] = dsp_pc;
#endif
//!!!!!!!!
	FlushDSPPipeline();
}


//
// Non-pipelined version...
//
void DSPHandleIRQsNP(void)
{
//CC only!
#ifdef DSP_DEBUG_CC
		memcpy(dsp_ram_8, ram1, 0x2000);
		memcpy(dsp_reg_bank_0, regs1, 32 * 4);
		memcpy(dsp_reg_bank_1, &regs1[32], 32 * 4);
		dsp_pc					= ctrl1[0];
		dsp_acc					= ctrl1[1];
		dsp_remain				= ctrl1[2];
		dsp_modulo				= ctrl1[3];
		dsp_flags				= ctrl1[4];
		dsp_matrix_control		= ctrl1[5];
		dsp_pointer_to_matrix	= ctrl1[6];
		dsp_data_organization	= ctrl1[7];
		dsp_control				= ctrl1[8];
		dsp_div_control			= ctrl1[9];
		IMASKCleared			= ctrl1[10];
		dsp_flag_z				= ctrl1[11];
		dsp_flag_n				= ctrl1[12];
		dsp_flag_c				= ctrl1[13];
DSPUpdateRegisterBanks();
#endif
//!!!!!!!!
	if (dsp_flags & IMASK) 							// Bail if we're already inside an interrupt
		return;

	// Get the active interrupt bits (latches) & interrupt mask (enables)
	uint32_t bits = ((dsp_control >> 10) & 0x20) | ((dsp_control >> 6) & 0x1F),
		mask = ((dsp_flags >> 11) & 0x20) | ((dsp_flags >> 4) & 0x1F);

//	WriteLog("dsp: bits=%.2x mask=%.2x\n",bits,mask);
	bits &= mask;

	if (!bits)										// Bail if nothing is enabled
		return;

	int which = 0;									// Determine which interrupt
	if (bits & 0x01)
		which = 0;
	if (bits & 0x02)
		which = 1;
	if (bits & 0x04)
		which = 2;
	if (bits & 0x08)
		which = 3;
	if (bits & 0x10)
		which = 4;
	if (bits & 0x20)
		which = 5;

	dsp_flags |= IMASK;		// Force Bank #0
//CC only!
#ifdef DSP_DEBUG_CC
ctrl1[4] = dsp_flags;
#endif
//!!!!!!!!
#ifdef DSP_DEBUG_IRQ
	WriteLog("DSP: Bank 0: R30=%08X, R31=%08X\n", dsp_reg_bank_0[30], dsp_reg_bank_0[31]);
	WriteLog("DSP: Bank 1: R30=%08X, R31=%08X\n", dsp_reg_bank_1[30], dsp_reg_bank_1[31]);
#endif
	DSPUpdateRegisterBanks();
#ifdef DSP_DEBUG_IRQ
	WriteLog("DSP: Bank 0: R30=%08X, R31=%08X\n", dsp_reg_bank_0[30], dsp_reg_bank_0[31]);
	WriteLog("DSP: Bank 1: R30=%08X, R31=%08X\n", dsp_reg_bank_1[30], dsp_reg_bank_1[31]);
#endif

#ifdef DSP_DEBUG_IRQ
	WriteLog("DSP: Generating interrupt #%i...", which);
	WriteLog(" [PC will return to %08X, R31 = %08X]\n", dsp_pc, dsp_reg[31]);
#endif

	// subqt  #4,r31		; pre-decrement stack pointer
	// move   pc,r30		; address of interrupted code
	// store  r30,(r31)     ; store return address
	dsp_reg[31] -= 4;
	dsp_reg[30] = dsp_pc - 2; // -2 because we've executed the instruction already

//CC only!
#ifdef DSP_DEBUG_CC
regs1[31] -= 4;
#endif
//!!!!!!!!
//	DSPWriteLong(dsp_reg[31], dsp_pc - 2, DSP);
	DSPWriteLong(dsp_reg[31], dsp_reg[30], DSP);
//CC only!
#ifdef DSP_DEBUG_CC
SET32(ram1, regs1[31] - 0xF1B000, dsp_pc - 2);
#endif
//!!!!!!!!

	// movei  #service_address,r30  ; pointer to ISR entry
	// jump  (r30)					; jump to ISR
	// nop
	dsp_pc = dsp_reg[30] = DSP_WORK_RAM_BASE + (which * 0x10);
//CC only!
#ifdef DSP_DEBUG_CC
ctrl1[0] = regs1[30] = dsp_pc;
#endif
//!!!!!!!!
}


//
// Set the specified DSP IRQ line to a given state
//
void DSPSetIRQLine(int irqline, int state)
{
//NOTE: This doesn't take INT_LAT5 into account. !!! FIX !!!
	uint32_t mask = INT_LAT0 << irqline;
	dsp_control &= ~mask;							// Clear the latch bit
//CC only!
#ifdef DSP_DEBUG_CC
ctrl1[8] = ctrl2[8] = dsp_control;
#endif
//!!!!!!!!

	if (state)
	{
		dsp_control |= mask;						// Set the latch bit
#warning !!! No checking done to see if we are using pipelined DSP or not !!!
//		DSPHandleIRQs();
		DSPHandleIRQsNP();
//CC only!
#ifdef DSP_DEBUG_CC
ctrl1[8] = ctrl2[8] = dsp_control;
DSPHandleIRQsNP();
#endif
//!!!!!!!!
	}

	// Not sure if this is correct behavior, but according to JTRM,
	// the IRQ output of JERRY is fed to this IRQ in the GPU...
// Not sure this is right--DSP interrupts seem to be different from the JERRY interrupts!
//	GPUSetIRQLine(GPUIRQ_DSP, ASSERT_LINE);
}


bool DSPIsRunning(void)
{
	return (DSP_RUNNING ? true : false);
}


void DSPInit(void)
{
//	memory_malloc_secure((void **)&dsp_ram_8, 0x2000, "DSP work RAM");
//	memory_malloc_secure((void **)&dsp_reg_bank_0, 32 * sizeof(int32_t), "DSP bank 0 regs");
//	memory_malloc_secure((void **)&dsp_reg_bank_1, 32 * sizeof(int32_t), "DSP bank 1 regs");

	dsp_build_branch_condition_table();
	DSPReset();
}


void DSPReset(void)
{
	dsp_pc				  = 0x00F1B000;
	dsp_acc				  = 0x00000000;
	dsp_remain			  = 0x00000000;
	dsp_modulo			  = 0xFFFFFFFF;
	dsp_flags			  = 0x00040000;
	dsp_matrix_control    = 0x00000000;
	dsp_pointer_to_matrix = 0x00000000;
	dsp_data_organization = 0xFFFFFFFF;
	dsp_control			  = 0x00002000;				// Report DSP version 2
	dsp_div_control		  = 0x00000000;
	dsp_in_exec			  = 0;

	dsp_reg = dsp_reg_bank_0;
	dsp_alternate_reg = dsp_reg_bank_1;

	for(int i=0; i<32; i++)
		dsp_reg[i] = dsp_alternate_reg[i] = 0x00000000;

	CLR_ZNC;
	IMASKCleared = false;
	FlushDSPPipeline();
	dsp_reset_stats();

	// Contents of local RAM are quasi-stable; we simulate this by randomizing RAM contents
	for(uint32_t i=0; i<8192; i+=4)
		*((uint32_t *)(&dsp_ram_8[i])) = rand();
}


void DSPDumpDisassembly(void)
{
	char buffer[512];

	WriteLog("\n---[DSP code at 00F1B000]---------------------------\n");
	uint32_t j = 0xF1B000;

	while (j <= 0xF1CFFF)
	{
		uint32_t oldj = j;
		j += dasmjag(JAGUAR_DSP, buffer, j);
		WriteLog("\t%08X: %s\n", oldj, buffer);
	}
}


void DSPDumpRegisters(void)
{
//Shoud add modulus, etc to dump here...
	WriteLog("\n---[DSP flags: NCZ %d%d%d, DSP PC: %08X]------------\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, dsp_pc);
	WriteLog("\nRegisters bank 0\n");

	for(int j=0; j<8; j++)
	{
		WriteLog("\tR%02i = %08X R%02i = %08X R%02i = %08X R%02i = %08X\n",
			(j << 2) + 0, dsp_reg_bank_0[(j << 2) + 0],
			(j << 2) + 1, dsp_reg_bank_0[(j << 2) + 1],
			(j << 2) + 2, dsp_reg_bank_0[(j << 2) + 2],
			(j << 2) + 3, dsp_reg_bank_0[(j << 2) + 3]);
	}

	WriteLog("Registers bank 1\n");

	for(int j=0; j<8; j++)
	{
		WriteLog("\tR%02i = %08X R%02i = %08X R%02i = %08X R%02i = %08X\n",
			(j << 2) + 0, dsp_reg_bank_1[(j << 2) + 0],
			(j << 2) + 1, dsp_reg_bank_1[(j << 2) + 1],
			(j << 2) + 2, dsp_reg_bank_1[(j << 2) + 2],
			(j << 2) + 3, dsp_reg_bank_1[(j << 2) + 3]);
	}
}


void DSPDone(void)
{
	WriteLog("\n\n---------------------------------------------------------------------\n");
	WriteLog("DSP I/O Registers\n");
	WriteLog("---------------------------------------------------------------------\n");
	WriteLog("F1%04X   (D_FLAGS): $%06X\n", 0xA100, (dsp_flags & 0xFFFFFFF8) | (dsp_flag_n << 2) | (dsp_flag_c << 1) | dsp_flag_z);
	WriteLog("F1%04X    (D_MTXC): $%04X\n", 0xA104, dsp_matrix_control);
	WriteLog("F1%04X    (D_MTXA): $%04X\n", 0xA108, dsp_pointer_to_matrix);
	WriteLog("F1%04X     (D_END): $%02X\n", 0xA10C, dsp_data_organization);
	WriteLog("F1%04X      (D_PC): $%06X\n", 0xA110, dsp_pc);
	WriteLog("F1%04X    (D_CTRL): $%06X\n", 0xA114, dsp_control);
	WriteLog("F1%04X     (D_MOD): $%08X\n", 0xA118, dsp_modulo);
	WriteLog("F1%04X  (D_REMAIN): $%08X\n", 0xA11C, dsp_remain);
	WriteLog("F1%04X (D_DIVCTRL): $%02X\n", 0xA11C, dsp_div_control);
	WriteLog("F1%04X   (D_MACHI): $%02X\n", 0xA120, (dsp_acc >> 32) & 0xFF);
	WriteLog("---------------------------------------------------------------------\n\n\n");

	WriteLog("DSP: Stopped at PC=%08X dsp_modulo=%08X (dsp was%s running)\n", dsp_pc, dsp_modulo, (DSP_RUNNING ? "" : "n't"));
	WriteLog("DSP: %sin interrupt handler\n", (dsp_flags & IMASK ? "" : "not "));

	// Get the active interrupt bits
	int bits = ((dsp_control >> 10) & 0x20) | ((dsp_control >> 6) & 0x1F);
	// Get the interrupt mask
	int mask = ((dsp_flags >> 11) & 0x20) | ((dsp_flags >> 4) & 0x1F);

	WriteLog("DSP: pending=$%X enabled=$%X (%s%s%s%s%s%s)\n", bits, mask,
		(mask & 0x01 ? "CPU " : ""), (mask & 0x02 ? "I2S " : ""),
		(mask & 0x04 ? "Timer0 " : ""), (mask & 0x08 ? "Timer1 " : ""),
		(mask & 0x10 ? "Ext0 " : ""), (mask & 0x20 ? "Ext1" : ""));
	DSPDumpRegisters();
	WriteLog("\n");

	static char buffer[512];
	int j = DSP_WORK_RAM_BASE;

	while (j <= 0xF1CFFF)
	{
		uint32_t oldj = j;
		j += dasmjag(JAGUAR_DSP, buffer, j);
		WriteLog("\t%08X: %s\n", oldj, buffer);
	}

	WriteLog("DSP opcodes use:\n");

	for(int i=0; i<64; i++)
	{
		if (dsp_opcode_use[i])
			WriteLog("\t%s %i\n", dsp_opcode_str[i], dsp_opcode_use[i]);
	}
}



//
// DSP comparison core...
//
#ifdef DSP_DEBUG_CC
static uint16_t lastExec;
void DSPExecComp(int32_t cycles)
{
	while (cycles > 0 && DSP_RUNNING)
	{
		// Load up vars for non-pipelined core
		memcpy(dsp_ram_8, ram1, 0x2000);
		memcpy(dsp_reg_bank_0, regs1, 32 * 4);
		memcpy(dsp_reg_bank_1, &regs1[32], 32 * 4);
		dsp_pc					= ctrl1[0];
		dsp_acc					= ctrl1[1];
		dsp_remain				= ctrl1[2];
		dsp_modulo				= ctrl1[3];
		dsp_flags				= ctrl1[4];
		dsp_matrix_control		= ctrl1[5];
		dsp_pointer_to_matrix	= ctrl1[6];
		dsp_data_organization	= ctrl1[7];
		dsp_control				= ctrl1[8];
		dsp_div_control			= ctrl1[9];
		IMASKCleared			= ctrl1[10];
		dsp_flag_z				= ctrl1[11];
		dsp_flag_n				= ctrl1[12];
		dsp_flag_c				= ctrl1[13];
DSPUpdateRegisterBanks();

		// Decrement cycles based on non-pipelined core...
		uint16_t instr1 = DSPReadWord(dsp_pc, DSP);
		cycles -= dsp_opcode_cycles[instr1 >> 10];

//WriteLog("\tAbout to execute non-pipelined core on tick #%u (DSP_PC=%08X)...\n", (uint32_t)count, dsp_pc);
		DSPExec(1);									// Do *one* instruction

		// Save vars
		memcpy(ram1, dsp_ram_8, 0x2000);
		memcpy(regs1, dsp_reg_bank_0, 32 * 4);
		memcpy(&regs1[32], dsp_reg_bank_1, 32 * 4);
		ctrl1[0]  = dsp_pc;
		ctrl1[1]  = dsp_acc;
		ctrl1[2]  = dsp_remain;
		ctrl1[3]  = dsp_modulo;
		ctrl1[4]  = dsp_flags;
		ctrl1[5]  = dsp_matrix_control;
		ctrl1[6]  = dsp_pointer_to_matrix;
		ctrl1[7]  = dsp_data_organization;
		ctrl1[8]  = dsp_control;
		ctrl1[9]  = dsp_div_control;
		ctrl1[10] = IMASKCleared;
		ctrl1[11] = dsp_flag_z;
		ctrl1[12] = dsp_flag_n;
		ctrl1[13] = dsp_flag_c;

		// Load up vars for pipelined core
		memcpy(dsp_ram_8, ram2, 0x2000);
		memcpy(dsp_reg_bank_0, regs2, 32 * 4);
		memcpy(dsp_reg_bank_1, &regs2[32], 32 * 4);
		dsp_pc					= ctrl2[0];
		dsp_acc					= ctrl2[1];
		dsp_remain				= ctrl2[2];
		dsp_modulo				= ctrl2[3];
		dsp_flags				= ctrl2[4];
		dsp_matrix_control		= ctrl2[5];
		dsp_pointer_to_matrix	= ctrl2[6];
		dsp_data_organization	= ctrl2[7];
		dsp_control				= ctrl2[8];
		dsp_div_control			= ctrl2[9];
		IMASKCleared			= ctrl2[10];
		dsp_flag_z				= ctrl2[11];
		dsp_flag_n				= ctrl2[12];
		dsp_flag_c				= ctrl2[13];
DSPUpdateRegisterBanks();

//WriteLog("\tAbout to execute pipelined core on tick #%u (DSP_PC=%08X)...\n", (uint32_t)count, dsp_pc);
		DSPExecP2(1);								// Do *one* instruction

		// Save vars
		memcpy(ram2, dsp_ram_8, 0x2000);
		memcpy(regs2, dsp_reg_bank_0, 32 * 4);
		memcpy(&regs2[32], dsp_reg_bank_1, 32 * 4);
		ctrl2[0]  = dsp_pc;
		ctrl2[1]  = dsp_acc;
		ctrl2[2]  = dsp_remain;
		ctrl2[3]  = dsp_modulo;
		ctrl2[4]  = dsp_flags;
		ctrl2[5]  = dsp_matrix_control;
		ctrl2[6]  = dsp_pointer_to_matrix;
		ctrl2[7]  = dsp_data_organization;
		ctrl2[8]  = dsp_control;
		ctrl2[9]  = dsp_div_control;
		ctrl2[10] = IMASKCleared;
		ctrl2[11] = dsp_flag_z;
		ctrl2[12] = dsp_flag_n;
		ctrl2[13] = dsp_flag_c;

		if (instr1 != lastExec)
		{
//			WriteLog("\nCores diverged at instruction tick #%u!\nAttemping to synchronize...\n\n", count);

//			uint32_t ppc = ctrl2[0] - (pipeline[plPtrExec].opcode == 38 ? 6 : (pipeline[plPtrExec].opcode == PIPELINE_STALL ? 0 : 2)) - (pipeline[plPtrWrite].opcode == 38 ? 6 : (pipeline[plPtrWrite].opcode == PIPELINE_STALL ? 0 : 2));
//WriteLog("[DSP_PC1=%08X, DSP_PC2=%08X]\n", ctrl1[0], ppc);
//			if (ctrl1[0] < ppc)						// P ran ahead of NP
//How to test this crap???
//			if (1)
			{
		DSPExecP2(1);								// Do one more instruction

		// Save vars
		memcpy(ram2, dsp_ram_8, 0x2000);
		memcpy(regs2, dsp_reg_bank_0, 32 * 4);
		memcpy(&regs2[32], dsp_reg_bank_1, 32 * 4);
		ctrl2[0]  = dsp_pc;
		ctrl2[1]  = dsp_acc;
		ctrl2[2]  = dsp_remain;
		ctrl2[3]  = dsp_modulo;
		ctrl2[4]  = dsp_flags;
		ctrl2[5]  = dsp_matrix_control;
		ctrl2[6]  = dsp_pointer_to_matrix;
		ctrl2[7]  = dsp_data_organization;
		ctrl2[8]  = dsp_control;
		ctrl2[9]  = dsp_div_control;
		ctrl2[10] = IMASKCleared;
		ctrl2[11] = dsp_flag_z;
		ctrl2[12] = dsp_flag_n;
		ctrl2[13] = dsp_flag_c;
			}
//			else									// NP ran ahead of P
		if (instr1 != lastExec)						// Must be the other way...

			{
		// Load up vars for non-pipelined core
		memcpy(dsp_ram_8, ram1, 0x2000);
		memcpy(dsp_reg_bank_0, regs1, 32 * 4);
		memcpy(dsp_reg_bank_1, &regs1[32], 32 * 4);
		dsp_pc					= ctrl1[0];
		dsp_acc					= ctrl1[1];
		dsp_remain				= ctrl1[2];
		dsp_modulo				= ctrl1[3];
		dsp_flags				= ctrl1[4];
		dsp_matrix_control		= ctrl1[5];
		dsp_pointer_to_matrix	= ctrl1[6];
		dsp_data_organization	= ctrl1[7];
		dsp_control				= ctrl1[8];
		dsp_div_control			= ctrl1[9];
		IMASKCleared			= ctrl1[10];
		dsp_flag_z				= ctrl1[11];
		dsp_flag_n				= ctrl1[12];
		dsp_flag_c				= ctrl1[13];
DSPUpdateRegisterBanks();

for(int k=0; k<2; k++)
{
		// Decrement cycles based on non-pipelined core...
		instr1 = DSPReadWord(dsp_pc, DSP);
		cycles -= dsp_opcode_cycles[instr1 >> 10];

//WriteLog("\tAbout to execute non-pipelined core on tick #%u (DSP_PC=%08X)...\n", (uint32_t)count, dsp_pc);
		DSPExec(1);									// Do *one* instruction
}

		// Save vars
		memcpy(ram1, dsp_ram_8, 0x2000);
		memcpy(regs1, dsp_reg_bank_0, 32 * 4);
		memcpy(&regs1[32], dsp_reg_bank_1, 32 * 4);
		ctrl1[0]  = dsp_pc;
		ctrl1[1]  = dsp_acc;
		ctrl1[2]  = dsp_remain;
		ctrl1[3]  = dsp_modulo;
		ctrl1[4]  = dsp_flags;
		ctrl1[5]  = dsp_matrix_control;
		ctrl1[6]  = dsp_pointer_to_matrix;
		ctrl1[7]  = dsp_data_organization;
		ctrl1[8]  = dsp_control;
		ctrl1[9]  = dsp_div_control;
		ctrl1[10] = IMASKCleared;
		ctrl1[11] = dsp_flag_z;
		ctrl1[12] = dsp_flag_n;
		ctrl1[13] = dsp_flag_c;
			}
		}

		if (instr1 != lastExec)
		{
			WriteLog("\nCores diverged at instruction tick #%u!\nStopped!\n\n", count);

			WriteLog("Instruction for non-pipelined core: %04X\n", instr1);
			WriteLog("Instruction for pipelined core: %04X\n", lastExec);

			log_done();
			exit(1);
		}

		count++;
	}
}
#endif


//
// DSP execution core
//
//static bool R20Set = false, tripwire = false;
//static uint32_t pcQueue[32], ptrPCQ = 0;
void DSPExec(int32_t cycles)
{
#ifdef DSP_SINGLE_STEPPING
	if (dsp_control & 0x18)
	{
		cycles = 1;
		dsp_control &= ~0x10;
	}
#endif
//There is *no* good reason to do this here!
//	DSPHandleIRQs();
	dsp_releaseTimeSlice_flag = 0;
	dsp_in_exec++;

	while (cycles > 0 && DSP_RUNNING)
	{
/*extern uint32_t totalFrames;
//F1B2F6: LOAD   (R14+$04), R24 [NCZ:001, R14+$04=00F20018, R24=FFFFFFFF] -> Jaguar: Unknown word read at 00F20018 by DSP (M68K PC=00E32E)
//-> 43 + 1 + 24 -> $2B + $01 + $18 -> 101011 00001 11000 -> 1010 1100 0011 1000 -> AC38
//C470 -> 1100 0100 0111 0000 -> 110001 00011 10000 -> 49, 3, 16 -> STORE R16, (R14+$0C)
//F1B140:
if (totalFrames >= 377 && GET16(dsp_ram_8, 0x0002F6) == 0xAC38 && dsp_pc == 0xF1B140)
{
	doDSPDis = true;
	WriteLog("Starting disassembly at frame #%u...\n", totalFrames);
}
if (dsp_pc == 0xF1B092)
	doDSPDis = false;//*/
/*if (dsp_pc == 0xF1B140)
	doDSPDis = true;//*/

		if (IMASKCleared)						// If IMASK was cleared,
		{
#ifdef DSP_DEBUG_IRQ
			WriteLog("DSP: Finished interrupt. PC=$%06X\n", dsp_pc);
#endif
			DSPHandleIRQsNP();					// See if any other interrupts are pending!
			IMASKCleared = false;
		}

/*if (badWrite)
{
	WriteLog("\nDSP: Encountered bad write in Atari Synth module. PC=%08X, R15=%08X\n", dsp_pc, dsp_reg[15]);
	for(int i=0; i<80; i+=4)
		WriteLog("     %08X: %08X\n", dsp_reg[15]+i, JaguarReadLong(dsp_reg[15]+i));
	WriteLog("\n");
}//*/
/*if (dsp_pc == 0xF1B55E)
{
	WriteLog("DSP: At $F1B55E--R15 = %08X at %u ms%s...\n", dsp_reg[15], SDL_GetTicks(), (dsp_flags & IMASK ? " (inside interrupt)" : ""));
}//*/
/*if (dsp_pc == 0xF1B7D2)	// Start here???
	doDSPDis = true;
pcQueue[ptrPCQ++] = dsp_pc;
ptrPCQ %= 32;*/
		uint16_t opcode = DSPReadWord(dsp_pc, DSP);
		uint32_t index = opcode >> 10;
		dsp_opcode_first_parameter = (opcode >> 5) & 0x1F;
		dsp_opcode_second_parameter = opcode & 0x1F;
		dsp_pc += 2;
		dsp_opcode[index]();
		dsp_opcode_use[index]++;
		cycles -= dsp_opcode_cycles[index];
/*if (dsp_reg_bank_0[20] == 0xF1A100 & !R20Set)
{
	WriteLog("DSP: R20 set to $F1A100 at %u ms%s...\n", SDL_GetTicks(), (dsp_flags & IMASK ? " (inside interrupt)" : ""));
	R20Set = true;
}
if (dsp_reg_bank_0[20] != 0xF1A100 && R20Set)
{
	WriteLog("DSP: R20 corrupted at %u ms from starting%s!\nAborting!\n", SDL_GetTicks(), (dsp_flags & IMASK ? " (inside interrupt)" : ""));
	DSPDumpRegisters();
	DSPDumpDisassembly();
	exit(1);
}
if ((dsp_pc < 0xF1B000 || dsp_pc > 0xF1CFFE) && !tripwire)
{
	char buffer[512];
	WriteLog("DSP: Jumping outside of DSP RAM at %u ms. Register dump:\n", SDL_GetTicks());
	DSPDumpRegisters();
	tripwire = true;
	WriteLog("\nBacktrace:\n");
	for(int i=0; i<32; i++)
	{
		dasmjag(JAGUAR_DSP, buffer, pcQueue[(ptrPCQ + i) % 32]);
		WriteLog("\t%08X: %s\n", pcQueue[(ptrPCQ + i) % 32], buffer);
	}
	WriteLog("\n");
}*/
	}

	dsp_in_exec--;
}


//
// DSP opcode handlers
//

// There is a problem here with interrupt handlers the JUMP and JR instructions that
// can cause trouble because an interrupt can occur *before* the instruction following the
// jump can execute... !!! FIX !!!
static void dsp_opcode_jump(void)
{
#ifdef DSP_DIS_JUMP
const char * condition[32] =
{	"T", "nz", "z", "???", "nc", "nc nz", "nc z", "???", "c", "c nz",
	"c z", "???", "???", "???", "???", "???", "???", "???", "???",
	"???", "nn", "nn nz", "nn z", "???", "n", "n nz", "n z", "???",
	"???", "???", "???", "F" };
	if (doDSPDis)
		WriteLog("%06X: JUMP   %s, (R%02u) [NCZ:%u%u%u, R%02u=%08X] ", dsp_pc-2, condition[IMM_2], IMM_1, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM);
#endif
	// normalize flags
/*	dsp_flag_c=dsp_flag_c?1:0;
	dsp_flag_z=dsp_flag_z?1:0;
	dsp_flag_n=dsp_flag_n?1:0;*/
	// KLUDGE: Used by BRANCH_CONDITION
	uint32_t jaguar_flags = (dsp_flag_n << 2) | (dsp_flag_c << 1) | dsp_flag_z;

	if (BRANCH_CONDITION(IMM_2))
	{
#ifdef DSP_DIS_JUMP
	if (doDSPDis)
		WriteLog("Branched!\n");
#endif
		uint32_t delayed_pc = RM;
		DSPExec(1);
		dsp_pc = delayed_pc;
	}
#ifdef DSP_DIS_JUMP
	else
		if (doDSPDis)
			WriteLog("Branch NOT taken.\n");
#endif
}


static void dsp_opcode_jr(void)
{
#ifdef DSP_DIS_JR
const char * condition[32] =
{	"T", "nz", "z", "???", "nc", "nc nz", "nc z", "???", "c", "c nz",
	"c z", "???", "???", "???", "???", "???", "???", "???", "???",
	"???", "nn", "nn nz", "nn z", "???", "n", "n nz", "n z", "???",
	"???", "???", "???", "F" };
	if (doDSPDis)
		WriteLog("%06X: JR     %s, %06X [NCZ:%u%u%u] ", dsp_pc-2, condition[IMM_2], dsp_pc+((IMM_1 & 0x10 ? 0xFFFFFFF0 | IMM_1 : IMM_1) * 2), dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
	// normalize flags
/*	dsp_flag_c=dsp_flag_c?1:0;
	dsp_flag_z=dsp_flag_z?1:0;
	dsp_flag_n=dsp_flag_n?1:0;*/
	// KLUDGE: Used by BRANCH_CONDITION
	uint32_t jaguar_flags = (dsp_flag_n << 2) | (dsp_flag_c << 1) | dsp_flag_z;

	if (BRANCH_CONDITION(IMM_2))
	{
#ifdef DSP_DIS_JR
	if (doDSPDis)
		WriteLog("Branched!\n");
#endif
		int32_t offset = (IMM_1 & 0x10 ? 0xFFFFFFF0 | IMM_1 : IMM_1);		// Sign extend IMM_1
		int32_t delayed_pc = dsp_pc + (offset * 2);
		DSPExec(1);
		dsp_pc = delayed_pc;
	}
#ifdef DSP_DIS_JR
	else
		if (doDSPDis)
			WriteLog("Branch NOT taken.\n");
#endif
}


static void dsp_opcode_add(void)
{
#ifdef DSP_DIS_ADD
	if (doDSPDis)
		WriteLog("%06X: ADD    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	uint32_t res = RN + RM;
	SET_ZNC_ADD(RN, RM, res);
	RN = res;
#ifdef DSP_DIS_ADD
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_addc(void)
{
#ifdef DSP_DIS_ADDC
	if (doDSPDis)
		WriteLog("%06X: ADDC   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	uint32_t res = RN + RM + dsp_flag_c;
	uint32_t carry = dsp_flag_c;
//	SET_ZNC_ADD(RN, RM, res); //???BUG??? Yes!
	SET_ZNC_ADD(RN + carry, RM, res);
//	SET_ZNC_ADD(RN, RM + carry, res);
	RN = res;
#ifdef DSP_DIS_ADDC
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_addq(void)
{
#ifdef DSP_DIS_ADDQ
	if (doDSPDis)
		WriteLog("%06X: ADDQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t r1 = dsp_convert_zero[IMM_1];
	uint32_t res = RN + r1;
	CLR_ZNC; SET_ZNC_ADD(RN, r1, res);
	RN = res;
#ifdef DSP_DIS_ADDQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_sub(void)
{
#ifdef DSP_DIS_SUB
	if (doDSPDis)
		WriteLog("%06X: SUB    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	uint32_t res = RN - RM;
	SET_ZNC_SUB(RN, RM, res);
	RN = res;
#ifdef DSP_DIS_SUB
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_subc(void)
{
#ifdef DSP_DIS_SUBC
	if (doDSPDis)
		WriteLog("%06X: SUBC   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	// This is how the DSP ALU does it--Two's complement with inverted carry
	uint64_t res = (uint64_t)RN + (uint64_t)(RM ^ 0xFFFFFFFF) + (dsp_flag_c ^ 1);
	// Carry out of the result is inverted too
	dsp_flag_c = ((res >> 32) & 0x01) ^ 1;
	RN = (res & 0xFFFFFFFF);
	SET_ZN(RN);
#ifdef DSP_DIS_SUBC
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_subq(void)
{
#ifdef DSP_DIS_SUBQ
	if (doDSPDis)
		WriteLog("%06X: SUBQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t r1 = dsp_convert_zero[IMM_1];
	uint32_t res = RN - r1;
	SET_ZNC_SUB(RN, r1, res);
	RN = res;
#ifdef DSP_DIS_SUBQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_cmp(void)
{
#ifdef DSP_DIS_CMP
	if (doDSPDis)
		WriteLog("%06X: CMP    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	uint32_t res = RN - RM;
	SET_ZNC_SUB(RN, RM, res);
#ifdef DSP_DIS_CMP
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
}


static void dsp_opcode_cmpq(void)
{
	static int32_t sqtable[32] =
		{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,-16,-15,-14,-13,-12,-11,-10,-9,-8,-7,-6,-5,-4,-3,-2,-1 };
#ifdef DSP_DIS_CMPQ
	if (doDSPDis)
		WriteLog("%06X: CMPQ   #%d, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, sqtable[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t r1 = sqtable[IMM_1 & 0x1F]; // I like this better -> (INT8)(jaguar.op >> 2) >> 3;
	uint32_t res = RN - r1;
	SET_ZNC_SUB(RN, r1, res);
#ifdef DSP_DIS_CMPQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
}


static void dsp_opcode_and(void)
{
#ifdef DSP_DIS_AND
	if (doDSPDis)
		WriteLog("%06X: AND    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	RN = RN & RM;
	SET_ZN(RN);
#ifdef DSP_DIS_AND
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_or(void)
{
#ifdef DSP_DIS_OR
	if (doDSPDis)
		WriteLog("%06X: OR     R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	RN = RN | RM;
	SET_ZN(RN);
#ifdef DSP_DIS_OR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_xor(void)
{
#ifdef DSP_DIS_XOR
	if (doDSPDis)
		WriteLog("%06X: XOR    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	RN = RN ^ RM;
	SET_ZN(RN);
#ifdef DSP_DIS_XOR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_not(void)
{
#ifdef DSP_DIS_NOT
	if (doDSPDis)
		WriteLog("%06X: NOT    R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	RN = ~RN;
	SET_ZN(RN);
#ifdef DSP_DIS_NOT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_move_pc(void)
{
	RN = dsp_pc - 2;
}


static void dsp_opcode_store_r14_indexed(void)
{
#ifdef DSP_DIS_STORE14I
	if (doDSPDis)
		WriteLog("%06X: STORE  R%02u, (R14+$%02X) [NCZ:%u%u%u, R%02u=%08X, R14+$%02X=%08X]\n", dsp_pc-2, IMM_2, dsp_convert_zero[IMM_1] << 2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN, dsp_convert_zero[IMM_1] << 2, dsp_reg[14]+(dsp_convert_zero[IMM_1] << 2));
#endif
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	DSPWriteLong((dsp_reg[14] & 0xFFFFFFFC) + (dsp_convert_zero[IMM_1] << 2), RN, DSP);
#else
	DSPWriteLong(dsp_reg[14] + (dsp_convert_zero[IMM_1] << 2), RN, DSP);
#endif
}


static void dsp_opcode_store_r15_indexed(void)
{
#ifdef DSP_DIS_STORE15I
	if (doDSPDis)
		WriteLog("%06X: STORE  R%02u, (R15+$%02X) [NCZ:%u%u%u, R%02u=%08X, R15+$%02X=%08X]\n", dsp_pc-2, IMM_2, dsp_convert_zero[IMM_1] << 2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN, dsp_convert_zero[IMM_1] << 2, dsp_reg[15]+(dsp_convert_zero[IMM_1] << 2));
#endif
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	DSPWriteLong((dsp_reg[15] & 0xFFFFFFFC) + (dsp_convert_zero[IMM_1] << 2), RN, DSP);
#else
	DSPWriteLong(dsp_reg[15] + (dsp_convert_zero[IMM_1] << 2), RN, DSP);
#endif
}


static void dsp_opcode_load_r14_ri(void)
{
#ifdef DSP_DIS_LOAD14R
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R14+R%02u), R%02u [NCZ:%u%u%u, R14+R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM+dsp_reg[14], IMM_2, RN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	RN = DSPReadLong((dsp_reg[14] + RM) & 0xFFFFFFFC, DSP);
#else
	RN = DSPReadLong(dsp_reg[14] + RM, DSP);
#endif
#ifdef DSP_DIS_LOAD14R
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_load_r15_ri(void)
{
#ifdef DSP_DIS_LOAD15R
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R15+R%02u), R%02u [NCZ:%u%u%u, R15+R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM+dsp_reg[15], IMM_2, RN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	RN = DSPReadLong((dsp_reg[15] + RM) & 0xFFFFFFFC, DSP);
#else
	RN = DSPReadLong(dsp_reg[15] + RM, DSP);
#endif
#ifdef DSP_DIS_LOAD15R
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_store_r14_ri(void)
{
	DSPWriteLong(dsp_reg[14] + RM, RN, DSP);
}


static void dsp_opcode_store_r15_ri(void)
{
	DSPWriteLong(dsp_reg[15] + RM, RN, DSP);
}


static void dsp_opcode_nop(void)
{
#ifdef DSP_DIS_NOP
	if (doDSPDis)
		WriteLog("%06X: NOP    [NCZ:%u%u%u]\n", dsp_pc-2, dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
}


static void dsp_opcode_storeb(void)
{
#ifdef DSP_DIS_STOREB
	if (doDSPDis)
		WriteLog("%06X: STOREB R%02u, (R%02u) [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_pc-2, IMM_2, IMM_1, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN, IMM_1, RM);
#endif
	if (RM >= DSP_WORK_RAM_BASE && RM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		DSPWriteLong(RM, RN & 0xFF, DSP);
	else
		JaguarWriteByte(RM, RN, DSP);
}


static void dsp_opcode_storew(void)
{
#ifdef DSP_DIS_STOREW
	if (doDSPDis)
		WriteLog("%06X: STOREW R%02u, (R%02u) [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_pc-2, IMM_2, IMM_1, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN, IMM_1, RM);
#endif
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	if (RM >= DSP_WORK_RAM_BASE && RM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		DSPWriteLong(RM & 0xFFFFFFFE, RN & 0xFFFF, DSP);
	else
		JaguarWriteWord(RM & 0xFFFFFFFE, RN, DSP);
#else
	if (RM >= DSP_WORK_RAM_BASE && RM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		DSPWriteLong(RM, RN & 0xFFFF, DSP);
	else
		JaguarWriteWord(RM, RN, DSP);
#endif
}


static void dsp_opcode_store(void)
{
#ifdef DSP_DIS_STORE
	if (doDSPDis)
		WriteLog("%06X: STORE  R%02u, (R%02u) [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_pc-2, IMM_2, IMM_1, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN, IMM_1, RM);
#endif
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	DSPWriteLong(RM & 0xFFFFFFFC, RN, DSP);
#else
	DSPWriteLong(RM, RN, DSP);
#endif
}


static void dsp_opcode_loadb(void)
{
#ifdef DSP_DIS_LOADB
	if (doDSPDis)
		WriteLog("%06X: LOADB  (R%02u), R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	if (RM >= DSP_WORK_RAM_BASE && RM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		RN = DSPReadLong(RM, DSP) & 0xFF;
	else
		RN = JaguarReadByte(RM, DSP);
#ifdef DSP_DIS_LOADB
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_loadw(void)
{
#ifdef DSP_DIS_LOADW
	if (doDSPDis)
		WriteLog("%06X: LOADW  (R%02u), R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	if (RM >= DSP_WORK_RAM_BASE && RM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		RN = DSPReadLong(RM & 0xFFFFFFFE, DSP) & 0xFFFF;
	else
		RN = JaguarReadWord(RM & 0xFFFFFFFE, DSP);
#else
	if (RM >= DSP_WORK_RAM_BASE && RM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		RN = DSPReadLong(RM, DSP) & 0xFFFF;
	else
		RN = JaguarReadWord(RM, DSP);
#endif
#ifdef DSP_DIS_LOADW
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_load(void)
{
#ifdef DSP_DIS_LOAD
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R%02u), R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	RN = DSPReadLong(RM & 0xFFFFFFFC, DSP);
#else
	RN = DSPReadLong(RM, DSP);
#endif
#ifdef DSP_DIS_LOAD
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_load_r14_indexed(void)
{
#ifdef DSP_DIS_LOAD14I
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R14+$%02X), R%02u [NCZ:%u%u%u, R14+$%02X=%08X, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1] << 2, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, dsp_convert_zero[IMM_1] << 2, dsp_reg[14]+(dsp_convert_zero[IMM_1] << 2), IMM_2, RN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	RN = DSPReadLong((dsp_reg[14] & 0xFFFFFFFC) + (dsp_convert_zero[IMM_1] << 2), DSP);
#else
	RN = DSPReadLong(dsp_reg[14] + (dsp_convert_zero[IMM_1] << 2), DSP);
#endif
#ifdef DSP_DIS_LOAD14I
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_load_r15_indexed(void)
{
#ifdef DSP_DIS_LOAD15I
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R15+$%02X), R%02u [NCZ:%u%u%u, R15+$%02X=%08X, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1] << 2, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, dsp_convert_zero[IMM_1] << 2, dsp_reg[15]+(dsp_convert_zero[IMM_1] << 2), IMM_2, RN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	RN = DSPReadLong((dsp_reg[15] & 0xFFFFFFFC) + (dsp_convert_zero[IMM_1] << 2), DSP);
#else
	RN = DSPReadLong(dsp_reg[15] + (dsp_convert_zero[IMM_1] << 2), DSP);
#endif
#ifdef DSP_DIS_LOAD15I
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_movei(void)
{
#ifdef DSP_DIS_MOVEI
	if (doDSPDis)
		WriteLog("%06X: MOVEI  #$%08X, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, (uint32_t)DSPReadWord(dsp_pc) | ((uint32_t)DSPReadWord(dsp_pc + 2) << 16), IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	// This instruction is followed by 32-bit value in LSW / MSW format...
	RN = (uint32_t)DSPReadWord(dsp_pc, DSP) | ((uint32_t)DSPReadWord(dsp_pc + 2, DSP) << 16);
	dsp_pc += 4;
#ifdef DSP_DIS_MOVEI
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_moveta(void)
{
#ifdef DSP_DIS_MOVETA
	if (doDSPDis)
		WriteLog("%06X: MOVETA R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u(alt)=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, ALTERNATE_RN);
#endif
	ALTERNATE_RN = RM;
#ifdef DSP_DIS_MOVETA
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u(alt)=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, ALTERNATE_RN);
#endif
}


static void dsp_opcode_movefa(void)
{
#ifdef DSP_DIS_MOVEFA
	if (doDSPDis)
		WriteLog("%06X: MOVEFA R%02u, R%02u [NCZ:%u%u%u, R%02u(alt)=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, ALTERNATE_RM, IMM_2, RN);
#endif
	RN = ALTERNATE_RM;
#ifdef DSP_DIS_MOVEFA
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u(alt)=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, ALTERNATE_RM, IMM_2, RN);
#endif
}


static void dsp_opcode_move(void)
{
#ifdef DSP_DIS_MOVE
	if (doDSPDis)
		WriteLog("%06X: MOVE   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	RN = RM;
#ifdef DSP_DIS_MOVE
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_moveq(void)
{
#ifdef DSP_DIS_MOVEQ
	if (doDSPDis)
		WriteLog("%06X: MOVEQ  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	RN = IMM_1;
#ifdef DSP_DIS_MOVEQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_resmac(void)
{
#ifdef DSP_DIS_RESMAC
	if (doDSPDis)
		WriteLog("%06X: RESMAC R%02u [NCZ:%u%u%u, R%02u=%08X, DSP_ACC=%02X%08X] -> ", dsp_pc-2, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN, (uint8_t)(dsp_acc >> 32), (uint32_t)(dsp_acc & 0xFFFFFFFF));
#endif
	RN = (uint32_t)dsp_acc;
#ifdef DSP_DIS_RESMAC
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_imult(void)
{
#ifdef DSP_DIS_IMULT
	if (doDSPDis)
		WriteLog("%06X: IMULT  R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	RN = (int16_t)RN * (int16_t)RM;
	SET_ZN(RN);
#ifdef DSP_DIS_IMULT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_mult(void)
{
#ifdef DSP_DIS_MULT
	if (doDSPDis)
		WriteLog("%06X: MULT   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	RN = (uint16_t)RM * (uint16_t)RN;
	SET_ZN(RN);
#ifdef DSP_DIS_MULT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_bclr(void)
{
#ifdef DSP_DIS_BCLR
	if (doDSPDis)
		WriteLog("%06X: BCLR   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t res = RN & ~(1 << IMM_1);
	RN = res;
	SET_ZN(res);
#ifdef DSP_DIS_BCLR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_btst(void)
{
#ifdef DSP_DIS_BTST
	if (doDSPDis)
		WriteLog("%06X: BTST   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	dsp_flag_z = (~RN >> IMM_1) & 1;
#ifdef DSP_DIS_BTST
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_bset(void)
{
#ifdef DSP_DIS_BSET
	if (doDSPDis)
		WriteLog("%06X: BSET   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t res = RN | (1 << IMM_1);
	RN = res;
	SET_ZN(res);
#ifdef DSP_DIS_BSET
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_subqt(void)
{
#ifdef DSP_DIS_SUBQT
	if (doDSPDis)
		WriteLog("%06X: SUBQT  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	RN -= dsp_convert_zero[IMM_1];
#ifdef DSP_DIS_SUBQT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_addqt(void)
{
#ifdef DSP_DIS_ADDQT
	if (doDSPDis)
		WriteLog("%06X: ADDQT  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	RN += dsp_convert_zero[IMM_1];
#ifdef DSP_DIS_ADDQT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_imacn(void)
{
#ifdef DSP_DIS_IMACN
	if (doDSPDis)
		WriteLog("%06X: IMACN  R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	int32_t res = (int16_t)RM * (int16_t)RN;
	dsp_acc += (int64_t)res;
//Should we AND the result to fit into 40 bits here???
#ifdef DSP_DIS_IMACN
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, DSP_ACC=%02X%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, (uint8_t)(dsp_acc >> 32), (uint32_t)(dsp_acc & 0xFFFFFFFF));
#endif
}


static void dsp_opcode_mtoi(void)
{
	RN = (((int32_t)RM >> 8) & 0xFF800000) | (RM & 0x007FFFFF);
	SET_ZN(RN);
}


static void dsp_opcode_normi(void)
{
	uint32_t _Rm = RM;
	uint32_t res = 0;

	if (_Rm)
	{
		while ((_Rm & 0xffc00000) == 0)
		{
			_Rm <<= 1;
			res--;
		}
		while ((_Rm & 0xff800000) != 0)
		{
			_Rm >>= 1;
			res++;
		}
	}
	RN = res;
	SET_ZN(RN);
}


static void dsp_opcode_mmult(void)
{
	int count	= dsp_matrix_control&0x0f;
	uint32_t addr = dsp_pointer_to_matrix; // in the dsp ram
	int64_t accum = 0;
	uint32_t res;

	if (!(dsp_matrix_control & 0x10))
	{
		for (int i = 0; i < count; i++)
		{
			int16_t a;
			if (i&0x01)
				a=(int16_t)((dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]>>16)&0xffff);
			else
				a=(int16_t)(dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]&0xffff);
			int16_t b=((int16_t)DSPReadWord(addr + 2, DSP));
			accum += a*b;
			addr += 4;
		}
	}
	else
	{
		for (int i = 0; i < count; i++)
		{
			int16_t a;
			if (i&0x01)
				a=(int16_t)((dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]>>16)&0xffff);
			else
				a=(int16_t)(dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]&0xffff);
			int16_t b=((int16_t)DSPReadWord(addr + 2, DSP));
			accum += a*b;
			addr += 4 * count;
		}
	}
	RN = res = (int32_t)accum;
	// carry flag to do
//NOTE: The flags are set based upon the last add/multiply done...
	SET_ZN(RN);
}


static void dsp_opcode_abs(void)
{
#ifdef DSP_DIS_ABS
	if (doDSPDis)
		WriteLog("%06X: ABS    R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t _Rn = RN;
	uint32_t res;

	if (_Rn == 0x80000000)
		dsp_flag_n = 1;
	else
	{
		dsp_flag_c = ((_Rn & 0x80000000) >> 31);
		res = RN = (_Rn & 0x80000000 ? -_Rn : _Rn);
		CLR_ZN; SET_Z(res);
	}
#ifdef DSP_DIS_ABS
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_div(void)
{
#if 0
	if (RM)
	{
		if (dsp_div_control & 0x01)		// 16.16 division
		{
			dsp_remain = ((uint64_t)RN << 16) % RM;
			RN = ((uint64_t)RN << 16) / RM;
		}
		else
		{
			// We calculate the remainder first because we destroy RN after
			// this by assigning it to itself.
			dsp_remain = RN % RM;
			RN = RN / RM;
		}

	}
	else
	{
		// This is what happens according to SCPCD. NYAN!
		RN = 0xFFFFFFFF;
		dsp_remain = 0;
	}
#else
	// Real algorithm, courtesy of SCPCD: NYAN!
	uint32_t q = RN;
	uint32_t r = 0;

	// If 16.16 division, stuff top 16 bits of RN into remainder and put the
	// bottom 16 of RN in top 16 of quotient
	if (dsp_div_control & 0x01)
		q <<= 16, r = RN >> 16;

	for(int i=0; i<32; i++)
	{
//		uint32_t sign = (r >> 31) & 0x01;
		uint32_t sign = r & 0x80000000;
		r = (r << 1) | ((q >> 31) & 0x01);
		r += (sign ? RM : -RM);
		q = (q << 1) | (((~r) >> 31) & 0x01);
	}

	RN = q;
	dsp_remain = r;
#endif
}


static void dsp_opcode_imultn(void)
{
#ifdef DSP_DIS_IMULTN
	if (doDSPDis)
		WriteLog("%06X: IMULTN R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	// This is OK, since this multiply won't overflow 32 bits...
	int32_t res = (int32_t)((int16_t)RN * (int16_t)RM);
	dsp_acc = (int64_t)res;
	SET_ZN(res);
#ifdef DSP_DIS_IMULTN
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, DSP_ACC=%02X%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, (uint8_t)(dsp_acc >> 32), (uint32_t)(dsp_acc & 0xFFFFFFFF));
#endif
}


static void dsp_opcode_neg(void)
{
#ifdef DSP_DIS_NEG
	if (doDSPDis)
		WriteLog("%06X: NEG    R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t res = -RN;
	SET_ZNC_SUB(0, RN, res);
	RN = res;
#ifdef DSP_DIS_NEG
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_shlq(void)
{
#ifdef DSP_DIS_SHLQ
	if (doDSPDis)
		WriteLog("%06X: SHLQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, 32 - IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	// NB: This instruction is the *only* one that does (32 - immediate data).
	int32_t r1 = 32 - IMM_1;
	uint32_t res = RN << r1;
	SET_ZN(res); dsp_flag_c = (RN >> 31) & 1;
	RN = res;
#ifdef DSP_DIS_SHLQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_shrq(void)
{
#ifdef DSP_DIS_SHRQ
	if (doDSPDis)
		WriteLog("%06X: SHRQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	int32_t r1 = dsp_convert_zero[IMM_1];
	uint32_t res = RN >> r1;
	SET_ZN(res); dsp_flag_c = RN & 1;
	RN = res;
#ifdef DSP_DIS_SHRQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_ror(void)
{
#ifdef DSP_DIS_ROR
	if (doDSPDis)
		WriteLog("%06X: ROR    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
	uint32_t r1 = RM & 0x1F;
	uint32_t res = (RN >> r1) | (RN << (32 - r1));
	SET_ZN(res); dsp_flag_c = (RN >> 31) & 1;
	RN = res;
#ifdef DSP_DIS_ROR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_1, RM, IMM_2, RN);
#endif
}


static void dsp_opcode_rorq(void)
{
#ifdef DSP_DIS_RORQ
	if (doDSPDis)
		WriteLog("%06X: RORQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t r1 = dsp_convert_zero[IMM_1 & 0x1F];
	uint32_t r2 = RN;
	uint32_t res = (r2 >> r1) | (r2 << (32 - r1));
	RN = res;
	SET_ZN(res); dsp_flag_c = (r2 >> 31) & 0x01;
#ifdef DSP_DIS_RORQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_sha(void)
{
	int32_t sRm=(int32_t)RM;
	uint32_t _Rn=RN;

	if (sRm<0)
	{
		uint32_t shift=-sRm;
		if (shift>=32) shift=32;
		dsp_flag_c=(_Rn&0x80000000)>>31;
		while (shift)
		{
			_Rn<<=1;
			shift--;
		}
	}
	else
	{
		uint32_t shift=sRm;
		if (shift>=32) shift=32;
		dsp_flag_c=_Rn&0x1;
		while (shift)
		{
			_Rn=((int32_t)_Rn)>>1;
			shift--;
		}
	}
	RN = _Rn;
	SET_ZN(RN);
}


static void dsp_opcode_sharq(void)
{
#ifdef DSP_DIS_SHARQ
	if (doDSPDis)
		WriteLog("%06X: SHARQ  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
	uint32_t res = (int32_t)RN >> dsp_convert_zero[IMM_1];
	SET_ZN(res); dsp_flag_c = RN & 0x01;
	RN = res;
#ifdef DSP_DIS_SHARQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}


static void dsp_opcode_sh(void)
{
	int32_t sRm=(int32_t)RM;
	uint32_t _Rn=RN;

	if (sRm<0)
	{
		uint32_t shift=(-sRm);
		if (shift>=32) shift=32;
		dsp_flag_c=(_Rn&0x80000000)>>31;
		while (shift)
		{
			_Rn<<=1;
			shift--;
		}
	}
	else
	{
		uint32_t shift=sRm;
		if (shift>=32) shift=32;
		dsp_flag_c=_Rn&0x1;
		while (shift)
		{
			_Rn>>=1;
			shift--;
		}
	}
	RN = _Rn;
	SET_ZN(RN);
}

void dsp_opcode_addqmod(void)
{
#ifdef DSP_DIS_ADDQMOD
	if (doDSPDis)
		WriteLog("%06X: ADDQMOD #%u, R%02u [NCZ:%u%u%u, R%02u=%08X, DSP_MOD=%08X] -> ", dsp_pc-2, dsp_convert_zero[IMM_1], IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN, dsp_modulo);
#endif
	uint32_t r1 = dsp_convert_zero[IMM_1];
	uint32_t r2 = RN;
	uint32_t res = r2 + r1;
	res = (res & (~dsp_modulo)) | (r2 & dsp_modulo);
	RN = res;
	SET_ZNC_ADD(r2, r1, res);
#ifdef DSP_DIS_ADDQMOD
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, IMM_2, RN);
#endif
}

void dsp_opcode_subqmod(void)
{
	uint32_t r1 = dsp_convert_zero[IMM_1];
	uint32_t r2 = RN;
	uint32_t res = r2 - r1;
	res = (res & (~dsp_modulo)) | (r2 & dsp_modulo);
	RN = res;

	SET_ZNC_SUB(r2, r1, res);
}

void dsp_opcode_mirror(void)
{
	uint32_t r1 = RN;
	RN = (mirror_table[r1 & 0xFFFF] << 16) | mirror_table[r1 >> 16];
	SET_ZN(RN);
}

void dsp_opcode_sat32s(void)
{
	int32_t r2 = (uint32_t)RN;
	int32_t temp = dsp_acc >> 32;
	uint32_t res = (temp < -1) ? (int32_t)0x80000000 : (temp > 0) ? (int32_t)0x7FFFFFFF : r2;
	RN = res;
	SET_ZN(res);
}

void dsp_opcode_sat16s(void)
{
	int32_t r2 = RN;
	uint32_t res = (r2 < -32768) ? -32768 : (r2 > 32767) ? 32767 : r2;
	RN = res;
	SET_ZN(res);
}

void dsp_opcode_illegal(void)
{
	// Don't know what it does, but it does *something*...
	WriteLog("%06X: illegal %u, %u [NCZ:%u%u%u]\n", dsp_pc-2, IMM_1, IMM_2, dsp_flag_n, dsp_flag_c, dsp_flag_z);
}

//
// New pipelined DSP core
//

static void DSP_abs(void);
static void DSP_add(void);
static void DSP_addc(void);
static void DSP_addq(void);
static void DSP_addqmod(void);
static void DSP_addqt(void);
static void DSP_and(void);
static void DSP_bclr(void);
static void DSP_bset(void);
static void DSP_btst(void);
static void DSP_cmp(void);
static void DSP_cmpq(void);
static void DSP_div(void);
static void DSP_imacn(void);
static void DSP_imult(void);
static void DSP_imultn(void);
static void DSP_illegal(void);
static void DSP_jr(void);
static void DSP_jump(void);
static void DSP_load(void);
static void DSP_loadb(void);
static void DSP_loadw(void);
static void DSP_load_r14_i(void);
static void DSP_load_r14_r(void);
static void DSP_load_r15_i(void);
static void DSP_load_r15_r(void);
static void DSP_mirror(void);
static void DSP_mmult(void);
static void DSP_move(void);
static void DSP_movefa(void);
static void DSP_movei(void);
static void DSP_movepc(void);
static void DSP_moveq(void);
static void DSP_moveta(void);
static void DSP_mtoi(void);
static void DSP_mult(void);
static void DSP_neg(void);
static void DSP_nop(void);
static void DSP_normi(void);
static void DSP_not(void);
static void DSP_or(void);
static void DSP_resmac(void);
static void DSP_ror(void);
static void DSP_rorq(void);
static void DSP_sat16s(void);
static void DSP_sat32s(void);
static void DSP_sh(void);
static void DSP_sha(void);
static void DSP_sharq(void);
static void DSP_shlq(void);
static void DSP_shrq(void);
static void DSP_store(void);
static void DSP_storeb(void);
static void DSP_storew(void);
static void DSP_store_r14_i(void);
static void DSP_store_r14_r(void);
static void DSP_store_r15_i(void);
static void DSP_store_r15_r(void);
static void DSP_sub(void);
static void DSP_subc(void);
static void DSP_subq(void);
static void DSP_subqmod(void);
static void DSP_subqt(void);
static void DSP_xor(void);

void (* DSPOpcode[64])() =
{
	DSP_add,			DSP_addc,			DSP_addq,			DSP_addqt,
	DSP_sub,			DSP_subc,			DSP_subq,			DSP_subqt,
	DSP_neg,			DSP_and,			DSP_or,				DSP_xor,
	DSP_not,			DSP_btst,			DSP_bset,			DSP_bclr,

	DSP_mult,			DSP_imult,			DSP_imultn,			DSP_resmac,
	DSP_imacn,			DSP_div,			DSP_abs,			DSP_sh,
	DSP_shlq,			DSP_shrq,			DSP_sha,			DSP_sharq,
	DSP_ror,			DSP_rorq,			DSP_cmp,			DSP_cmpq,

	DSP_subqmod,		DSP_sat16s,			DSP_move,			DSP_moveq,
	DSP_moveta,			DSP_movefa,			DSP_movei,			DSP_loadb,
	DSP_loadw,			DSP_load,			DSP_sat32s,			DSP_load_r14_i,
	DSP_load_r15_i,		DSP_storeb,			DSP_storew,			DSP_store,

	DSP_mirror,			DSP_store_r14_i,	DSP_store_r15_i,	DSP_movepc,
	DSP_jump,			DSP_jr,				DSP_mmult,			DSP_mtoi,
	DSP_normi,			DSP_nop,			DSP_load_r14_r,		DSP_load_r15_r,
	DSP_store_r14_r,	DSP_store_r15_r,	DSP_illegal,		DSP_addqmod
};

bool readAffected[64][2] =
{
	{ true,  true}, { true,  true}, {false,  true}, {false,  true},
	{ true,  true}, { true,  true}, {false,  true}, {false,  true},
	{false,  true}, { true,  true}, { true,  true}, { true,  true},
	{false,  true}, {false,  true}, {false,  true}, {false,  true},

	{ true,  true}, { true,  true}, { true,  true}, {false,  true},
	{ true,  true}, { true,  true}, {false,  true}, { true,  true},
	{false,  true}, {false,  true}, { true,  true}, {false,  true},
	{ true,  true}, {false,  true}, { true,  true}, {false,  true},

	{false,  true}, {false,  true}, { true, false}, {false, false},
	{ true, false}, {false, false}, {false, false}, { true, false},
	{ true, false}, { true, false}, {false,  true}, { true, false},
	{ true, false}, { true,  true}, { true,  true}, { true,  true},

	{false,  true}, { true,  true}, { true,  true}, {false,  true},
	{ true, false}, { true, false}, { true,  true}, { true, false},
	{ true, false}, {false, false}, { true, false}, { true, false},
	{ true,  true}, { true,  true}, {false, false}, {false,  true}
};

bool isLoadStore[65] =
{
	false, false, false, false, false, false, false, false,
	false, false, false, false, false, false, false, false,

	false, false, false, false, false, false, false, false,
	false, false, false, false, false, false, false, false,

	false, false, false, false, false, false, false,  true,
	 true,  true, false,  true,  true,  true,  true,  true,

	false,  true,  true, false, false, false, false, false,
	false, false,  true,  true,  true,  true, false, false, false
};

void FlushDSPPipeline(void)
{
	plPtrFetch = 3, plPtrRead = 2, plPtrExec = 1, plPtrWrite = 0;

	for(int i=0; i<4; i++)
		pipeline[i].opcode = PIPELINE_STALL;

	for(int i=0; i<32; i++)
		scoreboard[i] = 0;
}

//
// New pipelined DSP execution core
//
/*void DSPExecP(int32_t cycles)
{
//	bool inhibitFetch = false;

	dsp_releaseTimeSlice_flag = 0;
	dsp_in_exec++;

	while (cycles > 0 && DSP_RUNNING)
	{
WriteLog("DSPExecP: Pipeline status...\n");
WriteLog("\tF -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrFetch].opcode, pipeline[plPtrFetch].operand1, pipeline[plPtrFetch].operand2, pipeline[plPtrFetch].reg1, pipeline[plPtrFetch].reg2, pipeline[plPtrFetch].result, pipeline[plPtrFetch].writebackRegister);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister);
WriteLog("  --> Scoreboard: ");
for(int i=0; i<32; i++)
	WriteLog("%s ", scoreboard[i] ? "T" : "F");
WriteLog("\n");
		// Stage 1: Instruction fetch
//		if (!inhibitFetch)
//		{
		pipeline[plPtrFetch].instruction = DSPReadWord(dsp_pc, DSP);
		pipeline[plPtrFetch].opcode = pipeline[plPtrFetch].instruction >> 10;
		pipeline[plPtrFetch].operand1 = (pipeline[plPtrFetch].instruction >> 5) & 0x1F;
		pipeline[plPtrFetch].operand2 = pipeline[plPtrFetch].instruction & 0x1F;
		if (pipeline[plPtrFetch].opcode == 38)
			pipeline[plPtrFetch].result = (uint32_t)DSPReadWord(dsp_pc + 2, DSP)
				| ((uint32_t)DSPReadWord(dsp_pc + 4, DSP) << 16);
//		}
//		else
//			inhibitFetch = false;
WriteLog("DSPExecP: Fetching instruction (%04X) from DSP_PC = %08X...\n", pipeline[plPtrFetch].instruction, dsp_pc);

WriteLog("DSPExecP: Pipeline status (after stage 1)...\n");
WriteLog("\tF -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrFetch].opcode, pipeline[plPtrFetch].operand1, pipeline[plPtrFetch].operand2, pipeline[plPtrFetch].reg1, pipeline[plPtrFetch].reg2, pipeline[plPtrFetch].result, pipeline[plPtrFetch].writebackRegister);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister);
		// Stage 2: Read registers
//Ok, stalls here depend on whether or not the instruction reads two registers or not
//and *which* register (1 or 2) is the one being read... !!! FIX !!!
		if (scoreboard[pipeline[plPtrRead].operand2])
			&& pipeline[plPtrRead].opcode != PIPELINE_STALL)
			// We have a hit in the scoreboard, so we have to stall the pipeline...
{
//This is crappy, crappy CRAPPY! And it doesn't work! !!! FIX !!!
//			dsp_pc -= (pipeline[plPtrRead].opcode == 38 ? 6 : 2);
WriteLog("  --> Stalling pipeline: scoreboard = %s\n", scoreboard[pipeline[plPtrRead].operand2] ? "true" : "false");
			pipeline[plPtrFetch] = pipeline[plPtrRead];
			pipeline[plPtrRead].opcode = PIPELINE_STALL;
}
		else
		{
			pipeline[plPtrRead].reg1 = dsp_reg[pipeline[plPtrRead].operand1];
			pipeline[plPtrRead].reg2 = dsp_reg[pipeline[plPtrRead].operand2];
			pipeline[plPtrRead].writebackRegister = pipeline[plPtrRead].operand2;	// Set it to RN

			if (pipeline[plPtrRead].opcode != PIPELINE_STALL)
			// Shouldn't we be more selective with the register scoreboarding?
			// Yes, we should. !!! FIX !!!
			scoreboard[pipeline[plPtrRead].operand2] = true;
//Advance PC here??? Yes.
//			dsp_pc += (pipeline[plPtrRead].opcode == 38 ? 6 : 2);
//This is a mangling of the pipeline stages, but what else to do???
			dsp_pc += (pipeline[plPtrFetch].opcode == 38 ? 6 : 2);
		}

WriteLog("DSPExecP: Pipeline status (after stage 2)...\n");
WriteLog("\tF -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrFetch].opcode, pipeline[plPtrFetch].operand1, pipeline[plPtrFetch].operand2, pipeline[plPtrFetch].reg1, pipeline[plPtrFetch].reg2, pipeline[plPtrFetch].result, pipeline[plPtrFetch].writebackRegister);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister);
		// Stage 3: Execute
		if (pipeline[plPtrExec].opcode != PIPELINE_STALL)
		{
WriteLog("DSPExecP: About to execute opcode %s...\n", dsp_opcode_str[pipeline[plPtrExec].opcode]);
			DSPOpcode[pipeline[plPtrExec].opcode]();
			dsp_opcode_use[pipeline[plPtrExec].opcode]++;
			cycles -= dsp_opcode_cycles[pipeline[plPtrExec].opcode];
		}
		else
			cycles--;

WriteLog("DSPExecP: Pipeline status (after stage 3)...\n");
WriteLog("\tF -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrFetch].opcode, pipeline[plPtrFetch].operand1, pipeline[plPtrFetch].operand2, pipeline[plPtrFetch].reg1, pipeline[plPtrFetch].reg2, pipeline[plPtrFetch].result, pipeline[plPtrFetch].writebackRegister);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u \n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister);
		// Stage 4: Write back register
		if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
		{
			if (pipeline[plPtrWrite].writebackRegister != 0xFF)
				dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;

			scoreboard[pipeline[plPtrWrite].operand1]
				= scoreboard[pipeline[plPtrWrite].operand2] = false;
		}

		// Push instructions through the pipeline...
		plPtrFetch = (++plPtrFetch) & 0x03;
		plPtrRead = (++plPtrRead) & 0x03;
		plPtrExec = (++plPtrExec) & 0x03;
		plPtrWrite = (++plPtrWrite) & 0x03;
	}

	dsp_in_exec--;
}*/


//Problems: JR and any other instruction that relies on DSP_PC is getting WRONG values!
//!!! FIX !!!
// Should be fixed now. Another problem is figuring how to do the sequence following
// a branch followed with the JR & JUMP instructions...
//
// There are two conflicting problems:

/*
F1B236: LOAD   (R31), R03 [NCZ:000, R31=00F1CFDC, R03=00F14000] -> [NCZ:000, R03=00F1B084]
F1B238: BCLR   #3, R00 [NCZ:000, R00=00004039] -> [NCZ:000, R00=00004031]
F1B23A: ADDQ   #2, R03 [NCZ:000, R03=00F1B084] -> [NCZ:000, R03=00F1B086]
F1B23C: SUBQ   #1, R17 [NCZ:000, R17=00000040] -> [NCZ:000, R17=0000003F]
F1B23E: MOVEI  #$00F1CFE0, R31 [NCZ:000, R31=00F1CFDC] -> [NCZ:000, R31=00F1CFE0]
F1B244: JR     z, F1B254 [NCZ:000] Branch NOT taken.
F1B246: BSET   #10, R00 [NCZ:000, R00=00004031] -> [NCZ:000, R00=00004431]
F1B248: MOVEI  #$00F1A100, R01 [NCZ:000, R01=00F1A148] -> [NCZ:000, R01=00F1A100]
F1B24E: STORE  R00, (R01) [NCZ:000, R00=00004431, R01=00F1A100]
DSP: Writing 00004431 to DSP_FLAGS by DSP...
DSP: Finished interrupt.
; Without pipeline effects, the value in R03 is erroneously read from bank 1 instead of
; bank 0 (where is was prepared)!
F1B250: JUMP   T, (R03) [NCZ:001, R03=00000000] Branched!
F1B252: NOP    [NCZ:001]
*/

// The other is when you see this at the end of an IRQ:

/*
JUMP   T, (R29)		; R29 = Previous stack + 2
STORE  R28, (R30)	; R28 = Modified flags register, R30 = $F1A100

; Actually, this is OK if we do the atomic JUMP/JR operation correctly:
; 1) The STORE goes through the pipeline and is executed/written back
; 2) The pipeline is flushed
; 3) The DSP_PC is set to the new address
; 4) Execution resumes

JUMP   T, (R25)		; Oops! Because of pipeline effects R25 has the value from
					; bank 0 instead of the current bank 1 and so goes astray!
*/

//One other thing: Since these stages are supposed to happen simulaneously, try executing
//them in reverse order to see if that reduces pipeline stalls from late writebacks...


/*
Small problem here: The return address when INT0 comes up is $F1B088, but when INT1
follows it, the JUMP out of the previous interrupt is bypassed immediately--this is
because the STORE instruction writes back on stage #2 of the pipeline instead of stage #3...
If it were done properly, the STORE write back would occur *after* (well, technically,
during) the execution of the the JUMP that follows it.

!!! FIX !!! [DONE]

F1B08A: JR     z, F1B082 [NCZ:001] Branched!
F1B08A: NOP    [NCZ:001]
[STALL...]
F1B080: MOVEI  #$00F1B178, R00 [NCZ:001, R00=00F1B178] -> [NCZ:001, R00=00F1B178]
[STALL...]
[STALL...]
F1B086: LOAD   (R00), R01 [NCZ:001, R00=00F1B178, R01=00000000] -> [NCZ:001, R01=00000000]
[STALL...]
[STALL...]
F1B088: OR     R01, R01 [NCZ:001, R01=00000000, R01=00000000] -> [NCZ:001, R01=00000000, R01=00000000]
F1B08A: JR     z, F1B082 [NCZ:001] Branched!
F1B08A: NOP    [NCZ:001]
[STALL...]
F1B080: MOVEI  #$00F1B178, R00 [NCZ:001, R00=00F1B178] -> [NCZ:001, R00=00F1B178]
[STALL...]
[STALL...]
Write to DSP CTRL: 00002301  --> Starting to run at 00F1B088 by M68K...
DSP: CPU -> DSP interrupt
DSP: Generating interrupt #0... [PC will return to 00F1B088, R31 = 00F1CFE0]
Write to DSP CTRL: 00000001  --> Starting to run at 00F1B000 by M68K...
[STALL...]
F1B000: MOVEI  #$00F1B0D4, R30 [NCZ:001, R30=00F1B000] -> [NCZ:001, R30=00F1B0D4]
[STALL...]
[STALL...]
F1B006: JUMP   T, (R30) [NCZ:001, R30=00F1B0D4] Branched!
F1B006: NOP    [NCZ:001]
[STALL...]
F1B0D4: MOVEI  #$00F1A100, R01 [NCZ:001, R01=00F1A100] -> [NCZ:001, R01=00F1A100]
[STALL...]
[STALL...]
F1B0DA: LOAD   (R01), R00 [NCZ:001, R01=00F1A100, R00=00004431] -> [NCZ:001, R00=00004039]
F1B0DC: MOVEI  #$00F1B0C8, R01 [NCZ:001, R01=00F1A100] -> [NCZ:001, R01=00F1B0C8]
[STALL...]
[STALL...]
F1B0E2: LOAD   (R01), R02 [NCZ:001, R01=00F1B0C8, R02=00000000] -> [NCZ:001, R02=00000001]
F1B0E4: MOVEI  #$00F1B0CC, R01 [NCZ:001, R01=00F1B0C8] -> [NCZ:001, R01=00F1B0CC]
[STALL...]
[STALL...]
F1B0EA: LOAD   (R01), R03 [NCZ:001, R01=00F1B0CC, R03=00F1B086] -> [NCZ:001, R03=00000064]
F1B0EC: MOVEI  #$00F1B0D0, R01 [NCZ:001, R01=00F1B0CC] -> [NCZ:001, R01=00F1B0D0]
[STALL...]
[STALL...]
F1B0F2: LOAD   (R01), R04 [NCZ:001, R01=00F1B0D0, R04=00000000] -> [NCZ:001, R04=00000008]
F1B0F4: MOVEI  #$00F1B0BC, R01 [NCZ:001, R01=00F1B0D0] -> [NCZ:001, R01=00F1B0BC]
[STALL...]
[STALL...]
F1B0FA: ADD    R04, R01 [NCZ:001, R04=00000008, R01=00F1B0BC] -> [NCZ:000, R04=00000008, R01=00F1B0C4]
[STALL...]
[STALL...]
F1B0FC: LOAD   (R01), R01 [NCZ:000, R01=00F1B0C4, R01=00F1B0C4] -> [NCZ:000, R01=00F1B12E]
[STALL...]
[STALL...]
F1B0FE: JUMP   T, (R01) [NCZ:000, R01=00F1B12E] Branched!
F1B0FE: NOP    [NCZ:000]
[STALL...]
F1B12E: MOVE   R02, R08 [NCZ:000, R02=00000001, R08=00000000] -> [NCZ:000, R02=00000001, R08=00000001]
[STALL...]
[STALL...]
F1B132: MOVEI  #$00F1B102, R01 [NCZ:000, R01=00F1B12E] -> [NCZ:000, R01=00F1B102]
[STALL...]
[STALL...]
F1B138: JUMP   T, (R01) [NCZ:000, R01=00F1B102] Branched!
F1B138: NOP    [NCZ:000]
[STALL...]
F1B102: MOVEI  #$00F1B0C8, R01 [NCZ:000, R01=00F1B102] -> [NCZ:000, R01=00F1B0C8]
[STALL...]
[STALL...]
F1B108: STORE  R08, (R01) [NCZ:000, R08=00000000, R01=00F1B0C8]
F1B10A: MOVEI  #$00F1B0D0, R01 [NCZ:000, R01=00F1B0C8] -> [NCZ:000, R01=00F1B0D0]
F1B110: MOVEQ  #0, R04 [NCZ:000, R04=00000008] -> [NCZ:000, R04=00000000]
[STALL...]
[STALL...]
F1B112: STORE  R04, (R01) [NCZ:000, R04=00000000, R01=00F1B0D0]
F1B114: BCLR   #3, R00 [NCZ:000, R00=00004039] -> [NCZ:000, R00=00004031]
[STALL...]
[STALL...]
F1B116: BSET   #9, R00 [NCZ:000, R00=00004031] -> [NCZ:000, R00=00004231]
F1B118: LOAD   (R31), R04 [NCZ:000, R31=00F1CFDC, R04=00000000] -> [NCZ:000, R04=00F1B086]
F1B11A: MOVEI  #$00F1CFE0, R31 [NCZ:000, R31=00F1CFDC] -> [NCZ:000, R31=00F1CFE0]
[STALL...]
F1B120: ADDQ   #2, R04 [NCZ:000, R04=00F1B086] -> [NCZ:000, R04=00F1B088]
F1B122: MOVEI  #$00F1A100, R01 [NCZ:000, R01=00F1B0D0] -> [NCZ:000, R01=00F1A100]
[STALL...]
[STALL...]
F1B128: STORE  R00, (R01) [NCZ:000, R00=00004231, R01=00F1A100]
DSP: Writing 00004231 to DSP_FLAGS by DSP (REGPAGE is set)...
DSP: Finished interrupt.
DSP: Generating interrupt #1... [PC will return to 00F1B12A, R31 = 00F1CFE0]
[STALL...]
F1B010: MOVEI  #$00F1B1FC, R30 [NCZ:001, R30=00F1B010] -> [NCZ:001, R30=00F1B1FC]
[STALL...]
[STALL...]
F1B016: JUMP   T, (R30) [NCZ:001, R30=00F1B1FC] Branched!
F1B016: NOP    [NCZ:001]
[STALL...]
F1B1FC: MOVEI  #$00F1A100, R01 [NCZ:001, R01=00F1A100] -> [NCZ:001, R01=00F1A100]
*/

uint32_t pcQueue1[0x400];
uint32_t pcQPtr1 = 0;
static uint32_t prevR1;
//Let's try a 3 stage pipeline....
//Looks like 3 stage is correct, otherwise bad things happen...
void DSPExecP2(int32_t cycles)
{
	dsp_releaseTimeSlice_flag = 0;
	dsp_in_exec++;

	while (cycles > 0 && DSP_RUNNING)
	{
/*extern uint32_t totalFrames;
//F1B2F6: LOAD   (R14+$04), R24 [NCZ:001, R14+$04=00F20018, R24=FFFFFFFF] -> Jaguar: Unknown word read at 00F20018 by DSP (M68K PC=00E32E)
//-> 43 + 1 + 24 -> $2B + $01 + $18 -> 101011 00001 11000 -> 1010 1100 0011 1000 -> AC38
//C470 -> 1100 0100 0111 0000 -> 110001 00011 10000 -> 49, 3, 16 -> STORE R16, (R14+$0C)
//F1B140:
if (totalFrames >= 377 && GET16(dsp_ram_8, 0x0002F6) == 0xAC38 && dsp_pc == 0xF1B140)
{
	doDSPDis = true;
	WriteLog("Starting disassembly at frame #%u...\n", totalFrames);
}
if (dsp_pc == 0xF1B092)
	doDSPDis = false;//*/
/*if (totalFrames >= 373 && GET16(dsp_ram_8, 0x0002F6) == 0xAC38)
	doDSPDis = true;//*/
/*if (totalFrames >= 373 && dsp_pc == 0xF1B0A0)
	doDSPDis = true;//*/
/*if (dsp_pc == 0xF1B0A0)
	doDSPDis = true;//*/
/*if (dsp_pc == 0xF1B0D2) && dsp_reg[1] == 0x2140C)
	doDSPDis = true;//*/
//Two parter... (not sure how to write this)
//if (dsp_pc == 0xF1B0D2)
//	prevR1 = dsp_reg[1];

//F1B0D2: ADDQT  #8, R01 [NCZ:000, R01=0002140C] -> [NCZ:000, R01=00021414]
//F1B0D2: ADDQT  #8, R01 [NCZ:000, R01=0002140C] -> [NCZ:000, R01=00021414]


pcQueue1[pcQPtr1++] = dsp_pc;
pcQPtr1 &= 0x3FF;

#ifdef DSP_DEBUG_PL2
if ((dsp_pc < 0xF1B000 || dsp_pc > 0xF1CFFF) && !doDSPDis)
{
	WriteLog("DSP: PC has stepped out of bounds...\n\nBacktrace:\n\n");
	doDSPDis = true;

	char buffer[512];

	for(int i=0; i<0x400; i++)
	{
		dasmjag(JAGUAR_DSP, buffer, pcQueue1[(i + pcQPtr1) & 0x3FF]);
		WriteLog("\t%08X: %s\n", pcQueue1[(i + pcQPtr1) & 0x3FF], buffer);
	}
	WriteLog("\n");
}//*/
#endif

		if (IMASKCleared)						// If IMASK was cleared,
		{
#ifdef DSP_DEBUG_IRQ
			WriteLog("DSP: Finished interrupt.\n");
#endif
			DSPHandleIRQs();					// See if any other interrupts are pending!
			IMASKCleared = false;
		}

//if (dsp_flags & REGPAGE)
//	WriteLog("  --> REGPAGE has just been set!\n");
#ifdef DSP_DEBUG_PL2
if (doDSPDis)
{
WriteLog("DSPExecP: Pipeline status [PC=%08X]...\n", dsp_pc);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister, dsp_opcode_str[pipeline[plPtrWrite].opcode]);
WriteLog("  --> Scoreboard: ");
for(int i=0; i<32; i++)
	WriteLog("%s ", scoreboard[i] ? "T" : "F");
WriteLog("\n");
}
#endif
		// Stage 1a: Instruction fetch
		pipeline[plPtrRead].instruction = DSPReadWord(dsp_pc, DSP);
		pipeline[plPtrRead].opcode = pipeline[plPtrRead].instruction >> 10;
		pipeline[plPtrRead].operand1 = (pipeline[plPtrRead].instruction >> 5) & 0x1F;
		pipeline[plPtrRead].operand2 = pipeline[plPtrRead].instruction & 0x1F;
		if (pipeline[plPtrRead].opcode == 38)
			pipeline[plPtrRead].result = (uint32_t)DSPReadWord(dsp_pc + 2, DSP)
				| ((uint32_t)DSPReadWord(dsp_pc + 4, DSP) << 16);
#ifdef DSP_DEBUG_PL2
if (doDSPDis)
{
WriteLog("DSPExecP: Fetching instruction (%04X) from DSP_PC = %08X...\n", pipeline[plPtrRead].instruction, dsp_pc);
WriteLog("DSPExecP: Pipeline status (after stage 1a) [PC=%08X]...\n", dsp_pc);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister, dsp_opcode_str[pipeline[plPtrWrite].opcode]);
}
#endif
		// Stage 1b: Read registers
//Small problem--when say LOAD or STORE (R14/5+$nn) is executed AFTER an instruction that
//modifies R14/5, we don't check the scoreboard for R14/5 (and we need to!)... !!! FIX !!!
//Ugly, but [DONE]
//Another problem: Any sequential combination of LOAD and STORE operations will cause the
//pipeline to stall, and we don't take care of that here. !!! FIX !!!
		if ((scoreboard[pipeline[plPtrRead].operand1] && readAffected[pipeline[plPtrRead].opcode][0])
			|| (scoreboard[pipeline[plPtrRead].operand2] && readAffected[pipeline[plPtrRead].opcode][1])
			|| ((pipeline[plPtrRead].opcode == 43 || pipeline[plPtrRead].opcode == 58) && scoreboard[14])
			|| ((pipeline[plPtrRead].opcode == 44 || pipeline[plPtrRead].opcode == 59) && scoreboard[15])
//Not sure that this is the best way to fix the LOAD/STORE problem... But it seems to
//work--somewhat...
			|| (isLoadStore[pipeline[plPtrRead].opcode] && isLoadStore[pipeline[plPtrExec].opcode]))
			// We have a hit in the scoreboard, so we have to stall the pipeline...
#ifdef DSP_DEBUG_PL2
{
if (doDSPDis)
{
WriteLog("  --> Stalling pipeline: ");
if (readAffected[pipeline[plPtrRead].opcode][0])
	WriteLog("scoreboard[%u] = %s (reg 1) ", pipeline[plPtrRead].operand1, scoreboard[pipeline[plPtrRead].operand1] ? "true" : "false");
if (readAffected[pipeline[plPtrRead].opcode][1])
	WriteLog("scoreboard[%u] = %s (reg 2)", pipeline[plPtrRead].operand2, scoreboard[pipeline[plPtrRead].operand2] ? "true" : "false");
WriteLog("\n");
}
#endif
			pipeline[plPtrRead].opcode = PIPELINE_STALL;
#ifdef DSP_DEBUG_PL2
}
#endif
		else
		{
			pipeline[plPtrRead].reg1 = dsp_reg[pipeline[plPtrRead].operand1];
			pipeline[plPtrRead].reg2 = dsp_reg[pipeline[plPtrRead].operand2];
			pipeline[plPtrRead].writebackRegister = pipeline[plPtrRead].operand2;	// Set it to RN

			// Shouldn't we be more selective with the register scoreboarding?
			// Yes, we should. !!! FIX !!! Kinda [DONE]
#ifndef NEW_SCOREBOARD
			scoreboard[pipeline[plPtrRead].operand2] = affectsScoreboard[pipeline[plPtrRead].opcode];
#else
//Hopefully this will fix the dual MOVEQ # problem...
			scoreboard[pipeline[plPtrRead].operand2] += (affectsScoreboard[pipeline[plPtrRead].opcode] ? 1 : 0);
#endif

//Advance PC here??? Yes.
			dsp_pc += (pipeline[plPtrRead].opcode == 38 ? 6 : 2);
		}

#ifdef DSP_DEBUG_PL2
if (doDSPDis)
{
WriteLog("DSPExecP: Pipeline status (after stage 1b) [PC=%08X]...\n", dsp_pc);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister, dsp_opcode_str[pipeline[plPtrWrite].opcode]);
}
#endif
		// Stage 2: Execute
		if (pipeline[plPtrExec].opcode != PIPELINE_STALL)
		{
#ifdef DSP_DEBUG_PL2
if (doDSPDis)
	WriteLog("\t[inst=%02u][R28=%08X, alt R28=%08X, REGPAGE=%s]\n", pipeline[plPtrExec].opcode, dsp_reg[28], dsp_alternate_reg[28], (dsp_flags & REGPAGE ? "set" : "not set"));

if (doDSPDis)
{
WriteLog("DSPExecP: About to execute opcode %s...\n", dsp_opcode_str[pipeline[plPtrExec].opcode]);
}
#endif
//CC only!
#ifdef DSP_DEBUG_CC
lastExec = pipeline[plPtrExec].instruction;
//WriteLog("[lastExec = %04X]\n", lastExec);
#endif
			cycles -= dsp_opcode_cycles[pipeline[plPtrExec].opcode];
			dsp_opcode_use[pipeline[plPtrExec].opcode]++;
			DSPOpcode[pipeline[plPtrExec].opcode]();
//WriteLog("    --> Returned from execute. DSP_PC: %08X\n", dsp_pc);
		}
		else
{
//Let's not, until we do the stalling correctly...
//But, we gotta while we're doing the comparison core...!
//Or do we?			cycles--;
//Really, the whole thing is wrong. When the pipeline is correctly stuffed, most instructions
//will execute in one clock cycle (others, like DIV, will likely not). So, the challenge is
//to model this clock cycle behavior correctly...
//Also, the pipeline stalls too much--mostly because the transparent writebacks at stage 3
//don't affect the reads at stage 1...
#ifdef DSP_DEBUG_STALL
if (doDSPDis)
	WriteLog("[STALL... DSP_PC = %08X]\n", dsp_pc);
#endif
}

#ifdef DSP_DEBUG_PL2
if (doDSPDis)
{
WriteLog("DSPExecP: Pipeline status (after stage 2) [PC=%08X]...\n", dsp_pc);
WriteLog("\tR -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
WriteLog("\tW -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrWrite].opcode, pipeline[plPtrWrite].operand1, pipeline[plPtrWrite].operand2, pipeline[plPtrWrite].reg1, pipeline[plPtrWrite].reg2, pipeline[plPtrWrite].result, pipeline[plPtrWrite].writebackRegister, dsp_opcode_str[pipeline[plPtrWrite].opcode]);
WriteLog("\n");
}
#endif
		// Stage 3: Write back register/memory address
		if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
		{
/*if (pipeline[plPtrWrite].writebackRegister == 3
	&& (pipeline[plPtrWrite].result < 0xF14000 || pipeline[plPtrWrite].result > 0xF1CFFF)
	&& !doDSPDis)
{
	WriteLog("DSP: Register R03 has stepped out of bounds...\n\n");
	doDSPDis = true;
}//*/
			if (pipeline[plPtrWrite].writebackRegister != 0xFF)
			{
				if (pipeline[plPtrWrite].writebackRegister != 0xFE)
					dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;
				else
				{
					if (pipeline[plPtrWrite].type == TYPE_BYTE)
						JaguarWriteByte(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
					else if (pipeline[plPtrWrite].type == TYPE_WORD)
						JaguarWriteWord(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
					else
						JaguarWriteLong(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
				}
			}

#ifndef NEW_SCOREBOARD
			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				scoreboard[pipeline[plPtrWrite].operand2] = false;
#else
//Yup, sequential MOVEQ # problem fixing (I hope!)...
			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				if (scoreboard[pipeline[plPtrWrite].operand2])
					scoreboard[pipeline[plPtrWrite].operand2]--;
#endif
		}

		// Push instructions through the pipeline...
		plPtrRead = (plPtrRead + 1) & 0x03;
		plPtrExec = (plPtrExec + 1) & 0x03;
		plPtrWrite = (plPtrWrite + 1) & 0x03;
	}

	dsp_in_exec--;
}



/*
//#define DSP_DEBUG_PL3
//Let's try a 2 stage pipeline....
void DSPExecP3(int32_t cycles)
{
	dsp_releaseTimeSlice_flag = 0;
	dsp_in_exec++;

	while (cycles > 0 && DSP_RUNNING)
	{
//if (dsp_pc < 0xF1B000 || dsp_pc > 0xF1CFFF)
//	doDSPDis = true;
#ifdef DSP_DEBUG_PL3
WriteLog("DSPExecP: Pipeline status...\n");
WriteLog("\tF/R -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE/W -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
WriteLog("  --> Scoreboard: ");
for(int i=0; i<32; i++)
	WriteLog("%s ", scoreboard[i] ? "T" : "F");
WriteLog("\n");
#endif
		// Stage 1a: Instruction fetch
		pipeline[plPtrRead].instruction = DSPReadWord(dsp_pc, DSP);
		pipeline[plPtrRead].opcode = pipeline[plPtrRead].instruction >> 10;
		pipeline[plPtrRead].operand1 = (pipeline[plPtrRead].instruction >> 5) & 0x1F;
		pipeline[plPtrRead].operand2 = pipeline[plPtrRead].instruction & 0x1F;
		if (pipeline[plPtrRead].opcode == 38)
			pipeline[plPtrRead].result = (uint32_t)DSPReadWord(dsp_pc + 2, DSP)
				| ((uint32_t)DSPReadWord(dsp_pc + 4, DSP) << 16);
#ifdef DSP_DEBUG_PL3
WriteLog("DSPExecP: Fetching instruction (%04X) from DSP_PC = %08X...\n", pipeline[plPtrRead].instruction, dsp_pc);
WriteLog("DSPExecP: Pipeline status (after stage 1a)...\n");
WriteLog("\tF/R -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE/W -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
#endif
		// Stage 1b: Read registers
		if ((scoreboard[pipeline[plPtrRead].operand1] && readAffected[pipeline[plPtrRead].opcode][0])
			|| (scoreboard[pipeline[plPtrRead].operand2] && readAffected[pipeline[plPtrRead].opcode][1]))
			// We have a hit in the scoreboard, so we have to stall the pipeline...
#ifdef DSP_DEBUG_PL3
{
WriteLog("  --> Stalling pipeline: ");
if (readAffected[pipeline[plPtrRead].opcode][0])
	WriteLog("scoreboard[%u] = %s (reg 1) ", pipeline[plPtrRead].operand1, scoreboard[pipeline[plPtrRead].operand1] ? "true" : "false");
if (readAffected[pipeline[plPtrRead].opcode][1])
	WriteLog("scoreboard[%u] = %s (reg 2)", pipeline[plPtrRead].operand2, scoreboard[pipeline[plPtrRead].operand2] ? "true" : "false");
WriteLog("\n");
#endif
			pipeline[plPtrRead].opcode = PIPELINE_STALL;
#ifdef DSP_DEBUG_PL3
}
#endif
		else
		{
			pipeline[plPtrRead].reg1 = dsp_reg[pipeline[plPtrRead].operand1];
			pipeline[plPtrRead].reg2 = dsp_reg[pipeline[plPtrRead].operand2];
			pipeline[plPtrRead].writebackRegister = pipeline[plPtrRead].operand2;	// Set it to RN

			// Shouldn't we be more selective with the register scoreboarding?
			// Yes, we should. !!! FIX !!! [Kinda DONE]
			scoreboard[pipeline[plPtrRead].operand2] = affectsScoreboard[pipeline[plPtrRead].opcode];

//Advance PC here??? Yes.
			dsp_pc += (pipeline[plPtrRead].opcode == 38 ? 6 : 2);
		}

#ifdef DSP_DEBUG_PL3
WriteLog("DSPExecP: Pipeline status (after stage 1b)...\n");
WriteLog("\tF/R -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE/W -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
#endif
		// Stage 2a: Execute
		if (pipeline[plPtrExec].opcode != PIPELINE_STALL)
		{
#ifdef DSP_DEBUG_PL3
WriteLog("DSPExecP: About to execute opcode %s...\n", dsp_opcode_str[pipeline[plPtrExec].opcode]);
#endif
			DSPOpcode[pipeline[plPtrExec].opcode]();
			dsp_opcode_use[pipeline[plPtrExec].opcode]++;
			cycles -= dsp_opcode_cycles[pipeline[plPtrExec].opcode];
		}
		else
			cycles--;

#ifdef DSP_DEBUG_PL3
WriteLog("DSPExecP: Pipeline status (after stage 2a)...\n");
WriteLog("\tF/R -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrRead].opcode, pipeline[plPtrRead].operand1, pipeline[plPtrRead].operand2, pipeline[plPtrRead].reg1, pipeline[plPtrRead].reg2, pipeline[plPtrRead].result, pipeline[plPtrRead].writebackRegister, dsp_opcode_str[pipeline[plPtrRead].opcode]);
WriteLog("\tE/W -> %02u, %02u, %02u; r1=%08X, r2= %08X, res=%08X, wb=%u (%s)\n", pipeline[plPtrExec].opcode, pipeline[plPtrExec].operand1, pipeline[plPtrExec].operand2, pipeline[plPtrExec].reg1, pipeline[plPtrExec].reg2, pipeline[plPtrExec].result, pipeline[plPtrExec].writebackRegister, dsp_opcode_str[pipeline[plPtrExec].opcode]);
WriteLog("\n");
#endif
		// Stage 2b: Write back register
		if (pipeline[plPtrExec].opcode != PIPELINE_STALL)
		{
			if (pipeline[plPtrExec].writebackRegister != 0xFF)
				dsp_reg[pipeline[plPtrExec].writebackRegister] = pipeline[plPtrExec].result;

			if (affectsScoreboard[pipeline[plPtrExec].opcode])
				scoreboard[pipeline[plPtrExec].operand2] = false;
		}

		// Push instructions through the pipeline...
		plPtrRead = (++plPtrRead) & 0x03;
		plPtrExec = (++plPtrExec) & 0x03;
	}

	dsp_in_exec--;
}*/

//
// DSP pipelined opcode handlers
//

#define PRM				pipeline[plPtrExec].reg1
#define PRN				pipeline[plPtrExec].reg2
#define PIMM1			pipeline[plPtrExec].operand1
#define PIMM2			pipeline[plPtrExec].operand2
#define PRES			pipeline[plPtrExec].result
#define PWBR			pipeline[plPtrExec].writebackRegister
#define NO_WRITEBACK	pipeline[plPtrExec].writebackRegister = 0xFF
//#define DSP_PPC			dsp_pc - (pipeline[plPtrRead].opcode == 38 ? 6 : 2) - (pipeline[plPtrExec].opcode == 38 ? 6 : 2)
#define DSP_PPC			dsp_pc - (pipeline[plPtrRead].opcode == 38 ? 6 : (pipeline[plPtrRead].opcode == PIPELINE_STALL ? 0 : 2)) - (pipeline[plPtrExec].opcode == 38 ? 6 : (pipeline[plPtrExec].opcode == PIPELINE_STALL ? 0 : 2))
#define WRITEBACK_ADDR	pipeline[plPtrExec].writebackRegister = 0xFE

static void DSP_abs(void)
{
#ifdef DSP_DIS_ABS
	if (doDSPDis)
		WriteLog("%06X: ABS    R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	uint32_t _Rn = PRN;

	if (_Rn == 0x80000000)
		dsp_flag_n = 1;
	else
	{
		dsp_flag_c = ((_Rn & 0x80000000) >> 31);
		PRES = (_Rn & 0x80000000 ? -_Rn : _Rn);
		CLR_ZN; SET_Z(PRES);
	}
#ifdef DSP_DIS_ABS
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_add(void)
{
#ifdef DSP_DIS_ADD
	if (doDSPDis)
		WriteLog("%06X: ADD    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	uint32_t res = PRN + PRM;
	SET_ZNC_ADD(PRN, PRM, res);
	PRES = res;
#ifdef DSP_DIS_ADD
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_addc(void)
{
#ifdef DSP_DIS_ADDC
	if (doDSPDis)
		WriteLog("%06X: ADDC   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	uint32_t res = PRN + PRM + dsp_flag_c;
	uint32_t carry = dsp_flag_c;
//	SET_ZNC_ADD(PRN, PRM, res); //???BUG??? Yes!
	SET_ZNC_ADD(PRN + carry, PRM, res);
//	SET_ZNC_ADD(PRN, PRM + carry, res);
	PRES = res;
#ifdef DSP_DIS_ADDC
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_addq(void)
{
#ifdef DSP_DIS_ADDQ
	if (doDSPDis)
		WriteLog("%06X: ADDQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	uint32_t r1 = dsp_convert_zero[PIMM1];
	uint32_t res = PRN + r1;
	CLR_ZNC; SET_ZNC_ADD(PRN, r1, res);
	PRES = res;
#ifdef DSP_DIS_ADDQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_addqmod(void)
{
#ifdef DSP_DIS_ADDQMOD
	if (doDSPDis)
		WriteLog("%06X: ADDQMOD #%u, R%02u [NCZ:%u%u%u, R%02u=%08X, DSP_MOD=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN, dsp_modulo);
#endif
	uint32_t r1 = dsp_convert_zero[PIMM1];
	uint32_t r2 = PRN;
	uint32_t res = r2 + r1;
	res = (res & (~dsp_modulo)) | (r2 & dsp_modulo);
	PRES = res;
	SET_ZNC_ADD(r2, r1, res);
#ifdef DSP_DIS_ADDQMOD
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_addqt(void)
{
#ifdef DSP_DIS_ADDQT
	if (doDSPDis)
		WriteLog("%06X: ADDQT  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	PRES = PRN + dsp_convert_zero[PIMM1];
#ifdef DSP_DIS_ADDQT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_and(void)
{
#ifdef DSP_DIS_AND
	if (doDSPDis)
		WriteLog("%06X: AND    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	PRES = PRN & PRM;
	SET_ZN(PRES);
#ifdef DSP_DIS_AND
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_bclr(void)
{
#ifdef DSP_DIS_BCLR
	if (doDSPDis)
		WriteLog("%06X: BCLR   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	PRES = PRN & ~(1 << PIMM1);
	SET_ZN(PRES);
#ifdef DSP_DIS_BCLR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_bset(void)
{
#ifdef DSP_DIS_BSET
	if (doDSPDis)
		WriteLog("%06X: BSET   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	PRES = PRN | (1 << PIMM1);
	SET_ZN(PRES);
#ifdef DSP_DIS_BSET
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_btst(void)
{
#ifdef DSP_DIS_BTST
	if (doDSPDis)
		WriteLog("%06X: BTST   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	dsp_flag_z = (~PRN >> PIMM1) & 1;
	NO_WRITEBACK;
#ifdef DSP_DIS_BTST
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
}

static void DSP_cmp(void)
{
#ifdef DSP_DIS_CMP
	if (doDSPDis)
		WriteLog("%06X: CMP    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	uint32_t res = PRN - PRM;
	SET_ZNC_SUB(PRN, PRM, res);
	NO_WRITEBACK;
#ifdef DSP_DIS_CMP
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
}

static void DSP_cmpq(void)
{
	static int32_t sqtable[32] =
		{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,-16,-15,-14,-13,-12,-11,-10,-9,-8,-7,-6,-5,-4,-3,-2,-1 };
#ifdef DSP_DIS_CMPQ
	if (doDSPDis)
		WriteLog("%06X: CMPQ   #%d, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, sqtable[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	uint32_t r1 = sqtable[PIMM1 & 0x1F]; // I like this better -> (INT8)(jaguar.op >> 2) >> 3;
	uint32_t res = PRN - r1;
	SET_ZNC_SUB(PRN, r1, res);
	NO_WRITEBACK;
#ifdef DSP_DIS_CMPQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
}

static void DSP_div(void)
{
	uint32_t _Rm = PRM, _Rn = PRN;

	if (_Rm)
	{
		if (dsp_div_control & 1)
		{
			dsp_remain = (((uint64_t)_Rn) << 16) % _Rm;
			if (dsp_remain & 0x80000000)
				dsp_remain -= _Rm;
			PRES = (((uint64_t)_Rn) << 16) / _Rm;
		}
		else
		{
			dsp_remain = _Rn % _Rm;
			if (dsp_remain & 0x80000000)
				dsp_remain -= _Rm;
			PRES = PRN / _Rm;
		}
	}
	else
		PRES = 0xFFFFFFFF;
}

static void DSP_imacn(void)
{
#ifdef DSP_DIS_IMACN
	if (doDSPDis)
		WriteLog("%06X: IMACN  R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	int32_t res = (int16_t)PRM * (int16_t)PRN;
	dsp_acc += (int64_t)res;
//Should we AND the result to fit into 40 bits here???
	NO_WRITEBACK;
#ifdef DSP_DIS_IMACN
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, DSP_ACC=%02X%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, (uint8_t)(dsp_acc >> 32), (uint32_t)(dsp_acc & 0xFFFFFFFF));
#endif
}

static void DSP_imult(void)
{
#ifdef DSP_DIS_IMULT
	if (doDSPDis)
		WriteLog("%06X: IMULT  R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	PRES = (int16_t)PRN * (int16_t)PRM;
	SET_ZN(PRES);
#ifdef DSP_DIS_IMULT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_imultn(void)
{
#ifdef DSP_DIS_IMULTN
	if (doDSPDis)
		WriteLog("%06X: IMULTN R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	// This is OK, since this multiply won't overflow 32 bits...
	int32_t res = (int32_t)((int16_t)PRN * (int16_t)PRM);
	dsp_acc = (int64_t)res;
	SET_ZN(res);
	NO_WRITEBACK;
#ifdef DSP_DIS_IMULTN
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, DSP_ACC=%02X%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, (uint8_t)(dsp_acc >> 32), (uint32_t)(dsp_acc & 0xFFFFFFFF));
#endif
}

static void DSP_illegal(void)
{
#ifdef DSP_DIS_ILLEGAL
	if (doDSPDis)
		WriteLog("%06X: ILLEGAL [NCZ:%u%u%u]\n", DSP_PPC, dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
	NO_WRITEBACK;
}

// There is a problem here with interrupt handlers the JUMP and JR instructions that
// can cause trouble because an interrupt can occur *before* the instruction following the
// jump can execute... !!! FIX !!!
// This can probably be solved by judicious coding in the pipeline execution core...
// And should be fixed now...
static void DSP_jr(void)
{
#ifdef DSP_DIS_JR
const char * condition[32] =
{	"T", "nz", "z", "???", "nc", "nc nz", "nc z", "???", "c", "c nz",
	"c z", "???", "???", "???", "???", "???", "???", "???", "???",
	"???", "nn", "nn nz", "nn z", "???", "n", "n nz", "n z", "???",
	"???", "???", "???", "F" };
	if (doDSPDis)
//How come this is always off by 2???
		WriteLog("%06X: JR     %s, %06X [NCZ:%u%u%u] ", DSP_PPC, condition[PIMM2], DSP_PPC+((PIMM1 & 0x10 ? 0xFFFFFFF0 | PIMM1 : PIMM1) * 2)+2, dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
	// KLUDGE: Used by BRANCH_CONDITION macro
	uint32_t jaguar_flags = (dsp_flag_n << 2) | (dsp_flag_c << 1) | dsp_flag_z;

	if (BRANCH_CONDITION(PIMM2))
	{
#ifdef DSP_DIS_JR
	if (doDSPDis)
		WriteLog("Branched!\n");
#endif
		int32_t offset = (PIMM1 & 0x10 ? 0xFFFFFFF0 | PIMM1 : PIMM1);		// Sign extend PIMM1
//Account for pipeline effects...
		uint32_t newPC = dsp_pc + (offset * 2) - (pipeline[plPtrRead].opcode == 38 ? 6 : (pipeline[plPtrRead].opcode == PIPELINE_STALL ? 0 : 2));
//WriteLog("  --> Old PC: %08X, new PC: %08X\n", dsp_pc, newPC);

		// Now that we've branched, we have to make sure that the following instruction
		// is executed atomically with this one and then flush the pipeline before setting
		// the new PC.

		// Step 1: Handle writebacks at stage 3 of pipeline
/*		if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
		{
			if (pipeline[plPtrWrite].writebackRegister != 0xFF)
				dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;

			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				scoreboard[pipeline[plPtrWrite].operand2] = false;
		}//*/
		if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
		{
			if (pipeline[plPtrWrite].writebackRegister != 0xFF)
			{
				if (pipeline[plPtrWrite].writebackRegister != 0xFE)
					dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;
				else
				{
					if (pipeline[plPtrWrite].type == TYPE_BYTE)
						JaguarWriteByte(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
					else if (pipeline[plPtrWrite].type == TYPE_WORD)
						JaguarWriteWord(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
					else
						JaguarWriteLong(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
				}
			}

#ifndef NEW_SCOREBOARD
			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				scoreboard[pipeline[plPtrWrite].operand2] = false;
#else
//Yup, sequential MOVEQ # problem fixing (I hope!)...
			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				if (scoreboard[pipeline[plPtrWrite].operand2])
					scoreboard[pipeline[plPtrWrite].operand2]--;
#endif
		}

		// Step 2: Push instruction through pipeline & execute following instruction
		// NOTE: By putting our following instruction at stage 3 of the pipeline,
		//       we effectively handle the final push of the instruction through the
		//       pipeline when the new PC takes effect (since when we return, the
		//       pipeline code will be executing the writeback stage. If we reverse
		//       the execution order of the pipeline stages, this will no longer be
		//       the case!)...
		pipeline[plPtrExec] = pipeline[plPtrRead];
//This is BAD. We need to get that next opcode and execute it!
//NOTE: The problem is here because of a bad stall. Once those are fixed, we can probably
//      remove this crap.
		if (pipeline[plPtrExec].opcode == PIPELINE_STALL)
		{
		uint16_t instruction = DSPReadWord(dsp_pc, DSP);
		pipeline[plPtrExec].opcode = instruction >> 10;
		pipeline[plPtrExec].operand1 = (instruction >> 5) & 0x1F;
		pipeline[plPtrExec].operand2 = instruction & 0x1F;
			pipeline[plPtrExec].reg1 = dsp_reg[pipeline[plPtrExec].operand1];
			pipeline[plPtrExec].reg2 = dsp_reg[pipeline[plPtrExec].operand2];
			pipeline[plPtrExec].writebackRegister = pipeline[plPtrExec].operand2;	// Set it to RN
		}//*/
	dsp_pc += 2;	// For DSP_DIS_* accuracy
		DSPOpcode[pipeline[plPtrExec].opcode]();
		dsp_opcode_use[pipeline[plPtrExec].opcode]++;
		pipeline[plPtrWrite] = pipeline[plPtrExec];

		// Step 3: Flush pipeline & set new PC
		pipeline[plPtrRead].opcode = pipeline[plPtrExec].opcode = PIPELINE_STALL;
		dsp_pc = newPC;
	}
	else
#ifdef DSP_DIS_JR
	{
		if (doDSPDis)
			WriteLog("Branch NOT taken.\n");
#endif
		NO_WRITEBACK;
#ifdef DSP_DIS_JR
	}
#endif
//	WriteLog("  --> DSP_PC: %08X\n", dsp_pc);
}

static void DSP_jump(void)
{
#ifdef DSP_DIS_JUMP
const char * condition[32] =
{	"T", "nz", "z", "???", "nc", "nc nz", "nc z", "???", "c", "c nz",
	"c z", "???", "???", "???", "???", "???", "???", "???", "???",
	"???", "nn", "nn nz", "nn z", "???", "n", "n nz", "n z", "???",
	"???", "???", "???", "F" };
	if (doDSPDis)
		WriteLog("%06X: JUMP   %s, (R%02u) [NCZ:%u%u%u, R%02u=%08X] ", DSP_PPC, condition[PIMM2], PIMM1, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM);
#endif
	// KLUDGE: Used by BRANCH_CONDITION macro
	uint32_t jaguar_flags = (dsp_flag_n << 2) | (dsp_flag_c << 1) | dsp_flag_z;

	if (BRANCH_CONDITION(PIMM2))
	{
#ifdef DSP_DIS_JUMP
	if (doDSPDis)
		WriteLog("Branched!\n");
#endif
		uint32_t PCSave = PRM;
		// Now that we've branched, we have to make sure that the following instruction
		// is executed atomically with this one and then flush the pipeline before setting
		// the new PC.

		// Step 1: Handle writebacks at stage 3 of pipeline
/*		if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
		{
			if (pipeline[plPtrWrite].writebackRegister != 0xFF)
				dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;

			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				scoreboard[pipeline[plPtrWrite].operand2] = false;
		}//*/
		if (pipeline[plPtrWrite].opcode != PIPELINE_STALL)
		{
			if (pipeline[plPtrWrite].writebackRegister != 0xFF)
			{
				if (pipeline[plPtrWrite].writebackRegister != 0xFE)
					dsp_reg[pipeline[plPtrWrite].writebackRegister] = pipeline[plPtrWrite].result;
				else
				{
					if (pipeline[plPtrWrite].type == TYPE_BYTE)
						JaguarWriteByte(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
					else if (pipeline[plPtrWrite].type == TYPE_WORD)
						JaguarWriteWord(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
					else
						JaguarWriteLong(pipeline[plPtrWrite].address, pipeline[plPtrWrite].value);
				}
			}

#ifndef NEW_SCOREBOARD
			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				scoreboard[pipeline[plPtrWrite].operand2] = false;
#else
//Yup, sequential MOVEQ # problem fixing (I hope!)...
			if (affectsScoreboard[pipeline[plPtrWrite].opcode])
				if (scoreboard[pipeline[plPtrWrite].operand2])
					scoreboard[pipeline[plPtrWrite].operand2]--;
#endif
		}

		// Step 2: Push instruction through pipeline & execute following instruction
		// NOTE: By putting our following instruction at stage 3 of the pipeline,
		//       we effectively handle the final push of the instruction through the
		//       pipeline when the new PC takes effect (since when we return, the
		//       pipeline code will be executing the writeback stage. If we reverse
		//       the execution order of the pipeline stages, this will no longer be
		//       the case!)...
		pipeline[plPtrExec] = pipeline[plPtrRead];
//This is BAD. We need to get that next opcode and execute it!
//Also, same problem in JR!
//NOTE: The problem is here because of a bad stall. Once those are fixed, we can probably
//      remove this crap.
		if (pipeline[plPtrExec].opcode == PIPELINE_STALL)
		{
		uint16_t instruction = DSPReadWord(dsp_pc, DSP);
		pipeline[plPtrExec].opcode = instruction >> 10;
		pipeline[plPtrExec].operand1 = (instruction >> 5) & 0x1F;
		pipeline[plPtrExec].operand2 = instruction & 0x1F;
			pipeline[plPtrExec].reg1 = dsp_reg[pipeline[plPtrExec].operand1];
			pipeline[plPtrExec].reg2 = dsp_reg[pipeline[plPtrExec].operand2];
			pipeline[plPtrExec].writebackRegister = pipeline[plPtrExec].operand2;	// Set it to RN
		}//*/
	dsp_pc += 2;	// For DSP_DIS_* accuracy
		DSPOpcode[pipeline[plPtrExec].opcode]();
		dsp_opcode_use[pipeline[plPtrExec].opcode]++;
		pipeline[plPtrWrite] = pipeline[plPtrExec];

		// Step 3: Flush pipeline & set new PC
		pipeline[plPtrRead].opcode = pipeline[plPtrExec].opcode = PIPELINE_STALL;
		dsp_pc = PCSave;
	}
	else
#ifdef DSP_DIS_JUMP
	{
		if (doDSPDis)
			WriteLog("Branch NOT taken.\n");
#endif
		NO_WRITEBACK;
#ifdef DSP_DIS_JUMP
	}
#endif
}

static void DSP_load(void)
{
#ifdef DSP_DIS_LOAD
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R%02u), R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	PRES = DSPReadLong(PRM & 0xFFFFFFFC, DSP);
#else
	PRES = DSPReadLong(PRM, DSP);
#endif
#ifdef DSP_DIS_LOAD
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_loadb(void)
{
#ifdef DSP_DIS_LOADB
	if (doDSPDis)
		WriteLog("%06X: LOADB  (R%02u), R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	if (PRM >= DSP_WORK_RAM_BASE && PRM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		PRES = DSPReadLong(PRM, DSP) & 0xFF;
	else
		PRES = JaguarReadByte(PRM, DSP);
#ifdef DSP_DIS_LOADB
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_loadw(void)
{
#ifdef DSP_DIS_LOADW
	if (doDSPDis)
		WriteLog("%06X: LOADW  (R%02u), R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	if (PRM >= DSP_WORK_RAM_BASE && PRM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		PRES = DSPReadLong(PRM & 0xFFFFFFFE, DSP) & 0xFFFF;
	else
		PRES = JaguarReadWord(PRM & 0xFFFFFFFE, DSP);
#else
	if (PRM >= DSP_WORK_RAM_BASE && PRM <= (DSP_WORK_RAM_BASE + 0x1FFF))
		PRES = DSPReadLong(PRM, DSP) & 0xFFFF;
	else
		PRES = JaguarReadWord(PRM, DSP);
#endif
#ifdef DSP_DIS_LOADW
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_load_r14_i(void)
{
#ifdef DSP_DIS_LOAD14I
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R14+$%02X), R%02u [NCZ:%u%u%u, R14+$%02X=%08X, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1] << 2, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, dsp_convert_zero[PIMM1] << 2, dsp_reg[14]+(dsp_convert_zero[PIMM1] << 2), PIMM2, PRN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	PRES = DSPReadLong((dsp_reg[14] & 0xFFFFFFFC) + (dsp_convert_zero[PIMM1] << 2), DSP);
#else
	PRES = DSPReadLong(dsp_reg[14] + (dsp_convert_zero[PIMM1] << 2), DSP);
#endif
#ifdef DSP_DIS_LOAD14I
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_load_r14_r(void)
{
#ifdef DSP_DIS_LOAD14R
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R14+R%02u), R%02u [NCZ:%u%u%u, R14+R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM+dsp_reg[14], PIMM2, PRES);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	PRES = DSPReadLong((dsp_reg[14] + PRM) & 0xFFFFFFFC, DSP);
#else
	PRES = DSPReadLong(dsp_reg[14] + PRM, DSP);
#endif
#ifdef DSP_DIS_LOAD14R
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_load_r15_i(void)
{
#ifdef DSP_DIS_LOAD15I
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R15+$%02X), R%02u [NCZ:%u%u%u, R15+$%02X=%08X, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1] << 2, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, dsp_convert_zero[PIMM1] << 2, dsp_reg[15]+(dsp_convert_zero[PIMM1] << 2), PIMM2, PRN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	PRES = DSPReadLong((dsp_reg[15] &0xFFFFFFFC) + (dsp_convert_zero[PIMM1] << 2), DSP);
#else
	PRES = DSPReadLong(dsp_reg[15] + (dsp_convert_zero[PIMM1] << 2), DSP);
#endif
#ifdef DSP_DIS_LOAD15I
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_load_r15_r(void)
{
#ifdef DSP_DIS_LOAD15R
	if (doDSPDis)
		WriteLog("%06X: LOAD   (R15+R%02u), R%02u [NCZ:%u%u%u, R15+R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM+dsp_reg[15], PIMM2, PRN);
#endif
#ifdef DSP_CORRECT_ALIGNMENT
	PRES = DSPReadLong((dsp_reg[15] + PRM) & 0xFFFFFFFC, DSP);
#else
	PRES = DSPReadLong(dsp_reg[15] + PRM, DSP);
#endif
#ifdef DSP_DIS_LOAD15R
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_mirror(void)
{
	uint32_t r1 = PRN;
	PRES = (mirror_table[r1 & 0xFFFF] << 16) | mirror_table[r1 >> 16];
	SET_ZN(PRES);
}

static void DSP_mmult(void)
{
	int count	= dsp_matrix_control&0x0f;
	uint32_t addr = dsp_pointer_to_matrix; // in the dsp ram
	int64_t accum = 0;
	uint32_t res;

	if (!(dsp_matrix_control & 0x10))
	{
		for (int i = 0; i < count; i++)
		{
			int16_t a;
			if (i&0x01)
				a=(int16_t)((dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]>>16)&0xffff);
			else
				a=(int16_t)(dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]&0xffff);
			int16_t b=((int16_t)DSPReadWord(addr + 2, DSP));
			accum += a*b;
			addr += 4;
		}
	}
	else
	{
		for (int i = 0; i < count; i++)
		{
			int16_t a;
			if (i&0x01)
				a=(int16_t)((dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]>>16)&0xffff);
			else
				a=(int16_t)(dsp_alternate_reg[dsp_opcode_first_parameter + (i>>1)]&0xffff);
			int16_t b=((int16_t)DSPReadWord(addr + 2, DSP));
			accum += a*b;
			addr += 4 * count;
		}
	}

	PRES = res = (int32_t)accum;
	// carry flag to do
//NOTE: The flags are set based upon the last add/multiply done...
	SET_ZN(PRES);
}

static void DSP_move(void)
{
#ifdef DSP_DIS_MOVE
	if (doDSPDis)
		WriteLog("%06X: MOVE   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	PRES = PRM;
#ifdef DSP_DIS_MOVE
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_movefa(void)
{
#ifdef DSP_DIS_MOVEFA
	if (doDSPDis)
//		WriteLog("%06X: MOVEFA R%02u, R%02u [NCZ:%u%u%u, R%02u(alt)=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, ALTERNATE_RM, PIMM2, PRN);
		WriteLog("%06X: MOVEFA R%02u, R%02u [NCZ:%u%u%u, R%02u(alt)=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, dsp_alternate_reg[PIMM1], PIMM2, PRN);
#endif
//	PRES = ALTERNATE_RM;
	PRES = dsp_alternate_reg[PIMM1];
#ifdef DSP_DIS_MOVEFA
	if (doDSPDis)
//		WriteLog("[NCZ:%u%u%u, R%02u(alt)=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, ALTERNATE_RM, PIMM2, PRN);
		WriteLog("[NCZ:%u%u%u, R%02u(alt)=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, dsp_alternate_reg[PIMM1], PIMM2, PRES);
#endif
}

static void DSP_movei(void)
{
#ifdef DSP_DIS_MOVEI
	if (doDSPDis)
		WriteLog("%06X: MOVEI  #$%08X, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PRES, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
//	// This instruction is followed by 32-bit value in LSW / MSW format...
//	PRES = (uint32_t)DSPReadWord(dsp_pc, DSP) | ((uint32_t)DSPReadWord(dsp_pc + 2, DSP) << 16);
//	dsp_pc += 4;
#ifdef DSP_DIS_MOVEI
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_movepc(void)
{
#ifdef DSP_DIS_MOVEPC
	if (doDSPDis)
		WriteLog("%06X: MOVE   PC, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
//Need to fix this to take into account pipelining effects... !!! FIX !!! [DONE]
//	PRES = dsp_pc - 2;
//Account for pipeline effects...
	PRES = dsp_pc - 2 - (pipeline[plPtrRead].opcode == 38 ? 6 : (pipeline[plPtrRead].opcode == PIPELINE_STALL ? 0 : 2));
#ifdef DSP_DIS_MOVEPC
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_moveq(void)
{
#ifdef DSP_DIS_MOVEQ
	if (doDSPDis)
		WriteLog("%06X: MOVEQ  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	PRES = PIMM1;
#ifdef DSP_DIS_MOVEQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_moveta(void)
{
#ifdef DSP_DIS_MOVETA
	if (doDSPDis)
//		WriteLog("%06X: MOVETA R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u(alt)=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, ALTERNATE_RN);
		WriteLog("%06X: MOVETA R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u(alt)=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, dsp_alternate_reg[PIMM2]);
#endif
//	ALTERNATE_RN = PRM;
	dsp_alternate_reg[PIMM2] = PRM;
	NO_WRITEBACK;
#ifdef DSP_DIS_MOVETA
	if (doDSPDis)
//		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u(alt)=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, ALTERNATE_RN);
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u(alt)=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, dsp_alternate_reg[PIMM2]);
#endif
}

static void DSP_mtoi(void)
{
	PRES = (((int32_t)PRM >> 8) & 0xFF800000) | (PRM & 0x007FFFFF);
	SET_ZN(PRES);
}

static void DSP_mult(void)
{
#ifdef DSP_DIS_MULT
	if (doDSPDis)
		WriteLog("%06X: MULT   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	PRES = (uint16_t)PRM * (uint16_t)PRN;
	SET_ZN(PRES);
#ifdef DSP_DIS_MULT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_neg(void)
{
#ifdef DSP_DIS_NEG
	if (doDSPDis)
		WriteLog("%06X: NEG    R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	uint32_t res = -PRN;
	SET_ZNC_SUB(0, PRN, res);
	PRES = res;
#ifdef DSP_DIS_NEG
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_nop(void)
{
#ifdef DSP_DIS_NOP
	if (doDSPDis)
		WriteLog("%06X: NOP    [NCZ:%u%u%u]\n", DSP_PPC, dsp_flag_n, dsp_flag_c, dsp_flag_z);
#endif
	NO_WRITEBACK;
}

static void DSP_normi(void)
{
	uint32_t _Rm = PRM;
	uint32_t res = 0;

	if (_Rm)
	{
		while ((_Rm & 0xffc00000) == 0)
		{
			_Rm <<= 1;
			res--;
		}
		while ((_Rm & 0xff800000) != 0)
		{
			_Rm >>= 1;
			res++;
		}
	}
	PRES = res;
	SET_ZN(PRES);
}

static void DSP_not(void)
{
#ifdef DSP_DIS_NOT
	if (doDSPDis)
		WriteLog("%06X: NOT    R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	PRES = ~PRN;
	SET_ZN(PRES);
#ifdef DSP_DIS_NOT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_or(void)
{
#ifdef DSP_DIS_OR
	if (doDSPDis)
		WriteLog("%06X: OR     R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	PRES = PRN | PRM;
	SET_ZN(PRES);
#ifdef DSP_DIS_OR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_resmac(void)
{
#ifdef DSP_DIS_RESMAC
	if (doDSPDis)
		WriteLog("%06X: RESMAC R%02u [NCZ:%u%u%u, R%02u=%08X, DSP_ACC=%02X%08X] -> ", DSP_PPC, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN, (uint8_t)(dsp_acc >> 32), (uint32_t)(dsp_acc & 0xFFFFFFFF));
#endif
	PRES = (uint32_t)dsp_acc;
#ifdef DSP_DIS_RESMAC
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
}

static void DSP_ror(void)
{
#ifdef DSP_DIS_ROR
	if (doDSPDis)
		WriteLog("%06X: ROR    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	uint32_t r1 = PRM & 0x1F;
	uint32_t res = (PRN >> r1) | (PRN << (32 - r1));
	SET_ZN(res); dsp_flag_c = (PRN >> 31) & 1;
	PRES = res;
#ifdef DSP_DIS_ROR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_rorq(void)
{
#ifdef DSP_DIS_RORQ
	if (doDSPDis)
		WriteLog("%06X: RORQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	uint32_t r1 = dsp_convert_zero[PIMM1 & 0x1F];
	uint32_t r2 = PRN;
	uint32_t res = (r2 >> r1) | (r2 << (32 - r1));
	PRES = res;
	SET_ZN(res); dsp_flag_c = (r2 >> 31) & 0x01;
#ifdef DSP_DIS_RORQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_sat16s(void)
{
	int32_t r2 = PRN;
	uint32_t res = (r2 < -32768) ? -32768 : (r2 > 32767) ? 32767 : r2;
	PRES = res;
	SET_ZN(res);
}

static void DSP_sat32s(void)
{
	int32_t r2 = (uint32_t)PRN;
	int32_t temp = dsp_acc >> 32;
	uint32_t res = (temp < -1) ? (int32_t)0x80000000 : (temp > 0) ? (int32_t)0x7FFFFFFF : r2;
	PRES = res;
	SET_ZN(res);
}

static void DSP_sh(void)
{
	int32_t sRm = (int32_t)PRM;
	uint32_t _Rn = PRN;

	if (sRm < 0)
	{
		uint32_t shift = -sRm;

		if (shift >= 32)
			shift = 32;

		dsp_flag_c = (_Rn & 0x80000000) >> 31;

		while (shift)
		{
			_Rn <<= 1;
			shift--;
		}
	}
	else
	{
		uint32_t shift = sRm;

		if (shift >= 32)
			shift = 32;

		dsp_flag_c = _Rn & 0x1;

		while (shift)
		{
			_Rn >>= 1;
			shift--;
		}
	}

	PRES = _Rn;
	SET_ZN(PRES);
}

static void DSP_sha(void)
{
	int32_t sRm = (int32_t)PRM;
	uint32_t _Rn = PRN;

	if (sRm < 0)
	{
		uint32_t shift = -sRm;

		if (shift >= 32)
			shift = 32;

		dsp_flag_c = (_Rn & 0x80000000) >> 31;

		while (shift)
		{
			_Rn <<= 1;
			shift--;
		}
	}
	else
	{
		uint32_t shift = sRm;

		if (shift >= 32)
			shift = 32;

		dsp_flag_c = _Rn & 0x1;

		while (shift)
		{
			_Rn = ((int32_t)_Rn) >> 1;
			shift--;
		}
	}

	PRES = _Rn;
	SET_ZN(PRES);
}

static void DSP_sharq(void)
{
#ifdef DSP_DIS_SHARQ
	if (doDSPDis)
		WriteLog("%06X: SHARQ  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	uint32_t res = (int32_t)PRN >> dsp_convert_zero[PIMM1];
	SET_ZN(res); dsp_flag_c = PRN & 0x01;
	PRES = res;
#ifdef DSP_DIS_SHARQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_shlq(void)
{
#ifdef DSP_DIS_SHLQ
	if (doDSPDis)
		WriteLog("%06X: SHLQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, 32 - PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	int32_t r1 = 32 - PIMM1;
	uint32_t res = PRN << r1;
	SET_ZN(res); dsp_flag_c = (PRN >> 31) & 1;
	PRES = res;
#ifdef DSP_DIS_SHLQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_shrq(void)
{
#ifdef DSP_DIS_SHRQ
	if (doDSPDis)
		WriteLog("%06X: SHRQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	int32_t r1 = dsp_convert_zero[PIMM1];
	uint32_t res = PRN >> r1;
	SET_ZN(res); dsp_flag_c = PRN & 1;
	PRES = res;
#ifdef DSP_DIS_SHRQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_store(void)
{
#ifdef DSP_DIS_STORE
	if (doDSPDis)
		WriteLog("%06X: STORE  R%02u, (R%02u) [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", DSP_PPC, PIMM2, PIMM1, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN, PIMM1, PRM);
#endif
//	DSPWriteLong(PRM, PRN, DSP);
//	NO_WRITEBACK;
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	pipeline[plPtrExec].address = PRM & 0xFFFFFFFC;
#else
	pipeline[plPtrExec].address = PRM;
#endif
	pipeline[plPtrExec].value = PRN;
	pipeline[plPtrExec].type = TYPE_DWORD;
	WRITEBACK_ADDR;
}

static void DSP_storeb(void)
{
#ifdef DSP_DIS_STOREB
	if (doDSPDis)
		WriteLog("%06X: STOREB R%02u, (R%02u) [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", DSP_PPC, PIMM2, PIMM1, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN, PIMM1, PRM);
#endif
//	if (PRM >= DSP_WORK_RAM_BASE && PRM <= (DSP_WORK_RAM_BASE + 0x1FFF))
//		DSPWriteLong(PRM, PRN & 0xFF, DSP);
//	else
//		JaguarWriteByte(PRM, PRN, DSP);
//
//	NO_WRITEBACK;
	pipeline[plPtrExec].address = PRM;

	if (PRM >= DSP_WORK_RAM_BASE && PRM <= (DSP_WORK_RAM_BASE + 0x1FFF))
	{
		pipeline[plPtrExec].value = PRN & 0xFF;
		pipeline[plPtrExec].type = TYPE_DWORD;
	}
	else
	{
		pipeline[plPtrExec].value = PRN;
		pipeline[plPtrExec].type = TYPE_BYTE;
	}

	WRITEBACK_ADDR;
}

static void DSP_storew(void)
{
#ifdef DSP_DIS_STOREW
	if (doDSPDis)
		WriteLog("%06X: STOREW R%02u, (R%02u) [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", DSP_PPC, PIMM2, PIMM1, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN, PIMM1, PRM);
#endif
//	if (PRM >= DSP_WORK_RAM_BASE && PRM <= (DSP_WORK_RAM_BASE + 0x1FFF))
//		DSPWriteLong(PRM, PRN & 0xFFFF, DSP);
//	else
//		JaguarWriteWord(PRM, PRN, DSP);
//
//	NO_WRITEBACK;
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	pipeline[plPtrExec].address = PRM & 0xFFFFFFFE;
#else
	pipeline[plPtrExec].address = PRM;
#endif

	if (PRM >= DSP_WORK_RAM_BASE && PRM <= (DSP_WORK_RAM_BASE + 0x1FFF))
	{
		pipeline[plPtrExec].value = PRN & 0xFFFF;
		pipeline[plPtrExec].type = TYPE_DWORD;
	}
	else
	{
		pipeline[plPtrExec].value = PRN;
		pipeline[plPtrExec].type = TYPE_WORD;
	}
	WRITEBACK_ADDR;
}

static void DSP_store_r14_i(void)
{
#ifdef DSP_DIS_STORE14I
	if (doDSPDis)
		WriteLog("%06X: STORE  R%02u, (R14+$%02X) [NCZ:%u%u%u, R%02u=%08X, R14+$%02X=%08X]\n", DSP_PPC, PIMM2, dsp_convert_zero[PIMM1] << 2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN, dsp_convert_zero[PIMM1] << 2, dsp_reg[14]+(dsp_convert_zero[PIMM1] << 2));
#endif
//	DSPWriteLong(dsp_reg[14] + (dsp_convert_zero[PIMM1] << 2), PRN, DSP);
//	NO_WRITEBACK;
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	pipeline[plPtrExec].address = (dsp_reg[14] & 0xFFFFFFFC) + (dsp_convert_zero[PIMM1] << 2);
#else
	pipeline[plPtrExec].address = dsp_reg[14] + (dsp_convert_zero[PIMM1] << 2);
#endif
	pipeline[plPtrExec].value = PRN;
	pipeline[plPtrExec].type = TYPE_DWORD;
	WRITEBACK_ADDR;
}

static void DSP_store_r14_r(void)
{
//	DSPWriteLong(dsp_reg[14] + PRM, PRN, DSP);
//	NO_WRITEBACK;
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	pipeline[plPtrExec].address = (dsp_reg[14] + PRM) & 0xFFFFFFFC;
#else
	pipeline[plPtrExec].address = dsp_reg[14] + PRM;
#endif
	pipeline[plPtrExec].value = PRN;
	pipeline[plPtrExec].type = TYPE_DWORD;
	WRITEBACK_ADDR;
}

static void DSP_store_r15_i(void)
{
#ifdef DSP_DIS_STORE15I
	if (doDSPDis)
		WriteLog("%06X: STORE  R%02u, (R15+$%02X) [NCZ:%u%u%u, R%02u=%08X, R15+$%02X=%08X]\n", DSP_PPC, PIMM2, dsp_convert_zero[PIMM1] << 2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN, dsp_convert_zero[PIMM1] << 2, dsp_reg[15]+(dsp_convert_zero[PIMM1] << 2));
#endif
//	DSPWriteLong(dsp_reg[15] + (dsp_convert_zero[PIMM1] << 2), PRN, DSP);
//	NO_WRITEBACK;
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	pipeline[plPtrExec].address = (dsp_reg[15] & 0xFFFFFFFC) + (dsp_convert_zero[PIMM1] << 2);
#else
	pipeline[plPtrExec].address = dsp_reg[15] + (dsp_convert_zero[PIMM1] << 2);
#endif
	pipeline[plPtrExec].value = PRN;
	pipeline[plPtrExec].type = TYPE_DWORD;
	WRITEBACK_ADDR;
}

static void DSP_store_r15_r(void)
{
//	DSPWriteLong(dsp_reg[15] + PRM, PRN, DSP);
//	NO_WRITEBACK;
#ifdef DSP_CORRECT_ALIGNMENT_STORE
	pipeline[plPtrExec].address = (dsp_reg[15] + PRM) & 0xFFFFFFFC;
#else
	pipeline[plPtrExec].address = dsp_reg[15] + PRM;
#endif
	pipeline[plPtrExec].value = PRN;
	pipeline[plPtrExec].type = TYPE_DWORD;
	WRITEBACK_ADDR;
}

static void DSP_sub(void)
{
#ifdef DSP_DIS_SUB
	if (doDSPDis)
		WriteLog("%06X: SUB    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	uint32_t res = PRN - PRM;
	SET_ZNC_SUB(PRN, PRM, res);
	PRES = res;
#ifdef DSP_DIS_SUB
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_subc(void)
{
#ifdef DSP_DIS_SUBC
	if (doDSPDis)
		WriteLog("%06X: SUBC   R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	uint32_t res = PRN - PRM - dsp_flag_c;
	uint32_t borrow = dsp_flag_c;
	SET_ZNC_SUB(PRN - borrow, PRM, res);
	PRES = res;
#ifdef DSP_DIS_SUBC
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}

static void DSP_subq(void)
{
#ifdef DSP_DIS_SUBQ
	if (doDSPDis)
		WriteLog("%06X: SUBQ   #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	uint32_t r1 = dsp_convert_zero[PIMM1];
	uint32_t res = PRN - r1;
	SET_ZNC_SUB(PRN, r1, res);
	PRES = res;
#ifdef DSP_DIS_SUBQ
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_subqmod(void)
{
	uint32_t r1 = dsp_convert_zero[PIMM1];
	uint32_t r2 = PRN;
	uint32_t res = r2 - r1;
	res = (res & (~dsp_modulo)) | (r2 & dsp_modulo);
	PRES = res;
	SET_ZNC_SUB(r2, r1, res);
}

static void DSP_subqt(void)
{
#ifdef DSP_DIS_SUBQT
	if (doDSPDis)
		WriteLog("%06X: SUBQT  #%u, R%02u [NCZ:%u%u%u, R%02u=%08X] -> ", DSP_PPC, dsp_convert_zero[PIMM1], PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRN);
#endif
	PRES = PRN - dsp_convert_zero[PIMM1];
#ifdef DSP_DIS_SUBQT
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM2, PRES);
#endif
}

static void DSP_xor(void)
{
#ifdef DSP_DIS_XOR
	if (doDSPDis)
		WriteLog("%06X: XOR    R%02u, R%02u [NCZ:%u%u%u, R%02u=%08X, R%02u=%08X] -> ", DSP_PPC, PIMM1, PIMM2, dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRN);
#endif
	PRES = PRN ^ PRM;
	SET_ZN(PRES);
#ifdef DSP_DIS_XOR
	if (doDSPDis)
		WriteLog("[NCZ:%u%u%u, R%02u=%08X, R%02u=%08X]\n", dsp_flag_n, dsp_flag_c, dsp_flag_z, PIMM1, PRM, PIMM2, PRES);
#endif
}
