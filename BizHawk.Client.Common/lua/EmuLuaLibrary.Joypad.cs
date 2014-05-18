using System;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public class JoypadLuaLibrary : LuaLibraryBase
	{
		public JoypadLuaLibrary(Lua lua)
		{
			_lua = lua;
		}

		public override string Name { get { return "joypad"; } }

		private readonly Lua _lua;

		[LuaMethodAttributes(
			"get",
			"returns a lua table of the controller buttons pressed. If supplied, it will only return a table of buttons for the given controller"
		)]
		public LuaTable Get(int? controller = null)
		{
			var buttons = _lua.NewTable();
			foreach (var button in Global.ControllerOutput.Source.Type.BoolButtons)
			{
				if (!controller.HasValue)
				{
					buttons[button] = Global.ControllerOutput[button];
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + controller)
				{
					buttons[button.Substring(3)] = Global.ControllerOutput["P" + controller + " " + button.Substring(3)];
				}
			}

			foreach (var button in Global.ControllerOutput.Source.Type.FloatControls)
			{
				if (controller == null)
				{
					buttons[button] = Global.ControllerOutput.GetFloat(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + controller)
				{
					buttons[button.Substring(3)] = Global.ControllerOutput.GetFloat("P" + controller + " " + button.Substring(3));
				}
			}

			buttons["clear"] = null;
			buttons["getluafunctionslist"] = null;
			buttons["output"] = null;

			return buttons;
		}

		[LuaMethodAttributes(
			"getimmediate",
			"returns a lua table of any controller buttons currently pressed by the user"
		)]
		public LuaTable GetImmediate()
		{
			var buttons = _lua.NewTable();
			foreach (var button in Global.ActiveController.Type.BoolButtons)
			{
				buttons[button] = Global.ActiveController[button];
			}

			return buttons;
		}

		[LuaMethodAttributes(
			"setfrommnemonicstr",
			"sets the given buttons to their provided values for the current frame, string will be interpretted the same way an entry from a movie input log would be"
		)]
		public void SetFromMnemonicStr(string inputLogEntry)
		{
			var m = new MovieControllerAdapter { Type = Global.MovieSession.MovieControllerAdapter.Type };
			m.SetControllersAsMnemonic(inputLogEntry);

			foreach (var button in m.Type.BoolButtons)
			{
				Global.LuaAndAdaptor.SetButton(button, m.IsPressed(button));
			}

			foreach (var floatButton in m.Type.FloatControls)
			{
				Global.StickyXORAdapter.SetFloat(floatButton, m.GetFloat(floatButton));
			}
		}

		[LuaMethodAttributes(
			"set",
			"sets the given buttons to their provided values for the current frame"
		)]
		public void Set(LuaTable buttons, int? controller = null)
		{
			try
			{
				foreach (var button in buttons.Keys)
				{
					var invert = false;
					bool? theValue;
					var theValueStr = buttons[button].ToString();

					if (!String.IsNullOrWhiteSpace(theValueStr))
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
						toPress = "P" + controller + " " + button;
					}

					if (!invert)
					{
						if (theValue.HasValue) // Force
						{
							Global.LuaAndAdaptor.SetButton(toPress, theValue.Value);
						}
						else // Unset
						{
							Global.LuaAndAdaptor.UnSet(toPress);
						}
					}
					else // Inverse
					{
						Global.LuaAndAdaptor.SetInverse(toPress);
					}
				}
			}
			catch
			{
				 /*Eat it*/
			}
		}

		[LuaMethodAttributes(
			"setanalog",
			"sets the given analog controls to their provided values for the current frame. Note that unlike set() there is only the logic of overriding with the given value."
		)]
		public void SetAnalog(LuaTable controls, object controller = null)
		{
			try
			{
				foreach (var name in controls.Keys)
				{
					var theValueStr = controls[name].ToString();

					if (!String.IsNullOrWhiteSpace(theValueStr))
					{
						try
						{
							var theValue = float.Parse(theValueStr);
							if (controller == null)
							{
								Global.StickyXORAdapter.SetFloat(name.ToString(), theValue);
							}
							else
							{
								Global.StickyXORAdapter.SetFloat("P" + controller + " " + name, theValue);
							}
						}
						catch { }
					}
				}
			}
			catch { /*Eat it*/ }
		}
	}
}
