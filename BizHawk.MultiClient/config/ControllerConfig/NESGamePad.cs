using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient.config.ControllerConfig
{
	class NESGamePad : Panel
	{
		InputWidget UpBox = new InputWidget();
		public int ControllerNumber = 1;
		public bool Autofire = false;

		public NESGamePad()
		{
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Size = new Size(174, 74);
			ControllerNumber = 1;

			UpBox.Location = new Point(15, 15);
		}


	}
}
