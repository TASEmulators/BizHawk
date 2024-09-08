using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.M6502
{
	/// <remarks>
	/// this type parameter might look useless—and it is—but after monomorphisation,
	/// this way happens to perform better than the alternative
	/// </remarks>
	/// <seealso cref="IMOS6502XLink"/>
	public sealed partial class MOS6502X<TLink> where TLink : IMOS6502XLink
	{
		private readonly TLink _link;

		public MOS6502X(TLink link)
		{
			_link = link;
			InitOpcodeHandlers();
			Reset();
		}

		public bool BCD_Enabled = true;
		public bool debug = false;

		public void Reset()
		{
			A = 0;
			X = 0;
			Y = 0;
			P = 0x20; // 5th bit always set
			S = 0;
			PC = 0;
			TotalExecutedCycles = 0;
			mi = 0;
			opcode = VOP_RESET;
			iflag_pending = true;
			RDY = true;
		}

		public void NESSoftReset()
		{
			opcode = VOP_RESET;
			mi = 0;
			iflag_pending = true;
			FlagI = true;
		}

		public string TraceHeader => "6502: PC, machine code, mnemonic, operands, registers (A, X, Y, P, SP), flags (NVTBDIZCR)";

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, RegisterValue>
			{
				["A"] = A,
				["X"] = X,
				["Y"] = Y,
				["S"] = S,
				["PC"] = PC,
				["Flag C"] = FlagC,
				["Flag Z"] = FlagZ,
				["Flag I"] = FlagI,
				["Flag D"] = FlagD,
				["Flag B"] = FlagB,
				["Flag V"] = FlagV,
				["Flag N"] = FlagN,
				["Flag T"] = FlagT
			};
		}

		public void SetCpuRegister(string register, int value)
		{
			switch (register)
			{
				default:
					throw new InvalidOperationException();
				case "A":
					A = (byte)value;
					break;
				case "X":
					X = (byte)value;
					break;
				case "Y":
					Y = (byte)value;
					break;
				case "S":
					S = (byte)value;
					break;
				case "PC":
					PC = (ushort)value;
					break;
				case "Flag I":
					FlagI = value > 0;
					break;
			}
		}

		public TraceInfo State(bool disassemble = true)
		{
			if (!disassemble) return new(disassembly: string.Empty, registerInfo: string.Empty);

			string rawbytes = "";
			string disasm = Disassemble(PC, out var length);

			for (int i = 0; i < length; i++)
			{
				rawbytes += $" {_link.PeekMemory((ushort)(PC + i)):X2}";
			}

			return new(
				disassembly: $"{PC:X4}: {rawbytes,-9}  {disasm} ".PadRight(32),
				registerInfo: string.Join("  ",
					$"A:{A:X2}",
					$"X:{X:X2}",
					$"Y:{Y:X2}",
					$"SP:{S:X2}",
					$"P:{P:X2}",
					string.Concat(
						FlagN ? "N" : "n",
						FlagV ? "V" : "v",
						FlagT ? "T" : "t",
						FlagB ? "B" : "b",
						FlagD ? "D" : "d",
						FlagI ? "I" : "i",
						FlagZ ? "Z" : "z",
						FlagC ? "C" : "c"
//						!RDY ? "R" : "r"
						),
					$"Cy:{TotalExecutedCycles}",
					$"PPU-Cy:{ext_ppu_cycle}"));
		}

		public bool AtStart => opcode == VOP_Fetch1 || Microcode[opcode][mi] >= Uop.End;

		public TraceInfo TraceState()
		{
			// only disassemble when we're at the beginning of an opcode
			return State(AtStart);
		}

		public const ushort NMIVector = 0xFFFA;
		public const ushort ResetVector = 0xFFFC;
		public const ushort BRKVector = 0xFFFE;
		public const ushort IRQVector = 0xFFFE;

		// ==== CPU State ====

		public byte A;
		public byte X;
		public byte Y;
		public byte P;
		public ushort PC;
		public byte S;

		public bool IRQ;
		public bool NMI;
		public bool RDY;

		// ppu cycle (used with SubNESHawk)
		public int ext_ppu_cycle = 0;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(MOS6502X));
			ser.Sync(nameof(A), ref A);
			ser.Sync(nameof(X), ref X);
			ser.Sync(nameof(Y), ref Y);
			ser.Sync(nameof(P), ref P);
			ser.Sync(nameof(PC), ref PC);
			ser.Sync(nameof(S), ref S);
			ser.Sync(nameof(NMI), ref NMI);
			ser.Sync(nameof(IRQ), ref IRQ);
			ser.Sync(nameof(RDY), ref RDY);
			ser.Sync(nameof(TotalExecutedCycles), ref TotalExecutedCycles);
			ser.Sync(nameof(opcode), ref opcode);
			ser.Sync(nameof(opcode2), ref opcode2);
			ser.Sync(nameof(opcode3), ref opcode3);
			ser.Sync(nameof(ea), ref ea);
			ser.Sync(nameof(alu_temp), ref alu_temp);
			ser.Sync(nameof(mi), ref mi);
			ser.Sync(nameof(iflag_pending), ref iflag_pending);
			ser.Sync(nameof(interrupt_pending), ref interrupt_pending);
			ser.Sync(nameof(branch_irq_hack), ref branch_irq_hack);
			ser.Sync(nameof(rdy_freeze), ref rdy_freeze);
			ser.Sync(nameof(ext_ppu_cycle), ref ext_ppu_cycle);
			ser.EndSection();
		}

		// ==== End State ====

		/// <summary>Carry Flag</summary>
		public bool FlagC
		{
			get => (P & 0x01) != 0;
			private set => P = (byte)((P & ~0x01) | (value ? 0x01 : 0x00));
		}

		/// <summary>Zero Flag</summary>
		public bool FlagZ
		{
			get => (P & 0x02) != 0;
			private set => P = (byte)((P & ~0x02) | (value ? 0x02 : 0x00));
		}

		/// <summary>Interrupt Disable Flag</summary>
		public bool FlagI
		{
			get => (P & 0x04) != 0;
			set => P = (byte)((P & ~0x04) | (value ? 0x04 : 0x00));
		}

		/// <summary>Decimal Mode Flag</summary>
		public bool FlagD
		{
			get => (P & 0x08) != 0;
			private set => P = (byte)((P & ~0x08) | (value ? 0x08 : 0x00));
		}

		/// <summary>Break Flag</summary>
		public bool FlagB
		{
			get => (P & 0x10) != 0;
			private set => P = (byte)((P & ~0x10) | (value ? 0x10 : 0x00));
		}

		/// <summary>T... Flag</summary>
		public bool FlagT
		{
			get => (P & 0x20) != 0;
			private set => P = (byte)((P & ~0x20) | (value ? 0x20 : 0x00));
		}

		/// <summary>Overflow Flag</summary>
		public bool FlagV
		{
			get => (P & 0x40) != 0;
			private set => P = (byte)((P & ~0x40) | (value ? 0x40 : 0x00));
		}

		/// <summary>Negative Flag</summary>
		public bool FlagN
		{
			get => (P & 0x80) != 0;
			private set => P = (byte)((P & ~0x80) | (value ? 0x80 : 0x00));
		}

		public long TotalExecutedCycles;

		// SO pin
		public void SetOverflow()
		{
			FlagV = true;
		}

		private static readonly byte[] TableNZ = 
		{ 
			0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80
		};
	}
}
