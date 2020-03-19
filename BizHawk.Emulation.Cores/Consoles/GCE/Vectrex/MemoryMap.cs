using BizHawk.Emulation.Common;

/*
	0x0000 - 0x7FFF		ROM
	0x8000 - 0xC7FF		Unmapped
	0xC800 - 0xCFFF		RAM (and shadows)
	0xD000 - 0XD7FF		6522 (and shadows)
	0xD800 - 0xDFFF		6522 + RAM
	0xE000 - 0xEFFF		Minestorm
	0xF000 - 0xFFFF		BIOS
*/

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk
	{
		public byte ReadMemory(ushort addr)
		{
			uint flags = (uint)MemoryCallbackFlags.AccessRead;
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");

			if (addr < 0x8000)
			{
				return mapper.ReadMemory(addr);
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
				return minestorm[addr-0xE000];
			}
			else
			{
				return _bios[addr - 0xF000];
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			uint flags = (uint)MemoryCallbackFlags.AccessWrite;
			MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");

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
				return mapper.ReadMemory(addr);
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
				return minestorm[addr - 0xE000];
			}
			else
			{
				return _bios[addr - 0xF000];
			}
		}
	}
}
