using System;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.SNES;

namespace BizHawk.Client.EmuHawk
{
	public partial class BSNESOptions : Form
	{
		private BSNESOptions()
		{
			InitializeComponent();
		}

		public static void DoSettingsDialog(IMainFormForConfig mainForm, BsnesCore bsnes)
		{
			var s = bsnes.GetSettings();
			var ss = bsnes.GetSyncSettings();
			using var dlg = new BSNESOptions
			{
				Entropy = ss.Entropy,
				AlwaysDoubleSize = s.AlwaysDoubleSize,
				Hotfixes = ss.Hotfixes,
				FastPPU = ss.FastPPU,
				ShowObj1 = s.ShowOBJ_0,
				ShowObj2 = s.ShowOBJ_1,
				ShowObj3 = s.ShowOBJ_2,
				ShowObj4 = s.ShowOBJ_3,
				ShowBg1_0 = s.ShowBG1_0,
				ShowBg1_1 = s.ShowBG1_1,
				ShowBg2_0 = s.ShowBG2_0,
				ShowBg2_1 = s.ShowBG2_1,
				ShowBg3_0 = s.ShowBG3_0,
				ShowBg3_1 = s.ShowBG3_1,
				ShowBg4_0 = s.ShowBG4_0,
				ShowBg4_1 = s.ShowBG4_1
			};

			DialogResult result = mainForm.ShowDialogAsChild(dlg);
			if (result == DialogResult.OK)
			{
				ss.Entropy = dlg.Entropy;
				s.AlwaysDoubleSize = dlg.AlwaysDoubleSize;
				ss.Hotfixes = dlg.Hotfixes;
				ss.FastPPU = dlg.FastPPU;
				s.ShowOBJ_0 = dlg.ShowObj1;
				s.ShowOBJ_1 = dlg.ShowObj2;
				s.ShowOBJ_2 = dlg.ShowObj3;
				s.ShowOBJ_3 = dlg.ShowObj4;
				s.ShowBG1_0 = dlg.ShowBg1_0;
				s.ShowBG1_1 = dlg.ShowBg1_1;
				s.ShowBG2_0 = dlg.ShowBg2_0;
				s.ShowBG2_1 = dlg.ShowBg2_1;
				s.ShowBG3_0 = dlg.ShowBg3_0;
				s.ShowBG3_1 = dlg.ShowBg3_1;
				s.ShowBG4_0 = dlg.ShowBg4_0;
				s.ShowBG4_1 = dlg.ShowBg4_1;

				mainForm.PutCoreSettings(s);
				mainForm.PutCoreSyncSettings(ss);
			}
		}

		private bool AlwaysDoubleSize
		{
			get => cbDoubleSize.Checked;
			init => cbDoubleSize.Checked = value;
		}

		private bool Hotfixes
		{
			get => cbGameHotfixes.Checked;
			init => cbGameHotfixes.Checked = value;
		}

		private bool FastPPU
		{
			get => cbFastPPU.Checked;
			init => cbDoubleSize.Enabled = cbFastPPU.Checked = value;
		}

		private BsnesApi.ENTROPY Entropy
		{
			get => (BsnesApi.ENTROPY) EntropyBox.SelectedIndex;
			init => EntropyBox.SelectedIndex = (int) value;
		}

		private bool ShowObj1 { get => Obj1Checkbox.Checked; init => Obj1Checkbox.Checked = value; }
		private bool ShowObj2 { get => Obj2Checkbox.Checked; init => Obj2Checkbox.Checked = value; }
		private bool ShowObj3 { get => Obj3Checkbox.Checked; init => Obj3Checkbox.Checked = value; }
		private bool ShowObj4 { get => Obj4Checkbox.Checked; init => Obj4Checkbox.Checked = value; }

		private bool ShowBg1_0 { get => Bg1_0Checkbox.Checked; init => Bg1_0Checkbox.Checked = value; }
		private bool ShowBg1_1 { get => Bg1_1Checkbox.Checked; init => Bg1_1Checkbox.Checked = value; }
		private bool ShowBg2_0 { get => Bg2_0Checkbox.Checked; init => Bg2_0Checkbox.Checked = value; }
		private bool ShowBg2_1 { get => Bg2_1Checkbox.Checked; init => Bg2_1Checkbox.Checked = value; }
		private bool ShowBg3_0 { get => Bg3_0Checkbox.Checked; init => Bg3_0Checkbox.Checked = value; }
		private bool ShowBg3_1 { get => Bg3_1Checkbox.Checked; init => Bg3_1Checkbox.Checked = value; }
		private bool ShowBg4_0 { get => Bg4_0Checkbox.Checked; init => Bg4_0Checkbox.Checked = value; }
		private bool ShowBg4_1 { get => Bg4_1Checkbox.Checked; init => Bg4_1Checkbox.Checked = value; }

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

		private void FastPPU_CheckedChanged(object sender, EventArgs e)
		{
			cbDoubleSize.Enabled = cbFastPPU.Checked;
		}
	}
}
