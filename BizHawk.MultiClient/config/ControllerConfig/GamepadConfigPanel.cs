using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient
{
	class GamepadConfigPanel : Panel
	{
		public static List<string> buttons = new List<string>();
		public int ControllerNumber = 1;
		public bool Autofire = false;

		public int InputMarginLeft = 0;
		public int LabelPadding = 20;

		public int MarginTop = 0;
		public int Spacing = 30;
		public int InputSize = 200;

		public GamepadConfigPanel()
		{
			Size = new Size(174, 74);
			this.BorderStyle = BorderStyle.None;
			Startup();
		}

		public void Startup()
		{
			for (int i = 0; i < buttons.Count; i++)
			{
				int pos = i + 1;

				InputWidget iw = new InputWidget();
				iw.Location = new Point(InputMarginLeft, MarginTop + (pos * Spacing));
				iw.Size = new Size(InputSize, 23);
				Controls.Add(iw);

				Label l = new Label();
				l.Location = new Point(InputMarginLeft + InputSize + LabelPadding, MarginTop + (pos * Spacing) + 3);
				l.Text = buttons[i];
				Controls.Add(l);
			}
		}

		public virtual void Save()
		{
			for (int button = 0; button < 8; button++)
			{

			}
		}
	}
}
