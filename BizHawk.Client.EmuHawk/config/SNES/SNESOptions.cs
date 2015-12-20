using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SNESOptions : Form
	{
		public SNESOptions()
		{
			InitializeComponent();
		}

		bool SuppressDoubleSize;
		bool UserDoubleSizeOption;

		public static void DoSettingsDialog(IWin32Window owner)
		{
			var s = ((LibsnesCore)Global.Emulator).GetSettings();
			var ss = ((LibsnesCore)Global.Emulator).GetSyncSettings();
			var dlg = new SNESOptions
			{
				UseRingBuffer = s.UseRingBuffer,
				AlwaysDoubleSize = s.AlwaysDoubleSize,
				ForceDeterminism = s.ForceDeterminism,
				Profile = ss.Profile
			};

			var result = dlg.ShowDialog(owner);
			if (result == DialogResult.OK)
			{
				s.UseRingBuffer = dlg.UseRingBuffer;
				s.AlwaysDoubleSize = dlg.AlwaysDoubleSize;
				s.ForceDeterminism = dlg.ForceDeterminism;
				ss.Profile = dlg.Profile;
				GlobalWin.MainForm.PutCoreSettings(s);
				GlobalWin.MainForm.PutCoreSyncSettings(ss);
			}
		}

		private void SNESOptions_Load(object sender, EventArgs e)
		{
			rbAccuracy.Visible = VersionInfo.DeveloperBuild;
		}

		public string Profile
		{
			get
			{
				if (rbCompatibility.Checked) return "Compatibility";
				else if (rbPerformance.Checked) return "Performance";
				else if (rbAccuracy.Checked) return "Accuracy";
				else throw new InvalidOperationException();
			}

			set
			{
				rbCompatibility.Checked = (value == "Compatibility");
				rbPerformance.Checked = (value == "Performance");
				rbAccuracy.Checked = (value == "Accuracy");
			}
		}

		public bool UseRingBuffer
		{
			get { return cbRingbuf.Checked; }
			set { cbRingbuf.Checked = value; }
		}

		public bool AlwaysDoubleSize
		{
			get { return UserDoubleSizeOption; }
			set { UserDoubleSizeOption = value; RefreshDoubleSizeOption();  }
		}

		public bool ForceDeterminism
		{
			get { return cbForceDeterminism.Checked; }
			set { cbForceDeterminism.Checked = value; }
		}

		void RefreshDoubleSizeOption()
		{
			SuppressDoubleSize = true;
			if (cbDoubleSize.Enabled)
				cbDoubleSize.Checked = UserDoubleSizeOption;
			else cbDoubleSize.Checked = true;
			SuppressDoubleSize = false;
		}

		private void rbAccuracy_CheckedChanged(object sender, EventArgs e)
		{
			cbDoubleSize.Enabled = !rbAccuracy.Checked;
			lblDoubleSize.ForeColor = cbDoubleSize.Enabled ? System.Drawing.SystemColors.ControlText : System.Drawing.SystemColors.GrayText;
			RefreshDoubleSizeOption();
		}

		private void cbDoubleSize_CheckedChanged(object sender, EventArgs e)
		{
			if (SuppressDoubleSize) return;
			UserDoubleSizeOption = cbDoubleSize.Checked;
		}

		private void cbForceDeterminism_CheckedChanged(object sender, EventArgs e)
		{

		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

	}
}
