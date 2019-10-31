using System;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

/*
	$0400-$0FFF    Cartridge (Only 2K accessible, bit 10 not mapped to cart)
	$0000-$03FF    BIOS
*/

namespace BizHawk.Emulation.Cores.Nintendo.O2Hawk
{
	public partial class O2Hawk
	{
		public byte ReadMemory(ushort addr)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
			addr_access = addr;
			
			if (addr < 0x400)
			{
				return _bios[addr];
			}
			else
			{
				return mapper.ReadMemory(addr);
			}
		}

		public void WriteMemory(ushort addr, byte value)
		{
			uint flags = (uint)(MemoryCallbackFlags.AccessWrite);
			MemoryCallbacks.CallMemoryCallbacks(addr, value, flags, "System Bus");
			addr_access = addr;

			if (addr < 0x400)
			{

			}
			else
			{
				mapper.WriteMemory(addr, value);
			}
		}

		public byte PeekMemory(ushort addr)
		{
			if (addr < 0x400)
			{
				return _bios[addr];
			}
			else
			{
				return mapper.PeekMemory(addr);
			}
		}
	}
}
