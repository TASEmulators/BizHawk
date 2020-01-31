using System;

using NLua;

namespace BizHawk.Client.Common
{
	/// <summary>Extends <see cref="LuaLibraryBase"/> by including an <see cref="ApiContainerSubset"/> for the library to delegate its calls through. Some APIs may not be delegated.</summary>
	public abstract class DelegatingLuaLibrary : LuaLibraryBase
	{
		protected DelegatingLuaLibrary(Lua lua) : base(lua) {}

		protected DelegatingLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		public ApiContainerSubset APIs { protected get; set; }
	}
}
