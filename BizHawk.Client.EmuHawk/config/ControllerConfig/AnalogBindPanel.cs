using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class AnalogBindPanel : UserControl
	{
		private readonly Dictionary<string, Config.AnalogBind> _realConfigObject;

		public AnalogBindPanel(Dictionary<string, Config.AnalogBind> realConfigObject, List<string> realConfigButtons = null)
		{
			_realConfigObject = realConfigObject;
			LoadSettings(realConfigButtons ?? (IEnumerable<string>)realConfigObject.Keys);
		}

		private void LoadSettings(IEnumerable<string> buttonList)
		{
			SuspendLayout();
			int x = 4;
			int y = 4;
			foreach (string buttonName in buttonList)
			{
				var ctrl = new AnalogBindControl(buttonName, _realConfigObject[buttonName])
				{
					Location = new Point(x, y)
				};

				y += ctrl.Height + 4;
				Controls.Add(ctrl);
			}

			ResumeLayout();
		}

		/// <summary>
		/// save to config
		/// </summary>
		/// <param name="saveConfigObject">if non-null, save to possibly different config object than originally initialized from</param>
		public void Save(Dictionary<string, Config.AnalogBind> saveConfigObject = null)
		{
			var saveTo = saveConfigObject ?? _realConfigObject;
			foreach (Control c in Controls)
			{
				var abc = (AnalogBindControl)c;
				saveTo[abc.ButtonName] = abc.Bind;
			}
		}
	}
}
