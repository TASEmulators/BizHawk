using System;
using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	public sealed class JoypadLuaLibrary : LuaLibraryBase
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
			var buttons = Lua.NewTable();
			var adapter = Global.AutofireStickyXORAdapter;
			foreach (var button in adapter.Source.Definition.BoolButtons)
			{
				if (!controller.HasValue)
				{
					buttons[button] = adapter.IsPressed(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == $"P{controller}")
				{
					buttons[button.Substring(3)] = adapter.IsPressed($"P{controller} {button.Substring(3)}");
				}
			}

			foreach (var button in adapter.Source.Definition.FloatControls)
			{
				if (controller == null)
				{
					buttons[button] = adapter.GetFloat(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == $"P{controller}")
				{
					buttons[button.Substring(3)] = adapter.GetFloat($"P{controller} {button.Substring(3)}");
				}
			}

			buttons["clear"] = null;
			buttons["getluafunctionslist"] = null;
			buttons["output"] = null;

			return buttons;
		}

		// TODO: what about float controls?
		[LuaMethodExample("local nljoyget = joypad.getimmediate( );")]
		[LuaMethod("getimmediate", "returns a lua table of any controller buttons currently pressed by the user")]
		public LuaTable GetImmediate()
		{
			var buttons = Lua.NewTable();
			foreach (var button in Global.ActiveController.Definition.BoolButtons)
			{
				buttons[button] = Global.ActiveController.IsPressed(button);
			}

			return buttons;
		}

		[LuaMethodExample("joypad.setfrommnemonicstr( \"|    0,    0,    0,  100,...R..B....|\" );")]
		[LuaMethod("setfrommnemonicstr", "sets the given buttons to their provided values for the current frame, string will be interpretted the same way an entry from a movie input log would be")]
		public void SetFromMnemonicStr(string inputLogEntry)
		{
			try
			{
				var lg = Global.MovieSession.MovieControllerInstance();
				lg.SetControllersAsMnemonic(inputLogEntry);

				foreach (var button in lg.Definition.BoolButtons)
				{
					Global.LuaAndAdaptor.SetButton(button, lg.IsPressed(button));
				}

				foreach (var floatButton in lg.Definition.FloatControls)
				{
					Global.LuaAndAdaptor.SetFloat(floatButton, lg.GetFloat(floatButton));
				}
			}
			catch (Exception)
			{
				Log($"invalid mnemonic string: {inputLogEntry}");
			}
		}

		[LuaMethodExample("joypad.set( { [\"Left\"] = true, [ \"A\" ] = true, [ \"B\" ] = true } );")]
		[LuaMethod("set", "sets the given buttons to their provided values for the current frame")]
		public void Set(LuaTable buttons, int? controller = null)
		{
			try
			{
				foreach (var button in buttons.Keys)
				{
					var invert = false;
					bool? theValue;
					var theValueStr = buttons[button].ToString();

					if (!string.IsNullOrWhiteSpace(theValueStr))
					{
						if (theValueStr.ToLower() == "false")
						{
							theValue = false;
						}
						else if (theValueStr.ToLower() == "true")
						{
							theValue = true;
						}
						else
						{
							invert = true;
							theValue = null;
						}
					}
					else
					{
						theValue = null;
					}

					var toPress = button.ToString();
					if (controller.HasValue)
					{
						toPress = $"P{controller} {button}";
					}

					if (!invert)
					{
						if (theValue.HasValue) // Force
						{
							Global.LuaAndAdaptor.SetButton(toPress, theValue.Value);
							Global.ActiveController.Overrides(Global.LuaAndAdaptor);
						}
						else // Unset
						{
							Global.LuaAndAdaptor.UnSet(toPress);
							Global.ActiveController.Overrides(Global.LuaAndAdaptor);
						}
					}
					else // Inverse
					{
						Global.LuaAndAdaptor.SetInverse(toPress);
						Global.ActiveController.Overrides(Global.LuaAndAdaptor);
					}
				}
			}
			catch
			{
				/*Eat it*/
			}
		}

		[LuaMethodExample("joypad.setanalog( { [ \"Tilt X\" ] = true, [ \"Tilt Y\" ] = false } );")]
		[LuaMethod("setanalog", "sets the given analog controls to their provided values for the current frame. Note that unlike set() there is only the logic of overriding with the given value.")]
		public void SetAnalog(LuaTable controls, object controller = null)
		{
			try
			{
				foreach (var name in controls.Keys)
				{
					var theValueStr = controls[name].ToString();
					float? theValue = null;

					if (!string.IsNullOrWhiteSpace(theValueStr))
					{
						if (float.TryParse(theValueStr, out var f))
						{
							theValue = f;
						}
					}

					Global.StickyXORAdapter.SetFloat(controller == null ? name.ToString() : $"P{controller} {name}", theValue);
				}
			}
			catch
			{
				/*Eat it*/
			}
		}
	}
}
