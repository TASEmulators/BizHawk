using System;
using System.ComponentModel;

using LuaInterface;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

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

		public override string Name { get { return "memory"; } }

		protected override MemoryDomain Domain
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					if (_currentMemoryDomain == null)
					{
						if (MemoryDomainCore.HasSystemBus)
						{
							_currentMemoryDomain = MemoryDomainCore.SystemBus;
						}
						else
						{
							_currentMemoryDomain = MemoryDomainCore.MainMemory;
						}
					}

					return _currentMemoryDomain;
				}
				else
				{
					var error = string.Format("Error: {0} does not implement memory domains", Emulator.Attributes().CoreName);
					Log(error);
					throw new NotImplementedException(error);
				}
			}
		}

		#region Unique Library Methods

		[LuaMethodAttributes(
			"getmemorydomainlist",
			"Returns a string of the memory domains for the loaded platform core. List will be a single string delimited by line feeds"
		)]
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

		[LuaMethodAttributes(
			"getmemorydomainsize",
			"Returns the number of bytes of the specified memory domain. If no domain is specified, or the specified domain doesn't exist, returns the current domain size"
		)]
		public uint GetMemoryDomainSize(string name = "")
		{
			if (string.IsNullOrEmpty(name))
				return (uint)Domain.Size;
			else
				return (uint)DomainList[VerifyMemoryDomain(name)].Size;
		}

		[LuaMethodAttributes(
			"getcurrentmemorydomain",
			"Returns a string name of the current memory domain selected by Lua. The default is Main memory"
		)]
		public string GetCurrentMemoryDomain()
		{
			return Domain.Name;
		}

		[LuaMethodAttributes(
			"getcurrentmemorydomainsize",
			"Returns the number of bytes of the current memory domain selected by Lua. The default is Main memory"
		)]
		public uint GetCurrentMemoryDomainSize()
		{
			return (uint)Domain.Size;
		}

		[LuaMethodAttributes(
			"usememorydomain",
			"Attempts to set the current memory domain to the given domain. If the name does not match a valid memory domain, the function returns false, else it returns true"
		)]
		public bool UseMemoryDomain(string domain)
		{
			try
			{
				if (DomainList[domain] != null)
				{
					_currentMemoryDomain = DomainList[domain];
					return true;
				}
				else
				{
					Log(string.Format("Unable to find domain: {0}", domain));
					return false;
				}

			}
			catch // Just in case
			{
				Log(string.Format("Unable to find domain: {0}", domain));
			}

			return false;
		}

		#endregion

		#region Common Special and Legacy Methods

		[LuaMethodAttributes(
			"readbyte",
			"gets the value from the given address as an unsigned byte"
		)]
		public uint ReadByte(int addr, string domain = null)
		{
			return ReadUnsignedByte(addr, domain);
		}

		[LuaMethodAttributes(
			"writebyte",
			"Writes the given value to the given address as an unsigned byte"
		)]
		public void WriteByte(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		[LuaMethodAttributes(
			"readbyterange",
			"Reads the address range that starts from address, and is length long. Returns the result into a table of key value pairs (where the address is the key)."
		)]
		public new LuaTable ReadByteRange(int addr, int length, string domain = null)
		{
			return base.ReadByteRange(addr, length, domain);
		}

		[LuaMethodAttributes(
			"writebyterange",
			"Writes the given values to the given addresses as unsigned bytes"
		)]
		public new void WriteByteRange(LuaTable memoryblock, string domain = null)
		{
			base.WriteByteRange(memoryblock, domain);
		}

		[LuaMethodAttributes(
			"readfloat",
			"Reads the given address as a 32-bit float value from the main memory domain with th e given endian"
		)]
		public new float ReadFloat(int addr, bool bigendian, string domain = null)
		{
			return base.ReadFloat(addr, bigendian, domain);
		}

		[LuaMethodAttributes(
			"writefloat",
			"Writes the given 32-bit float value to the given address and endian"
		)]
		public new void WriteFloat(int addr, double value, bool bigendian, string domain = null)
		{
			base.WriteFloat(addr, value, bigendian, domain);
		}

		#endregion

		#region 1 Byte

		[LuaMethodAttributes("read_s8", "read signed byte")]
		public int ReadS8(int addr, string domain = null)
		{
			return (sbyte)ReadUnsignedByte(addr, domain);
		}

		[LuaMethodAttributes("write_s8", "write signed byte")]
		public void WriteS8(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		[LuaMethodAttributes("read_u8", "read unsigned byte")]
		public uint ReadU8(int addr, string domain = null)
		{
			return ReadUnsignedByte(addr, domain);
		}

		[LuaMethodAttributes("write_u8", "write unsigned byte")]
		public void WriteU8(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		#endregion

		#region 2 Byte

		[LuaMethodAttributes("read_s16_le", "read signed 2 byte value, little endian")]
		public int ReadS16Little(int addr, string domain = null)
		{
			return ReadSignedLittleCore(addr, 2, domain);
		}

		[LuaMethodAttributes("write_s16_le", "write signed 2 byte value, little endian")]
		public void WriteS16Little(int addr, int value, string domain = null)
		{
			WriteSignedLittle(addr, value, 2, domain);
		}

		[LuaMethodAttributes("read_s16_be", "read signed 2 byte value, big endian")]
		public int ReadS16Big(int addr, string domain = null)
		{
			return ReadSignedBig(addr, 2, domain);
		}

		[LuaMethodAttributes("write_s16_be", "write signed 2 byte value, big endian")]
		public void WriteS16Big(int addr, int value, string domain = null)
		{
			WriteSignedBig(addr, value, 2, domain);
		}

		[LuaMethodAttributes("read_u16_le", "read unsigned 2 byte value, little endian")]
		public uint ReadU16Little(int addr, string domain = null)
		{
			return ReadUnsignedLittle(addr, 2, domain);
		}

		[LuaMethodAttributes("write_u16_le", "write unsigned 2 byte value, little endian")]
		public void WriteU16Little(int addr, uint value, string domain = null)
		{
			WriteUnsignedLittle(addr, value, 2, domain);
		}

		[LuaMethodAttributes("read_u16_be", "read unsigned 2 byte value, big endian")]
		public uint ReadU16Big(int addr, string domain = null)
		{
			return ReadUnsignedBig(addr, 2, domain);
		}

		[LuaMethodAttributes("write_u16_be", "write unsigned 2 byte value, big endian")]
		public void WriteU16Big(int addr, uint value, string domain = null)
		{
			WriteUnsignedBig(addr, value, 2, domain);
		}

		#endregion

		#region 3 Byte

		[LuaMethodAttributes("read_s24_le", "read signed 24 bit value, little endian")]
		public int ReadS24Little(int addr, string domain = null)
		{
			return ReadSignedLittleCore(addr, 3, domain);
		}

		[LuaMethodAttributes("write_s24_le", "write signed 24 bit value, little endian")]
		public void WriteS24Little(int addr, int value, string domain = null)
		{
			WriteSignedLittle(addr, value, 3, domain);
		}

		[LuaMethodAttributes("read_s24_be", "read signed 24 bit value, big endian")]
		public int ReadS24Big(int addr, string domain = null)
		{
			return ReadSignedBig(addr, 3, domain);
		}

		[LuaMethodAttributes("write_s24_be", "write signed 24 bit value, big endian")]
		public void WriteS24Big(int addr, int value, string domain = null)
		{
			WriteSignedBig(addr, value, 3, domain);
		}

		[LuaMethodAttributes("read_u24_le", "read unsigned 24 bit value, little endian")]
		public uint ReadU24Little(int addr, string domain = null)
		{
			return ReadUnsignedLittle(addr, 3, domain);
		}

		[LuaMethodAttributes("write_u24_le", "write unsigned 24 bit value, little endian")]
		public void WriteU24Little(int addr, uint value, string domain = null)
		{
			WriteUnsignedLittle(addr, value, 3, domain);
		}

		[LuaMethodAttributes("read_u24_be", "read unsigned 24 bit value, big endian")]
		public uint ReadU24Big(int addr, string domain = null)
		{
			return ReadUnsignedBig(addr, 3, domain);
		}

		[LuaMethodAttributes("write_u24_be", "write unsigned 24 bit value, big endian")]
		public void WriteU24Big(int addr, uint value, string domain = null)
		{
			WriteUnsignedBig(addr, value, 3, domain);
		}

		#endregion

		#region 4 Byte

		[LuaMethodAttributes("read_s32_le", "read signed 4 byte value, little endian")]
		public int ReadS32Little(int addr, string domain = null)
		{
			return ReadSignedLittleCore(addr, 4, domain);
		}

		[LuaMethodAttributes("write_s32_le", "write signed 4 byte value, little endian")]
		public void WriteS32Little(int addr, int value, string domain = null)
		{
			WriteSignedLittle(addr, value, 4, domain);
		}

		[LuaMethodAttributes("read_s32_be", "read signed 4 byte value, big endian")]
		public int ReadS32Big(int addr, string domain = null)
		{
			return ReadSignedBig(addr, 4, domain);
		}

		[LuaMethodAttributes("write_s32_be", "write signed 4 byte value, big endian")]
		public void WriteS32Big(int addr, int value, string domain = null)
		{
			WriteSignedBig(addr, value, 4, domain);
		}

		[LuaMethodAttributes("read_u32_le", "read unsigned 4 byte value, little endian")]
		public uint ReadU32Little(int addr, string domain = null)
		{
			return ReadUnsignedLittle(addr, 4, domain);
		}

		[LuaMethodAttributes("write_u32_le", "write unsigned 4 byte value, little endian")]
		public void WriteU32Little(int addr, uint value, string domain = null)
		{
			WriteUnsignedLittle(addr, value, 4, domain);
		}

		[LuaMethodAttributes("read_u32_be", "read unsigned 4 byte value, big endian")]
		public uint ReadU32Big(int addr, string domain = null)
		{
			return ReadUnsignedBig(addr, 4, domain);
		}

		[LuaMethodAttributes("write_u32_be", "write unsigned 4 byte value, big endian")]
		public void WriteU32Big(int addr, uint value, string domain = null)
		{
			WriteUnsignedBig(addr, value, 4, domain);
		}

		#endregion
	}
}
