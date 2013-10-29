using System;

namespace BizHawk.MultiClient
{
	public static class BitLuaLibrary
	{
		public static string Name = "bit";
		public static string[] Functions = new[]
		{
			"band",
			"bnot",
			"bor",
			"bxor",
			"lshift",
			"rol",
			"ror",
			"rshift",
		};

		public static uint bit_band(object val, object amt)
		{
			return (uint)(LuaCommon.LuaInt(val) & LuaCommon.LuaInt(amt));
		}

		public static uint bit_bnot(object val)
		{
			return (uint)(~LuaCommon.LuaInt(val));
		}

		public static uint bit_bor(object val, object amt)
		{
			return (uint)(LuaCommon.LuaInt(val) | LuaCommon.LuaInt(amt));
		}

		public static uint bit_bxor(object val, object amt)
		{
			return (uint)(LuaCommon.LuaInt(val) ^ LuaCommon.LuaInt(amt));
		}

		public static uint bit_lshift(object val, object amt)
		{
			return (uint)(LuaCommon.LuaInt(val) << LuaCommon.LuaInt(amt));
		}

		public static uint bit_rol(object val, object amt)
		{
			return (uint)((LuaCommon.LuaInt(val) << LuaCommon.LuaInt(amt)) 
				| (LuaCommon.LuaInt(val) >> (32 - LuaCommon.LuaInt(amt))));
		}

		public static uint bit_ror(object val, object amt)
		{
			return (uint)((LuaCommon.LuaInt(val) >> LuaCommon.LuaInt(amt))
				| (LuaCommon.LuaInt(val) << (32 - LuaCommon.LuaInt(amt))));
		}

		public static uint bit_rshift(object val, object amt)
		{
			return (uint)(LuaCommon.LuaInt(val) >> LuaCommon.LuaInt(amt));
		}
	}
}
