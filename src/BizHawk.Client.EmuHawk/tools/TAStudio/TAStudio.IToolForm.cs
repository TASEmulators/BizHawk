using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[GenEmuServiceProp(typeof(ISaveRam), "SaveRamEmulator", IsOptional = true)]
	public partial class TAStudio : IToolForm
	{
		[RequiredService]
		public IEmulator/*?*/ _maybeEmulator { get; set; } = null;

		[RequiredService]
		public IStatable/*?*/ _maybeStatableEmulator { get; set; } = null;

		[RequiredService]
		public IVideoProvider/*?*/ _maybeVideoProvider { get; set; } = null;

		public IEmulator Emulator
			=> _maybeEmulator!;

		public IStatable StatableEmulator
			=> _maybeStatableEmulator!;

		public IVideoProvider VideoProvider
			=> _maybeVideoProvider!;

		private bool _initializing; // If true, will bypass restart logic, this is necessary since loading projects causes a movie to load which causes a rom to reload causing dialogs to restart

		private int _lastRefresh;

		private void UpdateProgressBar()
		{
			if (MainForm.PauseOnFrame.HasValue)
			{
				int diff = Emulator.Frame - _seekStartFrame.Value;
				int unit = MainForm.PauseOnFrame.Value - _seekStartFrame.Value;
				double progress = 0;

				if (diff != 0 && unit != 0)
				{
					progress = (double)100d / unit * diff;
				}

				if (progress < 0)
				{
					progress = 0;
				}
				else if (progress > 100)
				{
					progress = 100;
				}

				ProgressBar.Value = (int)progress;
			}
			else
			{
				ProgressBar.Visible = false;
				MessageStatusLabel.Text = "";
			}
		}

		protected override void GeneralUpdate()
		{
			RefreshDialog();
		}

		protected override void UpdateAfter()
		{
			if (!IsHandleCreated || IsDisposed || CurrentTasMovie == null)
			{
				return;
			}

			if (_exiting)
			{
				return;
			}

			var refreshNeeded = false;
			if (AutoadjustInputMenuItem.Checked)
			{
				refreshNeeded = AutoAdjustInput();
			}

			CurrentTasMovie.TasSession.UpdateValues(Emulator.Frame, CurrentTasMovie.Branches.Current);
			MaybeFollowCursor();

			if (TasView.IsPartiallyVisible(Emulator.Frame) || TasView.IsPartiallyVisible(_lastRefresh))
			{
				refreshNeeded = true;
			}

			RefreshDialog(refreshNeeded, refreshBranches: false);
			UpdateProgressBar();
		}

		protected override void FastUpdateAfter()
		{
			UpdateProgressBar();
		}

		public override void Restart()
		{
			if (!IsHandleCreated || IsDisposed)
			{
				return;
			}

			if (_initializing)
			{
				return;
			}

			if (CurrentTasMovie != null)
			{
				if (Game.Hash != CurrentTasMovie.Hash)
				{
					TastudioStopMovie();
					TasView.AllColumns.Clear();
					StartNewTasMovie();
					SetUpColumns();
					TasView.Refresh();
				}
				else
				{
					TastudioStopMovie();
					LoadMostRecentOrStartNew();
				}
			}
		}

		/// <summary>
		/// Ask whether changes should be saved. Returns false if cancelled, else true.
		/// </summary>
		public override bool AskSaveChanges()
		{
			if (_suppressAskSave)
			{
				return true;
			}

			StopSeeking();

			if (CurrentTasMovie != null && CurrentTasMovie.Changes)
			{
				var result = MainForm.DoWithTempMute(() => MessageBox.Show(
					"Save Changes?",
					"Tastudio",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button3));
				if (result == DialogResult.Yes)
				{
					SaveTas();
				}
				else if (result == DialogResult.No)
				{
					CurrentTasMovie.ClearChanges();
					return true;
				}
				else if (result == DialogResult.Cancel)
				{
					return false;
				}
			}

			return true;
		}
	}
}
