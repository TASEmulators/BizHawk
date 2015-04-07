using System.Windows.Forms;
using BizHawk.Client.Common;
using System.Collections.Generic;
using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : IToolForm
	{
		[RequiredService]
		public IEmulator Emulator { get; private set; }
		[RequiredService]
		public IStatable StatableEmulator { get; private set; }

		private bool _hackyDontUpdate;
		private bool _initializing; // If true, will bypass restart logic, this is necessary since loading projects causes a movie to load which causes a rom to reload causing dialogs to restart

		public bool UpdateBefore { get { return false; } }

		private int lastRefresh = 0;
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

			bool refreshNeeded = false;
			if (AutoadjustInputMenuItem.Checked)
				refreshNeeded = AutoAdjustInput();

			if (TasPlaybackBox.FollowCursor)
				SetVisibleIndex();

			if (TasView.IsPartiallyVisible(Global.Emulator.Frame) || TasView.IsPartiallyVisible(lastRefresh))
				refreshNeeded = true;

			if (refreshNeeded)
				RefreshDialog();
			else if (TasView.RowCount != CurrentTasMovie.InputLogLength + 1) // Perhaps not the best place to put this.
				TasView.RowCount = CurrentTasMovie.InputLogLength + 1;
		}

		public void FastUpdate()
		{
			if (!IsHandleCreated || IsDisposed || CurrentTasMovie == null)
			{
				return;
			}

			TasView.RowCount = CurrentTasMovie.InputLogLength + 1;

			if (TasPlaybackBox.FollowCursor)
			{
				SetVisibleIndex();
			}
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
					TastudioToStopMovie();
					TasView.AllColumns.Clear();
					StartNewTasMovie();
					SetUpColumns();
					RefreshTasView();
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
					SaveTasMenuItem_Click(null, null);
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
