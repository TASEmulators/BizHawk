using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient
{
	class NESGamePad : GamepadConfigPanel
	{
		public NESGamePad()
		{
			buttons = new List<string> { "Up", "Down", "Left", "Right", "A", "B", "Select", "Start" };
		}

		public override void Save()
		{
			for (int button = 0; button < 8; button++)
			{

			}
		}
	}
}
