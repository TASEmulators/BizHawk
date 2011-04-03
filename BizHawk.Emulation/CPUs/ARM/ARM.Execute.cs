using System;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.ARM
{

	partial class ARM
	{
		public uint Execute()
		{
			if (APSR.T == 1) return ExecuteThumb();
			else return ExecuteArm();
		}

		enum CC
		{
			EQ = 0,
			NE = 1,
			CS = 2, HS = 2,
			CC = 3, LO = 3,
			MI = 4,
			PL = 5,
			VS = 6,
			VC = 7,
			HI = 8,
			LS = 9,
			GE = 10,
			LT = 11,
			GT = 12,
			LE = 13,
			AL = 14,
		}
		//return (CPSR.N?0x80000000:0)|(CPSR.Z?0x40000000:0)|(CPSR.C?0x20000000:0)|(CPSR.V?0x10000000:0);

		readonly string[] CC_strings = { "EQ", "NE", "CS", "CC", "MI", "PL", "VS", "VC", "HI", "LS", "GE", "LT", "GT", "LE", "AL", "XX" };
		readonly string[] Reg_names = { "R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "R10", "R11", "R12", "SP", "LR", "PC" };

		uint CONDITION(uint i) { return i >> 28; }
		int SIGNEXTEND_24(uint x) { return (((int)x << 8) >> 8); }

		//an instruction sets this flag when the results are unpredictable but will be executed anyway
		public bool unpredictable;
		public uint cycles;
		uint _currentCondVal;
		uint _CurrentCond()
		{
			//TODO - calculate CurrentCond from from A8.3.1
			return _currentCondVal;
		}
		bool _ConditionPassed()
		{
			uint cond = _CurrentCond();
			bool result = false;
			switch (cond & _.b1110)
			{
				case _.b0000: result = APSR.Z == 1; break;
				case _.b0010: result = APSR.C == 1; break;
				case _.b0100: result = APSR.N == 1; break;
				case _.b0110: result = APSR.V == 1; break;
				case _.b1000: result = APSR.C == 1 && APSR.Z == 0; break;
				case _.b1010: result = APSR.N == APSR.V; break;
				case _.b1100: result = (APSR.N == APSR.V) && APSR.Z == 0; break;
				case _.b1110: result = true; break;
			}
			if ((cond & 1) == 1 && cond != _.b1111)
				result = !result;
			return result;
		}


		bool CHK(uint value, int mask, int test)
		{
			return (value & mask) == test;
		}

		uint Reg8(uint pos)
		{
			return (uint)((instruction >> (int)pos) & 7);
		}

		uint Reg16(uint pos)
		{
			return (uint)((instruction >> (int)pos) & 0xF);
		}

		uint Execute_Undefined()
		{
			if (disassemble) disassembly = "undefined";
			return 0;
		}

		uint Execute_Unhandled(string descr)
		{
			if (disassemble) disassembly = descr + " unhandled";
			return 1;
		}

		uint Disassembly_Unhandled(string descr)
		{
			disassembly = "disasm unhandled: " + descr;
			return 1;
		}

		uint Disassemble_LDR_STR_immediate(string opcode, uint t, uint imm32, uint n, bool index, bool add, bool wback)
		{
			bool offset = index == true && wback == false;
			bool preindexed = index == true && wback == true;
			bool postindex = index == false && wback == true;

			string rt = "<rt!>";
			opcode += "<c>";
			//we may want conditional logic here to control whether various registers are displayed (we used to have it, but i changed my mind)
			if (offset)
				return DISNEW(opcode, rt + ", [<Rn!><{,+/-#imm}>]", t, n, add, imm32);
			else if (preindexed)
				return DISNEW(opcode, rt + ", [<Rn!>, <+/-><imm>]!", t, n, add, imm32);
			else if (postindex)
				return DISNEW(opcode, rt + ", [<Rn!>], <+/-><imm>", t, n, add, imm32);
			else throw new InvalidOperationException();
		}

		uint Disassemble_LDR_STR_register(string opcode, uint t, uint n, uint m, SRType shift_t, int shift_n, bool index, bool add, bool wback)
		{
			bool offset = index == true && wback == false;
			bool preindexed = index == true && wback == true;
			bool postindex = index == false && wback == true;

			string rt = "<rt!>";
			opcode += "<c>";
			//we may want conditional logic here to control whether various registers are displayed (we used to have it, but i changed my mind)
			if (offset)
				return DISNEW(opcode, rt + ", [<Rn!>, <+/-><rm!><{, shift}>]", t, n, add, m, shift_t, shift_n);
			else if (preindexed)
				return DISNEW(opcode, rt + ", [<Rn!>, <+/-><rm!><{, shift}>]!", t, n, add, m, shift_t, shift_n);
			else if (postindex)
				return DISNEW(opcode, rt + ", [<Rn!>], <+/-><rm!><{, shift}>", t, n, add, m, shift_t, shift_n);
			else throw new InvalidOperationException();
		}

		void UnalignedAccess(uint addr)
		{
			Console.WriteLine("Warning! unaligned access at {0:x8}", addr);
		}
	}

}