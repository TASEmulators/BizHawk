using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.BSNES;

namespace BizHawk.Client.EmuHawk
{
	public partial class BSNESOptions : Form
	{
		private readonly BsnesCore.SnesSettings _settings;
		private readonly BsnesCore.SnesSyncSettings _syncSettings;

		private BSNESOptions(BsnesCore.SnesSettings s, BsnesCore.SnesSyncSettings ss)
		{
			_settings = s;
			_syncSettings = ss;
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			EntropyBox.PopulateFromEnum(_syncSettings.Entropy);
			AspectRatioCorrectionBox.PopulateFromEnum(_settings.AspectRatioCorrection);
			SatellaviewCartridgeBox.PopulateFromEnum(_syncSettings.SatellaviewCartridge);
			RegionBox.PopulateFromEnum(_syncSettings.RegionOverride);
		}

		public static DialogResult DoSettingsDialog(IDialogParent dialogParent, ISettingsAdapter settable)
		{
			var s = (BsnesCore.SnesSettings) settable.GetSettings();
			var ss = (BsnesCore.SnesSyncSettings) settable.GetSyncSettings();
			using var dlg = new BSNESOptions(s, ss)
			{
				AlwaysDoubleSize = s.AlwaysDoubleSize,
				CropSGBFrame = s.CropSGBFrame,
				NoPPUSpriteLimit = s.NoPPUSpriteLimit,
				ShowOverscan = s.ShowOverscan,
				ShowCursor = s.ShowCursor,
				Hotfixes = ss.Hotfixes,
				FastPPU = ss.FastPPU,
				FastDSP = ss.FastDSP,
				FastCoprocessors = ss.FastCoprocessors,
				UseSGB2 = ss.UseSGB2,
				UseRealTime = ss.UseRealTime,
				InitialTime = ss.InitialTime,
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

			var result = dialogParent.ShowDialogAsChild(dlg);
			if (!result.IsOk()) return result;

			s.AlwaysDoubleSize = dlg.AlwaysDoubleSize;
			s.CropSGBFrame = dlg.CropSGBFrame;
			s.NoPPUSpriteLimit = dlg.NoPPUSpriteLimit;
			s.ShowOverscan = dlg.ShowOverscan;
			s.ShowCursor = dlg.ShowCursor;
			s.AspectRatioCorrection = dlg.AspectRatioCorrection;
			ss.Entropy = dlg.Entropy;
			ss.RegionOverride = dlg.RegionOverride;
			ss.Hotfixes = dlg.Hotfixes;
			ss.FastPPU = dlg.FastPPU;
			ss.FastDSP = dlg.FastDSP;
			ss.FastCoprocessors = dlg.FastCoprocessors;
			ss.UseSGB2 = dlg.UseSGB2;
			ss.SatellaviewCartridge = dlg.SatellaviewCartridge;
			ss.UseRealTime = dlg.UseRealTime;
			ss.InitialTime = dlg.InitialTime;
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
			settable.PutCoreSettings(s);
			settable.PutCoreSyncSettings(ss);
			return result;
		}

		private bool AlwaysDoubleSize
		{
			get => cbDoubleSize.Checked;
			init => cbDoubleSize.Checked = value;
		}

		private bool CropSGBFrame
		{
			get => cbCropSGBFrame.Checked;
			init => cbCropSGBFrame.Checked = value;
		}

		private bool NoPPUSpriteLimit
		{
			get => cbNoPPUSpriteLimit.Checked;
			init => cbNoPPUSpriteLimit.Checked = value;
		}

		private bool ShowOverscan
		{
			get => cbShowOverscan.Checked;
			init => cbShowOverscan.Checked = value;
		}

		private bool ShowCursor
		{
			get => cbShowCursor.Checked;
			init => cbShowCursor.Checked = value;
		}

		private BsnesApi.ASPECT_RATIO_CORRECTION AspectRatioCorrection => (BsnesApi.ASPECT_RATIO_CORRECTION)AspectRatioCorrectionBox.SelectedIndex;

		private bool Hotfixes
		{
			get => cbGameHotfixes.Checked;
			init => cbGameHotfixes.Checked = value;
		}

		private bool FastPPU
		{
			get => cbFastPPU.Checked;
			init => cbDoubleSize.Enabled = cbNoPPUSpriteLimit.Enabled = cbFastPPU.Checked = value;
		}

		private bool FastDSP
		{
			get => cbFastDSP.Checked;
			init => cbFastDSP.Checked = value;
		}

		private bool FastCoprocessors
		{
			get => cbFastCoprocessor.Checked;
			init => cbFastCoprocessor.Checked = value;
		}

		private bool UseSGB2
		{
			get => cbUseSGB2.Checked;
			init => cbUseSGB2.Checked = value;
		}

		private bool UseRealTime
		{
			get => cbUseRealTime.Checked;
			init => cbUseRealTime.Checked = value;
		}

		private DateTime InitialTime
		{
			get => dtpInitialTime.Value;
			init => dtpInitialTime.Value = value;
		}

		private BsnesApi.ENTROPY Entropy => (BsnesApi.ENTROPY) EntropyBox.SelectedIndex;

		private BsnesApi.REGION_OVERRIDE RegionOverride => (BsnesApi.REGION_OVERRIDE)RegionBox.SelectedIndex;

		private BsnesCore.SATELLAVIEW_CARTRIDGE SatellaviewCartridge => (BsnesCore.SATELLAVIEW_CARTRIDGE)SatellaviewCartridgeBox.SelectedIndex;

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
			cbDoubleSize.Enabled = cbNoPPUSpriteLimit.Enabled = cbFastPPU.Checked;
		}
	}
}
