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

		protected uint ReadUnsignedByte(int addr)
		{
			if (addr < Domain.Size)
			{
				return Domain.PeekByte(addr);
			}

			Log("Warning: attempted read of " + addr +
				" outside the memory size of " + Domain.Size);
			return 0;
		}

		protected void WriteUnsignedByte(int addr, uint v)
		{
			if (addr < Domain.Size)
			{
				Domain.PokeByte(addr, (byte)v);
			}
			else
			{
				Log("Warning: attempted write to " + addr +
				" outside the memory size of " + Domain.Size);
			}
		}

		protected static int U2S(uint u, int size)
		{
			var s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		protected int ReadSignedLittleCore(int addr, int size)
		{
			return U2S(ReadUnsignedLittle(addr, size), size);
		}

		protected uint ReadUnsignedLittle(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * i);
			}

			return v;
		}

		protected int ReadSignedBig(int addr, int size)
		{
			return U2S(ReadUnsignedBig(addr, size), size);
		}

		protected uint ReadUnsignedBig(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * (size - 1 - i));
			}

			return v;
		}

		protected void WriteSignedLittle(int addr, int v, int size)
		{
			WriteUnsignedLittle(addr, (uint)v, size);
		}

		protected void WriteUnsignedLittle(int addr, uint v, int size)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * i)) & 0xFF);
			}
		}

		protected void WriteSignedBig(int addr, int v, int size)
		{
			WriteUnsignedBig(addr, (uint)v, size);
		}

		protected void WriteUnsignedBig(int addr, uint v, int size)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF);
			}
		}

		protected uint ReadSignedLittle(int addr, int size)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i) << (8 * i);
			}

			return v;
		}

		#region public Library implementations

		protected LuaTable ReadByteRange(int addr, int length)
		{
			var lastAddr = length + addr;
			var table = Lua.NewTable();
			if (lastAddr < Domain.Size)
			{
				for (var i = addr; i <= lastAddr; i++)
				{
					var a = string.Format("{0:X2}", i);
					var v = Domain.PeekByte(i);
					var vs = string.Format("{0:X2}", (int)v);
					table[a] = vs;
				}
			}
			else
			{
				Log("Warning: Attempted read " + lastAddr + " outside memory domain size of " +
					Domain.Size + " in readbyterange()");
			}

			return table;
		}

		protected void WriteByteRange(LuaTable memoryblock)
		{
			foreach (var address in memoryblock.Keys)
			{
				var addr = LuaInt(address);
				if (addr < Domain.Size)
				{
					Domain.PokeByte(addr, (byte)LuaInt(memoryblock[address]));
				}
				else
				{
					Log("Warning: Attempted write " + addr + " outside memory domain size of " +
						Domain.Size + " in writebyterange()");
				}
			}
		}

		protected float ReadFloat(int addr, bool bigendian)
		{
			if (addr < Domain.Size)
			{
				var val = Domain.PeekDWord(addr, bigendian);
				var bytes = BitConverter.GetBytes(val);
				return BitConverter.ToSingle(bytes, 0);
			}
			else
			{
				Log("Warning: Attempted read " + addr +
					" outside memory size of " + Domain.Size);

				return 0;
			}
		}

		protected void WriteFloat(int addr, double value, bool bigendian)
		{
			if (addr < Domain.Size)
			{
				var dv = (float)value;
				var bytes = BitConverter.GetBytes(dv);
				var v = BitConverter.ToUInt32(bytes, 0);
				Domain.PokeDWord(addr, v, bigendian);
			}
			else
			{
				Log("Warning: Attempted write " + addr + 
					" outside memory size of " + Domain.Size);
			}
		}

		#endregion
	}
}
