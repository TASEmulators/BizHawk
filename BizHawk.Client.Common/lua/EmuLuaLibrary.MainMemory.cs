using System;
using System.ComponentModel;

using NLua;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	[Description("Main memory library reads and writes from the Main memory domain (the default memory domain set by any given core)")]
	public sealed class MainMemoryLuaLibrary : LuaMemoryBase
	{
		public MainMemoryLuaLibrary(Lua lua)
			: base(lua) { }

		public MainMemoryLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "mainmemory";

		protected override MemoryDomain Domain
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					return MemoryDomainCore.MainMemory;
				}
				else
				{
					var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
					Log(error);
					throw new NotImplementedException(error);
				}
			}
		}

		#region Unique Library Methods

		[LuaMethodExample("local stmaiget = mainmemory.getname( );")]
		[LuaMethod("getname", "returns the name of the domain defined as main memory for the given core")]
		public string GetName()
		{
			return Domain.Name;
		}

		[LuaMethodExample("local uimaiget = mainmemory.getcurrentmemorydomainsize( );")]
		[LuaMethod("getcurrentmemorydomainsize", "Returns the number of bytes of the domain defined as main memory")]
		public uint GetSize()
		{
			return (uint)Domain.Size;
		}

		#endregion

		#region Common Special and Legacy Methods

		[LuaMethodExample("local uimairea = mainmemory.readbyte( 0x100 );")]
		[LuaMethod("readbyte", "gets the value from the given address as an unsigned byte")]
		public uint ReadByte(int addr)
		{
			return ReadUnsignedByte(addr);
		}

		[LuaMethodExample("mainmemory.writebyte( 0x100, 1000 );")]
		[LuaMethod("writebyte", "Writes the given value to the given address as an unsigned byte")]
		public void WriteByte(int addr, uint value)
		{
			WriteUnsignedByte(addr, value);
		}

		[LuaMethodExample("local nlmairea = mainmemory.readbyterange( 0x100, 64 );")]
		[LuaMethod("readbyterange", "Reads the address range that starts from address, and is length long. Returns the result into a table of key value pairs (where the address is the key).")]
		public LuaTable ReadByteRange(int addr, int length)
		{
			return base.ReadByteRange(addr, length);
		}

		[LuaMethodExample("")]
		[LuaMethod("writebyterange", "Writes the given values to the given addresses as unsigned bytes")]
		public void WriteByteRange(LuaTable memoryblock)
		{
			base.WriteByteRange(memoryblock);
		}

		[LuaMethodExample("local simairea = mainmemory.readfloat(0x100, false);")]
		[LuaMethod("readfloat", "Reads the given address as a 32-bit float value from the main memory domain with th e given endian")]
		public float ReadFloat(int addr, bool bigendian)
		{
			return base.ReadFloat(addr, bigendian);
		}

		[LuaMethodExample("mainmemory.writefloat( 0x100, 10.0, false );")]
		[LuaMethod("writefloat", "Writes the given 32-bit float value to the given address and endian")]
		public void WriteFloat(int addr, double value, bool bigendian)
		{
			base.WriteFloat(addr, value, bigendian);
		}

		#endregion

		#region 1 Byte

		[LuaMethodExample("local inmairea = mainmemory.read_s8( 0x100 );")]
		[LuaMethod("read_s8", "read signed byte")]
		public int ReadS8(int addr)
		{
			return (sbyte)ReadUnsignedByte(addr);
		}

		[LuaMethodExample("mainmemory.write_s8( 0x100, 1000 );")]
		[LuaMethod("write_s8", "write signed byte")]
		public void WriteS8(int addr, uint value)
		{
			WriteUnsignedByte(addr, value);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u8( 0x100 );")]
		[LuaMethod("read_u8", "read unsigned byte")]
		public uint ReadU8(int addr)
		{
			return ReadUnsignedByte(addr);
		}

		[LuaMethodExample("mainmemory.write_u8( 0x100, 1000 );")]
		[LuaMethod("write_u8", "write unsigned byte")]
		public void WriteU8(int addr, uint value)
		{
			WriteUnsignedByte(addr, value);
		}

		#endregion

		#region 2 Byte

		[LuaMethodExample("local inmairea = mainmemory.read_s16_le( 0x100 );")]
		[LuaMethod("read_s16_le", "read signed 2 byte value, little endian")]
		public int ReadS16Little(int addr)
		{
			return ReadSignedLittleCore(addr, 2);
		}

		[LuaMethodExample("mainmemory.write_s16_le( 0x100, -1000 );")]
		[LuaMethod("write_s16_le", "write signed 2 byte value, little endian")]
		public void WriteS16Little(int addr, int value)
		{
			WriteSignedLittle(addr, value, 2);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s16_be( 0x100 );")]
		[LuaMethod("read_s16_be", "read signed 2 byte value, big endian")]
		public int ReadS16Big(int addr)
		{
			return ReadSignedBig(addr, 2);
		}

		[LuaMethodExample("mainmemory.write_s16_be( 0x100, -1000 );")]
		[LuaMethod("write_s16_be", "write signed 2 byte value, big endian")]
		public void WriteS16Big(int addr, int value)
		{
			WriteSignedBig(addr, value, 2);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u16_le( 0x100 );")]
		[LuaMethod("read_u16_le", "read unsigned 2 byte value, little endian")]
		public uint ReadU16Little(int addr)
		{
			return ReadSignedLittle(addr, 2);
		}

		[LuaMethodExample("mainmemory.write_u16_le( 0x100, 1000 );")]
		[LuaMethod("write_u16_le", "write unsigned 2 byte value, little endian")]
		public void WriteU16Little(int addr, uint value)
		{
			WriteUnsignedLittle(addr, value, 2);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u16_be( 0x100 );")]
		[LuaMethod("read_u16_be", "read unsigned 2 byte value, big endian")]
		public uint ReadU16Big(int addr)
		{
			return ReadUnsignedBig(addr, 2);
		}

		[LuaMethodExample("mainmemory.write_u16_be( 0x100, 1000 );")]
		[LuaMethod("write_u16_be", "write unsigned 2 byte value, big endian")]
		public void WriteU16Big(int addr, uint value)
		{
			WriteUnsignedBig(addr, value, 2);
		}

		#endregion

		#region 3 Byte

		[LuaMethodExample("local inmairea = mainmemory.read_s24_le( 0x100 );")]
		[LuaMethod("read_s24_le", "read signed 24 bit value, little endian")]
		public int ReadS24Little(int addr)
		{
			return ReadSignedLittleCore(addr, 3);
		}

		[LuaMethodExample("mainmemory.write_s24_le( 0x100, -1000 );")]
		[LuaMethod("write_s24_le", "write signed 24 bit value, little endian")]
		public void WriteS24Little(int addr, int value)
		{
			WriteSignedLittle(addr, value, 3);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s24_be( 0x100 );")]
		[LuaMethod("read_s24_be", "read signed 24 bit value, big endian")]
		public int ReadS24Big(int addr)
		{
			return ReadSignedBig(addr, 3);
		}

		[LuaMethodExample("mainmemory.write_s24_be( 0x100, -1000 );")]
		[LuaMethod("write_s24_be", "write signed 24 bit value, big endian")]
		public void WriteS24Big(int addr, int value)
		{
			WriteSignedBig(addr, value, 3);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u24_le( 0x100 );")]
		[LuaMethod("read_u24_le", "read unsigned 24 bit value, little endian")]
		public uint ReadU24Little(int addr)
		{
			return ReadSignedLittle(addr, 3);
		}

		[LuaMethodExample("mainmemory.write_u24_le( 0x100, 1000 );")]
		[LuaMethod("write_u24_le", "write unsigned 24 bit value, little endian")]
		public void WriteU24Little(int addr, uint value)
		{
			WriteUnsignedLittle(addr, value, 3);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u24_be( 0x100 );")]
		[LuaMethod("read_u24_be", "read unsigned 24 bit value, big endian")]
		public uint ReadU24Big(int addr)
		{
			return ReadUnsignedBig(addr, 3);
		}

		[LuaMethodExample("mainmemory.write_u24_be( 0x100, 1000 );")]
		[LuaMethod("write_u24_be", "write unsigned 24 bit value, big endian")]
		public void WriteU24Big(int addr, uint value)
		{
			WriteUnsignedBig(addr, value, 3);
		}

		#endregion

		#region 4 Byte

		[LuaMethodExample("local inmairea = mainmemory.read_s32_le( 0x100 );")]
		[LuaMethod("read_s32_le", "read signed 4 byte value, little endian")]
		public int ReadS32Little(int addr)
		{
			return ReadSignedLittleCore(addr, 4);
		}

		[LuaMethodExample("mainmemory.write_s32_le( 0x100, -1000 );")]
		[LuaMethod("write_s32_le", "write signed 4 byte value, little endian")]
		public void WriteS32Little(int addr, int value)
		{
			WriteSignedLittle(addr, value, 4);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s32_be( 0x100 );")]
		[LuaMethod("read_s32_be", "read signed 4 byte value, big endian")]
		public int ReadS32Big(int addr)
		{
			return ReadSignedBig(addr, 4);
		}

		[LuaMethodExample("mainmemory.write_s32_be( 0x100, -1000 );")]
		[LuaMethod("write_s32_be", "write signed 4 byte value, big endian")]
		public void WriteS32Big(int addr, int value)
		{
			WriteSignedBig(addr, value, 4);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u32_le( 0x100 );")]
		[LuaMethod("read_u32_le", "read unsigned 4 byte value, little endian")]
		public uint ReadU32Little(int addr)
		{
			return ReadSignedLittle(addr, 4);
		}

		[LuaMethodExample("mainmemory.write_u32_le( 0x100, 1000 );")]
		[LuaMethod("write_u32_le", "write unsigned 4 byte value, little endian")]
		public void WriteU32Little(int addr, uint value)
		{
			WriteUnsignedLittle(addr, value, 4);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u32_be( 0x100 );")]
		[LuaMethod("read_u32_be", "read unsigned 4 byte value, big endian")]
		public uint ReadU32Big(int addr)
		{
			return ReadUnsignedBig(addr, 4);
		}

		[LuaMethodExample("mainmemory.write_u32_be( 0x100, 1000 );")]
		[LuaMethod("write_u32_be", "write unsigned 4 byte value, big endian")]
		public void WriteU32Big(int addr, uint value)
		{
			WriteUnsignedBig(addr, value, 4);
		}

		#endregion
	}
}
