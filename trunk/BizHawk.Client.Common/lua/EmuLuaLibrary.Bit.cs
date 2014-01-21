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
					"band",
					"bnot",
					"bor",
					"bxor",
					"lshift",
					"rol",
					"ror",
					"rshift",
					"check",
					"set",
					"clear",
					"byteswap_16",
					"byteswap_32",
					"byteswap_64",
				};
			}
		}

		public static uint bit_band(object val, object amt)
		{
			return (uint)(LuaInt(val) & LuaInt(amt));
		}

		public static uint bit_bnot(object val)
		{
			return (uint)(~LuaInt(val));
		}

		public static uint bit_bor(object val, object amt)
		{
			return (uint)(LuaInt(val) | LuaInt(amt));
		}

		public static uint bit_bxor(object val, object amt)
		{
			return (uint)(LuaInt(val) ^ LuaInt(amt));
		}

		public static uint bit_lshift(object val, object amt)
		{
			return (uint)(LuaInt(val) << LuaInt(amt));
		}

		public static uint bit_rol(object val, object amt)
		{
			return (uint)((LuaInt(val) << LuaInt(amt))
				| (LuaInt(val) >> (32 - LuaInt(amt))));
		}

		public static uint bit_ror(object val, object amt)
		{
			return (uint)((LuaInt(val) >> LuaInt(amt))
				| (LuaInt(val) << (32 - LuaInt(amt))));
		}

		public static uint bit_rshift(object val, object amt)
		{
			return (uint)(LuaInt(val) >> LuaInt(amt));
		}

		public static bool bit_check(object num, object pos)
		{
			return (LuaLong(num) & (1 << (LuaInt(pos)))) != 0;
		}
		 
		public static uint bit_set(object num, object pos)
		{
			return (uint) (LuaInt(num) |  1 << LuaInt(pos));
		}

		public static uint bit_clear(object num, object pos)
		{
			return (uint) (LuaInt(num) & ~(1 << LuaInt(pos)));
		}

		public static uint bit_byteswap_16(object short_)
		{
			 return (UInt16)((LuaInt(short_) & 0xFFU) << 8 | (LuaInt(short_) & 0xFF00U) >> 8);
		}

		public static uint bit_byteswap_32(object word_)
		{
			return (LuaUInt(word_) & 0x000000FFU) << 24 | (LuaUInt(word_) & 0x0000FF00U) << 8 |
				(LuaUInt(word_) & 0x00FF0000U) >> 8 | (LuaUInt(word_) & 0xFF000000U) >> 24;
		}

		public static UInt64 bit_byteswap_64(object long_)
		{
			UInt64 value = (UInt64)LuaLong(long_);
			return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
		 (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
		 (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
		 (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
		}

	}
}
