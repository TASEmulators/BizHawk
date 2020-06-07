using System;
using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GBPrefControl : UserControl
	{
		public GBPrefControl()
		{
			InitializeComponent();
		}

		[Browsable(false)]
		public bool ColorGameBoy { get; set; }

		[Browsable(false)]
		public bool SyncSettingsChanged { get; private set; }

		private Gameboy.GambatteSettings _s;
		private Gameboy.GambatteSyncSettings _ss;

		public void PutSettings(Gameboy.GambatteSettings s, Gameboy.GambatteSyncSettings ss)
		{
			_s = s ?? new Gameboy.GambatteSettings();
			_ss = ss ?? new Gameboy.GambatteSyncSettings();
			propertyGrid1.SelectedObject = _ss;
			propertyGrid1.Enabled = GlobalWin.MovieSession.Movie.NotActive();
			checkBoxMuted.Checked = _s.Muted;
			cbDisplayBG.Checked = _s.DisplayBG;
			cbDisplayOBJ.Checked = _s.DisplayOBJ;
			cbDisplayWIN.Checked = _s.DisplayWindow;
		}

		public void GetSettings(out Gameboy.GambatteSettings s, out Gameboy.GambatteSyncSettings ss)
		{
			s = _s;
			ss = _ss;
		}

		private void ButtonDefaults_Click(object sender, EventArgs e)
		{
			PutSettings(null, GlobalWin.MovieSession.Movie.IsActive() ? _ss : null);
			if (GlobalWin.MovieSession.Movie.NotActive())
			{
				SyncSettingsChanged = true;
			}
		}

		private void ButtonPalette_Click(object sender, EventArgs e)
		{
			if (ColorGameBoy)
			{
				CGBColorChooserForm.DoCGBColorChooserFormDialog(ParentForm, _s);
			}
			else
			{
				ColorChooserForm.DoColorChooserFormDialog(ParentForm, _s);
			}
		}

		private void PropertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			SyncSettingsChanged = true;
		}

		private void CheckBoxMuted_CheckedChanged(object sender, EventArgs e)
		{
			_s.Muted = ((CheckBox)sender).Checked;
		}

		private void CbDisplayBG_CheckedChanged(object sender, EventArgs e)
		{
			_s.DisplayBG = ((CheckBox)sender).Checked;
		}

		private void CbDisplayOBJ_CheckedChanged(object sender, EventArgs e)
		{
			_s.DisplayOBJ = ((CheckBox)sender).Checked;
		}

		private void CbDisplayWin_CheckedChanged(object sender, EventArgs e)
		{
			_s.DisplayWindow = ((CheckBox)sender).Checked;
		}
	}
}
