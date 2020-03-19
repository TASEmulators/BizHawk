using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.EmuHawk
{
	public partial class SNESControllerSettings : Form
	{
		private readonly MainForm _mainForm;
		private readonly LibsnesCore.SnesSyncSettings _syncSettings;
		private bool _suppressDropdownChangeEvents;

		public SNESControllerSettings(
			MainForm mainForm,
			LibsnesCore.SnesSyncSettings syncSettings)
		{
			_mainForm = mainForm;
			_syncSettings = syncSettings;
			InitializeComponent();
		}

		private void SNESControllerSettings_Load(object sender, EventArgs e)
		{
			LimitAnalogChangeCheckBox.Checked = _syncSettings.LimitAnalogChangeSensitivity;

			_suppressDropdownChangeEvents = true;
			Port1ComboBox.PopulateFromEnum<LibsnesControllerDeck.ControllerType>(_syncSettings.LeftPort);
			Port2ComboBox.PopulateFromEnum<LibsnesControllerDeck.ControllerType>(_syncSettings.RightPort);
			_suppressDropdownChangeEvents = false;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.LeftPort.ToString() != Port1ComboBox.SelectedItem.ToString()
				|| _syncSettings.RightPort.ToString() != Port2ComboBox.SelectedItem.ToString()
				|| _syncSettings.LimitAnalogChangeSensitivity != LimitAnalogChangeCheckBox.Checked;

			if (changed)
			{
				_syncSettings.LeftPort = (LibsnesControllerDeck.ControllerType)Enum.Parse(typeof(LibsnesControllerDeck.ControllerType), Port1ComboBox.SelectedItem.ToString());
				_syncSettings.RightPort = (LibsnesControllerDeck.ControllerType)Enum.Parse(typeof(LibsnesControllerDeck.ControllerType), Port2ComboBox.SelectedItem.ToString());
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
			if (!_suppressDropdownChangeEvents)
			{
				var leftPort = (LibsnesControllerDeck.ControllerType)Enum.Parse(typeof(LibsnesControllerDeck.ControllerType), Port1ComboBox.SelectedItem.ToString());
				var rightPort = (LibsnesControllerDeck.ControllerType)Enum.Parse(typeof(LibsnesControllerDeck.ControllerType), Port2ComboBox.SelectedItem.ToString());
				ToggleMouseSection(leftPort == LibsnesControllerDeck.ControllerType.Mouse
					|| rightPort == LibsnesControllerDeck.ControllerType.Mouse);
			}
		}

		private void ToggleMouseSection(bool show)
		{
			LimitAnalogChangeCheckBox.Visible =
				MouseSpeedLabel1.Visible =
				MouseSpeedLabel2.Visible =
				MouseSpeedLabel3.Visible =
				MouseNagLabel1.Visible =
				MouseNagLabel2.Visible =
				show;
		}
	}
}
