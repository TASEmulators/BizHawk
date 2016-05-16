using System;
using LuaInterface;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Base class for the Memory and MainMemory lua libraries
	/// </summary>
	public abstract class LuaMemoryBase : LuaLibraryBase
	{
		[RequiredService]
		protected IEmulator Emulator { get; set; }

		[OptionalService]
		protected IMemoryDomains MemoryDomainCore { get; set; }

		public LuaMemoryBase(Lua lua)
			: base(lua) { }

		public LuaMemoryBase(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		protected abstract MemoryDomain Domain { get; }

		protected IMemoryDomains DomainList
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					return MemoryDomainCore;
				}
				else
				{
					var error = string.Format("Error: {0} does not implement memory domains", Emulator.Attributes().CoreName);
					Log(error);
					throw new NotImplementedException(error);
				}
			}
		}

		public string VerifyMemoryDomain(string domain)
		{
			try
			{
				if (DomainList[domain] == null)
				{
					Log(string.Format("Unable to find domain: {0}, falling back to current", domain));
					return Domain.Name;
				}
				else
				{
					return domain;
				}

			}
			catch // Just in case
			{
				Log(string.Format("Unable to find domain: {0}, falling back to current", domain));
			}
			return Domain.Name;
		}

		protected uint ReadUnsignedByte(int addr, string domain = null)
		{
			var d = (string.IsNullOrEmpty(domain)) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (addr < d.Size)
			{
				return d.PeekByte(addr);
			}

			Log("Warning: attempted read of " + addr +
				" outside the memory size of " + d.Size);
			return 0;
		}

		protected void WriteUnsignedByte(int addr, uint v, string domain = null)
		{
			var d = (string.IsNullOrEmpty(domain)) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (d.CanPoke())
			{
				if (addr < Domain.Size)
				{
					d.PokeByte(addr, (byte)v);
				}
				else
				{
					Log("Warning: attempted write to " + addr +
					" outside the memory size of " + d.Size);
				}
			}
			else
			{
				Log(string.Format("Error: the domain {0} is not writable", d.Name));
			}
		}

		protected static int U2S(uint u, int size)
		{
			var s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		protected int ReadSignedLittleCore(int addr, int size, string domain = null)
		{
			return U2S(ReadUnsignedLittle(addr, size, domain), size);
		}

		protected uint ReadUnsignedLittle(int addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i, domain) << (8 * i);
			}

			return v;
		}

		protected int ReadSignedBig(int addr, int size, string domain = null)
		{
			return U2S(ReadUnsignedBig(addr, size, domain), size);
		}

		protected uint ReadUnsignedBig(int addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i, domain) << (8 * (size - 1 - i));
			}

			return v;
		}

		protected void WriteSignedLittle(int addr, int v, int size, string domain = null)
		{
			WriteUnsignedLittle(addr, (uint)v, size, domain);
		}

		protected void WriteUnsignedLittle(int addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * i)) & 0xFF, domain);
			}
		}

		protected void WriteSignedBig(int addr, int v, int size, string domain = null)
		{
			WriteUnsignedBig(addr, (uint)v, size, domain);
		}

		protected void WriteUnsignedBig(int addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF, domain);
			}
		}

		protected uint ReadSignedLittle(int addr, int size) // only used by mainmemory, so no domain can be passed
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * i);
			}

			return v;
		}

		#region public Library implementations

		protected LuaTable ReadByteRange(int addr, int length, string domain = null)
		{
			var d = (string.IsNullOrEmpty(domain)) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			var lastAddr = length + addr;
			var table = Lua.NewTable();
			if (lastAddr < d.Size)
			{
				for (var i = 0; i <length ; i++)
				{
					int a = addr + i;
					var v = d.PeekByte(a);
					table[i] = v;
				}
			}
			else
			{
				Log("Warning: Attempted read " + lastAddr + " outside memory domain size of " +
					d.Size + " in readbyterange()");
			}

			return table;
		}

		protected void WriteByteRange(LuaTable memoryblock, string domain = null)
		{
			var d = (string.IsNullOrEmpty(domain)) ? Domain : DomainList[VerifyMemoryDomain(domain)];
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
						Log("Warning: Attempted write " + addr + " outside memory domain size of " +
							d.Size + " in writebyterange()");
					}
				}
			}
			else
			{
				Log(string.Format("Error: the domain {0} is not writable", d.Name));
			}
		}

		protected float ReadFloat(int addr, bool bigendian, string domain = null)
		{
			var d = (string.IsNullOrEmpty(domain)) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (addr < d.Size)
			{
				var val = d.PeekUint(addr, bigendian);
				var bytes = BitConverter.GetBytes(val);
				return BitConverter.ToSingle(bytes, 0);
			}
			else
			{
				Log("Warning: Attempted read " + addr +
					" outside memory size of " + d.Size);

				return 0;
			}
		}

		protected void WriteFloat(int addr, double value, bool bigendian, string domain = null)
		{
			var d = (string.IsNullOrEmpty(domain)) ? Domain : DomainList[VerifyMemoryDomain(domain)];
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
					Log("Warning: Attempted write " + addr +
						" outside memory size of " + d.Size);
				}
			}
			else
			{
				Log(string.Format("Error: the domain {0} is not writable", Domain.Name));
			}
		}

		#endregion
	}
}
