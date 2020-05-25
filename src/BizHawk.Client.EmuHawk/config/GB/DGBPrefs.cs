using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class DGBPrefs : Form
	{
		private DGBPrefs()
		{
			InitializeComponent();
		}

		private void PutSettings(GambatteLink.GambatteLinkSettings s, GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.PutSettings(s.L, ss.L);
			gbPrefControl2.PutSettings(s.R, ss.R);
		}

		private void GetSettings(out GambatteLink.GambatteLinkSettings s, out GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.GetSettings(out var sl, out var ssl);
			gbPrefControl2.GetSettings(out var sr, out var ssr);

			s = new GambatteLink.GambatteLinkSettings(sl, sr);
			ss = new GambatteLink.GambatteLinkSyncSettings(ssl, ssr);
		}

		private bool SyncSettingsChanged => gbPrefControl1.SyncSettingsChanged || gbPrefControl2.SyncSettingsChanged;

		public static void DoDGBPrefsDialog(MainForm mainForm, GambatteLink gambatte)
		{
			var s = gambatte.GetSettings();
			var ss = gambatte.GetSyncSettings();

			using var dlg = new DGBPrefs();
			dlg.PutSettings(s, ss);

			var emu = (GambatteLink)GlobalWin.Emulator;
			dlg.gbPrefControl1.ColorGameBoy = emu.IsCGBMode(false);
			dlg.gbPrefControl2.ColorGameBoy = emu.IsCGBMode(true);

			if (dlg.ShowDialog(mainForm) == DialogResult.OK)
			{
				dlg.GetSettings(out s, out ss);
				gambatte.PutSettings(s);
				if (dlg.SyncSettingsChanged)
				{
					mainForm.PutCoreSyncSettings(ss);
				}
			}
		}
	}
}
