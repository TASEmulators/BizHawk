using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class DGBPrefs : Form
	{
		private readonly Config _config;
		private readonly IGameInfo _game;
		private readonly IMovieSession _movieSession;

		private DGBPrefs(Config config, IGameInfo game, IMovieSession movieSession)
		{
			_config = config;
			_game = game;
			_movieSession = movieSession;
			InitializeComponent();
			Icon = Properties.Resources.DualIcon;
		}

		private void PutSettings(GambatteLink.GambatteLinkSettings s, GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.PutSettings(_config, _game, _movieSession, s.L, ss.L);
			gbPrefControl2.PutSettings(_config, _game, _movieSession, s.R, ss.R);
		}

		private void GetSettings(out GambatteLink.GambatteLinkSettings s, out GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.GetSettings(out var sl, out var ssl);
			gbPrefControl2.GetSettings(out var sr, out var ssr);

			s = new GambatteLink.GambatteLinkSettings(sl, sr);
			ss = new GambatteLink.GambatteLinkSyncSettings(ssl, ssr);
		}

		private bool SyncSettingsChanged => gbPrefControl1.SyncSettingsChanged || gbPrefControl2.SyncSettingsChanged;

		public static void DoDGBPrefsDialog(IMainFormForConfig mainForm, Config config, IGameInfo game, IMovieSession movieSession, GambatteLink gambatte)
		{
			var s = gambatte.GetSettings();
			var ss = gambatte.GetSyncSettings();

			using var dlg = new DGBPrefs(config, game, movieSession);
			dlg.PutSettings(s, ss);

			dlg.gbPrefControl1.ColorGameBoy = gambatte.IsCGBMode(false);
			dlg.gbPrefControl2.ColorGameBoy = gambatte.IsCGBMode(true);

			if (mainForm.ShowDialogAsChild(dlg) == DialogResult.OK)
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
