using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DefaultGreenzoneSettings : Form
	{
		TasStateManagerSettings settings;

		public DefaultGreenzoneSettings()
		{
			InitializeComponent();

			settings = new TasStateManagerSettings(Global.Config.DefaultTasProjSettings);

			SettingsPropertyGrid.SelectedObject = settings;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			Global.Config.DefaultTasProjSettings = settings;
			this.Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void DefaultsButton_Click(object sender, EventArgs e)
		{
			settings = new TasStateManagerSettings();
			SettingsPropertyGrid.SelectedObject = settings;
		}
	}
}
