using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient.config.ControllerConfig
{
	class AnalogBindPanel : UserControl
	{
		Dictionary<string, Config.AnalogBind> RealConfigObject;

		public AnalogBindPanel(Dictionary<string, Config.AnalogBind> RealConfigObject, List<string> RealConfigButtons = null)
			:base()
		{
			this.RealConfigObject = RealConfigObject;

			LoadSettings(RealConfigButtons ?? (IEnumerable<string>)RealConfigObject.Keys);
		}

		void LoadSettings(IEnumerable<string> ButtonList)
		{
			SuspendLayout();
			int x = 4;
			int y = 4;
			foreach (string ButtonName in ButtonList)
			{
				var ctrl = new AnalogBindControl(ButtonName, RealConfigObject[ButtonName]);
				ctrl.Location = new Point(x, y);
				y += ctrl.Height + 4;
				if (Width < ctrl.Width + 8)
					Width = ctrl.Width + 8;
				Controls.Add(ctrl);
			}
			ResumeLayout();
		}
	}
}
