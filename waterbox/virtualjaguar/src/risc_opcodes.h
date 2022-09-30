// memory.h has GPU == 3 and DSP == 2

#if RISC == 3
	#define RISC_OPCODE(op)			static void gpu_opcode_##op(void)
	#define risc_inhibit_interrupt	gpu_inhibit_interrupt
	#define IMM_1					gpu_opcode_first_parameter
	#define IMM_2					gpu_opcode_second_parameter
	#define risc_flag_n				gpu_flag_n
	#define risc_flag_c				gpu_flag_c
	#define risc_flag_z				gpu_flag_z
	#define risc_pc					gpu_pc
	#define risc_reg				gpu_reg
	#define risc_alternate_reg		gpu_alternate_reg
	#define risc_reg_bank_1			gpu_reg_bank_1
	#define risc_acc				gpu_acc
	#define risc_matrix_control		gpu_matrix_control
	#define risc_pointer_to_matrix	gpu_pointer_to_matrix
	#define risc_div_control		gpu_div_control
	#define risc_remain				gpu_remain
	#define IS_RISC_RAM(x)			x >= GPU_WORK_RAM_BASE && x <= (GPU_WORK_RAM_BASE + 0xFFF)
	#define RISCExec(x)				GPUExec(x)
	#define RISCReadWord(x, y)		GPUReadWord(x, y)
	#define RISCReadLong(x, y)		GPUReadLong(x, y)
	#define RISCWriteLong(x, y, z)	GPUWriteLong(x, y, z)
#elif RISC == 2
	#define RISC_OPCODE(op)			static void dsp_opcode_##op(void)
	#define risc_inhibit_interrupt	dsp_inhibit_interrupt
	#define IMM_1					dsp_opcode_first_parameter
	#define IMM_2					dsp_opcode_second_parameter
	#define risc_flag_n				dsp_flag_n
	#define risc_flag_c				dsp_flag_c
	#define risc_flag_z				dsp_flag_z
	#define risc_pc					dsp_pc
	#define risc_reg				dsp_reg
	#define risc_alternate_reg		dsp_alternate_reg
	#define risc_reg_bank_1			dsp_reg_bank_1
	#define risc_acc				dsp_acc
	#define risc_matrix_control		dsp_matrix_control
	#define risc_pointer_to_matrix	dsp_pointer_to_matrix
	#define risc_div_control		dsp_div_control
	#define risc_remain				dsp_remain
	#define IS_RISC_RAM(x)			x >= DSP_WORK_RAM_BASE && x <= (DSP_WORK_RAM_BASE + 0x1FFF)
	#define RISCExec(x)				DSPExec(x)
	#define RISCReadWord(x, y)		DSPReadWord(x, y)
	#define RISCReadLong(x, y)		DSPReadLong(x, y)
	#define RISCWriteLong(x, y, z)	DSPWriteLong(x, y, z)
#else
	#error RISC improperly defined
#endif

#define RM					risc_reg[IMM_1]
#define RN					risc_reg[IMM_2]
#define ALTERNATE_RM		risc_alternate_reg[IMM_1]
#define ALTERNATE_RN		risc_alternate_reg[IMM_2]

#define CLR_Z				(risc_flag_z = 0)
#define CLR_ZN				(risc_flag_z = risc_flag_n = 0)
#define CLR_ZNC				(risc_flag_z = risc_flag_n = risc_flag_c = 0)
#define SET_Z(r)			(risc_flag_z = ((r) == 0))
#define SET_N(r)			(risc_flag_n = (((uint32_t)(r) >> 31) & 0x01))
#define SET_C_ADD(a,b)		(risc_flag_c = ((uint32_t)(b) > (uint32_t)(~(a))))
#define SET_C_SUB(a,b)		(risc_flag_c = ((uint32_t)(b) > (uint32_t)(a)))
#define SET_ZN(r)			SET_N(r); SET_Z(r)
#define SET_ZNC_ADD(a,b,r)	SET_N(r); SET_Z(r); SET_C_ADD(a,b)
#define SET_ZNC_SUB(a,b,r)	SET_N(r); SET_Z(r); SET_C_SUB(a,b)

#define BRANCH_CONDITION(x)	branch_condition_table[(x) + ((risc_flags & 7) << 5)]

#if 0
#include <stdio.h>
#define ALERT_UNALIGNED_LONG(x) if (x & 3) fprintf(stderr, "unaligned long access in %s\n", __func__)
#define ALERT_UNALIGNED_WORD(x) if (x & 1) fprintf(stderr, "unaligned word access in %s\n", __func__)
#else
#define ALERT_UNALIGNED_LONG(x)
#define ALERT_UNALIGNED_WORD(x)
#endif

static const uint32_t risc_convert_zero[32] = {
	32, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
	17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31
};

RISC_OPCODE(jump)
{
	uint32_t risc_flags = (risc_flag_n << 2) | (risc_flag_c << 1) | risc_flag_z;

	if (BRANCH_CONDITION(IMM_2))
	{
		uint32_t delayed_pc = RM;
		risc_inhibit_interrupt = 1;
		RISCExec(1);
		risc_pc = delayed_pc;
	}
}

RISC_OPCODE(jr)
{
	uint32_t risc_flags = (risc_flag_n << 2) | (risc_flag_c << 1) | risc_flag_z;

	if (BRANCH_CONDITION(IMM_2))
	{
		int32_t offset = (IMM_1 & 0x10 ? 0xFFFFFFF0 | IMM_1 : IMM_1);
		int32_t delayed_pc = risc_pc + (offset * 2);
		risc_inhibit_interrupt = 1;
		RISCExec(1);
		risc_pc = delayed_pc;
	}
}

RISC_OPCODE(add)
{
	uint32_t res = RN + RM;
	SET_ZNC_ADD(RN, RM, res);
	RN = res;
}

RISC_OPCODE(addc)
{
	uint32_t res = RN + RM + risc_flag_c;
	uint32_t carry = risc_flag_c;
	SET_ZNC_ADD(RN, RM + carry, res);
	RN = res;
}

RISC_OPCODE(addq)
{
	uint32_t r1 = risc_convert_zero[IMM_1];
	uint32_t res = RN + r1;
	SET_ZNC_ADD(RN, r1, res);
	RN = res;
}

RISC_OPCODE(sub)
{
	uint32_t res = RN - RM;
	SET_ZNC_SUB(RN, RM, res);
	RN = res;
}

RISC_OPCODE(subc)
{
	uint64_t res = (uint64_t)RN + (uint64_t)(RM ^ 0xFFFFFFFF) + (risc_flag_c ^ 1);
	risc_flag_c = ((res >> 32) & 0x01) ^ 1;
	RN = (res & 0xFFFFFFFF);
	SET_ZN(RN);
}

RISC_OPCODE(subq)
{
	uint32_t r1 = risc_convert_zero[IMM_1];
	uint32_t res = RN - r1;
	SET_ZNC_SUB(RN, r1, res);
	RN = res;
}

RISC_OPCODE(cmp)
{
	uint32_t res = RN - RM;
	SET_ZNC_SUB(RN, RM, res);
}

RISC_OPCODE(cmpq)
{
	static const int32_t sqtable[32] =
		{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,-16,-15,-14,-13,-12,-11,-10,-9,-8,-7,-6,-5,-4,-3,-2,-1 };

	uint32_t r1 = sqtable[IMM_1 & 0x1F];
	uint32_t res = RN - r1;
	SET_ZNC_SUB(RN, r1, res);
}

RISC_OPCODE(and)
{
	RN = RN & RM;
	SET_ZN(RN);
}

RISC_OPCODE(or)
{
	RN = RN | RM;
	SET_ZN(RN);
}

RISC_OPCODE(xor)
{
	RN = RN ^ RM;
	SET_ZN(RN);
}

RISC_OPCODE(not)
{
	RN = ~RN;
	SET_ZN(RN);
}

RISC_OPCODE(move_pc)
{
	RN = risc_pc - 2;
}

RISC_OPCODE(store_r14_indexed)
{
	uint32_t address = risc_reg[14] + (risc_convert_zero[IMM_1] << 2);
	ALERT_UNALIGNED_LONG(address);
	RISCWriteLong(address & 0xFFFFFFFC, RN, RISC);
}

RISC_OPCODE(store_r15_indexed)
{
	uint32_t address = risc_reg[15] + (risc_convert_zero[IMM_1] << 2);
	ALERT_UNALIGNED_LONG(address);
	RISCWriteLong(address & 0xFFFFFFFC, RN, RISC);
}

RISC_OPCODE(load_r14_ri)
{
	uint32_t address = risc_reg[14] + RM;
	ALERT_UNALIGNED_LONG(address);
	RN = RISCReadLong(address & 0xFFFFFFFC, RISC);
}

RISC_OPCODE(load_r15_ri)
{
	uint32_t address = risc_reg[15] + RM;
	ALERT_UNALIGNED_LONG(address);
	RN = RISCReadLong(address & 0xFFFFFFFC, RISC);
}

RISC_OPCODE(store_r14_ri)
{
	uint32_t address = risc_reg[14] + RM;
	ALERT_UNALIGNED_LONG(address);
	RISCWriteLong(address & 0xFFFFFFFC, RN, RISC);
}

RISC_OPCODE(store_r15_ri)
{
	uint32_t address = risc_reg[15] + RM;
	ALERT_UNALIGNED_LONG(address);
	RISCWriteLong(address & 0xFFFFFFFC, RN, RISC);
}

RISC_OPCODE(nop)
{
}

RISC_OPCODE(storeb)
{
	if (IS_RISC_RAM(RM))
		RISCWriteLong(RM & 0xFFFFFFFC, RN, RISC);
	else
		JaguarWriteByte(RM, RN, RISC);
}

RISC_OPCODE(storew)
{
	ALERT_UNALIGNED_WORD(RM);

	if (IS_RISC_RAM(RM))
		RISCWriteLong(RM & 0xFFFFFFFC, RN, RISC);
	else
		JaguarWriteWord(RM & 0xFFFFFFFE, RN, RISC);
}

RISC_OPCODE(store)
{
	ALERT_UNALIGNED_LONG(RM);
	RISCWriteLong(RM & 0xFFFFFFFC, RN, RISC);
}

RISC_OPCODE(loadb)
{
	if (IS_RISC_RAM(RM))
		RN = RISCReadLong(RM & 0xFFFFFFFC, RISC);
	else
		RN = JaguarReadByte(RM, RISC);
}

RISC_OPCODE(loadw)
{
	ALERT_UNALIGNED_WORD(RM);

	if (IS_RISC_RAM(RM))
		RN = RISCReadLong(RM & 0xFFFFFFFC, RISC);
	else
		RN = JaguarReadWord(RM & 0xFFFFFFFE, RISC);
}

RISC_OPCODE(load)
{
	ALERT_UNALIGNED_LONG(RM);
	RN = RISCReadLong(RM & 0xFFFFFFFC, RISC);
}

RISC_OPCODE(load_r14_indexed)
{
	uint32_t address = risc_reg[14] + (risc_convert_zero[IMM_1] << 2);
	ALERT_UNALIGNED_LONG(address);
	RN = RISCReadLong(address & 0xFFFFFFFC, RISC);
}

RISC_OPCODE(load_r15_indexed)
{
	uint32_t address = risc_reg[15] + (risc_convert_zero[IMM_1] << 2);
	ALERT_UNALIGNED_LONG(address);
	RN = RISCReadLong(address & 0xFFFFFFFC, RISC);
}

RISC_OPCODE(movei)
{
	RN = (uint32_t)RISCReadWord(risc_pc, RISC) | ((uint32_t)RISCReadWord(risc_pc + 2, RISC) << 16);
	risc_pc += 4;
}

RISC_OPCODE(moveta)
{
	ALTERNATE_RN = RM;
}

RISC_OPCODE(movefa)
{
	RN = ALTERNATE_RM;
}

RISC_OPCODE(move)
{
	RN = RM;
}

RISC_OPCODE(moveq)
{
	RN = IMM_1;
}

RISC_OPCODE(resmac)
{
	RN = (uint32_t)risc_acc;
}

RISC_OPCODE(imult)
{
	RN = (int16_t)RN * (int16_t)RM;
	SET_ZN(RN);
}

RISC_OPCODE(mult)
{
	RN = (uint16_t)RM * (uint16_t)RN;
	SET_ZN(RN);
}

RISC_OPCODE(bclr)
{
	uint32_t res = RN & ~(1 << IMM_1);
	RN = res;
	SET_ZN(res);
}

RISC_OPCODE(btst)
{
	risc_flag_z = (~RN >> IMM_1) & 1;
}

RISC_OPCODE(bset)
{
	uint32_t res = RN | (1 << IMM_1);
	RN = res;
	SET_ZN(res);
}

RISC_OPCODE(subqt)
{
	RN -= risc_convert_zero[IMM_1];
}

RISC_OPCODE(addqt)
{
	RN += risc_convert_zero[IMM_1];
}

RISC_OPCODE(imacn)
{
	int32_t res = (int16_t)RM * (int16_t)RN;
	risc_acc += (int64_t)res;
	risc_inhibit_interrupt = 1;
}

RISC_OPCODE(mtoi)
{
	uint32_t _RM = RM;
	uint32_t res = RN = (((int32_t)_RM >> 8) & 0xFF800000) | (_RM & 0x007FFFFF);
	SET_ZN(res);
}

RISC_OPCODE(normi)
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

RISC_OPCODE(mmult)
{
	int count = risc_matrix_control&0x0f;
	uint32_t addr = risc_pointer_to_matrix;
	int64_t accum = 0;
	uint32_t res;

	if (!(risc_matrix_control & 0x10))
	{
		for (int i = 0; i < count; i++)
		{
			int16_t a;
			if (i & 0x01)
				a = (int16_t)((risc_reg_bank_1[IMM_1 + (i >> 1)] >> 16) & 0xffff);
			else
				a = (int16_t)(risc_reg_bank_1[IMM_1 + (i >> 1)] & 0xffff);

			int16_t b = (int16_t)RISCReadLong(addr, RISC);
			accum += a * b;
			addr += 4;
		}
	}
	else
	{
		for (int i = 0; i < count; i++)
		{
			int16_t a;
			if (i & 0x01)
				a = (int16_t)((risc_reg_bank_1[IMM_1 + (i >> 1)] >> 16) & 0xffff);
			else
				a = (int16_t)(risc_reg_bank_1[IMM_1 + (i >> 1)] & 0xffff);

			int16_t b = (int16_t)RISCReadLong(addr, RISC);
			accum += a * b;
			addr += 4 * count;
		}
	}

	RN = res = (uint32_t)accum;
	SET_ZN(RN);
}

RISC_OPCODE(abs)
{
	uint32_t _Rn = RN;
	uint32_t res;

	risc_flag_c = ((_Rn & 0x80000000) >> 31);
	res = RN = (_Rn > 0x80000000 ? -_Rn : _Rn);
	CLR_ZN; SET_Z(res);
}

RISC_OPCODE(div)
{
	uint32_t q = RN;
	uint32_t r = 0;

	if (risc_div_control & 0x01)
		q <<= 16, r = RN >> 16;

	for(int i=0; i<32; i++)
	{
		uint32_t sign = r & 0x80000000;
		r = (r << 1) | ((q >> 31) & 0x01);
		r += (sign ? RM : -RM);
		q = (q << 1) | (((~r) >> 31) & 0x01);
	}

	RN = q;
	risc_remain = r;
}

RISC_OPCODE(imultn)
{
	uint32_t res = (int32_t)((int16_t)RN * (int16_t)RM);
	risc_acc = (int64_t)res;
	SET_ZN(res);
	risc_inhibit_interrupt = 1;
}

RISC_OPCODE(neg)
{
	uint32_t res = -RN;
	SET_ZNC_SUB(0, RN, res);
	RN = res;
}

RISC_OPCODE(shlq)
{
	int32_t r1 = 32 - IMM_1;
	uint32_t res = RN << r1;
	SET_ZN(res); risc_flag_c = (RN >> 31) & 1;
	RN = res;
}

RISC_OPCODE(shrq)
{
	int32_t r1 = risc_convert_zero[IMM_1];
	uint32_t res = RN >> r1;
	SET_ZN(res); risc_flag_c = RN & 1;
	RN = res;
}

RISC_OPCODE(ror)
{
	uint32_t r1 = RM & 0x1F;
	uint32_t res = (RN >> r1) | (RN << (32 - r1));
	SET_ZN(res); risc_flag_c = (RN >> 31) & 1;
	RN = res;
}

RISC_OPCODE(rorq)
{
	uint32_t r1 = risc_convert_zero[IMM_1 & 0x1F];
	uint32_t r2 = RN;
	uint32_t res = (r2 >> r1) | (r2 << (32 - r1));
	RN = res;
	SET_ZN(res); risc_flag_c = (r2 >> 31) & 0x01;
}

RISC_OPCODE(sha)
{
	uint32_t res;

	if (RM & 0x80000000)
	{
		res = ((int32_t)RM <= -32) ? 0 : (RN << -(int32_t)RM);
		risc_flag_c = RN >> 31;
	}
	else
	{
		res = ((int32_t)RM >= 32) ? ((int32_t)RN >> 31) : ((int32_t)RN >> (int32_t)RM);
		risc_flag_c = RN & 0x01;
	}

	RN = res;
	SET_ZN(res);
}

RISC_OPCODE(sharq)
{
	uint32_t res = (int32_t)RN >> risc_convert_zero[IMM_1];
	SET_ZN(res); risc_flag_c = RN & 0x01;
	RN = res;
}

RISC_OPCODE(sh)
{
	if (RM & 0x80000000)
	{
		RN = ((int32_t)RM <= -32) ? 0 : (RN << -(int32_t)RM);
		risc_flag_c = RN >> 31;
	}
	else
	{
		RN = ((int32_t)RM >= 32) ? 0 : (RN >> RM);
		risc_flag_c = RN & 0x01;
	}

	SET_ZN(RN);
}

#if RISC == 3

RISC_OPCODE(sat8)
{
	RN = ((int32_t)RN < 0 ? 0 : (RN > 0xFF ? 0xFF : RN));
	SET_ZN(RN);
}

RISC_OPCODE(sat16)
{
	RN = ((int32_t)RN < 0 ? 0 : (RN > 0xFFFF ? 0xFFFF : RN));
	SET_ZN(RN);
}

RISC_OPCODE(sat24)
{
	RN = ((int32_t)RN < 0 ? 0 : (RN > 0xFFFFFF ? 0xFFFFFF : RN));
	SET_ZN(RN);
}

RISC_OPCODE(pack)
{
	uint32_t val = RN;

	if (IMM_1 == 0)
		RN = ((val >> 10) & 0x0000F000) | ((val >> 5) & 0x00000F00) | (val & 0x000000FF);
	else
		RN = ((val & 0x0000F000) << 10) | ((val & 0x00000F00) << 5) | (val & 0x000000FF);
}

RISC_OPCODE(storep)
{
	ALERT_UNALIGNED_LONG(RM);

	if (IS_RISC_RAM(RM))
	{
		RISCWriteLong(RM & 0xFFFFFFFC, RN, RISC);
	}
	else
	{
		RISCWriteLong((RM + 0) & 0xFFFFFFFC, gpu_hidata, RISC);
		RISCWriteLong((RM + 4) & 0xFFFFFFFC, RN, RISC);
	}
}

RISC_OPCODE(loadp)
{
	ALERT_UNALIGNED_LONG(RM);

	if (IS_RISC_RAM(RM))
	{
		RN			= RISCReadLong(RM & 0xFFFFFFFC, RISC);
	}
	else
	{
		gpu_hidata	= RISCReadLong((RM + 0) & 0xFFFFFFFC, RISC);
		RN			= RISCReadLong((RM + 4) & 0xFFFFFFFC, RISC);
	}
}

#elif RISC == 2

RISC_OPCODE(addqmod)
{
	uint32_t r1 = risc_convert_zero[IMM_1];
	uint32_t r2 = RN;
	uint32_t res = r2 + r1;
	res = (res & (~dsp_modulo)) | (r2 & dsp_modulo);
	RN = res;
	SET_ZNC_ADD(r2, r1, res);
}

RISC_OPCODE(subqmod)
{
	uint32_t r1 = risc_convert_zero[IMM_1];
	uint32_t r2 = RN;
	uint32_t res = r2 - r1;
	res = (res & (~dsp_modulo)) | (r2 & dsp_modulo);
	RN = res;

	SET_ZNC_SUB(r2, r1, res);
}

RISC_OPCODE(mirror)
{
	uint32_t r1 = RN;
	RN = (mirror_table[r1 & 0xFFFF] << 16) | mirror_table[r1 >> 16];
	SET_ZN(RN);
}

RISC_OPCODE(sat32s)
{
	int32_t r2 = (uint32_t)RN;
	int32_t temp = risc_acc >> 32;
	uint32_t res = (temp < -1) ? (int32_t)0x80000000 : (temp > 0) ? (int32_t)0x7FFFFFFF : r2;
	RN = res;
	SET_ZN(res);
}

RISC_OPCODE(sat16s)
{
	int32_t r2 = RN;
	uint32_t res = (r2 < -32768) ? -32768 : (r2 > 32767) ? 32767 : r2;
	RN = res;
	SET_ZN(res);
}

RISC_OPCODE(illegal)
{
}

#else
#error How did this happen?
#endif
