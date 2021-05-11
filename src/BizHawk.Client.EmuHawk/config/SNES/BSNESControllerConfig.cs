using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.EmuHawk
{
	public partial class BSNESControllerSettings : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly BsnesCore.SnesSyncSettings _syncSettings;

		public BSNESControllerSettings(
			IMainFormForConfig mainForm,
			BsnesCore.SnesSyncSettings syncSettings)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void SNESControllerSettings_Load(object sender, EventArgs e)
		{
			LimitAnalogChangeCheckBox.Checked = _syncSettings.LimitAnalogChangeSensitivity;

			Port1ComboBox.SelectedIndex = (int) _syncSettings.LeftPort >= Port1ComboBox.Items.Count ? 0 : (int) _syncSettings.LeftPort;
			Port2ComboBox.PopulateFromEnum(_syncSettings.RightPort);
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.LeftPort != (BsnesApi.BSNES_INPUT_DEVICE) Port1ComboBox.SelectedIndex
				|| _syncSettings.RightPort != (BsnesApi.BSNES_INPUT_DEVICE) Port2ComboBox.SelectedIndex
				|| _syncSettings.LimitAnalogChangeSensitivity != LimitAnalogChangeCheckBox.Checked;

			if (changed)
			{
				_syncSettings.LeftPort = (BsnesApi.BSNES_INPUT_DEVICE) Port1ComboBox.SelectedIndex;
				_syncSettings.RightPort = (BsnesApi.BSNES_INPUT_DEVICE) Port2ComboBox.SelectedIndex;
				_syncSettings.LimitAnalogChangeSensitivity = LimitAnalogChangeCheckBox.Checked;

				_mainForm.PutCoreSyncSettings(_syncSettings);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			_mainForm.AddOnScreenMessage("Controller settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void PortComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			var leftPort = (BsnesApi.BSNES_INPUT_DEVICE) Port1ComboBox.SelectedIndex;
			var rightPort = (BsnesApi.BSNES_INPUT_DEVICE) Port2ComboBox.SelectedIndex;
			ToggleMouseSection(leftPort == BsnesApi.BSNES_INPUT_DEVICE.Mouse || rightPort == BsnesApi.BSNES_INPUT_DEVICE.Mouse);
		}

		private void ToggleMouseSection(bool show)
		{
			LimitAnalogChangeCheckBox.Visible =
				MouseSpeedLabel1.Visible =
				MouseNagLabel1.Visible =
				show;
		}
	}
}
