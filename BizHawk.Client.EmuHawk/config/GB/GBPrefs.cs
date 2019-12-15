using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GBPrefs : Form
	{
		private GBPrefs()
		{
			InitializeComponent();
		}

		public static void DoGBPrefsDialog(IWin32Window owner)
		{
			var gb = (Gameboy)Global.Emulator;
			var s = gb.GetSettings();
			var ss = gb.GetSyncSettings();

			using var dlg = new GBPrefs();
			dlg.gbPrefControl1.PutSettings(s, ss);
			dlg.gbPrefControl1.ColorGameBoy = gb.IsCGBMode();
			if (dlg.ShowDialog(owner) == DialogResult.OK)
			{
				dlg.gbPrefControl1.GetSettings(out s, out ss);
				gb.PutSettings(s);
				if (dlg.gbPrefControl1.SyncSettingsChanged)
				{
					GlobalWin.MainForm.PutCoreSyncSettings(ss);
				}
			}
		}
	}
}
