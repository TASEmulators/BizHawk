using System.Collections.Generic;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	public sealed class JoypadLuaLibrary : LuaLibraryBase
	{
		public JoypadLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "joypad";

		[LuaMethodExample("local nljoyget = joypad.get( 1 );")]
		[LuaMethod("get", "returns a lua table of the controller buttons pressed. If supplied, it will only return a table of buttons for the given controller")]
		public LuaTable Get(int? controller = null)
			=> _th.DictToTable(APIs.Joypad.Get(controller));

		[LuaMethodExample("local nljoyget = joypad.getwithmovie( 1 );")]
		[LuaMethod("getwithmovie", "returns a lua table of the controller buttons pressed, including ones pressed by the current movie. If supplied, it will only return a table of buttons for the given controller")]
		public LuaTable GetWithMovie(int? controller = null)
			=> _th.DictToTable(APIs.Joypad.GetWithMovie(controller));

		[LuaMethodExample("local nljoyget = joypad.getimmediate( );")]
		[LuaMethod("getimmediate", "returns a lua table of any controller buttons currently pressed by the user")]
		public LuaTable GetImmediate(int? controller = null)
			=> _th.DictToTable(APIs.Joypad.GetImmediate(controller));

		[LuaMethodExample("joypad.setfrommnemonicstr( \"|    0,    0,    0,  100,...R..B....|\" );")]
		[LuaMethod("setfrommnemonicstr", "sets the given buttons to their provided values for the current frame, string will be interpreted the same way an entry from a movie input log would be")]
		public void SetFromMnemonicStr(string inputLogEntry)
			=> APIs.Joypad.SetFromMnemonicStr(inputLogEntry);

		[LuaMethodExample("joypad.set( { [\"Left\"] = true, [ \"A\" ] = true, [ \"B\" ] = true } );")]
		[LuaMethod("set", "sets the given buttons to their provided values for the current frame")]
		public void Set(LuaTable buttons, int? controller = null)
		{
			var dict = new Dictionary<string, bool>();
			foreach (var (k, v) in _th.EnumerateEntries<object, object>(buttons))
			{
				dict[k.ToString()] = Convert.ToBoolean(v); // Accepts 1/0 or true/false
			}
			APIs.Joypad.Set(dict, controller);
		}

		[LuaMethodExample("joypad.setanalog( { [ \"Tilt X\" ] = -63, [ \"Tilt Y\" ] = 127 } );")]
		[LuaMethod("setanalog", "sets the given analog controls to their provided values for the current frame. Note that unlike set() there is only the logic of overriding with the given value.")]
		public void SetAnalog(LuaTable controls, int? controller = null)
		{
			var dict = new Dictionary<string, int?>();
			foreach (var (k, v) in _th.EnumerateEntries<object, object>(controls))
			{
				dict[k.ToString()] = long.TryParse(v.ToString(), out var d) ? (int) d : null;
			}
			APIs.Joypad.SetAnalog(dict, controller);
		}
	}
}
