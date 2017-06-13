using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SNESOptions : Form
	{
		private SNESOptions()
		{
			InitializeComponent();
		}

		private bool _suppressDoubleSize;
		private bool _userDoubleSizeOption;

		public static void DoSettingsDialog(IWin32Window owner)
		{
			var s = ((LibsnesCore)Global.Emulator).GetSettings();
			var ss = ((LibsnesCore)Global.Emulator).GetSyncSettings();
			var dlg = new SNESOptions
			{
				AlwaysDoubleSize = s.AlwaysDoubleSize,
				ForceDeterminism = s.ForceDeterminism,
				//Profile = ss.Profile
			};

			var result = dlg.ShowDialog(owner);
			if (result == DialogResult.OK)
			{
				s.AlwaysDoubleSize = dlg.AlwaysDoubleSize;
				s.ForceDeterminism = dlg.ForceDeterminism;
				//ss.Profile = dlg.Profile;
				GlobalWin.MainForm.PutCoreSettings(s);
				GlobalWin.MainForm.PutCoreSyncSettings(ss);
			}
		}

		private bool AlwaysDoubleSize
		{
			get
			{
				return _userDoubleSizeOption;
			}

			set
			{
				_userDoubleSizeOption = value;
				RefreshDoubleSizeOption();
			}
		}

		private bool ForceDeterminism
		{
			get { return cbForceDeterminism.Checked; }
			set { cbForceDeterminism.Checked = value; }
		}

		void RefreshDoubleSizeOption()
		{
			_suppressDoubleSize = true;
			cbDoubleSize.Checked = !cbDoubleSize.Enabled || _userDoubleSizeOption;
			_suppressDoubleSize = false;
		}

		private void CbDoubleSize_CheckedChanged(object sender, EventArgs e)
		{
			if (_suppressDoubleSize)
			{
				return;
			}

			_userDoubleSizeOption = cbDoubleSize.Checked;
		}

		private void BtnOk_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void BtnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

	}
}
