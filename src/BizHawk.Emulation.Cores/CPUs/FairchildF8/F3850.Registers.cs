using System.Collections.Generic;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.FairchildF8
{
	/// <summary>
	/// Internal Registers
	/// </summary>
	public sealed partial class F3850<TLink>
	{
		/// <summary>
		/// Registers (counters and scratchpad)
		/// </summary>
		public byte[] Regs = new byte[100];

		// scratchpad registers live in Regs 0-64
		public const byte J = 9;
		public const byte Hh = 10;
		public const byte Hl = 11;
		public const byte Kh = 12;
		public const byte Kl = 13;
		public const byte Qh = 14;
		public const byte Ql = 15;

		// Internal CPU counters kept after the scratchpad for ease of implementation
		/// <summary>
		/// Accumulator
		/// </summary>
		public const byte A = 65;
		/// <summary>
		/// Status Register
		/// </summary>
		public const byte W = 66;
		/// <summary>
		/// Indirect Scratchpad Address Register
		/// (6bit)
		/// </summary>
		public const byte ISAR = 67;
		/// <summary>
		/// Primary Program Counter (high byte)
		/// </summary>
		public const byte PC0h = 68;
		/// <summary>
		/// Primary Program Counter (low byte)
		/// </summary>
		public const byte PC0l = 69;
		/// <summary>
		/// Backup Program Counter (high byte)
		/// </summary>
		public const byte PC1h = 70;
		/// <summary>
		/// Backup Program Counter (low byte)
		/// </summary>
		public const byte PC1l = 71;
		/// <summary>
		/// Data counter (high byte)
		/// </summary>
		public const byte DC0h = 72;
		/// <summary>
		/// Data Counter (low byte)
		/// </summary>
		public const byte DC0l = 73;
		/// <summary>
		/// Temporary Arithmetic Storage
		/// </summary>
		public const byte ALU0 = 74;
		/// <summary>
		/// Temporary Arithmetic Storage
		/// </summary>
		public const byte ALU1 = 75;
		/// <summary>
		/// Data Bus
		/// </summary>
		public const byte DB = 76;
		/// <summary>
		/// IO Bus/Latch
		/// </summary>
		public const byte IO = 77;
		/// <summary>
		/// 0x00 value for arithmetic ops
		/// </summary>
		public const byte ZERO = 78;
		/// <summary>
		/// 0x01 value for arithmetic ops
		/// </summary>
		public const byte ONE = 79;
		/// <summary>
		/// 0xFF value for arithmetic ops
		/// </summary>
		public const byte BYTE = 80;
		/// <summary>
		/// Backup Data counter (high byte)
		/// </summary>
		public const byte DC1h = 81;
		/// <summary>
		/// Backup Data Counter (low byte)
		/// </summary>
		public const byte DC1l = 82;
		/// <summary>
		/// IRQ Vector (high byte)
		/// </summary>
		public const byte IRQVh = 83;
		/// <summary>
		/// IRQ Vector (low byte)
		/// </summary>
		public const byte IRQVl = 84;
		/// <summary>
		/// IRQ Request Pending
		/// </summary>
		public const byte IRQR = 85;


		/// <summary>
		/// Status Register - Sign Flag
		/// When the results of an ALU operation are being interpreted as a signed binary number, the high oidei bit (bit 7) represents the sign of the number
		/// At the conclusion of instructions that may modify the Accumulator bit 7, the S bit (W register bit 0) is set to the complement of the Accumulator bit 7.
		/// </summary>
		public bool FlagS
		{
			get => (Regs[W] & 0x01) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x01) | (value ? 0x01 : 0x00));
		}

		/// <summary>
		/// Status Register - Carry Flag
		/// The C bit (W register bit 1) may be visualized as an extension of an 8-bit data unit; i.e., bit 8 of a 9-bit data unit. 
		/// When two bytes are added and the sum is greater than 255, the carry out of bit 7 appears in the C bit.
		/// </summary>
		public bool FlagC
		{
			get => (Regs[W] & 0x02) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x02) | (value ? 0x02 : 0x00));
		}

		/// <summary>
		/// Status Register - Zero Flag
		/// The Z bit (W Register bit 2) is set whenever an arithmetic or logical operation generates a zero result. 
		/// The Z bit is reset to 0 when an arithmetic or logical operation could have generated a zero result, but did not.
		/// </summary>
		public bool FlagZ
		{
			get => (Regs[W] & 0x04) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x04) | (value ? 0x04 : 0x00));
		}

		/// <summary>
		/// Status Register - Overflow Flag
		/// The high order Accumulator bit (bit 7) represents the sign of the number. 
		/// When the Accumulator contents are being interpreted as a signed binary number, some method must be provided for indicating carries out of the highest numeric bit (bit 6 of the Accumulator). 
		/// This is done using the 0 bit (W register bit 3). After arithmetic operations, the 0 bit is set to the EXCLUSIVE-OR of Carry Out of bits 6 and bits 7. This simplifies signed binary arithmetic. 
		/// </summary>
		public bool FlagO
		{
			get => (Regs[W] & 0x08) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x08) | (value ? 0x08 : 0x00));
		}

		/// <summary>
		/// Status Register - Interrupt Master Enable Flag
		/// External logic can alter the operations sequence within the CPU by interrupting ongoing operations. 
		/// However, interrupts are allowed only when t (W register bit 4) is set to 1; interrupts are disallowed when the ICB bit is reset to O.
		/// </summary>
		public bool FlagICB
		{
			get => (Regs[W] & 0x10) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x10) | (value ? 0x10 : 0x00));
		}

		/// <summary>
		/// Signals that IRQ Request is pending
		/// </summary>
		public bool IRQRequest
		{
			get => Regs[IRQR] > 0;
			set => Regs[IRQR] = value ? (byte)1 : (byte)0;
		}

		/// <summary>
		/// Access to the full 16-bit Primary Program Counter
		/// </summary>
		public ushort RegPC0
		{
			get => (ushort)(Regs[PC0l] | (Regs[PC0h] << 8));
			set
			{
				Regs[PC0l] = (byte)(value & 0xFF);
				Regs[PC0h] = (byte)((value >> 8) & 0xFF);
			}
		}

		/// <summary>
		/// Access to the full 16-bit Backup Program Counter
		/// </summary>
		public ushort RegPC1
		{
			get => (ushort)(Regs[PC1l] | (Regs[PC1h] << 8));
			set
			{
				Regs[PC1l] = (byte)(value & 0xFF);
				Regs[PC1h] = (byte)((value >> 8) & 0xFF);
			}
		}

		/// <summary>
		/// Access to the full 16-bit Data Counter
		/// </summary>
		public ushort RegDC0
		{
			get => (ushort)(Regs[DC0l] | (Regs[DC0h] << 8));
			set
			{
				Regs[DC0l] = (byte)(value & 0xFF);
				Regs[DC0h] = (byte)((value >> 8) & 0xFF);
			}
		}

		/// <summary>
		/// Access to the full 16-bit Backup Data Counter
		/// </summary>
		public ushort RegDC1
		{
			get => (ushort)(Regs[DC1l] | (Regs[DC1h] << 8));
			set
			{
				Regs[DC1l] = (byte)(value & 0xFF);
				Regs[DC1h] = (byte)((value >> 8) & 0xFF);
			}
		}

		/// <summary>
		/// Access to the full 16-bit IRQ Vector
		/// </summary>
		public ushort RegIRQV
		{
			get => (ushort)(Regs[IRQVl] | (Regs[IRQVh] << 8));
			set
			{
				Regs[IRQVl] = (byte)(value & 0xFF);
				Regs[IRQVh] = (byte)((value >> 8) & 0xFF);
			}
		}

		private const string PFX_SCRATCHPAD_REG = "SPR";

		public IDictionary<string, RegisterValue> GetCpuFlagsAndRegisters()
		{
			var res = new Dictionary<string, RegisterValue>
			{
				["A"] = Regs[A],
				["W"] = Regs[W],
				["ISAR"] = Regs[ISAR],
				["PC0"] = RegPC0,
				["PC1"] = RegPC1,
				["DC0"] = RegDC0,
				["DB"] = Regs[DB],
				["IO"] = Regs[IO],
				["J"] = Regs[J],
				["H"] = Regs[Hl] + (Regs[Hh] << 8),
				["K"] = Regs[Kl] + (Regs[Kh] << 8),
				["Q"] = Regs[Ql] + (Regs[Qh] << 8),
				["Flag C"] = FlagC,
				["Flag O"] = FlagO,
				["Flag Z"] = FlagZ,
				["Flag S"] = FlagS,
				["Flag I"] = FlagICB
			};

			for (int i = 0; i < 64; i++)
			{
				res.Add(PFX_SCRATCHPAD_REG + i, Regs[i]);
			}

			return res;
		}

		public void SetCpuRegister(string register, int value)
		{
			if (register.StartsWithOrdinal(PFX_SCRATCHPAD_REG))
			{
				var reg = int.Parse(register.Substring(startIndex: PFX_SCRATCHPAD_REG.Length));

				if (reg > 63)
				{
					throw new InvalidOperationException();
				}

				Regs[reg] = (byte) value;
			}
			else
			{
				switch (register)
				{
					default:
						throw new InvalidOperationException();
					case "A":
						Regs[A] = (byte)value;
						break;
					case "W":
						Regs[W] = (byte)value;
						break;
					case "ISAR":
						Regs[ISAR] = (byte)(value & 0x3F);
						break;
					case "PC0":
						RegPC0 = (ushort)value;
						break;
					case "PC1":
						RegPC1 = (ushort)value;
						break;
					case "DC0":
						RegDC0 = (ushort)value;
						break;
					case "DC1":
						RegDC1 = (ushort)value;
						break;
					case "DB":
						Regs[DB] = (byte)value;
						break;
					case "IO":
						Regs[IO] = (byte)value;
						break;
					case "J":
						Regs[J] = (byte)value;
						break;
					case "H":
						Regs[Hl] = (byte)(value & 0xFF);
						Regs[Hh] = (byte)(value & 0xFF00);
						break;
					case "K":
						Regs[Kl] = (byte)(value & 0xFF);
						Regs[Kh] = (byte)(value & 0xFF00);
						break;
					case "Q":
						Regs[Ql] = (byte)(value & 0xFF);
						Regs[Qh] = (byte)(value & 0xFF00);
						break;
				}
			}
		}

		private void ResetRegisters()
		{
			for (var i = 0; i < Regs.Length; i++)
			{
				Regs[i] = 0;
			}

			Regs[ONE] = 1;
			Regs[ZERO] = 0;
			Regs[BYTE] = 0xFF;
		}
	}
}
