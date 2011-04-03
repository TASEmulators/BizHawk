using System;
using System.Diagnostics;

namespace BizHawk.Emulation.CPUs.ARM
{

	partial class ARM
	{
		//A8.6.4 ADD
		uint ExecuteCore_ADD_immediate_thumb(Encoding encoding, uint n, uint d, bool setflags, uint imm32)
		{
			if (disassemble)
				if (disopt.showExplicitAccumulateRegisters || n != d)
					return DISNEW("ADD<s?><c?>", "<Rd!>, <Rn!>, #<const>", setflags, false, d, n, imm32);
				else
					return DISNEW("ADD<s?><c?>", "<Rd!>, #<const>", setflags, false, d, imm32);

			_AddWithCarry32(r[n], imm32, 0);
			r[d] = alu_result_32;
			if (setflags)
			{
				APSR.N = _.BIT31(alu_result_32);
				APSR.Z = _IsZeroBit(alu_result_32);
				APSR.C = alu_carry_out;
				APSR.V = alu_overflow;
			}

			return 1;
		}

		//A8.6.5 ADD (immediate, ARM)
		uint ExecuteCore_ADD_immediate_arm(Encoding encoding, bool setflags, uint n, uint d, uint imm32)
		{
			if (disassemble)
				if (disopt.showExplicitAccumulateRegisters || n != d)
					return DISNEW("ADD<s?><c>", "<rd!>, <rn!>, #<const>", setflags, d, n, imm32);
				else
					return DISNEW("ADD<s?><c>", "<rd!>, #<const>", setflags, d, imm32);

			_AddWithCarry32(r[n], imm32, 0);
			if (d == 15)
				_ALUWritePC(alu_result_32); //"setflags is always FALSE here"
			else
			{
				r[d] = alu_result_32;
				if (setflags)
				{
					APSR.N = _.BIT31(alu_result_32);
					APSR.Z = _IsZeroBit(alu_result_32);
					APSR.C = alu_carry_out;
					APSR.V = alu_overflow;
				}
			}

			return 1;
		}

		//A8.6.6 ADD (register)
		uint ExecuteCore_ADD_register(Encoding encoding, uint m, uint d, uint n, bool setflags, SRType shift_t, int shift_n)
		{
			if (disassemble)
				return DISNEW("ADD<s?><c>", "<Rd!>, <Rn!>, <Rm!><{, shift}>", setflags, d, n, m, shift_t, shift_n);

			uint shifted = _Shift(r[m], shift_t, shift_n, APSR.C);
			_AddWithCarry32(r[n], shifted, 0);
			if (d == 15) _ALUWritePC(alu_result_32);
			else
			{
				r[d] = alu_result_32;
				if (setflags)
				{
					APSR.N = _.BIT31(alu_result_32);
					APSR.Z = _IsZeroBit(alu_result_32);
					APSR.C = alu_carry_out;
					APSR.V = alu_overflow;
				}
			}
			return 0;
		}

		//A8.6.8 ADD (SP plus immediate)
		uint ExecuteCore_ADD_SP_plus_immedate(Encoding encoding, uint d, bool setflags, uint imm32)
		{
			if (disassemble)
				return DISNEW("ADD<s?><c>", "<rd!>, <sp!>, #<const>", setflags, d, imm32);

			if (setflags && d == 15) throw new NotImplementedException("see SUBS PC, LR and related instruction on page B6-25");
			//addendum about !s && d==PC is correctly handled already by writing pseudocode
			_AddWithCarry32(SP, imm32, 0);
			if (d == 15)
			{
				Debug.Assert(!setflags);
				_ALUWritePC(alu_result_32); //"can only occur for ARM encoding. setflags is always FALSE here"
			}
			else
			{
				r[d] = alu_result_32;
				if (setflags)
				{
					APSR.N = _.BIT31(alu_result_32);
					APSR.Z = _IsZeroBit(alu_result_32);
					APSR.C = alu_carry_out;
					APSR.V = alu_overflow;
				}
			}

			return 1;
		}

		//A8.6.10 ADR
		uint ExecuteCore_ADR(Encoding encoding, uint d, uint imm32, bool add)
		{
			uint result;
			if (add)
				result = _Align(PC, 4) + imm32;
			else result = _Align(PC, 4) - imm32;

			if (disassemble)
				if (nstyle)
					return DIS("ADD", "/r0/,pc,/imm8_1/ ; maybe MOV rX,pc", d, imm32);
				else return DIS("ADR", "/r0/, /label1/ ; maybe MOV rX,pc", d, result);

			if (d == 15)
				_ALUWritePC(result);
			else r[d] = result;
			return 1;
		}

		//A8.6.14 ASR (immediate)
		uint ExecuteCore_ASR_immediate(Encoding encoding, uint d, uint m, bool setflags, int shift_n)
		{
			if (disassemble)
				return DISNEW("ASR<s?><c>", "<{rd!~rm, }><rm!>, #<imm>", setflags, d, m, m, shift_n);

			uint result;
			Bit carry;
			_Shift_C(r[m], SRType.ASR, shift_n, APSR.C, out result, out carry);
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = _.BIT31(result);
					APSR.Z = _IsZeroBit(result);
					APSR.C = carry;
					//APSR.V unchanged
				}
			}

			return 0;
		}

		//A8.6.16 B
		uint ExecuteCore_B(Encoding encoding, int imm32)
		{
			uint label = (uint)((int)PC + imm32);

			if (disassemble)
				return DISNEW("B<c>", "<label>", label);

			//make sure to check conditions here since this can get called from thumb
			if (_ConditionPassed())
				_BranchWritePC(label);

			return 1;
		}

		//A8.6.23 BL, BLX (immediate)
		uint ExecuteCore_BL_BLX_immediate(Encoding encoding, EInstrSet targetInstrSet, int imm32, bool blx)
		{
			uint targetAddress;
			if (targetInstrSet == EInstrSet.ARM)
				targetAddress = (uint)((int)_Align(PC, 4) + imm32);
			else
				targetAddress = (uint)((int)PC + imm32);

			if (disassemble)
				return DISNEW(blx ? "blx<c?>" : "bl<c?>", "<label>", (encoding == Encoding.A1), targetAddress);

			if (_CurrentInstrSet() == EInstrSet.ARM)
				LR = PC - 4;
			else LR = (uint)((PC & ~1) | 1);

			_SelectInstrSet(targetInstrSet);
			_BranchWritePC(targetAddress);
			return 1;
		}

		//A8.6.24 BLX (register)
		uint ExecuteCore_BLX_register(Encoding encoding, uint m)
		{
			if (disassemble)
				return DISNEW("BLX<c>", "<rm!>", m);

			//NEXT_INSTR_ADDR not to be confused with other variables in our core emulator
			uint target = r[m];
			if (_CurrentInstrSet() == EInstrSet.ARM)
			{
				uint NEXT_INSTR_ADDR = PC - 4;
				LR = NEXT_INSTR_ADDR;
			}
			else
			{
				uint NEXT_INSTR_ADDR = PC - 2;
				LR = (NEXT_INSTR_ADDR & 0xFFFFFFFE) | 1;
			}
			_BXWritePC(target);

			return 1;
		}

		//A8.6.35 CMP (immediate)
		uint ExecuteCore_CMP_immediate(Encoding encoding, uint n, uint imm32)
		{
			if (disassemble)
				return DISNEW("CMP<c>", "<Rn!>, #<const>", n, imm32);

			_AddWithCarry32(r[n], ~imm32, 1);
			APSR.N = _.BIT31(alu_result_32);
			APSR.Z = _IsZeroBit(alu_result_32);
			APSR.C = alu_carry_out;
			APSR.V = alu_overflow;

			return 0;
		}

		//A8.6.36 CMP (register)
		uint ExecuteCore_CMP_register(Encoding encoding, uint n, uint m, SRType shift_t, int shift_n)
		{
			Debug.Assert(!(EncodingT(encoding) && n == 15)); //?? not in pseudocode but sort of suggested by addenda

			if (disassemble)
				return DISNEW("CMP<c>", "<rn!>, <rm!><{, shift}>", n, m, shift_t, shift_n);

			uint shifted = _Shift(r[m], shift_t, shift_n, APSR.C);
			_AddWithCarry32(r[n], ~shifted, 1);
			APSR.N = _.BIT31(alu_result_32);
			APSR.Z = _IsZeroBit(alu_result_32);
			APSR.C = alu_carry_out;
			APSR.V = alu_overflow;

			return 1;
		}

		//A8.6.39 CPY
		uint ExecuteCore_CPY()
		{
			//(this space intentionally blank. instruction is obsoleted)
			return 0;
		}

		//A8.6.45 EOR (register)
		uint ExecuteCore_EOR_register(Encoding encoding, uint m, uint d, uint n, bool setflags, SRType shift_t, int shift_n)
		{
			if (disassemble)
				return DISNEW("EOR<s?><c>", "<{rd!~rn, }><rn!>, <rm!><{, shift}>", setflags, d, n, n, m, shift_t, shift_n);

			uint shifted;
			Bit carry;
			_Shift_C(r[m], shift_t, shift_n, APSR.C, out shifted, out carry);
			uint result = r[n] ^ shifted;
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = _.BIT31(result);
					APSR.Z = _IsZeroBit(result);
					APSR.C = carry;
					//APSR.V unchanged
				}
			}

			return 1;
		}

		//A8.6.53 LDM/LDMIA/LDMFD
		uint ExecuteCore_LDM_LDMIA_LDMFD(Encoding encoding, bool wback, uint n, uint registers)
		{
			//many ways to write this...
			//I chose LDMIA to match rvct
			if (disassemble)
				return DISNEW("LDMIA<c>", "<Rn!><{wback!}>, <registers>", n, wback, registers);

			_NullCheckIfThumbEE(n);
			uint bitcount = _.BitCount(registers);
			uint address = r[n];
			for (int i = 0; i <= 14; i++)
				if (_.BITN(i, registers) == 1)
				{
					r[i] = MemA_Read32(address);
					address += 4;
				}
			if (_.BIT15(registers) == 1)
				_LoadWritePC(MemA_Read32(address));
			if (wback && _.BITN((int)n, registers) == 0) r[n] = r[n] + 4 * bitcount;
			if (wback && _.BITN((int)n, registers) == 1) r[n] = (uint)_UNKNOWN(32, r[n] + 4 * bitcount);

			return 0;
		}

		//A8.6.57 LDR (immediate, thumb)
		uint ExecuteCore_LDR_immediate_thumb(Encoding encoding, uint t, uint imm32, uint n, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("LDR", t, imm32, n, index, add, wback);

			_NullCheckIfThumbEE(n);
			uint offset_addr, address;
			if (add) offset_addr = r[n] + imm32; else offset_addr = r[n] - imm32;
			if (index) address = offset_addr; else address = r[n];
			uint data = MemU_Read32(address);
			if (wback) r[n] = offset_addr;
			if (t == 15)
			{
				if ((address & 3) == 0) _LoadWritePC(data); else _UNPREDICTABLE();
			}
			else if (_UnalignedSupport() || (address & 3) == 0)
				r[t] = data;
			else r[t] = (uint)_UNKNOWN(32, data);

			return 0;
		}

		//A8.6.58 LDR (immediate, arm)
		uint ExecuteCore_LDR_immediate_arm(Encoding encoding, uint t, uint imm32, uint n, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("LDR", t, imm32, n, index, add, wback);

			uint offset_addr, address;
			if (add) offset_addr = r[n] + imm32;
			else offset_addr = r[n] - imm32;
			if (index) address = offset_addr;
			else address = r[n];
			uint data = MemU_Read32(address);
			if (wback) r[n] = offset_addr;
			if (t == 15)
			{
				if ((address & 3) == _.b00) _LoadWritePC(data); else unpredictable = true;
			}
			else if (_UnalignedSupport() || ((address & 3) == _.b00))
				r[t] = data;
			else //can only apply before ARMv7
				r[t] = _ROR(data, 8 * ((int)address & 3));

			return 0;
		}

		//A8.6.59 LDR (literal)
		uint ExecuteCore_LDR_literal(Encoding encoding, uint t, uint imm32, bool add)
		{
			uint @base = _Align(PC, 4);
			uint address = add ? (@base + imm32) : (@base - imm32);

			if (disassemble)
				return DISNEW("LDR<c>", "<Rt>,[PC,#<optaddsub><imm>] ; @<label>=<%08X>", t, add, imm32, address, bus.Read32(AT.PEEK, address));

			uint data = _MemU(address, 4);

			if (t == 15)
				if ((address & 3) == 0) _LoadWritePC(data); else _UNPREDICTABLE();
			else if (_UnalignedSupport() || ((address & 3) == 0))
				r[t] = data;
			else
				//can only apply before armv7
				if (_CurrentInstrSet() == EInstrSet.ARM)
					r[t] = _ROR(data, (int)(address & 3));
				else
				{ r[t] = (uint)_UNKNOWN(32, data); }

			return 1;
		}

		//A8.6.60 LDR (register)
		uint ExecuteCore_LDR_register(Encoding encoding, uint t, uint n, uint m, SRType shift_t, int shift_n, bool index, bool add, bool wback)
		{
			if(disassemble)
				return Disassemble_LDR_STR_register("LDR<c>", t, n, m, shift_t, shift_n, index, add, wback);

			_NullCheckIfThumbEE(n);
			uint offset = _Shift(r[m], shift_t, shift_n, APSR.C);
			uint offset_addr = add ? (r[n] + offset) : (r[n] - offset);
			uint address = index ? offset_addr : r[n];
			uint data = MemU_Read32(address);
			if (wback) r[n] = offset_addr;
			if (t == 15)
			{
				if ((address & 3) == _.b00) _LoadWritePC(data);
				else _FlagUnpredictable();
			}
			else if (_UnalignedSupport() || (address & 3) == _.b00)
				r[t] = data;
			else //can only apply before ARMv7
				if (_CurrentInstrSet() == EInstrSet.ARM)
					r[t] = _ROR(data, 8 * ((int)address & 3));
				else
					r[t] = (uint)_UNKNOWN(32, _ROR(data, 8 * ((int)address & 3)));

			return 1;
		}

		//A8.6.61 LDRB (immediate, thumb)
		uint ExecuteCore_LDRB_immediate_thumb(Encoding encoding, uint t, uint n, uint imm32, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("LDRB<c>", t, imm32, n, index, add, wback);

			uint offset_addr = add ? (r[n] + imm32) : (r[n] - imm32);
			uint address = index ? offset_addr : r[n];
			r[t] = _ZeroExtend_32(MemU_Read08(address));
			if (wback) r[n] = offset_addr;

			return 1;
		}

		//A8.6.64 LDRB (register)
		uint ExecuteCore_LDRB_register(Encoding encoding, uint t, uint n, uint m, SRType shift_t, int shift_n, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_register("LDRB<c>", t, n, m, shift_t, shift_n, index, add, wback);

			uint offset = _Shift(r[m], shift_t, shift_n, APSR.C);
			uint offset_addr = add ? (r[n] + offset) : (r[n] - offset);
			uint address = index ? offset_addr : r[n];
			r[t] = _ZeroExtend_32(_MemU(address, 1));
			if (wback) r[n] = offset_addr;

			return 1;
		}

		//A8.6.69 LDREX
		uint ExecuteCore_LDREX(Encoding encoding, uint n, uint t, uint imm32)
		{
			if (disassemble)
				return DISNEW("LDREX<c>", "<rt!>, [<rn!><{ ,#imm}>]", t, n, imm32);
			_NullCheckIfThumbEE(n);
			uint address = r[n] + imm32;
			_SetExclusiveMonitors(address, 4);
			r[t] = MemA_Read32(address);

			return 1;
		}

		//A8.6.73 LDRH (immediate, thumb)
		uint ExecuteCore_LDRH_immediate_thumb(Encoding encoding, uint t, uint n, uint imm32, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("LDRH", t, imm32, n, index, add, wback);
			_NullCheckIfThumbEE(n);
			uint offset_addr = add ? (r[n] + imm32) : (r[n] - imm32);
			uint address = index ? offset_addr : r[n];
			ushort data = MemU_Read16(address);
			if (wback) r[n] = offset_addr;
			if (_UnalignedSupport() || _.BIT0(address) == 0)
				r[t] = _ZeroExtend_32((uint)data);
			else r[t] = (uint)_UNKNOWN(32, (ulong)_ZeroExtend_32((uint)data));

			return 1;
		}

		//A8.6.88 LSL (immediate)
		uint ExecuteCore_LSL_immediate(Encoding encoding, uint d, uint m, bool setflags, int shift_n)
		{
			//sometimes (in arm) this shows up as MOV. i sort of like that syntax better...
			if (disassemble)
				return DISNEW("LSL<s?><c>", "<{rd!~rm, }><rm!>, #<imm5>", setflags, d, m, m, shift_n);

			uint result;
			Bit carry;
			_Shift_C(r[m], SRType.LSL, shift_n, APSR.C, out result, out carry);
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = _.BIT31(result);
					APSR.Z = _IsZeroBit(result);
					APSR.C = carry;
					//APSR.V unchanged
				}
			}

			return 0;
		}

		//A8.6.90 LSR (immediate)
		uint ExecuteCore_LSR_immediate(Encoding encoding, uint d, uint m, bool setflags, int shift_n)
		{
			if (disassemble)
				return DISNEW("LSR<s?><c>", "<{rd!~rm, }><rm!>, #<imm5>", setflags, d, m, m, shift_n);

			uint result;
			Bit carry;
			_Shift_C(r[m], SRType.LSR, shift_n, APSR.C, out result, out carry);
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = _.BIT31(result);
					APSR.Z = _IsZeroBit(result);
					APSR.C = carry;
					//APSR.V unchanged
				}
			}

			return 1;
		}

		//A8.6.97 MOV (register)
		uint ExecuteCore_MOV_register(Encoding encoding, uint d, uint m, bool setflags)
		{
			//there are many caveats and addenda in the manual. check it carefully.

			if (disassemble)
				return DISNEW("MOV<s?><c?>", "<Rd!>,<Rm!>", setflags, EncodingA(encoding), d, m);

			uint result = r[m];
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = _.BIT31(result);
					APSR.Z = _IsZeroBit(result);
					//C and V are unchanged
				}
			}

			return 1;
		}

		//A8.6.96 MOV (immedate)
		uint ExecuteCore_MOV_immediate(Encoding encoding, uint d, bool setflags, uint imm32, uint carry)
		{
			if (disassemble)
				return DIS("MOV/s0//c/", "/r1/,/const2/", setflags, d, imm32);
			uint result = imm32;
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = _.BIT31(result);
					APSR.Z = (result == 0) ? 1U : 0U;
					APSR.C = carry;
				}
			}
			return 1;
		}

		//A8.6.100 MRC, MRC2
		uint ExecuteCore_MRC_MRC2(Encoding encoding, uint cp, uint opc1, uint t, uint crn, uint crm, uint opc2)
		{
			if (disassemble)
				return DISNEW("MRC<c>" + ((encoding == Encoding.A2 || encoding == Encoding.T2) ? "2" : ""), "<coproc>, <opc1>, <coproc_rt!>, <crn>, <crm>, <opc2><{ ;cp_comment}>", cp, opc1, t, crn, crm, opc2, "TBD");

			uint value = sys.coprocessors.MRC(cp, opc1, t, crn, crm, opc2);
			if (t != 15)
				r[t] = value;
			else
			{
				APSR.N = _.BIT31(value);
				APSR.Z = _.BIT30(value);
				APSR.C = _.BIT29(value);
				APSR.V = _.BIT28(value);
			}
			return 1;
		}

		//A8.6.109 NEG [BLANK]
		uint ExecuteCore_NEG()
		{
			//(this space intentionally blank. NEG was removed from the spec and replaced with RSB (immediate)
			return 0;
		}

		//A8.6.113 ORR (immediate)
		uint ExecuteCore_ORR_immediate(Encoding encoding, uint n, uint d, bool setflags, uint imm32, Bit carry)
		{
			if (disassemble)
				return DISNEW("ORR<s?><c>", "<{rd!~rn, }><rn!>, #<const>", setflags, d, n, n, imm32);

			uint result = r[n] | imm32;
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = result;
					APSR.Z = _IsZeroBit(result);
					APSR.C = carry;
					//APSR.V unchanged
				}
			}

			return 1;
		}

		//A8.6.114 ORR (reg)
		uint ExecuteCore_ORR_register(Encoding encoding, uint m, uint d, uint n, bool setflags, SRType shift_t, int shift_n)
		{
			if (disassemble)
				return DISNEW("ORR<s?><c>", "<Rd!>, <Rn!>, <Rm!><{, shift}>", setflags, d, n, m, shift_t, shift_n);

			uint shifted;
			Bit carry;
			_Shift_C(r[m], shift_t, shift_n, APSR.C, out shifted, out carry);
			uint result = r[n] | shifted;
			if (d == 15)
				_ALUWritePC(result);
			else
			{
				r[d] = result;
				if (setflags)
				{
					APSR.N = _.BIT31(result);
					APSR.Z = _IsZeroBit(result);
					APSR.C = carry;
					//APSR.V unchanged
				}
			}

			return 1;
		}

		//A8.6.122 POP
		uint ExecuteCore_POP(Encoding encoding, uint registers, bool UnalignedAllowed)
		{
			if (disassemble)
				return DISNEW("pop<c>", "<registers> ; maybe LDMIA sp!,{..}", registers);

			//in armv7, if SP is in the list then the instruction is UNKNOWN
			uint address = SP;

			uint bitcount = 0;
			for (int i = 0; i <= 14; i++)
			{
				if (_.BITN(i, registers) == 1)
				{
					bitcount++;
					r[i] = UnalignedAllowed ? _MemU(address, 4) : _MemA(address, 4);
					address = address + 4;
				}
			}
			if (_.BIT15(registers) == 1)
			{
				bitcount++;
				if (UnalignedAllowed)
					_LoadWritePC(_MemU(address, 4));
				else
					_LoadWritePC(_MemA(address, 4));
			}
			if (_.BIT13(registers) == 0) SP = SP + 4 * bitcount;
			else SP = (uint)_UNKNOWN(32, SP + 4 * bitcount);

			return 1;
		}

		//A8.6.123 PUSH
		uint ExecuteCore_PUSH(Encoding encoding, uint registers, bool UnalignedAllowed)
		{
			uint bitcount = _.BitCount(registers);

			if (disassemble)
				if (bitcount == 1)
					return DISNEW("push<c>", "<registers> ; maybe STR rx,[SP,#-0x4]", registers);
				else
					return DISNEW("push<c>", "<registers> ; maybe STMDB sp!,{..}", registers);

			_NullCheckIfThumbEE(13);
			uint address = SP - 4 * bitcount;
			for (int i = 0; i <= 14; i++)
			{
				if (_.BITN(i, registers) == 1)
				{
					//TODO - work on this
					if (i == 13 && i != _LowestSetBit(registers, 16))
						MemA_Write32(address, (uint)_UNKNOWN(32, r[i]));
					else if (UnalignedAllowed)
						MemU_Write32(address, r[i]);
					else
						MemA_Write32(address, r[i]);
					address += 4;
				}
			}
			if (_.BIT15(registers) == 1)
			{
				if (UnalignedAllowed)
					MemU_Write32(address, _PCStoreValue());
				else
					MemA_Write32(address, _PCStoreValue());
			}

			SP = SP - 4 * bitcount;

			return 1;
		}

		//A8.6.142 RSB (immediate)
		uint ExecuteCore_RSB_immediate(Encoding encoding, uint d, uint n, bool setflags, uint imm32)
		{
			if (disassemble)
				return DISNEW("RSB<s?><c>", "<{rd!~rn, }><rn!>, #<const>" + (imm32 == 0 ? " ;maybe NEG" : ""), setflags, d, n, n, imm32);
			_AddWithCarry32(~r[n], imm32, 1);
			if (d == 15)
				_ALUWritePC(alu_result_32);
			else
			{
				r[d] = alu_result_32;
				APSR.N = _.BIT31(alu_result_32);
				APSR.Z = _IsZeroBit(alu_result_32);
				APSR.C = alu_carry_out;
				APSR.V = alu_overflow;
			}
			return 1;
		}

		//A8.6.189 STM/STMIA/STMEA
		uint ExecuteCore_STM_STMIA_STMEA(Encoding encoding, bool wback, uint n, uint registers)
		{
			//we could write this as STM<c>, STM<c>IA or STMIA<c> or STMEA<c> or STM<c>EA but how to choose?? who cares. christ.
			//I chose STMIA to match rvct
			if (disassemble)
				return DISNEW("STMIA<c>", "<Rn!><{wback!}>, <registers>", n, wback, registers);

			uint bitcount = _.BitCount(registers);
			uint address = r[n];
			for (int i = 0; i <= 14; i++)
			{
				if (_.BITN(i, registers) == 1)
				{
					if (i == n && wback && i != _LowestSetBit(registers, 16))
						MemA_Write32(address, (uint)_UNKNOWN(32, r[i])); //"only possible for encodings T1 and A1"
					else MemA_Write32(address, r[i]);
					address += 4;
				}
			}

			if (_.BIT15(registers) == 1) //"only possible for encoding A1"
				MemA_Write32(address, _PCStoreValue());
			if (wback) r[n] = r[n] + 4 * bitcount;

			return 1;
		}

		//A8.6.191 STMDB/STMFD
		uint ExecuteCore_STMDB_STMFD(Encoding encoding, bool wback, uint n, uint registers)
		{
			if (disassemble)
				return DISNEW("STMDB<c>", "<Rn!><{wback!}>, <registers>", n, wback, registers);

			uint bitcount = _.BitCount(registers);
			uint address = r[n] - 4 * bitcount;
			for (int i = 0; i <= 14; i++)
			{
				if (_.BITN(i, registers) == 1)
				{
					if (i == n && wback && i != _LowestSetBit(registers, 16))
						MemA_Write32(address, (uint)_UNKNOWN(32, r[i])); //"only possible for encoding A1"
					else
						MemA_Write32(address, r[i]);
					address += 4;
				}
			}

			if (_.BIT15(registers) == 1) //"only possible for encoding A1"
				MemA_Write32(address, _PCStoreValue());

			if (wback) r[n] = r[n] - 4 * bitcount;

			return 0;
		}

		//A8.6.193 STR (immediate, thumb)
		uint ExecuteCore_STR_immediate_thumb(Encoding encoding, uint t, uint imm32, uint n, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("STR", t, imm32, n, index, add, wback);

			uint offset_addr;
			if (add) offset_addr = r[n] + imm32; else offset_addr = r[n] - imm32;
			uint address;
			if (index) address = offset_addr; else address = r[n];
			if (_UnalignedSupport() || ((address & 3) == 0))
				MemU_Write32(address, r[t]);
			else
				UnalignedAccess(address);
			if (wback) r[n] = offset_addr;
			return 1;
		}

		//A8.6.194 STR (immediate, arm)
		uint ExecuteCore_STR_immediate_arm(Encoding encoding, bool P, bool U, bool W, uint n, uint t, uint imm32, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("STR", t, imm32, n, index, add, wback);

			uint offset_addr = add ? (r[n] + imm32) : (r[n] - imm32);
			uint address = index ? offset_addr : r[n];
			MemU_Write32(address, t == 15 ? _PCStoreValue() : r[t]);
			if (wback) r[n] = offset_addr;

			return 1;
		}

		//A8.6.196 STRB (immediate, thumb)
		uint ExecuteCore_STRB_immediate_thumb(Encoding encoding, uint t, uint n, uint imm32, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("STRB", t, imm32, n, index, add, wback);

			uint offset_addr = add ? (r[n] + imm32) : (r[n] - imm32);
			uint address = index ? offset_addr : r[n];
			MemU_Write08(address, r[t] & 0xFF);
			if (wback) r[n] = offset_addr;

			return 1;
		}

		//A8.6.198 STRB (register)
		uint ExecuteCore_STRB_register(Encoding encoding, uint t, uint n, uint m, SRType shift_t, int shift_n, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_register("STRB<c>", t, n, m, shift_t, shift_n, index, add, wback);

			uint offset = _Shift(r[m], shift_t, shift_n, APSR.C);
			uint offset_addr = add ? (r[n] + offset) : (r[n] - offset);
			uint address = index ? offset_addr : r[n];
			MemU_Write08(address, r[t] & 0xFF);
			if (wback) r[n] = offset_addr;
			return 1;
		}

		//A8.6.202 STREX
		uint ExecuteCore_STREX(Encoding encoding, uint d, uint n, uint t, uint imm32)
		{
			if (disassemble)
				return DISNEW("STREX<c>", "<rd!>, <rt!>, [<rn!><{ ,#imm}>]", d, t, n, imm32);
			_NullCheckIfThumbEE(n);
			uint address = r[n] + imm32;
			if (_ExclusiveMonitorsPass(address, 4))
			{
				MemA_Write32(address, r[t]);
				r[d] = 0;
			}
			else r[d] = 1;
			return 1;
		}

		//A8.6.206 STRH (immediate, thumb)
		uint ExecuteCore_STRH_immediate_thumb(Encoding encoding, uint t, uint n, uint imm32, bool index, bool add, bool wback)
		{
			if (disassemble)
				return Disassemble_LDR_STR_immediate("STRH", t, imm32, n, index, add, wback);
			_NullCheckIfThumbEE(n);
			uint offset_addr = add ? (r[n] + imm32) : (r[n] - imm32);
			uint address = index ? offset_addr : r[n];
			if (_UnalignedSupport() || _.BIT0(address) == 0)
				MemU_Write16(address, r[t] & 0xFFFF);
			else
				MemU_Write16(address, (uint)_UNKNOWN(16, r[t] & 0xFFFF));
			if (wback) r[n] = offset_addr;

			return 1;
		}

		//A8.6.211 SUB (immediate, thumb)
		uint ExecuteCore_SUB_immediate_thumb(Encoding encoding, uint n, uint d, bool setflags, uint imm32)
		{
			if (disassemble)
				return DISNEW("SUB<s?><c>", "<{rd!~rn, }><rn!>, #<const>", setflags, d, n, n, imm32);

			_AddWithCarry32(r[n], ~imm32, 1);
			r[d] = alu_result_32;
			if (setflags)
			{
				APSR.N = _.BIT31(alu_result_32);
				APSR.Z = _IsZeroBit(alu_result_32);
				APSR.C = alu_carry_out;
				APSR.V = alu_overflow;
			}

			return 1;
		}

		//A8.6.212 SUB (immediate, arm)
		uint ExecuteCore_SUB_immediate_arm(Encoding encoding, bool setflags, uint n, uint d, uint imm32)
		{
			if (disassemble)
				return DISNEW("SUB<s?><c>", "<{rd!~rn, }><rn!>, #<const>", setflags, d, n, n, imm32);

			_AddWithCarry32(r[n], ~imm32, 1);
			if (d == 15)
				_ALUWritePC(alu_result_32); //setflags is always FALSE here
			else
			{
				r[d] = alu_result_32;
				if (setflags)
				{
					APSR.N = _.BIT31(alu_result_32);
					APSR.Z = _IsZeroBit(alu_result_32);
					APSR.C = alu_carry_out;
					APSR.V = alu_overflow;
				}
			}

			return 1;
		}

		//A8.6.213 SUB (register)
		uint ExecuteCore_SUB_register(Encoding encoding, bool setflags, uint m, uint n, uint d, SRType shift_t, int shift_n)
		{
			if (disassemble)
				return DISNEW("SUB<s?><c>", "<{rd!~rn, }><rn!>, <rm!><{, shift}>", setflags, d, n, n, m, shift_t, shift_n);

			uint shifted = _Shift(r[m], shift_t, shift_n, APSR.C);
			_AddWithCarry32(r[n], ~shifted, 1);
			if (d == 15)
				_ALUWritePC(alu_result_32);
			else
			{
				r[d] = alu_result_32;
				if (setflags)
				{
					APSR.N = _.BIT31(alu_result_32);
					APSR.Z = _IsZeroBit(alu_result_32);
					APSR.C = alu_carry_out;
					APSR.V = alu_overflow;
				}
			}

			return 1;
		}

		//A8.6.215 SUB (SP minus immediate)
		uint ExecuteCore_SUB_SP_minus_immediate(Encoding encoding, uint d, bool setflags, uint imm32)
		{
			uint n = 13;
			if (disassemble)
				return DISNEW("SUB<s?><c>", "<{rd!~rn, }><rn!>, #<const>", setflags, d, n, n, imm32);

			_AddWithCarry32(SP, ~imm32, 1);
			if (d == 15)
				_ALUWritePC(alu_result_32);
			else
			{
				r[d] = alu_result_32;
				if (setflags)
				{
					APSR.N = _.BIT31(alu_result_32);
					APSR.Z = _IsZeroBit(alu_result_32);
					APSR.C = alu_carry_out;
					APSR.V = alu_overflow;
				}
			}

			return 1;
		}

		//A8.6.218 SVC (previously SWI)
		uint ExecuteCore_SVC(Encoding encoding, uint imm32)
		{
			if (disassemble)
				return DISNEW("XXX", "<svc>", imm32);
			return sys.svc(imm32);
		}

		//A8.6.263 UXTB
		uint ExecuteCore_UXTB(Encoding encoding, uint d, uint m, uint rotation)
		{
			if (disassemble)
				return DISNEW("UXTB<c>", "<rd!>, <rm!><{, rotation}>", d, m, rotation);

			rotation *= 8;
			uint rotated = _ROR(r[m], (int)rotation);
			r[d] = _ZeroExtend_32(rotated & 0xFF);

			return 1;
		}

		//A8.6.265 UXTH
		uint ExecuteCore_UXTH(Encoding encoding, uint d, uint m, uint rotation)
		{
			if (disassemble)
				return DISNEW("UXTH<c>", "<rd!>, <rm!><{, rotation}>", d, m, rotation);

			rotation *= 8;
			uint rotated = _ROR(r[m], (int)rotation);
			r[d] = _ZeroExtend_32(rotated & 0xFFFF);

			return 1;
		}

		//A8.6.320 VLDR
		uint ExecuteCore_VLDR(Encoding encoding, bool single_reg, bool add, uint d, uint n, uint imm32)
		{
			throw new NotImplementedException("TODO");
		}

		//A8.6.336 VMSR
		uint ExecuteCore_VMSR(uint t)
		{
			_CheckVFPEnabled(true);
			_SerializeVFP();
			_VFPExcBarrier();
			FPSCR = r[t];
			return 1;
		}

		//A8.6.355 VPUSH
		uint ExecuteCore_VPUSH(Encoding encoding, bool single_regs, uint d, uint regs, uint imm32)
		{
			if (disassemble)
				return DISNEW("VPUSH<c><{.size}>", "<list>", single_regs ? 32 : 64, single_regs, d, regs);

			_CheckVFPEnabled(true);
			_NullCheckIfThumbEE(13);
			
			uint address = SP - imm32;
			SP = SP - imm32;
			if (single_regs)
				for (uint r = 0; r <= regs-1; r++)
				{
					MemA_WriteSingle(address, S[d+r]); 
					address += 4;
				}
			else
				for (uint r = 0; r <= regs - 1; r++)
				{
					MemA_WriteDouble(address, D[d+r]);
					address += 8;
				}

			return 1;
		}

	}

	//unnecessary opcodes: BKPT, BXJ, CBNZ, CHKA, NOP, CPY, DBG, ENTERX, HB, IT, LDRBT, LDRHT, LDRSBT, LDRSHT, LEAVEX, RBIT, SBFX, SDIV, TBB
	//likely unnecessary: CDP, SETEND, SEV
	//etc..

}