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
#include "jagdasm.h"
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

uint8_t gpu_opcode_cycles[64] =
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

void (*gpu_opcode[64])()=
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

static uint8_t gpu_ram_8[0x1000];
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

#define RM				gpu_reg[gpu_opcode_first_parameter]
#define RN				gpu_reg[gpu_opcode_second_parameter]
#define ALTERNATE_RM	gpu_alternate_reg[gpu_opcode_first_parameter]
#define ALTERNATE_RN	gpu_alternate_reg[gpu_opcode_second_parameter]
#define IMM_1			gpu_opcode_first_parameter
#define IMM_2			gpu_opcode_second_parameter

#define SET_FLAG_Z(r)	(gpu_flag_z = ((r) == 0));
#define SET_FLAG_N(r)	(gpu_flag_n = (((uint32_t)(r) >> 31) & 0x01));

#define RESET_FLAG_Z()	gpu_flag_z = 0;
#define RESET_FLAG_N()	gpu_flag_n = 0;
#define RESET_FLAG_C()	gpu_flag_c = 0;

#define CLR_Z				(gpu_flag_z = 0)
#define CLR_ZN				(gpu_flag_z = gpu_flag_n = 0)
#define CLR_ZNC				(gpu_flag_z = gpu_flag_n = gpu_flag_c = 0)
#define SET_Z(r)			(gpu_flag_z = ((r) == 0))
#define SET_N(r)			(gpu_flag_n = (((uint32_t)(r) >> 31) & 0x01))
#define SET_C_ADD(a,b)		(gpu_flag_c = ((uint32_t)(b) > (uint32_t)(~(a))))
#define SET_C_SUB(a,b)		(gpu_flag_c = ((uint32_t)(b) > (uint32_t)(a)))
#define SET_ZN(r)			SET_N(r); SET_Z(r)
#define SET_ZNC_ADD(a,b,r)	SET_N(r); SET_Z(r); SET_C_ADD(a,b)
#define SET_ZNC_SUB(a,b,r)	SET_N(r); SET_Z(r); SET_C_SUB(a,b)

uint32_t gpu_convert_zero[32] =
	{ 32,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31 };

uint8_t * branch_condition_table = 0;
#define BRANCH_CONDITION(x)	branch_condition_table[(x) + ((jaguar_flags & 7) << 5)]

uint32_t GPUGetPC(void)
{
	return gpu_pc;
}

void build_branch_condition_table(void)
{
	if (!branch_condition_table)
	{
		branch_condition_table = (uint8_t *)malloc(32 * 8 * sizeof(branch_condition_table[0]));

		if (branch_condition_table)
		{
			for(int i=0; i<8; i++)
			{
				for(int j=0; j<32; j++)
				{
					int result = 1;
					if (j & 1)
						if (i & ZERO_FLAG)
							result = 0;
					if (j & 2)
						if (!(i & ZERO_FLAG))
							result = 0;
					if (j & 4)
						if (i & (CARRY_FLAG << (j >> 4)))
							result = 0;
					if (j & 8)
						if (!(i & (CARRY_FLAG << (j >> 4))))
							result = 0;
					branch_condition_table[i * 32 + j] = result;
				}
			}
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

	CLR_ZNC;
	memset(gpu_ram_8, 0xFF, 0x1000);

	for(uint32_t i=0; i<4096; i+=4)
		*((uint32_t *)(&gpu_ram_8[i])) = rand();
}

uint32_t GPUReadPC(void)
{
	return gpu_pc;
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

static void gpu_opcode_jump(void)
{
	uint32_t jaguar_flags = (gpu_flag_n << 2) | (gpu_flag_c << 1) | gpu_flag_z;

	if (BRANCH_CONDITION(IMM_2))
	{
		uint32_t delayed_pc = RM;
		GPUExec(1);
		gpu_pc = delayed_pc;
	}
}

static void gpu_opcode_jr(void)
{
	uint32_t jaguar_flags = (gpu_flag_n << 2) | (gpu_flag_c << 1) | gpu_flag_z;

	if (BRANCH_CONDITION(IMM_2))
	{
		int32_t offset = (IMM_1 & 0x10 ? 0xFFFFFFF0 | IMM_1 : IMM_1);
		int32_t delayed_pc = gpu_pc + (offset * 2);
		GPUExec(1);
		gpu_pc = delayed_pc;
	}
}

static void gpu_opcode_add(void)
{
	uint32_t res = RN + RM;
	CLR_ZNC; SET_ZNC_ADD(RN, RM, res);
	RN = res;
}

static void gpu_opcode_addc(void)
{
	uint32_t res = RN + RM + gpu_flag_c;
	uint32_t carry = gpu_flag_c;
	SET_ZNC_ADD(RN + carry, RM, res);
	RN = res;
}

static void gpu_opcode_addq(void)
{
	uint32_t r1 = gpu_convert_zero[IMM_1];
	uint32_t res = RN + r1;
	CLR_ZNC; SET_ZNC_ADD(RN, r1, res);
	RN = res;
}

static void gpu_opcode_addqt(void)
{
	RN += gpu_convert_zero[IMM_1];
}

static void gpu_opcode_sub(void)
{
	uint32_t res = RN - RM;
	SET_ZNC_SUB(RN, RM, res);
	RN = res;
}

static void gpu_opcode_subc(void)
{
	uint64_t res = (uint64_t)RN + (uint64_t)(RM ^ 0xFFFFFFFF) + (gpu_flag_c ^ 1);
	gpu_flag_c = ((res >> 32) & 0x01) ^ 1;
	RN = (res & 0xFFFFFFFF);
	SET_ZN(RN);
}

static void gpu_opcode_subq(void)
{
	uint32_t r1 = gpu_convert_zero[IMM_1];
	uint32_t res = RN - r1;
	SET_ZNC_SUB(RN, r1, res);
	RN = res;
}

static void gpu_opcode_subqt(void)
{
	RN -= gpu_convert_zero[IMM_1];
}

static void gpu_opcode_cmp(void)
{
	uint32_t res = RN - RM;
	SET_ZNC_SUB(RN, RM, res);
}

static void gpu_opcode_cmpq(void)
{
	static int32_t sqtable[32] =
		{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,-16,-15,-14,-13,-12,-11,-10,-9,-8,-7,-6,-5,-4,-3,-2,-1 };

	uint32_t r1 = sqtable[IMM_1 & 0x1F];
	uint32_t res = RN - r1;
	SET_ZNC_SUB(RN, r1, res);
}

static void gpu_opcode_and(void)
{
	RN = RN & RM;
	SET_ZN(RN);
}

static void gpu_opcode_or(void)
{
	RN = RN | RM;
	SET_ZN(RN);
}

static void gpu_opcode_xor(void)
{
	RN = RN ^ RM;
	SET_ZN(RN);
}

static void gpu_opcode_not(void)
{
	RN = ~RN;
	SET_ZN(RN);
}

static void gpu_opcode_move_pc(void)
{
	RN = gpu_pc - 2;
}

static void gpu_opcode_sat8(void)
{
	RN = ((int32_t)RN < 0 ? 0 : (RN > 0xFF ? 0xFF : RN));
	SET_ZN(RN);
}

static void gpu_opcode_sat16(void)
{
	RN = ((int32_t)RN < 0 ? 0 : (RN > 0xFFFF ? 0xFFFF : RN));
	SET_ZN(RN);
}

static void gpu_opcode_sat24(void)
{
	RN = ((int32_t)RN < 0 ? 0 : (RN > 0xFFFFFF ? 0xFFFFFF : RN));
	SET_ZN(RN);
}

static void gpu_opcode_store_r14_indexed(void)
{
	uint32_t address = gpu_reg[14] + (gpu_convert_zero[IMM_1] << 2);
	
	if (address >= 0xF03000 && address <= 0xF03FFF)
		GPUWriteLong(address & 0xFFFFFFFC, RN, GPU);
	else
		GPUWriteLong(address, RN, GPU);
}

static void gpu_opcode_store_r15_indexed(void)
{
	uint32_t address = gpu_reg[15] + (gpu_convert_zero[IMM_1] << 2);

	if (address >= 0xF03000 && address <= 0xF03FFF)
		GPUWriteLong(address & 0xFFFFFFFC, RN, GPU);
	else
		GPUWriteLong(address, RN, GPU);
}

static void gpu_opcode_load_r14_ri(void)
{
	uint32_t address = gpu_reg[14] + RM;

	if (address >= 0xF03000 && address <= 0xF03FFF)
		RN = GPUReadLong(address & 0xFFFFFFFC, GPU);
	else
		RN = GPUReadLong(address, GPU);
}

static void gpu_opcode_load_r15_ri(void)
{
	uint32_t address = gpu_reg[15] + RM;

	if (address >= 0xF03000 && address <= 0xF03FFF)
		RN = GPUReadLong(address & 0xFFFFFFFC, GPU);
	else
		RN = GPUReadLong(address, GPU);
}

static void gpu_opcode_store_r14_ri(void)
{
	uint32_t address = gpu_reg[14] + RM;

	if (address >= 0xF03000 && address <= 0xF03FFF)
		GPUWriteLong(address & 0xFFFFFFFC, RN, GPU);
	else
		GPUWriteLong(address, RN, GPU);
}

static void gpu_opcode_store_r15_ri(void)
{
	GPUWriteLong(gpu_reg[15] + RM, RN, GPU);
}

static void gpu_opcode_nop(void)
{
}

static void gpu_opcode_pack(void)
{
	uint32_t val = RN;

	if (IMM_1 == 0)
		RN = ((val >> 10) & 0x0000F000) | ((val >> 5) & 0x00000F00) | (val & 0x000000FF);
	else
		RN = ((val & 0x0000F000) << 10) | ((val & 0x00000F00) << 5) | (val & 0x000000FF);
}

static void gpu_opcode_storeb(void)
{
	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
		GPUWriteLong(RM, RN & 0xFF, GPU);
	else
		JaguarWriteByte(RM, RN, GPU);
}

static void gpu_opcode_storew(void)
{
	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
		GPUWriteLong(RM & 0xFFFFFFFE, RN & 0xFFFF, GPU);
	else
		JaguarWriteWord(RM, RN, GPU);
}

static void gpu_opcode_store(void)
{
	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
		GPUWriteLong(RM & 0xFFFFFFFC, RN, GPU);
	else
		GPUWriteLong(RM, RN, GPU);
}

static void gpu_opcode_storep(void)
{
	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
	{
		GPUWriteLong((RM & 0xFFFFFFF8) + 0, gpu_hidata, GPU);
		GPUWriteLong((RM & 0xFFFFFFF8) + 4, RN, GPU);
	}
	else
	{
		GPUWriteLong(RM + 0, gpu_hidata, GPU);
		GPUWriteLong(RM + 4, RN, GPU);
	}
}

static void gpu_opcode_loadb(void)
{
	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
		RN = GPUReadLong(RM, GPU) & 0xFF;
	else
		RN = JaguarReadByte(RM, GPU);
}

static void gpu_opcode_loadw(void)
{
	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
		RN = GPUReadLong(RM & 0xFFFFFFFE, GPU) & 0xFFFF;
	else
		RN = JaguarReadWord(RM, GPU);
}

static void gpu_opcode_load(void)
{
	RN = GPUReadLong(RM & 0xFFFFFFFC, GPU);
}

static void gpu_opcode_loadp(void)
{
	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
	{
		gpu_hidata = GPUReadLong((RM & 0xFFFFFFF8) + 0, GPU);
		RN		   = GPUReadLong((RM & 0xFFFFFFF8) + 4, GPU);
	}
	else
	{
		gpu_hidata = GPUReadLong(RM + 0, GPU);
		RN		   = GPUReadLong(RM + 4, GPU);
	}
}

static void gpu_opcode_load_r14_indexed(void)
{
	uint32_t address = gpu_reg[14] + (gpu_convert_zero[IMM_1] << 2);

	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
		RN = GPUReadLong(address & 0xFFFFFFFC, GPU);
	else
		RN = GPUReadLong(address, GPU);
}

static void gpu_opcode_load_r15_indexed(void)
{
	uint32_t address = gpu_reg[15] + (gpu_convert_zero[IMM_1] << 2);

	if ((RM >= 0xF03000) && (RM <= 0xF03FFF))
		RN = GPUReadLong(address & 0xFFFFFFFC, GPU);
	else
		RN = GPUReadLong(address, GPU);
}

static void gpu_opcode_movei(void)
{
	RN = (uint32_t)GPUReadWord(gpu_pc, GPU) | ((uint32_t)GPUReadWord(gpu_pc + 2, GPU) << 16);
	gpu_pc += 4;
}

static void gpu_opcode_moveta(void)
{
	ALTERNATE_RN = RM;
}

static void gpu_opcode_movefa(void)
{
	RN = ALTERNATE_RM;
}

static void gpu_opcode_move(void)
{
	RN = RM;
}

static void gpu_opcode_moveq(void)
{
	RN = IMM_1;
}

static void gpu_opcode_resmac(void)
{
	RN = gpu_acc;
}

static void gpu_opcode_imult(void)
{
	RN = (int16_t)RN * (int16_t)RM;
	SET_ZN(RN);
}

static void gpu_opcode_mult(void)
{
	RN = (uint16_t)RM * (uint16_t)RN;
	SET_ZN(RN);
}

static void gpu_opcode_bclr(void)
{
	uint32_t res = RN & ~(1 << IMM_1);
	RN = res;
	SET_ZN(res);
}

static void gpu_opcode_btst(void)
{
	gpu_flag_z = (~RN >> IMM_1) & 1;
}

static void gpu_opcode_bset(void)
{
	uint32_t res = RN | (1 << IMM_1);
	RN = res;
	SET_ZN(res);
}

static void gpu_opcode_imacn(void)
{
	uint32_t res = (int16_t)RM * (int16_t)(RN);
	gpu_acc += res;
}

static void gpu_opcode_mtoi(void)
{
	uint32_t _RM = RM;
	uint32_t res = RN = (((int32_t)_RM >> 8) & 0xFF800000) | (_RM & 0x007FFFFF);
	SET_ZN(res);
}

static void gpu_opcode_normi(void)
{
	uint32_t _RM = RM;
	uint32_t res = 0;

	if (_RM)
	{
		while ((_RM & 0xFFC00000) == 0)
		{
			_RM <<= 1;
			res--;
		}
		while ((_RM & 0xFF800000) != 0)
		{
			_RM >>= 1;
			res++;
		}
	}
	RN = res;
	SET_ZN(res);
}

static void gpu_opcode_mmult(void)
{
	int count	= gpu_matrix_control & 0x0F;
	uint32_t addr = gpu_pointer_to_matrix;
	int64_t accum = 0;
	uint32_t res;

	if (gpu_matrix_control & 0x10)
	{
		for(int i=0; i<count; i++)
		{
			int16_t a;
			if (i & 0x01)
				a = (int16_t)((gpu_alternate_reg[IMM_1 + (i >> 1)] >> 16) & 0xFFFF);
			else
				a = (int16_t)(gpu_alternate_reg[IMM_1 + (i >> 1)] & 0xFFFF);

			int16_t b = ((int16_t)GPUReadWord(addr + 2, GPU));
			accum += a * b;
			addr += 4 * count;
		}
	}
	else
	{
		for(int i=0; i<count; i++)
		{
			int16_t a;
			if (i & 0x01)
				a = (int16_t)((gpu_alternate_reg[IMM_1 + (i >> 1)] >> 16) & 0xFFFF);
			else
				a = (int16_t)(gpu_alternate_reg[IMM_1 + (i >> 1)] & 0xFFFF);

			int16_t b = ((int16_t)GPUReadWord(addr + 2, GPU));
			accum += a * b;
			addr += 4;
		}
	}
	RN = res = (int32_t)accum;
	SET_ZN(res);
}

static void gpu_opcode_abs(void)
{
	gpu_flag_c = RN >> 31;
	if (RN == 0x80000000)
		gpu_flag_n = 1, gpu_flag_z = 0;
	else
	{
		if (gpu_flag_c)
			RN = -RN;
		gpu_flag_n = 0; SET_FLAG_Z(RN);
	}
}

static void gpu_opcode_div(void)
{
	uint32_t q = RN;
	uint32_t r = 0;

	if (gpu_div_control & 0x01)
		q <<= 16, r = RN >> 16;

	for(int i=0; i<32; i++)
	{
		uint32_t sign = r & 0x80000000;
		r = (r << 1) | ((q >> 31) & 0x01);
		r += (sign ? RM : -RM);
		q = (q << 1) | (((~r) >> 31) & 0x01);
	}

	RN = q;
	gpu_remain = r;
}

static void gpu_opcode_imultn(void)
{
	uint32_t res = (int32_t)((int16_t)RN * (int16_t)RM);
	gpu_acc = (int32_t)res;
	SET_FLAG_Z(res);
	SET_FLAG_N(res);
}

static void gpu_opcode_neg(void)
{
	uint32_t res = -RN;
	SET_ZNC_SUB(0, RN, res);
	RN = res;
}

static void gpu_opcode_shlq(void)
{
	int32_t r1 = 32 - IMM_1;
	uint32_t res = RN << r1;
	SET_ZN(res); gpu_flag_c = (RN >> 31) & 1;
	RN = res;
}

static void gpu_opcode_shrq(void)
{
	int32_t r1 = gpu_convert_zero[IMM_1];
	uint32_t res = RN >> r1;
	SET_ZN(res); gpu_flag_c = RN & 1;
	RN = res;
}

static void gpu_opcode_ror(void)
{
	uint32_t r1 = RM & 0x1F;
	uint32_t res = (RN >> r1) | (RN << (32 - r1));
	SET_ZN(res); gpu_flag_c = (RN >> 31) & 1;
	RN = res;
}

static void gpu_opcode_rorq(void)
{
	uint32_t r1 = gpu_convert_zero[IMM_1 & 0x1F];
	uint32_t r2 = RN;
	uint32_t res = (r2 >> r1) | (r2 << (32 - r1));
	RN = res;
	SET_ZN(res); gpu_flag_c = (r2 >> 31) & 0x01;
}

static void gpu_opcode_sha(void)
{
	uint32_t res;

	if ((int32_t)RM < 0)
	{
		res = ((int32_t)RM <= -32) ? 0 : (RN << -(int32_t)RM);
		gpu_flag_c = RN >> 31;
	}
	else
	{
		res = ((int32_t)RM >= 32) ? ((int32_t)RN >> 31) : ((int32_t)RN >> (int32_t)RM);
		gpu_flag_c = RN & 0x01;
	}
	RN = res;
	SET_ZN(res);
}

static void gpu_opcode_sharq(void)
{
	uint32_t res = (int32_t)RN >> gpu_convert_zero[IMM_1];
	SET_ZN(res); gpu_flag_c = RN & 0x01;
	RN = res;
}

static void gpu_opcode_sh(void)
{
	if (RM & 0x80000000)
	{
		gpu_flag_c = RN >> 31;
		RN = ((int32_t)RM <= -32 ? 0 : RN << -(int32_t)RM);
	}
	else
	{
		gpu_flag_c = RN & 0x01;
		RN = (RM >= 32 ? 0 : RN >> RM);
	}
	SET_ZN(RN);
}
