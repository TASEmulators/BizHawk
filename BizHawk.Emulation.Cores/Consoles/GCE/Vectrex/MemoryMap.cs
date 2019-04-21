using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;


/*
	Fill in the memory map in this space for easy reference
	ex:
	0x0000 - 0x0FFF		RAM
	etc
*/

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk
	{
		public byte ReadMemory(ushort addr)
		{
			MemoryCallbacks.CallReads(addr, "System Bus");

			if (addr < 0x8000)
			{
				return 0xFF;
			}
			else if (addr < 0xC800)
			{
				return 0xFF;
			}
			else if (addr < 0xD000)
			{
				return RAM[(addr-0xC800) & 0x3FF];
			}
			else if (addr < 0xD800)
			{
				return Read_Registers(addr & 0xF);
			}
			else if (addr < 0xE000)
			{
				return 0xFF;
			}
			else if (addr < 0xF000)
			{
				return 0xFF;
			}
			else
			{
				return _bios[addr - 0xF000];
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			MemoryCallbacks.CallWrites(addr, "System Bus");

			if (addr < 0x8000)
			{

			}
			else if (addr < 0xC800)
			{

			}
			else if (addr < 0xD000)
			{
				RAM[(addr - 0xC800) & 0x3FF] = value;
			}
			else if (addr < 0xD800)
			{
				Write_Registers(addr & 0xF, value);
			}
			else if (addr < 0xE000)
			{

			}
			else if (addr < 0xF000)
			{

			}
			else
			{

			}
		}

		public byte PeekMemory(ushort addr)
		{
			if (addr < 0x8000)
			{
				return 0xFF;
			}
			else if (addr < 0xC800)
			{
				return 0xFF;
			}
			else if (addr < 0xD000)
			{
				return RAM[(addr - 0xC800) & 0x3FF];
			}
			else if (addr < 0xD800)
			{
				return Read_Registers(addr & 0xF);
			}
			else if (addr < 0xE000)
			{
				return 0xFF;
			}
			else if (addr < 0xF000)
			{
				return 0xFF;
			}
			else
			{
				return _bios[addr - 0xF000];
			}
		}
	}
}
