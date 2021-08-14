using System;
using System.Windows.Forms;

using BizHawk.Common.ReflectionExtensions;
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
			Port1ComboBox.PopulateFromEnum(_syncSettings.Port1);
			Port2ComboBox.PopulateFromEnum(_syncSettings.Port2);
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			var port1Option = Port1ComboBox.SelectedItem.ToString().GetEnumFromDescription<PeripheralOption>();
			var port2Option = Port2ComboBox.SelectedItem.ToString().GetEnumFromDescription<PeripheralOption>();
			if (port1Option != _syncSettings.Port1 || port2Option != _syncSettings.Port2)
			{
				_syncSettings.Port1 = port1Option;
				_syncSettings.Port2 = port2Option;
				_mainForm.PutCoreSyncSettings(_syncSettings);
			}

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
