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
			var value = Convert.ToInt64(LuaLong(num));
			return (value & (1 << (int)(LuaInt(pos)))) != 0;
		}
	}
}
