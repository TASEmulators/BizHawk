using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class InputApi : IInput
	{
		public Dictionary<string, bool> Get()
		{
			var buttons = new Dictionary<string, bool>();
			foreach (var kvp in Global.InputManager.ControllerInputCoalescer.BoolButtons().Where(kvp => kvp.Value)) buttons[kvp.Key] = true;
			return buttons;
		}

		public Dictionary<string, object> GetMouse()
		{
			var buttons = new Dictionary<string, object>();
			// TODO - need to specify whether in "emu" or "native" coordinate space.
			var p = GlobalWin.DisplayManager.UntransformPoint(Control.MousePosition);
			buttons["X"] = p.X;
			buttons["Y"] = p.Y;
			buttons[MouseButtons.Left.ToString()] = (Control.MouseButtons & MouseButtons.Left) != 0;
			buttons[MouseButtons.Middle.ToString()] = (Control.MouseButtons & MouseButtons.Middle) != 0;
			buttons[MouseButtons.Right.ToString()] = (Control.MouseButtons & MouseButtons.Right) != 0;
			buttons[MouseButtons.XButton1.ToString()] = (Control.MouseButtons & MouseButtons.XButton1) != 0;
			buttons[MouseButtons.XButton2.ToString()] = (Control.MouseButtons & MouseButtons.XButton2) != 0;
			buttons["Wheel"] = GlobalWin.MainForm.MouseWheelTracker;
			return buttons;
		}
	}
}
