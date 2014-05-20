using System;
using System.Linq;

using LuaInterface;

namespace BizHawk.Client.Common
{
	public class MemoryLuaLibrary : LuaLibraryBase
	{
		private readonly Lua _lua;
		private int _currentMemoryDomain; // Main memory by default

		public MemoryLuaLibrary(Lua lua)
		{
			_lua = lua;
		}

		public override string Name { get { return "memory"; } }

		#region Memory Library Helpers

		private static int U2S(uint u, int size)
		{
			var s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		private int ReadSignedLittleCore(int addr, int size)
		{
			return U2S(ReadUnsignedLittle(addr, size), size);
		}

		private uint ReadUnsignedLittle(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * i);
			}

			return v;
		}

		private int ReadSignedBig(int addr, int size)
		{
			return U2S(ReadUnsignedBig(addr, size), size);
		}

		private uint ReadUnsignedBig(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * (size - 1 - i));
			}

			return v;
		}

		private void WriteSignedLittle(int addr, int v, int size)
		{
			WriteUnsignedLittle(addr, (uint)v, size);
		}

		private void WriteUnsignedLittle(int addr, uint v, int size)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * i)) & 0xFF);
			}
		}

		private void WriteSignedBig(int addr, int v, int size)
		{
			WriteUnsignedBig(addr, (uint)v, size);
		}

		private void WriteUnsignedBig(int addr, uint v, int size)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF);
			}
		}

		private uint ReadUnsignedByte(int addr)
		{
			return Global.Emulator.MemoryDomains[_currentMemoryDomain].PeekByte(addr);
		}

		private void WriteUnsignedByte(int addr, uint v)
		{
			Global.Emulator.MemoryDomains[_currentMemoryDomain].PokeByte(addr, (byte)v);
		}

		#endregion

		[LuaMethodAttributes(
			"getmemorydomainlist",
			"Returns a string of the memory domains for the loaded platform core. List will be a single string delimited by line feeds"
		)]
		public LuaTable GetMemoryDomainList()
		{
			var table = _lua.NewTable();
			for (int i = 0; i < Global.Emulator.MemoryDomains.Count; i++)
			{
				table[i] = Global.Emulator.MemoryDomains[i].Name;
			}

			return table;
		}

		[LuaMethodAttributes(
			"readbyterange",
			"Reads the address range that starts from address, and is length long. Returns the result into a table of key value pairs (where the address is the key)."
		)]
		public LuaTable ReadByteRange(int addr, int length)
		{
			var lastAddr = length + addr;
			var table = _lua.NewTable();

			if (lastAddr < Global.Emulator.MemoryDomains[_currentMemoryDomain].Size)
			{
				for (var i = addr; i <= lastAddr; i++)
				{
					var a = string.Format("{0:X2}", i);
					var v = Global.Emulator.MemoryDomains[_currentMemoryDomain].PeekByte(i);
					var vs = string.Format("{0:X2}", (int)v);
					table[a] = vs;
				}
			}
			else
			{
				Log("Warning: Attempted read " + lastAddr + " outside memory domain size of " +
					Global.Emulator.MemoryDomains[_currentMemoryDomain].Size +
					" in memory.readbyterange()");
			}

			return table;
		}

		[LuaMethodAttributes(
			"writebyterange",
			"Writes the given values to the given addresses as unsigned bytes"
		)]
		public void WriteByteRange(LuaTable memoryblock)
		{
			foreach (var address in memoryblock.Keys)
			{
				var addr = LuaInt(address);
				if (addr < Global.Emulator.MemoryDomains[_currentMemoryDomain].Size)
				{
					Global.Emulator.MemoryDomains[_currentMemoryDomain].PokeByte(
						addr,
						(byte)LuaInt(memoryblock[address]));
				}
				else
				{
					Log("Warning: Attempted read " + addr + " outside memory domain size of " +
						Global.Emulator.MemoryDomains[_currentMemoryDomain].Size +
						" in memory.writebyterange()");
				}
			}
		}

		[LuaMethodAttributes(
			"getcurrentmemorydomain",
			"Returns a string name of the current memory domain selected by Lua. The default is Main memory"
		)]
		public string GetCurrentMemoryDomain()
		{
			return Global.Emulator.MemoryDomains[_currentMemoryDomain].Name;
		}

		[LuaMethodAttributes(
			"getcurrentmemorydomainsize",
			"Returns the number of bytes of the current memory domain selected by Lua. The default is Main memory"
		)]
		public int GetCurrentMemoryDomainSize()
		{
			return Global.Emulator.MemoryDomains[_currentMemoryDomain].Size;
		}

		[LuaMethodAttributes(
			"readbyte",
			"gets the value from the given address as an unsigned byte"
		)]
		public uint ReadByte(int addr)
		{
			return ReadUnsignedByte(addr);
		}

		[LuaMethodAttributes(
			"readfloat",
			"Reads the given address as a 32-bit float value from the main memory domain with th e given endian"
		)]
		public float ReadFloat(int addr, bool bigendian)
		{
			var val = Global.Emulator.MemoryDomains[_currentMemoryDomain].PeekDWord(addr, bigendian);
			var bytes = BitConverter.GetBytes(val);
			return BitConverter.ToSingle(bytes, 0);
		}

		[LuaMethodAttributes(
			"writebyte",
			"Writes the given value to the given address as an unsigned byte"
		)]
		public void WriteByte(int addr, uint value)
		{
			WriteUnsignedByte(addr, value);
		}

		[LuaMethodAttributes(
			"writefloat",
			"Writes the given 32-bit float value to the given address and endian"
		)]
		public void WriteFloat(int addr, double value, bool bigendian)
		{
			var dv = (float)value;
			var bytes = BitConverter.GetBytes(dv);
			var v = BitConverter.ToUInt32(bytes, 0);
			Global.Emulator.MemoryDomains[_currentMemoryDomain].PokeDWord(addr, v, bigendian);
		}

		[LuaMethodAttributes(
			"usememorydomain",
			"Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true"
		)]
		public bool UseMemoryDomain(string domain)
		{
			for (var i = 0; i < Global.Emulator.MemoryDomains.Count; i++)
			{
				if (Global.Emulator.MemoryDomains[i].Name == domain)
				{
					_currentMemoryDomain = i;
					return true;
				}
			}

			return false;
		}

		[LuaMethodAttributes(
			"read_s8",
			"read signed byte"
		)]
		public int ReadS8(int addr)
		{
			return (sbyte)ReadUnsignedByte(addr);
		}

		[LuaMethodAttributes(
			"read_u8",
			"read unsigned byte"
		)]
		public uint ReadU8(int addr)
		{
			return ReadUnsignedByte(addr);
		}

		[LuaMethodAttributes(
			"read_s16_le",
			"read signed 2 byte value, little endian"
		)]
		public int ReadS16Little(int addr)
		{
			return ReadSignedLittleCore(addr, 2);
		}

		[LuaMethodAttributes(
			"read_s24_le",
			"read signed 24 bit value, little endian"
		)]
		public int ReadS24Little(int addr)
		{
			return ReadSignedLittleCore(addr, 3);
		}

		[LuaMethodAttributes(
			"read_s32_le",
			"read signed 4 byte value, little endian"
		)]
		public int ReadS32Little(int addr)
		{
			return ReadSignedLittleCore(addr, 4);
		}

		[LuaMethodAttributes(
			"read_u16_le",
			"read unsigned 2 byte value, little endian"
		)]
		public uint ReadU16Little(int addr)
		{
			return ReadUnsignedLittle(addr, 2);
		}

		[LuaMethodAttributes(
			"read_u24_le",
			"read unsigned 24 bit value, little endian"
		)]
		public uint ReadU24Little(int addr)
		{
			return ReadUnsignedLittle(addr, 3);
		}

		[LuaMethodAttributes(
			"read_u32_le",
			"read unsigned 4 byte value, little endian"
		)]
		public uint ReadU32Little(int addr)
		{
			return ReadUnsignedLittle(addr, 4);
		}

		[LuaMethodAttributes(
			"read_s16_be",
			"read signed 2 byte value, big endian"
		)]
		public int ReadS16Big(int addr)
		{
			return ReadSignedBig(addr, 2);
		}

		[LuaMethodAttributes(
			"read_s24_be",
			"read signed 24 bit value, big endian"
		)]
		public int ReadS24Big(int addr)
		{
			return ReadSignedBig(addr, 3);
		}

		[LuaMethodAttributes(
			"read_s32_be",
			"read signed 4 byte value, big endian"
		)]
		public int ReadS32Big(int addr)
		{
			return ReadSignedBig(addr, 4);
		}

		[LuaMethodAttributes(
			"read_u16_be",
			"read unsigned 2 byte value, big endian"
		)]
		public uint ReadU16Big(int addr)
		{
			return ReadUnsignedBig(addr, 2);
		}

		[LuaMethodAttributes(
			"read_u24_be",
			"read unsigned 24 bit value, big endian"
		)]
		public uint ReadU24Big(int addr)
		{
			return ReadUnsignedBig(addr, 3);
		}

		[LuaMethodAttributes(
			"u32_be",
			"read unsigned 4 byte value, big endian"
		)]
		public uint ReadU32Big(int addr)
		{
			return ReadUnsignedBig(addr, 4);
		}

		[LuaMethodAttributes(
			"write_s8",
			"write signed byte"
		)]
		public void WriteS8(int addr, uint value)
		{
			WriteUnsignedByte(addr, value);
		}

		[LuaMethodAttributes(
			"write_u8",
			"write unsigned byte"
		)]
		public void WriteU8(int addr, uint value)
		{
			WriteUnsignedByte(addr, value);
		}

		[LuaMethodAttributes(
			"write_s16_le",
			"write signed 2 byte value, little endian"
		)]
		public void WriteS16Little(int addr, int value)
		{
			WriteSignedLittle(addr, value, 2);
		}

		[LuaMethodAttributes(
			"write_s24_le",
			"write signed 24 bit value, little endian"
		)]
		public void WriteS24Little(int addr, int value)
		{
			WriteSignedLittle(addr, value, 3);
		}

		[LuaMethodAttributes(
			"write_s32_le",
			"write signed 4 byte value, little endian"
		)]
		public void WriteS32Little(int addr, int value)
		{
			WriteSignedLittle(addr, value, 4);
		}

		[LuaMethodAttributes(
			"write_u16_le",
			"write unsigned 2 byte value, little endian"
		)]
		public void WriteU16Little(int addr, uint value)
		{
			WriteUnsignedLittle(addr, value, 2);
		}

		[LuaMethodAttributes(
			"write_u24_le",
			"write unsigned 24 bit value, little endian"
		)]
		public void WriteU24Little(int addr, uint value)
		{
			WriteUnsignedLittle(addr, value, 3);
		}

		[LuaMethodAttributes(
			"write_u32_le",
			"write unsigned 4 byte value, little endian"
		)]
		public void WriteU32Little(int addr, uint value)
		{
			WriteUnsignedLittle(addr, value, 4);
		}

		[LuaMethodAttributes(
			"write_s16_be",
			"write signed 2 byte value, big endian"
		)]
		public void WriteS16Big(int addr, int value)
		{
			WriteSignedBig(addr, value, 2);
		}

		[LuaMethodAttributes(
			"write_s24_be",
			"write signed 24 bit value, big endian"
		)]
		public void WriteS24Big(int addr, int value)
		{
			WriteSignedBig(addr, value, 3);
		}

		[LuaMethodAttributes(
			"write_s32_be",
			"write signed 4 byte value, big endian"
		)]
		public void WriteS32Big(int addr, int value)
		{
			WriteSignedBig(addr, value, 4);
		}

		[LuaMethodAttributes(
			"write_u16_be",
			"write unsigned 2 byte value, big endian"
		)]
		public void WriteU16Big(int addr, uint value)
		{
			WriteUnsignedBig(addr, value, 2);
		}

		[LuaMethodAttributes(
			"write_u24_be",
			"write unsigned 24 bit value, big endian"
		)]
		public void WriteU24Big(int addr, uint value)
		{
			WriteUnsignedBig(addr, value, 3);
		}

		[LuaMethodAttributes(
			"write_u32_be",
			"write unsigned 4 byte value, big endian"
		)]
		public void WriteU32Big(int addr, uint value)
		{
			WriteUnsignedBig(addr, value, 4);
		}
	}
}
