using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk.config.GB
{
	public partial class DGBPrefs : Form
	{
		DGBPrefs()
		{
			InitializeComponent();
		}

		void PutSettings(GambatteLink.GambatteLinkSettings s, GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.PutSettings(s.L, ss.L);
			gbPrefControl2.PutSettings(s.R, ss.R);
		}

		void GetSettings(out GambatteLink.GambatteLinkSettings s, out GambatteLink.GambatteLinkSyncSettings ss)
		{
			Gameboy.GambatteSettings sl;
			Gameboy.GambatteSyncSettings ssl;
			Gameboy.GambatteSettings sr;
			Gameboy.GambatteSyncSettings ssr;
			gbPrefControl1.GetSettings(out sl, out ssl);
			gbPrefControl2.GetSettings(out sr, out ssr);

			s = new GambatteLink.GambatteLinkSettings(sl, sr);
			ss = new GambatteLink.GambatteLinkSyncSettings(ssl, ssr);
		}

		public static void DoDGBPrefsDialog(IWin32Window owner)
		{
			var s = (GambatteLink.GambatteLinkSettings)Global.Emulator.GetSettings();
			var ss = (GambatteLink.GambatteLinkSyncSettings)Global.Emulator.GetSyncSettings();

			using (var dlg = new DGBPrefs())
			{
				dlg.PutSettings(s, ss);

				var emu = (GambatteLink)Global.Emulator;
				dlg.gbPrefControl1.ColorGameBoy = emu.IsCGBMode(false);
				dlg.gbPrefControl2.ColorGameBoy = emu.IsCGBMode(true);

				if (dlg.ShowDialog(owner) == DialogResult.OK)
				{
					dlg.GetSettings(out s, out ss);
					Global.Emulator.PutSettings(s);
					GlobalWin.MainForm.PutCoreSyncSettings(ss);
				}
			}
		}
	}
}
