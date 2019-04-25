using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Fairchild F3850 (F8) CPU (Channel F-specific implementation)
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
	/// However, the Channel F does not use the timer or interrupt logic at all (as far as I can see) so we can hopefully just
	/// maintain the PC and DC here in the CPU and move the ROMs into the core.
	/// </summary>
	public sealed partial class F3850
	{
		// operations that can take place in an instruction
		public const ushort ROMC_01 = 1;
		public const ushort ROMC_02 = 2;
		public const ushort ROMC_03_S = 3;
		public const ushort ROMC_04 = 4;
		public const ushort ROMC_05 = 5;
		public const ushort ROMC_06 = 6;
		public const ushort ROMC_07 = 7;
		public const ushort ROMC_08 = 8;
		public const ushort ROMC_09 = 9;
		public const ushort ROMC_0A = 10;
		public const ushort ROMC_0B = 11;
		public const ushort ROMC_0C = 12;
		public const ushort ROMC_0D = 13;
		public const ushort ROMC_0E = 14;
		public const ushort ROMC_0F = 15;
		public const ushort ROMC_10 = 16;
		public const ushort ROMC_11 = 17;
		public const ushort ROMC_12 = 18;
		public const ushort ROMC_13 = 19;
		public const ushort ROMC_14 = 20;
		public const ushort ROMC_15 = 21;
		public const ushort ROMC_16 = 22;
		public const ushort ROMC_17 = 23;
		public const ushort ROMC_18 = 24;
		public const ushort ROMC_19 = 25;
		public const ushort ROMC_1A = 26;
		public const ushort ROMC_1B = 27;
		public const ushort ROMC_1C_S = 28;
		public const ushort ROMC_1D = 29;
		public const ushort ROMC_1E = 30;
		public const ushort ROMC_1F = 31;
		public const ushort ROMC_00_S = 32;
		public const ushort ROMC_00_L = 33;
		public const ushort ROMC_03_L = 34;
		public const ushort ROMC_1C_L = 35;

		public const ushort IDLE = 0;
		public const ushort END = 51;

		public const ushort OP_LR8 = 100;
		public const ushort OP_SHFT_R = 101;
		public const ushort OP_SHFT_L = 102;
		public const ushort OP_LNK = 103;
		public const ushort OP_DI = 104;
		public const ushort OP_EI = 105;
		public const ushort OP_INC8 = 106;
		public const ushort OP_AND8 = 107;
		public const ushort OP_OR8 = 108;
		public const ushort OP_XOR8 = 109;
		public const ushort OP_COM = 110;
		public const ushort OP_ADD8 = 111;
		public const ushort OP_CI = 112;
		public const ushort OP_IS_INC = 113;
		public const ushort OP_IS_DEC = 114;
		public const ushort OP_LISU = 115;
		public const ushort OP_LISL = 116;
		public const ushort OP_BT = 117;
		public const ushort OP_ADD8D = 118;
		public const ushort OP_BR7 = 119;
		public const ushort OP_BF = 120;
		public const ushort OP_IN = 121;
		public const ushort OP_OUT = 122;
		public const ushort OP_AS_IS = 123;
		public const ushort OP_XS_IS = 124;
		public const ushort OP_NS_IS = 125;
		public const ushort OP_CLEAR_FLAGS = 126;
		public const ushort OP_SET_FLAGS_SZ = 127;


		public F3850()
		{
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

			ALU_ClearFlags();
			FlagICB = false;
		}

		public IMemoryCallbackSystem MemoryCallbacks { get; set; }

		// Memory Access 
		public Func<ushort, byte> ReadMemory;
		public Action<ushort, byte> WriteMemory;
		public Func<ushort, byte> PeekMemory;
		public Func<ushort, byte> DummyReadMemory;

		// Hardware I/O Port Access
		public Func<ushort, byte> ReadHardware;
		public Action<ushort, byte> WriteHardware;

		public Action<ushort> OnExecFetch;

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

		/// <summary>
		/// Runs a single CPU clock cycle
		/// </summary>
		public void ExecuteOne()
		{
			switch (cur_instr[instr_pntr++])
			{
				// always the last tick within an opcode instruction cycle
				case END:
					OnExecFetch?.Invoke(RegPC0);
					TraceCallback?.Invoke(State());
					opcode = (byte)Regs[DB];
					instr_pntr = 0;
					FetchInstruction();
					break;

				// used as timing 'padding'
				case IDLE:
					break;

				// clears all flags except for ICB
				case OP_CLEAR_FLAGS:
					ALU_ClearFlags();
					break;

				// sets SIGN and CARRY flags based upon the supplied value
				case OP_SET_FLAGS_SZ:
					ALU_SetFlags_SZ(cur_instr[instr_pntr++]);
					break;

				// load one register into another (or databus)
				case OP_LR8:
					LR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// Shift register n bit positions to the right (zero fill)
				case OP_SHFT_R:
					ALU_SR_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// Shift register n bit positions to the left (zero fill)
				case OP_SHFT_L:
					ALU_SL_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (x) ADD y
				case OP_ADD8:
					ALU_ADD8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (x) ADD y (decimal)
				case OP_ADD8D:
					ALU_ADD8D_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// A <- (A) + (C)
				case OP_LNK:
					bool fc = FlagC;
					ALU_ClearFlags();

					if (fc)
					{
						ALU_ADD8_Func(A, ONE);
					}

					ALU_SetFlags_SZ(A);
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
					ALU_XOR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (y) XOR DB (complement accumulator)
				case OP_COM:
					Regs[A] = (byte)~Regs[A];
					ALU_ClearFlags();
					ALU_SetFlags_SZ(A);
					break;

				// x <- (x) + 1
				case OP_INC8:
					ALU_ClearFlags();
					ALU_ADD8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					ALU_SetFlags_SZ(A);
					break;

				// x <- (y) & DB
				case OP_AND8:
					ALU_AND8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// x <- (y) | DB
				case OP_OR8:
					ALU_OR8_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// DB + (x) + 1 (modify flags without saving result)
				case OP_CI:
					Regs[ALU0] = (byte)~Regs[cur_instr[instr_pntr++]];
					ALU_ADD8_Func(ALU0, DB, true);
					ALU_SetFlags_SZ(ALU0);
					break;

				// ISAR is incremented
				case OP_IS_INC:
					Regs[ISAR] = (ushort)((Regs[ISAR]& 0x38) | ((Regs[ISAR] + 1) & 0x07));
					break;

				// ISAR is decremented
				case OP_IS_DEC:
					Regs[ISAR] = (ushort)((Regs[ISAR] & 0x38) | ((Regs[ISAR] - 1) & 0x07));
					break;

				// set the upper octal ISAR bits (b3,b4,b5)
				case OP_LISU:
					Regs[ISAR] = (ushort) (((Regs[ISAR] & 0x07) | cur_instr[instr_pntr++]) & 0x3F);
					break;

				// set the lower octal ISAR bits (b0,b1,b2)
				case OP_LISL:
					Regs[ISAR] = (ushort) (((Regs[ISAR] & 0x38) | cur_instr[instr_pntr++]) & 0x3F);
					break;

				// test operand against status register
				case OP_BT:
					instr_pntr = 0;
					if ((Regs[W] & cur_instr[instr_pntr++]) != 0)
					{
						PopulateCURINSTR(
							// L
							ROMC_01, 
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							// S
							ROMC_00_S,          // DB <- ((PC0)); PC0++	
							IDLE,
							IDLE,
							END);
					}
					else
					{
						PopulateCURINSTR(
							// S
							ROMC_03_S,  
							IDLE,
							IDLE,
							IDLE,
							// S
							ROMC_00_S,          // DB < -((PC0)); PC0++
							IDLE,
							IDLE,
							END);
					}
					break;
					
				// Branch based on ISARL
				case OP_BR7:
					instr_pntr = 0;
					if ((Regs[ISAR] & 7) == 7)
					{
						PopulateCURINSTR(
							// S
							ROMC_03_S,			// DB/IO <- ((PC0)); PC0++
							//IDLE, <- lose a cycle that was stolen in the table
							IDLE,
							IDLE,
							// S
							ROMC_00_S,			// DB <- ((PC0)); PC0++
							IDLE,
							IDLE,
							END);
					}
					else
					{
						PopulateCURINSTR(
							// L
							ROMC_01,  
							//IDLE, <- lose a cycle that was stolen in the table
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							ROMC_00_S,          // DB <- ((PC0)); PC0++
							IDLE,
							IDLE,
							END);
					}
					break;

				//  PC0 <- PC0+n+1
				case OP_BF:
					instr_pntr = 0;
					if ((Regs[W] & cur_instr[instr_pntr++]) != 0)
					{
						PopulateCURINSTR(
							// S
							ROMC_03_S,          // DB/IO <- ((PC0)); PC0++
							IDLE,
							IDLE,
							IDLE,
							// S
							ROMC_00_S,          // DB <- ((PC0)); PC0++
							IDLE,
							IDLE,
							END);
					}
					else
					{
						PopulateCURINSTR(
							// L
							ROMC_01,  
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							IDLE,
							// S
							ROMC_00_S,          // DB <- ((PC0)); PC0++
							IDLE,
							IDLE,
							END);
					}
					break;

				// A <- (I/O Port 0 or 1) 
				case OP_IN:
					Regs[cur_instr[instr_pntr++]] = ReadHardware(cur_instr[instr_pntr++]);
					break;

				// I/O Port 0 or 1 <- (A)
				case OP_OUT:
					WriteHardware(cur_instr[instr_pntr++], (byte)cur_instr[instr_pntr++]);
					//OUT_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// Add the content of the SR register addressed by ISAR to A (Binary)
				case OP_AS_IS:
					ALU_ClearFlags();
					ALU_ADD8_Func(A, Regs[ISAR]);
					ALU_SetFlags_SZ(A);
					break;

				// XOR the content of the SR register addressed by ISAR to A
				case OP_XS_IS:
					ALU_ClearFlags();
					ALU_XOR8_Func(A, Regs[ISAR]);
					ALU_SetFlags_SZ(A);
					break;

				// AND the content of the SR register addressed by ISAR to A
				case OP_NS_IS:
					ALU_ClearFlags();
					ALU_AND8_Func(A, Regs[ISAR]);
					ALU_SetFlags_SZ(A);
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
					ADDS_Func(PC0l, PC0h, DB, ZERO);
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
					break;

				// Place the high order byte of DC0 on the data bus
				// CYCLE LENGTH: L
				case ROMC_06:
					Regs[DB] = (byte)Regs[DC0h];
					break;

				// Place the high order byte of PC1 on the data bus
				// CYCLE LENGTH: L
				case ROMC_07:
					Regs[DB] = (byte)Regs[PC1h];
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
					Regs[DB] = (byte)Regs[DC0l];
					break;

				// All devices add the 8-bit value on the data bus, treated as a signed binary number, to the data counter
				// CYCLE LENGTH: L
				case ROMC_0A:
					ADDS_Func(DC0l, DC0h, DB, ZERO);
					break;

				// The device whose address space includes the value in PC1 must place the low order byte of PC1 on the data bus
				// CYCLE LENGTH: L
				case ROMC_0B:
					Regs[DB] = (byte)Regs[PC1l];
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
					WriteHardware(Regs[IO], (byte)Regs[DB]);
					break;

				// During the prior cycle, the data bus specified the address of an I/O port. The device containing the addressed I/O port
				// must place the contents of the I/O port on the data bus. (Note that the contents of the timer and interrupt control
				// registers cannot be read back onto the data bus)
				// CYCLE LENGTH: L
				case ROMC_1B:
					Regs[DB] = ReadHardware(Regs[IO]);
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
					// we have no DC1 in this implementation
					break;

				// The device whose address space includes the contents of PC0 must place the low order byte of PC0 onto the data bus
				// CYCLE LENGTH: L
				case ROMC_1E:
					Regs[DB] = (byte)Regs[PC0l];
					break;

				// The device whose address space includes the contents of PC0 must place the high order byte of PC0 onto the data bus
				// CYCLE LENGTH: L
				case ROMC_1F:
					Regs[DB] = (byte)Regs[PC0h];
					break;
			}

			TotalExecutedCycles++;
		}

		public Action<TraceInfo> TraceCallback;

		public string TraceHeader => "F3850: PC, machine code, mnemonic, operands, registers (R0, R1, R2, R3, R4, R5, R6, R7, R8, J, HU, HL, KU, KL, QU, QL, Cy), flags (IOZCS)";

		public TraceInfo State(bool disassemble = true)
		{
			int bytes_read = 0;
			string disasm = disassemble ? Disassemble(RegPC0, ReadMemory, out bytes_read) : "---";
			string byte_code = null;

			for (ushort i = 0; i < bytes_read; i++)
			{
				byte_code += ReadMemory((ushort)(RegPC0 + i)).ToHexString(2);
				if (i < (bytes_read - 1))
				{
					byte_code += " ";
				}
			}

			return new TraceInfo
			{
				Disassembly = string.Format(
					"{0:X4}: {1} {2}",
					RegPC0,
					byte_code.PadRight(12),
					disasm.PadRight(26)),
				RegisterInfo = string.Format(
					"R0:{0:X2} R1:{1:X2} R2:{2:X2} R3:{3:X2} R4:{4:X2} R5:{5:X2} R6:{6:X2} R7:{7:X2} R8:{8:X2} J:{9:X2} HU:{10:X2} HL:{11:X2} KU:{12:X2} KL:{13:X2} QU:{14:X2} QL:{15:X2} Cy:{16} {17}{18}{19}{20}{21}",
					Regs[0],
					Regs[1],
					Regs[2],
					Regs[3],
					Regs[4],
					Regs[5],
					Regs[6],
					Regs[7],
					Regs[8],
					Regs[J],
					Regs[Hh],
					Regs[Hl],
					Regs[Kh],
					Regs[Kl],
					Regs[Qh],
					Regs[Ql],
					TotalExecutedCycles,
					FlagICB ? "I" : "i",
					FlagO ? "O" : "o",
					FlagZ ? "Z" : "z",
					FlagC ? "C" : "c",
					FlagS ? "S" : "s")
			};
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

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(F3850));
			ser.Sync(nameof(Regs), ref Regs, false);
			ser.Sync(nameof(cur_instr), ref cur_instr, false);
			ser.Sync(nameof(instr_pntr), ref instr_pntr);
			ser.EndSection();
		}
	}
}
