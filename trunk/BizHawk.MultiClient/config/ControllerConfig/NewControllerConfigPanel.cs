using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient.config.ControllerConfig
{
	class NewControllerConfigPanel : ControllerConfigPanel
	{
		Dictionary<string, string> RealConfigObject;

		public override void Save()
		{
			for (int button = 0; button < buttons.Count; button++)
				RealConfigObject[buttons[button]] = Inputs[button].Text;
		}

		public void LoadSettings(Dictionary<string, string> configobj)
		{
			RealConfigObject = configobj;
			SetButtonList();
			Startup();
			SetWidgetStrings();
		}

		public override void LoadSettings(iControllerConfigObject configobj)
		{
			throw new InvalidOperationException();
		}

		protected override void SetButtonList()
		{
			buttons.Clear();
			foreach (string s in RealConfigObject.Keys)
				buttons.Add(s);
		}

		protected override void SetWidgetStrings()
		{
			for (int button = 0; button < buttons.Count; button++)
			{
				string s;
				if (!RealConfigObject.TryGetValue(buttons[button], out s))
					s = "";
				Inputs[button].SetBindings(s);
			}
		}

		protected override void restoreDefaultsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// this is a TODO: we have no concept of default values in our config system at the moment
			// so for the moment, "defaults" = "no binds at all"
			RealConfigObject.Clear();
			SetWidgetStrings();
		}
	}
}
