using System;
namespace BizHawk.Client.Common
{
	public class BitLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "bit"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"Band",
					"Bnot",
					"Bor",
					"Bxor",
					"Lshift",
					"Rol",
					"Ror",
					"Rshift",
					"Check",
					"Set",
					"Clear",
					"Byteswap_16",
					"Byteswap_32",
					"Byteswap_64"
				};
			}
		}

		public static uint Band(object val, object amt)
		{
			return (uint)(LuaInt(val) & LuaInt(amt));
		}

		public static uint Bnot(object val)
		{
			return (uint)(~LuaInt(val));
		}

		public static uint Bor(object val, object amt)
		{
			return (uint)(LuaInt(val) | LuaInt(amt));
		}

		public static uint Bxor(object val, object amt)
		{
			return (uint)(LuaInt(val) ^ LuaInt(amt));
		}

		public static uint Lshift(object val, object amt)
		{
			return (uint)(LuaInt(val) << LuaInt(amt));
		}

		public static uint Rol(object val, object amt)
		{
			return (uint)((LuaInt(val) << LuaInt(amt))
				| (LuaInt(val) >> (32 - LuaInt(amt))));
		}

		public static uint Ror(object val, object amt)
		{
			return (uint)((LuaInt(val) >> LuaInt(amt))
				| (LuaInt(val) << (32 - LuaInt(amt))));
		}

		public static uint Rshift(object val, object amt)
		{
			return (uint)(LuaInt(val) >> LuaInt(amt));
		}

		public static bool Check(object num, object pos)
		{
			return (LuaLong(num) & (1 << LuaInt(pos))) != 0;
		}
		 
		public static uint Set(object num, object pos)
		{
			return (uint)(LuaInt(num) | 1 << LuaInt(pos));
		}

		public static uint Clear(object num, object pos)
		{
			return (uint)(LuaInt(num) & ~(1 << LuaInt(pos)));
		}

		public static uint Byteswap_16(object _short)
		{
			 return (UInt16)((LuaInt(_short) & 0xFFU) << 8 | (LuaInt(_short) & 0xFF00U) >> 8);
		}

		public static uint Byteswap_32(object _dword)
		{
			return (LuaUInt(_dword) & 0x000000FFU) << 24 | (LuaUInt(_dword) & 0x0000FF00U) << 8 |
				(LuaUInt(_dword) & 0x00FF0000U) >> 8 | (LuaUInt(_dword) & 0xFF000000U) >> 24;
		}

		public static UInt64 Byteswap_64(object _long)
		{
			UInt64 value = (UInt64)LuaLong(_long);
			return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
		 (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
		 (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
		 (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
		}

	}
}
