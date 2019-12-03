using System;
using System.ComponentModel;

using NLua;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Common.BufferExtensions;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	[Description("These functions behavior identically to the mainmemory functions but the user can set the memory domain to read and write from. The default domain is main memory. Use getcurrentmemorydomain(), and usememorydomain() to control which domain is used. Each core has its own set of valid memory domains. Use getmemorydomainlist() to get a list of memory domains for the current core loaded.")]
	public sealed class MemoryLuaLibrary : LuaMemoryBase
	{
		private MemoryDomain _currentMemoryDomain;

		public MemoryLuaLibrary(Lua lua)
			: base(lua)
		{
		}

		public MemoryLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback)
		{
		}

		public override string Name => "memory";

		protected override MemoryDomain Domain
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					if (_currentMemoryDomain == null)
					{
						_currentMemoryDomain = MemoryDomainCore.HasSystemBus
							? MemoryDomainCore.SystemBus
							: MemoryDomainCore.MainMemory;
					}

					return _currentMemoryDomain;
				}

				var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
				Log(error);
				throw new NotImplementedException(error);
			}
		}

		#region Unique Library Methods

		[LuaMethodExample("local nlmemget = memory.getmemorydomainlist();")]
		[LuaMethod("getmemorydomainlist", "Returns a string of the memory domains for the loaded platform core. List will be a single string delimited by line feeds")]
		public LuaTable GetMemoryDomainList()
		{
			var table = Lua.NewTable();

			int i = 0;
			foreach (var domain in DomainList)
			{
				table[i] = domain.Name;
				i++;
			}

			return table;
		}

		[LuaMethodExample("local uimemget = memory.getmemorydomainsize( mainmemory.getname( ) );")]
		[LuaMethod("getmemorydomainsize", "Returns the number of bytes of the specified memory domain. If no domain is specified, or the specified domain doesn't exist, returns the current domain size")]
		public uint GetMemoryDomainSize(string name = "")
		{
			if (string.IsNullOrEmpty(name))
			{
				return (uint)Domain.Size;
			}

			return (uint)DomainList[VerifyMemoryDomain(name)].Size;
		}

		[LuaMethodExample("local stmemget = memory.getcurrentmemorydomain( );")]
		[LuaMethod("getcurrentmemorydomain", "Returns a string name of the current memory domain selected by Lua. The default is Main memory")]
		public string GetCurrentMemoryDomain()
		{
			return Domain.Name;
		}

		[LuaMethodExample("local uimemget = memory.getcurrentmemorydomainsize( );")]
		[LuaMethod("getcurrentmemorydomainsize", "Returns the number of bytes of the current memory domain selected by Lua. The default is Main memory")]
		public uint GetCurrentMemoryDomainSize()
		{
			return (uint)Domain.Size;
		}

		[LuaMethodExample("if ( memory.usememorydomain( mainmemory.getname( ) ) ) then\r\n\tconsole.log( \"Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true\" );\r\nend;")]
		[LuaMethod("usememorydomain", "Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true")]
		public bool UseMemoryDomain(string domain)
		{
			try
			{
				if (DomainList[domain] != null)
				{
					_currentMemoryDomain = DomainList[domain];
					return true;
				}

				Log($"Unable to find domain: {domain}");
				return false;
			}
			catch // Just in case
			{
				Log($"Unable to find domain: {domain}");
			}

			return false;
		}

		[LuaMethodExample("local stmemhas = memory.hash_region( 0x100, 50, mainmemory.getname( ) );")]
		[LuaMethod("hash_region", "Returns a hash as a string of a region of memory, starting from addr, through count bytes. If the domain is unspecified, it uses the current region.")]
		public string HashRegion(int addr, int count, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];

			// checks
			if (addr < 0 || addr >= d.Size)
			{
				string error = $"Address {addr} is outside the bounds of domain {d.Name}";
				Log(error);
				throw new ArgumentOutOfRangeException(error);
			}
			if (addr + count > d.Size)
			{
				string error = $"Address {addr} + count {count} is outside the bounds of domain {d.Name}";
				Log(error);
				throw new ArgumentOutOfRangeException(error);
			}

			byte[] data = new byte[count];
			for (int i = 0; i < count; i++)
			{
				data[i] = d.PeekByte(addr + i);
			}

			using var hasher = System.Security.Cryptography.SHA256.Create();
			return hasher.ComputeHash(data).BytesToHexString();
		}

		#endregion

		#region Common Special and Legacy Methods

		[LuaMethodExample("local uimemrea = memory.readbyte( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("readbyte", "gets the value from the given address as an unsigned byte")]
		public uint ReadByte(int addr, string domain = null)
		{
			return ReadUnsignedByte(addr, domain);
		}

		[LuaMethodExample("memory.writebyte( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("writebyte", "Writes the given value to the given address as an unsigned byte")]
		public void WriteByte(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		[LuaMethodExample("local nlmemrea = memory.readbyterange( 0x100, 30, mainmemory.getname( ) );")]
		[LuaMethod("readbyterange", "Reads the address range that starts from address, and is length long. Returns the result into a table of key value pairs (where the address is the key).")]
		public new LuaTable ReadByteRange(int addr, int length, string domain = null)
		{
			return base.ReadByteRange(addr, length, domain);
		}

		[LuaMethodExample("")]
		[LuaMethod("writebyterange", "Writes the given values to the given addresses as unsigned bytes")]
		public new void WriteByteRange(LuaTable memoryblock, string domain = null)
		{
			base.WriteByteRange(memoryblock, domain);
		}

		[LuaMethodExample("local simemrea = memory.readfloat( 0x100, false, mainmemory.getname( ) );")]
		[LuaMethod("readfloat", "Reads the given address as a 32-bit float value from the main memory domain with th e given endian")]
		public new float ReadFloat(int addr, bool bigendian, string domain = null)
		{
			return base.ReadFloat(addr, bigendian, domain);
		}

		[LuaMethodExample("memory.writefloat( 0x100, 10.0, false, mainmemory.getname( ) );")]
		[LuaMethod("writefloat", "Writes the given 32-bit float value to the given address and endian")]
		public new void WriteFloat(int addr, double value, bool bigendian, string domain = null)
		{
			base.WriteFloat(addr, value, bigendian, domain);
		}

		#endregion

		#region 1 Byte

		[LuaMethodExample("local inmemrea = memory.read_s8( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s8", "read signed byte")]
		public int ReadS8(int addr, string domain = null)
		{
			return (sbyte)ReadUnsignedByte(addr, domain);
		}

		[LuaMethodExample("memory.write_s8( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s8", "write signed byte")]
		public void WriteS8(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u8( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u8", "read unsigned byte")]
		public uint ReadU8(int addr, string domain = null)
		{
			return ReadUnsignedByte(addr, domain);
		}

		[LuaMethodExample("memory.write_u8( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u8", "write unsigned byte")]
		public void WriteU8(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		#endregion

		#region 2 Byte

		[LuaMethodExample("local inmemrea = memory.read_s16_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s16_le", "read signed 2 byte value, little endian")]
		public int ReadS16Little(int addr, string domain = null)
		{
			return ReadSignedLittleCore(addr, 2, domain);
		}

		[LuaMethodExample("memory.write_s16_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s16_le", "write signed 2 byte value, little endian")]
		public void WriteS16Little(int addr, int value, string domain = null)
		{
			WriteSignedLittle(addr, value, 2, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s16_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s16_be", "read signed 2 byte value, big endian")]
		public int ReadS16Big(int addr, string domain = null)
		{
			return ReadSignedBig(addr, 2, domain);
		}

		[LuaMethodExample("memory.write_s16_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s16_be", "write signed 2 byte value, big endian")]
		public void WriteS16Big(int addr, int value, string domain = null)
		{
			WriteSignedBig(addr, value, 2, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u16_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u16_le", "read unsigned 2 byte value, little endian")]
		public uint ReadU16Little(int addr, string domain = null)
		{
			return ReadUnsignedLittle(addr, 2, domain);
		}

		[LuaMethodExample("memory.write_u16_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u16_le", "write unsigned 2 byte value, little endian")]
		public void WriteU16Little(int addr, uint value, string domain = null)
		{
			WriteUnsignedLittle(addr, value, 2, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u16_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u16_be", "read unsigned 2 byte value, big endian")]
		public uint ReadU16Big(int addr, string domain = null)
		{
			return ReadUnsignedBig(addr, 2, domain);
		}

		[LuaMethodExample("memory.write_u16_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u16_be", "write unsigned 2 byte value, big endian")]
		public void WriteU16Big(int addr, uint value, string domain = null)
		{
			WriteUnsignedBig(addr, value, 2, domain);
		}

		#endregion

		#region 3 Byte

		[LuaMethodExample("local inmemrea = memory.read_s24_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s24_le", "read signed 24 bit value, little endian")]
		public int ReadS24Little(int addr, string domain = null)
		{
			return ReadSignedLittleCore(addr, 3, domain);
		}

		[LuaMethodExample("memory.write_s24_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s24_le", "write signed 24 bit value, little endian")]
		public void WriteS24Little(int addr, int value, string domain = null)
		{
			WriteSignedLittle(addr, value, 3, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s24_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s24_be", "read signed 24 bit value, big endian")]
		public int ReadS24Big(int addr, string domain = null)
		{
			return ReadSignedBig(addr, 3, domain);
		}

		[LuaMethodExample("memory.write_s24_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s24_be", "write signed 24 bit value, big endian")]
		public void WriteS24Big(int addr, int value, string domain = null)
		{
			WriteSignedBig(addr, value, 3, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u24_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u24_le", "read unsigned 24 bit value, little endian")]
		public uint ReadU24Little(int addr, string domain = null)
		{
			return ReadUnsignedLittle(addr, 3, domain);
		}

		[LuaMethodExample("memory.write_u24_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u24_le", "write unsigned 24 bit value, little endian")]
		public void WriteU24Little(int addr, uint value, string domain = null)
		{
			WriteUnsignedLittle(addr, value, 3, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u24_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u24_be", "read unsigned 24 bit value, big endian")]
		public uint ReadU24Big(int addr, string domain = null)
		{
			return ReadUnsignedBig(addr, 3, domain);
		}

		[LuaMethodExample("memory.write_u24_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u24_be", "write unsigned 24 bit value, big endian")]
		public void WriteU24Big(int addr, uint value, string domain = null)
		{
			WriteUnsignedBig(addr, value, 3, domain);
		}

		#endregion

		#region 4 Byte

		[LuaMethodExample("local inmemrea = memory.read_s32_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s32_le", "read signed 4 byte value, little endian")]
		public int ReadS32Little(int addr, string domain = null)
		{
			return ReadSignedLittleCore(addr, 4, domain);
		}

		[LuaMethodExample("memory.write_s32_le( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s32_le", "write signed 4 byte value, little endian")]
		public void WriteS32Little(int addr, int value, string domain = null)
		{
			WriteSignedLittle(addr, value, 4, domain);
		}

		[LuaMethodExample("local inmemrea = memory.read_s32_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_s32_be", "read signed 4 byte value, big endian")]
		public int ReadS32Big(int addr, string domain = null)
		{
			return ReadSignedBig(addr, 4, domain);
		}

		[LuaMethodExample("memory.write_s32_be( 0x100, -1000, mainmemory.getname( ) );")]
		[LuaMethod("write_s32_be", "write signed 4 byte value, big endian")]
		public void WriteS32Big(int addr, int value, string domain = null)
		{
			WriteSignedBig(addr, value, 4, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u32_le( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u32_le", "read unsigned 4 byte value, little endian")]
		public uint ReadU32Little(int addr, string domain = null)
		{
			return ReadUnsignedLittle(addr, 4, domain);
		}

		[LuaMethodExample("memory.write_u32_le( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u32_le", "write unsigned 4 byte value, little endian")]
		public void WriteU32Little(int addr, uint value, string domain = null)
		{
			WriteUnsignedLittle(addr, value, 4, domain);
		}

		[LuaMethodExample("local uimemrea = memory.read_u32_be( 0x100, mainmemory.getname( ) );")]
		[LuaMethod("read_u32_be", "read unsigned 4 byte value, big endian")]
		public uint ReadU32Big(int addr, string domain = null)
		{
			return ReadUnsignedBig(addr, 4, domain);
		}

		[LuaMethodExample("memory.write_u32_be( 0x100, 1000, mainmemory.getname( ) );")]
		[LuaMethod("write_u32_be", "write unsigned 4 byte value, big endian")]
		public void WriteU32Big(int addr, uint value, string domain = null)
		{
			WriteUnsignedBig(addr, value, 4, domain);
		}

		#endregion
	}
}
