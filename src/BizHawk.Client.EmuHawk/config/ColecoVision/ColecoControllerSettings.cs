using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.ColecoVision;

namespace BizHawk.Client.EmuHawk
{
	public partial class ColecoControllerSettings : Form
	{
		private readonly IMainFormForConfig _mainForm;
		private readonly ColecoVision.ColecoSyncSettings _syncSettings;

		public ColecoControllerSettings(
			IMainFormForConfig mainForm,
			ColecoVision.ColecoSyncSettings settings)
		{
			_mainForm = mainForm;
			_syncSettings = settings;
			InitializeComponent();
			Icon = Properties.Resources.GameControllerIcon;
		}

		private void ColecoControllerSettings_Load(object sender, EventArgs e)
		{
			foreach (var val in ColecoVisionControllerDeck.ControllerCtors.Keys)
			{
				Port1ComboBox.Items.Add(val);
				Port2ComboBox.Items.Add(val);
			}

			Port1ComboBox.SelectedItem = _syncSettings.Port1;
			Port2ComboBox.SelectedItem = _syncSettings.Port2;
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.Port1 != Port1ComboBox.SelectedItem.ToString()
				|| _syncSettings.Port2 != Port2ComboBox.SelectedItem.ToString();

			if (changed)
			{
				_syncSettings.Port1 = Port1ComboBox.SelectedItem.ToString();
				_syncSettings.Port2 = Port2ComboBox.SelectedItem.ToString();

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
	}
}
