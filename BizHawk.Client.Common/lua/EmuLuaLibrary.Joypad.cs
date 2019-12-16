using System;
using System.Collections.Generic;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	public sealed class JoypadLuaLibrary : DelegatingLuaLibrary
	{
		public JoypadLuaLibrary(Lua lua)
			: base(lua) { }

		public JoypadLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "joypad";

		[LuaMethodExample("local nljoyget = joypad.get( 1 );")]
		[LuaMethod("get", "returns a lua table of the controller buttons pressed. If supplied, it will only return a table of buttons for the given controller")]
		public LuaTable Get(int? controller = null)
		{
			var table = APIs.Joypad.Get(controller).ToLuaTable(Lua);
			table["clear"] = null;
			table["getluafunctionslist"] = null;
			table["output"] = null;
			return table;
		}

		[LuaMethodExample("local nljoyget = joypad.getimmediate( );")]
		[LuaMethod("getimmediate", "returns a lua table of any controller buttons currently pressed by the user")]
		public LuaTable GetImmediate()
		{
			return APIs.Joypad
				.GetImmediate()
				.ToLuaTable(Lua);
		}

		[LuaMethodExample("joypad.setfrommnemonicstr( \"|    0,    0,    0,  100,...R..B....|\" );")]
		[LuaMethod("setfrommnemonicstr", "sets the given buttons to their provided values for the current frame, string will be interpretted the same way an entry from a movie input log would be")]
		public void SetFromMnemonicStr(string inputLogEntry) => APIs.Joypad.SetFromMnemonicStr(inputLogEntry);

		[LuaMethodExample("joypad.set( { [\"Left\"] = true, [ \"A\" ] = true, [ \"B\" ] = true } );")]
		[LuaMethod("set", "sets the given buttons to their provided values for the current frame")]
		public void Set(LuaTable buttons, int? controller = null)
		{
			var dict = new Dictionary<string, bool>();
			foreach (var k in buttons.Keys) dict[k.ToString()] = (bool) buttons[k];
			APIs.Joypad.Set(dict, controller);
		}

		[LuaMethodExample("joypad.setanalog( { [ \"Tilt X\" ] = true, [ \"Tilt Y\" ] = false } );")]
		[LuaMethod("setanalog", "sets the given analog controls to their provided values for the current frame. Note that unlike set() there is only the logic of overriding with the given value.")]
		public void SetAnalog(LuaTable controls, object controller = null)
		{
			var dict = new Dictionary<string, float>();
			foreach (var k in controls.Keys) dict[k.ToString()] = (float) controls[k];
			APIs.Joypad.SetAnalog(dict, controller);
		}
	}
}
