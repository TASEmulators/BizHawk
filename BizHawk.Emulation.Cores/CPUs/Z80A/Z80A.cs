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
		public const ushort WAIT = 64; // enterred when readin or writing and FlagW is true
		public const ushort RST = 65;
		public const ushort REP_OP_I = 66;
		public const ushort REP_OP_O = 67;

		// non-state variables
		public ushort Ztemp1, Ztemp2, Ztemp3, Ztemp4;	
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
			cur_instr = new ushort[] 
						{ IDLE,
						  WAIT,
						  OP_F,
						  OP };

			BUSRQ = new ushort[] { PCh, 0, 0, 0 };
			MEMRQ = new ushort[] { PCh, 0, 0, 0 };
			instr_pntr = 0; bus_pntr = 0;
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

		// Data Bus
		// Interrupting Devices are responsible for putting a value onto the data bus
		// for as long as the interrupt is valid
		public Func<byte> FetchDB;

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
			//FlagI6 = FlagI5;
			FlagI5 = FlagI4;
			FlagI4 = FlagI3;
			FlagI3 = FlagI2;
			FlagI2 = FlagI1;
			FlagI1 = FlagI;
			
			bus_pntr++;
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
						instr_pntr = 0; bus_pntr = 0;
					}
					else if (iff1 && FlagI5)
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
								INTERRUPT_2();
								break;
						}
						IRQCallback();
						instr_pntr = 0; bus_pntr = 0;
					}
					else
					{
						if (OnExecFetch != null) OnExecFetch(RegPC);
						if (TraceCallback != null) TraceCallback(State());
						RegPC++;
						FetchInstruction();
						instr_pntr = 0; bus_pntr = 0;
					}

					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);
					break;
				case OP_F:
					opcode = FetchMemory(RegPC);
					break;
				case HALT:
					halted = true;
					// NOTE: Check how halt state effects the DB
					Regs[DB] = 0xFF;

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
					else if (iff1 && FlagI5)
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
								INTERRUPT_2();
								break;
						}
						IRQCallback();
						halted = false;
					}

					temp_R = (byte)(Regs[R] & 0x7F);
					temp_R++;
					temp_R &= 0x7F;
					Regs[R] = (byte)((Regs[R] & 0x80) | temp_R);

					instr_pntr = 0; bus_pntr = 0;
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
				case WR_INC:
					Write_INC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR_DEC:
					Write_DEC_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
					if (prefix_src == IXCBpre) { IXCB_prefix = true; }
					if (prefix_src == IYCBpre) { IYCB_prefix = true; }

					RegPC++;
					FetchInstruction();
					instr_pntr = 0; bus_pntr = 0;
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
					OUT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case IN:
					IN_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
						cur_instr = new ushort[]
									{DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									TR16, Z, W, PCl, PCh,
									INC16, Z, W,								
									IDLE,
									Ztemp2, E, D,
									WAIT,
									OP_F,
									OP};

						BUSRQ = new ushort[] { D, D, D, D, D, PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { 0, 0, 0, 0, 0, PCh, 0, 0, 0 };
					}
					else
					{
						cur_instr = new ushort[]
									{ Ztemp2, E, D,
									  WAIT,
									  OP_F,
									  OP };

						BUSRQ = new ushort[] { PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { PCh, 0, 0, 0 };
					}
					instr_pntr = 0; bus_pntr = 0;
					break;
				case SET_FL_CP_R:
					SET_FL_CP_Func();

					Ztemp1 = cur_instr[instr_pntr++];
					Ztemp2 = cur_instr[instr_pntr++];
					Ztemp3 = cur_instr[instr_pntr++];

					if (((Regs[C] | (Regs[B] << 8)) != 0) && (Ztemp3 > 0) && !FlagZ)
					{
						cur_instr = new ushort[]
									{DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									TR16, Z, W, PCl, PCh,
									INC16, Z, W,								
									IDLE,
									Ztemp2, L, H,
									WAIT,
									OP_F,
									OP};

						BUSRQ = new ushort[] { H, H, H, H, H, PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { 0, 0, 0, 0, 0, PCh, 0, 0, 0 };
					}
					else
					{
						cur_instr = new ushort[]
									{ Ztemp2, L, H,
									  WAIT,
									  OP_F,
									  OP };

						BUSRQ = new ushort[] { PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { PCh, 0, 0, 0 };
					}
					instr_pntr = 0; bus_pntr = 0;
					break;
				case SET_FL_IR:
					SET_FL_IR_Func(cur_instr[instr_pntr++]);
					break;
				case FTCH_DB:
					FTCH_DB_Func();
					break;
				case WAIT:
					if (FlagW)
					{
						instr_pntr--;
						bus_pntr--;
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
						cur_instr = new ushort[]
									{IDLE,
									IDLE,
									DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									IDLE,
									Ztemp2, L, H,
									WAIT,
									OP_F,
									OP};

						BUSRQ = new ushort[] { H, H, H, H, H, PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { 0, 0, 0, 0, 0, PCh, 0, 0, 0 };
					}
					else
					{
						cur_instr = new ushort[]
									{ Ztemp2, L, H,
									  WAIT,
									  OP_F,
									  OP };

						BUSRQ = new ushort[] { PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { PCh, 0, 0, 0 };
					}
					instr_pntr = 0; bus_pntr = 0;
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
						cur_instr = new ushort[]
									{IDLE,
									IDLE,
									DEC16, PCl, PCh,
									DEC16, PCl, PCh,
									IDLE,
									IDLE,//DEC16, B, ALU,
									WAIT,
									OP_F,
									OP};

						//Regs[B] = (ushort)((Regs[B] + 1) & 0xFF);


						BUSRQ = new ushort[] { B, B, B, B, B, PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { 0, 0, 0, 0, 0, PCh, 0, 0, 0 };
					}
					else
					{
						cur_instr = new ushort[]
									{ IDLE,
									  WAIT,
									  OP_F,
									  OP };

						BUSRQ = new ushort[] { PCh, 0, 0, 0 };
						MEMRQ = new ushort[] { PCh, 0, 0, 0 };
					}
					instr_pntr = 0; bus_pntr = 0;
					break;
			}
			TotalExecutedCycles++;
		}

		// tracer stuff
		public Action<TraceInfo> TraceCallback;

		public string TraceHeader
		{
			get { return "Z80A: PC, machine code, mnemonic, operands, registers (AF, BC, DE, HL, IX, IY, SP, Cy), flags (CNP3H5ZS)"; }
		}

		public TraceInfo State(bool disassemble = true)
		{
			int bytes_read = 0;

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
			ser.Sync("ExecutedCycles", ref TotalExecutedCycles);
			ser.Sync("EI_pending", ref EI_pending);

			ser.Sync("instr_pntr", ref instr_pntr);
			ser.Sync("bus_pntr", ref bus_pntr);
			ser.Sync("cur_instr", ref cur_instr, false);
			ser.Sync("BUSRQ", ref BUSRQ, false);
			ser.Sync("MEMRQ", ref MEMRQ, false);
			ser.Sync("instr_swap", ref instr_swap);
			ser.Sync("opcode", ref opcode);
			ser.Sync("FlagI", ref FlagI);
			ser.Sync("FlagI1", ref FlagI1);
			ser.Sync("FlagI2", ref FlagI2);
			ser.Sync("FlagI3", ref FlagI3);
			ser.Sync("FlagI4", ref FlagI4);
			ser.Sync("FlagI5", ref FlagI5);
			ser.Sync("FlagI6", ref FlagI6);
			ser.Sync("FlagW", ref FlagW);

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
