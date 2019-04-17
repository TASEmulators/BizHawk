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
		/// Accumulator
		/// </summary>
		public ushort A = 0;
		/// <summary>
		/// Status Register
		/// </summary>
		public ushort W = 1;
		/// <summary>
		/// Indirect Scratchpad Address Register
		/// (6bit)
		/// </summary>
		public ushort ISAR = 2;
		/// <summary>
		/// Primary Program Counter (low byte)
		/// </summary>
		public ushort PC0l = 3;
		/// <summary>
		/// Primary Program Counter (high byte)
		/// </summary>
		public ushort PC0h = 4;
		/// <summary>
		/// Backup Program Counter (low byte)
		/// </summary>
		public ushort PC1l = 5;
		/// <summary>
		/// Backup Program Counter (high byte)
		/// </summary>
		public ushort PC1h = 6;
		/// <summary>
		/// Data Counter (low byte)
		/// </summary>
		public ushort DC0l = 7;
		/// <summary>
		/// Data counter (high byte)
		/// </summary>
		public ushort DC0h = 8;
		/// <summary>
		/// Temporary Arithmetic Storage
		/// </summary>
		public ushort ALU = 9;
		/// <summary>
		/// Data Bus
		/// </summary>
		public ushort DB = 10;
		/// <summary>
		/// IO Bus
		/// </summary>
		public ushort IO = 11;

		/// <summary>
		/// Registers (counters and scratchpad)
		/// </summary>
		public ushort[] Regs = new ushort[100];

		// scratchpad registers
		public ushort SR0 = 20;
		public ushort SR1 = 21;
		public ushort SR2 = 22;
		public ushort SR3 = 23;
		public ushort SR4 = 24;
		public ushort SR5 = 25;
		public ushort SR6 = 26;
		public ushort SR7 = 27;
		public ushort SR8 = 28;
		public ushort SR9 = 29;
		public ushort SR10 = 30;
		public ushort SR11 = 31;
		public ushort SR12 = 32;
		public ushort SR13 = 33;
		public ushort SR14 = 34;
		public ushort SR15 = 35;
		public ushort SR16 = 36;
		public ushort SR17 = 37;
		public ushort SR18 = 38;
		public ushort SR19 = 39;
		public ushort SR20 = 40;
		public ushort SR21 = 41;
		public ushort SR22 = 42;
		public ushort SR23 = 43;
		public ushort SR24 = 44;
		public ushort SR25 = 45;
		public ushort SR26 = 46;
		public ushort SR27 = 47;
		public ushort SR28 = 48;
		public ushort SR29 = 49;
		public ushort SR30 = 50;
		public ushort SR31 = 51;
		public ushort SR32 = 52;
		public ushort SR33 = 53;
		public ushort SR34 = 54;
		public ushort SR35 = 55;
		public ushort SR36 = 56;
		public ushort SR37 = 57;
		public ushort SR38 = 58;
		public ushort SR39 = 59;
		public ushort SR40 = 60;
		public ushort SR41 = 61;
		public ushort SR42 = 62;
		public ushort SR43 = 63;
		public ushort SR44 = 64;
		public ushort SR45 = 65;
		public ushort SR46 = 66;
		public ushort SR47 = 67;
		public ushort SR48 = 68;
		public ushort SR49 = 69;
		public ushort SR50 = 70;
		public ushort SR51 = 71;
		public ushort SR52 = 72;
		public ushort SR53 = 73;
		public ushort SR54 = 74;
		public ushort SR55 = 75;
		public ushort SR56 = 76;
		public ushort SR57 = 77;
		public ushort SR58 = 78;
		public ushort SR59 = 79;
		public ushort SR60 = 80;
		public ushort SR61 = 81;
		public ushort SR62 = 82;
		public ushort SR63 = 83;
		public ushort SR64 = 84;

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
		}
	}
}
