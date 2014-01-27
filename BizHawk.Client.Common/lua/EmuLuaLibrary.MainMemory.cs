using System;

using LuaInterface;

namespace BizHawk.Client.Common
{
	// TODO: this needs a major refactor, as well as MemoryLuaLibrary, and this shoudl inherit memorylua library and extend it
	public class MainMemoryLuaLibrary : LuaLibraryBase
	{
		public MainMemoryLuaLibrary(Lua lua)
		{
			_lua = lua;
		}

		public override string Name { get { return "mainmemory"; } }

		private readonly Lua _lua;

		#region Main Memory Library Helpers

		private static int U2S(uint u, int size)
		{
			var s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		private static int ReadSignedLittleCore(int addr, int size)
		{
			return U2S(ReadSignedLittle(addr, size), size);
		}

		private static uint ReadSignedLittle(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * i);
			}

			return v;
		}

		private static int ReadSignedBig(int addr, int size)
		{
			return U2S(ReadUnsignedBig(addr, size), size);
		}

		private static uint ReadUnsignedBig(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * (size - 1 - i));
			}

			return v;
		}

		private static void WriteSignedLittle(int addr, int v, int size)
		{
			WriteUnsignedLittle(addr, (uint)v, size);
		}

		private static void WriteUnsignedLittle(int addr, uint v, int size)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * i)) & 0xFF);
			}
		}

		private static void WriteSignedBig(int addr, int v, int size)
		{
			WriteUnsignedBig(addr, (uint)v, size);
		}

		private static void WriteUnsignedBig(int addr, uint v, int size)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF);
			}
		}

		private static uint ReadUnsignedByte(int addr)
		{
			return Global.Emulator.MemoryDomains.MainMemory.PeekByte(addr);
		}

		private static void WriteUnsignedByte(int addr, uint v)
		{
			Global.Emulator.MemoryDomains.MainMemory.PokeByte(addr, (byte)v);
		}

		#endregion

		[LuaMethodAttributes(
			"getname",
			"returns the name of the domain defined as main memory for the given core"
		)]
		public string GetName()
		{
			return Global.Emulator.MemoryDomains.MainMemory.Name;
		}

		[LuaMethodAttributes(
			"readbyte", "gets the value from the given address as an unsigned byte"
		)]
		public uint ReadByte(int addr)
		{
			return ReadUnsignedByte(addr);
		}

		[LuaMethodAttributes(
			"readbyterange",
			"Reads the address range that starts from address, and is length long. Returns the result into a table of key value pairs (where the address is the key)."
		)]
		public LuaTable ReadByteRange(int addr, int length)
		{
			var lastAddr = length + addr;
			var table = _lua.NewTable();
			for (var i = addr; i <= lastAddr; i++)
			{
				var a = String.Format("{0:X2}", i);
				var v = Global.Emulator.MemoryDomains.MainMemory.PeekByte(i);
				var vs = String.Format("{0:X2}", (int)v);
				table[a] = vs;
			}

			return table;
		}

		[LuaMethodAttributes(
			"readfloat",
			"Reads the given address as a 32-bit float value from the main memory domain with th e given endian"
		)]
		public float ReadFloat(int addr, bool bigendian)
		{
			var val = Global.Emulator.MemoryDomains.MainMemory.PeekDWord(addr, bigendian);
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
			"writebyterange",
			"Writes the given values to the given addresses as unsigned bytes"
		)]
		public void WriteByteRange(LuaTable memoryblock)
		{
			foreach (var address in memoryblock.Keys)
			{
				Global.Emulator.MemoryDomains.MainMemory.PokeByte(
					LuaInt(address),
					(byte)LuaInt(memoryblock[address])
				);
			}
		}

		[LuaMethodAttributes(
			"writefloat",
			"Writes the given 32-bit float value to the given address and endian"
		)]
		public void WriteFloat(int address, double value, bool bigendian)
		{
			var dv = (float)value;
			var bytes = BitConverter.GetBytes(dv);
			var v = BitConverter.ToUInt32(bytes, 0);
			Global.Emulator.MemoryDomains.MainMemory.PokeDWord(address, v, bigendian);
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
			return ReadSignedLittle(addr, 2);
		}

		[LuaMethodAttributes(
			"read_u24_le",
			"read unsigned 24 bit value, little endian"
		)]
		public uint ReadU24Little(int addr)
		{
			return ReadSignedLittle(addr, 3);
		}

		[LuaMethodAttributes(
			"read_u32_le",
			"read unsigned 4 byte value, little endian"
		)]
		public uint ReadU32Little(int addr)
		{
			return ReadSignedLittle(addr, 4);
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
			"read_u32_be",
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
