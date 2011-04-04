using System;
using System.Diagnostics;

namespace BizHawk.Emulation.CPUs.ARM
{

	partial class ARM
	{

		uint ExecuteThumb()
		{
			uint opcode = instruction >> 10;
			_currentCondVal = 14;

			//TODO - this one could really be turned into a table

			switch (opcode)
			{
				case _.b000000:
				case _.b000001:
				case _.b000010:
				case _.b000011:
				case _.b000100:
				case _.b000101:
				case _.b000110:
				case _.b000111:
				case _.b001000:
				case _.b001001:
				case _.b001010:
				case _.b001011:
				case _.b001100:
				case _.b001101:
				case _.b001110:
				case _.b001111:
					return ExecuteThumb_AluMisc();

				case _.b010000: return ExecuteThumb_DataProcessing();
				case _.b010001: return ExecuteThumb_SpecialBX();

				case _.b010010:
				case _.b010011:
					return Execute_LDR_literal_T1();

				case _.b010100:
				case _.b010101:
				case _.b010110:
				case _.b010111:
				case _.b011000:
				case _.b011001:
				case _.b011010:
				case _.b011011:
				case _.b011100:
				case _.b011101:
				case _.b011110:
				case _.b011111:
				case _.b100000:
				case _.b100001:
				case _.b100010:
				case _.b100011:
				case _.b100100:
				case _.b100101:
				case _.b100110:
				case _.b100111:
					return ExecuteThumb_LoadStore();

				case _.b101000:
				case _.b101001: return Execute_ADR_T1();
				case _.b101010:
				case _.b101011: return Execute_ADD_SP_plus_immediate_T1();

				case _.b101100:
				case _.b101101:
				case _.b101110:
				case _.b101111:
					return ExecuteThumb_Misc16();

				case _.b110000:
				case _.b110001: return Execute_STM_STMIA_STMEA_T1();
				case _.b110010:
				case _.b110011: return Execute_LDM_LDMIA_LDMFD_T1();

				case _.b110100:
				case _.b110101:
				case _.b110110:
				case _.b110111:
					return ExecuteThumb_CondBr_And_SVC();

				case _.b111000:
				case _.b111001: return Execute_B_T2();

				case _.b111010:
				case _.b111011:
				case _.b111100:
				case _.b111101:
				case _.b111110:
				case _.b111111:
					return ExecuteThumb_32();

				default:
					throw new InvalidOperationException("unhandled case in ExecuteThumb");
			}
		}

		uint Execute_LDR_literal_T1()
		{
			//A8.6.59
			uint t = Reg8(8);
			uint imm8 = instruction & 0xFF;
			uint imm32 = imm8 << 2;
			const bool add = true;

			return ExecuteCore_LDR_literal(Encoding.T1, t, imm32, add);
		}

		uint ExecuteThumb_LoadStore()
		{
			//A6.2.4
			uint opA = (instruction >> 12) & 0xF;
			uint opB = (instruction >> 9) & 0x7;
			switch (opA)
			{
				case _.b0101:
					switch (opB)
					{
						case _.b000: return Execute_Unhandled("STR (register) on page A8-386");
						case _.b001: return Execute_Unhandled("STRH (register) on page A8-412");
						case _.b010: return Execute_STRB_register_T1();
						case _.b011: return Execute_Unhandled("LDRSB (register) on page A8-164");
						case _.b100: return Execute_LDR_register_T1();
						case _.b101: return Execute_Unhandled("LDRH (register)");
						case _.b110: return Execute_LDRB_register_T1();
						case _.b111: return Execute_Unhandled("LDRSH (register) on page A8-172");
						default: throw new InvalidOperationException("decoder fail");
					}
				case _.b0110:
					switch (opB)
					{
						case _.b000:
						case _.b001:
						case _.b010:
						case _.b011: return Execute_STR_immediate_thumb_T1();
						case _.b100:
						case _.b101:
						case _.b110:
						case _.b111: return Execute_LDR_immediate_thumb_T1();
						default: throw new InvalidOperationException("decoder fail");
					}
				case _.b0111:
					switch (opB)
					{
						case _.b000:
						case _.b001:
						case _.b010:
						case _.b011: return Execute_STRB_immediate_thumb_T1();
						case _.b100:
						case _.b101:
						case _.b110:
						case _.b111: return Execute_LDRB_immediate_thumb_T1();
						default: throw new InvalidOperationException("decoder fail");
					}
				case _.b1000:
					switch (opB)
					{
						case _.b000:
						case _.b001:
						case _.b010:
						case _.b011: return Execute_STRH_immediate_thumb_T1();
						case _.b100:
						case _.b101:
						case _.b110:
						case _.b111: return Execute_LDRH_immediate_thumb_T1();
						default: throw new InvalidOperationException("decoder fail");
					}
				case _.b1001:
					switch (opB)
					{
						case _.b000:
						case _.b001:
						case _.b010:
						case _.b011: return Execute_STR_immediate_thumb_T2();
						case _.b100:
						case _.b101:
						case _.b110:
						case _.b111: return Execute_LDR_immediate_thumb_T2();
						default: throw new InvalidOperationException("decoder fail");
					}
				default: throw new InvalidOperationException("decoder fail");
			} //switch(opA)
		}

		uint Execute_STRB_register_T1()
		{
			//A8.6.198
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint m = Reg8(6);
			const bool index = true; const bool add = true; const bool wback = false;
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			return ExecuteCore_STRB_register(Encoding.T1, t, n, m, shift_t, shift_n, index, add, wback);
		}

		uint Execute_LDR_register_T1()
		{
			//A8.6.60
			if (_CurrentInstrSet() == EInstrSet.THUMBEE) throw new NotSupportedException("Modified operatoin in ThumbEE");
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint m = Reg8(6);
			const bool index = true; const bool add = true; const bool wback = false;
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			return ExecuteCore_LDR_register(Encoding.T1, t, n, m, shift_t, shift_n, index, add, wback);
		}

		uint Execute_LDRB_register_T1()
		{
			//A8.6.64
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint m = Reg8(6);
			const bool index = true; const bool add = true; const bool wback = false;
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			return ExecuteCore_LDRB_register(Encoding.T1, t, n, m, shift_t, shift_n, index, add, wback);
		}

		uint Execute_LDRB_immediate_thumb_T1()
		{
			//A8.6.61
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint imm5 = (instruction >> 6) & 0x1F;
			uint imm32 = _ZeroExtend_32(imm5);
			const bool index = true; const bool add = true; const bool wback = false;
			return ExecuteCore_LDRB_immediate_thumb(Encoding.T1, t, n, imm32, index, add, wback);
		}

		uint Execute_STRB_immediate_thumb_T1()
		{
			//A8.6.196
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint imm5 = (instruction >> 6) & 0x1F;
			uint imm32 = _ZeroExtend_32(imm5);
			const bool index = true; const bool add = true; const bool wback = false;
			return ExecuteCore_STRB_immediate_thumb(Encoding.T1, t, n, imm32, index, add, wback);
		}

		uint Execute_LDRH_immediate_thumb_T1()
		{
			//A8.6.73
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint imm5 = (instruction >> 6) & 0x1F;
			uint imm32 = _ZeroExtend_32(imm5 << 1);
			const bool index = true; const bool add = true; const bool wback = false;
			return ExecuteCore_LDRH_immediate_thumb(Encoding.T1, t, n, imm32, index, add, wback);
		}

		uint Execute_STRH_immediate_thumb_T1()
		{
			//A8.6.206 STRH
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint imm5 = (instruction >> 6) & 0x1F;
			uint imm32 = _ZeroExtend_32(imm5 << 1);
			const bool index = true; const bool add = true; const bool wback = false;
			return ExecuteCore_STRH_immediate_thumb(Encoding.T1, t, n, imm32, index, add, wback);
		}

		uint Execute_LDR_immediate_thumb_T1()
		{
			//A8.6.57 LDR (immediate,thumb)
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint imm5 = (instruction >> 6) & 0x1F;
			uint imm32 = _ZeroExtend_32(imm5 << 2);
			bool index = true; bool add = true; bool wback = false;
			return ExecuteCore_LDR_immediate_thumb(Encoding.T1, t, imm32, n, index, add, wback);
		}

		uint Execute_STR_immediate_thumb_T1()
		{
			//A8.6.193 STR (immediate,thumb)
			uint t = Reg8(0);
			uint n = Reg8(3);
			uint imm5 = (instruction >> 6) & 0x1F;
			uint imm32 = _ZeroExtend_32(imm5 << 2);
			bool index = true; bool add = true; bool wback = false;
			return ExecuteCore_STR_immediate_thumb(Encoding.T1, t, imm32, n, index, add, wback);
		}

		uint Execute_LDR_immediate_thumb_T2()
		{
			//A8.6.57 LDR (immediate, thumb)
			uint t = Reg8(8);
			uint imm8 = instruction & 0xFF;
			const uint n = 13;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			const bool index = true; const bool add = true; const bool wback = false;
			return ExecuteCore_LDR_immediate_thumb(Encoding.T2, t, imm32, n, index, add, wback);
		}

		uint Execute_STR_immediate_thumb_T2()
		{
			//A8.6.193 STR (immediate,thumb)
			uint Rt = Reg8(8);
			uint imm8 = instruction & 0xFF;
			const uint n = 13;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			const bool index = true; const bool add = true; const bool wback = false;
			return ExecuteCore_STR_immediate_thumb(Encoding.T2, Rt, imm32, n, index, add, wback);
		}

		uint ExecuteThumb_32()
		{
			uint op1 = (instruction >> 11) & 3;
			uint op2 = (instruction >> 4) & 0x7F;
			ThumbFetchExtra();
			uint op = _.BIT15(thumb_32bit_extra);
			switch (op1)
			{
				case _.b00: throw new InvalidOperationException("decode error");
				case _.b01: return Execute_Unhandled("thumb-32bit");
				case _.b10:
					if (op == 0) return Execute_Unhandled("thumb-32bit");
					else return ExecuteThumb_32_BranchAndMiscControl(op2);
				case _.b11:
					return Execute_Unhandled("thumb-32bit");
				default: throw new InvalidOperationException();
			}
		}

		uint ExecuteThumb_32_BranchAndMiscControl(uint op)
		{
			uint op1 = (thumb_32bit_extra >> 12) & 7;
			switch (op1)
			{
				case _.b000:
				case _.b010:
					return Execute_Unhandled("thumb-32bit");
				case _.b001:
				case _.b011: return Execute_Unhandled("thumb-32bit 6T2 branch");
				case _.b100:
				case _.b110: return Execute_BL_BLX_immediate_T2();
				case _.b101:
				case _.b111: return Execute_BL_BLX_immediate_T1();
				default: throw new InvalidOperationException();
			}
		}

		uint Execute_BL_BLX_immediate_T1()
		{
			//A8.6.23
			uint S = _.BIT10(instruction);
			uint J1 = _.BIT13(thumb_32bit_extra);
			uint J2 = _.BIT11(thumb_32bit_extra);
			uint imm11 = thumb_32bit_extra & _.b11111111111;
			uint imm10 = instruction & _.b1111111111;
			uint I1 = (~(J1 ^ S)) & 1;
			uint I2 = (~(J2 ^ S)) & 1;
			int imm32 = _SignExtend_32(25, (imm11 << 1) | (imm10 << 12) | (I2 << 22) | (I1 << 23) | (S << 24));
			if (_InITBlock() && !_LastInITBlock()) _UNPREDICTABLE();
			return ExecuteCore_BL_BLX_immediate(Encoding.T1, _CurrentInstrSet(), imm32, false);
		}

		uint Execute_BL_BLX_immediate_T2()
		{
			//A8.6.23
			uint S = _.BIT10(instruction);
			uint J1 = _.BIT13(thumb_32bit_extra);
			uint J2 = _.BIT11(thumb_32bit_extra);
			uint H = _.BIT0(thumb_32bit_extra);
			uint imm10L = (thumb_32bit_extra >> 1) & 0x3FF;
			uint imm10H = instruction & 0x3FF;
			uint I1 = (~(J1 ^ S)) & 1;
			uint I2 = (~(J2 ^ S)) & 1;
			int imm32 = _SignExtend_32(25, (imm10L << 2) | (imm10H << 12) | (I2 << 22) | (I1 << 23) | (S << 24));
			return ExecuteCore_BL_BLX_immediate(Encoding.T2, EInstrSet.ARM, imm32, true);
		}

		uint ExecuteThumb_AluMisc()
		{
			//A6.2.1
			uint opcode = (instruction >> 9) & 0x1F;
			switch (opcode)
			{
				case _.b00000:
				case _.b00001:
				case _.b00010:
				case _.b00011:
					return Execute_LSL_immediate_T1();
				case _.b00100:
				case _.b00101:
				case _.b00110:
				case _.b00111:
					return Execute_LSR_immediate_T1();
				case _.b01000:
				case _.b01001:
				case _.b01010:
				case _.b01011:
					return Execute_ASR_immediate_T1();
				case _.b01100: return Execute_ADD_Register_T1();
				case _.b01101: return Execute_SUB_Register_T1();
				case _.b01110: return Execute_ADD_immediate_thumb_T1();
				case _.b01111: return Execute_SUB_immediate_thumb_T1();
				case _.b10000:
				case _.b10001:
				case _.b10010:
				case _.b10011:
					return Execute_MOV_immediate_T1();
				case _.b10100:
				case _.b10101:
				case _.b10110:
				case _.b10111:
					return Execute_CMP_immediate_T1();
				case _.b11000:
				case _.b11001:
				case _.b11010:
				case _.b11011:
					return Execute_ADD_immediate_thumb_T2();
				case _.b11100:
				case _.b11101:
				case _.b11110:
				case _.b11111:
					return Execute_SUB_immediate_thumb_T2();
			}
			return Execute_Unhandled("ExecuteThumb_AluMisc");
		}

		uint Execute_CMP_immediate_T1()
		{
			//A8.6.35 CMP immediate
			//A8-80
			uint n = Reg8(8);
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8);
			return ExecuteCore_CMP_immediate(Encoding.T1, n, imm32);
		}

		uint Execute_MOV_immediate_T1()
		{
			//A8.6.96 MOV immediate
			uint d = Reg8(8);
			uint imm8 = instruction & 0xFF;
			bool setflags = !_InITBlock();
			uint imm32 = _ZeroExtend_32(imm8);
			Bit carry = APSR.C;
			return ExecuteCore_MOV_immediate(Encoding.T1, d, setflags, imm32, carry);
		}

		uint Execute_SUB_Register_T1()
		{
			//A8.6.213 SUB (register)
			uint d = Reg8(0);
			uint n = Reg8(3);
			uint m = Reg8(6);
			bool setflags = !_InITBlock();
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			return ExecuteCore_SUB_register(Encoding.T1, setflags, m, n, d, shift_t, shift_n);
		}

		uint Execute_ADD_Register_T1()
		{
			//A8.6.6 ADD (register)
			uint d = Reg8(0);
			uint n = Reg8(3);
			uint m = Reg8(6);
			bool setflags = !_InITBlock();
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			return ExecuteCore_ADD_register(Encoding.T1, m, d, n, setflags, shift_t, shift_n);
		}

		uint Execute_ADD_register_T2()
		{
			//A8.6.6 ADD (register)
			Bit DN = _.BIT7(instruction);
			uint rdn = Reg8(0);
			uint m = Reg16(3);
			Debug.Assert(!(DN == 1 && rdn == _.b101 || m == _.b1101), "see ADD (SP plus register)");
			uint d = rdn;
			uint n = rdn;
			bool setflags = false;
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			if (n == 15 && m == 15) _FlagUnpredictable();
			if (d == 15 && _InITBlock() && !_LastInITBlock()) _FlagUnpredictable();
			return ExecuteCore_ADD_register(Encoding.T2, m, d, n, setflags, shift_t, shift_n);
		}

		uint Execute_SUB_immediate_thumb_T1()
		{
			//A7.6.211
			uint d = Reg8(0);
			uint n = Reg8(3);
			uint imm3 = (instruction >> 6) & 7;
			bool setflags = !_InITBlock();
			uint imm32 = _ZeroExtend_32(imm3);
			return ExecuteCore_SUB_immediate_thumb(Encoding.T1, n, d, setflags, imm32);
		}

		uint Execute_ADD_immediate_thumb_T1()
		{
			uint d = Reg8(0);
			uint n = Reg8(3);
			uint imm3 = (instruction >> 6) & 7;
			bool setflags = !_InITBlock();
			uint imm32 = _ZeroExtend_32(imm3);
			return ExecuteCore_ADD_immediate_thumb(Encoding.T1, n, d, setflags, imm32);
		}

		uint Execute_SUB_immediate_thumb_T2()
		{
			//A8.6.211
			uint d = Reg8(8);
			uint n = d;
			bool setflags = !_InITBlock();
			uint imm8 = (instruction & 0xFF);
			uint imm32 = _ZeroExtend_32(imm8);
			return ExecuteCore_SUB_immediate_thumb(Encoding.T2, n, d, setflags, imm32);
		}

		uint Execute_ADD_immediate_thumb_T2()
		{
			uint Rdn = Reg8(8);
			uint imm8 = instruction & _.b11111111;
			uint d = Rdn;
			uint n = Rdn;
			bool setflags = !_InITBlock();
			uint imm32 = imm8;
			return ExecuteCore_ADD_immediate_thumb(Encoding.T2, n, d, setflags, imm32);
		}

		uint Execute_LSL_immediate_T1()
		{
			//A8.6.14
			uint imm5 = (instruction >> 6) & 0x1F;
			if (imm5 == 0 && procopt.Thumb_LSL_immediate_T1_0_is_MOV_register_T2) return Execute_MOV_register_T2();

			uint d = Reg8(0);
			uint m = Reg8(3);
			bool setflags = !_InITBlock();
			_DecodeImmShift(0, imm5);

			return ExecuteCore_LSL_immediate(Encoding.T1, d, m, setflags, shift_n);
		}

		uint Execute_ASR_immediate_T1()
		{
			//A8.6.14 ASR (immediate)
			uint d = Reg8(0);
			uint m = Reg8(3);
			uint imm5 = (instruction >> 6) & 0x1F;
			bool setflags = !_InITBlock();
			_DecodeImmShift(_.b10, imm5);

			return ExecuteCore_ASR_immediate(Encoding.T1, d, m, setflags, shift_n);
		}

		uint Execute_LSR_immediate_T1()
		{
			uint imm5 = (instruction >> 6) & 0x1F;
			uint m = Reg8(3);
			uint d = Reg8(0);
			bool setflags = !_InITBlock();
			_DecodeImmShift(_.b01, imm5);
			return ExecuteCore_LSR_immediate(Encoding.T1, d, m, setflags, shift_n);
		}

		uint Execute_MOV_register_T1()
		{
			//A8.6.97 MOV (register)
			uint D = _.BIT7(instruction);
			uint d = Reg8(0) + 8 * D;
			uint m = Reg16(3);
			bool setflags = false;
			if (d == 15 && _InITBlock() && !_LastInITBlock()) unpredictable = true;

			//my own sanity check:
			if (m >= 8 && _ArchVersion() < 6) throw new InvalidOperationException("thumb mov register invalid for your architecture version. need to think about this.");

			return ExecuteCore_MOV_register(Encoding.T1, d, m, setflags);
		}

		uint Execute_MOV_register_T2()
		{
			uint D = _.BIT7(instruction);
			uint d = Reg8(0);
			uint m = Reg8(3);
			bool setflags = true;

			if (_InITBlock()) unpredictable = true;

			return ExecuteCore_MOV_register(Encoding.T2, d, m, setflags);
		}

		uint ExecuteThumb_DataProcessing()
		{
			//A6.2.2
			uint opcode = (instruction >> 6) & 0xF;
			switch (opcode)
			{
				case _.b0000: return Execute_Unhandled("thumb AND reg");
				case _.b0001: return Execute_EOR_register_T1();
				case _.b0010: return Execute_Unhandled("thumb LSL reg");
				case _.b0011: return Execute_Unhandled("thumb LSR reg");
				case _.b0100: return Execute_Unhandled("thumb ASR reg");
				case _.b0101: return Execute_Unhandled("thumb ADC reg");
				case _.b0110: return Execute_Unhandled("thumb SBC reg");
				case _.b0111: return Execute_Unhandled("thumb ROR reg");
				case _.b1000: return Execute_Unhandled("thumb TST reg");
				case _.b1001: return Execute_RSB_immediate_T1();
				case _.b1010: return Execute_CMP_register_T1();
				case _.b1011: return Execute_Unhandled("thumb CMN reg");
				case _.b1100: return Execute_ORR_register_T1();
				case _.b1101: return Execute_Unhandled("thumb MUL");
				case _.b1110: return Execute_Unhandled("thumb BIC reg");
				case _.b1111: return Execute_Unhandled("thumb MVN reg");
				default: throw new InvalidOperationException();
			}
		}

		uint Execute_RSB_immediate_T1()
		{
			//A8.6.142
			uint d = Reg8(0);
			uint n = Reg8(3);
			bool setflags = !_InITBlock();
			uint imm32 = 0;
			return ExecuteCore_RSB_immediate(Encoding.T1, d, n, setflags, imm32);
		}

		uint Execute_EOR_register_T1()
		{
			//A8.6.45
			uint d = Reg8(0);
			uint n = d;
			uint m = Reg8(3);
			bool setflags = !_InITBlock();
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			return ExecuteCore_EOR_register(Encoding.T1, m, d, n, setflags, shift_t, shift_n);
		}

		uint Execute_ORR_register_T1()
		{
			//A7.6.114
			uint d = Reg8(0);
			uint n = d;
			uint m = Reg8(3);
			bool setflags = !_InITBlock();
			const SRType shift_t = SRType.LSL;
			const int shift_n = 0;
			return ExecuteCore_ORR_register(Encoding.T1, m, d, n, setflags, shift_t, shift_n);
		}

		uint Execute_CMP_register_T1()
		{
			//A8.6.36
			uint n = Reg8(0);
			uint m = Reg8(3);
			SRType shift_t = SRType.LSL;
			int shift_n = 0;
			return ExecuteCore_CMP_register(Encoding.T1, n, m, shift_t, shift_n);
		}

		uint ExecuteThumb_SpecialBX()
		{
			uint opcode = (instruction >> 6) & _.b1111;
			switch (opcode)
			{
				case _.b0000:
					return Execute_Unhandled("ADD (low register) on page A8-24 [v6T2*] *unpredictable in earlier variants");
				case _.b0001:
				case _.b0010:
				case _.b0011:
					return Execute_ADD_register_T2();
				case _.b0100:
					return _UNPREDICTABLE();
				case _.b0101:
				case _.b0110:
				case _.b0111:
					return Execute_Unhandled("CMP (high register) on page A8-82 [v4t]");
				case _.b1000:
					return Execute_MOV_register_T1();
				case _.b1001:
				case _.b1010:
				case _.b1011:
					return Execute_MOV_register_T1();
				case _.b1100:
				case _.b1101:
					return Execute_BX_T1();
				case _.b1110:
				case _.b1111:
					return Execute_BLX_register_T1();
				default: throw new InvalidOperationException("decode fail");

			}
		}

		uint Execute_BLX_register_T1()
		{
			//A8.6.24
			uint m = Reg16(3);
			if (m == 15) _FlagUnpredictable();
			if (_InITBlock() && !_LastInITBlock()) _FlagUnpredictable();
			return ExecuteCore_BLX_register(Encoding.T1, m);
		}

		uint Execute_BX_T1()
		{
			//A8.6.25
			uint m = Reg16(3);
			if (_InITBlock() && !_LastInITBlock()) return _UNPREDICTABLE();
			if (disassemble)
				return DISNEW("BX", "<Rm!>", m);
			_BXWritePC(r[m]);
			return 1;
		}

		uint Execute_ADR_T1()
		{
			//A8.6.10
			uint d = Reg8(8);
			uint imm8 = instruction & 0xFF;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			const bool add = true;
			return ExecuteCore_ADR(Encoding.T1, d, imm32, add);
		}

		uint Execute_ADD_SP_plus_immediate_T2()
		{
			//A8.6.8
			uint d = 13;
			uint imm7 = (instruction & 0xFF);
			bool setflags = false;
			uint imm32 = _ZeroExtend_32(imm7 << 2);
			return ExecuteCore_ADD_SP_plus_immedate(Encoding.T2, d, setflags, imm32);
		}

		uint Execute_ADD_SP_plus_immediate_T1()
		{
			//A8.6.8 ADD SP plus immediate
			//A8-28
			uint d = Reg8(8);
			uint imm8 = (instruction & 0xFF);
			bool setflags = false;
			uint imm32 = _ZeroExtend_32(imm8 << 2);
			return ExecuteCore_ADD_SP_plus_immedate(Encoding.T1, d, setflags, imm32);
		}

		Decoder decoder_ExecuteThumb_Misc16 = new Decoder();
		uint ExecuteThumb_Misc16()
		{
			decoder_ExecuteThumb_Misc16.Ensure(() => decoder_ExecuteThumb_Misc16
				.d("opcode", 7)
				.r("opcode == #0110010", () => Execute_Unhandled("SETEND on page A8-314"))
				.r("opcode == #0110011", () => Execute_Unhandled("CPS on page B6-3"))
				.r("opcode == #00000xx", () => Execute_ADD_SP_plus_immediate_T2())
				.r("opcode == #00001xx", () => Execute_SUB_SP_minus_immediate_T1())
				.r("opcode == #0001xxx", () => Execute_Unhandled("CBNZ,CBZ on page A8-66 [v6T2]"))
				.r("opcode == #001000x", () => Execute_Unhandled("SXTH on page A8-444"))
				.r("opcode == #001001x", () => Execute_Unhandled("SXTB on page A8-440"))
				.r("opcode == #001010x", () => Execute_UXTH_T1())
				.r("opcode == #001011x", () => Execute_UXTB_T1())
				.r("opcode == #0011xxx", () => Execute_Unhandled("CBNZ,CBZ on page A8-66 [v6T2]"))
				.r("opcode == #010xxxx", () => Execute_PUSH_T1())
				.r("opcode == #1001xxx", () => Execute_Unhandled("CBNZ,CBZ on page A8-66 [v6T2]"))
				.r("opcode == #101000x", () => Execute_Unhandled("REV on page A8-272"))
				.r("opcode == #101001x", () => Execute_Unhandled("REV16 on page A8-274"))
				.r("opcode == #101011x", () => Execute_Unhandled("REVSH on page A8-276"))
				.r("opcode == #110xxxx", () => Execute_POP_T1())
				.r("opcode == #1110xxx", () => Execute_Unhandled("BKPT on page A8-56"))
				.r("opcode == #1111xxx", () => Execute_Unhandled("If-Then and hints on page A6-12"))
				);

			uint opcode = (instruction >> 5) & 0x7F;
			decoder_ExecuteThumb_Misc16.Evaluate(opcode);
			return 1;
		}

		uint Execute_POP_T1()
		{
			//A8.6.122
			uint P = _.BIT8(instruction);
			uint registers = (P << 15) | (instruction & 0xFF);
			bool UnalignedAllowed = false;
			if (registers == 0) { _UNPREDICTABLE(); return 1; }
			if (P == 1 && _InITBlock() && !_LastInITBlock()) { _UNPREDICTABLE(); return 1; }
			if (disassemble)
				return DISNEW("pop<c>", "<registers>", registers);
			return ExecuteCore_POP(Encoding.T1, registers, UnalignedAllowed);
		}

		uint Execute_SUB_SP_minus_immediate_T1()
		{
			//A8.6.215 SUB (SP minus immediate)
			const uint d = 13;
			uint imm7 = instruction & 0x7F;
			bool setflags = false;
			uint imm32 = _ZeroExtend_32(imm7 << 2);
			return ExecuteCore_SUB_SP_minus_immediate(Encoding.T1, d, setflags, imm32);
		}

		uint Execute_UXTH_T1()
		{
			//A8.6.265
			uint d = Reg8(0);
			uint m = Reg8(3);
			const uint rotation = 0;
			return ExecuteCore_UXTH(Encoding.T1, d, m, rotation);
		}

		uint Execute_UXTB_T1()
		{
			//A8.6.263
			uint d = Reg8(0);
			uint m = Reg8(3);
			const uint rotation = 0;
			return ExecuteCore_UXTB(Encoding.T1, d, m, rotation);
		}

		uint Execute_PUSH_T1()
		{
			//A8.6.123
			uint M = _.BIT8(instruction);
			uint regs = (M << 14) | (instruction & 0xFF);
			if (_.BitCount(regs) < 1) unpredictable = true;

			return ExecuteCore_PUSH(Encoding.T1, regs, false);
		}

		uint Execute_LDM_LDMIA_LDMFD_T1()
		{
			//A8.6.53 LDM/LDMIA/LDMFD
			if (_CurrentInstrSet() == EInstrSet.THUMBEE) throw new NotImplementedException();
			uint n = Reg8(8);
			uint registers = instruction & 0xFF;
			bool wback = (_.BITN((int)n, registers) == 0);
			if (registers == 0) _FlagUnpredictable();
			return ExecuteCore_LDM_LDMIA_LDMFD(Encoding.T1, wback, n, registers);
		}

		uint Execute_STM_STMIA_STMEA_T1()
		{
			//A8.6.189 STM/STMIA/STMEA
			if (_CurrentInstrSet() == EInstrSet.THUMBEE) throw new NotImplementedException();
			uint n = Reg8(8);
			uint registers = instruction & 0xFF;
			bool wback = true;
			if (registers == 0) unpredictable = true;
			return ExecuteCore_STM_STMIA_STMEA(Encoding.T1, wback, n, registers);
		}

		uint ExecuteThumb_CondBr_And_SVC()
		{
			uint opcode = (instruction >> 8) & 0xF;
			switch (opcode)
			{
				case _.b0000:
				case _.b0001:
				case _.b0010:
				case _.b0011:
				case _.b0100:
				case _.b0101:
				case _.b0110:
				case _.b0111:
				case _.b1000:
				case _.b1001:
				case _.b1010:
				case _.b1011:
				case _.b1100:
				case _.b1101:
					return Execute_B_T1();

				case _.b1110: return _PERMANENTLY_UNDEFINED();
				case _.b1111: return Execute_SVC_T1();

				default: throw new InvalidOperationException("decode fail");
			}
		}
		uint ExecuteThumb_UnCondBr() { return Execute_Unhandled("ExecuteThumb_UnCondBr"); }

		uint Execute_B_T2()
		{
			//A8.6.16 B
			uint imm11 = instruction & 0x7FF;
			int imm32 = _SignExtend_32(11, imm11 << 1);
			if (_InITBlock() && !_LastInITBlock()) unpredictable = true;
			return ExecuteCore_B(Encoding.T2, imm32);
		}

		uint Execute_B_T1()
		{
			//A8.6.16 B
			uint imm8 = (instruction & 0xFF);
			int imm32 = _SignExtend_32(9, imm8 << 1);
			_currentCondVal = (instruction >> 8) & 0xF;
			if (_InITBlock()) return _UNPREDICTABLE();
			return ExecuteCore_B(Encoding.T1, imm32);
		}

		uint Execute_SVC_T1()
		{
			return Execute_Unhandled("Execute_SVC_T1");
		}

	}
}