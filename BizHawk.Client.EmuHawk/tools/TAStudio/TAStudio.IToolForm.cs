using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IToolForm
	{
		[RequiredService]
		public IEmulator Emulator { get; private set; }

		[RequiredService]
		public IStatable StatableEmulator { get; private set; }

		[RequiredService]
		public IVideoProvider VideoProvider { get; private set; }

		[OptionalService]
		public ISaveRam SaveRamEmulator { get; private set; }

		private bool _hackyDontUpdate;
		private bool _initializing; // If true, will bypass restart logic, this is necessary since loading projects causes a movie to load which causes a rom to reload causing dialogs to restart

		public bool UpdateBefore => false;

		public void NewUpdate(ToolFormUpdateType type) { }

		private int _lastRefresh;

		public void UpdateValues()
		{
			if (!IsHandleCreated || IsDisposed || CurrentTasMovie == null)
			{
				return;
			}

			if (_hackyDontUpdate)
			{
				return;
			}

			if (_exiting)
			{
				return;
			}

			bool refreshNeeded = false;
			if (AutoadjustInputMenuItem.Checked)
			{
				refreshNeeded = AutoAdjustInput();
			}

			CurrentTasMovie.Session.UpdateValues(Emulator.Frame, CurrentTasMovie.Branches.Current);
			MaybeFollowCursor();

			if (TasView.IsPartiallyVisible(Emulator.Frame) || TasView.IsPartiallyVisible(_lastRefresh))
			{
				refreshNeeded = true;
			}

			RefreshDialog(refreshNeeded, refreshBranches: false);
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
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
				if (Global.Game.Hash != CurrentTasMovie.Hash)
				{
					TastudioStopMovie();
					TasView.AllColumns.Clear();
					StartNewTasMovie();
					SetUpColumns();
					TasView.Refresh();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		public bool AskSaveChanges()
		{
			if (_suppressAskSave)
			{
				return true;
			}

			StopSeeking();

			if (CurrentTasMovie != null && CurrentTasMovie.Changes)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show(
					"Save Changes?",
					"Tastudio",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button3);

				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					_exiting = true; // Asking to save changes should only ever be called when closing something
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
