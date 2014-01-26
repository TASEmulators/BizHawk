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
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"getname",
					"readbyte",
					"readbyterange",
					"readfloat",
					"writebyte",
					"writebyterange",
					"writefloat",

					"read_s8",
					"read_u8",
					"read_s16_le",
					"read_s24_le",
					"read_s32_le",
					"read_u16_le",
					"read_u24_le",
					"read_u32_le",
					"read_s16_be",
					"read_s24_be",
					"read_s32_be",
					"read_u16_be",
					"read_u24_be",
					"read_u32_be",
					"write_s8",
					"write_u8",
					"write_s16_le",
					"write_s24_le",
					"write_s32_le",
					"write_u16_le",
					"write_u24_le",
					"write_u32_le",
					"write_s16_be",
					"write_s24_be",
					"write_s32_be",
					"write_u16_be",
					"write_u24_be",
					"write_u32_be"
				};
			}
		}

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
			"TODO"
		)]
		public string GetName()
		{
			return Global.Emulator.MemoryDomains.MainMemory.Name;
		}

		[LuaMethodAttributes(
			"readbyte",
			"TODO"
		)]
		public uint ReadByte(object addr)
		{
			return ReadUnsignedByte(LuaInt(addr));
		}

		[LuaMethodAttributes(
			"readbyterange",
			"TODO"
		)]
		public LuaTable ReadByteRange(object address, object length)
		{
			var addr = LuaInt(address);
			var lastAddr = LuaInt(length) + addr;
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
			"TODO"
		)]
		public float ReadFloat(object addr, bool bigendian)
		{
			var val = Global.Emulator.MemoryDomains.MainMemory.PeekDWord(LuaInt(addr), bigendian);
			var bytes = BitConverter.GetBytes(val);
			var _float = BitConverter.ToSingle(bytes, 0);

			return _float;
		}

		[LuaMethodAttributes(
			"writebyte",
			"TODO"
		)]
		public void WriteByte(object addr, object value)
		{
			WriteUnsignedByte(
				LuaInt(addr),
				LuaUInt(value)
			);
		}

		[LuaMethodAttributes(
			"writebyterange",
			"TODO"
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
			"TODO"
		)]
		public void WriteFloat(object address, object value, bool bigendian)
		{
			var addr = LuaInt(address);
			var dv = (float)(double)value;
			var bytes = BitConverter.GetBytes(dv);
			var v = BitConverter.ToUInt32(bytes, 0);
			Global.Emulator.MemoryDomains.MainMemory.PokeDWord(addr, v, bigendian);
		}

		[LuaMethodAttributes(
			"read_s8",
			"TODO"
		)]
		public int ReadS8(object addr)
		{
			return (sbyte)ReadUnsignedByte(LuaInt(addr));
		}

		[LuaMethodAttributes(
			"read_u8",
			"TODO"
		)]
		public uint ReadU8(object addr)
		{
			return ReadUnsignedByte(LuaInt(addr));
		}

		[LuaMethodAttributes(
			"read_s16_le",
			"TODO"
		)]
		public int ReadS16Little(object addr)
		{
			return ReadSignedLittleCore(LuaInt(addr), 2);
		}

		[LuaMethodAttributes(
			"read_s24_le",
			"TODO"
		)]
		public int ReadS24Little(object addr)
		{
			return ReadSignedLittleCore(LuaInt(addr), 3);
		}

		[LuaMethodAttributes(
			"read_s32_le",
			"TODO"
		)]
		public int ReadS32Little(object addr)
		{
			return ReadSignedLittleCore(LuaInt(addr), 4);
		}

		[LuaMethodAttributes(
			"read_u16_le",
			"TODO"
		)]
		public uint ReadU16Little(object addr)
		{
			return ReadSignedLittle(LuaInt(addr), 2);
		}

		[LuaMethodAttributes(
			"read_u16_le",
			"TODO"
		)]
		public uint ReadU24Little(object addr)
		{
			return ReadSignedLittle(LuaInt(addr), 3);
		}

		[LuaMethodAttributes(
			"read_u32_le",
			"TODO"
		)]
		public uint ReadU32Little(object addr)
		{
			return ReadSignedLittle(LuaInt(addr), 4);
		}

		[LuaMethodAttributes(
			"read_s16_be",
			"TODO"
		)]
		public int ReadS16Big(object addr)
		{
			return ReadSignedBig(LuaInt(addr), 2);
		}

		[LuaMethodAttributes(
			"read_s24_be",
			"TODO"
		)]
		public int ReadS24Big(object addr)
		{
			return ReadSignedBig(LuaInt(addr), 3);
		}

		[LuaMethodAttributes(
			"read_s32_be",
			"TODO"
		)]
		public int ReadS32Big(object addr)
		{
			return ReadSignedBig(LuaInt(addr), 4);
		}

		[LuaMethodAttributes(
			"read_u16_be",
			"TODO"
		)]
		public uint ReadU16Big(object addr)
		{
			return ReadUnsignedBig(LuaInt(addr), 2);
		}

		[LuaMethodAttributes(
			"read_u24_be",
			"TODO"
		)]
		public uint ReadU24Big(object addr)
		{
			return ReadUnsignedBig(LuaInt(addr), 3);
		}

		[LuaMethodAttributes(
			"read_u32_be",
			"TODO"
		)]
		public uint ReadU32Big(object addr)
		{
			return ReadUnsignedBig(LuaInt(addr), 4);
		}

		[LuaMethodAttributes(
			"write_s8",
			"TODO"
		)]
		public void WriteS8(object addr, object value)
		{
			WriteUnsignedByte(
				LuaInt(addr),
				LuaUInt(value)
			);
		}

		[LuaMethodAttributes(
			"write_u8",
			"TODO"
		)]
		public void WriteU8(object addr, object value)
		{
			WriteUnsignedByte(
				LuaInt(addr),
				LuaUInt(value)
			);
		}

		[LuaMethodAttributes(
			"write_s16_le",
			"TODO"
		)]
		public void WriteS16Little(object addr, object value)
		{
			WriteSignedLittle(
				LuaInt(addr),
				LuaInt(value),
				2);
		}

		[LuaMethodAttributes(
			"write_s24_le",
			"TODO"
		)]
		public void WriteS24Little(object addr, object value)
		{
			WriteSignedLittle(
				LuaInt(addr),
				LuaInt(value),
				3);
		}

		[LuaMethodAttributes(
			"write_s32_le",
			"TODO"
		)]
		public void WriteS32Little(object addr, object value)
		{
			WriteSignedLittle(
				LuaInt(addr),
				LuaInt(value),
				4);
		}

		[LuaMethodAttributes(
			"write_u16_le",
			"TODO"
		)]
		public void WriteU16Little(object addr, object value)
		{
			WriteUnsignedLittle(
				LuaInt(addr),
				LuaUInt(value),
				2);
		}

		[LuaMethodAttributes(
			"write_u24_le",
			"TODO"
		)]
		public void WriteU24Little(object addr, object value)
		{
			WriteUnsignedLittle(
				LuaInt(addr),
				LuaUInt(value),
				3);
		}

		[LuaMethodAttributes(
			"write_u32_le",
			"TODO"
		)]
		public void WriteU32Little(object addr, object value)
		{
			WriteUnsignedLittle(
				LuaInt(addr),
				LuaUInt(value),
				4);
		}

		[LuaMethodAttributes(
			"write_s16_be",
			"TODO"
		)]
		public void WriteS16Big(object addr, object value)
		{
			WriteSignedBig(
				LuaInt(addr),
				LuaInt(value),
				2);
		}

		[LuaMethodAttributes(
			"write_s24_be",
			"TODO"
		)]
		public void WriteS24Big(object addr, object value)
		{
			WriteSignedBig(
				LuaInt(addr),
				LuaInt(value),
				3);
		}

		[LuaMethodAttributes(
			"write_s32_be",
			"TODO"
		)]
		public void WriteS32Big(object addr, object value)
		{
			WriteSignedBig(
				LuaInt(addr),
				LuaInt(value),
				4);
		}

		[LuaMethodAttributes(
			"write_u16_be",
			"TODO"
		)]
		public void WriteU16Big(object addr, object value)
		{
			WriteUnsignedBig(
				LuaInt(addr),
				LuaUInt(value),
				2);
		}

		[LuaMethodAttributes(
			"write_u24_be",
			"TODO"
		)]
		public void WriteU24Big(object addr, object value)
		{
			WriteUnsignedBig(
				LuaInt(addr),
				LuaUInt(value),
				3);
		}

		[LuaMethodAttributes(
			"write_u32_be",
			"TODO"
		)]
		public void WriteU32Big(object addr, object value)
		{
			WriteUnsignedBig(
				LuaInt(addr),
				LuaUInt(value),
				4);
		}
	}
}
