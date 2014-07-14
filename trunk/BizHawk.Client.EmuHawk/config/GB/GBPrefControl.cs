using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

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

		Gameboy.GambatteSettings s;
		Gameboy.GambatteSyncSettings ss;

		public void PutSettings(Gameboy.GambatteSettings s, Gameboy.GambatteSyncSettings ss)
		{
			this.s = s ?? new Gameboy.GambatteSettings();
			this.ss = ss ?? new Gameboy.GambatteSyncSettings();
			propertyGrid1.SelectedObject = this.ss;
		}

		public void GetSettings(out Gameboy.GambatteSettings s, out Gameboy.GambatteSyncSettings ss)
		{
			s = this.s;
			ss = this.ss;
		}

		private void buttonDefaults_Click(object sender, EventArgs e)
		{
			PutSettings(null, null);
		}

		private void buttonPalette_Click(object sender, EventArgs e)
		{
			if (ColorGameBoy)
				CGBColorChooserForm.DoCGBColorChoserFormDialog(this.ParentForm, s);
			else
				ColorChooserForm.DoColorChooserFormDialog(this.ParentForm, s);
		}


	}
}
