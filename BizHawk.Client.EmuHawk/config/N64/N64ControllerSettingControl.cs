using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Consoles.Nintendo.N64;
using BizHawk.Common;

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
				Refresh();
			}
		}

		public N64ControllerSettings.N64ControllerPakType PakType
		{
			get
			{
				return EnumHelper.GetValueFromDescription<N64ControllerSettings.N64ControllerPakType>(
					PakTypeDropdown.SelectedItem.ToString());
			}

			set
			{
				var toSelect = PakTypeDropdown.Items
					.OfType<object>()
					.FirstOrDefault(item => item.ToString() == EnumHelper.GetDescription(value));
				PakTypeDropdown.SelectedItem = toSelect;

				Refresh();
			}
		}

		public override void Refresh()
		{
			ControllerNameLabel.Text = "Controller " + ControllerNumber;
			base.Refresh();
		}
	}
}
