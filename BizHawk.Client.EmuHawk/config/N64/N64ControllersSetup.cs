using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class N64ControllersSetup : Form
	{
		private List<N64ControllerSettingControl> ControllerSettingControls
		{
			get
			{
				return Controls
					.OfType<N64ControllerSettingControl>()
					.OrderBy(n => n.ControllerNumber)
					.ToList();
			}
		}

		public N64ControllersSetup()
		{
			InitializeComponent();
		}

		private void N64ControllersSetup_Load(object sender, EventArgs e)
		{
			var n64Settings = ((N64)Global.Emulator).GetSyncSettings();
			
			ControllerSettingControls
				.ForEach(c =>
				{
					c.IsConnected = n64Settings.Controllers[c.ControllerNumber - 1].IsConnected;
					c.PakType = n64Settings.Controllers[c.ControllerNumber - 1].PakType;
					c.Refresh();
				});
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var n64 = (N64)Global.Emulator;
			var n64Settings = n64.GetSyncSettings();
			
			ControllerSettingControls
				.ForEach(c =>
				{
					n64Settings.Controllers[c.ControllerNumber - 1].IsConnected = c.IsConnected;
					n64Settings.Controllers[c.ControllerNumber - 1].PakType = c.PakType;
				});

			n64.PutSyncSettings(n64Settings);

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
