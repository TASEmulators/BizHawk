using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.SNES;

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

		public static DialogResult DoSettingsDialog(IDialogParent dialogParent, ISettingsAdapter settable)
		{
			var s = (LibsnesCore.SnesSettings) settable.GetSettings();
			var ss = (LibsnesCore.SnesSyncSettings) settable.GetSyncSettings();
			using var dlg = new SNESOptions
			{
				RandomizedInitialState = ss.RandomizedInitialState,
				AlwaysDoubleSize = s.AlwaysDoubleSize,
				CropSGBFrame = s.CropSGBFrame,
				ShowObj1 = s.ShowOBJ_0,
				ShowObj2 = s.ShowOBJ_1,
				ShowObj3 = s.ShowOBJ_2,
				ShowObj4 = s.ShowOBJ_3,
				ShowBg1 = s.ShowBG1_0,
				ShowBg2 = s.ShowBG2_0,
				ShowBg3 = s.ShowBG3_0,
				ShowBg4 = s.ShowBG4_0
			};

			var result = dialogParent.ShowDialogAsChild(dlg);
			if (!result.IsOk()) return result;

			s.AlwaysDoubleSize = dlg.AlwaysDoubleSize;
			s.CropSGBFrame = dlg.CropSGBFrame;
			ss.RandomizedInitialState = dlg.RandomizedInitialState;
			s.ShowOBJ_0 = dlg.ShowObj1;
			s.ShowOBJ_1 = dlg.ShowObj2;
			s.ShowOBJ_2 = dlg.ShowObj3;
			s.ShowOBJ_3 = dlg.ShowObj4;
			s.ShowBG1_0 = s.ShowBG1_1 = dlg.ShowBg1;
			s.ShowBG2_0 = s.ShowBG2_1 = dlg.ShowBg2;
			s.ShowBG3_0 = s.ShowBG3_1 = dlg.ShowBg3;
			s.ShowBG4_0 = s.ShowBG4_1 = dlg.ShowBg4;
			settable.PutCoreSettings(s);
			settable.PutCoreSyncSettings(ss);
			return result;
		}

		private bool AlwaysDoubleSize
		{
			get => _userDoubleSizeOption;
			set
			{
				_userDoubleSizeOption = value;
				RefreshDoubleSizeOption();
			}
		}

		private bool CropSGBFrame
		{
			get => cbCropSGBFrame.Checked;
			set => cbCropSGBFrame.Checked = value;
		}

		private bool RandomizedInitialState
		{
			get => cbRandomizedInitialState.Checked;
			set => cbRandomizedInitialState.Checked = value;
		}

		private bool ShowObj1 { get => Obj1Checkbox.Checked; set => Obj1Checkbox.Checked = value;
		}
		private bool ShowObj2 { get => Obj2Checkbox.Checked; set => Obj2Checkbox.Checked = value; }
		private bool ShowObj3 { get => Obj3Checkbox.Checked; set => Obj3Checkbox.Checked = value; }
		private bool ShowObj4 { get => Obj4Checkbox.Checked; set => Obj4Checkbox.Checked = value; }

		private bool ShowBg1 { get => Bg1Checkbox.Checked; set => Bg1Checkbox.Checked = value; }
		private bool ShowBg2 { get => Bg2Checkbox.Checked; set => Bg2Checkbox.Checked = value; }
		private bool ShowBg3 { get => Bg3Checkbox.Checked; set => Bg3Checkbox.Checked = value; }
		private bool ShowBg4 { get => Bg4Checkbox.Checked; set => Bg4Checkbox.Checked = value; }

		private void RefreshDoubleSizeOption()
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
