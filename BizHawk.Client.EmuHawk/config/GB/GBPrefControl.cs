using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk.config.GB
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

		Gameboy.GambatteSettings s;
		Gameboy.GambatteSyncSettings ss;

		public void PutSettings(Gameboy.GambatteSettings s, Gameboy.GambatteSyncSettings ss)
		{
			this.s = s ?? new Gameboy.GambatteSettings();
			this.ss = ss ?? new Gameboy.GambatteSyncSettings();
			propertyGrid1.SelectedObject = this.ss;
			propertyGrid1.Enabled = !Global.MovieSession.Movie.IsActive;
			checkBoxMuted.Checked = this.s.Muted;
			cbDisplayBG.Checked = this.s.DisplayBG;
			cbDisplayOBJ.Checked = this.s.DisplayOBJ;
			cbDisplayWIN.Checked = this.s.DisplayWindow;
		}

		public void GetSettings(out Gameboy.GambatteSettings s, out Gameboy.GambatteSyncSettings ss)
		{
			s = this.s;
			ss = this.ss;
		}

		private void buttonDefaults_Click(object sender, EventArgs e)
		{
			PutSettings(null, Global.MovieSession.Movie.IsActive ? ss : null);
			if (!Global.MovieSession.Movie.IsActive)
				SyncSettingsChanged = true;
		}

		private void buttonPalette_Click(object sender, EventArgs e)
		{
			if (ColorGameBoy)
				CGBColorChooserForm.DoCGBColorChoserFormDialog(this.ParentForm, s);
			else
				ColorChooserForm.DoColorChooserFormDialog(this.ParentForm, s);
		}

		private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			SyncSettingsChanged = true;
		}

		private void checkBoxMuted_CheckedChanged(object sender, EventArgs e)
		{
			s.Muted = (sender as CheckBox).Checked;
		}

		private void cbDisplayBG_CheckedChanged(object sender, EventArgs e)
		{
			s.DisplayBG = (sender as CheckBox).Checked;
		}

		private void cbDisplayOBJ_CheckedChanged(object sender, EventArgs e)
		{
			s.DisplayOBJ = (sender as CheckBox).Checked;
		}

		private void cbDisplayWIN_CheckedChanged(object sender, EventArgs e)
		{
			s.DisplayWindow = (sender as CheckBox).Checked;
		}
	}
}
