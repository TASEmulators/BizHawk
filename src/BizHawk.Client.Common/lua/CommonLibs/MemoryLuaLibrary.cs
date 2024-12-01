using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("These functions behavior identically to the mainmemory functions but the user can set the memory domain to read and write from. The default domain is the system bus. Use getcurrentmemorydomain(), and usememorydomain() to control which domain is used. Each core has its own set of valid memory domains. Use getmemorydomainlist() to get a list of memory domains for the current core loaded.")]
	public sealed class MemoryLuaLibrary : LuaLibraryBase
	{
		public MemoryLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "memory";

		[LuaMethodExample("local nlmemget = memory.getmemorydomainlist();")]
		[LuaMethod("getmemorydomainlist", "Returns a string of the memory domains for the loaded platform core. List will be a single string delimited by line feeds")]
		public LuaTable GetMemoryDomainList()
			=> _th.ListToTable((List<string>) APIs.Memory.GetMemoryDomainList(), indexFrom: 0); //HACK cast will succeed as long as impl. returns .Select<T, string>().ToList() as IROC<string>

		[LuaMethodExample("local uimemget = memory.getmemorydomainsize( mainmemory.getname( ) );")]
		[LuaMethod("getmemorydomainsize", "Returns the number of bytes of the specified memory domain. If no domain is specified, or the specified domain doesn't exist, returns the current domain size")]
		public uint GetMemoryDomainSize(string name = "")
			=> APIs.Memory.GetMemoryDomainSize(name);

		[LuaMethodExample("local stmemget = memory.getcurrentmemorydomain( );")]
		[LuaMethod("getcurrentmemorydomain", "Returns a string name of the current memory domain selected by Lua. The default is Main memory")]
		public string GetCurrentMemoryDomain()
			=> APIs.Memory.GetCurrentMemoryDomain();

		[LuaMethodExample("local uimemget = memory.getcurrentmemorydomainsize( );")]
		[LuaMethod("getcurrentmemorydomainsize", "Returns the number of bytes of the current memory domain selected by Lua. The default is Main memory")]
		public uint GetCurrentMemoryDomainSize()
			=> APIs.Memory.GetCurrentMemoryDomainSize();

		[LuaMethodExample("if ( memory.usememorydomain( mainmemory.getname( ) ) ) then\r\n\tconsole.log( \"Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true\" );\r\nend;")]
		[LuaMethod("usememorydomain", "Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true")]
		public bool UseMemoryDomain(string domain)
			=> APIs.Memory.UseMemoryDomain(domain);

		[LuaMethodExample("local stmemhas = memory.hash_region( 0x100, 50, mainmemory.getname( ) );")]
		[LuaMethod("hash_region", "Returns a hash as a string of a region of memory, starting from addr, through count bytes. If the domain is unspecified, it uses the current region.")]
		public string HashRegion(long addr, int count, string domain = null)
			=> APIs.Memory.HashRegion(addr, count, domain);

		[LuaMethodExample("local uimemrea = memory.readbyte( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("readbyte", "gets the value from the given address as an unsigned byte")]
		public uint ReadByte(long addr, string domain = null)
			=> APIs.Memory.ReadByte(addr, domain);

		[LuaMethodExample("memory.writebyte( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("writebyte", "Writes the given value to the given address as an unsigned byte")]
		public void WriteByte(long addr, uint value, string domain = null)
			=> APIs.Memory.WriteByte(addr, value, domain);

		[LuaDeprecatedMethod]
		[LuaMethod("readbyterange", "Reads the address range that starts from address, and is length long. Returns a zero-indexed table containing the read values (an array of bytes.)")]
		public LuaTable ReadByteRange(long addr, int length, string domain = null)
			=> _th.ListToTable(APIs.Memory.ReadByteRange(addr, length, domain), indexFrom: 0);

		[LuaMethodExample("local bytes = memory.read_bytes_as_array(0x100, 30, \"WRAM\");")]
		[LuaMethod("read_bytes_as_array", "Reads length bytes starting at addr into an array-like table (1-indexed).")]
		public LuaTable ReadBytesAsArray(long addr, int length, string domain = null)
			=> _th.ListToTable(APIs.Memory.ReadByteRange(addr, length, domain));

		[LuaMethodExample("local bytes = memory.read_bytes_as_dict(0x100, 30, \"WRAM\");")]
		[LuaMethod("read_bytes_as_dict", "Reads length bytes starting at addr into a dict-like table (where the keys are the addresses, relative to the start of the domain).")]
		public LuaTable ReadBytesAsDict(long addr, int length, string domain = null)
			=> _th.MemoryBlockToTable(APIs.Memory.ReadByteRange(addr, length, domain), addr);

		[LuaDeprecatedMethod]
		[LuaMethod("writebyterange", "Writes the given values to the given addresses as unsigned bytes")]
		public void WriteByteRange(LuaTable memoryblock, string domain = null)
		{
#if true
			WriteBytesAsDict(memoryblock, domain);
#else
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
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

		[LuaMethodExample("memory.write_bytes_as_array(0x100, { 0xAB, 0x12, 0xCD, 0x34 });")]
		[LuaMethod("write_bytes_as_array", "Writes sequential bytes starting at addr.")]
		public void WriteBytesAsArray(long addr, LuaTable bytes, string domain = null)
			=> APIs.Memory.WriteByteRange(addr, _th.EnumerateValues<long>(bytes).Select(l => (byte) l).ToList(), domain);

		[LuaMethodExample("memory.write_bytes_as_dict({ [0x100] = 0xAB, [0x104] = 0xCD, [0x106] = 0x12, [0x107] = 0x34, [0x108] = 0xEF });")]
		[LuaMethod("write_bytes_as_dict", "Writes bytes at arbitrary addresses (the keys of the given table are the addresses, relative to the start of the domain).")]
		public void WriteBytesAsDict(LuaTable addrMap, string domain = null)
		{
			foreach (var (addr, v) in _th.EnumerateEntries<long, long>(addrMap))
			{
				APIs.Memory.WriteByte(addr, (uint) v, domain);
			}
		}

		[LuaMethodExample("local simemrea = memory.readfloat( 0x100, false, mainmemory.getname( ) );")]
		[LuaMethod("readfloat", "Reads the given address as a 32-bit float value from the main memory domain with th e given endian")]
		public float ReadFloat(long addr, bool bigendian, string domain = null)
		{
			APIs.Memory.SetBigEndian(bigendian);
			return APIs.Memory.ReadFloat(addr, domain);
		}

		[LuaMethodExample("memory.writefloat( 0x100, 10.0, false, mainmemory.getname( ) );")]
		[LuaMethod("writefloat", "Writes the given 32-bit float value to the given address and endian")]
		public void WriteFloat(long addr, float value, bool bigendian, string domain = null)
		{
			APIs.Memory.SetBigEndian(bigendian);
			APIs.Memory.WriteFloat(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s8( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s8", "read signed byte")]
		public int ReadS8(long addr, string domain = null)
			=> APIs.Memory.ReadS8(addr, domain);

		[LuaMethodExample("memory.write_s8( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s8", "write signed byte")]
		public void WriteS8(long addr, uint value, string domain = null)
			=> APIs.Memory.WriteS8(addr, unchecked((int) value), domain);

		[LuaMethodExample("local uimemrea = memory.read_u8( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u8", "read unsigned byte")]
		public uint ReadU8(long addr, string domain = null)
			=> APIs.Memory.ReadU8(addr, domain);

		[LuaMethodExample("memory.write_u8( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u8", "write unsigned byte")]
		public void WriteU8(long addr, uint value, string domain = null)
			=> APIs.Memory.WriteU8(addr, value, domain);

		[LuaMethodExample("local inmemrea = memory.read_s16_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s16_le", "read signed 2 byte value, little endian")]
		public int ReadS16Little(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS16(addr, domain);
		}

		[LuaMethodExample("memory.write_s16_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s16_le", "write signed 2 byte value, little endian")]
		public void WriteS16Little(long addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS16(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s16_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s16_be", "read signed 2 byte value, big endian")]
		public int ReadS16Big(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS16(addr, domain);
		}

		[LuaMethodExample("memory.write_s16_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s16_be", "write signed 2 byte value, big endian")]
		public void WriteS16Big(long addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS16(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u16_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u16_le", "read unsigned 2 byte value, little endian")]
		public uint ReadU16Little(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU16(addr, domain);
		}

		[LuaMethodExample("memory.write_u16_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u16_le", "write unsigned 2 byte value, little endian")]
		public void WriteU16Little(long addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU16(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u16_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u16_be", "read unsigned 2 byte value, big endian")]
		public uint ReadU16Big(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU16(addr, domain);
		}

		[LuaMethodExample("memory.write_u16_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u16_be", "write unsigned 2 byte value, big endian")]
		public void WriteU16Big(long addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU16(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s24_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s24_le", "read signed 24 bit value, little endian")]
		public int ReadS24Little(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS24(addr, domain);
		}

		[LuaMethodExample("memory.write_s24_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s24_le", "write signed 24 bit value, little endian")]
		public void WriteS24Little(long addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS24(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s24_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s24_be", "read signed 24 bit value, big endian")]
		public int ReadS24Big(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS24(addr, domain);
		}

		[LuaMethodExample("memory.write_s24_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s24_be", "write signed 24 bit value, big endian")]
		public void WriteS24Big(long addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS24(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u24_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u24_le", "read unsigned 24 bit value, little endian")]
		public uint ReadU24Little(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU24(addr, domain);
		}

		[LuaMethodExample("memory.write_u24_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u24_le", "write unsigned 24 bit value, little endian")]
		public void WriteU24Little(long addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU24(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u24_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u24_be", "read unsigned 24 bit value, big endian")]
		public uint ReadU24Big(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU24(addr, domain);
		}

		[LuaMethodExample("memory.write_u24_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u24_be", "write unsigned 24 bit value, big endian")]
		public void WriteU24Big(long addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU24(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s32_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s32_le", "read signed 4 byte value, little endian")]
		public int ReadS32Little(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadS32(addr, domain);
		}

		[LuaMethodExample("memory.write_s32_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s32_le", "write signed 4 byte value, little endian")]
		public void WriteS32Little(long addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteS32(addr, value, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s32_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s32_be", "read signed 4 byte value, big endian")]
		public int ReadS32Big(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadS32(addr, domain);
		}

		[LuaMethodExample("memory.write_s32_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s32_be", "write signed 4 byte value, big endian")]
		public void WriteS32Big(long addr, int value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteS32(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u32_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u32_le", "read unsigned 4 byte value, little endian")]
		public uint ReadU32Little(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			return APIs.Memory.ReadU32(addr, domain);
		}

		[LuaMethodExample("memory.write_u32_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u32_le", "write unsigned 4 byte value, little endian")]
		public void WriteU32Little(long addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian(false);
			APIs.Memory.WriteU32(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u32_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u32_be", "read unsigned 4 byte value, big endian")]
		public uint ReadU32Big(long addr, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			return APIs.Memory.ReadU32(addr, domain);
		}

		[LuaMethodExample("memory.write_u32_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u32_be", "write unsigned 4 byte value, big endian")]
		public void WriteU32Big(long addr, uint value, string domain = null)
		{
			APIs.Memory.SetBigEndian();
			APIs.Memory.WriteU32(addr, value, domain);
		}
	}
}
