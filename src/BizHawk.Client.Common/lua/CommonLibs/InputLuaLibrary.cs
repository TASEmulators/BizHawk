using System;

using NLua;

namespace BizHawk.Client.Common
{
	public sealed class InputLuaLibrary : LuaLibraryBase
	{
		public InputLuaLibrary(IPlatformLuaLibEnv luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "input";

		[LuaMethodExample("local nlinpget = input.get( );")]
		[LuaMethod("get", "Returns a lua table of all the buttons the user is currently pressing on their keyboard and gamepads\nAll buttons that are pressed have their key values set to true; all others remain nil.")]
		public LuaTable Get() => _th.DictToTable(APIs.Input.Get());

		[LuaMethodExample("local nlinpget = input.getmouse( );")]
		[LuaMethod("getmouse", "Returns a lua table of the mouse X/Y coordinates and button states. Table keys are X, Y, Left, Middle, Right, XButton1, XButton2, Wheel.")]
		public LuaTable GetMouse() => _th.DictToTable(APIs.Input.GetMouse());
	}
}
