using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Text;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		public LuaTable input_get()
		{
			LuaTable buttons = _lua.NewTable();
			foreach (var kvp in GlobalWinF.ControllerInputCoalescer.BoolButtons())
				if (kvp.Value)
					buttons[kvp.Key] = true;
			return buttons;
		}

		public LuaTable input_getmouse()
		{
			LuaTable buttons = _lua.NewTable();
			Point p = GlobalWinF.RenderPanel.ScreenToScreen(Control.MousePosition);
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
