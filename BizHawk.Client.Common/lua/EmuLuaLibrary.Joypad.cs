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
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"get",
					"getimmediate",
					"set",
					"setanalog"
				};
			}
		}

		private readonly Lua _lua;

		[LuaMethodAttributes(
			"get",
			"TODO"
		)]
		public LuaTable Get(object controller = null)
		{
			var buttons = _lua.NewTable();
			foreach (var button in Global.ControllerOutput.Source.Type.BoolButtons)
			{
				if (controller == null)
				{
					buttons[button] = Global.ControllerOutput[button];
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + LuaInt(controller))
				{
					buttons[button.Substring(3)] = Global.ControllerOutput["P" + LuaInt(controller) + " " + button.Substring(3)];
				}
			}

			foreach (var button in Global.ControllerOutput.Source.Type.FloatControls)
			{
				if (controller == null)
				{
					buttons[button] = Global.ControllerOutput.GetFloat(button);
				}
				else if (button.Length >= 3 && button.Substring(0, 2) == "P" + LuaInt(controller))
				{
					buttons[button.Substring(3)] = Global.ControllerOutput.GetFloat("P" + LuaInt(controller) + " " + button.Substring(3));
				}
			}

			buttons["clear"] = null;
			buttons["getluafunctionslist"] = null;
			buttons["output"] = null;

			return buttons;
		}

		[LuaMethodAttributes(
			"getimmediate",
			"TODO"
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
			"set",
			"TODO"
		)]
		public void Set(LuaTable buttons, object controller = null)
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

					if (!invert)
					{
						if (theValue == true)
						{
							if (controller == null) // Force On
							{
								Global.ClickyVirtualPadController.Click(button.ToString());
								Global.ForceOffAdaptor.SetSticky(button.ToString(), false);
							}
							else
							{
								Global.ClickyVirtualPadController.Click("P" + controller + " " + button);
								Global.ForceOffAdaptor.SetSticky("P" + controller + " " + button, false);
							}
						}
						else if (theValue == false) // Force off
						{
							if (controller == null)
							{
								Global.ForceOffAdaptor.SetSticky(button.ToString(), true);
							}
							else
							{
								Global.ForceOffAdaptor.SetSticky("P" + controller + " " + button, true);
							}
						}
						else
						{
							// Turn everything off
							if (controller == null)
							{
								Global.ForceOffAdaptor.SetSticky(button.ToString(), false);
							}
							else
							{
								Global.ForceOffAdaptor.SetSticky("P" + controller + " " + button, false);
							}
						}
					}
					else // Inverse
					{
						if (controller == null)
						{
							Global.StickyXORAdapter.SetSticky(button.ToString(), true);
							Global.ForceOffAdaptor.SetSticky(button.ToString(), false);
						}
						else
						{
							Global.StickyXORAdapter.SetSticky("P" + controller + " " + button, true);
							Global.ForceOffAdaptor.SetSticky("P" + controller + " " + button, false);
						}
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
			"TODO"
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
