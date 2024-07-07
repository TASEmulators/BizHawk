using System.ComponentModel;
using System.Windows.Forms;

using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class GBPrefControl : UserControl
	{
		private Config _config;
		private IGameInfo _game;
		private IMovieSession _movieSession;

		public GBPrefControl()
		{
			InitializeComponent();
		}

		/// <remarks>TODO <see cref="UserControl">UserControls</see> can be <see cref="IDialogParent">IDialogParents</see> too, the modal should still be tied to the parent <see cref="Form"/> if used that way</remarks>
		[Browsable(false)]
		public IDialogParent DialogParent { get; set; }

		[Browsable(false)]
		public bool SyncSettingsChanged { get; private set; }

		private Gameboy.GambatteSettings _s;
		private Gameboy.GambatteSyncSettings _ss;

		public void PutSettings(Config config, IGameInfo game, IMovieSession movieSession, Gameboy.GambatteSettings s, Gameboy.GambatteSyncSettings ss)
		{
			_game = game;
			_config = config;
			_movieSession = movieSession;
			_s = s ?? new Gameboy.GambatteSettings();
			_ss = ss ?? new Gameboy.GambatteSyncSettings();
			propertyGrid1.SelectedObject = _ss;
			propertyGrid1.Enabled = movieSession.Movie.NotActive();
			checkBoxMuted.Checked = _s.Muted;
			cbRgbdsSyntax.Checked = _s.RgbdsSyntax;
			cbShowBorder.Checked = _s.ShowBorder;
		}

		public void GetSettings(out Gameboy.GambatteSettings s, out Gameboy.GambatteSyncSettings ss)
		{
			s = _s;
			ss = _ss;
		}

		private void ButtonDefaults_Click(object sender, EventArgs e)
		{
			PutSettings(_config, _game, _movieSession, null, _movieSession.Movie.IsActive() ? _ss : null);
			if (_movieSession.Movie.NotActive())
			{
				SyncSettingsChanged = true;
			}
		}

		private void ButtonGbPalette_Click(object sender, EventArgs e)
		{
			ColorChooserForm.DoColorChooserFormDialog(DialogParent, _config, _game, _s);
		}

		private void ButtonGbcPalette_Click(object sender, EventArgs e)
		{
			CGBColorChooserForm.DoCGBColorChooserFormDialog(DialogParent, _s);
		}

		private void PropertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			SyncSettingsChanged = true;
		}

		private void CheckBoxMuted_CheckedChanged(object sender, EventArgs e)
		{
			_s.Muted = ((CheckBox)sender).Checked;
		}
		
		private void CbRgbdsSyntax_CheckedChanged(object sender, EventArgs e)
		{
			_s.RgbdsSyntax = ((CheckBox)sender).Checked;
		}

		private void CbShowBorder_CheckedChanged(object sender, EventArgs e)
		{
			_s.ShowBorder = ((CheckBox)sender).Checked;
		}
	}
}
