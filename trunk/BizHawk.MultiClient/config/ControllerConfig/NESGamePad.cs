using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Reflection;

namespace BizHawk.MultiClient
{
	class NESGamePad : GamepadConfigPanel
	{
		public NESGamePad()
		{
			buttons = new List<string> { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" };
			Startup();
		}

		public override void Save()
		{
			for (int button = 0; button < 8; button++)
			{
				NESControllerTemplate o = Global.Config.NESController[ControllerNumber - 1];
				FieldInfo buttonF = o.GetType().GetField(buttons[button]);
				buttonF.SetValue(o, Inputs[button].Text);
			}
		}

		public void Load()
		{
			for (int button = 0; button < 8; button++)
			{
				NESControllerTemplate o = Global.Config.NESController[ControllerNumber - 1];
				FieldInfo buttonF = o.GetType().GetField(buttons[button]);
				object field = o.GetType().GetField(buttons[button]).GetValue(o);
				Inputs[button].Text = field.ToString();
			}
		}
	}
}
