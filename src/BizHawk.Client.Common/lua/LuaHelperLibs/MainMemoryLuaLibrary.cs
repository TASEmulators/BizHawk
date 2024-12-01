using System.ComponentModel;
using System.Linq;

using BizHawk.Emulation.Common;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("Main memory library reads and writes from the Main memory domain (the default memory domain set by any given core)")]
	public sealed class MainMemoryLuaLibrary : LuaLibraryBase
	{
		public MainMemoryLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "mainmemory";

		private MemoryDomain _mainMemDomain;

		private string _mainMemName;

		private MemoryDomain Domain => _mainMemDomain ??= ((MemoryApi) APIs.Memory).DomainList[MainMemName]!;

		private string MainMemName => _mainMemName ??= APIs.Memory.MainMemoryName;

		public override void Restarted()
		{
			_mainMemDomain = null;
			_mainMemName = null;
		}

		[LuaMethodExample("local stmaiget = mainmemory.getname( );")]
		[LuaMethod("getname", "returns the name of the domain defined as main memory for the given core")]
		public string GetName()
			=> MainMemName;

		[LuaMethodExample("local uimaiget = mainmemory.getcurrentmemorydomainsize( );")]
		[LuaMethod("getcurrentmemorydomainsize", "Returns the number of bytes of the domain defined as main memory")]
		public uint GetSize()
		{
			return (uint)Domain.Size;
		}

		[LuaMethodExample("local uimairea = mainmemory.readbyte( 0x100 );")]
		[LuaMethod("readbyte", "gets the value from the given address as an unsigned byte")]
		public uint ReadByte(long addr)
			=> APIs.Memory.ReadByte(addr, MainMemName);

		[LuaMethodExample("mainmemory.writebyte( 0x100, 1000 );")]
		[LuaMethod("writebyte", "Writes the given value to the given address as an unsigned byte")]
		public void WriteByte(long addr, uint value)
			=> APIs.Memory.WriteByte(addr, value, MainMemName);

		[LuaDeprecatedMethod]
		[LuaMethod("readbyterange", "Reads the address range that starts from address, and is length long. Returns a zero-indexed table containing the read values (an array of bytes.)")]
		public LuaTable ReadByteRange(long addr, int length)
			=> _th.ListToTable(APIs.Memory.ReadByteRange(addr, length, MainMemName), indexFrom: 0);

		[LuaMethodExample("local bytes = mainmemory.read_bytes_as_array(0x100, 30);")]
		[LuaMethod("read_bytes_as_array", "Reads length bytes starting at addr into an array-like table (1-indexed).")]
		public LuaTable ReadBytesAsArray(long addr, int length)
			=> _th.ListToTable(APIs.Memory.ReadByteRange(addr, length, MainMemName));

		[LuaMethodExample("local bytes = mainmemory.read_bytes_as_dict(0x100, 30);")]
		[LuaMethod("read_bytes_as_dict", "Reads length bytes starting at addr into a dict-like table (where the keys are the addresses, relative to the start of the main memory).")]
		public LuaTable ReadBytesAsDict(long addr, int length)
			=> _th.MemoryBlockToTable(APIs.Memory.ReadByteRange(addr, length, MainMemName), addr);

		[LuaDeprecatedMethod]
		[LuaMethod("writebyterange", "Writes the given values to the given addresses as unsigned bytes")]
		public void WriteByteRange(LuaTable memoryblock)
		{
#if true
			WriteBytesAsDict(memoryblock);
#else
			var d = Domain;
			if (d.CanPoke())
			{
				foreach (var (addr, v) in _th.EnumerateEntries<long, long>(memoryblock))
				{
					if (addr < d.Size)
					{
						d.PokeByte(addr, (byte) v);
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
#endif
		}

		[LuaMethodExample("mainmemory.write_bytes_as_array(0x100, { 0xAB, 0x12, 0xCD, 0x34 });")]
		[LuaMethod("write_bytes_as_array", "Writes sequential bytes starting at addr.")]
		public void WriteBytesAsArray(long addr, LuaTable bytes)
			=> APIs.Memory.WriteByteRange(addr, _th.EnumerateValues<long>(bytes).Select(l => (byte) l).ToList(), MainMemName);

		[LuaMethodExample("mainmemory.write_bytes_as_dict({ [0x100] = 0xAB, [0x104] = 0xCD, [0x106] = 0x12, [0x107] = 0x34, [0x108] = 0xEF });")]
		[LuaMethod("write_bytes_as_dict", "Writes bytes at arbitrary addresses (the keys of the given table are the addresses, relative to the start of the main memory).")]
		public void WriteBytesAsDict(LuaTable addrMap)
		{
			foreach (var (addr, v) in _th.EnumerateEntries<long, long>(addrMap))
			{
				APIs.Memory.WriteByte(addr, (uint) v, MainMemName);
			}
		}

		[LuaMethodExample("local simairea = mainmemory.readfloat(0x100, false);")]
		[LuaMethod("readfloat", "Reads the given address as a 32-bit float value from the main memory domain with th e given endian")]
		public float ReadFloat(long addr, bool bigendian)
		{
			APIs.Memory.SetBigEndian(bigendian);
			return APIs.Memory.ReadFloat(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.writefloat( 0x100, 10.0, false );")]
		[LuaMethod("writefloat", "Writes the given 32-bit float value to the given address and endian")]
		public void WriteFloat(long addr, float value, bool bigendian)
		{
			APIs.Memory.SetBigEndian(bigendian);
			APIs.Memory.WriteFloat(addr, value, MainMemName);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s8( 0x100 );")]
		[LuaMethod("read_s8", "read signed byte")]
		public int ReadS8(long addr)
			=> APIs.Memory.ReadS8(addr, MainMemName);

		[LuaMethodExample("mainmemory.write_s8( 0x100, 1000 );")]
		[LuaMethod("write_s8", "write signed byte")]
		public void WriteS8(long addr, uint value)
			=> APIs.Memory.WriteS8(addr, unchecked((int) value), MainMemName);

		[LuaMethodExample("local uimairea = mainmemory.read_u8( 0x100 );")]
		[LuaMethod("read_u8", "read unsigned byte")]
		public uint ReadU8(long addr)
			=> APIs.Memory.ReadU8(addr, MainMemName);

		[LuaMethodExample("mainmemory.write_u8( 0x100, 1000 );")]
		[LuaMethod("write_u8", "write unsigned byte")]
		public void WriteU8(long addr, uint value)
			=> APIs.Memory.WriteU8(addr, value, MainMemName);

		[LuaMethodExample("local inmairea = mainmemory.read_s16_le( 0x100 );")]
		[LuaMethod("read_s16_le", "read signed 2 byte value, little endian")]
		public int ReadS16Little(long addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS16(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_s16_le( 0x100, -1000 );")]
		[LuaMethod("write_s16_le", "write signed 2 byte value, little endian")]
		public void WriteS16Little(long addr, int value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS16(addr, value, MainMemName);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s16_be( 0x100 );")]
		[LuaMethod("read_s16_be", "read signed 2 byte value, big endian")]
		public int ReadS16Big(long addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS16(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_s16_be( 0x100, -1000 );")]
		[LuaMethod("write_s16_be", "write signed 2 byte value, big endian")]
		public void WriteS16Big(long addr, int value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS16(addr, value, MainMemName);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u16_le( 0x100 );")]
		[LuaMethod("read_u16_le", "read unsigned 2 byte value, little endian")]
		public uint ReadU16Little(long addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU16(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_u16_le( 0x100, 1000 );")]
		[LuaMethod("write_u16_le", "write unsigned 2 byte value, little endian")]
		public void WriteU16Little(long addr, uint value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU16(addr, value, MainMemName);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u16_be( 0x100 );")]
		[LuaMethod("read_u16_be", "read unsigned 2 byte value, big endian")]
		public uint ReadU16Big(long addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU16(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_u16_be( 0x100, 1000 );")]
		[LuaMethod("write_u16_be", "write unsigned 2 byte value, big endian")]
		public void WriteU16Big(long addr, uint value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU16(addr, value, MainMemName);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s24_le( 0x100 );")]
		[LuaMethod("read_s24_le", "read signed 24 bit value, little endian")]
		public int ReadS24Little(long addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS24(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_s24_le( 0x100, -1000 );")]
		[LuaMethod("write_s24_le", "write signed 24 bit value, little endian")]
		public void WriteS24Little(long addr, int value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS24(addr, value, MainMemName);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s24_be( 0x100 );")]
		[LuaMethod("read_s24_be", "read signed 24 bit value, big endian")]
		public int ReadS24Big(long addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS24(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_s24_be( 0x100, -1000 );")]
		[LuaMethod("write_s24_be", "write signed 24 bit value, big endian")]
		public void WriteS24Big(long addr, int value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS24(addr, value, MainMemName);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u24_le( 0x100 );")]
		[LuaMethod("read_u24_le", "read unsigned 24 bit value, little endian")]
		public uint ReadU24Little(long addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU24(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_u24_le( 0x100, 1000 );")]
		[LuaMethod("write_u24_le", "write unsigned 24 bit value, little endian")]
		public void WriteU24Little(long addr, uint value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU24(addr, value, MainMemName);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u24_be( 0x100 );")]
		[LuaMethod("read_u24_be", "read unsigned 24 bit value, big endian")]
		public uint ReadU24Big(long addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU24(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_u24_be( 0x100, 1000 );")]
		[LuaMethod("write_u24_be", "write unsigned 24 bit value, big endian")]
		public void WriteU24Big(long addr, uint value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU24(addr, value, MainMemName);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s32_le( 0x100 );")]
		[LuaMethod("read_s32_le", "read signed 4 byte value, little endian")]
		public int ReadS32Little(long addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS32(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_s32_le( 0x100, -1000 );")]
		[LuaMethod("write_s32_le", "write signed 4 byte value, little endian")]
		public void WriteS32Little(long addr, int value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS32(addr, value, MainMemName);
		}

		[LuaMethodExample("local inmairea = mainmemory.read_s32_be( 0x100 );")]
		[LuaMethod("read_s32_be", "read signed 4 byte value, big endian")]
		public int ReadS32Big(long addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS32(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_s32_be( 0x100, -1000 );")]
		[LuaMethod("write_s32_be", "write signed 4 byte value, big endian")]
		public void WriteS32Big(long addr, int value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS32(addr, value, MainMemName);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u32_le( 0x100 );")]
		[LuaMethod("read_u32_le", "read unsigned 4 byte value, little endian")]
		public uint ReadU32Little(long addr)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU32(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_u32_le( 0x100, 1000 );")]
		[LuaMethod("write_u32_le", "write unsigned 4 byte value, little endian")]
		public void WriteU32Little(long addr, uint value)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU32(addr, value, MainMemName);
		}

		[LuaMethodExample("local uimairea = mainmemory.read_u32_be( 0x100 );")]
		[LuaMethod("read_u32_be", "read unsigned 4 byte value, big endian")]
		public uint ReadU32Big(long addr)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU32(addr, MainMemName);
		}

		[LuaMethodExample("mainmemory.write_u32_be( 0x100, 1000 );")]
		[LuaMethod("write_u32_be", "write unsigned 4 byte value, big endian")]
		public void WriteU32Big(long addr, uint value)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU32(addr, value, MainMemName);
		}
	}
}
