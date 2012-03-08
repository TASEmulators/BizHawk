using System;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the M6532 RIOT Chip
	public partial class M6532
	{
		MOS6502 Cpu;
		public byte[] ram;

		public M6532(MOS6502 cpu, byte[] ram)
		{
			Cpu = cpu;
			this.ram = ram;
		}

		public byte ReadMemory(ushort addr)
		{
			ushort maskedAddr;

			if ((addr & 0x1080) == 0x0080 && (addr & 0x0200) == 0x0000)
			{
				maskedAddr = (ushort)(addr & 0x007f);
				Console.WriteLine("6532 ram read: " + maskedAddr.ToString("x"));
				return ram[maskedAddr];
			}
			else
			{
				maskedAddr = (ushort)(addr & 0x0007);
				Console.WriteLine("6532 register read: " + maskedAddr.ToString("x"));
			}

			return 0x3A;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			ushort maskedAddr;

			if ((addr & 0x1080) == 0x0080 && (addr & 0x0200) == 0x0000)
			{
				maskedAddr = (ushort)(addr & 0x007f);
				Console.WriteLine("6532 ram write: " + maskedAddr.ToString("x"));
				ram[maskedAddr] = value;
			}
			else
			{
				maskedAddr = (ushort)(addr & 0x0007);
				Console.WriteLine("6532 register write: " + maskedAddr.ToString("x"));
			}
		}
	}
}