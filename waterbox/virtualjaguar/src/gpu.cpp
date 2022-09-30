//
// GPU Core
//
// Originally by David Raingeard (Cal2)
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Cleanups, endian wrongness, and bad ASM amelioration by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
// JLH  11/26/2011  Added fixes for LOAD/STORE alignment issues

//
// Note: Endian wrongness probably stems from the MAME origins of this emu and
//       the braindead way in which MAME handles memory. :-)
//
// Problem with not booting the BIOS was the incorrect way that the
// SUBC instruction set the carry when the carry was set going in...
// Same problem with ADDC...
//

#include "gpu.h"

#include <stdlib.h>
#include <string.h>
#include "dsp.h"
#include "jaguar.h"
#include "m68000/m68kinterface.h"
#include "tom.h"

// Various bits

#define CINT0FLAG			0x0200
#define CINT1FLAG			0x0400
#define CINT2FLAG			0x0800
#define CINT3FLAG			0x1000
#define CINT4FLAG			0x2000
#define CINT04FLAGS			(CINT0FLAG | CINT1FLAG | CINT2FLAG | CINT3FLAG | CINT4FLAG)

// GPU_FLAGS bits

#define ZERO_FLAG		0x0001
#define CARRY_FLAG		0x0002
#define NEGA_FLAG		0x0004
#define IMASK			0x0008
#define INT_ENA0		0x0010
#define INT_ENA1		0x0020
#define INT_ENA2		0x0040
#define INT_ENA3		0x0080
#define INT_ENA4		0x0100
#define INT_CLR0		0x0200
#define INT_CLR1		0x0400
#define INT_CLR2		0x0800
#define INT_CLR3		0x1000
#define INT_CLR4		0x2000
#define REGPAGE			0x4000
#define DMAEN			0x8000

// External global variables

// Private function prototypes

void GPUUpdateRegisterBanks(void);

static void gpu_opcode_add(void);
static void gpu_opcode_addc(void);
static void gpu_opcode_addq(void);
static void gpu_opcode_addqt(void);
static void gpu_opcode_sub(void);
static void gpu_opcode_subc(void);
static void gpu_opcode_subq(void);
static void gpu_opcode_subqt(void);
static void gpu_opcode_neg(void);
static void gpu_opcode_and(void);
static void gpu_opcode_or(void);
static void gpu_opcode_xor(void);
static void gpu_opcode_not(void);
static void gpu_opcode_btst(void);
static void gpu_opcode_bset(void);
static void gpu_opcode_bclr(void);
static void gpu_opcode_mult(void);
static void gpu_opcode_imult(void);
static void gpu_opcode_imultn(void);
static void gpu_opcode_resmac(void);
static void gpu_opcode_imacn(void);
static void gpu_opcode_div(void);
static void gpu_opcode_abs(void);
static void gpu_opcode_sh(void);
static void gpu_opcode_shlq(void);
static void gpu_opcode_shrq(void);
static void gpu_opcode_sha(void);
static void gpu_opcode_sharq(void);
static void gpu_opcode_ror(void);
static void gpu_opcode_rorq(void);
static void gpu_opcode_cmp(void);
static void gpu_opcode_cmpq(void);
static void gpu_opcode_sat8(void);
static void gpu_opcode_sat16(void);
static void gpu_opcode_move(void);
static void gpu_opcode_moveq(void);
static void gpu_opcode_moveta(void);
static void gpu_opcode_movefa(void);
static void gpu_opcode_movei(void);
static void gpu_opcode_loadb(void);
static void gpu_opcode_loadw(void);
static void gpu_opcode_load(void);
static void gpu_opcode_loadp(void);
static void gpu_opcode_load_r14_indexed(void);
static void gpu_opcode_load_r15_indexed(void);
static void gpu_opcode_storeb(void);
static void gpu_opcode_storew(void);
static void gpu_opcode_store(void);
static void gpu_opcode_storep(void);
static void gpu_opcode_store_r14_indexed(void);
static void gpu_opcode_store_r15_indexed(void);
static void gpu_opcode_move_pc(void);
static void gpu_opcode_jump(void);
static void gpu_opcode_jr(void);
static void gpu_opcode_mmult(void);
static void gpu_opcode_mtoi(void);
static void gpu_opcode_normi(void);
static void gpu_opcode_nop(void);
static void gpu_opcode_load_r14_ri(void);
static void gpu_opcode_load_r15_ri(void);
static void gpu_opcode_store_r14_ri(void);
static void gpu_opcode_store_r15_ri(void);
static void gpu_opcode_sat24(void);
static void gpu_opcode_pack(void);

static const uint8_t gpu_opcode_cycles[64] =
{
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1,
	1,  1,  1,  1,  1,  1,  1,  1
};

static void (*gpu_opcode[64])()=
{
	gpu_opcode_add,					gpu_opcode_addc,				gpu_opcode_addq,				gpu_opcode_addqt,
	gpu_opcode_sub,					gpu_opcode_subc,				gpu_opcode_subq,				gpu_opcode_subqt,
	gpu_opcode_neg,					gpu_opcode_and,					gpu_opcode_or,					gpu_opcode_xor,
	gpu_opcode_not,					gpu_opcode_btst,				gpu_opcode_bset,				gpu_opcode_bclr,
	gpu_opcode_mult,				gpu_opcode_imult,				gpu_opcode_imultn,				gpu_opcode_resmac,
	gpu_opcode_imacn,				gpu_opcode_div,					gpu_opcode_abs,					gpu_opcode_sh,
	gpu_opcode_shlq,				gpu_opcode_shrq,				gpu_opcode_sha,					gpu_opcode_sharq,
	gpu_opcode_ror,					gpu_opcode_rorq,				gpu_opcode_cmp,					gpu_opcode_cmpq,
	gpu_opcode_sat8,				gpu_opcode_sat16,				gpu_opcode_move,				gpu_opcode_moveq,
	gpu_opcode_moveta,				gpu_opcode_movefa,				gpu_opcode_movei,				gpu_opcode_loadb,
	gpu_opcode_loadw,				gpu_opcode_load,				gpu_opcode_loadp,				gpu_opcode_load_r14_indexed,
	gpu_opcode_load_r15_indexed,	gpu_opcode_storeb,				gpu_opcode_storew,				gpu_opcode_store,
	gpu_opcode_storep,				gpu_opcode_store_r14_indexed,	gpu_opcode_store_r15_indexed,	gpu_opcode_move_pc,
	gpu_opcode_jump,				gpu_opcode_jr,					gpu_opcode_mmult,				gpu_opcode_mtoi,
	gpu_opcode_normi,				gpu_opcode_nop,					gpu_opcode_load_r14_ri,			gpu_opcode_load_r15_ri,
	gpu_opcode_store_r14_ri,		gpu_opcode_store_r15_ri,		gpu_opcode_sat24,				gpu_opcode_pack,
};

uint8_t gpu_ram_8[0x1000];
uint32_t gpu_pc;
static uint32_t gpu_acc;
static uint32_t gpu_remain;
static uint32_t gpu_hidata;
static uint32_t gpu_flags;
static uint32_t gpu_matrix_control;
static uint32_t gpu_pointer_to_matrix;
static uint32_t gpu_data_organization;
static uint32_t gpu_control;
static uint32_t gpu_div_control;
static uint8_t gpu_flag_z, gpu_flag_n, gpu_flag_c;
uint32_t gpu_reg_bank_0[32];
uint32_t gpu_reg_bank_1[32];
static uint32_t * gpu_reg;
static uint32_t * gpu_alternate_reg;

static uint32_t gpu_opcode_first_parameter;
static uint32_t gpu_opcode_second_parameter;

#define GPU_RUNNING		(gpu_control & 0x01)

static uint8_t branch_condition_table[32 * 8];

bool GPURunning(void)
{
	return GPU_RUNNING;
}

void build_branch_condition_table(void)
{
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

//
// GPU byte access (read)
//
uint8_t GPUReadByte(uint32_t offset, uint32_t who)
{
	if ((offset >= GPU_WORK_RAM_BASE) && (offset < GPU_WORK_RAM_BASE+0x1000))
		return gpu_ram_8[offset & 0xFFF];
	else if ((offset >= GPU_CONTROL_RAM_BASE) && (offset < GPU_CONTROL_RAM_BASE+0x20))
	{
		uint32_t data = GPUReadLong(offset & 0xFFFFFFFC, who);

		if ((offset & 0x03) == 0)
			return data >> 24;
		else if ((offset & 0x03) == 1)
			return (data >> 16) & 0xFF;
		else if ((offset & 0x03) == 2)
			return (data >> 8) & 0xFF;
		else if ((offset & 0x03) == 3)
			return data & 0xFF;
	}

	return JaguarReadByte(offset, who);
}

//
// GPU word access (read)
//
uint16_t GPUReadWord(uint32_t offset, uint32_t who)
{
	if ((offset >= GPU_WORK_RAM_BASE) && (offset < GPU_WORK_RAM_BASE+0x1000))
	{
		offset &= 0xFFF;
		uint16_t data = ((uint16_t)gpu_ram_8[offset] << 8) | (uint16_t)gpu_ram_8[offset+1];
		return data;
	}
	else if ((offset >= GPU_CONTROL_RAM_BASE) && (offset < GPU_CONTROL_RAM_BASE+0x20))
	{
		if (offset & 0x01)
			return (GPUReadByte(offset, who) << 8) | GPUReadByte(offset+1, who);

		uint32_t data = GPUReadLong(offset & 0xFFFFFFFC, who);

		if (offset & 0x02)
			return data & 0xFFFF;
		else
			return data >> 16;
	}

	return JaguarReadWord(offset, who);
}

//
// GPU dword access (read)
//
uint32_t GPUReadLong(uint32_t offset, uint32_t who)
{
	if (offset >= 0xF02000 && offset <= 0xF020FF)
	{
		uint32_t reg = (offset & 0xFC) >> 2;
		return (reg < 32 ? gpu_reg_bank_0[reg] : gpu_reg_bank_1[reg - 32]); 
	}

	if ((offset >= GPU_WORK_RAM_BASE) && (offset <= GPU_WORK_RAM_BASE + 0x0FFC))
	{
		offset &= 0xFFF;
		return ((uint32_t)gpu_ram_8[offset] << 24) | ((uint32_t)gpu_ram_8[offset+1] << 16)
			| ((uint32_t)gpu_ram_8[offset+2] << 8) | (uint32_t)gpu_ram_8[offset+3];
	}
	else if ((offset >= GPU_CONTROL_RAM_BASE) && (offset <= GPU_CONTROL_RAM_BASE + 0x1C))
	{
		offset &= 0x1F;
		switch (offset)
		{
			case 0x00:
				gpu_flag_c = (gpu_flag_c ? 1 : 0);
				gpu_flag_z = (gpu_flag_z ? 1 : 0);
				gpu_flag_n = (gpu_flag_n ? 1 : 0);

				gpu_flags = (gpu_flags & 0xFFFFFFF8) | (gpu_flag_n << 2) | (gpu_flag_c << 1) | gpu_flag_z;

				return gpu_flags & 0xFFFFC1FF;
			case 0x04:
				return gpu_matrix_control;
			case 0x08:
				return gpu_pointer_to_matrix;
			case 0x0C:
				return gpu_data_organization;
			case 0x10:
				return gpu_pc;
			case 0x14:
				return gpu_control;
			case 0x18:
				return gpu_hidata;
			case 0x1C:
				return gpu_remain;
			default:
				return 0;
		}
	}

	return (JaguarReadWord(offset, who) << 16) | JaguarReadWord(offset + 2, who);
}

//
// GPU byte access (write)
//
void GPUWriteByte(uint32_t offset, uint8_t data, uint32_t who)
{
	if ((offset >= GPU_WORK_RAM_BASE) && (offset <= GPU_WORK_RAM_BASE + 0x0FFF))
	{
		gpu_ram_8[offset & 0xFFF] = data;
		return;
	}
	else if ((offset >= GPU_CONTROL_RAM_BASE) && (offset <= GPU_CONTROL_RAM_BASE + 0x1F))
	{
		uint32_t reg = offset & 0x1C;
		int bytenum = offset & 0x03;

		if ((reg >= 0x1C) && (reg <= 0x1F))
			gpu_div_control = (gpu_div_control & (~(0xFF << (bytenum << 3)))) | (data << (bytenum << 3));
		else
		{
			uint32_t old_data = GPUReadLong(offset & 0xFFFFFFC, who);
			bytenum = 3 - bytenum;
			old_data = (old_data & (~(0xFF << (bytenum << 3)))) | (data << (bytenum << 3));
			GPUWriteLong(offset & 0xFFFFFFC, old_data, who);
		}
		return;
	}

	JaguarWriteByte(offset, data, who);
}

//
// GPU word access (write)
//
void GPUWriteWord(uint32_t offset, uint16_t data, uint32_t who)
{
	if ((offset >= GPU_WORK_RAM_BASE) && (offset <= GPU_WORK_RAM_BASE + 0x0FFE))
	{
		gpu_ram_8[offset & 0xFFF] = (data>>8) & 0xFF;
		gpu_ram_8[(offset+1) & 0xFFF] = data & 0xFF;
		return;
	}
	else if ((offset >= GPU_CONTROL_RAM_BASE) && (offset <= GPU_CONTROL_RAM_BASE + 0x1E))
	{
		if (offset & 0x01)
		{
			return;
		}

		if ((offset & 0x1C) == 0x1C)
		{
			if (offset & 0x02)
				gpu_div_control = (gpu_div_control & 0xFFFF0000) | (data & 0xFFFF);
			else
				gpu_div_control = (gpu_div_control & 0x0000FFFF) | ((data & 0xFFFF) << 16);
		}
		else
		{
			uint32_t old_data = GPUReadLong(offset & 0xFFFFFFC, who);

			if (offset & 0x02)
				old_data = (old_data & 0xFFFF0000) | (data & 0xFFFF);
			else
				old_data = (old_data & 0x0000FFFF) | ((data & 0xFFFF) << 16);

			GPUWriteLong(offset & 0xFFFFFFC, old_data, who);
		}

		return;
	}
	else if ((offset == GPU_WORK_RAM_BASE + 0x0FFF) || (GPU_CONTROL_RAM_BASE + 0x1F))
	{
		return;
	}

	JaguarWriteWord(offset, data, who);
}

//
// GPU dword access (write)
//
void GPUWriteLong(uint32_t offset, uint32_t data, uint32_t who)
{
	if ((offset >= GPU_WORK_RAM_BASE) && (offset <= GPU_WORK_RAM_BASE + 0x0FFC))
	{
		offset &= 0xFFF;
		SET32(gpu_ram_8, offset, data);
		return;
	}
	else if ((offset >= GPU_CONTROL_RAM_BASE) && (offset <= GPU_CONTROL_RAM_BASE + 0x1C))
	{
		offset &= 0x1F;
		switch (offset)
		{
			case 0x00:
			{
				bool IMASKCleared = (gpu_flags & IMASK) && !(data & IMASK);
				gpu_flags = data & (~IMASK);
				gpu_flag_z = gpu_flags & ZERO_FLAG;
				gpu_flag_c = (gpu_flags & CARRY_FLAG) >> 1;
				gpu_flag_n = (gpu_flags & NEGA_FLAG) >> 2;
				GPUUpdateRegisterBanks();
				gpu_control &= ~((gpu_flags & CINT04FLAGS) >> 3);
				if (IMASKCleared)
					GPUHandleIRQs();
				break;
			}
			case 0x04:
				gpu_matrix_control = data;
				break;
			case 0x08:
				gpu_pointer_to_matrix = data & 0xFFFFFFFC;
				break;
			case 0x0C:
				gpu_data_organization = data;
				break;
			case 0x10:
				gpu_pc = data;
				break;
			case 0x14:
			{
				data &= ~0xF7C0;

				if (data & 0x02)
				{
					if (TOMIRQEnabled(IRQ_GPU))
					{
						TOMSetPendingGPUInt();
						m68k_set_irq(2);
					}
					data &= ~0x02;
				}

				if (data & 0x04)
				{
					GPUSetIRQLine(0, ASSERT_LINE);
					m68k_end_timeslice();
					data &= ~0x04;
				}

				gpu_control = (gpu_control & 0xF7C0) | (data & (~0xF7C0));

				if (GPU_RUNNING)
					m68k_end_timeslice();
				break;
			}
			case 0x18:
				gpu_hidata = data;
				break;
			case 0x1C:
				gpu_div_control = data;
				break;
		}

		return;
	}

	JaguarWriteLong(offset, data, who);
}

//
// Change register banks if necessary
//
void GPUUpdateRegisterBanks(void)
{
	int bank = (gpu_flags & REGPAGE);

	if (gpu_flags & IMASK)
		bank = 0;

	if (bank)
		gpu_reg = gpu_reg_bank_1, gpu_alternate_reg = gpu_reg_bank_0;
	else
		gpu_reg = gpu_reg_bank_0, gpu_alternate_reg = gpu_reg_bank_1;
}

void GPUHandleIRQs(void)
{
	if (gpu_flags & IMASK)
		return;

	uint32_t bits = (gpu_control >> 6) & 0x1F, mask = (gpu_flags >> 4) & 0x1F;

	bits &= mask;
	if (!bits)
		return;

	uint32_t which = 0;
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

	gpu_flags |= IMASK;
	GPUUpdateRegisterBanks();

	gpu_reg[31] -= 4;
	GPUWriteLong(gpu_reg[31], gpu_pc - 2, GPU);

	gpu_pc = gpu_reg[30] = GPU_WORK_RAM_BASE + (which * 0x10);
}

void GPUSetIRQLine(int irqline, int state)
{
	uint32_t mask = 0x0040 << irqline;
	gpu_control &= ~mask;

	if (state)
	{
		gpu_control |= mask;
		GPUHandleIRQs();
	}
}

void GPUInit(void)
{
	build_branch_condition_table();

	GPUReset();
}

void GPUReset(void)
{
	gpu_flags			  = 0x00000000;
	gpu_matrix_control    = 0x00000000;
	gpu_pointer_to_matrix = 0x00000000;
	gpu_data_organization = 0xFFFFFFFF;
	gpu_pc				  = 0x00F03000;
	gpu_control			  = 0x00002800;
	gpu_hidata			  = 0x00000000;
	gpu_remain			  = 0x00000000;
	gpu_div_control		  = 0x00000000;

	gpu_acc				  = 0x00000000;

	gpu_reg = gpu_reg_bank_0;
	gpu_alternate_reg = gpu_reg_bank_1;

	for(int i=0; i<32; i++)
		gpu_reg[i] = gpu_alternate_reg[i] = 0x00000000;

	gpu_flag_z = gpu_flag_n = gpu_flag_c = 0;
	memset(gpu_ram_8, 0xFF, 0x1000);

	for(uint32_t i=0; i<4096; i+=4)
		*((uint32_t *)(&gpu_ram_8[i])) = rand();
}

void GPUDone(void)
{
}

//
// Main GPU execution core
//

void GPUExec(int32_t cycles)
{
	if (!GPU_RUNNING)
		return;

	GPUHandleIRQs();

	while (cycles > 0 && GPU_RUNNING)
	{
		MAYBE_CALLBACK(GPUTraceCallback, gpu_pc, gpu_reg);

		uint16_t opcode = GPUReadWord(gpu_pc, GPU);
		uint32_t index = opcode >> 10;
		gpu_opcode_first_parameter = (opcode >> 5) & 0x1F;
		gpu_opcode_second_parameter = opcode & 0x1F;

		gpu_pc += 2;
		gpu_opcode[index]();

		cycles -= gpu_opcode_cycles[index];
	}
}

//
// GPU opcodes
//

#define RISC 3

#include "risc_opcodes.h"
