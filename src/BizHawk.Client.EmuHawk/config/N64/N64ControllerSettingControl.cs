using System.Linq;
using System.Windows.Forms;

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

		public N64SyncSettings.N64ControllerSettings.N64ControllerPakType PakType
		{
			get => PakTypeDropdown.SelectedItem
				.ToString()
				.GetEnumFromDescription<N64SyncSettings.N64ControllerSettings.N64ControllerPakType>();
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
			ControllerNameLabel.Text = $"Controller {ControllerNumber}";
			base.Refresh();
		}

		private void EnabledCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			PakTypeDropdown.Enabled = EnabledCheckbox.Checked;
		}
	}
}
