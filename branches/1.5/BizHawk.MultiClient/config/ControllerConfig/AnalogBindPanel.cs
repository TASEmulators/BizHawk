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
				Controls.Add(ctrl);
			}
			ResumeLayout();
		}

		/// <summary>
		/// save to config
		/// </summary>
		/// <param name="SaveConfigObject">if non-null, save to possibly different config object than originally initialized from</param>
		public void Save(Dictionary<string, Config.AnalogBind> SaveConfigObject = null)
		{
			var saveto = SaveConfigObject ?? RealConfigObject;
			foreach (Control c in Controls)
			{
				var abc = (AnalogBindControl)c;
				saveto[abc.ButtonName] = abc.Bind;
			}
		}
	}
}
