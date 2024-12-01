using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

// Z80A CPU
namespace BizHawk.Emulation.Cores.Components.Z80A
{
	/// <remarks>
	/// this type parameter might look useless—and it is—but after monomorphisation,
	/// this way happens to perform better than the alternative
	/// </remarks>
	/// <seealso cref="IZ80ALink"/>
	public sealed partial class Z80A<TLink> where TLink : IZ80ALink
	{
		// operations that can take place in an instruction
		public const ushort IDLE = 0; 
		public const ushort OP = 1;
		public const ushort OP_F = 2; // used for repeating operations
		public const ushort HALT = 3;
		public const ushort RD = 4;
		public const ushort WR = 5;
		public const ushort RD_INC = 6; // read and increment
		public const ushort WR_INC = 7; // write and increment
		public const ushort WR_DEC = 8; // write and increment (for stack pointer)
		public const ushort TR = 9;
		public const ushort TR16 = 10;
		public const ushort ADD16 = 11;
		public const ushort ADD8 = 12;
		public const ushort SUB8 = 13;
		public const ushort ADC8 = 14;
		public const ushort SBC8 = 15;
		public const ushort SBC16 = 16;
		public const ushort ADC16 = 17;
		public const ushort INC16 = 18;
		public const ushort INC8 = 19;
		public const ushort DEC16 = 20;
		public const ushort DEC8 = 21;
		public const ushort RLC = 22;
		public const ushort RL = 23;
		public const ushort RRC = 24;
		public const ushort RR = 25;	
		public const ushort CPL = 26;
		public const ushort DA = 27;
		public const ushort SCF = 28;
		public const ushort CCF = 29;
		public const ushort AND8 = 30;
		public const ushort XOR8 = 31;
		public const ushort OR8 = 32;
		public const ushort CP8 = 33;
		public const ushort SLA = 34;
		public const ushort SRA = 35;
		public const ushort SRL = 36;
		public const ushort SLL = 37;
		public const ushort BIT = 38;
		public const ushort RES = 39;
		public const ushort SET = 40;		
		public const ushort EI = 41;
		public const ushort DI = 42;	
		public const ushort EXCH = 43;
		public const ushort EXX = 44;
		public const ushort EXCH_16 = 45;
		public const ushort PREFIX = 46;
		public const ushort PREFETCH = 47;
		public const ushort ASGN = 48;
		public const ushort ADDS = 49; // signed 16 bit operation used in 2 instructions
		public const ushort INT_MODE = 50;
		public const ushort EI_RETN = 51;
		public const ushort EI_RETI = 52; // reti has no delay in interrupt enable
		public const ushort OUT = 53;
		public const ushort IN = 54;
		public const ushort NEG = 55;		
		public const ushort RRD = 56;
		public const ushort RLD = 57;		
		public const ushort SET_FL_LD_R = 58;
		public const ushort SET_FL_CP_R = 59;
		public const ushort SET_FL_IR = 60;
		public const ushort I_BIT = 61;
		public const ushort HL_BIT = 62;
		public const ushort FTCH_DB = 63;
		public const ushort WAIT = 64; // enterred when reading or writing and FlagW is true
		public const ushort RST = 65;
		public const ushort REP_OP_I = 66;
		public const ushort REP_OP_O = 67;
		public const ushort IN_A_N_INC = 68;
		public const ushort RD_INC_TR_PC = 69; // transfer WZ to PC after read
		public const ushort WR_TR_PC = 70; // transfer WZ to PC after write
		public const ushort OUT_INC = 71;
		public const ushort IN_INC = 72;
		public const ushort WR_INC_WA = 73; // A -> W after WR_INC
		public const ushort RD_OP = 74;
		public const ushort IORQ = 75;

		// non-state variables
		public ushort Ztemp1, Ztemp2, Ztemp3, Ztemp4;	
		public byte temp_R;

		private TLink _link;

		public Z80A(TLink link)
		{
			_link = link;
			Reset();
			InitTableParity();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 0;

			PopulateCURINSTR
					(IDLE,
						DEC16, F, A,
						DEC16, SPl, SPh);

			PopulateBUSRQ(0, 0, 0);
			PopulateMEMRQ(0, 0, 0);
			IRQS = 3;
			instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
			NO_prefix = true;
		}

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = Regs[A],
				["AF"] = Regs[F] + (Regs[A] << 8),
				["B"] = Regs[B],
				["BC"] = Regs[C] + (Regs[B] << 8),
				["C"] = Regs[C],
				["D"] = Regs[D],
				["DE"] = Regs[E] + (Regs[D] << 8),
				["E"] = Regs[E],
				["F"] = Regs[F],
				["H"] = Regs[H],
				["HL"] = Regs[L] + (Regs[H] << 8),
				["I"] = Regs[I],
				["IX"] = Regs[Ixl] + (Regs[Ixh] << 8),
				["IY"] = Regs[Iyl] + (Regs[Iyh] << 8),
				["L"] = Regs[L],
				["PC"] = Regs[PCl] + (Regs[PCh] << 8),
				["R"] = Regs[R],
				["Shadow AF"] = Regs[F_s] + (Regs[A_s] << 8),
				["Shadow BC"] = Regs[C_s] + (Regs[B_s] << 8),
				["Shadow DE"] = Regs[E_s] + (Regs[D_s] << 8),
				["Shadow HL"] = Regs[L_s] + (Regs[H_s] << 8),
				["SP"] = Regs[Iyl] + (Regs[Iyh] << 8),
				["Flag C"] = FlagC,
				["Flag N"] = FlagN,
				["Flag P/V"] = FlagP,
				["Flag 3rd"] = Flag3,
				["Flag H"] = FlagH,
				["Flag 5th"] = Flag5,
				["Flag Z"] = FlagZ,
				["Flag S"] = FlagS
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					Regs[A] = (ushort)value;
					break;
				case "AF":
					Regs[F] = (ushort)(value & 0xFF);
					Regs[A] = (ushort)(value & 0xFF00);
					break;
				case "B":
					Regs[B] = (ushort)value;
					break;
				case "BC":
					Regs[C] = (ushort)(value & 0xFF);
					Regs[B] = (ushort)(value & 0xFF00);
					break;
				case "C":
					Regs[C] = (ushort)value;
					break;
				case "D":
					Regs[D] = (ushort)value;
					break;
				case "DE":
					Regs[E] = (ushort)(value & 0xFF);
					Regs[D] = (ushort)(value & 0xFF00);
					break;
				case "E":
					Regs[E] = (ushort)value;
					break;
				case "F":
					Regs[F] = (ushort)value;
					break;
				case "H":
					Regs[H] = (ushort)value;
					break;
				case "HL":
					Regs[L] = (ushort)(value & 0xFF);
					Regs[H] = (ushort)(value & 0xFF00);
					break;
				case "I":
					Regs[I] = (ushort)value;
					break;
				case "IX":
					Regs[Ixl] = (ushort)(value & 0xFF);
					Regs[Ixh] = (ushort)(value & 0xFF00);
					break;
				case "IY":
					Regs[Iyl] = (ushort)(value & 0xFF);
					Regs[Iyh] = (ushort)(value & 0xFF00);
					break;
				case "L":
					Regs[L] = (ushort)value;
					break;
				case "PC":
					Regs[PCl] = (ushort)(value & 0xFF);
					Regs[PCh] = (ushort)(value & 0xFF00);
					break;
				case "R":
					Regs[R] = (ushort)value;
					break;
				case "Shadow AF":
					Regs[F_s] = (ushort)(value & 0xFF);
					Regs[A_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow BC":
					Regs[C_s] = (ushort)(value & 0xFF);
					Regs[B_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow DE":
					Regs[E_s] = (ushort)(value & 0xFF);
					Regs[D_s] = (ushort)(value & 0xFF00);
					break;
				case "Shadow HL":
					Regs[L_s] = (ushort)(value & 0xFF);
					Regs[H_s] = (ushort)(value & 0xFF00);
					break;
				case "SP":
					Regs[SPl] = (ushort)(value & 0xFF);
					Regs[SPh] = (ushort)(value & 0xFF00);
					break;
			}
		}

		public void SetCpuLink(TLink link)
			=> _link = link;

		// Execute instructions
		public void ExecuteOne()
		{
			bus_pntr++; mem_pntr++;
			switch (cur_instr[instr_pntr++])
			{
				case IDLE:
					// do nothing
					break;
				case OP:
					// should never reach here
				
					break;
				case OP_F:
					// Read the opcode of the next instruction	
					_link.OnExecFetch(RegPC);
					TraceCallback?.Invoke(State());
					opcode = _link.FetchMemory(RegPC++);
					FetchInstruction();
					
					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);

					instr_pntr = bus_pntr = mem_pntr = irq_pntr = 0;
					I_skip = true;
					break;
				case HALT:
					halted = true;
					// NOTE: Check how halt state effects the DB
					Regs[DB] = 0xFF;

					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);
					break;
				case RD:
					Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR:
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_INC:
					Read_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_INC_TR_PC:
					Read_INC_TR_PC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_OP:
					if (cur_instr[instr_pntr++] == 1) { Read_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]); }
					else { Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]); }

					switch (cur_instr[instr_pntr++])
					{
						case ADD8:
							ADD8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case ADC8:
							ADC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case SUB8:
							SUB8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
						case SBC8:
							SBC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
						case TR:
							TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
							break;
					}
					break;
				case WR_INC:
					Write_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_DEC:
					Write_DEC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_TR_PC:
					Write_TR_PC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_INC_WA:
					Write_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					Regs[W] = Regs[A];
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
					ushort src_t = PRE_SRC;

					NO_prefix = false;
					if (PRE_SRC == CBpre) { CB_prefix = true; }
					if (PRE_SRC == EXTDpre) { EXTD_prefix = true; }
					if (PRE_SRC == IXpre) { IX_prefix = true; }
					if (PRE_SRC == IYpre) { IY_prefix = true; }
					if (PRE_SRC == IXCBpre) { IXCB_prefix = true; }
					if (PRE_SRC == IYCBpre) { IYCB_prefix = true; }

					// only the first prefix in a double prefix increases R, although I don't know how / why
					if (PRE_SRC < 4)
					{
						temp_R = (byte)(Regs[R] & 0x7F);
						temp_R++;
						temp_R &= 0x7F;
						Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);
					}

					opcode = _link.FetchMemory(RegPC++);
					FetchInstruction();
					instr_pntr = bus_pntr = mem_pntr = irq_pntr = 0;
					I_skip = true;
					
					// for prefetched case, the PC stays on the BUS one cycle longer
					if ((src_t == IXCBpre) || (src_t == IYCBpre)) { BUSRQ[0] = PCh; }

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
					OUT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case OUT_INC:
					OUT_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case IN:
					IN_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case IN_INC:
					IN_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case IN_A_N_INC:
					IN_A_N_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
				case SET_FL_LD_R:
					DEC16_Func(C, B);
					SET_FL_LD_Func();

					Ztemp1 = cur_instr[instr_pntr++];
					Ztemp2 = cur_instr[instr_pntr++];
					Ztemp3 = cur_instr[instr_pntr++];

					if (((Regs[C] | (Regs[B] << 8)) != 0) && (Ztemp3 > 0))
					{
						PopulateCURINSTR
							(DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									TR16, Z, W, PCl, PCh,
									INC16, Z, W,
									Ztemp2, E, D);

						PopulateBUSRQ(D, D, D, D, D);
						PopulateMEMRQ(0, 0, 0, 0, 0);
						IRQS = 5;

						instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
						I_skip = true;
					}
					else
					{
						if (Ztemp2 == INC16) { INC16_Func(E, D); }
						else { DEC16_Func(E, D); }
					}				
					break;
				case SET_FL_CP_R:
					SET_FL_CP_Func();

					Ztemp1 = cur_instr[instr_pntr++];
					Ztemp2 = cur_instr[instr_pntr++];
					Ztemp3 = cur_instr[instr_pntr++];

					if (((Regs[C] | (Regs[B] << 8)) != 0) && (Ztemp3 > 0) && !FlagZ)
					{

						PopulateCURINSTR
							(DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									TR16, Z, W, PCl, PCh,
									INC16, Z, W,
									Ztemp2, L, H);

						PopulateBUSRQ(H, H, H, H, H);
						PopulateMEMRQ(0, 0, 0, 0, 0);
						IRQS = 5;

						instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
						I_skip = true;
					}
					else
					{
						if (Ztemp2 == INC16) { INC16_Func(L, H); }
						else { DEC16_Func(L, H); }
					}
					break;
				case SET_FL_IR:
					ushort dest_t = cur_instr[instr_pntr++];
					TR_Func(dest_t, cur_instr[instr_pntr++]);
					SET_FL_IR_Func(dest_t);
					break;
				case FTCH_DB:
					FTCH_DB_Func();
					break;
				case WAIT:
					if (FlagW)
					{
						instr_pntr--; bus_pntr--; mem_pntr--;
						I_skip = true;
					}
					break;
				case RST:
					Regs[Z] = cur_instr[instr_pntr++];
					Regs[W] = 0;
					break;
				case REP_OP_I:			
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);

					Ztemp4 = cur_instr[instr_pntr++];
					if (Ztemp4 == DEC16)
					{
						TR16_Func(Z, W, C, B);
						DEC16_Func(Z, W);
						DEC8_Func(B);

						// take care of other flags
						// taken from 'undocumented z80 documented' and Fuse
						FlagN = Regs[ALU].Bit(7);
						FlagH = FlagC = ((Regs[ALU] + Regs[C] - 1) & 0xFF) < Regs[ALU];
						FlagP = TableParity[((Regs[ALU] + Regs[C] - 1) & 7) ^ Regs[B]];
					}
					else
					{				
						TR16_Func(Z, W, C, B);
						INC16_Func(Z, W);
						DEC8_Func(B);

						// take care of other flags
						// taken from 'undocumented z80 documented' and Fuse
						FlagN = Regs[ALU].Bit(7);
						FlagH = FlagC = ((Regs[ALU] + Regs[C] + 1) & 0xFF) < Regs[ALU];
						FlagP = TableParity[((Regs[ALU] + Regs[C] + 1) & 7) ^ Regs[B]];
					}

					Ztemp1 = cur_instr[instr_pntr++];
					Ztemp2 = cur_instr[instr_pntr++];
					Ztemp3 = cur_instr[instr_pntr++];

					if ((Regs[B] != 0) && (Ztemp3 > 0))
					{
						PopulateCURINSTR
							(IDLE,
									IDLE,
									DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									Ztemp2, L, H);

						PopulateBUSRQ(H, H, H, H, H);
						PopulateMEMRQ(0, 0, 0, 0, 0);
						IRQS = 5;

						instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
						I_skip = true;
					}
					else
					{
						if (Ztemp2 == INC16) { INC16_Func(L, H); }
						else { DEC16_Func(L, H); }
					}
					break;
				case REP_OP_O:
					OUT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);

					Ztemp4 = cur_instr[instr_pntr++];
					if (Ztemp4 == DEC16)
					{
						DEC16_Func(L, H);
						DEC8_Func(B);
						TR16_Func(Z, W, C, B);
						DEC16_Func(Z, W);			
					}
					else
					{
						INC16_Func(L, H);
						DEC8_Func(B);
						TR16_Func(Z, W, C, B);
						INC16_Func(Z, W);
					}

					// take care of other flags
					// taken from 'undocumented z80 documented'
					FlagN = Regs[ALU].Bit(7);
					FlagH = FlagC = (Regs[ALU] + Regs[L]) > 0xFF;
					FlagP = TableParity[((Regs[ALU] + Regs[L]) & 7) ^ (Regs[B])];

					Ztemp1 = cur_instr[instr_pntr++];
					Ztemp2 = cur_instr[instr_pntr++];
					Ztemp3 = cur_instr[instr_pntr++];

					if ((Regs[B] != 0) && (Ztemp3 > 0))
					{
						PopulateCURINSTR
							(IDLE,
									IDLE,
									DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									IDLE);

						PopulateBUSRQ(B, B, B, B, B);
						PopulateMEMRQ(0, 0, 0, 0, 0);
						IRQS = 5;

						instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
						I_skip = true;
					}
					break;

				case IORQ:
					_link.IRQACKCallback();
					break;
			}

			if (I_skip)
			{
				I_skip = false;
			}
			else if (++irq_pntr == IRQS)
			{
				if (EI_pending > 0)
				{
					EI_pending--;
					if (EI_pending == 0) { IFF1 = IFF2 = true; }
				}

				// NMI has priority
				if (nonMaskableInterruptPending)
				{
					nonMaskableInterruptPending = false;

					TraceCallback?.Invoke(new(disassembly: "====NMI====", registerInfo: string.Empty));

					iff2 = iff1;
					iff1 = false;
					NMI_();
					_link.NMICallback();
					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;

					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);

					halted = false;
				}
				// if we are processing an interrrupt, we need to modify the instruction vector
				else if (iff1 && FlagI)
				{
					iff1 = iff2 = false;
					EI_pending = 0;

					TraceCallback?.Invoke(new(disassembly: "====IRQ====", registerInfo: string.Empty));

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
							INTERRUPT_2();
							break;
					}
					_link.IRQCallback();
					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;

					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);

					halted = false;
				}
				// otherwise start a new normal access
				else if (!halted)
				{
					PopulateCURINSTR
							(IDLE,
								WAIT,
								OP_F,
								OP);

					PopulateBUSRQ(PCh, 0, 0, 0);
					PopulateMEMRQ(PCh, 0, 0, 0);
					IRQS = 4;

					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
				}
				else
				{
					instr_pntr = mem_pntr = bus_pntr = irq_pntr = 0;
				}
			}

			TotalExecutedCycles++;
		}

		// tracer stuff
		public Action<TraceInfo> TraceCallback;

		public string TraceHeader => "Z80A: PC, machine code, mnemonic, operands, registers (AF, BC, DE, HL, IX, IY, SP, Cy), flags (CNP3H5ZS)";

		public TraceInfo State(bool disassemble = true)
		{
			int bytes_read = 0;

			string disasm = disassemble ? Z80ADisassembler.Disassemble(RegPC, _link.ReadMemory, out bytes_read) : "---";
			string byte_code = null;

			for (ushort i = 0; i < bytes_read; i++)
			{
				byte_code += $"{_link.ReadMemory((ushort)(RegPC + i)):X2}";
				if (i < (bytes_read - 1))
				{
					byte_code += " ";
				}
			}

			return new(
				disassembly: $"{RegPC:X4}: {byte_code.PadRight(12)} {disasm.PadRight(26)}",
				registerInfo: string.Join(" ",
					$"AF:{(Regs[A] << 8) + Regs[F]:X4}",
					$"BC:{(Regs[B] << 8) + Regs[C]:X4}",
					$"DE:{(Regs[D] << 8) + Regs[E]:X4}",
					$"HL:{(Regs[H] << 8) + Regs[L]:X4}",
					$"IX:{(Regs[Ixh] << 8) + Regs[Ixl]:X4}",
					$"IY:{(Regs[Iyh] << 8) + Regs[Iyl]:X4}",
					$"SP:{Regs[SPl] | (Regs[SPh] << 8):X4}",
					$"Cy:{TotalExecutedCycles}",
					string.Concat(
						FlagC ? "C" : "c",
						FlagN ? "N" : "n",
						FlagP ? "P" : "p",
						Flag3 ? "3" : "-",
						FlagH ? "H" : "h",
						Flag5 ? "5" : "-",
						FlagZ ? "Z" : "z",
						FlagS ? "S" : "s",
						FlagI ? "E" : "e")));
		}

		/// <summary>
		/// Optimization method to set BUSRQ
		/// </summary>
		private void PopulateBUSRQ(ushort d0 = 0, ushort d1 = 0, ushort d2 = 0, ushort d3 = 0, ushort d4 = 0, ushort d5 = 0, ushort d6 = 0, ushort d7 = 0, ushort d8 = 0,
			ushort d9 = 0, ushort d10 = 0, ushort d11 = 0, ushort d12 = 0, ushort d13 = 0, ushort d14 = 0, ushort d15 = 0, ushort d16 = 0, ushort d17 = 0, ushort d18 = 0)
		{
			BUSRQ[0] = d0; BUSRQ[1] = d1; BUSRQ[2] = d2;
			BUSRQ[3] = d3; BUSRQ[4] = d4; BUSRQ[5] = d5;
			BUSRQ[6] = d6; BUSRQ[7] = d7; BUSRQ[8] = d8;
			BUSRQ[9] = d9; BUSRQ[10] = d10; BUSRQ[11] = d11;
			BUSRQ[12] = d12; BUSRQ[13] = d13; BUSRQ[14] = d14;
			BUSRQ[15] = d15; BUSRQ[16] = d16; BUSRQ[17] = d17;
			BUSRQ[18] = d18;
		}

		/// <summary>
		/// Optimization method to set MEMRQ
		/// </summary>
		private void PopulateMEMRQ(ushort d0 = 0, ushort d1 = 0, ushort d2 = 0, ushort d3 = 0, ushort d4 = 0, ushort d5 = 0, ushort d6 = 0, ushort d7 = 0, ushort d8 = 0,
			ushort d9 = 0, ushort d10 = 0, ushort d11 = 0, ushort d12 = 0, ushort d13 = 0, ushort d14 = 0, ushort d15 = 0, ushort d16 = 0, ushort d17 = 0, ushort d18 = 0)
		{
			MEMRQ[0] = d0; MEMRQ[1] = d1; MEMRQ[2] = d2;
			MEMRQ[3] = d3; MEMRQ[4] = d4; MEMRQ[5] = d5;
			MEMRQ[6] = d6; MEMRQ[7] = d7; MEMRQ[8] = d8;
			MEMRQ[9] = d9; MEMRQ[10] = d10; MEMRQ[11] = d11;
			MEMRQ[12] = d12; MEMRQ[13] = d13; MEMRQ[14] = d14;
			MEMRQ[15] = d15; MEMRQ[16] = d16; MEMRQ[17] = d17;
			MEMRQ[18] = d18;
		}

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		private void PopulateCURINSTR(ushort d0 = 0, ushort d1 = 0, ushort d2 = 0, ushort d3 = 0, ushort d4 = 0, ushort d5 = 0, ushort d6 = 0, ushort d7 = 0, ushort d8 = 0,
			ushort d9 = 0, ushort d10 = 0, ushort d11 = 0, ushort d12 = 0, ushort d13 = 0, ushort d14 = 0, ushort d15 = 0, ushort d16 = 0, ushort d17 = 0, ushort d18 = 0,
			ushort d19 = 0, ushort d20 = 0, ushort d21 = 0, ushort d22 = 0, ushort d23 = 0, ushort d24 = 0, ushort d25 = 0, ushort d26 = 0, ushort d27 = 0, ushort d28 = 0,
			ushort d29 = 0, ushort d30 = 0, ushort d31 = 0, ushort d32 = 0, ushort d33 = 0, ushort d34 = 0, ushort d35 = 0, ushort d36 = 0, ushort d37 = 0)
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
			ser.BeginSection(nameof(Z80A));
			ser.Sync(nameof(Regs), ref Regs, false);
			ser.Sync("NMI", ref nonMaskableInterrupt);
			ser.Sync("NMIPending", ref nonMaskableInterruptPending);
			ser.Sync("IM", ref interruptMode);
			ser.Sync("IFF1", ref iff1);
			ser.Sync("IFF2", ref iff2);
			ser.Sync("Halted", ref halted);
			ser.Sync(nameof(I_skip), ref I_skip);
			ser.Sync("ExecutedCycles", ref TotalExecutedCycles);
			ser.Sync(nameof(EI_pending), ref EI_pending);

			ser.Sync(nameof(instr_pntr), ref instr_pntr);
			ser.Sync(nameof(bus_pntr), ref bus_pntr);
			ser.Sync(nameof(mem_pntr), ref mem_pntr);
			ser.Sync(nameof(irq_pntr), ref irq_pntr);
			ser.Sync(nameof(cur_instr), ref cur_instr, false);
			ser.Sync(nameof(BUSRQ), ref BUSRQ, false);
			ser.Sync(nameof(IRQS), ref IRQS);
			ser.Sync(nameof(MEMRQ), ref MEMRQ, false);
			ser.Sync(nameof(opcode), ref opcode);
			ser.Sync(nameof(FlagI), ref FlagI);
			ser.Sync(nameof(FlagW), ref FlagW);

			ser.Sync(nameof(NO_prefix), ref NO_prefix);
			ser.Sync(nameof(CB_prefix), ref CB_prefix);
			ser.Sync(nameof(IX_prefix), ref IX_prefix);
			ser.Sync(nameof(IY_prefix), ref IY_prefix);
			ser.Sync(nameof(IXCB_prefix), ref IXCB_prefix);
			ser.Sync(nameof(IYCB_prefix), ref IYCB_prefix);
			ser.Sync(nameof(EXTD_prefix), ref EXTD_prefix);
			ser.Sync(nameof(PRE_SRC), ref PRE_SRC);

			ser.EndSection();
		}
	}
}
