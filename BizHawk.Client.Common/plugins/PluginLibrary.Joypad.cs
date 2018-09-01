using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class JoypadPluginLibrary
	{
		public JoypadPluginLibrary() { }

		public Dictionary<string,dynamic> Get(int? controller = null)
		{
			var buttons = new Dictionary<string, dynamic>();
			var adaptor = Global.AutofireStickyXORAdapter;
			foreach (var button in adaptor.Source.Definition.BoolButtons)
			{
				if (!controller.HasValue)
				{
					buttons[button] = adaptor.IsPressed(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + controller)
				{
					buttons[button.Substring(3)] = adaptor.IsPressed("P" + controller + " " + button.Substring(3));
				}
			}

			foreach (var button in adaptor.Source.Definition.FloatControls)
			{
				if (controller == null)
				{
					buttons[button] = adaptor.GetFloat(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + controller)
				{
					buttons[button.Substring(3)] = adaptor.GetFloat("P" + controller + " " + button.Substring(3));
				}
			}

			buttons["clear"] = null;
			buttons["getluafunctionslist"] = null;
			buttons["output"] = null;

			return buttons;
		}

		// TODO: what about float controls?
		public Dictionary<string, dynamic> GetImmediate()
		{
			var buttons = new Dictionary<string, dynamic>();
			var adaptor = Global.ActiveController;
			foreach (var button in adaptor.Definition.BoolButtons)
			{
				buttons[button] = adaptor.IsPressed(button);
			}
			foreach (var button in adaptor.Definition.FloatControls)
			{
				buttons[button] = adaptor.GetFloat(button);
			}

			return buttons;
		}

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
				Console.WriteLine("invalid mnemonic string: " + inputLogEntry);
			}
		}

		[LuaMethodExample("joypad.set( { [\"Left\"] = true, [ \"A\" ] = true, [ \"B\" ] = true } );")]
		[LuaMethod("set", "sets the given buttons to their provided values for the current frame")]
		public void Set(Dictionary<string,bool> buttons, int? controller = null)
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
						toPress = "P" + controller + " " + button;
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
		public void SetAnalog(Dictionary<string,float> controls, object controller = null)
		{
			try
			{
				foreach (var name in controls.Keys)
				{
					var theValueStr = controls[name].ToString();
					float? theValue = null;

					if (!string.IsNullOrWhiteSpace(theValueStr))
					{
						float f;
						if (float.TryParse(theValueStr, out f))
						{
							theValue = f;
						}
					}

					if (controller == null)
					{
						Global.StickyXORAdapter.SetFloat(name.ToString(), theValue);
					}
					else
					{
						Global.StickyXORAdapter.SetFloat("P" + controller + " " + name, theValue);
					}
				}
			}
			catch
			{
				/*Eat it*/
			}
		}
	}
}
