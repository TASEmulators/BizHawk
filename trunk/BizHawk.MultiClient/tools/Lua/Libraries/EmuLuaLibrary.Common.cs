using System;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// Generic helper functions for Lua Libraries
	/// </summary>
	public static class LuaCommon
	{
		public static int LuaInt(object lua_arg)
		{
			return Convert.ToInt32((double)lua_arg);
		}

		public static uint LuaUInt(object lua_arg)
		{
			return Convert.ToUInt32((double)lua_arg);
		}
	}
}
