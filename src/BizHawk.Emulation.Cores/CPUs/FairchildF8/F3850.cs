using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.FairchildF8
{
	/// <summary>
	/// Fairchild F3850 (F8) CPU
	/// Author: Asnivor
	/// 
	/// The F8 microprocessor is made up of separate interchangeable devices
	/// The Channel F has:
	///		* x1 F3850 CPU (central processing unit)
	///		* x2 F3851 PSU (program storage unit)
	/// The CPU does not have its own data counters or program counters, rather each F8 component connected to the CPU
	/// holds their own PCs and SPs and are all connected to the ROMC (ROM control) pins that are serviced by the CPU.
	/// Every device must respond to changes in the CPU ROMC pins output and they each update their PCs and DCs in the same way.
	/// e.g. SPs and PCs should always be identical
	/// Each device has a factory ROM mask applied and with every ROMC change observed is able to know whether it should respond (via the shared data bus)
	/// or not based on the value within its counters.
	/// 
	/// For this reason we will hold the PCs and SPs within the F3850 implementation.
	/// 
	/// We are currently also *not* using a separate F3851 implementation. In reality the F3851 chip has/does:
	///		* 1024 byte masked ROM
	///		* x2 16-bit program counters
	///		* x1 16-bit data counter
	///		* Programmable timer
	///		* Interrupt logic
	///
	/// Note: Programmable timer and interrupt logic from the F3851 is not currently emulated
	/// </summary>
	/// <remarks>
	/// this type parameter might look useless—and it is—but after monomorphisation,
	/// this way happens to perform better than the alternative
	/// </remarks>
	/// <seealso cref="IF3850Link"/>
	public sealed partial class F3850<TLink> where TLink : IF3850Link
	{
		// operations that can take place in an instruction
		public const byte ROMC_01 = 1;
		public const byte ROMC_02 = 2;
		public const byte ROMC_03_S = 3;
		public const byte ROMC_04 = 4;
		public const byte ROMC_05 = 5;
		public const byte ROMC_06 = 6;
		public const byte ROMC_07 = 7;
		public const byte ROMC_08 = 8;
		public const byte ROMC_09 = 9;
		public const byte ROMC_0A = 10;
		public const byte ROMC_0B = 11;
		public const byte ROMC_0C = 12;
		public const byte ROMC_0D = 13;
		public const byte ROMC_0E = 14;
		public const byte ROMC_0F = 15;
		public const byte ROMC_10 = 16;
		public const byte ROMC_11 = 17;
		public const byte ROMC_12 = 18;
		public const byte ROMC_13 = 19;
		public const byte ROMC_14 = 20;
		public const byte ROMC_15 = 21;
		public const byte ROMC_16 = 22;
		public const byte ROMC_17 = 23;
		public const byte ROMC_18 = 24;
		public const byte ROMC_19 = 25;
		public const byte ROMC_1A = 26;
		public const byte ROMC_1B = 27;
		public const byte ROMC_1C_S = 28;
		public const byte ROMC_1D = 29;
		public const byte ROMC_1E = 30;
		public const byte ROMC_1F = 31;
		public const byte ROMC_00_S = 32;
		public const byte ROMC_00_L = 33;
		public const byte ROMC_03_L = 34;
		public const byte ROMC_1C_L = 35;

		public const byte IDLE = 0;
		public const byte END = 51;

		public const byte OP_LR8 = 100;
		public const byte OP_SHFT_R = 101;
		public const byte OP_SHFT_L = 102;
		public const byte OP_LNK = 103;
		public const byte OP_DI = 104;
		public const byte OP_EI = 105;
		public const byte OP_INC8 = 106;
		public const byte OP_AND8 = 107;
		public const byte OP_OR8 = 108;
		public const byte OP_XOR8 = 109;
		public const byte OP_COM = 99;
		public const byte OP_SUB8 = 110;
		public const byte OP_ADD8 = 111;
		public const byte OP_CI = 112;
		public const byte OP_IS_INC = 113;
		public const byte OP_IS_DEC = 114;
		public const byte OP_LISU = 115;
		public const byte OP_LISL = 116;
		public const byte OP_BT = 117;
		public const byte OP_ADD8D = 118;
		public const byte OP_BR7 = 119;		
		public const byte OP_BF = 141;

		public const byte OP_IN = 151;
		public const byte OP_OUT = 152;
		public const byte OP_LR_A_DB_IO = 156;
		public const byte OP_DS = 157;
		public const byte OP_LIS = 158;

		private readonly TLink _link;

		public F3850(TLink link)
		{
			_link = link;
			Reset();
		}

		public void Reset()
		{
			ResetRegisters();
			TotalExecutedCycles = 0;
			instr_pntr = 0;

			PopulateCURINSTR(
				ROMC_1C_S,      // S
				IDLE,
				IDLE,
				IDLE,
				ROMC_08,		// L
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,		// S
				IDLE,
				IDLE,
				END);

			ClearFlags_Func();
			FlagICB = false;
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; set; }

		/// <summary>
		/// Runs a single CPU clock cycle
		/// </summary>
		public void ExecuteOne()
		{
			if (Regs[ISAR] > 0x3F)
			{

			}
			if (Regs[W] > 0x1F)
			{ 
			}

			switch (cur_instr[instr_pntr++])
			{
				// always the last tick within an opcode instruction cycle
				case END:
					_link.OnExecFetch(RegPC0);
					TraceCallback?.Invoke(State());
					opcode = Regs[DB];
					instr_pntr = 0;
					FetchInstruction();
					break;

				// used as timing 'padding'
				case IDLE:
					break;

				// load one register into another (or databus)
				case OP_LR8:
					LR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// load DB into A (as a part of an IN or INS instruction)
				case OP_LR_A_DB_IO:
					LR_A_IO_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// loads supplied index value into the bottom 4 bits of a register (upper bits are set to 0)
				case OP_LIS:
					Regs[ALU1] = (byte)(cur_instr[instr_pntr++] & 0x0F);
					LR_Func(A, ALU1);
					break;

				// Shift register n bit positions to the right (zero fill)
				case OP_SHFT_R:
					SR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// Shift register n bit positions to the left (zero fill)
				case OP_SHFT_L:
					SL_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (x) ADD y
				case OP_ADD8:
					ADD_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (x) MINUS y
				case OP_SUB8:
					SUB_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (x) ADD y (decimal)
				case OP_ADD8D:
					ADDD_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// A <- (A) + (C)
				case OP_LNK:
					Regs[ALU0] = (byte)(FlagC ? 1 : 0);
					ADD_Func(A, ALU0);
					break;

				// Clear ICB status bit
				case OP_DI:
					FlagICB = false;
					break;

				// Set ICB status bit
				case OP_EI:
					FlagICB = true;
					break;

				// x <- (y) XOR DB
				case OP_XOR8:
					XOR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// The accumulator is loaded with its one's complement
				case OP_COM:
					XOR_Func(A, BYTE);
					//Regs[A] = (byte)(Regs[A] ^ 0xFF);
					break;

				// x <- (x) + 1
				case OP_INC8:
					ADD_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (y) & DB
				case OP_AND8:
					AND_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (y) | DB
				case OP_OR8:
					OR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// DB + (x) + 1 (modify flags without saving result)
				case OP_CI:
					CI_Func();
					break;

				// ISAR is incremented
				case OP_IS_INC:
					Regs[ISAR] = (byte)((Regs[ISAR] & 0x38) | ((Regs[ISAR] + 1) & 0x07));
					break;

				// ISAR is decremented
				case OP_IS_DEC:
					Regs[ISAR] = (byte)((Regs[ISAR] & 0x38) | ((Regs[ISAR] - 1) & 0x07));
					break;

				// set the upper octal ISAR bits (b3,b4,b5) but do not alter the three least significant bits
				case OP_LISU:
					//Regs[ISAR] = (byte)(((Regs[ISAR] & 0x07) | (cur_instr[instr_pntr++] & 0x07) << 3) & 0x3F);
					Regs[ISAR] = (byte)((Regs[ISAR] & 0x07) | cur_instr[instr_pntr++]);
					break;

				// set the lower octal ISAR bits (b0,b1,b2) but do not alter the three most significant bits
				case OP_LISL:
					//Regs[ISAR] = (byte) (((Regs[ISAR] & 0x38) | (cur_instr[instr_pntr++] & 0x07)) & 0x3F);
					Regs[ISAR] = (byte)((Regs[ISAR] & 0x38) | cur_instr[instr_pntr++]);
					break;

				// Branch on TRUE
				case OP_BT:
					if ((Regs[W] & cur_instr[instr_pntr++]) > 0)
						DO_BRANCH(0);
					else
						DONT_BRANCH(0);
					break;

				// Branch on ISARL - if any of the low 3 bits of ISAR are reset
				case OP_BR7:
				
				if (!Regs[ISAR].Bit(0) || !Regs[ISAR].Bit(1) || !Regs[ISAR].Bit(2))
					DO_BRANCH(1);
				else
					DONT_BRANCH(1);
					break;				

				// Branch on FALSE
				case OP_BF:
					if ((Regs[W] & cur_instr[instr_pntr++]) > 0)
						DONT_BRANCH(0);
					else
						DO_BRANCH(0);
					break;
					
				// A <- (I/O Port 0 or 1) 
				case OP_IN:
					IN_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// I/O Port 0 or 1 <- (A)
				case OP_OUT:
					OUT_Func(IO, A);
					break;

				// instruction fetch
				// The device whose address space includes the contents of the PC0 register must place on the data bus the op code addressed by PC0;
				// then all devices increments the content of PC0.
				// CYCLE LENGTH: S
				case ROMC_00_S:
					Read_Func(DB, PC0l, PC0h);
					RegPC0++;
					break;

				// instruction fetch
				// The device whose address space includes the contents of the PC0 register must place on the data bus the op code addressed by PC0;
				// then all devices increments the content of PC0.
				// CYCLE LENGTH: L
				case ROMC_00_L:
					Read_Func(DB, PC0l, PC0h);
					RegPC0++;
					break;

				// The device whose address space includes the contents of the PC0 register must place on the data bus the contents of the memory location
				// addressed by by PC0; then all devices add the 8-bit value on the data bus, as a signed binary number, to PC0
				// CYCLE LENGTH: L
				case ROMC_01:
					Read_Func(DB, PC0l, PC0h);
					RegPC0 = Regs[DB].Bit(7) ? (ushort)(RegPC0 - (byte)((Regs[DB] ^ 0xFF) + 1)) : (ushort)(RegPC0 + Regs[DB]);
					break;

				// The device whose DC0 address addresses a memory word within the address space of that device must place on the data bus the contents
				// of the memory location addressed by DC0; then all devices increment DC0
				// CYCLE LENGTH: L
				case ROMC_02:
					Read_Func(DB, DC0l, DC0h);
					RegDC0++;
					break;

				// Similar to 0x00, except that it is used for Immediate Operand fetches (using PC0) instead of instruction fetches
				// CYCLE LENGTH: S
				case ROMC_03_S:
					Read_Func(DB, PC0l, PC0h);
					RegPC0++;
					Regs[IO] = Regs[DB];
					break;

				// Similar to 0x00, except that it is used for Immediate Operand fetches (using PC0) instead of instruction fetches
				// CYCLE LENGTH: L
				case ROMC_03_L:
					Read_Func(DB, PC0l, PC0h);
					RegPC0++;
					Regs[IO] = Regs[DB];
					break;

				// Copy the contents of PC1 into PC0
				// CYCLE LENGTH: S
				case ROMC_04:
					RegPC0 = RegPC1;
					break;

				// Store the data bus contents into the memory location pointed to by DC0; increment DC0
				// CYCLE LENGTH: L
				case ROMC_05:
					Write_Func(DC0l, DC0h, DB);
					RegDC0++;
					break;

				// Place the high order byte of DC0 on the data bus
				// CYCLE LENGTH: L
				case ROMC_06:
					Regs[DB] = Regs[DC0h];
					break;

				// Place the high order byte of PC1 on the data bus
				// CYCLE LENGTH: L
				case ROMC_07:
					Regs[DB] = Regs[PC1h];
					break;

				// All devices copy the contents of PC0 into PC1. The CPU outputs zero on the data bus in this ROMC state.
				// Load the data bus into both halves of PC0, this clearing the register.
				// CYCLE LENGTH: L
				case ROMC_08:
					RegPC1 = RegPC0;
					Regs[DB] = 0;
					Regs[PC0h] = 0;
					Regs[PC0l] = 0;
					break;

				// The device whose address space includes the contents of the DC0 register must place the low order byte of DC0 onto the data bus
				// CYCLE LENGTH: L
				case ROMC_09:
					Regs[DB] = Regs[DC0l];
					break;

				// All devices add the 8-bit value on the data bus, treated as a signed binary number, to the data counter
				// CYCLE LENGTH: L
				case ROMC_0A:
					// The contents of the accumulator are treated as a signed binary number, and are added to the contents of every DCO register.
					RegDC0 = Regs[DB].Bit(7) ? (ushort)(RegDC0 - (byte)((Regs[DB] ^ 0xFF) + 1)) : (ushort)(RegDC0 + Regs[DB]);
					break;

				// The device whose address space includes the value in PC1 must place the low order byte of PC1 on the data bus
				// CYCLE LENGTH: L
				case ROMC_0B:
					Regs[DB] = Regs[PC1l];
					break;

				// The device whose address space includes the contents of the PC0 register must place the contents of the memory word addressed by PC0
				// onto the data bus; then all devices move the value that has just been placed on the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_0C:
					Read_Func(DB, PC0l, PC0h);
					Regs[PC0l] = Regs[DB];
					break;

				// All devices store in PC1 the current contents of PC0, incremented by 1; PC0 is unaltered
				// CYCLE LENGTH: S
				case ROMC_0D:
					RegPC1 = (ushort)(RegPC0 + 1);
					break;

				// The device whose address space includes the contents of PC0 must place the contents of the word addressed by PC0 onto the data bus.
				// The value on the data bus is then moved to the low order byte of DC0 by all devices
				// CYCLE LENGTH: L
				case ROMC_0E:
					Read_Func(DB, PC0l, PC0h);
					Regs[DC0l] = Regs[DB];
					break;

				// The interrupting device with the highest priority must place the low order byte of the interrupt vector on the data bus.
				// All devices must copy the contents of PC0 into PC1. All devices must move the contents of the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_0F:
					throw new NotImplementedException("ROMC 0x0F not implemented");

				// Inhibit any modification to the interrupt priority logic
				// CYCLE LENGTH: L
				case ROMC_10:
					throw new NotImplementedException("ROMC 0x10 not implemented");

				// The device whose memory space includes the contents of PC0 must place the contents of the addressed memory word on the data bus.
				// All devices must then move the contents of the data bus to the upper byte of DC0
				// CYCLE LENGTH: L
				case ROMC_11:
					Read_Func(DB, PC0l, PC0h);
					Regs[DC0h] = Regs[DB];
					break;

				// All devices copy the contents of PC0 into PC1. All devices then move the contents of the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_12:
					RegPC1 = RegPC0;
					Regs[PC0l] = Regs[DB];
					break;

				// The interrupting device with the highest priority must move the high order half of the interrupt vector onto the data bus.
				// All devices must move the conetnts of the data bus into the high order byte of of PC0. The interrupting device resets its
				// interrupt circuitry (so that it is no longer requesting CPU servicing and can respond to another interrupt)
				// CYCLE LENGTH: L
				case ROMC_13:
					throw new NotImplementedException("ROMC 0x13 not implemented");

				// All devices move the contents of the data bus into the high order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_14:
					Regs[PC0h] = Regs[DB];
					break;

				// All devices move the contents of the data bus into the high order byte of PC1
				// CYCLE LENGTH: L
				case ROMC_15:
					Regs[PC1h] = Regs[DB];
					break;

				// All devices move the contents of the data bus into the high order byte of DC0
				// CYCLE LENGTH: L
				case ROMC_16:
					Regs[DC0h] = Regs[DB];
					break;

				// All devices move the contents of the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_17:
					Regs[PC0l] = Regs[DB];
					break;

				// All devices move the contents of the data bus into the low order byte of PC1
				// CYCLE LENGTH: L
				case ROMC_18:
					Regs[PC1l] = Regs[DB];
					break;

				// All devices move the contents of the data bus into the low order byte of DC0
				// CYCLE LENGTH: L
				case ROMC_19:
					Regs[DC0l] = Regs[DB];
					break;

				// During the prior cycle, an I/O port timer or interrupt control register was addressed; the device containing the addressed
				// port must move the current contents of the data bus into the addressed port
				// CYCLE LENGTH: L
				case ROMC_1A:
					OUT_Func(IO, DB);
					break;

				// During the prior cycle, the data bus specified the address of an I/O port. The device containing the addressed I/O port
				// must place the contents of the I/O port on the data bus. (Note that the contents of the timer and interrupt control
				// registers cannot be read back onto the data bus)
				// CYCLE LENGTH: L
				case ROMC_1B:
					IN_Func(DB, IO);
					break;

				// None
				// CYCLE LENGTH: S
				case ROMC_1C_S:
					break;

				// None
				// CYCLE LENGTH: L
				case ROMC_1C_L:
					break;

				// Devices with DC0 and DC1 registers must switch registers. Devices without a DC1 register perform no operation
				// CYCLE LENGTH: S
				case ROMC_1D:
					ushort temp = RegDC0;
					RegDC0 = RegDC1;
					RegDC1 = temp;
					break;

				// The device whose address space includes the contents of PC0 must place the low order byte of PC0 onto the data bus
				// CYCLE LENGTH: L
				case ROMC_1E:
					Regs[DB] = Regs[PC0l];
					break;

				// The device whose address space includes the contents of PC0 must place the high order byte of PC0 onto the data bus
				// CYCLE LENGTH: L
				case ROMC_1F:
					Regs[DB] = Regs[PC0h];
					break;
			}

			TotalExecutedCycles++;
		}

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader => "F3850: PC, machine code, mnemonic, operands, flags (IOZCS), registers (PC1, DC0, A, ISAR, DB, IO, J, H, K, Q, R00-R63), Cycles";

		public TraceInfo State(bool disassemble = true)
		{
			int bytes_read = 0;
			ushort pc = (ushort)(RegPC0 - 1);
			string disasm = disassemble ? Disassemble(pc, _link.ReadMemory, out bytes_read) : "---";
			string byte_code = null;

			for (ushort i = 0; i < bytes_read; i++)
			{
				byte_code += _link.ReadMemory((ushort)(pc + i)).ToString("X2");
				if (i < (bytes_read - 1))
				{
					byte_code += " ";
				}
			}

			return new(
				disassembly: string.Format(
					"{0:X4}: {1} {2}",
					pc,
					byte_code.PadRight(12),
					disasm.PadRight(26)),
				registerInfo: string.Format(
					"Flags:{75}{76}{77}{78}{79} " + 
					"PC1:{0:X4} DC0:{1:X4} A:{2:X2} ISAR:{3:X2} DB:{4:X2} IO:{5:X2} J:{6:X2} H:{7:X4} K:{8:X4} Q:{9:X4} " + 
					"R0:{10:X2} R1:{11:X2} R2:{12:X2} R3:{13:X2} R4:{14:X2} R5:{15:X2} R6:{16:X2} R7:{17:X2} R8:{18:X2} R9:{19:X2} " +
					"R10:{20:X2} R11:{21:X2} R12:{22:X2} R13:{23:X2} R14:{24:X2} R15:{25:X2} R16:{26:X2} R17:{27:X2} R18:{28:X2} R19:{29:X2} " +
					"R20:{30:X2} R21:{31:X2} R22:{32:X2} R23:{33:X2} R24:{34:X2} R25:{35:X2} R26:{36:X2} R27:{37:X2} R28:{38:X2} R29:{39:X2} " +
					"R30:{40:X2} R31:{41:X2} R32:{42:X2} R33:{43:X2} R34:{44:X2} R35:{45:X2} R36:{46:X2} R37:{47:X2} R38:{48:X2} R39:{49:X2} " +
					"R40:{50:X2} R41:{51:X2} R42:{52:X2} R43:{53:X2} R44:{54:X2} R45:{55:X2} R46:{56:X2} R47:{57:X2} R48:{58:X2} R49:{59:X2} " +
					"R50:{60:X2} R51:{61:X2} R52:{62:X2} R53:{63:X2} R54:{64:X2} R55:{65:X2} R56:{66:X2} R57:{67:X2} R58:{68:X2} R59:{69:X2} " +
					"R60:{70:X2} R61:{71:X2} R62:{72:X2} R63:{73:X2} " +
					"Cy:{74}",
					RegPC1,
					RegDC0,
					Regs[A],
					Regs[ISAR],
					Regs[DB],
					Regs[IO],
					Regs[J],
					(ushort)(Regs[Hl] | (Regs[Hh] << 8)),
					(ushort)(Regs[Kl] | (Regs[Kh] << 8)),
					(ushort)(Regs[Ql] | (Regs[Qh] << 8)),
					Regs[0], Regs[1], Regs[2], Regs[3], Regs[4], Regs[5], Regs[6], Regs[7], Regs[8], Regs[9],
					Regs[10], Regs[11], Regs[12], Regs[13], Regs[14], Regs[15], Regs[16], Regs[17], Regs[18], Regs[19],
					Regs[20], Regs[21], Regs[22], Regs[23], Regs[24], Regs[25], Regs[26], Regs[27], Regs[28], Regs[29],
					Regs[30], Regs[31], Regs[32], Regs[33], Regs[34], Regs[35], Regs[36], Regs[37], Regs[38], Regs[39],
					Regs[40], Regs[41], Regs[42], Regs[43], Regs[44], Regs[45], Regs[46], Regs[47], Regs[48], Regs[49],
					Regs[50], Regs[51], Regs[52], Regs[53], Regs[54], Regs[55], Regs[56], Regs[57], Regs[58], Regs[59],
					Regs[60], Regs[61], Regs[62], Regs[63],
					TotalExecutedCycles,
					FlagICB ? "I" : "i",
					FlagO ? "O" : "o",
					FlagZ ? "Z" : "z",
					FlagC ? "C" : "c",
					FlagS ? "S" : "s"));
		}

		/// <summary>
		/// Optimization method to set cur_instr
		/// </summary>	
		private void PopulateCURINSTR(byte d0 = 0, byte d1 = 0, byte d2 = 0, byte d3 = 0, byte d4 = 0, byte d5 = 0, byte d6 = 0, byte d7 = 0, byte d8 = 0,
			byte d9 = 0, byte d10 = 0, byte d11 = 0, byte d12 = 0, byte d13 = 0, byte d14 = 0, byte d15 = 0, byte d16 = 0, byte d17 = 0, byte d18 = 0,
			byte d19 = 0, byte d20 = 0, byte d21 = 0, byte d22 = 0, byte d23 = 0, byte d24 = 0, byte d25 = 0, byte d26 = 0, byte d27 = 0, byte d28 = 0,
			byte d29 = 0, byte d30 = 0, byte d31 = 0, byte d32 = 0, byte d33 = 0, byte d34 = 0, byte d35 = 0, byte d36 = 0, byte d37 = 0,
			byte d38 = 0, byte d39 = 0, byte d40 = 0, byte d41 = 0, byte d42 = 0, byte d43 = 0, byte d44 = 0, byte d45 = 0, byte d46 = 0, byte d47 = 0)
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
			cur_instr[36] = d36; cur_instr[37] = d37; cur_instr[37] = d38;
			cur_instr[39] = d36; cur_instr[40] = d37; cur_instr[41] = d38;
			cur_instr[42] = d36; cur_instr[43] = d37; cur_instr[44] = d38;
			cur_instr[45] = d36; cur_instr[46] = d37; cur_instr[47] = d38;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("F3850");
			ser.Sync(nameof(Regs), ref Regs, false);
			ser.Sync(nameof(cur_instr), ref cur_instr, false);
			ser.Sync(nameof(instr_pntr), ref instr_pntr);
			ser.EndSection();
		}
	}
}
