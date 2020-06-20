using System;

using NLua;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>As <see cref="DelegatingLuaLibrary"/>, but also includes EmuHawk APIs via an <see cref="ApiContainer"/>.</summary>
	public abstract class DelegatingLuaLibraryEmu : DelegatingLuaLibrary
	{
		protected DelegatingLuaLibraryEmu(Lua lua) : base(lua) {}

		protected DelegatingLuaLibraryEmu(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback) {}

		public new ApiContainer APIs { protected get; set; }
	}
}
