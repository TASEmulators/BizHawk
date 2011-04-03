using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.ARM
{

	partial class ARM
	{
		uint ExecuteArm()
		{
			_currentCondVal = CONDITION(instruction);
			if (_currentCondVal == 0xF) return ExecuteArm_Unconditional();
			bool pass = _ConditionPassed() || disassemble;
			if (pass)
			{
				uint op1 = (instruction & 0x0E000000) >> 25;
				uint op = (instruction & 0x10) >> 4;
				switch (op1)
				{
					case _.b000:
					case _.b001:
						return ExecuteArm_DataProcessing();
					case _.b010:
						return ExecuteArm_LoadStore();
					case _.b011:
						if (op == 0) return ExecuteArm_LoadStore();
						else return ExecuteArm_Media();
					case _.b100:
					case _.b101:
						return ExecuteArm_BranchAndTransfer();
					case _.b110:
					case _.b111:
						return ExecuteArm_SVCAndCP();
					default:
						throw new InvalidOperationException();
				}
			}
			else
				return 1;
		}


		Decoder decoder_ExecuteArm_LoadStore = new Decoder();
		uint ExecuteArm_LoadStore()
		{
			//A5.3
			//A5-19

			decoder_ExecuteArm_LoadStore.Ensure(() => decoder_ExecuteArm_LoadStore
				.d("A", 1).d("op1", 5).d("B", 1).d("Rn", 4)
				.r("A==0 && op1 == #xx0x0 && op1 != #0x010", () => Execute_STR_immediate_A1())
				.r("A==1 && op1 == #xx0x0 && op1 != #0x010 && B==0", () => Execute_Unhandled("STR (register) on page A8-386"))
				.r("A==0 && op1 == #0x010", () => Execute_Unhandled("STRT on page A8-416"))
				.r("A==1 && op1 == #0x010 && B==0", () => Execute_Unhandled("STRT on page A8-416"))
				.r("A==0 && op1 == #xx0x1 && op1 != #0x011 && Rn != #1111", () => Execute_LDR_immediate_arm_A1())
				.r("A==0 && op1 == #xx0x1 && op1 != #0x011 && Rn == #1111", () => ExecuteArm_LDR_literal_A1())
				.r("A==1 && op1 == #xx0x1 && A != #0x011 && B==0", () => Execute_Unhandled("LDR (register) on page A8-124"))
				.r("A==0 && op1 == #0x011", () => Execute_Unhandled("LDRT on page A8-176"))
				.r("A==1 && op1 == #0x011 && B==0", () => Execute_Unhandled("LDRT on page A8-176"))
				.r("A==0 && op1 == #xx1x0 && op1 != #0x110", () => Execute_Unhandled("STRB (immediate, ARM) on page A8-390"))
				.r("A==1 && op1 == #xx1x0 && op1 != #0x110 && B==0", () => Execute_Unhandled("STRB (register) on page A8-392"))
				.r("A==0 && op1 == #0x110", () => Execute_Unhandled("STRBT on page A8-394"))
				.r("A==1 && op1 == #0x110 && B==0", () => Execute_Unhandled("STRBT on page A8-394"))
				.r("A==0 && op1 == #xx1x1 && op1 != #0x111 && Rn != #1111", () => Execute_Unhandled("LDRB (immediate, ARM) on page A8-128"))
				.r("A==0 && op1 == #xx1x1 && op1 != #0x111 && Rn == #1111", () => Execute_Unhandled("LDRB (literal) on page A8-130"))
				.r("A==1 && op1 == #xx1x1 && op1 != #0x111 && B==0", () => Execute_Unhandled("LDRB (register) on page A8-132"))
				.r("A==0 && op1 == #0x111", () => Execute_Unhandled("LDRBT on page A8-134"))
				.r("A==1 && op1 == #0x111 && B==0", () => Execute_Unhandled("LDRBT on page A8-134"))
				);

			uint A = _.BIT25(instruction);
			uint op1 = (instruction >> 20) & 0x1F;
			uint B = _.BIT4(instruction);
			uint Rn = Reg16(16);
			decoder_ExecuteArm_LoadStore.Evaluate(A, op1, B, Rn);

			return cycles;
		}

		uint Execute_LDR_immediate_arm_A1()
		{
			//A8.6.58 LDR (immediate, ARM)
			Bit P = _.BIT24(instruction);
			Bit U = _.BIT23(instruction);
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint t = Reg16(12);
			uint imm12 = instruction & 0xFFF;
			if (n == _.b1111) throw new NotImplementedException("see LDR (literal");
			if (P == 0 && W == 1) throw new NotImplementedException("see LDRT");
			if (n == _.b1101 && P == 0 && U == 1 && W == 0 && imm12 == _.b000000000100)
				return Execute_POP_A2();
			uint imm32 = _ZeroExtend_32(imm12);
			bool index = (P == 1);
			bool add = (U == 1);
			bool wback = (P == 0) || (W == 1);
			if (wback && n == t) unpredictable = true;

			return ExecuteCore_LDR_immediate_arm(Encoding.A1, t, imm32, n, index, add, wback);
		}

		uint Execute_POP_A2()
		{
			//A8.6.122 POP
			uint t = Reg16(12);
			uint regs = (uint)(1 << (int)t);
			if (t == 13) unpredictable = true;
			const bool UnalignedAllowed = true;
			return ExecuteCore_POP(Encoding.A2, regs, UnalignedAllowed);
		}

		uint Execute_STR_immediate_A1()
		{
			//A8.6.194
			Bit P = _.BIT24(instruction);
			Bit U = _.BIT23(instruction);
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint t = Reg16(12);
			uint imm32 = instruction & 0xFFF;
			if (P == 0 && W == 1) throw new NotImplementedException("see STRT");
			if (n == _.b1101 && P == 1 && U == 0 && W == 1 && imm32 == _.b000000000100)
				return Execute_PUSH_A2();
			bool index = (P == 1);
			bool add = (U == 1);
			bool wback = (P == 0) || (W == 1);
			if (wback && n == 16 || n == t) unpredictable = true;

			return ExecuteCore_STR_immediate_arm(Encoding.A1, P, U, W, n, t, imm32, index, add, wback);
		}

		uint Execute_PUSH_A2()
		{
			//A8.6.123
			uint t = Reg8(12);
			bool UnalignedAllowed = true;
			uint registers = (uint)(1 << (int)t);
			if (t == 13) _FlagUnpredictable();
			return ExecuteCore_PUSH(Encoding.A2, registers, UnalignedAllowed);
		}

		uint ExecuteArm_LDR_literal_A1()
		{
			//A8.6.59
			//A8-122
			uint t = Reg16(12);
			uint imm32 = instruction & 0xFFF;
			uint U = _.BIT23(instruction);
			bool add = (U == 1);

			return ExecuteCore_LDR_literal(Encoding.A1, t, imm32, add);
		}


		uint ExecuteArm_DataProcessing()
		{
			uint op = _.BIT25(instruction);
			uint op1 = (instruction >> 20) & 0x1F;
			uint op2 = (instruction >> 4) & 0xF;
			switch (op)
			{
				case 0:
					if (!CHK(op1, _.b11001, _.b10000))
						if (CHK(op2, _.b0001, _.b0000)) return ExecuteArm_DataProcessing_Register();
						else if (CHK(op2, _.b1001, _.b0001)) return Execute_Unhandled("data-processing (register-shifted register) on page A5-7");
					if (CHK(op1, _.b11001, _.b10000))
						if (CHK(op2, _.b1000, _.b0000)) return ExecuteArm_DataProcessing_Misc();
						else if (CHK(op2, _.b1001, _.b1000)) return Execute_Unhandled("halfword multiply and multiply-accumulate on page A5-13");
					if (CHK(op1, _.b10000, _.b00000) && op2 == _.b1001) return Execute_Unhandled("multiply and multiply-accumulate on page A5-12");
					if (CHK(op1, _.b10000, _.b10000) && op2 == _.b1001) return ExecuteArm_SynchronizationPrimitives();
					if (!CHK(op1, _.b10010, _.b00010))
						if (op2 == _.b1011) return Execute_Unhandled("extra load/store instructions on page A5-14");
						else if (CHK(op2, _.b1011, _.b1101)) return Execute_Unhandled("extra load/store instructions on page A5-14");
					if (CHK(op1, _.b10010, _.b00010))
						if (op2 == _.b1011) return Execute_Unhandled("extra load/store instructions (unprivileged) on page A5-15");
						else if (CHK(op2, _.b1011, _.b1101)) return Execute_Unhandled("extra load/store instructions (unprivileged) on page A5-15");
					throw new InvalidOperationException("unexpected decoder fail");
				case 1:
					if (!CHK(op1, _.b11001, _.b10000))
						return ExecuteArm_DataProcessing_Immediate();
					if (op1 == _.b10000) return Execute_Unhandled("16-bit immediate load (MOV (immediate on page A8-193) //v6T2");
					if (op1 == _.b10100) return Execute_Unhandled("high halfword 16-bit immediate load (MOVT on page A8-200) //v6T2");
					if (CHK(op1, _.b11011, _.b10010)) return Execute_Unhandled("MSR (immediate), and hints on page A5-17");
					throw new InvalidOperationException("unexpected decoder fail");

				default:
					throw new InvalidOperationException("totally impossible decoder fail");
			}
		}

		uint ExecuteArm_SynchronizationPrimitives()
		{
			uint op = (instruction >> 20) & 0xF;
			switch (op)
			{
				case _.b0000:
				case _.b0100: return Execute_Unhandled("ExecuteArm_SWP_SWPB();");
				case _.b1000: return Execute_STREX_A1();
				case _.b1001: return Execute_LDREX_A1();
				case _.b1010: return Execute_Unhandled("ExecuteArm_STREXD();");
				case _.b1011: return Execute_Unhandled("ExecuteArm_LDREXD();");
				case _.b1100: return Execute_Unhandled("ExecuteArm_STREXB();");
				case _.b1101: return Execute_Unhandled("ExecuteArm_LDREXB();");
				case _.b1110: return Execute_Unhandled("ExecuteArm_STREXH();");
				case _.b1111: return Execute_Unhandled("ExecuteArm_LDREXH();");
				default: throw new InvalidOperationException("decoder fail");
			}
		}

		uint Execute_STREX_A1()
		{
			//A8.6.202 STREX
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint t = Reg16(0);
			const uint imm32 = 0;
			if (d == 15 || t == 15 || n == 15) unpredictable = true;
			if (d == n || d == t) unpredictable = true;
			return ExecuteCore_STREX(Encoding.A1, d, n, t, imm32);
		}

		uint Execute_LDREX_A1()
		{
			//A8.6.69 LDREX
			uint n = Reg16(16);
			uint t = Reg16(12);
			uint imm32 = 0;
			if (t == 15 || n == 15) unpredictable = true;
			return ExecuteCore_LDREX(Encoding.A1, n, t, imm32);
		}

		uint ExecuteArm_DataProcessing_Register()
		{
			//A5.2.1
			uint op1 = (instruction >> 20) & 0x1F;
			uint op2 = (instruction >> 7) & 0x1F;
			uint op3 = (instruction >> 5) & 3;

			switch (op1)
			{
				case _.b00000:
				case _.b00001: return Execute_Unhandled("arm and reg");
				case _.b00010:
				case _.b00011: return Execute_Unhandled("arm eor reg");
				case _.b00100:
				case _.b00101: return Execute_Unhandled("arm sub reg");
				case _.b00110:
				case _.b00111: return Execute_Unhandled("arm rsb reg");
				case _.b01000:
				case _.b01001: return Execute_ADD_register_A1();
				case _.b01010:
				case _.b01011: return Execute_Unhandled("arm adc reg");
				case _.b01100:
				case _.b01101: return Execute_Unhandled("arm sbc reg");
				case _.b01110:
				case _.b01111: return Execute_Unhandled("arm rsc reg");
				case _.b10001: return Execute_Unhandled("arm tst reg");
				case _.b10011: return Execute_Unhandled("arm teq reg");
				case _.b10101: return Execute_CMP_register_A1();
				case _.b10111: return Execute_Unhandled("arm cmn reg");
				case _.b11000:
				case _.b11001: return Execute_Unhandled("arm orr reg");
				case _.b11010:
				case _.b11011:
					if (op2 == _.b00000 && op3 == _.b00) return Execute_MOV_register_A1();
					if (op2 != _.b00000 && op3 == _.b00) return Execute_LSL_immediate_A1();
					if (op3 == _.b01) return Execute_LSR_immediate_A1();
					if (op3 == _.b10) return Execute_Unhandled("arm asr imm");
					if (op2 == _.b00000 && op3 == _.b11) return Execute_Unhandled("arm rrx");
					if (op2 != _.b00000 && op3 == _.b11) return Execute_Unhandled("arm ror imm");
					throw new InvalidOperationException("decode fail");
				case _.b11100:
				case _.b11101: return Execute_Unhandled("arm bic reg");
				case _.b11110:
				case _.b11111: return Execute_Unhandled("arm mvn reg");
				default:
					throw new InvalidOperationException("decode fail");
			}
		}

		uint Execute_LSR_immediate_A1()
		{
			//A8.6.90
			uint d = Reg16(12);
			uint m = Reg16(0);
			Bit S = _.BIT20(instruction);
			bool setflags = (S == 1);
			uint imm5 = (instruction >> 7) & 0x1F;
			_DecodeImmShift(_.b01, imm5);
			return ExecuteCore_LSR_immediate(Encoding.A1, d, m, setflags, shift_n);
		}

		uint Execute_LSL_immediate_A1()
		{
			//A8.6.87
			Bit S = _.BIT20(instruction);
			uint d = Reg16(12);
			uint imm5 = (instruction >> 7) & 0x1F;
			uint m = Reg16(0);
			Debug.Assert(imm5 != _.b00000); //should have been prevented by decoder
			bool setflags = (S == 1);
			_DecodeImmShift(_.b00, imm5);
			return ExecuteCore_LSL_immediate(Encoding.A1, d, m, setflags, shift_n);
		}

		uint Execute_MOV_register_A1()
		{
			//A8.6.97
			Bit S = _.BIT20(instruction);
			uint d = Reg16(12);
			uint m = Reg16(0);

			if (d == _.b1111 && S == 1) throw new NotSupportedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			return ExecuteCore_MOV_register(Encoding.A1, d, m, setflags);
		}

		uint Execute_ADD_register_A1()
		{
			//A8.6.6 ADD (register)
			uint m = Reg16(0);
			uint type = (instruction >> 4) & 3;
			uint imm5 = (instruction >> 7) & 0x1F;
			uint d = Reg16(12);
			uint n = Reg16(16);
			Bit s = _.BIT20(instruction);
			bool setflags = (s == 1);
			if (d == _.b1111 && s == 1) { throw new InvalidOperationException("see SUBS PC, LR and related instructions;"); }
			if (n == _.b1101) { throw new InvalidOperationException("see ADD (SP plus register);"); }
			_DecodeImmShift(type, imm5);

			return ExecuteCore_ADD_register(Encoding.A1, m, d, n, setflags, shift_t, shift_n);
		}

		uint Execute_CMP_register_A1()
		{
			//A8.6.36
			//A8-82
			uint n = Reg16(16);
			uint imm5 = (instruction >> 7) & 0x1F;
			uint m = Reg16(0);
			uint type = (instruction >> 5) & 3;
			_DecodeImmShift(type, imm5);

			if (disassemble)
				return DISNEW("CMP<c>", "<Rn!>,<Rm!><,shift>", n, m, shift_t, shift_n);

			uint shifted = _Shift(r[m], shift_t, shift_n, APSR.C);
			_AddWithCarry32(r[n], ~shifted, 1);
			APSR.N = _.BIT31(alu_result_32);
			APSR.Z = alu_result_32 == 0;
			APSR.C = alu_carry_out;
			APSR.V = alu_overflow;

			return 0;
		}

		uint ExecuteArm_DataProcessing_Immediate()
		{
			//A5.2.3
			uint op = (instruction >> 20) & 0x1F;
			uint Rn = Reg16(16);
			switch (op)
			{
				case _.b00000:
				case _.b00001: return Execute_Unhandled("arm and imm");
				case _.b00010:
				case _.b00011: return Execute_Unhandled("arm eor imm");
				case _.b00100:
				case _.b00101:
					if (Rn != _.b1111) return Execute_SUB_immediate_arm_A1();
					else return Execute_Unhandled("arm adr");
				case _.b00110:
				case _.b00111: return Execute_Unhandled("arm rsb imm");
				case _.b01000:
				case _.b01001:
					if (Rn != _.b1111) return Execute_ADD_immedate_arm_A1();
					else return Execute_ADR_A1();
				case _.b01010:
				case _.b01011: return Execute_Unhandled("arm adc imm");
				case _.b01100:
				case _.b01101: return Execute_Unhandled("arm sbc imm");
				case _.b01110:
				case _.b01111: return Execute_Unhandled("arm rsc imm");
				case _.b10000:
				case _.b10010:
				case _.b10100:
				case _.b10110: return ExecuteArm_DataProcessing_Misc_Imm();
				case _.b10001: return Execute_Unhandled("arm tst imm");
				case _.b10011: return Execute_Unhandled("arm teq imm");
				case _.b10101: return Execute_CMP_immediate_A1();
				case _.b10111: return Execute_Unhandled("arm cmn imm");
				case _.b11000:
				case _.b11001: return Execute_ORR_immediate_A1();
				case _.b11010:
				case _.b11011: return Execute_MOV_immediate_A1();
				case _.b11100:
				case _.b11101: return Execute_Unhandled("arm bic imm");
				case _.b11110:
				case _.b11111: return Execute_Unhandled("arm mvn imm");
				default: throw new InvalidOperationException("decoder fail");
			}
		}

		uint Execute_SUB_immediate_arm_A1()
		{
			//A8.6.212
			Bit S = _.BIT20(instruction);
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;

			if (n == _.b1111 && S == 0) throw new NotImplementedException("SEE ADR");
			if (n == _.b1101) return Execute_SUB_SP_minus_immediate_A1();
			if (n == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32 = _ARMExpandImm(imm12);
			return ExecuteCore_SUB_immediate_arm(Encoding.A1, setflags, n, d, imm32);
		}

		uint Execute_SUB_SP_minus_immediate_A1()
		{
			//A8.6.215
			uint d = Reg16(12);
			Bit S = _.BIT20(instruction);
			if (d == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm12 = instruction & 0xFFF;
			uint imm32 = _ARMExpandImm(imm12);
			return ExecuteCore_SUB_SP_minus_immediate(Encoding.A1,d,setflags,imm32);
		}

		uint Execute_ADR_A1()
		{
			//A8.6.10
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;
			uint imm32 = _ARMExpandImm(imm12);
			const bool add = true;
			return ExecuteCore_ADR(Encoding.A1, d, imm32, add);
		}

		uint Execute_ADD_immedate_arm_A1()
		{
			Bit S = _.BIT20(instruction);
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;

			if (n == _.b1111 && S == 0) { throw new NotImplementedException("SEE ADR"); }
			if (n == _.b1101) return Execute_ADD_SP_plus_immediate_A1();
			if (d == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32 = _ARMExpandImm(imm12);

			return ExecuteCore_ADD_immediate_arm(Encoding.A1, setflags, n, d, imm32);
		}

		uint Execute_ADD_SP_plus_immediate_A1()
		{
			uint d = Reg16(12);
			Bit S = _.BIT20(instruction);
			uint imm12 = instruction & 0xFFF;
			if (d == _.b1111 && S == 1) throw new NotImplementedException("SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32 = _ARMExpandImm(imm12);

			return ExecuteCore_ADD_SP_plus_immedate(Encoding.A1, d, setflags, imm32);
		}

		uint Execute_CMP_immediate_A1()
		{
			//A8.6.35
			uint Rn = Reg16(16);
			uint imm12 = instruction & 0xFFF;
			uint imm32 = _ARMExpandImm(imm12);
			return ExecuteCore_CMP_immediate(Encoding.A1, Rn, imm32);
		}

		uint Execute_ORR_immediate_A1()
		{
			//A8.6.113
			Bit S = _.BIT20(instruction);
			uint n = Reg16(16);
			uint d = Reg16(12);
			uint imm12 = instruction & 0xFFF;
			Debug.Assert(!(d == _.b1111 && S == 1), "SEE SUBS PC, LR and related instructions");
			bool setflags = (S == 1);
			uint imm32;
			Bit carry;
			_ARMExpandImm_C(imm12, APSR.C, out imm32, out carry);
			return ExecuteCore_ORR_immediate(Encoding.A1, n, d, setflags, imm32, carry);
		}

		uint Execute_MOV_immediate_A1()
		{
			//A8.6.96
			uint Rd = Reg16(12);
			uint S = _.BIT20(instruction);
			uint imm12 = instruction & 0xFFF;
			if (Rd == _.b1111 && S == 1)
				throw new InvalidOperationException("see subs pc, lr and related instructions");
			bool setflags = (S == 1);
			uint imm32;
			Bit carry;
			_ARMExpandImm_C(imm12, APSR.C, out imm32, out carry);
			return ExecuteCore_MOV_immediate(Encoding.A1, Rd, setflags, imm32, carry);
		}

		uint ExecuteArm_DataProcessing_Misc_Imm()
		{
			//A5-4
			//TODO
			return Execute_Unhandled("ExecuteArm_DataProcessing_Misc_Imm");
		}

		uint ExecuteArm_DataProcessing_Misc()
		{
			//A5.2.12
			uint op = (instruction >> 21) & 0x3;
			uint op1 = (instruction >> 16) & 0xF;
			uint op2 = (instruction >> 4) & 0x7;

			switch (op2)
			{
				case _.b000:
					switch (op)
					{
						case _.b00:
						case _.b10:
							return Execute_Unhandled("MRS");
						case _.b01:
							switch (op1)
							{
								case _.b0000:
								case _.b0100:
								case _.b1000:
								case _.b1100: return Execute_Unhandled("MSR (register) application level");
								case _.b0001:
								case _.b0101:
								case _.b1001:
								case _.b1101:
								case _.b0010:
								case _.b0011:
								case _.b0110:
								case _.b0111:
								case _.b1010:
								case _.b1011:
								case _.b1110:
								case _.b1111:
									return Execute_Unhandled("MSR (register) system level");
								default:
									throw new InvalidOperationException("decoder fail");
							}
						case _.b11:
							return Execute_Unhandled("MSR (register) system level");
						default:
							throw new InvalidOperationException("decoder fail");
					}
				case _.b001:
					switch (op)
					{
						case _.b01: return ExecuteArm_BX_A1();
						case _.b11: return Execute_Unhandled("ExecuteArm_CLZ");
						default:
							return Execute_Undefined();
					}
				case _.b010:
					if (op == _.b01) return Execute_Unhandled("BXJ");
					else return Execute_Undefined();
				case _.b011:
					if (op == _.b01) return Execute_Unhandled("BLX (register) on page A8-60");
					else return Execute_Undefined();
				case _.b100: return Execute_Undefined();
				case _.b101: return Execute_Unhandled("saturating addition and subtraction on page A5-13");
				case _.b110: return Execute_Undefined();
				case _.b111:
					switch (op)
					{
						case _.b01: return Execute_Unhandled("BKPT on page A8-56");
						case _.b11: return Execute_Unhandled("SMC/SMI on page B6-18 //sec.ext");
						default: return Execute_Undefined();
					}
				default:
					throw new InvalidOperationException("decoder fail");
			} //switch(op2)
		}

		uint ExecuteArm_BX_A1()
		{
			//A8-62
			//A8.6.25
			uint Rm = Reg16(0);
			if (disassemble)
				return DIS("BX/c/", "/r0/", Rm);
			uint m = r[Rm];
			_BXWritePC(m);
			return 1;
		}

		uint ExecuteArm_Media() { return Execute_Unhandled("ExecuteArm_Media"); }

		Decoder decoder_ExecuteArm_BranchAndTransfer = new Decoder();
		uint ExecuteArm_BranchAndTransfer()
		{
			decoder_ExecuteArm_BranchAndTransfer.Ensure(() => decoder_ExecuteArm_BranchAndTransfer
				.d("op", 6).d("R", 1).d("Rn", 4)
				.r("op == #0000x0", () => Execute_Unhandled("STMDA/STMED on page A8-376"))
				.r("op == #0000x1", () => Execute_Unhandled("LDMDA/LDMFA on page A8-112"))
				.r("op == #0010x0", () => Execute_STM_STMIA_STMEA_A1())
				.r("op == #001001", () => Execute_Unhandled("LDM/LDMIA/LDMFD on page A8-110"))
				.r("op == #001011 && Rn != #1101", () => Execute_Unhandled("LDM/LDMIA/LDMFD on page A8-110"))
				.r("op == #001011 && Rn == #1101", () => Execute_POP_A1())
				.r("op == #010000", () => Execute_Unhandled("STMDB/STMFD on page A8-378"))
				.r("op == #010010 && Rn != #1101", () => Execute_Unhandled("STMDB/STMFD on page A8-378"))
				.r("op == #010010 && Rn == #1101", () => Execute_PUSH_A1())
				.r("op == #0100x1", () => Execute_Unhandled("LDMDB/LDMEA on page A8-114"))
				.r("op == #0110x0", () => Execute_Unhandled("STMIB/STMFA on page A8-380"))
				.r("op == #0110x1", () => Execute_Unhandled("LDMIB/LDMED on page A8-116"))
				.r("op == #0xx1x0", () => Execute_Unhandled("STM (user registers) on page B6-22"))
				.r("op == #0xx1x1 && R==0", () => Execute_Unhandled("LDM (user registers on page B6-5"))
				.r("op == #0xx1x1 && R==1", () => Execute_Unhandled("LDM (exception return) on page B6-5"))
				.r("op == #10xxxx", () => Execute_B_A1())
				.r("op == #11xxxx", () => Execute_BL_A1())
				);

			uint op = (instruction >> 20) & 0x3F;
			uint Rn = Reg16(16);
			uint R = _.BIT15(instruction);

			decoder_ExecuteArm_BranchAndTransfer.Evaluate(op, R, Rn);

			return 1;
		}


		uint Execute_POP_A1()
		{
			uint register_list = instruction & 0xFFFF;
			if (_.BitCount(register_list) < 2) return Execute_LDM_LDMIA_LDMFD_A1();
			bool UnalignedAllowed = false;
			return ExecuteCore_POP(Encoding.A1, register_list, UnalignedAllowed);
		}

		uint Execute_PUSH_A1()
		{
			uint register_list = instruction & 0xFFFF;
			if (_.BitCount(register_list) < 2) return Execute_STMDB_STMFD_A1();
			bool UnalignedAllowed = false;
			return ExecuteCore_PUSH(Encoding.A1, register_list, UnalignedAllowed);
		}

		uint Execute_LDM_LDMIA_LDMFD_A1()
		{
			//A8.6.53 LDM/LDMIA/LDMFD
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint register_list = instruction & 0xFFFF;
			bool wback = (W == 1);
			if (n == 15 || _.BitCount(register_list) < 1) unpredictable = true;
			if (wback && _.BITN((int)n, register_list) == 1 && _ArchVersion() >= 7) unpredictable = true;
			return ExecuteCore_LDM_LDMIA_LDMFD(Encoding.A1, wback, n, register_list);
		}


		uint Execute_STM_STMIA_STMEA_A1()
		{
			//A8.6.189 STM/STMIA/STMEA
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint register_list = instruction & 0xFFFF;
			bool wback = W == 1;
			if (n == 15 || _.BitCount(register_list) < 1) unpredictable = true;
			return ExecuteCore_STM_STMIA_STMEA(Encoding.A1, wback, n, register_list);
		}

		uint Execute_STMDB_STMFD_A1()
		{
			//A8.6.191 STMDB/STMFD
			Bit W = _.BIT21(instruction);
			uint n = Reg16(16);
			uint register_list = instruction & 0xFFFF;
			if (W == 1 && n == _.b1101 && _.BitCount(register_list) >= 2) return Execute_PUSH_A1();
			bool wback = W == 1;
			if (n == 15 || _.BitCount(register_list) < 1) unpredictable = true;
			return ExecuteCore_STMDB_STMFD(Encoding.A1, wback, n, register_list);
		}

		uint Execute_BL_A1()
		{
			//A8.6.23
			//A8-58
			uint imm24 = instruction & 0xFFFFFF;
			int imm32 = _SignExtend_32(26, imm24 << 2);
			return ExecuteCore_BL_BLX_immediate(Encoding.A1, EInstrSet.ARM, imm32, false);
		}

		uint Execute_B_A1()
		{
			uint imm24 = instruction & 0xFFFFFF;
			int imm32 = _SignExtend_32(26, imm24 << 2);

			return ExecuteCore_B(Encoding.A1, imm32);
		}

		Decoder Decoder_ExecuteArm_SVCAndCP = new Decoder();
		uint ExecuteArm_SVCAndCP()
		{
			//A5.6 Supervisor Call, and coprocessor instructions

			Decoder_ExecuteArm_SVCAndCP.Ensure(() => Decoder_ExecuteArm_SVCAndCP
				.d("op1", 6).d("op", 1).d("coproc_special", 1).d("rn_is_15", 1)
				.r("op1==#0xxxxx && op1!=#000x0x && coproc_special==#1", () => Execute_ExtensionRegister_LoadStore())
				.r("op1==#0xxxx0 && op1!=#000x0x && coproc_special==#0", () => Execute_Unhandled("STC,STC2"))
				.r("op1==#0xxxx1 && op1!=#000x0x && coproc_special==#0 && rn_is_15==#0", () => Execute_Unhandled("LDC,LDC2(immediate)"))
				.r("op1==#0xxxx1 && op1!=#000x0x && coproc_special==#0 && rn_is_15==#1", () => Execute_Unhandled("LDC,LDC2(literal)"))
				.r("op1==#00000x", () => Execute_Undefined())
				.r("op1==#00010x && coproc_special==#1", () => Execute_Unhandled("ExecuteArm_SIMD_VFP_64bit_xfer"))
				.r("op1==#000100 && coproc_special==#0", () => Execute_Unhandled("MCRR,MCRR2"))
				.r("op1==#000101 && coproc_special==#0", () => Execute_Unhandled("MRRC,MRRC2"))
				.r("op1==#10xxxx && op==0 && coproc_special==#1", () => Execute_Unhandled("VFP data-processing on page A7-24"))
				.r("op1==#10xxxx && op==0 && coproc_special==#0", () => Execute_Unhandled("CDP,CDP2 on page A8-68"))
				.r("op1==#10xxxx && op==1 && coproc_special==#1", () => ExecuteArm_ShortVFPTransfer())
				.r("op1==#10xxx0 && op==1 && coproc_special==#0", () => Execute_Unhandled("MCR,MCR2 on pageA8-186"))
				.r("op1==#10xxx1 && op==1 && coproc_special==#0", () => Execute_MRC_MRC2_A1())
				.r("op1==#110000", () => Execute_SVC_A1())
				);

			uint op1 = (instruction >> 20) & 0x3F;
			uint Rn = Reg16(16);
			uint coproc = (instruction >> 8) & 0xF;
			uint op = _.BIT4(instruction);
			uint coproc_special = (coproc == _.b1010 || coproc == _.b1011) ? 1U : 0U;
			uint rn_is_15 = (Rn == 15) ? 1U : 0U;

			Decoder_ExecuteArm_SVCAndCP.Evaluate(op1, op, coproc_special, rn_is_15);
			return 1;
		}

		uint Execute_ExtensionRegister_LoadStore()
		{
			//A7.6 Extension register load/store instructions
			uint opcode = (instruction >> 20) & 0x1F;
			uint n = Reg16(16);
			bool bit8 = _.BIT8(instruction)==1;
			switch (opcode)
			{
				case _.b00100: case _.b00101: return Execute_Unhandled("64-bit transfers between ARM core and extension registers");
				case _.b01000: case _.b01100: return Execute_Unhandled("VSTM");
				case _.b01010: case _.b01110: return Execute_Unhandled("VSTM");
				case _.b10000: case _.b10100: case _.b11000: case _.b11100: return Execute_Unhandled("VSTR");
				case _.b10010: case _.b10110:
					if (n != _.b1101) return Execute_Unhandled("VSTM");
					else return bit8?Execute_VPUSH_A1():Execute_VPUSH_A2();
				case _.b01001: case _.b01101: return Execute_Unhandled("VLDM");
				case _.b01011: case _.b01111:
					if (n != _.b1101) return Execute_Unhandled("VLDM");
					else return Execute_Unhandled("VPOP");
				case _.b10001: case _.b10101: case _.b11001: case _.b11101:
					return bit8 ? Execute_VLDR_A1() : Execute_VLDR_A2();
				case _.b10011: case _.b10111: return Execute_Unhandled("VLDM");
				default: throw new InvalidOperationException("decoder fail");
			}
		}

		uint Execute_VLDR_A1()
		{
			//A8.6.320
			throw new NotSupportedException("Execute_VLDR_A1");
		}

		uint Execute_VLDR_A2()
		{
			//A8.6.320
			const bool single_reg = true;
			Bit U = _.BIT23(instruction);
			Bit D = _.BIT22(instruction);
			bool add = (U == 1);
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			uint Vd = Reg16(12);
			uint n = Reg16(0);
			uint d = (Vd << 1) | D;
			return ExecuteCore_VLDR(Encoding.A1, single_reg, add, d, n, imm32);
		}

		uint Execute_VPUSH_A1()
		{
			//A8.6.355
			const bool single_regs = false;
			Bit D = _.BIT22(instruction);
			uint d = ((uint)D << 4) | Reg16(12);
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			uint regs = imm8 / 2;
			Debug.Assert(!((imm8&1)==1),"see FSTMX");
			if(regs==0 || regs>16 || (d+regs)>32) _FlagUnpredictable();
			return ExecuteCore_VPUSH(Encoding.A1, single_regs, d, regs, imm32);

		}

		uint Execute_VPUSH_A2()
		{
			//A8.6.355
			const bool single_regs = true;
			Bit D = _.BIT22(instruction);
			uint d = (Reg16(12)<<1)|D;
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			uint regs = imm8;
			if (regs == 0 || regs > 16 || (d + regs) > 32) _FlagUnpredictable();
			return ExecuteCore_VPUSH(Encoding.A2, single_regs, d, regs, imm32);
		}

		uint Execute_MRC_MRC2_A1()
		{
			//ignoring admonition to see "advanced SIMD and VFP" which has been handled by decode earlier
			//TODO - but i should assert anyway
			uint t = Reg16(12);
			uint cp = Reg16(8);
			uint opc1 = Reg8(21);
			uint crn = Reg16(16);
			uint opc2 = Reg8(5);
			uint crm = Reg16(0);
			if (t == 13 && _CurrentInstrSet() != EInstrSet.ARM) unpredictable = true;
			return ExecuteCore_MRC_MRC2(Encoding.A1, cp, opc1, t, crn, crm, opc2);
		}

		Decoder Decoder_ExecuteArm_ShortVFPTransfer = new Decoder();
		uint ExecuteArm_ShortVFPTransfer()
		{
			//A7.8
			//A7-31

			Decoder_ExecuteArm_ShortVFPTransfer.Ensure(() => Decoder_ExecuteArm_ShortVFPTransfer
				.d("A", 3).d("L", 1).d("C", 1).d("B", 2)
				.r("L==0 && C==0 && A==#000", () => Execute_Unhandled("VMOV (between ARM core register and single-precision register) on page A8-648"))
				.r("L==0 && C==0 && A==#111", () => ExecuteArm_VMSR_A1())
				.r("L==0 && C==1 && A==#0xx", () => Execute_Unhandled("VMOV (ARM core register to scalar) on page A8-644"))
				.r("L==0 && C==1 && A==#1xx && B==#0x", () => Execute_Unhandled("VDUP (ARM core register) on page A8-594"))
				.r("L==1 && C==0 && A==#000", () => Execute_Unhandled("VMOV (between ARM core register and single-precision register) on page A8-648"))
				.r("L==1 && C==0 && A==#111", () => Execute_Unhandled("VMRS on page A8-658 or page B6-27"))
				.r("L==1 && C==1", () => Execute_Unhandled("VMOV (scalar to ARM core register) on page A8-646"))
				);

			uint A = (instruction >> 21) & _.b111;
			uint L = _.BIT20(instruction);
			uint C = _.BIT8(instruction);
			uint B = (instruction >> 5) & _.b11;

			Decoder_ExecuteArm_ShortVFPTransfer.Evaluate(A, L, C, B);
			return 1;
		}

		uint ExecuteArm_VMSR_A1()
		{
			uint t = Reg16(12);
			if (t == 15 || (t == 13 && _CurrentInstrSet() != EInstrSet.ARM)) return _UNPREDICTABLE();

			if (disassemble)
				if (nstyle)
					return DISNEW("FMXR<c>", "<fpscr>,<Rt>", t);
				else
					return DISNEW("VMSR<c>", "<fpscr>, <Rt>", t);

			return ExecuteCore_VMSR(t);
		}

		uint Execute_SVC_A1()
		{
			//A8.6.218
			//A8-430
			uint imm24 = instruction & 0xFFFFFF;
			uint imm32 = imm24;

			return ExecuteCore_SVC(Encoding.A1, imm32);
		}


	} //class ARM11
}