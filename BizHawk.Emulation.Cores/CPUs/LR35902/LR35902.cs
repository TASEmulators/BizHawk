using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

// GameBoy CPU (Sharp LR35902)
namespace BizHawk.Emulation.Cores.Components.LR35902
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
		public const ushort HALT_CHK = 45; // when in halt mode, actually check I Flag here
		public const ushort IRQ_CLEAR = 46;
		public const ushort COND_CHECK = 47;
		public const ushort HALT_FUNC = 48;

		// test conditions
		public const ushort ALWAYS_T = 0;
		public const ushort ALWAYS_F = 1;
		public const ushort FLAG_Z = 2;
		public const ushort FLAG_NZ = 3;
		public const ushort FLAG_C = 4;
		public const ushort FLAG_NC = 5;

		public LR35902()
		{
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			BuildInstructionTable();
			TotalExecutedCycles = 8;
			stop_check = false;
			instr_pntr = 256 * 60 * 2; // point to reset
			stopped = jammed = halted = FlagI = false;
			EI_pending = 0;
			CB_prefix = false;
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
		public delegate void DoCDLCallbackType(ushort addr, LR35902.eCDLogMemFlags flags);

		public DoCDLCallbackType CDLCallback;

		public enum eCDLogMemFlags
		{
			FetchFirst = 1,
			FetchOperand = 2,
			Data = 4,
			Write = 8
		}

		// Execute instructions
		public void ExecuteOne(ref byte interrupt_src, byte interrupt_enable)
		{
			switch (instr_table[instr_pntr++])
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

					if (I_use && interrupts_enabled && !CB_prefix && !jammed)
					{
						interrupts_enabled = false;

						TraceCallback?.Invoke(new TraceInfo
						{
							Disassembly = "====IRQ====",
							RegisterInfo = ""
						});

						// call interrupt processor 
						// lowest bit set is highest priority
						instr_pntr = 256 * 60 * 2 + 60 * 6; // point to Interrupt
					}
					else
					{
						OnExecFetch?.Invoke(RegPC);
						if (TraceCallback != null && !CB_prefix) TraceCallback(State());
						CDLCallback?.Invoke(RegPC, eCDLogMemFlags.FetchFirst);
						FetchInstruction(ReadMemory(RegPC++));
					}
					I_use = false;
					break;
				case RD:
					Read_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1], instr_table[instr_pntr + 2]);
					instr_pntr += 3;
					break;
				case WR:
					Write_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1], instr_table[instr_pntr + 2]);
					instr_pntr += 3;
					break;
				case TR:
					TR_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case ADD16:
					ADD16_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1], instr_table[instr_pntr + 2], instr_table[instr_pntr + 3]);
					instr_pntr += 4;
					break;
				case ADD8:
					ADD8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case SUB8:
					SUB8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case ADC8:
					ADC8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case SBC8:
					SBC8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case INC16:
					INC16_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case INC8:
					INC8_Func(instr_table[instr_pntr++]);
					break;
				case DEC16:
					DEC16_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case DEC8:
					DEC8_Func(instr_table[instr_pntr++]);
					break;
				case RLC:
					RLC_Func(instr_table[instr_pntr++]);
					break;
				case RL:
					RL_Func(instr_table[instr_pntr++]);
					break;
				case RRC:
					RRC_Func(instr_table[instr_pntr++]);
					break;
				case RR:
					RR_Func(instr_table[instr_pntr++]);
					break;
				case CPL:
					CPL_Func(instr_table[instr_pntr++]);
					break;
				case DA:
					DA_Func(instr_table[instr_pntr++]);
					break;
				case SCF:
					SCF_Func(instr_table[instr_pntr++]);
					break;
				case CCF:
					CCF_Func(instr_table[instr_pntr++]);
					break;
				case AND8:
					AND8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case XOR8:
					XOR8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case OR8:
					OR8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case CP8:
					CP8_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case SLA:
					SLA_Func(instr_table[instr_pntr++]);
					break;
				case SRA:
					SRA_Func(instr_table[instr_pntr++]);
					break;
				case SRL:
					SRL_Func(instr_table[instr_pntr++]);
					break;
				case SWAP:
					SWAP_Func(instr_table[instr_pntr++]);
					break;
				case BIT:
					BIT_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case RES:
					RES_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case SET:
					SET_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
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

					bool temp = false;

					if (instr_table[instr_pntr++] == 1)
					{
						temp = FlagI;
					}
					else
					{
						temp = I_use;
					}

					if (EI_pending > 0 && !CB_prefix)
					{
						EI_pending--;
						if (EI_pending == 0)
						{
							interrupts_enabled = true;
						}
					}

					// if the I flag is asserted at the time of halt, don't halt
					if (temp && interrupts_enabled && !CB_prefix && !jammed)
					{
						interrupts_enabled = false;

						TraceCallback?.Invoke(new TraceInfo
						{
							Disassembly = "====IRQ====",
							RegisterInfo = ""
						});
						halted = false;
						
						if (is_GBC)
						{
							// call the interrupt processor after 4 extra cycles
							if (!Halt_bug_3)
							{
								instr_pntr = 256 * 60 * 2 + 60 * 7; // point to Interrupt for GBC
							}
							else
							{
								instr_pntr = 256 * 60 * 2 + 60 * 6; // point to Interrupt
								Halt_bug_3 = false;
								//Console.WriteLine("Hit INT");
							}
						}
						else
						{
							// call interrupt processor
							instr_pntr = 256 * 60 * 2 + 60 * 6; // point to Interrupt
							Halt_bug_3 = false;
						}
					}
					else if (temp)
					{
						// even if interrupt servicing is disabled, any interrupt flag raised still resumes execution
						TraceCallback?.Invoke(new TraceInfo
						{
							Disassembly = "====un-halted====",
							RegisterInfo = ""
						});
						halted = false;

						if (is_GBC)
						{
							// extra 4 cycles for GBC
							if (Halt_bug_3)
							{
								OnExecFetch?.Invoke(RegPC);
								if (TraceCallback != null && !CB_prefix) TraceCallback(State());
								CDLCallback?.Invoke(RegPC, eCDLogMemFlags.FetchFirst);

								RegPC++;
								FetchInstruction(ReadMemory(RegPC));
								Halt_bug_3 = false;
								//Console.WriteLine("Hit un");
							}
							else
							{
								instr_pntr = 256 * 60 * 2 + 60; // point to halt loop
							}
						}
						else
						{
							OnExecFetch?.Invoke(RegPC);
							if (TraceCallback != null && !CB_prefix) TraceCallback(State());
							CDLCallback?.Invoke(RegPC, eCDLogMemFlags.FetchFirst);

							if (Halt_bug_3)
							{
								//special variant of halt bug where RegPC also isn't incremented post fetch
								RegPC++;
								FetchInstruction(ReadMemory(RegPC));
								Halt_bug_3 = false;
							}
							else
							{
								FetchInstruction(ReadMemory(RegPC++));
							}
						}
					}
					else
					{
						if (skip_once)
						{
							instr_pntr = 256 * 60 * 2 + 60 * 2; // point to skipped loop
							skip_once = false;
						}
						else
						{
							if (is_GBC)
							{
								instr_pntr = 256 * 60 * 2 + 60 * 3; // point to GBC Halt loop
							}
							else
							{
								instr_pntr = 256 * 60 * 2 + 60 * 4; // point to spec Halt loop
							}
						}					
					}
					I_use = false;
					break;
				case STOP:
					stopped = true;
					if (!stop_check)
					{
						stop_time = SpeedFunc(0);
						stop_check = true;
					}
					
					if (stop_time > 0)
					{
						stop_time--;
						if (stop_time == 0)
						{
							TraceCallback?.Invoke(new TraceInfo
							{
								Disassembly = "====un-stop====",
								RegisterInfo = ""
							});

							stopped = false;
							OnExecFetch?.Invoke(RegPC);
							if (TraceCallback != null && !CB_prefix) TraceCallback(State());
							CDLCallback?.Invoke(RegPC, eCDLogMemFlags.FetchFirst);
							FetchInstruction(ReadMemory(RegPC++));

							stop_check = false;
						}
						else
						{
							instr_pntr = 256 * 60 * 2 + 60 * 5; // point to stop loop
						}
					}
					else if (interrupt_src.Bit(4)) // button pressed, not actually an interrupt though
					{
						TraceCallback?.Invoke(new TraceInfo
						{
							Disassembly = "====un-stop====",
							RegisterInfo = ""
						});

						stopped = false;
						OnExecFetch?.Invoke(RegPC);
						if (TraceCallback != null && !CB_prefix) TraceCallback(State());
						CDLCallback?.Invoke(RegPC, eCDLogMemFlags.FetchFirst);
						FetchInstruction(ReadMemory(RegPC++));

						stop_check = false;
					}
					else
					{
						instr_pntr = 256 * 60 * 2 + 60 * 5; // point to stop loop
					}
					break;
				case PREFIX:
					CB_prefix = true;
					break;
				case ASGN:
					ASGN_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1]);
					instr_pntr += 2;
					break;
				case ADDS:
					ADDS_Func(instr_table[instr_pntr], instr_table[instr_pntr + 1], instr_table[instr_pntr + 2], instr_table[instr_pntr + 3]);
					instr_pntr += 4;
					break;
				case OP_G:
					OnExecFetch?.Invoke(RegPC);
					TraceCallback?.Invoke(State());
					CDLCallback?.Invoke(RegPC, eCDLogMemFlags.FetchFirst);

					FetchInstruction(ReadMemory(RegPC)); // note no increment
					break;
				case JAM:
					jammed = true;
					instr_pntr--;
					break;
				case RD_F:
					Read_Func_F(instr_table[instr_pntr], instr_table[instr_pntr + 1], instr_table[instr_pntr + 2]);
					instr_pntr += 3;
					break;
				case EI_RETI:
					EI_pending = 1;
					break;
				case INT_GET:
					// check if any interrupts got cancelled along the way
					// interrupt src = 5 sets the PC to zero as observed
					// also the triggering interrupt seems like it is held low (i.e. cannot trigger I flag) until the interrupt is serviced
					ushort bit_check = instr_table[instr_pntr++];
					//Console.WriteLine(interrupt_src + " " + interrupt_enable + " " + TotalExecutedCycles);

					if (interrupt_src.Bit(bit_check) && interrupt_enable.Bit(bit_check)) { int_src = bit_check; int_clear = (byte)(1 << bit_check); }
					/*
					if (interrupt_src.Bit(0) && interrupt_enable.Bit(0)) { int_src = 0; int_clear = 1; }
					else if (interrupt_src.Bit(1) && interrupt_enable.Bit(1)) { int_src = 1; int_clear = 2; }
					else if (interrupt_src.Bit(2) && interrupt_enable.Bit(2)) { int_src = 2; int_clear = 4; }
					else if (interrupt_src.Bit(3) && interrupt_enable.Bit(3)) { int_src = 3; int_clear = 8; }
					else if (interrupt_src.Bit(4) && interrupt_enable.Bit(4)) { int_src = 4; int_clear = 16; }
					else { int_src = 5; int_clear = 0; }
					*/
					Regs[instr_table[instr_pntr++]] = INT_vectors[int_src];
					break;
				case HALT_CHK:
					I_use = FlagI;
					if (Halt_bug_2 && I_use)
					{
						RegPC--;
						Halt_bug_3 = true;
						//Console.WriteLine("Halt_bug_3");
						//Console.WriteLine(TotalExecutedCycles);
					}
					
					Halt_bug_2 = false;
					break;
				case IRQ_CLEAR:
					if (interrupt_src.Bit(int_src)) { interrupt_src -= int_clear; }

					if ((interrupt_src & interrupt_enable) == 0) { FlagI = false; }

					// reset back to default state
					int_src = 5;
					int_clear = 0;
					break;
				case COND_CHECK:
					checker = false;
					switch (instr_table[instr_pntr++])
					{
						case ALWAYS_T:
							checker = true;
							break;
						case ALWAYS_F:
							checker = false;
							break;
						case FLAG_Z:
							checker = FlagZ;
							break;
						case FLAG_NZ:
							checker = !FlagZ;
							break;
						case FLAG_C:
							checker = FlagC;
							break;
						case FLAG_NC:
							checker = !FlagC;
							break;
					}

					// true condition is what is represented in the instruction vectors	
					// jump ahead if false
					if (checker)
					{
						instr_pntr++;
					}
					else
					{
						// 0 = JR COND, 1 = JP COND, 2 = RET COND, 3 = CALL
						switch (instr_table[instr_pntr++])
						{
							case 0:
								instr_pntr += 10;
								break;
							case 1:
								instr_pntr += 8;
								break;
							case 2:
								instr_pntr += 22;
								break;
							case 3:
								instr_pntr += 26;
								break;
						}
					}
					break;
				case HALT_FUNC:
					if (was_FlagI && (EI_pending == 0) && !interrupts_enabled)
					{
						// in GBC mode, the HALT bug is worked around by simply adding a NOP
						// so it just takes 4 cycles longer to reach the next instruction

						// otherwise, if interrupts are disabled,
						// a glitchy decrement to the program counter happens

						// either way, nothing needs to be done here
					}
					else
					{
						instr_pntr += 3;

						if (!is_GBC) { skip_once = true; }
						// If the interrupt flag is not currently set, but it does get set in the first check
						// then a bug is triggered 
						// With interrupts enabled, this runs the halt command twice 
						// when they are disabled, it reads the next byte twice
						if (!was_FlagI || (was_FlagI && !interrupts_enabled)) { Halt_bug_2 = true; }

					}
					break;
			}
			TotalExecutedCycles++;
		}

		// tracer stuff

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader => "LR35902: PC, machine code, mnemonic, operands, registers (A, F, B, C, D, E, H, L, SP), Cy, flags (ZNHCI)";

		public TraceInfo State(bool disassemble = true)
		{
			ushort notused;

			return new TraceInfo
			{
				Disassembly = $"{(disassemble ? Disassemble(RegPC, ReadMemory, out notused) : "---")} ".PadRight(40),
				RegisterInfo = string.Join(" ",
					$"A:{Regs[A]:X2}",
					$"F:{Regs[F]:X2}",
					$"B:{Regs[B]:X2}",
					$"C:{Regs[C]:X2}",
					$"D:{Regs[D]:X2}",
					$"E:{Regs[E]:X2}",
					$"H:{Regs[H]:X2}",
					$"L:{Regs[L]:X2}",
					$"SP:{Regs[SPl] | (Regs[SPh] << 8):X2}",
					$"Cy:{TotalExecutedCycles}",
					$"LY:{LY}",
					string.Concat(
						FlagZ ? "Z" : "z",
						FlagN ? "N" : "n",
						FlagH ? "H" : "h",
						FlagC ? "C" : "c",
						FlagI ? "I" : "i",
						interrupts_enabled ? "E" : "e"))
			};
		}

		void FetchInstruction(int op)
		{
			opcode = op;
			
			instr_pntr = 0;
			
			if (CB_prefix) { instr_pntr += 256 * 60; }

			instr_pntr += op * 60;

			CB_prefix = false;

			was_FlagI = FlagI;
		}

		// State Save/Load
		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(LR35902));
			ser.Sync(nameof(Regs), ref Regs, false);
			ser.Sync(nameof(interrupts_enabled), ref interrupts_enabled);
			ser.Sync(nameof(I_use), ref I_use);
			ser.Sync(nameof(skip_once), ref skip_once);
			ser.Sync(nameof(Halt_bug_2), ref Halt_bug_2);
			ser.Sync(nameof(Halt_bug_3), ref Halt_bug_3);
			ser.Sync(nameof(halted), ref halted);
			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);
			ser.Sync(nameof(EI_pending), ref EI_pending);
			ser.Sync(nameof(int_src), ref int_src);
			ser.Sync(nameof(int_clear), ref int_clear);
			ser.Sync(nameof(stop_time), ref stop_time);
			ser.Sync(nameof(stop_check), ref stop_check);
			ser.Sync(nameof(is_GBC), ref is_GBC);

			ser.Sync(nameof(instr_pntr), ref instr_pntr);
			ser.Sync(nameof(CB_prefix), ref CB_prefix);
			ser.Sync(nameof(stopped), ref stopped);
			ser.Sync(nameof(opcode), ref opcode);
			ser.Sync(nameof(jammed), ref jammed);
			ser.Sync(nameof(LY), ref LY);
			ser.Sync(nameof(FlagI), ref FlagI);
			ser.Sync(nameof(was_FlagI), ref was_FlagI);

			ser.EndSection();
		}
	}
}