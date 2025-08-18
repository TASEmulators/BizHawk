﻿using BizHawk.Client.Common;
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

		private bool _initializing; // If true, will bypass restart logic, this is necessary since loading projects causes a movie to load which causes a rom to reload causing dialogs to restart

		private int _lastRefresh;
		private bool _doPause;

		private void UpdateProgressBar()
		{
			if (_seekingTo != -1)
			{
				int diff = Emulator.Frame - _seekStartFrame;
				int unit = _seekingTo - _seekStartFrame;
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

		protected override void UpdateBefore()
		{
			if (CurrentTasMovie.IsAtEnd())
			{
				CurrentTasMovie.RecordFrame(CurrentTasMovie.Emulator.Frame, MovieSession.StickySource);
			}
		}

		protected override void FastUpdateBefore() => UpdateBefore();

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

			bool refreshNeeded = TasView.IsPartiallyVisible(Emulator.Frame) ||
				TasView.IsPartiallyVisible(_lastRefresh) ||
				TasView.RowCount != CurrentTasMovie.InputLogLength + 1;
			if (Settings.AutoadjustInput)
			{
				//refreshNeeded = AutoAdjustInput();
			}

			CurrentTasMovie.TasSession.UpdateValues(Emulator.Frame, CurrentTasMovie.Branches.Current);
			MaybeFollowCursor();

			if (Settings.AutoPause && _seekingTo == -1)
			{
				if (_doPause && CurrentTasMovie.IsAtEnd()) MainForm.PauseEmulator();
				_doPause = !CurrentTasMovie.IsAtEnd();
			}

			if (!_seekingByEdit)
			{
				_shouldMoveGreenArrow = true;
			}

			FastUpdateAfter();
			RefreshDialog(refreshNeeded, refreshBranches: false);
		}

		protected override void FastUpdateAfter()
		{
			if (_seekingTo != -1 && Emulator.Frame >= _seekingTo)
			{
				StopSeeking();
			}
			UpdateProgressBar();
		}

		public override void Restart()
		{
			if (!IsActive)
			{
				return;
			}

			if (_initializing)
			{
				return;
			}

			if (CurrentTasMovie != null)
			{
				bool loadRecent = Game.Hash == CurrentTasMovie.Hash && CurrentTasMovie.Filename == Settings.RecentTas.MostRecent;
				TastudioStopMovie();
				// try to load the most recent movie if it matches the currently loaded movie
				if (loadRecent)
				{
					LoadMostRecentOrStartNew();
				}
				else
				{
					StartNewTasMovie();
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
			if (CurrentTasMovie?.Changes is not true) return true;
			var result = DialogController.DoWithTempMute(() => this.ModalMessageBox3(
				caption: "Closing with Unsaved Changes",
				icon: EMsgBoxIcon.Question,
				text: $"Save {WindowTitleStatic} project?"));
			if (result is null) return false;
			if (result.Value) SaveTas();
			else CurrentTasMovie.ClearChanges();
			return true;
		}
	}
}
