using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class C64 : IEmulator
	{
		public byte PeekCia0(int addr)
		{
			return cia0.Peek(addr);
		}

		public byte PeekCia1(int addr)
		{
			return cia1.Peek(addr);
		}

		public byte PeekColorRAM(int addr)
		{
			return (byte)((mem.colorRam[addr & 0x3FF] & 0xF) | mem.busData);
		}

		public byte PeekMemory(ushort addr)
		{
			return mem.Peek(addr);
		}

		public byte PeekMemoryInt(int addr)
		{
			return mem.Peek((ushort)(addr & 0xFFFF));
		}

		public byte PeekRAM(int addr)
		{
			return mem.PeekRam(addr);
		}

		public byte PeekSid(int addr)
		{
			return sid.Peek(addr);
		}

		public byte PeekVic(int addr)
		{
			return vic.Peek(addr);
		}

		public void PokeCia0(int addr, byte val)
		{
			cia0.Poke(addr, val);
		}

		public void PokeCia1(int addr, byte val)
		{
			cia1.Poke(addr, val);
		}

		public void PokeColorRAM(int addr, byte val)
		{
			mem.colorRam[addr & 0x3FF] = (byte)(val & 0xF);
		}

		public void PokeMemoryInt(int addr, byte val)
		{
			mem.Poke((ushort)(addr & 0xFFFF), val);
		}

		public void PokeRAM(int addr, byte val)
		{
			mem.PokeRam(addr, val);
		}

		public void PokeSid(int addr, byte val)
		{
			sid.Poke(addr, val);
		}

		public void PokeVic(int addr, byte val)
		{
			vic.Poke(addr, val);
		}
	}
}
