using System;

using BizHawk.Common;

// Intel Corp 8048
namespace BizHawk.Emulation.Common.Components.I8048
{
	public sealed partial class I8048
	{
		// operations that can take place in an instruction
		public const ushort IDLE = 0; 
		public const ushort OP = 1;
		public const ushort RD = 2;
		public const ushort WR = 3;
		public const ushort TR = 4;
		public const ushort ADD16BR = 5;
		public const ushort ADD8 = 6;
		public const ushort ADD8RAM = 7;
		public const ushort ADC8 = 8;
		public const ushort ADC8RAM = 9;
		public const ushort INC16 = 10;
		public const ushort INC8 = 11;
		public const ushort INCA = 12;
		public const ushort DEC16 = 13;
		public const ushort DEC8 = 14;
		public const ushort DECA = 15;
		public const ushort ROL = 16;
		public const ushort ROR = 17;
		public const ushort RLC = 18;
		public const ushort RRC = 19;
		public const ushort SWP = 20;
		public const ushort COM = 21;
		public const ushort CMC = 22;
		public const ushort CM0 = 23;
		public const ushort CM1 = 24;
		public const ushort DA = 25;
		public const ushort AND8 = 26;
		public const ushort AND8RAM = 27;
		public const ushort XOR8 = 28;
		public const ushort XOR8RAM = 29;
		public const ushort OR8 = 30;
		public const ushort OR8RAM = 31;
		public const ushort ASL = 32;
		public const ushort ASR = 33;
		public const ushort LSR = 34;
		public const ushort BIT = 35;
		public const ushort RD_INC = 36;
		public const ushort SET_ADDR = 37;
		public const ushort NEG = 38;
		public const ushort TST = 39;
		public const ushort CLR = 40;
		public const ushort CLC = 41;
		public const ushort CL0 = 42;
		public const ushort CL1 = 43;
		public const ushort EI = 44;
		public const ushort EN = 45;
		public const ushort DI = 46;
		public const ushort DN = 47;
		public const ushort TFR = 48;
		public const ushort ADD8BR = 49;
		public const ushort ABX = 50;
		public const ushort JPE = 51;
		public const ushort MSK = 52;
		public const ushort CMP8 = 53;
		public const ushort SUB16 = 54;
		public const ushort ADD16 = 55;
		public const ushort CMP16 = 56;
		public const ushort CMP16D = 57;
		public const ushort CLR_E = 63;
		public const ushort CLK_OUT = 64;
		public const ushort IN = 65;
		public const ushort OUT = 66;
		public const ushort XCH = 67;
		public const ushort XCH_RAM = 68;
		public const ushort XCHD_RAM = 69;
		public const ushort SEL_MB0 = 70;
		public const ushort SEL_MB1 = 71;
		public const ushort SEL_RB0 = 72;
		public const ushort SEL_RB1 = 73;
		public const ushort INC_RAM = 74;
		public const ushort RES_TF = 75;
		public const ushort MOV = 76;
		public const ushort MOV_RAM = 77;
		public const ushort MOVT = 78;
		public const ushort MOVT_RAM = 79;
		public const ushort ST_CNT = 80;
		public const ushort STP_CNT = 81;
		public const ushort ST_T = 82;

		public I8048()
		{
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			ResetInterrupts();
			TotalExecutedCycles = 0;
			Regs[PC] = 0xFFFE;
			PopulateCURINSTR(IDLE,
							IDLE,
							IDLE,
							RD_INC, ALU, PC,
							RD_INC, ALU2, PC,
							SET_ADDR, PC, ALU, ALU2);

			IRQS = 6;
			instr_pntr = irq_pntr = 0;
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
		public delegate void DoCDLCallbackType(ushort addr, I8048.eCDLogMemFlags flags);

		public DoCDLCallbackType CDLCallback;

		public enum eCDLogMemFlags
		{
			FetchFirst = 1,
			FetchOperand = 2,
			Data = 4,
			Write = 8
		};

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
					if (OnExecFetch != null) OnExecFetch(PC);
					if (TraceCallback != null) TraceCallback(State());
					if (CDLCallback != null) CDLCallback(PC, eCDLogMemFlags.FetchFirst);
					FetchInstruction(ReadMemory(Regs[PC]++));
					instr_pntr = 0;
					irq_pntr = -1;
					break;
				case RD:
					Read_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case RD_INC:
					Read_Inc_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case WR:
					Write_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TR:
					TR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case TFR:
					TFR_Func(cur_instr[instr_pntr++]);
					break;
				case SET_ADDR:
					reg_d_ad = cur_instr[instr_pntr++];
					reg_h_ad = cur_instr[instr_pntr++];
					reg_l_ad = cur_instr[instr_pntr++];

					// Console.WriteLine(reg_d_ad + " " + reg_h_ad + " " + reg_l_ad);
					// Console.WriteLine(Regs[reg_d_ad] + " " + Regs[reg_h_ad] + " " + Regs[reg_l_ad]);

					Regs[reg_d_ad] = (ushort)((Regs[reg_h_ad] << 8) | Regs[reg_l_ad]);
					break;
				case TST:
					TST_Func(cur_instr[instr_pntr++]);
					break;
				case CLR:
					CLR_Func(cur_instr[instr_pntr++]);
					break;
				case CLR_E:

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
				case ADC8:
					ADC8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case CMP8:
					CMP8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case INC16:
					INC16_Func(cur_instr[instr_pntr++]);
					break;
				case INC8:
					INC8_Func(cur_instr[instr_pntr++]);
					break;
				case DEC16:
					DEC16_Func(cur_instr[instr_pntr++]);
					break;
				case CMP16:
					CMP16_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case DEC8:
					DEC8_Func(cur_instr[instr_pntr++]);
					break;
				case ROL:
					ROL_Func(cur_instr[instr_pntr++]);
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
				case AND8:
					AND8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case XOR8:
					XOR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case OR8:
					OR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
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
				case BIT:
					BIT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;
				case CLK_OUT:

					break;
				case IN:

					break;
				case OUT:

					break;
				case XCH:

					break;
				case XCH_RAM:

					break;
				case XCHD_RAM:

					break;
				case SEL_MB0:

					break;
				case SEL_MB1:

					break;
				case SEL_RB0:

					break;
				case SEL_RB1:

					break;
				case INC_RAM:

					break;
				case RES_TF:

					break;
				case MOV:

					break;
				case MOV_RAM:

					break;
				case MOVT:

					break;
				case MOVT_RAM:

					break;
				case ST_CNT:

					break;
				case STP_CNT:

					break;
				case ST_T:

					break;
			}

			if (++irq_pntr == IRQS)
			{
				// then regular IRQ				
				if (IRQPending && IntEn)
				{
					IRQPending = false;

					if (TraceCallback != null) { TraceCallback(new TraceInfo { Disassembly = "====IRQ====", RegisterInfo = "" }); }

					IRQ_();
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
		}

		// tracer stuff

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader
		{
			get { return "MC6809: PC, machine code, mnemonic, operands, registers (A, B, X, Y, US, SP, DP, CC), Cy, flags (EFHINZVC)"; }
		}

		public TraceInfo State(bool disassemble = true)
		{
			ushort notused;

			return new TraceInfo
			{
				Disassembly = $"{(disassemble ? Disassemble(Regs[PC], ReadMemory, out notused) : "---")} ".PadRight(50),
				RegisterInfo = string.Format(
					"A:{0:X2} R0:{1:X2} R1:{2:X2} R2:{3:X2} R3:{4:X2} R4:{5:X2} R5:{6:X2} R6:{7:X2} R7:{8:X2} PSW:{9:X4} Cy:{10} {11}{12}{13}{14}  {15}{16}{17}{18}",
					Regs[A],
					Regs[R0],
					Regs[R1],
					Regs[R2],
					Regs[R3],
					Regs[R4],
					Regs[R5],
					Regs[R6],
					Regs[R7],
					Regs[PSW],
					TotalExecutedCycles,
					FlagC ? "C" : "c",
					FlagAC ? "A" : "a",
					FlagF0 ? "F" : "f",
					FlagBS ? "B" : "b",
					IntEn ? "I" : "i",
					F1 ? "F" : "f",
					T0 ? "T" : "t",
					T1 ? "T" : "t"
					)
			};
		}

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		private void PopulateCURINSTR(ushort d0 = 0, ushort d1 = 0, ushort d2 = 0, ushort d3 = 0, ushort d4 = 0, ushort d5 = 0, ushort d6 = 0, ushort d7 = 0, ushort d8 = 0,
			ushort d9 = 0, ushort d10 = 0, ushort d11 = 0, ushort d12 = 0, ushort d13 = 0, ushort d14 = 0, ushort d15 = 0, ushort d16 = 0, ushort d17 = 0, ushort d18 = 0,
			ushort d19 = 0, ushort d20 = 0, ushort d21 = 0, ushort d22 = 0, ushort d23 = 0, ushort d24 = 0, ushort d25 = 0, ushort d26 = 0, ushort d27 = 0, ushort d28 = 0,
			ushort d29 = 0, ushort d30 = 0, ushort d31 = 0, ushort d32 = 0, ushort d33 = 0, ushort d34 = 0, ushort d35 = 0, ushort d36 = 0, ushort d37 = 0, ushort d38 = 0)
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
			cur_instr[36] = d36; cur_instr[37] = d37; cur_instr[38] = d38;
		}

		// State Save/Load
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("MC6809");

			ser.Sync(nameof(IntEn), ref IntEn);
			ser.Sync(nameof(IRQPending), ref IRQPending);

			ser.Sync(nameof(instr_pntr), ref instr_pntr);
			ser.Sync(nameof(cur_instr), ref cur_instr, false);
			ser.Sync(nameof(opcode_see), ref opcode_see);
			ser.Sync(nameof(IRQS), ref IRQS);
			ser.Sync(nameof(irq_pntr), ref irq_pntr);

			ser.Sync(nameof(TF), ref TF);
			ser.Sync(nameof(timer_en), ref timer_en);

			ser.Sync(nameof(Regs), ref Regs, false);
			ser.Sync(nameof(RAM), ref RAM, false);
			
			ser.Sync(nameof(F1), ref F1);
			ser.Sync(nameof(T0), ref T0);
			ser.Sync(nameof(T1), ref T1);

			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);

			ser.EndSection();
		}
	}
}
