using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Disk
{
	public class VIC1541PLA
	{
		public Func<int, byte> PeekRam;
		public Func<int, byte> PeekRom;
		public Func<int, byte> PeekVia0;
		public Func<int, byte> PeekVia1;
		public Action<int, byte> PokeRam;
		public Action<int, byte> PokeRom;
		public Action<int, byte> PokeVia0;
		public Action<int, byte> PokeVia1;
		public Func<ushort, byte> ReadRam;
		public Func<ushort, byte> ReadRom;
		public Func<ushort, byte> ReadVia0;
		public Func<ushort, byte> ReadVia1;
		public Action<ushort, byte> WriteRam;
		public Action<ushort, byte> WriteRom;
		public Action<ushort, byte> WriteVia0;
		public Action<ushort, byte> WriteVia1;

		public byte Peek(int addr)
		{
			addr &= 0xFFFF;
			if (addr >= 0x1800 && addr < 0x1C00)
				return PeekVia0(addr);
			else if (addr >= 0x1C00 && addr < 0x2000)
				return PeekVia1(addr);
			else if (addr >= 0xC000)
				return PeekRom(addr);
			else
				return PeekRam(addr);
		}

		public void Poke(int addr, byte val)
		{
			addr &= 0xFFFF;
			if (addr >= 0x1800 && addr < 0x1C00)
				PokeVia0(addr, val);
			else if (addr >= 0x1C00 && addr < 0x2000)
				PokeVia1(addr, val);
			else if (addr >= 0xC000)
				PokeRom(addr, val);
			else
				PokeRam(addr, val);
		}

		public byte Read(ushort addr)
		{
			addr &= 0xFFFF;
			if (addr >= 0x1800 && addr < 0x1C00)
				return ReadVia0(addr);
			else if (addr >= 0x1C00 && addr < 0x2000)
				return ReadVia1(addr);
			else if (addr >= 0xC000)
				return ReadRom(addr);
			else
				return ReadRam(addr);
		}

		public void Write(ushort addr, byte val)
		{
			addr &= 0xFFFF;
			if (addr >= 0x1800 && addr < 0x1C00)
				WriteVia0(addr, val);
			else if (addr >= 0x1C00 && addr < 0x2000)
				WriteVia1(addr, val);
			else if (addr >= 0xC000)
				WriteRom(addr, val);
			else
				WriteRam(addr, val);
		}
	}
}
