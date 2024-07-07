namespace NLua.Native
{
	/// <summary>
	/// Lua types
	/// </summary>
	public enum LuaType
	{
		/// <summary>
		/// 
		/// </summary>
		None = -1,
		/// <summary>
		/// LUA_TNIL
		/// </summary>
		Nil = 0,
		/// <summary>
		/// LUA_TBOOLEAN
		/// </summary>
		Boolean = 1,
		/// <summary>
		/// LUA_TLIGHTUSERDATA
		/// </summary>
		LightUserData = 2,
		/// <summary>
		/// LUA_TNUMBER
		/// </summary>
		Number = 3,
		/// <summary>
		/// LUA_TSTRING
		/// </summary>
		String = 4,
		/// <summary>
		/// LUA_TTABLE
		/// </summary>
		Table = 5,
		/// <summary>
		/// LUA_TFUNCTION
		/// </summary>
		Function = 6,
		/// <summary>
		/// LUA_TUSERDATA
		/// </summary>
		UserData = 7,
		/// <summary>
		/// LUA_TTHREAD
		/// </summary>
		/// //
		Thread = 8,
	}
}
