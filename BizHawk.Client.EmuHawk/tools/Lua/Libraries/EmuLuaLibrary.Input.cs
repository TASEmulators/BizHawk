using System;

using NLua;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class InputLuaLibrary : DelegatingLuaLibraryEmu
	{
		public InputLuaLibrary(Lua lua)
			: base(lua) { }

		public InputLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "input";

		[LuaMethodExample("local nlinpget = input.get( );")]
		[LuaMethod("get", "Returns a lua table of all the buttons the user is currently pressing on their keyboard and gamepads\nAll buttons that are pressed have their key values set to true; all others remain nil.")]
		public LuaTable Get()
		{
			var result = APIs.Input.Get();
			var table = Lua.NewTable();
			foreach (var kvp in result) table[kvp.Key] = kvp.Value;
			return table;
		}

		[LuaMethodExample("local nlinpget = input.getmouse( );")]
		[LuaMethod("getmouse", "Returns a lua table of the mouse X/Y coordinates and button states. Table keys are X, Y, Left, Middle, Right, XButton1, XButton2, Wheel.")]
		public LuaTable GetMouse()
		{
			var result = APIs.Input.GetMouse();
			var table = Lua.NewTable();
			foreach (var kvp in result) table[kvp.Key] = kvp.Value;
			return table;
		}
	}
}
