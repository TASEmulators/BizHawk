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
		// typically here you have a big if / else if block to decide what to do with memory reads and writes
		// send hardware register accesses to the Read_register / Write_register methods
		// make sure you are returning the correct value (typically 0 or 0xFF) for unmapped memory

		// PeekMemory is called by the hex eidtor and other tools to read what's on the bus
		// make sure it doesn't modify anything in the core or you will be in debugging hell.

		public byte ReadMemory(ushort addr)
		{
			// memory callbacks are used for LUA and such
			MemoryCallbacks.CallReads(addr, "System Bus");

			return 0;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			MemoryCallbacks.CallWrites(addr, "System Bus");
			
		}

		public byte PeekMemory(ushort addr)
		{
			return 0;
		}
	}
}
