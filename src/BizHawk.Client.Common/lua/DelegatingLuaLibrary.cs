using System;

using NLua;

namespace BizHawk.Client.Common
{
	/// <summary>Extends <see cref="LuaLibraryBase"/> by including an <see cref="ApiContainer"/> for the library to delegate its calls through.</summary>
	public abstract class DelegatingLuaLibrary : LuaLibraryBase
	{
		protected DelegatingLuaLibrary(Lua lua) : base(lua) {}

		protected DelegatingLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		public ApiContainer APIs { protected get; set; }
	}
}
