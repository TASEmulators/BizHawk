using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Client.Common;
using BizHawk.Common;


namespace BizHawk.Client.EmuHawk
{
	public partial class PSXControllerConfig : Form
	{
		public PSXControllerConfig()
		{
			InitializeComponent();
		}

		private void PSXControllerConfig_Load(object sender, EventArgs e)
		{
			var psxSettings = ((Octoshock)Global.Emulator).GetSyncSettings();
			for (int i = 0; i < psxSettings.Controllers.Length; i++)
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
					Checked = psxSettings.Controllers[i].IsConnected
				});
			}
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var psxSettings = ((Octoshock)Global.Emulator).GetSyncSettings();

			Controls
				.OfType<CheckBox>()
				.OrderBy(c => c.Name)
				.ToList()
				.ForEach(c =>
				{
					var index = int.Parse(c.Name.Replace("Controller", ""));
					psxSettings.Controllers[index].IsConnected = c.Checked;
				});
			GlobalWin.MainForm.PutCoreSyncSettings(psxSettings);
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
