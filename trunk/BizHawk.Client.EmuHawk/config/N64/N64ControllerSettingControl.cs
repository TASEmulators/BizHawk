using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;

using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Client.EmuHawk
{
	public partial class N64ControllerSettingControl : UserControl
	{
		private int _controllerNumber = 1;

		public N64ControllerSettingControl()
		{
			InitializeComponent();

			ControllerNameLabel.Text = "Controller " + ControllerNumber;
		}

		private void N64ControllerSettingControl_Load(object sender, EventArgs e)
		{

		}

		public int ControllerNumber
		{
			get
			{
				return _controllerNumber;
			}
			set
			{
				_controllerNumber = value;
				Refresh();
			}
		}

		public bool IsConnected
		{
			get
			{
				return EnabledCheckbox.Checked;
			}

			set
			{
				EnabledCheckbox.Checked = value;
				if (PakTypeDropdown != null) // Null check for designer
				{
					PakTypeDropdown.Enabled = value;
				}

				Refresh();
			}
		}

		public N64SyncSettings.N64ControllerSettings.N64ControllerPakType PakType
		{
			get
			{
				if (PakTypeDropdown.SelectedItem != null) // Null check for designer
				{
					return PakTypeDropdown.SelectedItem
						.ToString()
						.GetEnumFromDescription<N64SyncSettings.N64ControllerSettings.N64ControllerPakType>();
				}

				return N64SyncSettings.N64ControllerSettings.N64ControllerPakType.NO_PAK;
			}

			set
			{
				if (PakTypeDropdown.Items.Count > 0) // Null check for designer
				{
					var toSelect = PakTypeDropdown.Items
						.OfType<object>()
						.FirstOrDefault(item => item.ToString() == value.GetDescription());
					PakTypeDropdown.SelectedItem = toSelect;

					Refresh();
				}
			}
		}

		public override void Refresh()
		{
			ControllerNameLabel.Text = "Controller " + ControllerNumber;
			base.Refresh();
		}

		private void EnabledCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			PakTypeDropdown.Enabled = EnabledCheckbox.Checked;
		}
	}
}
