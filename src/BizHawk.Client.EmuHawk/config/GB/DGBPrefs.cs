using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class DGBPrefs : Form, IDialogParent
	{
		private readonly Config _config;
		private readonly IGameInfo _game;
		private readonly IMovieSession _movieSession;

		public IDialogController DialogController { get; }

		private DGBPrefs(IDialogController dialogController, Config config, IGameInfo game, IMovieSession movieSession)
		{
			_config = config;
			_game = game;
			_movieSession = movieSession;
			DialogController = dialogController;
			InitializeComponent();
			gbPrefControl1.DialogParent = this;
			gbPrefControl2.DialogParent = this;
			Icon = Properties.Resources.DualIcon;
		}

		private void PutSettings(GambatteLink.GambatteLinkSettings s, GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.PutSettings(_config, _game, _movieSession, s._linkedSettings[0], ss._linkedSyncSettings[1]);
			gbPrefControl2.PutSettings(_config, _game, _movieSession, s._linkedSettings[0], ss._linkedSyncSettings[1]);
		}

		private void GetSettings(out GambatteLink.GambatteLinkSettings s, out GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.GetSettings(out var sl, out var ssl);
			gbPrefControl2.GetSettings(out var sr, out var ssr);

			s = new GambatteLink.GambatteLinkSettings(sl, sr, null, null);
			ss = new GambatteLink.GambatteLinkSyncSettings(ssl, ssr, null, null);
		}

		private bool SyncSettingsChanged => gbPrefControl1.SyncSettingsChanged || gbPrefControl2.SyncSettingsChanged;

		public static void DoDGBPrefsDialog(IMainFormForConfig mainForm, Config config, IGameInfo game, IMovieSession movieSession, GambatteLink gambatte)
		{
			var s = gambatte.GetSettings();
			var ss = gambatte.GetSyncSettings();

			using var dlg = new DGBPrefs(mainForm.DialogController, config, game, movieSession);
			dlg.PutSettings(s, ss);

			dlg.gbPrefControl1.ColorGameBoy = gambatte.IsCGBMode(0);
			dlg.gbPrefControl2.ColorGameBoy = gambatte.IsCGBMode(1);

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
