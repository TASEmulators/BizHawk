using System;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.M6502;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the M6532 RIOT Chip
	public partial class TIA
	{
		MOS6502 Cpu;

		public TIA(MOS6502 cpu)
		{
			Cpu = cpu;
		}

		public byte ReadMemory(ushort addr)
		{
			ushort maskedAddr = (ushort)(addr & 0x3f);
			Console.WriteLine("TIA read:  " + maskedAddr.ToString("x"));
			
			return 0x3A;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			ushort maskedAddr = (ushort)(addr & 0x3f);
			Console.WriteLine("TIA write:  " + maskedAddr.ToString("x"));
		}
	}
}