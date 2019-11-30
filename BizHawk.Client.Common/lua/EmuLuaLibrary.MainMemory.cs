using System;
using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("Main memory library reads and writes from the Main memory domain (the default memory domain set by any given core)")]
	public sealed class MainMemoryLuaLibrary : LuaLibraryBase
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomainCore { get; set; }

		public MainMemoryLuaLibrary(Lua lua)
			: base(lua) { }

		public MainMemoryLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "mainmemory";

		private MemoryDomain Domain
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

		private uint ReadUnsignedByte(int addr)
		{
			var d = Domain;
			if (addr < d.Size)
			{
				return d.PeekByte(addr);
			}

			Log($"Warning: attempted read of {addr} outside the memory size of {d.Size}");
			return 0;
		}

		private void WriteUnsignedByte(int addr, uint v)
		{
			var d = Domain;
			if (d.CanPoke())
			{
				if (addr < Domain.Size)
				{
					d.PokeByte(addr, (byte)v);
				}
				else
				{
					Log($"Warning: attempted write to {addr} outside the memory size of {d.Size}");
				}
			}
			else
			{
				Log($"Error: the domain {d.Name} is not writable");
			}
		}

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

		private uint ReadSignedLittle(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * i);
			}

			return v;
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
			var d = Domain;
			var lastAddr = length + addr;
			var table = Lua.NewTable();
			if (lastAddr <= d.Size)
			{
				for (var i = 0; i < length; i++)
				{
					int a = addr + i;
					var v = d.PeekByte(a);
					table[i] = v;
				}
			}
			else
			{
				Log($"Warning: Attempted read {lastAddr} outside memory domain size of {d.Size} in readbyterange()");
			}

			return table;
		}

		[LuaMethodExample("")]
		[LuaMethod("writebyterange", "Writes the given values to the given addresses as unsigned bytes")]
		public void WriteByteRange(LuaTable memoryblock)
		{
			var d = Domain;
			if (d.CanPoke())
			{
				foreach (var address in memoryblock.Keys)
				{
					var addr = LuaInt(address);
					if (addr < d.Size)
					{
						d.PokeByte(addr, (byte)LuaInt(memoryblock[address]));
					}
					else
					{
						Log($"Warning: Attempted write {addr} outside memory domain size of {d.Size} in writebyterange()");
					}
				}
			}
			else
			{
				Log($"Error: the domain {d.Name} is not writable");
			}
		}

		[LuaMethodExample("local simairea = mainmemory.readfloat(0x100, false);")]
		[LuaMethod("readfloat", "Reads the given address as a 32-bit float value from the main memory domain with th e given endian")]
		public float ReadFloat(int addr, bool bigendian)
		{
			var d = Domain;
			if (addr < d.Size)
			{
				var val = d.PeekUint(addr, bigendian);
				var bytes = BitConverter.GetBytes(val);
				return BitConverter.ToSingle(bytes, 0);
			}

			Log($"Warning: Attempted read {addr} outside memory size of {d.Size}");

			return 0;
		}

		[LuaMethodExample("mainmemory.writefloat( 0x100, 10.0, false );")]
		[LuaMethod("writefloat", "Writes the given 32-bit float value to the given address and endian")]
		public void WriteFloat(int addr, double value, bool bigendian)
		{
			var d = Domain;
			if (d.CanPoke())
			{
				if (addr < d.Size)
				{
					var dv = (float)value;
					var bytes = BitConverter.GetBytes(dv);
					var v = BitConverter.ToUInt32(bytes, 0);
					d.PokeUint(addr, v, bigendian);
				}
				else
				{
					Log($"Warning: Attempted write {addr} outside memory size of {d.Size}");
				}
			}
			else
			{
				Log($"Error: the domain {Domain.Name} is not writable");
			}
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
