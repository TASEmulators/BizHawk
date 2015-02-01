using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Common.ReflectionExtensions;

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
					Location = new Point(15, 19 + (i * 25)),
					Width = 85
				});
				Controls.Add(new CheckBox
				{
					Text = "Connected",
					Name = "Controller" + i,
					Location = new Point(105, 15 + (i * 25)),
					Checked = psxSettings.Controllers[i].IsConnected,
					Width = 90
				});

				var dropdown = new ComboBox
				{
					Name = "Controller" + i,
					DropDownStyle = ComboBoxStyle.DropDownList,
					Location = new Point(200, 15 + (i * 25))
				};

				dropdown.PopulateFromEnum<Octoshock.ControllerSetting.ControllerType>(psxSettings.Controllers[i].Type);

				Controls.Add(dropdown);
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

			Controls
				.OfType<ComboBox>()
				.OrderBy(c => c.Name)
				.ToList()
				.ForEach(c =>
				{
					var index = int.Parse(c.Name.Replace("Controller", ""));
					psxSettings.Controllers[index].Type = c.SelectedItem.ToString().GetEnumFromDescription<Octoshock.ControllerSetting.ControllerType>();
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
