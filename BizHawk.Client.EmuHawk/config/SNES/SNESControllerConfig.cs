using System;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class SNESControllerSettings : Form
	{
		private LibsnesCore.SnesSyncSettings _syncSettings;

		public SNESControllerSettings()
		{
			InitializeComponent();
		}

		private void SNESControllerSettings_Load(object sender, EventArgs e)
		{
			_syncSettings = ((LibsnesCore)Global.Emulator).GetSyncSettings().Clone();
			Port1ComboBox.PopulateFromEnum<LibsnesControllerDeck.ControllerType>(_syncSettings.LeftPort);
			Port2ComboBox.PopulateFromEnum<LibsnesControllerDeck.ControllerType>(_syncSettings.RightPort);
		}

		private void OkBtn_Click(object sender, EventArgs e)
		{
			bool changed =
				_syncSettings.LeftPort.ToString() != Port1ComboBox.SelectedItem.ToString()
				|| _syncSettings.RightPort.ToString() != Port2ComboBox.SelectedItem.ToString();

			if (changed)
			{
				_syncSettings.LeftPort = (LibsnesControllerDeck.ControllerType)Enum.Parse(typeof(LibsnesControllerDeck.ControllerType), Port1ComboBox.SelectedItem.ToString());
				_syncSettings.RightPort = (LibsnesControllerDeck.ControllerType)Enum.Parse(typeof(LibsnesControllerDeck.ControllerType), Port2ComboBox.SelectedItem.ToString());

				GlobalWin.MainForm.PutCoreSyncSettings(_syncSettings);
			}

			DialogResult = DialogResult.OK;
			Close();
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("Controller settings aborted");
			DialogResult = DialogResult.Cancel;
			Close();
		}
	}
}
