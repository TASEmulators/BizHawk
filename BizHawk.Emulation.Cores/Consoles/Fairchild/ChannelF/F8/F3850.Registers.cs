using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	/// <summary>
	/// Internal Registers
	/// </summary>
	public sealed partial class F3850
	{
		/// <summary>
		/// Registers (counters and scratchpad)
		/// </summary>
		public ushort[] Regs = new ushort[100];

		// scratchpad registers live in Regs 0-64
		public ushort J = 9;
		public ushort Hh = 10;
		public ushort Hl = 11;
		public ushort Kh = 12;
		public ushort Kl = 13;
		public ushort Qh = 14;
		public ushort Ql = 15;

		// Internal CPU counters kept after the scratchpad for ease of implementation
		/// <summary>
		/// Accumulator
		/// </summary>
		public ushort A = 65;
		/// <summary>
		/// Status Register
		/// </summary>
		public ushort W = 66;
		/// <summary>
		/// Indirect Scratchpad Address Register
		/// (6bit)
		/// </summary>
		public ushort ISAR = 67;
		/// <summary>
		/// Primary Program Counter (high byte)
		/// </summary>
		public  ushort PC0h = 68;
		/// <summary>
		/// Primary Program Counter (low byte)
		/// </summary>
		public ushort PC0l = 69;
		/// <summary>
		/// Backup Program Counter (high byte)
		/// </summary>
		public ushort PC1h = 70;
		/// <summary>
		/// Backup Program Counter (low byte)
		/// </summary>
		public ushort PC1l = 71;
		/// <summary>
		/// Data counter (high byte)
		/// </summary>
		public ushort DC0h = 72;
		/// <summary>
		/// Data Counter (low byte)
		/// </summary>
		public ushort DC0l = 73;
		/// <summary>
		/// Temporary Arithmetic Storage
		/// </summary>
		public ushort ALU = 74;
		/// <summary>
		/// Data Bus
		/// </summary>
		public ushort DB = 75;
		/// <summary>
		/// IO Bus/Latch
		/// </summary>
		public ushort IO = 76;
		/// <summary>
		/// 0x00 value for arithmetic ops
		/// </summary>
		public ushort ZERO = 77;
		/// <summary>
		/// 0xff value for arithmetic ops
		/// </summary>
		public ushort BYTE = 78;


		/// <summary>
		/// Status Register - Sign Flag
		/// </summary>
		public bool FlagS
		{
			get { return (Regs[W] & 0x01) != 0; }
			set { Regs[W] = (ushort)((Regs[W] & ~0x01) | (value ? 0x01 : 0x00)); }
		}

		/// <summary>
		/// Status Register - Carry Flag
		/// </summary>
		public bool FlagC
		{
			get { return (Regs[W] & 0x02) != 0; }
			set { Regs[W] = (ushort)((Regs[W] & ~0x02) | (value ? 0x02 : 0x00)); }
		}

		/// <summary>
		/// Status Register - Zero Flag
		/// </summary>
		public bool FlagZ
		{
			get { return (Regs[W] & 0x04) != 0; }
			set { Regs[W] = (ushort)((Regs[W] & ~0x04) | (value ? 0x04 : 0x00)); }
		}

		/// <summary>
		/// Status Register - Overflow Flag
		/// </summary>
		public bool FlagO
		{
			get { return (Regs[W] & 0x08) != 0; }
			set { Regs[W] = (ushort)((Regs[W] & ~0x08) | (value ? 0x08 : 0x00)); }
		}

		/// <summary>
		/// Status Register - Interrupt Master Enable Flag
		/// </summary>
		public bool FlagICB
		{
			get { return (Regs[W] & 0x10) != 0; }
			set { Regs[W] = (ushort)((Regs[W] & ~0x10) | (value ? 0x10 : 0x00)); }
		}

		/// <summary>
		/// Access to the full 16-bit Primary Program Counter
		/// </summary>
		public ushort RegPC0
		{
			get { return (ushort)(Regs[PC0l] | (Regs[PC0h] << 8)); }
			set
			{
				Regs[PC0l] = (ushort)(value & 0xFF);
				Regs[PC0h] = (ushort)((value >> 8) & 0xFF);
			}
		}

		/// <summary>
		/// Access to the full 16-bit Backup Program Counter
		/// </summary>
		public ushort RegPC1
		{
			get { return (ushort)(Regs[PC1l] | (Regs[PC1h] << 8)); }
			set
			{
				Regs[PC1l] = (ushort)(value & 0xFF);
				Regs[PC1h] = (ushort)((value >> 8) & 0xFF);
			}
		}

		/// <summary>
		/// Access to the full 16-bit Data Counter
		/// </summary>
		public ushort RegDC0
		{
			get { return (ushort)(Regs[DC0l] | (Regs[DC0h] << 8)); }
			set
			{
				Regs[DC0l] = (ushort)(value & 0xFF);
				Regs[DC0h] = (ushort)((value >> 8) & 0xFF);
			}
		}

		private void ResetRegisters()
		{
			for (var i = 0; i < Regs.Length; i++)
			{
				Regs[i] = 0;
			}

			Regs[BYTE] = 0xff;
		}
	}
}
