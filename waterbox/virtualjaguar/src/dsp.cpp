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
#include "jaguar.h"
#include "jerry.h"
#include "m68000/m68kinterface.h"

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

static const uint8_t dsp_opcode_cycles[64] =
{
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  9,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  2,
	2,  2,  2,  3,  3,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  4,  1,
	1,  1,  3,  3,  1,  1,  1,  1
};

static void (* dsp_opcode[64])() =
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

uint32_t dsp_pc;
static uint64_t dsp_acc;
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

static bool IMASKCleared;
static uint32_t dsp_inhibit_interrupt;

#define DSP_RUNNING	(dsp_control & 0x01)

static uint8_t branch_condition_table[32 * 8];
static uint16_t mirror_table[65536];
uint8_t dsp_ram_8[0x2000];

void dsp_build_branch_condition_table(void)
{
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

			branch_condition_table[i * 32 + j] = result;
		}
	}
}

uint8_t DSPReadByte(uint32_t offset, uint32_t who)
{
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

uint16_t DSPReadWord(uint32_t offset, uint32_t who)
{
	offset &= 0xFFFFFFFE;

	if (offset >= DSP_WORK_RAM_BASE && offset <= DSP_WORK_RAM_BASE+0x1FFF)
	{
		offset -= DSP_WORK_RAM_BASE;
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

uint32_t DSPReadLong(uint32_t offset, uint32_t who)
{
	offset &= 0xFFFFFFFC;

	if (offset >= DSP_WORK_RAM_BASE && offset <= DSP_WORK_RAM_BASE + 0x1FFF)
	{
		offset -= DSP_WORK_RAM_BASE;
		return GET32(dsp_ram_8, offset);
	}

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
				return (int32_t)((int8_t)(dsp_acc >> 32));
		}

		return 0xFFFFFFFF;
	}

	return JaguarReadLong(offset, who);
}

void DSPWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	if ((offset >= DSP_WORK_RAM_BASE) && (offset < DSP_WORK_RAM_BASE + 0x2000))
	{
		offset -= DSP_WORK_RAM_BASE;
		dsp_ram_8[offset] = data;
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
			uint32_t old_data = DSPReadLong(offset&0xFFFFFFC, who);
			bytenum = 3 - bytenum;
			old_data = (old_data & (~(0xFF << (bytenum << 3)))) | (data << (bytenum << 3));
			DSPWriteLong(offset & 0xFFFFFFC, old_data, who);
		}
		return;
	}

	JaguarWriteByte(offset, data, who);
}

void DSPWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	offset &= 0xFFFFFFFE;

	if ((offset >= DSP_WORK_RAM_BASE) && (offset < DSP_WORK_RAM_BASE+0x2000))
	{
		offset -= DSP_WORK_RAM_BASE;
		dsp_ram_8[offset] = data >> 8;
		dsp_ram_8[offset+1] = data & 0xFF;
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

void DSPWriteLong(uint32_t offset, uint32_t data, uint32_t who)
{
	offset &= 0xFFFFFFFC;

	if (offset >= DSP_WORK_RAM_BASE && offset <= DSP_WORK_RAM_BASE + 0x1FFF)
	{
		offset -= DSP_WORK_RAM_BASE;
		SET32(dsp_ram_8, offset, data);
		return;
	}
	else if (offset >= DSP_CONTROL_RAM_BASE && offset <= (DSP_CONTROL_RAM_BASE + 0x1F))
	{
		offset &= 0x1F;
		switch (offset)
		{
			case 0x00:
			{
				IMASKCleared |= (dsp_flags & IMASK) && !(data & IMASK);

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
				dsp_pointer_to_matrix = 0xF1B000 | (data & 0x000FFC);
				break;
			case 0x0C:
				dsp_data_organization = data;
				break;
			case 0x10:
				dsp_pc = data;
				break;
			case 0x14:
			{
				bool wasRunning = DSP_RUNNING;
				if (data & CPUINT)
				{
					if (JERRYIRQEnabled(IRQ2_DSP))
					{
						JERRYSetPendingIRQ(IRQ2_DSP);
						m68k_set_irq(2);
					}
					data &= ~CPUINT;
				}

				if (data & DSPINT0)
				{
					m68k_end_timeslice();
					DSPSetIRQLine(DSPIRQ_CPU, ASSERT_LINE);
					data &= ~DSPINT0;
				}

				uint32_t mask = VERSION | INT_LAT0 | INT_LAT1 | INT_LAT2 | INT_LAT3 | INT_LAT4 | INT_LAT5;
				dsp_control = (dsp_control & mask) | (data & ~mask);

				if (DSP_RUNNING)
				{
					if (who == M68K)
						m68k_end_timeslice();
				}
				break;
			}
			case 0x18:
				dsp_modulo = data;
				break;
			case 0x1C:
				dsp_div_control = data;
				break;
		}

		return;
	}

	JaguarWriteLong(offset, data, who);
}

//
// Update the DSP register file pointers depending on REGPAGE bit
//
void DSPUpdateRegisterBanks(void)
{
	int bank = (dsp_flags & REGPAGE);

	if (dsp_flags & IMASK)
		bank = 0;

	if (bank)
		dsp_reg = dsp_reg_bank_1, dsp_alternate_reg = dsp_reg_bank_0;
	else
		dsp_reg = dsp_reg_bank_0, dsp_alternate_reg = dsp_reg_bank_1;
}

//
// Check for and handle any asserted DSP IRQs
//
void DSPHandleIRQsNP(void)
{
	if (dsp_flags & IMASK)
		return;

	uint32_t bits = ((dsp_control >> 10) & 0x20) | ((dsp_control >> 6) & 0x1F),
		mask = ((dsp_flags >> 11) & 0x20) | ((dsp_flags >> 4) & 0x1F);

	bits &= mask;

	if (!bits)
		return;

	int which = 0;
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

	dsp_flags |= IMASK;

	DSPUpdateRegisterBanks();

	dsp_reg[31] -= 4;
	dsp_reg[30] = dsp_pc - 2;

	DSPWriteLong(dsp_reg[31], dsp_reg[30], DSP);

	dsp_pc = dsp_reg[30] = DSP_WORK_RAM_BASE + (which * 0x10);
}

//
// Set the specified DSP IRQ line to a given state
//
void DSPSetIRQLine(int irqline, int state)
{
	uint32_t mask = INT_LAT0 << irqline;
	dsp_control &= ~mask;

	if (state)
	{
		dsp_control |= mask;
		DSPHandleIRQsNP();
	}
}

bool DSPIsRunning(void)
{
	return (DSP_RUNNING ? true : false);
}

void DSPInit(void)
{
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
	dsp_control			  = 0x00002000;
	dsp_div_control		  = 0x00000000;

	dsp_reg = dsp_reg_bank_0;
	dsp_alternate_reg = dsp_reg_bank_1;

	for(int i=0; i<32; i++)
		dsp_reg[i] = dsp_alternate_reg[i] = 0x00000000;

	dsp_flag_z = dsp_flag_n = dsp_flag_c = 0;
	IMASKCleared = false;

	for(uint32_t i=0; i<8192; i+=4)
		*((uint32_t *)(&dsp_ram_8[i])) = rand();
}

void DSPDone(void)
{
}

//
// DSP execution core
//
void DSPExec(int32_t cycles)
{
	while (cycles > 0 && DSP_RUNNING)
	{
		MAYBE_CALLBACK(DSPTraceCallback, dsp_pc, dsp_reg);

		if (IMASKCleared && !dsp_inhibit_interrupt)
		{
			DSPHandleIRQsNP();
			IMASKCleared = false;
		}

		dsp_inhibit_interrupt = 0;
		uint16_t opcode = DSPReadWord(dsp_pc, DSP);
		uint32_t index = opcode >> 10;
		dsp_opcode_first_parameter = (opcode >> 5) & 0x1F;
		dsp_opcode_second_parameter = opcode & 0x1F;

		dsp_pc += 2;
		dsp_opcode[index]();

		cycles -= dsp_opcode_cycles[index];
	}
}

//
// DSP opcodes
//

#define RISC 2

#include "risc_opcodes.h"
