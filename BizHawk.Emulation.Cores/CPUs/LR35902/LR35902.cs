using System;
using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

// GameBoy CPU (Sharp LR35902)
namespace BizHawk.Emulation.Common.Components.LR35902
{
	public sealed partial class LR35902
	{
		// operations that can take place in an instruction
		public const ushort IDLE = 0; 
		public const ushort OP = 1;
		public const ushort RD = 2;
		public const ushort WR = 3;
		public const ushort TR = 4;
		public const ushort ADD16 = 5;
		public const ushort ADD8 = 6;
		public const ushort SUB8 = 7;
		public const ushort ADC8 = 8;
		public const ushort SBC8 = 9;
		public const ushort INC16 = 10;
		public const ushort INC8 = 11;
		public const ushort DEC16 = 12;
		public const ushort DEC8 = 13;
		public const ushort RLC = 14;
		public const ushort RL = 15;
		public const ushort RRC = 16;
		public const ushort RR = 17;	
		public const ushort CPL = 18;
		public const ushort DA = 19;
		public const ushort SCF = 20;
		public const ushort CCF = 21;
		public const ushort AND8 = 22;
		public const ushort XOR8 = 23;
		public const ushort OR8 = 24;
		public const ushort CP8 = 25;
		public const ushort SLA = 26;
		public const ushort SRA = 27;
		public const ushort SRL = 28;
		public const ushort SWAP = 29;
		public const ushort BIT = 30;
		public const ushort RES = 31;
		public const ushort SET = 32;		
		public const ushort EI = 33;
		public const ushort DI = 34;
		public const ushort HALT = 35;
		public const ushort STOP = 36;
		public const ushort PREFIX = 37;
		public const ushort ASGN = 38;
		public const ushort ADDS = 39; // signed 16 bit operation used in 2 instructions
		public const ushort OP_G = 40; // glitchy opcode read performed by halt when interrupts disabled
		public const ushort JAM = 41;  // all undocumented opcodes jam the machine
		public const ushort RD_F = 42; // special read case to pop value into F
		public const ushort EI_RETI = 43; // reti has no delay in interrupt enable
		public const ushort INT_GET = 44;

		public LR35902()
		{
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 0;
			cur_instr = new ushort[] { OP };
		}

		// Memory Access 

		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;
		public Func<ushort, byte> PeekMemory;
		public Func<ushort, byte> DummyReadMemory;

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
					if (EI_pending > 0 && !CB_prefix)
					{
						EI_pending--;
						if (EI_pending == 0)
						{
							interrupts_enabled = true;
						}
					}

					if (FlagI && interrupts_enabled && !CB_prefix && !jammed)
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
						if (OnExecFetch != null) OnExecFetch(RegPC);
						if (TraceCallback != null && !CB_prefix) TraceCallback(State());
						FetchInstruction(ReadMemory(RegPC++));
					}
					instr_pntr = 0;
					break;
				case RD:
					Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR:
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TR:
					TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADD16:
					ADD16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
					DEC16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case DEC8:
					DEC8_Func(cur_instr[instr_pntr++]);
					break;
				case RLC:
					RLC_Func(cur_instr[instr_pntr++]);
					break;
				case RL:
					RL_Func(cur_instr[instr_pntr++]);
					break;
				case RRC:
					RRC_Func(cur_instr[instr_pntr++]);
					break;
				case RR:
					RR_Func(cur_instr[instr_pntr++]);
					break;
				case CPL:
					CPL_Func(cur_instr[instr_pntr++]);
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
				case SLA:
					SLA_Func(cur_instr[instr_pntr++]);
					break;
				case SRA:
					SRA_Func(cur_instr[instr_pntr++]);
					break;
				case SRL:
					SRL_Func(cur_instr[instr_pntr++]);
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
					halted = true;

					if (EI_pending > 0 && !CB_prefix)
					{
						EI_pending--;
						if (EI_pending == 0)
						{
							interrupts_enabled = true;
						}
					}

					// if the I flag is asserted at the time of halt, don't halt

					if (FlagI && interrupts_enabled && !CB_prefix && !jammed)
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
						halted = false;
						// call interrupt processor 
						INTERRUPT_();
					}
					else if (FlagI)
					{
						// even if interrupt servicing is disabled, any interrupt flag raised still resumes execution
						if (TraceCallback != null)
						{
							TraceCallback(new TraceInfo
							{
								Disassembly = "====un-halted====",
								RegisterInfo = ""
							});
						}
						halted = false;
						if (OnExecFetch != null) OnExecFetch(RegPC);
						if (TraceCallback != null && !CB_prefix) TraceCallback(State());
						FetchInstruction(ReadMemory(RegPC++));
					}
					else
					{
						cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						HALT };
					}
					instr_pntr = 0;
					break;
				case STOP:
					stopped = true;

					if (interrupt_src.Bit(4)) // button pressed, not actually an interrupt though
					{
						if (TraceCallback != null)
						{
							TraceCallback(new TraceInfo
							{
								Disassembly = "====un-stop====",
								RegisterInfo = ""
							});
						}

						stopped = false;
						if (OnExecFetch != null) OnExecFetch(RegPC);
						if (TraceCallback != null && !CB_prefix) TraceCallback(State());
						FetchInstruction(ReadMemory(RegPC++));
						instr_pntr = 0;
					}
					else
					{
						instr_pntr = 0;
						cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						STOP };
					}
					break;
				case PREFIX:
					CB_prefix = true;
					break;
				case ASGN:
					ASGN_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADDS:
					ADDS_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case OP_G:
					if (OnExecFetch != null) OnExecFetch(RegPC);
					if (TraceCallback != null) TraceCallback(State());

					FetchInstruction(ReadMemory(RegPC)); // note no increment

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
					// check if any interrupts got cancelled along the way
					// interrupt src = 5 sets the PC to zero as observed

					if (interrupt_src.Bit(0) && interrupt_enable.Bit(0)) { int_src = 0; interrupt_src -= 1; }
					else if (interrupt_src.Bit(1) && interrupt_enable.Bit(1)) { int_src = 1; interrupt_src -= 2; }
					else if (interrupt_src.Bit(2) && interrupt_enable.Bit(2)) { int_src = 2; interrupt_src -= 4; }
					else if (interrupt_src.Bit(3) && interrupt_enable.Bit(3)) { int_src = 3; interrupt_src -= 8; }
					else if (interrupt_src.Bit(4) && interrupt_enable.Bit(4)) { int_src = 4; interrupt_src -= 16; }
					else { int_src = 5; }
						
					if ((interrupt_src & interrupt_enable) == 0) { FlagI = false; }

					Regs[cur_instr[instr_pntr++]] = INT_vectors[int_src];
					break;
			}
			totalExecutedCycles++;
		}

		// tracer stuff

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader
		{
			get { return "LR35902: PC, machine code, mnemonic, operands, registers (A, F, B, C, D, E, H, L, SP), Cy, flags (ZNHCI)"; }
		}

		public TraceInfo State(bool disassemble = true)
		{
			ushort notused;

			return new TraceInfo
			{
				Disassembly = string.Format(
					"{0} ",
					disassemble ? Disassemble(RegPC, ReadMemory, out notused) : "---").PadRight(40),
				RegisterInfo = string.Format(
					"A:{0:X2} F:{1:X2} B:{2:X2} C:{3:X2} D:{4:X2} E:{5:X2} H:{6:X2} L:{7:X2} SP:{8:X2} Cy:{9} LY:{10} {11}{12}{13}{14}{15}{16}",
					Regs[A],
					Regs[F],
					Regs[B],
					Regs[C],
					Regs[D],
					Regs[E],
					Regs[H],
					Regs[L],
					Regs[SPl] | (Regs[SPh] << 8),
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
		// State Save/Load

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("LR35902");
			ser.Sync("Regs", ref Regs, false);
			ser.Sync("IRQ", ref interrupts_enabled);
			ser.Sync("NMI", ref nonMaskableInterrupt);
			ser.Sync("NMIPending", ref nonMaskableInterruptPending);
			ser.Sync("IM", ref interruptMode);
			ser.Sync("IFF1", ref iff1);
			ser.Sync("IFF2", ref iff2);
			ser.Sync("Halted", ref halted);
			ser.Sync("ExecutedCycles", ref totalExecutedCycles);
			ser.Sync("EI_pending", ref EI_pending);
			ser.Sync("int_src", ref int_src);

			ser.Sync("instruction_pointer", ref instr_pntr);
			ser.Sync("current instruction", ref cur_instr, false);
			ser.Sync("CB Preifx", ref CB_prefix);
			ser.Sync("Stopped", ref stopped);
			ser.Sync("opcode", ref opcode);
			ser.Sync("jammped", ref jammed);
			ser.Sync("LY", ref LY);
			ser.Sync("FlagI", ref FlagI);

			ser.EndSection();
		}
	}
}