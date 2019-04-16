using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BizHawk.Common;
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
		
		//public const ushort OP = 1;
		//public const ushort LR_8 = 2;
		//public const ushort LR_16 = 3;


		public const ushort ROMC_00_S = 40;
		public const ushort ROMC_00_L = 41;
		public const ushort ROMC_01 = 1;
		public const ushort ROMC_02 = 2;
		public const ushort ROMC_03_S = 3;
		public const ushort ROMC_03_L = 33;
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
		public const ushort ROMC_1C_L = 34;
		public const ushort ROMC_1D = 29;
		public const ushort ROMC_1E = 30;
		public const ushort ROMC_1F = 31;

		public const ushort IDLE = 0;
		public const ushort END = 51;

		public const ushort OP_LR_8 = 100;
		public const ushort OP_SHFT_R = 101;
		public const ushort OP_SHFT_L = 102;
		public const ushort OP_COM = 103;
		public const ushort OP_LNK = 104;
		public const ushort OP_DI = 105;
		public const ushort OP_EI = 106;
		public const ushort OP_INC8 = 107;


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
				ROMC_08,      // S
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				IDLE,
				ROMC_00_S,
				IDLE,
				IDLE,
				IDLE,
				END);

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
					opcode = databus;
					instr_pntr = 0;
					FetchInstruction();
					break;

				// used as timing 'padding'
				case IDLE:
					break;

				// load one register into another (or databus)
				case OP_LR_8:
					LoadReg_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// Shift register n bit positions to the right (zero fill)
				case OP_SHFT_R:
					ShiftRight_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// Shift register n bit positions to the left (zero fill)
				case OP_SHFT_L:
					ShiftLeft_Func(cur_instr[instr_pntr++], cur_instr[instr_pntr++]);
					break;

				// A <- A ^ 255
				case OP_COM:
					COM_Func(A);
					break;

				// A <- (A) + (C)
				case OP_LNK:
					ADD8_Func(Regs[A], (ushort)(FlagC ? 1 : 0));
					break;

				case OP_DI:
					FlagICB = false;
					break;

				case OP_EI:
					FlagICB = true;
					break;

				case OP_INC8:
					INC8_Func(cur_instr[instr_pntr++]);
					break;



				// instruction fetch
				// The device whose address space includes the contents of the PC0 register must place on the data bus the op code addressed by PC0;
				// then all devices increments the content of PC0.
				// CYCLE LENGTH: S
				case ROMC_00_S:
					databus = ReadMemory(RegPC0++);
					break;

				// instruction fetch
				// The device whose address space includes the contents of the PC0 register must place on the data bus the op code addressed by PC0;
				// then all devices increments the content of PC0.
				// CYCLE LENGTH: L
				case ROMC_00_L:
					databus = ReadMemory(RegPC0++);
					break;

				// The device whose address space includes the contents of the PC0 register must place on the data bus the contents of the memory location
				// addressed by by PC0; then all devices add the 8-bit value on the data bus, as a signed binary number, to PC0
				// CYCLE LENGTH: L
				case ROMC_01:
					databus = ReadMemory(RegPC0);
					IncrementBySignedByte(RegPC0, databus);
					break;

				// The device whose DC0 address addresses a memory word within the address space of that device must place on the data bus the contents
				// of the memory location addressed by DC0; then all devices increment DC0
				// CYCLE LENGTH: L
				case ROMC_02:
					databus = ReadMemory(RegDC0++);
					break;

				// Similar to 0x00, except that it is used for Immediate Operand fetches (using PC0) instead of instruction fetches
				// CYCLE LENGTH: S
				case ROMC_03_S:
					databus = ReadMemory(RegPC0++);
					iobus = databus;
					break;

				// Similar to 0x00, except that it is used for Immediate Operand fetches (using PC0) instead of instruction fetches
				// CYCLE LENGTH: L
				case ROMC_03_L:
					databus = ReadMemory(RegPC0++);
					iobus = databus;
					break;

				// Copy the contents of PC1 into PC0
				// CYCLE LENGTH: S
				case ROMC_04:
					RegPC0 = RegPC1;
					break;

				// Store the data bus contents into the memory location pointed to by DC0; increment DC0
				// CYCLE LENGTH: L
				case ROMC_05:
					WriteMemory(RegDC0++, databus);
					break;

				// Place the high order byte of DC0 on the data bus
				// CYCLE LENGTH: L
				case ROMC_06:
					databus = (byte)Regs[DC0h];
					break;

				// Place the high order byte of PC1 on the data bus
				// CYCLE LENGTH: L
				case ROMC_07:
					databus = (byte)Regs[PC1h];
					break;

				// All devices copy the contents of PC0 into PC1. The CPU outputs zero on the data bus in this ROMC state.
				// Load the data bus into both halves of PC0, this clearing the register.
				// CYCLE LENGTH: L
				case ROMC_08:
					RegPC1 = RegPC0;
					databus = 0;
					Regs[PC0h] = 0;
					Regs[PC0l] = 0;
					break;

				// The device whose address space includes the contents of the DC0 register must place the low order byte of DC0 onto the data bus
				// CYCLE LENGTH: L
				case ROMC_09:
					databus = (byte)Regs[DC0l];
					break;

				// All devices add the 8-bit value on the data bus, treated as a signed binary number, to the data counter
				// CYCLE LENGTH: L
				case ROMC_0A:
					IncrementBySignedByte(RegDC0, databus);
					break;

				// The device whose address space includes the value in PC1 must place the low order byte of PC1 on the data bus
				// CYCLE LENGTH: L
				case ROMC_0B:
					databus = (byte)Regs[PC1l];
					break;

				// The device whose address space includes the contents of the PC0 register must place the contents of the memory word addressed by PC0
				// onto the data bus; then all devices move the value that has just been placed on the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_0C:
					databus = ReadMemory(RegPC0);
					Regs[PC0l] = databus;
					break;

				// All devices store in PC1 the current contents of PC0, incremented by 1; PC1 is unaltered
				// CYCLE LENGTH: S
				case ROMC_0D:
					RegPC1 = (ushort)(RegPC0 + 1);
					break;

				// The device whose address space includes the contents of PC0 must place the contents of the word addressed by PC0 onto the data bus.
				// The value on the data bus is then moved to the low order byte of DC0 by all devices
				// CYCLE LENGTH: L
				case ROMC_0E:
					databus = ReadMemory(RegPC0);
					Regs[DC0l] = databus;
					break;

				// The interrupting device with the highest priority must place the low order byte of the interrupt vector on the data bus.
				// All devices must copy the contents of PC0 into PC1. All devices must move the contents of the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_0F:
					throw new NotImplementedException("ROMC 0x0F not implemented");
					break;

				// Inhibit any modification to the interrupt priority logic
				// CYCLE LENGTH: L
				case ROMC_10:
					throw new NotImplementedException("ROMC 0x10 not implemented");
					break;

				// The device whose memory space includes the contents of PC0 must place the contents of the addressed memory word on the data bus.
				// All devices must then move the contents of the data bus to the upper byte of DC0
				// CYCLE LENGTH: L
				case ROMC_11:
					databus = ReadMemory(RegPC0);
					Regs[DC0h] = databus;
					break;

				// All devices copy the contents of PC0 into PC1. All devices then move the contents of the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_12:
					RegPC1 = RegPC0;
					Regs[PC0l] = databus;
					break;

				// The interrupting device with the highest priority must move the high order half of the interrupt vector onto the data bus.
				// All devices must move the conetnts of the data bus into the high order byte of of PC0. The interrupting device resets its
				// interrupt circuitry (so that it is no longer requesting CPU servicing and can respond to another interrupt)
				// CYCLE LENGTH: L
				case ROMC_13:
					throw new NotImplementedException("ROMC 0x13 not implemented");
					break;

				// All devices move the contents of the data bus into the high order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_14:
					Regs[PC0h] = databus;
					break;

				// All devices move the contents of the data bus into the high order byte of PC1
				// CYCLE LENGTH: L
				case ROMC_15:
					Regs[PC1h] = databus;
					break;

				// All devices move the contents of the data bus into the high order byte of DC0
				// CYCLE LENGTH: L
				case ROMC_16:
					Regs[DC0h] = databus;
					break;

				// All devices move the contents of the data bus into the low order byte of PC0
				// CYCLE LENGTH: L
				case ROMC_17:
					Regs[PC0l] = databus;
					break;

				// All devices move the contents of the data bus into the low order byte of PC1
				// CYCLE LENGTH: L
				case ROMC_18:
					Regs[PC1l] = databus;
					break;

				// All devices move the contents of the data bus into the low order byte of DC0
				// CYCLE LENGTH: L
				case ROMC_19:
					Regs[DC0l] = databus;
					break;

				// During the prior cycle, an I/O port timer or interrupt control register was addressed; the device containing the addressed
				// port must move the current contents of the data bus into the addressed port
				// CYCLE LENGTH: L
				case ROMC_1A:
					WriteHardware(iobus, databus);
					break;

				// During the prior cycle, the data bus specified the address of an I/O port. The device containing the addressed I/O port
				// must place the contents of the I/O port on the data bus. (Note that the contents of the timer and interrupt control
				// registers cannot be read back onto the data bus)
				// CYCLE LENGTH: L
				case ROMC_1B:
					databus = ReadHardware(iobus);
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
					databus = (byte)Regs[PC0l];
					break;

				// The device whose address space includes the contents of PC0 must place the high order byte of PC0 onto the data bus
				// CYCLE LENGTH: L
				case ROMC_1F:
					databus = (byte)Regs[PC0h];
					break;
			}

			TotalExecutedCycles++;
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

			ser.EndSection();
		}
	}
}
