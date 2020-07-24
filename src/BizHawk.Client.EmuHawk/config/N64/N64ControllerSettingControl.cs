using System;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Cores.Nintendo.N64;

using EnumsNET;

using static BizHawk.Emulation.Cores.Nintendo.N64.N64SyncSettings.N64ControllerSettings;

namespace BizHawk.Client.EmuHawk
{
	public partial class N64ControllerSettingControl : UserControl
	{
		private int _controllerNumber = 1;

		public N64ControllerSettingControl()
		{
			InitializeComponent();

			ControllerNameLabel.Text = $"Controller {ControllerNumber}";
		}

		public int ControllerNumber
		{
			get => _controllerNumber;
			set
			{
				_controllerNumber = value;
				Refresh();
			}
		}

		public bool IsConnected
		{
			get => EnabledCheckbox.Checked;
			set
			{
				EnabledCheckbox.Checked = value;
				PakTypeDropdown.Enabled = value;
				Refresh();
			}
		}

		public N64ControllerPakType PakType
		{
			get => Enums.Parse<N64ControllerPakType>(PakTypeDropdown.SelectedItem.ToString(), false, EnumFormat.Description);
			set
			{
				var chosen = value.AsString(EnumFormat.Description);
				for (int i = 0, l = PakTypeDropdown.Items.Count; i < l; i++)
				{
					if (PakTypeDropdown.Items[i].ToString() != chosen) continue;
					PakTypeDropdown.SelectedIndex = i;
					Refresh();
					return;
				}
			}
		}

		public override void Refresh()
		{
			ControllerNameLabel.Text = $"Controller {ControllerNumber}";
			base.Refresh();
		}

		private void EnabledCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			PakTypeDropdown.Enabled = EnabledCheckbox.Checked;
		}
	}
}
