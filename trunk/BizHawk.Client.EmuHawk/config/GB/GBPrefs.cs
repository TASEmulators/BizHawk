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
	public partial class GBPrefs : Form
	{
		public GBPrefs()
		{
			InitializeComponent();
		}

		public static void DoGBPrefsDialog(IWin32Window owner)
		{
			var s = (Gameboy.GambatteSettings)Global.Emulator.GetSettings();
			var ss = (Gameboy.GambatteSyncSettings)Global.Emulator.GetSyncSettings();

			using (var dlg = new GBPrefs())
			{
				dlg.gbPrefControl1.PutSettings(s, ss);
				dlg.gbPrefControl1.ColorGameBoy = ((Gameboy)Global.Emulator).IsCGBMode();
				if (dlg.ShowDialog(owner) == DialogResult.OK)
				{
					dlg.gbPrefControl1.GetSettings(out s, out ss);
					Global.Emulator.PutSettings(s);
					GlobalWin.MainForm.PutCoreSyncSettings(ss);
				}
			}
		}
	}
}
