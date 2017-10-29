using System;
using System.Globalization;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

// Z80A CPU
namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public sealed partial class Z80A
	{
		// operations that can take place in an instruction
		public const ushort IDLE = 0; 
		public const ushort OP = 1;
		public const ushort OP_R = 2; // used for repeating operations
		public const ushort HALT = 3;
		public const ushort RD = 4;
		public const ushort WR = 5;
		public const ushort I_RD = 6;
		public const ushort I_WR = 7;
		public const ushort TR = 8;
		public const ushort TR16 = 9;
		public const ushort ADD16 = 10;
		public const ushort ADD8 = 11;
		public const ushort SUB8 = 12;
		public const ushort ADC8 = 13;
		public const ushort SBC8 = 14;
		public const ushort SBC16 = 15;
		public const ushort ADC16 = 16;
		public const ushort INC16 = 17;
		public const ushort INC8 = 18;
		public const ushort DEC16 = 19;
		public const ushort DEC8 = 20;
		public const ushort RLC = 21;
		public const ushort RL = 22;
		public const ushort RRC = 23;
		public const ushort RR = 24;	
		public const ushort CPL = 25;
		public const ushort DA = 26;
		public const ushort SCF = 27;
		public const ushort CCF = 28;
		public const ushort AND8 = 29;
		public const ushort XOR8 = 30;
		public const ushort OR8 = 31;
		public const ushort CP8 = 32;
		public const ushort SLA = 33;
		public const ushort SRA = 34;
		public const ushort SRL = 35;
		public const ushort SLL = 36;
		public const ushort BIT = 37;
		public const ushort RES = 38;
		public const ushort SET = 39;		
		public const ushort EI = 40;
		public const ushort DI = 41;	
		public const ushort EXCH = 42;
		public const ushort EXX = 43;
		public const ushort EXCH_16 = 44;
		public const ushort PREFIX = 45;
		public const ushort PREFETCH = 46;
		public const ushort ASGN = 47;
		public const ushort ADDS = 48; // signed 16 bit operation used in 2 instructions
		public const ushort INT_MODE = 49;
		public const ushort EI_RETN = 50;
		public const ushort EI_RETI = 51; // reti has no delay in interrupt enable
		public const ushort OUT = 52;
		public const ushort IN = 53;
		public const ushort NEG = 54;		
		public const ushort RRD = 55;
		public const ushort RLD = 56;		
		public const ushort SET_FL_LD = 57;
		public const ushort SET_FL_CP = 58;
		public const ushort SET_FL_IR = 59;
		public const ushort I_BIT = 60;
		public const ushort HL_BIT = 61;

		public byte temp_R;

		public Z80A()
		{
			Reset();
			InitTableParity();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 0;
			cur_instr = new ushort[] { OP };
			NO_prefix = true;
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; set; }

		// Memory Access 
		public Func<ushort, byte> FetchMemory;
		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;
		public Func<ushort, byte> PeekMemory;
		public Func<ushort, byte> DummyReadMemory;

		// Hardware I/O Port Access
		public Func<ushort, byte> ReadHardware;
		public Action<ushort, byte> WriteHardware;

		//this only calls when the first byte of an instruction is fetched.
		public Action<ushort> OnExecFetch;

		public void UnregisterMemoryMapper()
		{
			ReadMemory = null;
			WriteMemory = null;
			PeekMemory = null;
			DummyReadMemory = null;
			ReadHardware = null;
			WriteHardware = null;
		}

		public void SetCallbacks
		(
			Func<ushort, byte> ReadMemory,
			Func<ushort, byte> DummyReadMemory,
			Func<ushort, byte> PeekMemory,
			Action<ushort, byte> WriteMemory,
			Func<ushort, byte> ReadHardware,
			Action<ushort, byte> WriteHardware
		)
		{
			this.ReadMemory = ReadMemory;
			this.DummyReadMemory = DummyReadMemory;
			this.PeekMemory = PeekMemory;
			this.WriteMemory = WriteMemory;
			this.ReadHardware = ReadHardware;
			this.WriteHardware = WriteHardware;
		}

		// Execute instructions
		public void ExecuteOne()
		{
			if (Regs[A] > 255) { Console.WriteLine(RegPC); }
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
						if (EI_pending == 0) { IFF1 = IFF2 = true; }
					}

					// Process interrupt requests.
					if (nonMaskableInterruptPending)
					{
						nonMaskableInterruptPending = false;

						if (TraceCallback != null)
						{
							TraceCallback(new TraceInfo{Disassembly = "====NMI====", RegisterInfo = ""});
						}

						iff2 = iff1;
						iff1 = false;
						NMI_();
						NMICallback();
					}
					else if (iff1 && FlagI)
					{
						iff1 = iff2 = false;
						EI_pending = 0;

						if (TraceCallback != null)
						{
							TraceCallback(new TraceInfo{Disassembly = "====IRQ====", RegisterInfo = ""});
						}

						switch (interruptMode)
						{
							case 0:
								// Requires something to be pushed onto the data bus
								// we'll assume it's a zero for now
								INTERRUPT_0(0);
								break;
							case 1:
								INTERRUPT_1();
								break;
							case 2:
								// Low byte of interrupt vector comes from data bus
								// We'll assume it's zero for now
								INTERRUPT_2(0);
								break;
						}
						IRQCallback();
					}
					else
					{
						if (OnExecFetch != null) OnExecFetch(RegPC);
						if (TraceCallback != null) TraceCallback(State());
						FetchInstruction(FetchMemory(RegPC++));
					}
					instr_pntr = 0;

					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);
					break;
				case OP_R:
					// determine if we repeat based on what operation we are doing
					// single execution versions also come here, but never repeat
					ushort temp1 = cur_instr[instr_pntr++];
					ushort temp2 = cur_instr[instr_pntr++];
					ushort temp3 = cur_instr[instr_pntr++];

					bool repeat = false;
					int Reg16_d = Regs[C] | (Regs[B] << 8);
					switch (temp1)
					{
						case 0:
							repeat = Reg16_d != 0;
							break;
						case 1:
							repeat = (Reg16_d != 0) && !FlagZ;
							break;
						case 2:
							repeat = Regs[B] != 0;
							break;
						case 3:
							repeat = Regs[B] != 0;
							break;
					}

					// if we repeat, we do a 5 cycle refresh which decrements PC by 2
					// if we don't repeat, continue on as a normal opcode fetch
					if (repeat && temp3 > 0)
					{
						cur_instr = new ushort[]
									{IDLE,
									DEC16, PCl, PCh,
									IDLE,
									DEC16, PCl, PCh,
									OP };

						// adjust WZ register accordingly
						switch (temp1)
						{
							case 0:
								// TEST: PC before or after the instruction?
								Regs[Z] = Regs[PCl];
								Regs[W] = Regs[PCh];
								INC16_Func(Z, W);
								break;
							case 1:
								// TEST: PC before or after the instruction?
								Regs[Z] = Regs[PCl];
								Regs[W] = Regs[PCh];
								INC16_Func(Z, W);
								break;
							case 2:
								// Nothing
								break;
							case 3:
								// Nothing
								break;
						}
					}
					else
					{
						// Interrupts can occur at this point, so process them accordingly
						// Read the opcode of the next instruction				
						if (EI_pending > 0)
						{
							EI_pending--;
							if (EI_pending == 0) { IFF1 = IFF2 = true; }
						}

						// Process interrupt requests.
						if (nonMaskableInterruptPending)
						{
							nonMaskableInterruptPending = false;

							if (TraceCallback != null)
							{
								TraceCallback(new TraceInfo{Disassembly = "====NMI====", RegisterInfo = ""});
							}

							iff2 = iff1;
							iff1 = false;
							NMI_();
							NMICallback();
						}
						else if (iff1 && FlagI)
						{
							iff1 = iff2 = false;
							EI_pending = 0;

							if (TraceCallback != null)
							{
								TraceCallback(new TraceInfo{Disassembly = "====IRQ====", RegisterInfo = ""});
							}

							switch (interruptMode)
							{
								case 0:
									// Requires something to be pushed onto the data bus
									// we'll assume it's a zero for now
									INTERRUPT_0(0);
									break;
								case 1:
									INTERRUPT_1();
									break;
								case 2:
									// Low byte of interrupt vector comes from data bus
									// We'll assume it's zero for now
									INTERRUPT_2(0);
									break;
							}
							IRQCallback();
						}
						else
						{
							if (OnExecFetch != null) OnExecFetch(RegPC);
							if (TraceCallback != null) TraceCallback(State());
							FetchInstruction(FetchMemory(RegPC++));
						}

						temp_R = (byte)(Regs[R] & 0x7F);
						temp_R++;
						temp_R &= 0x7F;
						Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);
					}
					instr_pntr = 0;
					break;

				case HALT:
					halted = true;
					if (EI_pending > 0)
					{
						EI_pending--;
						if (EI_pending == 0) { IFF1 = IFF2 = true; }
					}

					// Process interrupt requests.
					if (nonMaskableInterruptPending)
					{
						nonMaskableInterruptPending = false;

						if (TraceCallback != null)
						{
							TraceCallback(new TraceInfo{Disassembly = "====NMI====", RegisterInfo = ""});
						}

						iff2 = iff1;
						iff1 = false;
						NMI_();
						NMICallback();
						halted = false;
					}
					else if (iff1 && FlagI)
					{
						iff1 = iff2 = false;
						EI_pending = 0;

						if (TraceCallback != null)
						{
							TraceCallback(new TraceInfo{Disassembly = "====IRQ====", RegisterInfo = ""});
						}

						switch (interruptMode)
						{
							case 0:
								// Requires something to be pushed onto the data bus
								// we'll assume it's a zero for now
								INTERRUPT_0(0);
								break;
							case 1:
								INTERRUPT_1();
								break;
							case 2:
								// Low byte of interrupt vector comes from data bus
								// We'll assume it's zero for now
								INTERRUPT_2(0);
								break;
						}
						IRQCallback();
						halted = false;
					}
					else
					{
						cur_instr = new ushort[]
						{IDLE,
						IDLE,
						IDLE,
						HALT };
					}
					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);

					instr_pntr = 0;
					break;
				case RD:
					Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR:
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case I_RD:
					I_Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case I_WR:
					I_Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TR:
					TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TR16:
					TR16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
				case ADC16:
					ADC_16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SBC8:
					SBC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SBC16:
					SBC_16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
				case SLL:
					SLL_Func(cur_instr[instr_pntr++]);
					break;
				case BIT:
					BIT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case I_BIT:
					I_BIT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RES:
					RES_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SET:
					SET_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case EI:
					EI_pending = 2;
					break;
				case DI:
					IFF1 = IFF2 = false;
					break;
				case EXCH:
					EXCH_16_Func(F_s, A_s, F, A);
					break;
				case EXX:
					EXCH_16_Func(C_s, B_s, C, B);
					EXCH_16_Func(E_s, D_s, E, D);
					EXCH_16_Func(L_s, H_s, L, H);
					break;
				case EXCH_16:
					EXCH_16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case PREFIX:
					ushort prefix_src = cur_instr[instr_pntr++];
					NO_prefix = false;
					if (prefix_src == CBpre) { CB_prefix = true; }
					if (prefix_src == EXTDpre) { EXTD_prefix = true; }
					if (prefix_src == IXpre) { IX_prefix = true; }
					if (prefix_src == IYpre) { IY_prefix = true; }
					if (prefix_src == IXCBpre) { IXCB_prefix = true; IXCB_prefetch = true; }
					if (prefix_src == IYCBpre) { IYCB_prefix = true; IYCB_prefetch = true; }

					FetchInstruction(FetchMemory(RegPC++));
					instr_pntr = 0;
					// only the first prefix in a double prefix increases R, although I don't know how / why
					if (prefix_src < 4)
					{
						temp_R = (byte)(Regs[R] & 0x7F);
						temp_R++;
						temp_R &= 0x7F;
						Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);
					}
					break;
				case ASGN:
					ASGN_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADDS:
					ADDS_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case EI_RETI:
					// NOTE: This is needed for systems using multiple interrupt sources, it triggers the next interrupt
					// Not currently implemented here
					iff1 = iff2;
					break;
				case EI_RETN:
					iff1 = iff2;
					break;
				case OUT:
					OUT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case IN:
					IN_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case NEG:
					NEG_8_Func(cur_instr[instr_pntr++]);
					break;
				case INT_MODE:
					interruptMode = cur_instr[instr_pntr++];
					break;
				case RRD:
					RRD_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RLD:
					RLD_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case SET_FL_LD:
					SET_FL_LD_Func();
					break;
				case SET_FL_CP:
					SET_FL_CP_Func();
					break;
				case SET_FL_IR:
					SET_FL_IR_Func(cur_instr[instr_pntr++]);
					break;
			}
			totalExecutedCycles++;
		}

		// tracer stuff
		public Action<TraceInfo> TraceCallback;

		public string TraceHeader
		{
			get { return "Z80A: PC, machine code, mnemonic, operands, registers (AF, BC, DE, HL, IX, IY, SP, Cy), flags (CNP3H5ZS)"; }
		}

		public TraceInfo State(bool disassemble = true)
		{
			ushort bytes_read = 0;

			string disasm = disassemble ? Disassemble(RegPC, ReadMemory, out bytes_read) : "---";
			string byte_code = null;

			for (ushort i = 0; i < bytes_read; i++)
			{
				byte_code += ReadMemory((ushort)(RegPC + i)).ToHexString(2);
				if (i < (bytes_read - 1))
				{
					byte_code += " ";
				}
			}

			return new TraceInfo
			{
				Disassembly = string.Format(
					"{0:X4}: {1} {2}",
					RegPC,
					byte_code.PadRight(12),
					disasm.PadRight(26)),
				RegisterInfo = string.Format(
					"AF:{0:X4} BC:{1:X4} DE:{2:X4} HL:{3:X4} IX:{4:X4} IY:{5:X4} SP:{6:X4} Cy:{7} {8}{9}{10}{11}{12}{13}{14}{15}{16}",
					(Regs[A] << 8) + Regs[F],
					(Regs[B] << 8) + Regs[C],
					(Regs[D] << 8) + Regs[E],
					(Regs[H] << 8) + Regs[L],
					(Regs[Ixh] << 8) + Regs[Ixl],
					(Regs[Iyh] << 8) + Regs[Iyl],
					Regs[SPl] | (Regs[SPh] << 8),
					TotalExecutedCycles,
					FlagC ? "C" : "c",
					FlagN ? "N" : "n",
					FlagP ? "P" : "p",
					Flag3 ? "3" : "-",
					FlagH ? "H" : "h",
					Flag5 ? "5" : "-",
					FlagZ ? "Z" : "z",
					FlagS ? "S" : "s",
					FlagI ? "E" : "e")
			};
		}
		// State Save/Load

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Z80A");
			ser.Sync("Regs", ref Regs, false);
			ser.Sync("NMI", ref nonMaskableInterrupt);
			ser.Sync("NMIPending", ref nonMaskableInterruptPending);
			ser.Sync("IM", ref interruptMode);
			ser.Sync("IFF1", ref iff1);
			ser.Sync("IFF2", ref iff2);
			ser.Sync("Halted", ref halted);
			ser.Sync("ExecutedCycles", ref totalExecutedCycles);
			ser.Sync("EI_pending", ref EI_pending);

			ser.Sync("instruction_pointer", ref instr_pntr);
			ser.Sync("current instruction", ref cur_instr, false);		
			ser.Sync("opcode", ref opcode);
			ser.Sync("FlagI", ref FlagI);

			ser.Sync("NO Preifx", ref NO_prefix);
			ser.Sync("CB Preifx", ref CB_prefix);
			ser.Sync("IX_prefix", ref IX_prefix);
			ser.Sync("IY_prefix", ref IY_prefix);
			ser.Sync("IXCB_prefix", ref IXCB_prefix);
			ser.Sync("IYCB_prefix", ref IYCB_prefix);
			ser.Sync("EXTD_prefix", ref EXTD_prefix);
			ser.Sync("IXCB_prefetch", ref IXCB_prefetch);
			ser.Sync("IYCB_prefetch", ref IYCB_prefetch);
			ser.Sync("PF", ref PF);

			ser.EndSection();
		}
	}
}