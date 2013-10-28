namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		public uint bit_band(object val, object amt)
		{
			return (uint)(LuaInt(val) & LuaInt(amt));
		}

		public uint bit_bnot(object val)
		{
			return (uint)(~LuaInt(val));
		}

		public uint bit_bor(object val, object amt)
		{
			return (uint)(LuaInt(val) | LuaInt(amt));
		}

		public uint bit_bxor(object val, object amt)
		{
			return (uint)(LuaInt(val) ^ LuaInt(amt));
		}

		public uint bit_lshift(object val, object amt)
		{
			return (uint)(LuaInt(val) << LuaInt(amt));
		}

		public uint bit_rol(object val, object amt)
		{
			return (uint)((LuaInt(val) << LuaInt(amt)) | (LuaInt(val) >> (32 - LuaInt(amt))));
		}

		public uint bit_ror(object val, object amt)
		{
			return (uint)((LuaInt(val) >> LuaInt(amt)) | (LuaInt(val) << (32 - LuaInt(amt))));
		}

		public uint bit_rshift(object val, object amt)
		{
			return (uint)(LuaInt(val) >> LuaInt(amt));
		}
	}
}
