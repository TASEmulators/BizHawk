using System.Windows.Forms;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class GBPrefs : Form
	{
		private GBPrefs()
		{
			InitializeComponent();
		}

		public static void DoGBPrefsDialog(MainForm mainForm, Gameboy gb)
		{
			var s = gb.GetSettings();
			var ss = gb.GetSyncSettings();

			using var dlg = new GBPrefs();
			dlg.gbPrefControl1.PutSettings(s, ss);
			dlg.gbPrefControl1.ColorGameBoy = gb.IsCGBMode();
			if (dlg.ShowDialog(mainForm) == DialogResult.OK)
			{
				dlg.gbPrefControl1.GetSettings(out s, out ss);
				gb.PutSettings(s);
				if (dlg.gbPrefControl1.SyncSettingsChanged)
				{
					mainForm.PutCoreSyncSettings(ss);
				}
			}
		}
	}
}
