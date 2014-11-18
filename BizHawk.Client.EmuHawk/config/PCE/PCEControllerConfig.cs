using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCEControllerConfig : Form
	{
		public PCEControllerConfig()
		{
			InitializeComponent();
		}

		private void PCEControllerConfig_Load(object sender, EventArgs e)
		{
			var pceSettings = ((PCEngine)Global.Emulator).GetSyncSettings();
			for (int i = 0; i < 5; i++)
			{
				Controls.Add(new Label
				{
					Text = "Controller " + (i + 1),
					Location = new Point(15, 15 + (i * 25))
				});
				Controls.Add(new CheckBox
				{
					Text = "Connected",
					Name = "Controller" + i,
					Location = new Point(135, 15 + (i * 25)),
					Checked = pceSettings.Controllers[i].IsConnected
				});
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var pceSettings = ((PCEngine)Global.Emulator).GetSyncSettings();

			Controls
				.OfType<CheckBox>()
				.OrderBy(c => c.Name)
				.ToList()
				.ForEach(c => {
					var index = int.Parse(c.Name.Replace("Controller", ""));
					pceSettings.Controllers[index].IsConnected = c.Checked;
				});
			GlobalWin.MainForm.PutCoreSyncSettings(pceSettings);
			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
