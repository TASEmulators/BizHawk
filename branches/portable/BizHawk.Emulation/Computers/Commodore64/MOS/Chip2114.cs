using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// used as Color RAM in C64

	public class Chip2114 : IStandardIO
	{
		private byte[] ram;

		public Chip2114()
		{
			HardReset();
		}

		public void HardReset()
		{
			ram = new byte[0x400];
		}

		public byte Peek(int addr)
		{
			return ram[addr & 0x3FF];
		}

		public byte Peek(int addr, byte bus)
		{
			return (byte)(ram[addr & 0x3FF] | (bus & 0xF0));
		}

		public void Poke(int addr, byte val)
		{
			ram[addr & 0x3FF] = (byte)(val & 0xF);
		}

		public byte Read(ushort addr)
		{
			return (byte)(ram[addr & 0x3FF]);
		}

		public byte Read(ushort addr, byte bus)
		{
			return (byte)(ram[addr & 0x3FF] | (bus & 0xF0));
		}

		public void Write(ushort addr, byte val)
		{
			ram[addr & 0x3FF] = (byte)(val & 0xF);
		}
	}
}
