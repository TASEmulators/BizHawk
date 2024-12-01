using BizHawk.Common;
using BizHawk.Emulation.Common;

// Intel Corp 8048
namespace BizHawk.Emulation.Cores.Components.I8048
{
	public sealed partial class I8048
	{
		public int LY;
		// operations that can take place in an instruction
		public const ushort IDLE = 0; 
		public const ushort OP = 1;
		public const ushort RD = 2;
		public const ushort WR = 3;
		public const ushort TR = 4;
		public const ushort INC11 = 5;
		public const ushort ADD8 = 6;
		public const ushort ADC8 = 7;
		public const ushort AND8 = 8;
		public const ushort XOR8 = 9;
		public const ushort OR8 = 10;
		public const ushort INC8 = 11;
		public const ushort INCA = 12;
		public const ushort DEC8 = 13;
		public const ushort DECA = 14;
		public const ushort ROL = 15;
		public const ushort ROR = 16;
		public const ushort RLC = 17;
		public const ushort RRC = 18;
		public const ushort CLRA = 19;
		public const ushort SWP = 20;
		public const ushort COMA = 21;
		public const ushort CMC = 22;
		public const ushort CM0 = 23;
		public const ushort CM1 = 24;
		public const ushort DA = 25;
		public const ushort SET_ADDR = 26;
		public const ushort CLC = 27;
		public const ushort CL0 = 28;
		public const ushort CL1 = 29;
		public const ushort EI = 30;
		public const ushort EN = 31;
		public const ushort DI = 32;
		public const ushort DN = 33;
		public const ushort MSK = 34;
		public const ushort CLK_OUT = 35;
		public const ushort XCH = 36;
		public const ushort XCH_RAM = 37;
		public const ushort XCHD_RAM = 38;
		public const ushort SEL_MB0 = 39;
		public const ushort SEL_MB1 = 40;
		public const ushort SEL_RB0 = 41;
		public const ushort SEL_RB1 = 42;
		public const ushort INC_RAM = 43;
		public const ushort RES_TF = 44;
		public const ushort SET_ADDR_M3 = 45;
		public const ushort MOVT_RAM_D = 46;
		public const ushort MOV = 47;
		public const ushort MOVT = 48;
		public const ushort MOVAR = 49;
		public const ushort MOVT_RAM = 50;
		public const ushort ST_CNT = 51;
		public const ushort STP_CNT = 52;
		public const ushort ST_T = 53;
		public const ushort SET_ADDR_8 = 54;
		public const ushort MEM_ALU = 55;
		public const ushort PUSH = 56;
		public const ushort PULL = 57;
		public const ushort PULL_PC = 58;
		public const ushort EEA = 59;
		public const ushort DEA = 60;
		public const ushort RD_P = 61;
		public const ushort WR_P = 62;
		public const ushort EM = 63;
		public const ushort DM = 64;	
		public const ushort TEST_COND = 65;

		public I8048()
		{
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 0;
			Regs[PC] = 0x0;
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE);

			IRQS = 5;
			instr_pntr = irq_pntr = 0;
			Regs[PX + 1] = 0xFF;
			Regs[PX + 2] = 0xFF;
		}

		// Memory Access 
		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;
		public Func<ushort, byte> PeekMemory;
		public Func<ushort, byte> DummyReadMemory;

		// Port Access
		public Func<ushort, byte> ReadPort;
		public Action<ushort, byte> WritePort;

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
		public delegate void DoCDLCallbackType(ushort addr, eCDLogMemFlags flags);

		public DoCDLCallbackType CDLCallback;

		public enum eCDLogMemFlags
		{
			FetchFirst = 1,
			FetchOperand = 2,
			Data = 4,
			Write = 8
		}

		// Execute instructions
		public void ExecuteOne()
		{
			//Console.Write(opcode_see + " ");
			//Console.WriteLine(Regs[PC] + " ");
			switch (cur_instr[instr_pntr++])
			{
				case IDLE:
					// do nothing
					break;
				case OP:
					// Read the opcode of the next instruction
					OnExecFetch?.Invoke(PC);
					TraceCallback?.Invoke(State());
					CDLCallback?.Invoke(PC, eCDLogMemFlags.FetchFirst);
					FetchInstruction(ReadMemory(Regs[PC]));
					Regs[ALU2] = (ushort)(Regs[PC] & 0x800);
					Regs[PC] = (ushort)(((Regs[PC] + 1) & 0x7FF) | Regs[ALU2]);
					instr_pntr = 0;
					irq_pntr = -1;
					break;
				case RD:
					Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR:
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TR:
					TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case INC11:
					reg_d_ad = cur_instr[instr_pntr++];
					Regs[ALU2] = (ushort)(Regs[reg_d_ad] & 0x800);
					Regs[reg_d_ad] = (ushort)(((Regs[reg_d_ad] + 1) & 0x7FF) | Regs[ALU2]);
					break;
				case ADD8:
					ADD8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case ADC8:
					ADC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
				case INC8:
					INC8_Func(cur_instr[instr_pntr++]);
					break;
				case INCA:
					INC8_Func(A);
					break;
				case DEC8:
					DEC8_Func(cur_instr[instr_pntr++]);
					break;
				case DECA:
					DEC8_Func(A);
					break;
				case ROL:
					ROL_Func(A);
					break;
				case ROR:
					ROR_Func(A);
					break;
				case RLC:
					RLC_Func(A);
					break;
				case RRC:
					RRC_Func(A);
					break;
				case CLRA:
					Regs[A] = 0;
					break;
				case SWP:
					reg_d_ad = Regs[A];
					Regs[A] = (ushort)(Regs[A] >> 4);
					Regs[A] |= (ushort)((reg_d_ad << 4) & 0xF0);
					break;
				case COMA:
					Regs[A] = (ushort)((~Regs[A]) & 0xFF);
					break;
				case CMC:
					FlagC = !FlagC;
					break;
				case CM0:
					FlagF0 = !FlagF0;
					break;
				case CM1:
					F1 = !F1;
					break;
				case DA:
					DA_Func(A);
					break;
				case SET_ADDR:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_l_ad = cur_instr[instr_pntr++];
					reg_h_ad = cur_instr[instr_pntr++]; // direct value

					// bit 11 held low during interrupt
					if (INT_MSTR)
					{
						Regs[reg_d_ad] = (ushort)(MB | (reg_h_ad << 8) | Regs[reg_l_ad]);
					}
					else
					{
						Regs[reg_d_ad] = (ushort)((reg_h_ad << 8) | Regs[reg_l_ad]);
					}
					break;
				case CLC:
					FlagC = false;
					break;
				case CL0:
					FlagF0 = false;
					break;
				case CL1:
					F1 = false;
					break;
				case EI:
					IntEn = true;
					break;
				case EN:
					TimIntEn = true;
					break;
				case DI:
					IntEn = false;
					break;
				case DN:
					TimIntEn = false;
					TIRQPending = false;
					break;
				case MSK:

					break;
				case CLK_OUT:

					break;
				case XCH:
					Regs[ALU] = Regs[cur_instr[instr_pntr]];
					Regs[cur_instr[instr_pntr++]] = Regs[cur_instr[instr_pntr]];
					Regs[cur_instr[instr_pntr++]] = Regs[ALU];
					break;
				case XCH_RAM:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_d_ad = (ushort)(Regs[reg_d_ad] & 0x3F);

					Regs[ALU] = Regs[reg_d_ad];
					Regs[reg_d_ad] = Regs[A];
					Regs[A] = Regs[ALU];
					break;
				case XCHD_RAM:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_d_ad = (ushort)(Regs[reg_d_ad] & 0x3F);

					Regs[ALU] = Regs[reg_d_ad];
					Regs[reg_d_ad] = (ushort)((Regs[reg_d_ad] & 0xF0) | (Regs[A] & 0xF));
					Regs[A] = (ushort)((Regs[A] & 0xF0) | (Regs[ALU] & 0xF));
					break;
				case SEL_MB0:
					MB = 0;
					break;
				case SEL_MB1:
					MB = 1 << 11;
					break;
				case SEL_RB0:
					FlagBS = false; // register bank also changed here
					break;
				case SEL_RB1:
					FlagBS = true; // register bank also changed here
					break;
				case INC_RAM:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_d_ad = (ushort)(Regs[reg_d_ad] & 0x3F);
					Regs[reg_d_ad] = (ushort)((Regs[reg_d_ad] + 1) & 0xFF);
					break;
				case RES_TF:
					if (test_pass) { TF = false; }				
					break;
				case SET_ADDR_M3:
					Regs[ALU] &= 0xFF;
					Regs[ALU] |= 0x300;
					break;
				case MOVT_RAM_D:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_d_ad = (ushort)(Regs[reg_d_ad] & 0x3F);
					Regs[reg_d_ad] = Regs[cur_instr[instr_pntr++]];
					//Console.WriteLine(reg_d_ad + " " + Regs[reg_d_ad] + " " + Regs[ALU] + " " + TotalExecutedCycles);
					break;
				case MOV:
					reg_d_ad = cur_instr[instr_pntr++];
					Regs[reg_d_ad] = Regs[cur_instr[instr_pntr++]];
					break;
				case MOVT:
					reg_d_ad = cur_instr[instr_pntr++];
					Regs[reg_d_ad] = Regs[cur_instr[instr_pntr++]];
					break;
				case MOVAR:
					Regs[cur_instr[instr_pntr++]] = Regs[A];
					break;
				case MOVT_RAM:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_d_ad = (ushort)(Regs[reg_d_ad] & 0x3F);
					Regs[reg_d_ad] = Regs[A];
					break;
				case ST_CNT:
					counter_en = true;
					next_T1_check = TotalExecutedCycles + 20;
					timer_en = false;
					break;
				case STP_CNT:
					counter_en = timer_en = false;
					break;
				case ST_T:
					timer_en = true;
					timer_prescale = 0;
					counter_en = false;
					break;
				case SET_ADDR_8:
					reg_d_ad = cur_instr[instr_pntr++];
					Regs[reg_d_ad] &= 0xFF00;
					Regs[reg_d_ad] |= Regs[cur_instr[instr_pntr++]];
					break;
				case MEM_ALU:
					Regs[ALU] = Regs[(ushort)(Regs[cur_instr[instr_pntr++]] & 0x3F)];
					break;
				case PUSH:
					Regs[(Regs[PSW] & 0x7) * 2 + 8] = (ushort)(Regs[PC] & 0xFF);
					Regs[(Regs[PSW] & 0x7) * 2 + 8 + 1] = (ushort)(((Regs[PC] >> 8) & 0xF) | (Regs[PSW] & 0xF0));
					Regs[PSW] = (ushort)((((Regs[PSW] & 0x7) + 1) & 0x7) | (Regs[PSW] & 0xF8));
					break;
				case PULL:
					Regs[PSW] = (ushort)((((Regs[PSW] & 0x7) - 1) & 0x7) | (Regs[PSW] & 0xF8));
					Regs[PC] = (ushort)(Regs[(Regs[PSW] & 0x7) * 2 + 8] & 0xFF);
					Regs[PC] |= (ushort)((Regs[(Regs[PSW] & 0x7) * 2 + 8 + 1] & 0xF) << 8);
					Regs[PSW] &= 0xF;
					Regs[PSW] |= (ushort)(Regs[(Regs[PSW] & 0x7) * 2 + 8 + 1] & 0xF0);
					RB = (ushort)(FlagBS ? 24 : 0);
					break;
				case PULL_PC:
					Regs[PSW] = (ushort)((((Regs[PSW] & 0x7) - 1) & 0x7) | (Regs[PSW] & 0xF8));
					Regs[PC] = (ushort)(Regs[(Regs[PSW] & 0x7) * 2 + 8] & 0xFF);
					Regs[PC] |= (ushort)((Regs[(Regs[PSW] & 0x7) * 2 + 8 + 1] & 0xF) << 8);
					break;	
				case EEA:
					EA = true;
					break;
				case DEA:
					EA = false;
					break;
				case RD_P:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_l_ad = cur_instr[instr_pntr++];

					Regs[reg_d_ad] = ReadPort(reg_l_ad);
					Regs[PX + reg_l_ad] = Regs[reg_d_ad];
					break;
				case WR_P:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_l_ad = cur_instr[instr_pntr++];

					WritePort(reg_d_ad, (byte)Regs[reg_l_ad]);
					Regs[PX + reg_d_ad] = Regs[reg_l_ad];
					break;
				case EM:
					INT_MSTR = true;
					break;
				case DM:
					INT_MSTR = false;
					break;		
				case TEST_COND:
					reg_d_ad = cur_instr[instr_pntr++];

					test_pass = true;
					if ((reg_d_ad == 0) && !TF) { test_pass = false; }
					if ((reg_d_ad == 1) && T0) { test_pass = false; }
					if ((reg_d_ad == 2) && !T0) { test_pass = false; }
					if ((reg_d_ad == 3) && T1) { test_pass = false; }
					if ((reg_d_ad == 4) && !T1) { test_pass = false; }
					if ((reg_d_ad == 5) && !F1) { test_pass = false; }
					if ((reg_d_ad == 6) && IRQPending) { test_pass = false; }
					if ((reg_d_ad == 7) && (Regs[A] == 0)) { test_pass = false; }
					if ((reg_d_ad == 8) && !FlagF0) { test_pass = false; }
					if ((reg_d_ad == 9) && (Regs[A] != 0)) { test_pass = false; }
					if ((reg_d_ad == 10) && FlagC) { test_pass = false; }
					if ((reg_d_ad == 11) && !FlagC) { test_pass = false; }

					if (test_pass) { cur_instr[instr_pntr + 6] = ALU2; }
					else { cur_instr[instr_pntr + 9] = IDLE; }
					break;
			}

			if (++irq_pntr == IRQS)
			{
				// then regular IRQ
				if (IRQPending && IntEn && INT_MSTR)
				{
					IRQPending = false;

					TraceCallback?.Invoke(new(disassembly: "====IRQ====", registerInfo: string.Empty));

					IRQ_(0);
					IRQCallback();
					instr_pntr = irq_pntr = 0;
				}
				else if (TIRQPending && TimIntEn && INT_MSTR)
				{
					TIRQPending = false;

					TraceCallback?.Invoke(new(disassembly: "====TIRQ====", registerInfo: string.Empty));

					IRQ_(1);
					IRQCallback();
					instr_pntr = irq_pntr = 0;
				}
				// otherwise start the next instruction
				else
				{
					PopulateCURINSTR(OP);
					instr_pntr = irq_pntr = 0;
					IRQS = -1;
				}
			}

			TotalExecutedCycles++;

			if (timer_en)
			{
				timer_prescale++;
				if (timer_prescale == 32 * 5)
				{
					timer_prescale = 0;
					if (Regs[TIM] == 255)
					{
						TF = true;
						if (TimIntEn)
						{
							TIRQPending = true;
							// Console.WriteLine("Timer: " + LY + " " + TotalExecutedCycles);
						}
					}
					Regs[TIM] = (ushort)((Regs[TIM] + 1) & 0xFF);
				}
			}

			if (counter_en)
			{
				// NOTE: Odyssey 2 games tend to enable the counter within a few cycles of a falling edge and expect to count to take place
				if (!T1 && T1_old && (TotalExecutedCycles > next_T1_check))
				{
					if (Regs[TIM] == 255)
					{
						TF = true;
						if (TimIntEn)
						{
							TIRQPending = true;
							// Console.WriteLine("Counter: " + LY + " " + TotalExecutedCycles);
						}
					}
					Regs[TIM] = (ushort)((Regs[TIM] + 1) & 0xFF);
				}
			}

			T1_old = T1;
		}

		// tracer stuff

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader => "I8048: PC, machine code, mnemonic, operands, registers (A, B, X, Y, US, SP, DP, CC), Cy, flags (CAFBIFT0T1TFR)";

		public TraceInfo State(bool disassemble = true)
			=> new(
				disassembly: $"{(disassemble ? Disassemble(Regs[PC], ReadMemory, out _) : "---")} ".PadRight(50),
				registerInfo: string.Format(
					"A:{0:X2} R0:{1:X2} R1:{2:X2} R2:{3:X2} R3:{4:X2} R4:{5:X2} R5:{6:X2} R6:{7:X2} R7:{8:X2} PSW:{9:X4} Cy:{10} LY:{11} {12}{13}{14}{15}{16}{17}{18}{19}{20}{21}{22}",
					Regs[A],
					Regs[(ushort)(R0 + RB)],
					Regs[(ushort)(R1 + RB)],
					Regs[(ushort)(R2 + RB)],
					Regs[(ushort)(R3 + RB)],
					Regs[(ushort)(R4 + RB)],
					Regs[(ushort)(R5 + RB)],
					Regs[(ushort)(R6 + RB)],
					Regs[(ushort)(R7 + RB)],
					Regs[PSW],
					TotalExecutedCycles,
					LY,
					FlagC ? "C" : "c",
					FlagAC ? "A" : "a",
					FlagF0 ? "F" : "f",
					FlagBS ? "B" : "b",
					IntEn ? "I" : "i",
					TimIntEn ? "N" : "n",
					F1 ? "F" : "f",
					T0 ? "T" : "t",
					T1 ? "T" : "t",
					TF ? "T" : "t",
					RB > 0 ? "R" : "r"));

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		private void PopulateCURINSTR(ushort d0 = 0, ushort d1 = 0, ushort d2 = 0, ushort d3 = 0, ushort d4 = 0, ushort d5 = 0, ushort d6 = 0, ushort d7 = 0, ushort d8 = 0,
			ushort d9 = 0, ushort d10 = 0, ushort d11 = 0, ushort d12 = 0, ushort d13 = 0, ushort d14 = 0, ushort d15 = 0, ushort d16 = 0, ushort d17 = 0, ushort d18 = 0,
			ushort d19 = 0, ushort d20 = 0, ushort d21 = 0, ushort d22 = 0, ushort d23 = 0, ushort d24 = 0, ushort d25 = 0, ushort d26 = 0)
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
		}

		// State Save/Load
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("I8048");

			ser.Sync(nameof(IntEn), ref IntEn);
			ser.Sync(nameof(TimIntEn), ref TimIntEn);
			ser.Sync(nameof(IRQPending), ref IRQPending);
			ser.Sync(nameof(TIRQPending), ref TIRQPending);
			ser.Sync(nameof(INT_MSTR), ref INT_MSTR);
			ser.Sync(nameof(test_pass), ref test_pass);

			ser.Sync(nameof(instr_pntr), ref instr_pntr);
			ser.Sync(nameof(cur_instr), ref cur_instr, false);
			ser.Sync(nameof(opcode_see), ref opcode_see);
			ser.Sync(nameof(IRQS), ref IRQS);
			ser.Sync(nameof(irq_pntr), ref irq_pntr);

			ser.Sync(nameof(LY), ref LY);
			ser.Sync(nameof(EA), ref EA);
			ser.Sync(nameof(TF), ref TF);
			ser.Sync(nameof(timer_en), ref timer_en);
			ser.Sync(nameof(counter_en), ref counter_en);
			ser.Sync(nameof(timer_prescale), ref timer_prescale);

			ser.Sync(nameof(RB), ref RB);
			ser.Sync(nameof(MB), ref MB);
			ser.Sync(nameof(Regs), ref Regs, false);
			
			ser.Sync(nameof(F1), ref F1);
			ser.Sync(nameof(T0), ref T0);
			ser.Sync(nameof(T1), ref T1);
			ser.Sync(nameof(T1_old), ref T1_old);

			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);
			ser.Sync(nameof(next_T1_check), ref next_T1_check);

			ser.EndSection();
		}
	}
}
