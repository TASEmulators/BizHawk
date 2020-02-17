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
		public byte[] Regs = new byte[100];

		// scratchpad registers live in Regs 0-64
		public byte J = 9;
		public byte Hh = 10;
		public byte Hl = 11;
		public byte Kh = 12;
		public byte Kl = 13;
		public byte Qh = 14;
		public byte Ql = 15;

		// Internal CPU counters kept after the scratchpad for ease of implementation
		/// <summary>
		/// Accumulator
		/// </summary>
		public byte A = 65;
		/// <summary>
		/// Status Register
		/// </summary>
		public byte W = 66;
		/// <summary>
		/// Indirect Scratchpad Address Register
		/// (6bit)
		/// </summary>
		public byte ISAR = 67;
		/// <summary>
		/// Primary Program Counter (high byte)
		/// </summary>
		public  byte PC0h = 68;
		/// <summary>
		/// Primary Program Counter (low byte)
		/// </summary>
		public byte PC0l = 69;
		/// <summary>
		/// Backup Program Counter (high byte)
		/// </summary>
		public byte PC1h = 70;
		/// <summary>
		/// Backup Program Counter (low byte)
		/// </summary>
		public byte PC1l = 71;
		/// <summary>
		/// Data counter (high byte)
		/// </summary>
		public byte DC0h = 72;
		/// <summary>
		/// Data Counter (low byte)
		/// </summary>
		public byte DC0l = 73;
		/// <summary>
		/// Temporary Arithmetic Storage
		/// </summary>
		public byte ALU0 = 74;
		/// <summary>
		/// Temporary Arithmetic Storage
		/// </summary>
		public byte ALU1 = 75;
		/// <summary>
		/// Data Bus
		/// </summary>
		public byte DB = 76;
		/// <summary>
		/// IO Bus/Latch
		/// </summary>
		public byte IO = 77;
		/// <summary>
		/// 0x00 value for arithmetic ops
		/// </summary>
		public byte ZERO = 78;
		/// <summary>
		/// 0x01 value for arithmetic ops
		/// </summary>
		public byte ONE = 79;
		/// <summary>
		/// 0xFF value for arithmetic ops
		/// </summary>
		public byte BYTE = 80;


		/// <summary>
		/// Status Register - Sign Flag
		/// </summary>
		public bool FlagS
		{
			get => (Regs[W] & 0x01) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x01) | (value ? 0x01 : 0x00));
		}

		/// <summary>
		/// Status Register - Carry Flag
		/// </summary>
		public bool FlagC
		{
			get => (Regs[W] & 0x02) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x02) | (value ? 0x02 : 0x00));
		}

		/// <summary>
		/// Status Register - Zero Flag
		/// </summary>
		public bool FlagZ
		{
			get => (Regs[W] & 0x04) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x04) | (value ? 0x04 : 0x00));
		}

		/// <summary>
		/// Status Register - Overflow Flag
		/// </summary>
		public bool FlagO
		{
			get => (Regs[W] & 0x08) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x08) | (value ? 0x08 : 0x00));
		}

		/// <summary>
		/// Status Register - Interrupt Master Enable Flag
		/// </summary>
		public bool FlagICB
		{
			get => (Regs[W] & 0x10) != 0;
			set => Regs[W] = (byte)((Regs[W] & ~0x10) | (value ? 0x10 : 0x00));
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

		private void ResetRegisters()
		{
			for (var i = 0; i < Regs.Length; i++)
			{
				Regs[i] = 0;
			}

			Regs[ONE] = 1;
			Regs[BYTE] = 0xFF;

			// testing only - fill scratchpad with 0xff
			for (int i = 0; i < 64; i++)
				Regs[i] = 0xff;
		}
	}
}
