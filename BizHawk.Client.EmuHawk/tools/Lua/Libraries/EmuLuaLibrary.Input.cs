using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using LuaInterface;

namespace BizHawk.Client.EmuHawk
{
	public sealed class InputLuaLibrary : LuaLibraryBase
	{
		public InputLuaLibrary(Lua lua)
			: base(lua) { }

		public InputLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "input"; } }

		[LuaMethodAttributes(
			"get",
			"Returns a lua table of all the buttons the user is currently pressing on their keyboard and gamepads\nAll buttons that are pressed have their key values set to true; all others remain nil."
		)]
		public LuaTable Get()
		{
			var buttons = Lua.NewTable();
			foreach (var kvp in Global.ControllerInputCoalescer.BoolButtons().Where(kvp => kvp.Value))
			{
				buttons[kvp.Key] = true;
			}

			return buttons;
		}

		[LuaMethodAttributes(
			"getmouse",
			"Returns a lua table of the mouse X/Y coordinates and button states. Table keys are X, Y, Left, Middle, Right, XButton1, XButton2"
		)]
		public LuaTable GetMouse()
		{
			var buttons = Lua.NewTable();
			//TODO - need to specify whether in "emu" or "native" coordinate space.
			var p = GlobalWin.DisplayManager.UntransformPoint(Control.MousePosition);
			buttons["X"] = p.X;
			buttons["Y"] = p.Y;
			buttons[MouseButtons.Left.ToString()] = (Control.MouseButtons & MouseButtons.Left) != 0;
			buttons[MouseButtons.Middle.ToString()] = (Control.MouseButtons & MouseButtons.Middle) != 0;
			buttons[MouseButtons.Right.ToString()] = (Control.MouseButtons & MouseButtons.Right) != 0;
			buttons[MouseButtons.XButton1.ToString()] = (Control.MouseButtons & MouseButtons.XButton1) != 0;
			buttons[MouseButtons.XButton2.ToString()] = (Control.MouseButtons & MouseButtons.XButton2) != 0;
			return buttons;
		}
	}
}
