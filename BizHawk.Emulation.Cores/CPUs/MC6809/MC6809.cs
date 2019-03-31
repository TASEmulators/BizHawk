using System;
using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

// Motorola Corp 6809
namespace BizHawk.Emulation.Common.Components.MC6809
{
	public sealed partial class MC6809
	{
		// operations that can take place in an instruction
		public const ushort IDLE = 0; 
		public const ushort OP = 1;
		public const ushort RD = 2;
		public const ushort WR = 3;
		public const ushort TR = 4;
		public const ushort ADD16BR = 5;
		public const ushort ADD8 = 6;
		public const ushort SUB8 = 7;
		public const ushort ADC8 = 8;
		public const ushort SBC8 = 9;
		public const ushort INC16 = 10;
		public const ushort INC8 = 11;
		public const ushort DEC16 = 12;
		public const ushort DEC8 = 13;
		public const ushort RLC = 14;
		public const ushort ROL = 15;
		public const ushort RRC = 16;
		public const ushort ROR = 17;
		public const ushort COM = 18;
		public const ushort DA = 19;
		public const ushort SCF = 20;
		public const ushort CCF = 21;
		public const ushort AND8 = 22;
		public const ushort XOR8 = 23;
		public const ushort OR8 = 24;
		public const ushort CP8 = 25;
		public const ushort ASL = 26;
		public const ushort ASR = 27;
		public const ushort LSR = 28;
		public const ushort SWAP = 29;
		public const ushort BIT = 30;
		public const ushort RES = 31;
		public const ushort SET = 32;
		public const ushort EI = 33;
		public const ushort DI = 34;
		public const ushort HALT = 35;
		public const ushort STOP = 36;
		public const ushort ASGN = 38;
		public const ushort ADDS = 39; // signed 16 bit operation used in 2 instructions
		public const ushort OP_G = 40; // glitchy opcode read performed by halt when interrupts disabled
		public const ushort JAM = 41;  // all undocumented opcodes jam the machine
		public const ushort RD_F = 42; // special read case to pop value into F
		public const ushort EI_RETI = 43; // reti has no delay in interrupt enable
		public const ushort INT_GET = 44;
		public const ushort HALT_CHK = 45; // when in halt mode, actually check I Flag here
		public const ushort RD_INC = 46;
		public const ushort SET_ADDR = 47;
		public const ushort NEG = 48;
		public const ushort TST = 49;
		public const ushort CLR = 50;
		public const ushort OP_PG_2 = 51;
		public const ushort OP_PG_3 = 52;
		public const ushort SEX = 53;
		public const ushort RD_INC_OP = 54;
		public const ushort EXG = 55;
		public const ushort TFR = 56;
		public const ushort WR_DEC_LO = 57;
		public const ushort WR_HI = 58;
		public const ushort ADD8BR = 59;
		public const ushort ABX = 60;
		public const ushort MUL = 61;
		public const ushort JPE = 62;

		public MC6809()
		{
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 8;
			stop_check = false;
			cur_instr = new ushort[] { IDLE, IDLE, HALT_CHK, OP };
		}

		// Memory Access 

		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;
		public Func<ushort, byte> PeekMemory;
		public Func<ushort, byte> DummyReadMemory;

		// Special Function for Speed switching executed on a STOP
		public Func<int, int> SpeedFunc;

		//this only calls when the first byte of an instruction is fetched.
		public Action<ushort> OnExecFetch;

		public void UnregisterMemoryMapper()
		{
			ReadMemory = null;
			ReadMemory = null;
			PeekMemory = null;
			DummyReadMemory = null;
		}

		public void SetCallbacks
		(
			Func<ushort, byte> ReadMemory,
			Func<ushort, byte> DummyReadMemory,
			Func<ushort, byte> PeekMemory,
			Action<ushort, byte> WriteMemory
		)
		{
			this.ReadMemory = ReadMemory;
			this.DummyReadMemory = DummyReadMemory;
			this.PeekMemory = PeekMemory;
			this.WriteMemory = WriteMemory;
		}

		//a little CDL related stuff
		public delegate void DoCDLCallbackType(ushort addr, MC6809.eCDLogMemFlags flags);

		public DoCDLCallbackType CDLCallback;

		public enum eCDLogMemFlags
		{
			FetchFirst = 1,
			FetchOperand = 2,
			Data = 4,
			Write = 8
		};

		// Execute instructions
		public void ExecuteOne(ref byte interrupt_src, byte interrupt_enable)
		{
			switch (cur_instr[instr_pntr++])
			{
				case IDLE:
					// do nothing
					break;
				case OP:
					// Read the opcode of the next instruction
					if (EI_pending > 0)
					{
						EI_pending--;
						if (EI_pending == 0)
						{
							interrupts_enabled = true;
						}
					}

					if (I_use && interrupts_enabled && !jammed)
					{
						interrupts_enabled = false;

						if (TraceCallback != null)
						{
							TraceCallback(new TraceInfo
							{
								Disassembly = "====IRQ====",
								RegisterInfo = ""
							});
						}

						// call interrupt processor 
						// lowest bit set is highest priority
						INTERRUPT_();
					}
					else
					{
						if (OnExecFetch != null) OnExecFetch(PC);
						if (TraceCallback != null) TraceCallback(State());
						if (CDLCallback != null) CDLCallback(PC, eCDLogMemFlags.FetchFirst);
						FetchInstruction(ReadMemory(Regs[PC]++));
					}
					instr_pntr = 0;
					I_use = false;
					break;

				case OP_PG_2:
					FetchInstruction2(ReadMemory(Regs[PC]++));
					break;
				case OP_PG_3:
					FetchInstruction3(ReadMemory(Regs[PC]++));
					break;
				case RD:
					Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_INC:
					Read_Inc_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_INC_OP:
					Read_Inc_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					switch (cur_instr[instr_pntr++])
					{
						case AND8:
							AND8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case OR8:
							OR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case SUB8:
							SUB8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case SET_ADDR:
							Regs[cur_instr[instr_pntr++]] = (ushort)((Regs[cur_instr[instr_pntr++]] << 8) | Regs[cur_instr[instr_pntr++]]);
							break;
						case JPE:
							if (!FlagE) { instr_pntr = 35; };
							break;
					}
					break;
				case WR:
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_DEC_LO:
					Write_Dec_Lo_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_HI:
					Write_Hi_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TR:
					TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case EXG:
					EXG_Func(cur_instr[instr_pntr++]);
					break;
				case TFR:
					TFR_Func(cur_instr[instr_pntr++]);
					break;
				case SET_ADDR:
					Regs[cur_instr[instr_pntr++]] = (ushort)((Regs[cur_instr[instr_pntr++]] << 8) | Regs[cur_instr[instr_pntr++]]);
					break;
				case NEG:
					NEG_8_Func(cur_instr[instr_pntr++]);
					break;
				case TST:
					TST_Func(cur_instr[instr_pntr++]);
					break;
				case CLR:
					CLR_Func(cur_instr[instr_pntr++]);
					break;
				case SEX:
					SEX_Func(cur_instr[instr_pntr++]);
					break;
				case ABX:
					Regs[X] += Regs[B];
					break;
				case MUL:
					Mul_Func();
					break;
				case ADD16BR:
					ADD16BR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADD8BR:
					ADD8BR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADD8:
					ADD8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SUB8:
					SUB8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADC8:
					ADC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SBC8:
					SBC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case INC16:
					INC16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case INC8:
					INC8_Func(cur_instr[instr_pntr++]);
					break;
				case DEC16:
					DEC16_Func(cur_instr[instr_pntr++]);
					break;
				case DEC8:
					DEC8_Func(cur_instr[instr_pntr++]);
					break;
				case RLC:
					RLC_Func(cur_instr[instr_pntr++]);
					break;
				case ROL:
					ROL_Func(cur_instr[instr_pntr++]);
					break;
				case RRC:
					RRC_Func(cur_instr[instr_pntr++]);
					break;
				case ROR:
					ROR_Func(cur_instr[instr_pntr++]);
					break;
				case COM:
					COM_Func(cur_instr[instr_pntr++]);
					break;
				case DA:
					DA_Func(cur_instr[instr_pntr++]);
					break;
				case SCF:
					SCF_Func(cur_instr[instr_pntr++]);
					break;
				case CCF:
					CCF_Func(cur_instr[instr_pntr++]);
					break;
				case AND8:
					AND8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case XOR8:
					XOR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case OR8:
					OR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case CP8:
					CP8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ASL:
					ASL_Func(cur_instr[instr_pntr++]);
					break;
				case ASR:
					ASR_Func(cur_instr[instr_pntr++]);
					break;
				case LSR:
					LSR_Func(cur_instr[instr_pntr++]);
					break;
				case SWAP:
					SWAP_Func(cur_instr[instr_pntr++]);
					break;
				case BIT:
					BIT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RES:
					RES_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SET:
					SET_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case EI:
					if (EI_pending == 0) { EI_pending = 2; }
					break;
				case DI:
					interrupts_enabled = false;
					EI_pending = 0;
					break;
				case HALT:

					break;
				case STOP:

					break;
				case ASGN:
					ASGN_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADDS:
					ADDS_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case OP_G:
					if (OnExecFetch != null) OnExecFetch(PC);
					if (TraceCallback != null) TraceCallback(State());
					if (CDLCallback != null) CDLCallback(PC, eCDLogMemFlags.FetchFirst);

					FetchInstruction(ReadMemory(PC)); // note no increment

					instr_pntr = 0;
					break;
				case JAM:
					jammed = true;
					instr_pntr--;
					break;
				case RD_F:
					Read_Func_F(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case EI_RETI:
					EI_pending = 1;
					break;
				case INT_GET:

					break;
				case HALT_CHK:

					break;
			}
			totalExecutedCycles++;
		}

		// tracer stuff

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader
		{
			get { return "MC6809: PC, machine code, mnemonic, operands, registers (A, F, B, C, D, E, H, L, SP), Cy, flags (ZNHCI)"; }
		}

		public TraceInfo State(bool disassemble = true)
		{
			ushort notused;

			return new TraceInfo
			{
				Disassembly = string.Format(
					"{0} ",
					disassemble ? Disassemble(PC, ReadMemory, out notused) : "---").PadRight(40),
				RegisterInfo = string.Format(
					"A:{0:X2} F:{1:X2} B:{2:X2} C:{3:X2} D:{4:X2} E:{5:X2} H:{6:X2} L:{7:X2}",

					TotalExecutedCycles,
					LY,
					FlagZ ? "Z" : "z",
					FlagN ? "N" : "n",
					FlagH ? "H" : "h",
					FlagC ? "C" : "c",			
					FlagI ? "I" : "i",
					interrupts_enabled ? "E" : "e")
			};
		}

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		private void PopulateCURINSTR(ushort d0 = 0, ushort d1 = 0, ushort d2 = 0, ushort d3 = 0, ushort d4 = 0, ushort d5 = 0, ushort d6 = 0, ushort d7 = 0, ushort d8 = 0,
			ushort d9 = 0, ushort d10 = 0, ushort d11 = 0, ushort d12 = 0, ushort d13 = 0, ushort d14 = 0, ushort d15 = 0, ushort d16 = 0, ushort d17 = 0, ushort d18 = 0,
			ushort d19 = 0, ushort d20 = 0, ushort d21 = 0, ushort d22 = 0, ushort d23 = 0, ushort d24 = 0, ushort d25 = 0, ushort d26 = 0, ushort d27 = 0, ushort d28 = 0,
			ushort d29 = 0, ushort d30 = 0, ushort d31 = 0, ushort d32 = 0, ushort d33 = 0, ushort d34 = 0, ushort d35 = 0, ushort d36 = 0, ushort d37 = 0, ushort d38 = 0,
			ushort d39 = 0, ushort d40 = 0, ushort d41 = 0, ushort d42 = 0, ushort d43 = 0, ushort d44 = 0, ushort d45 = 0, ushort d46 = 0, ushort d47 = 0, ushort d48 = 0)
		{
			cur_instr[0] = d0; cur_instr[1] = d1; cur_instr[2] = d2;
			cur_instr[3] = d3; cur_instr[4] = d4; cur_instr[5] = d5;
			cur_instr[6] = d6; cur_instr[7] = d7; cur_instr[8] = d8;
			cur_instr[9] = d9; cur_instr[10] = d10; cur_instr[11] = d11;
			cur_instr[12] = d12; cur_instr[13] = d13; cur_instr[14] = d14;
			cur_instr[15] = d15; cur_instr[16] = d16; cur_instr[17] = d17;
			cur_instr[18] = d18; cur_instr[19] = d19; cur_instr[20] = d20;
			cur_instr[21] = d21; cur_instr[22] = d22; cur_instr[23] = d23;
			cur_instr[24] = d24; cur_instr[25] = d25; cur_instr[26] = d26;
			cur_instr[27] = d27; cur_instr[28] = d28; cur_instr[29] = d29;
			cur_instr[30] = d30; cur_instr[31] = d31; cur_instr[32] = d32;
			cur_instr[33] = d33; cur_instr[34] = d34; cur_instr[35] = d35;
			cur_instr[36] = d36; cur_instr[37] = d37;
		}

		// State Save/Load
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MC6809");
			ser.Sync("IRQ", ref interrupts_enabled);
			ser.Sync("I_use", ref I_use);
			ser.Sync("skip_once", ref skip_once);
			ser.Sync("Halt_bug_2", ref Halt_bug_2);
			ser.Sync("Halt_bug_3", ref Halt_bug_3);
			ser.Sync("Halted", ref halted);
			ser.Sync("ExecutedCycles", ref totalExecutedCycles);
			ser.Sync("EI_pending", ref EI_pending);
			ser.Sync("int_src", ref int_src);
			ser.Sync("stop_time", ref stop_time);
			ser.Sync("stop_check", ref stop_check);
			ser.Sync("is_GBC", ref is_GBC);

			ser.Sync("instr_pntr", ref instr_pntr);
			ser.Sync("cur_instr", ref cur_instr, false);
			ser.Sync("Stopped", ref stopped);
			ser.Sync("opcode", ref opcode);
			ser.Sync("jammped", ref jammed);
			ser.Sync("LY", ref LY);

			ser.EndSection();
		}
	}
}