using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;

namespace BizHawk.Client.EmuHawk
{
	public partial class GBLPrefs : Form, IDialogParent
	{
		private readonly Config _config;
		private readonly IGameInfo _game;
		private readonly IMovieSession _movieSession;

		public IDialogController DialogController { get; }

		private GBLPrefs(IDialogController dialogController, Config config, IGameInfo game, IMovieSession movieSession)
		{
			_config = config;
			_game = game;
			_movieSession = movieSession;
			DialogController = dialogController;
			InitializeComponent();
			gbPrefControl1.DialogParent = this;
			gbPrefControl2.DialogParent = this;
			gbPrefControl3.DialogParent = this;
			gbPrefControl4.DialogParent = this;
			Icon = Properties.Resources.DualIcon;
		}

		private void PutSettings(GambatteLink.GambatteLinkSettings s, GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.PutSettings(_config, _game, _movieSession, s._linkedSettings[0], ss._linkedSyncSettings[0]);
			gbPrefControl2.PutSettings(_config, _game, _movieSession, s._linkedSettings[1], ss._linkedSyncSettings[1]);
			gbPrefControl3.PutSettings(_config, _game, _movieSession, s._linkedSettings[2], ss._linkedSyncSettings[2]);
			gbPrefControl4.PutSettings(_config, _game, _movieSession, s._linkedSettings[3], ss._linkedSyncSettings[3]);
		}

		private void GetSettings(out GambatteLink.GambatteLinkSettings s, out GambatteLink.GambatteLinkSyncSettings ss)
		{
			gbPrefControl1.GetSettings(out var s1, out var ss1);
			gbPrefControl2.GetSettings(out var s2, out var ss2);
			gbPrefControl3.GetSettings(out var s3, out var ss3);
			gbPrefControl4.GetSettings(out var s4, out var ss4);

			s = new GambatteLink.GambatteLinkSettings(s1, s2, s3, s4);
			ss = new GambatteLink.GambatteLinkSyncSettings(ss1, ss2, ss3, ss4);
		}

		private bool SyncSettingsChanged => gbPrefControl1.SyncSettingsChanged || gbPrefControl2.SyncSettingsChanged || gbPrefControl3.SyncSettingsChanged || gbPrefControl4.SyncSettingsChanged;

		[CLSCompliant(MovieSession.CLS_IMOVIESESSION)]
		public static DialogResult DoGBLPrefsDialog(
			Config config,
			IDialogParent dialogParent,
			IGameInfo game,
			IMovieSession movieSession,
			ISettingsAdapter settable)
		{
			var s = (GambatteLink.GambatteLinkSettings) settable.GetSettings();
			var ss = (GambatteLink.GambatteLinkSyncSettings) settable.GetSyncSettings();

			using var dlg = new GBLPrefs(dialogParent.DialogController, config, game, movieSession);
			dlg.PutSettings(s, ss);

			var result = dialogParent.ShowDialogAsChild(dlg);
			if (!result.IsOk()) return result;
			dlg.GetSettings(out s, out ss);
			settable.PutCoreSettings(s);
			if (dlg.SyncSettingsChanged) settable.PutCoreSyncSettings(ss);
			return result;
		}
	}
}
