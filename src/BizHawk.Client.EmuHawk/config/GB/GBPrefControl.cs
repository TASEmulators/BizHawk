using System;
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

		[Browsable(false)]
		public bool ColorGameBoy { get; set; }

		/// <remarks>TODO <see cref="UserControl">UserControls</see> can be <see cref="IDialogParent">IDialogParents</see> too, the modal should still be tied to the parent <see cref="Form"/> if used that way</remarks>
		[Browsable(false)]
		public IDialogParent DialogParent { private get; set; }

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

		private void ButtonPalette_Click(object sender, EventArgs e)
		{
			if (ColorGameBoy)
			{
				CGBColorChooserForm.DoCGBColorChooserFormDialog(DialogParent, _s);
			}
			else
			{
				ColorChooserForm.DoColorChooserFormDialog(DialogParent, _config, _game, _s);
			}
		}

		private void PropertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			SyncSettingsChanged = true;
		}

		private void CheckBoxMuted_CheckedChanged(object sender, EventArgs e)
		{
			_s.Muted = ((CheckBox)sender).Checked;
		}
	}
}
