namespace BizHawk.Emulation.CPUs.ARM
{
	partial class ARM
	{
		uint ExecuteArm_Unconditional()
		{
			//A5.7

			uint op1 = (instruction & 0xFF00000) >> 20;
			uint op = (instruction & 0x10) >> 4;

			//todo: misc instructions, memory hints, and advanced SIMD instructions on page A5-31
			if (CHK(op1, _.b10000000, _.b00000000)) return ExecuteArm_Unconditional_Misc();
			if (CHK(op1, _.b11100101, _.b10000100)) return Execute_SRS_A1(); //v6
			if (CHK(op1, _.b11100101, _.b10000001)) return Execute_RFE_A1(); //v6
			if (CHK(op1, _.b11100000, _.b10100000)) return Execute_BL_BLX_immediate_A2(); //v5
			if (CHK(op1, _.b11111011, _.b11000011)) return ExecuteArm_LDC_LDC2_immediate(); //v5
			if (CHK(op1, _.b11111001, _.b11000011)) return ExecuteArm_LDC_LDC2_literal(1); //v5
			if (CHK(op1, _.b11110001, _.b11010001)) return ExecuteArm_LDC_LDC2_literal(2); //v5
			if (CHK(op1, _.b11111011, _.b11000010)) return ExecuteArm_STC_STC2(1); //v5
			if (CHK(op1, _.b11111001, _.b11001000)) return ExecuteArm_STC_STC2(2); //v5
			if (CHK(op1, _.b11110001, _.b11010000)) return ExecuteArm_STC_STC2(3); //v5
			if (op1 == _.b11000100) return ExecuteArm_MCRR_MCRR2(); //v6
			if (op1 == _.b11000101) return ExecuteArm_MRRC_MRRC2(); //v6
			if (CHK(op1, _.b11110000, _.b11100000) && op == 0) return ExecuteArm_CDP_CDP2(); //v5
			if (CHK(op1, _.b11110001, _.b11100000) && op == 1) return ExecuteArm_MCR_MCR2(); //v5
			if (CHK(op1, _.b11110001, _.b11100001) && op == 1) return ExecuteArm_MRC_MRC2(); //v5

			return Execute_Unhandled("ExecuteArm_Unconditional");
		}

		uint ExecuteArm_Unconditional_Misc() { return Execute_Unhandled("ExecuteArm_Unconditional_Misc"); }
		uint Execute_SRS_A1() { return Execute_Unhandled("ExecuteArm_SRS_A1"); }
		uint Execute_RFE_A1() { return Execute_Unhandled("ExecuteArm_RFE_A1"); }
		uint Execute_BL_BLX_immediate_A2()
		{
			//A8.6.23
			uint imm24 = instruction & 0xFFFFFF;
			uint H = _.BIT24(instruction);
			int imm32 = SIGNEXTEND_24((imm24 << 2) | (H << 1));
			return ExecuteCore_BL_BLX_immediate(Encoding.A2, EInstrSet.THUMB, imm32, true);
		}



		uint ExecuteArm_LDC_LDC2_immediate()
		{
			uint Rn = instruction & 0xF0000;
			if (Rn == 0xF) { } //special handling for 0xF vs not 0xF
			return Execute_Unhandled("ExecuteArm_LDC_LDC2_immediate");
		}
		uint ExecuteArm_LDC_LDC2_literal(int form) { return Execute_Unhandled("ExecuteArm_LDC_LDC2_literal"); }
		uint ExecuteArm_STC_STC2(int form) { return Execute_Unhandled("ExecuteArm_STC_STC2"); }
		uint ExecuteArm_MCRR_MCRR2() { return Execute_Unhandled("ExecuteArm_MCRR_MCRR2"); }
		uint ExecuteArm_MRRC_MRRC2() { return Execute_Unhandled("ExecuteArm_MRRC_MRRC2"); }
		uint ExecuteArm_CDP_CDP2() { return Execute_Unhandled("ExecuteArm_CDP_CDP2"); }
		uint ExecuteArm_MCR_MCR2() { return Execute_Unhandled("ExecuteArm_MCR_MCR2"); }
		uint ExecuteArm_MRC_MRC2() { return Execute_Unhandled("ExecuteArm_MRC_MRC2"); }
	}

}