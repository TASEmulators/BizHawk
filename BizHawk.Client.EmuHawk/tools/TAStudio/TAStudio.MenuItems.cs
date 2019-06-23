using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;
using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio
	{
		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToBk2MenuItem.Enabled =
				!string.IsNullOrWhiteSpace(CurrentTasMovie.Filename) &&
				(CurrentTasMovie.Filename != DefaultTasProjName());

			SaveTASMenuItem.Enabled =
				!string.IsNullOrWhiteSpace(CurrentTasMovie.Filename) &&
				(CurrentTasMovie.Filename != DefaultTasProjName());

			saveSelectionToMacroToolStripMenuItem.Enabled =
				placeMacroAtSelectionToolStripMenuItem.Enabled =
				TasView.AnyRowsSelected;
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				Settings.RecentTas.RecentMenu(DummyLoadProject, true));
		}

		private void NewTasMenuItem_Click(object sender, EventArgs e)
		{
			if (!Mainform.GameIsClosing)
			{
				StartNewTasMovie();
			}
		}

		private void OpenTasMenuItem_Click(object sender, EventArgs e)
		{
			if (AskSaveChanges())
			{
				var filename = CurrentTasMovie.Filename;
				if (string.IsNullOrWhiteSpace(filename) || filename == DefaultTasProjName())
				{
					filename = "";
				}

				// need to be fancy here, so call the ofd constructor directly instead of helper
				var all = $"*.{string.Join(";*.", MovieService.MovieExtensions.Reverse())}";
				var ofd = new OpenFileDialog
				{
					FileName = filename,
					InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
					Filter = string.Format(
						"All Available Files ({0})|{0}|TAS Project Files (*.{1})|*.{1}|Movie Files (*.{2})|*.{2}|All Files|*.*",
						all, TasMovie.Extension, MovieService.DefaultExtension)
				};

				var result = ofd.ShowHawkDialog();
				if (result == DialogResult.OK)
				{
					if (ofd.FileName.EndsWith(TasMovie.Extension))
					{
						LoadFile(new FileInfo(ofd.FileName));
					}
					else if (ofd.FileName.EndsWith(".bkm") || ofd.FileName.EndsWith(".bk2")) // todo: proper extention iteration
					{
						var result1 = MessageBox.Show("This is a regular movie, a new project must be created from it, in order to use in TAStudio\nProceed?", "Convert movie", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
						if (result1 == DialogResult.OK)
						{
							Mainform.StartNewMovie(MovieService.Get(ofd.FileName), false);
							ConvertCurrentMovieToTasproj();
							StartNewMovieWrapper(false);
							SetUpColumns();
							SetTextProperty();
						}
					}
					else
					{
						MessageBox.Show("This is not a BizHawk movie!", "Movie load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private bool _exiting;

		private void SaveTas(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(CurrentTasMovie.Filename)
				|| CurrentTasMovie.Filename == DefaultTasProjName())
			{
				SaveAsTas(sender, e);
			}
			else
			{
				_autosaveTimer.Stop();
				GlobalWin.Sound.StopSound();
				MessageStatusLabel.Text = "Saving...";
				Cursor = Cursors.WaitCursor;
				Update();
				CurrentTasMovie.Save();
				if (Settings.AutosaveInterval > 0)
				{
					_autosaveTimer.Start();
				}

				MessageStatusLabel.Text = $"{CurrentTasMovie.Name} saved.";
				Settings.RecentTas.Add(CurrentTasMovie.Filename);
				Cursor = Cursors.Default;
				GlobalWin.Sound.StartSound();
			}
		}

		// call this one from the menu only
		private void SaveTasMenuItem_Click(object sender, EventArgs e)
		{
			SaveTas(sender, e);
			if (Settings.BackupPerFileSave)
			{
				SaveBackupMenuItem_Click(sender, e);
			}
		}

		private void SaveAsTas(object sender, EventArgs e)
		{
			_autosaveTimer.Stop();
			GlobalWin.Sound.StopSound();
			ClearLeftMouseStates();
			var filename = CurrentTasMovie.Filename;
			if (string.IsNullOrWhiteSpace(filename) || filename == DefaultTasProjName())
			{
				filename = SuggestedTasProjName();
			}

			var file = SaveFileDialog(
				filename,
				PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
				"Tas Project Files",
				"tasproj");

			if (file != null)
			{
				CurrentTasMovie.Filename = file.FullName;
				MessageStatusLabel.Text = "Saving...";
				Cursor = Cursors.WaitCursor;
				Update();
				CurrentTasMovie.Save();
				Settings.RecentTas.Add(CurrentTasMovie.Filename);
				SetTextProperty();
				MessageStatusLabel.Text = $"{Path.GetFileName(CurrentTasMovie.Filename)} saved.";
				Cursor = Cursors.Default;
			}

			// keep insisting
			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Start();
			}

			Mainform.SetWindowText();
			GlobalWin.Sound.StartSound();
		}

		// call this one from the menu only
		private void SaveAsTasMenuItem_Click(object sender, EventArgs e)
		{
			SaveAsTas(sender, e);
			if (Settings.BackupPerFileSave)
			{
				SaveBackupMenuItem_Click(sender, e);
			}
		}

		private void SaveBackupMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(CurrentTasMovie.Filename)
				|| CurrentTasMovie.Filename == DefaultTasProjName())
			{
				SaveAsTas(sender, e);
			}
			else
			{
				_autosaveTimer.Stop();
				GlobalWin.Sound.StopSound();
				MessageStatusLabel.Text = "Saving...";
				Cursor = Cursors.WaitCursor;
				Update();
				CurrentTasMovie.SaveBackup();
				if (Settings.AutosaveInterval > 0)
				{
					_autosaveTimer.Start();
				}

				MessageStatusLabel.Text = "Backup .tasproj saved to \"Movie backups\" path.";
				Settings.RecentTas.Add(CurrentTasMovie.Filename);
				Cursor = Cursors.Default;
				GlobalWin.Sound.StartSound();
			}
		}

		private void SaveBk2BackupMenuItem_Click(object sender, EventArgs e)
		{
			_autosaveTimer.Stop();
			var bk2 = CurrentTasMovie.ToBk2(copy: true, backup: true);
			MessageStatusLabel.Text = "Exporting to .bk2...";
			Cursor = Cursors.WaitCursor;
			Update();
			bk2.SaveBackup();
			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Start();
			}

			MessageStatusLabel.Text = "Backup .bk2 saved to \"Movie backups\" path.";
			Cursor = Cursors.Default;
		}

		private void SaveSelectionToMacroMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.LastSelectedIndex == CurrentTasMovie.InputLogLength)
			{
				TasView.SelectRow(CurrentTasMovie.InputLogLength, false);
			}

			if (!TasView.AnyRowsSelected)
			{
				return;
			}

			MovieZone macro = new MovieZone(CurrentTasMovie, TasView.FirstSelectedIndex.Value,
				TasView.LastSelectedIndex.Value - TasView.FirstSelectedIndex.Value + 1);
			MacroInputTool.SaveMacroAs(macro);
		}

		private void PlaceMacroAtSelectionMenuItem_Click(object sender, EventArgs e)
		{
			if (!TasView.AnyRowsSelected)
			{
				return;
			}

			MovieZone macro = MacroInputTool.LoadMacro();
			if (macro != null)
			{
				macro.Start = TasView.FirstSelectedIndex.Value;
				macro.PlaceZone(CurrentTasMovie);
			}
		}

		private void RecentMacrosMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			recentMacrosToolStripMenuItem.DropDownItems.Clear();
			recentMacrosToolStripMenuItem.DropDownItems.AddRange(Global.Config.RecentMacros.RecentMenu(DummyLoadMacro));
		}

		private void ToBk2MenuItem_Click(object sender, EventArgs e)
		{
			_autosaveTimer.Stop();
			var bk2 = CurrentTasMovie.ToBk2(true, true);
			MessageStatusLabel.Text = "Exporting to .bk2...";
			Cursor = Cursors.WaitCursor;
			Update();
			string d_exp = " not exported.";
			var file = new FileInfo(bk2.Filename);
			if (file.Exists)
			{
				GlobalWin.Sound.StopSound();
				var result = MessageBox.Show(
					"Overwrite Existing File?",
					"Tastudio",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button3);

				GlobalWin.Sound.StartSound();
				if (result == DialogResult.Yes)
				{
					bk2.Save();
					d_exp = " exported.";
				}
			}
			else
			{
				bk2.Save();
				d_exp = " exported.";
			}

			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Start();
			}

			MessageStatusLabel.Text = bk2.Name + d_exp;
			Cursor = Cursors.Default;
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Edit

		private void EditSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DeselectMenuItem.Enabled =
				SelectBetweenMarkersMenuItem.Enabled =
				CopyMenuItem.Enabled =
				CutMenuItem.Enabled =
				ClearFramesMenuItem.Enabled =
				DeleteFramesMenuItem.Enabled =
				CloneFramesMenuItem.Enabled =
				TruncateMenuItem.Enabled =
				InsertFrameMenuItem.Enabled =
				InsertNumFramesMenuItem.Enabled = 
				TasView.AnyRowsSelected;

			ReselectClipboardMenuItem.Enabled =
				PasteMenuItem.Enabled =
				PasteInsertMenuItem.Enabled =
				Clipboard.GetDataObject().GetDataPresent(DataFormats.StringFormat) && TasView.AnyRowsSelected;

			ClearGreenzoneMenuItem.Enabled =
				CurrentTasMovie != null && CurrentTasMovie.TasStateManager.Any();

			GreenzoneICheckSeparator.Visible =
				StateHistoryIntegrityCheckMenuItem.Visible =
				VersionInfo.DeveloperBuild;

			ClearFramesMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Clear Frames"].Bindings;
			InsertFrameMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Insert Frame"].Bindings;
			DeleteFramesMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Delete Frames"].Bindings;
			CloneFramesMenuItem.ShortcutKeyDisplayString = Global.Config.HotkeyBindings["Clone Frames"].Bindings;
		}

		public void ClearFramesExternal()
		{
			ClearFramesMenuItem_Click(null, null);
		}

		public void InsertFrameExternal()
		{
			InsertFrameMenuItem_Click(null, null);
		}

		public void DeleteFramesExternal()
		{
			DeleteFramesMenuItem_Click(null, null);
		}

		public void CloneFramesExternal()
		{
			CloneFramesMenuItem_Click(null, null);
		}

		private void UndoMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentTasMovie.ChangeLog.Undo() < Emulator.Frame)
			{
				GoToFrame(CurrentTasMovie.ChangeLog.PreviousUndoFrame);
			}
			else
			{
				RefreshDialog();
			}

			// Currently I don't have a way to easily detect when CanUndo changes, so this button should be enabled always.
			// UndoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanUndo;
			RedoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanRedo;
		}

		private void RedoMenuItem_Click(object sender, EventArgs e)
		{
			if (CurrentTasMovie.ChangeLog.Redo() < Emulator.Frame)
			{
				GoToFrame(CurrentTasMovie.ChangeLog.PreviousRedoFrame);
			}
			else
			{
				RefreshDialog();
			}

			// Currently I don't have a way to easily detect when CanUndo changes, so this button should be enabled always.
			// UndoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanUndo;
			RedoMenuItem.Enabled = CurrentTasMovie.ChangeLog.CanRedo;
		}

		private void ShowUndoHistoryMenuItem_Click(object sender, EventArgs e)
		{
			_undoForm = new UndoHistoryForm(this) { Owner = this };
			_undoForm.Show();
			_undoForm.UpdateValues();
		}

		private void DeselectMenuItem_Click(object sender, EventArgs e)
		{
			TasView.DeselectAll();
			RefreshTasView();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			TasView.SelectAll();
			RefreshTasView();
		}

		private void SelectBetweenMarkersMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var prevMarker = CurrentTasMovie.Markers.PreviousOrCurrent(TasView.LastSelectedIndex.Value);
				var nextMarker = CurrentTasMovie.Markers.Next(TasView.LastSelectedIndex.Value);

				int prev = prevMarker?.Frame ?? 0;
				int next = nextMarker?.Frame ?? CurrentTasMovie.InputLogLength;

				for (int i = prev; i < next; i++)
				{
					TasView.SelectRow(i, true);
				}

				SetSplicer();
				RefreshTasView();
			}
		}

		private void ReselectClipboardMenuItem_Click(object sender, EventArgs e)
		{
			TasView.DeselectAll();
			foreach (var item in _tasClipboard)
			{
				TasView.SelectRow(item.Frame, true);
			}

			SetSplicer();
			RefreshTasView();
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				_tasClipboard.Clear();
				var list = TasView.SelectedRows.ToArray();
				var sb = new StringBuilder();

				foreach (var index in list)
				{
					var input = CurrentTasMovie.GetInputState(index);
					if (input == null)
					{
						break;
					}

					_tasClipboard.Add(new TasClipboardEntry(index, input));
					var lg = CurrentTasMovie.LogGeneratorInstance();
					lg.SetSource(input);
					sb.AppendLine(lg.GenerateLogEntry());
				}

				Clipboard.SetDataObject(sb.ToString());
				SetSplicer();
			}
		}

		private void PasteMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				// TODO: if highlighting 2 rows and pasting 3, only paste 2 of them
				// FCEUX Taseditor does't do this, but I think it is the expected behavior in editor programs
				var wasPaused = Mainform.EmulatorPaused;

				// copypaste from PasteInsertMenuItem_Click!
				IDataObject data = Clipboard.GetDataObject();
				if (data.GetDataPresent(DataFormats.StringFormat))
				{
					string input = (string)data.GetData(DataFormats.StringFormat);
					if (!string.IsNullOrWhiteSpace(input))
					{
						string[] lines = input.Split('\n');
						if (lines.Length > 0)
						{
							_tasClipboard.Clear();
							for (int i = 0; i < lines.Length - 1; i++)
							{
								var line = TasClipboardEntry.SetFromMnemonicStr(lines[i]);
								if (line == null)
								{
									return;
								}
								else
								{
									_tasClipboard.Add(new TasClipboardEntry(i, line));
								}
							}

							var needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;
							CurrentTasMovie.CopyOverInput(TasView.FirstSelectedIndex.Value, _tasClipboard.Select(x => x.ControllerState));
							if (needsToRollback)
							{
								GoToLastEmulatedFrameIfNecessary(TasView.FirstSelectedIndex.Value);
								DoAutoRestore();
							}
							else
							{
								RefreshDialog();
							}
						}
					}
				}
			}
		}

		private void PasteInsertMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var wasPaused = Mainform.EmulatorPaused;

				// copypaste from PasteMenuItem_Click!
				IDataObject data = Clipboard.GetDataObject();
				if (data.GetDataPresent(DataFormats.StringFormat))
				{
					string input = (string)data.GetData(DataFormats.StringFormat);
					if (!string.IsNullOrWhiteSpace(input))
					{
						string[] lines = input.Split('\n');
						if (lines.Length > 0)
						{
							_tasClipboard.Clear();
							for (int i = 0; i < lines.Length - 1; i++)
							{
								var line = TasClipboardEntry.SetFromMnemonicStr(lines[i]);
								if (line == null)
								{
									return;
								}
								else
								{
									_tasClipboard.Add(new TasClipboardEntry(i, line));
								}
							}

							var needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;
							CurrentTasMovie.InsertInput(TasView.FirstSelectedIndex.Value, _tasClipboard.Select(x => x.ControllerState));
							if (needsToRollback)
							{
								GoToLastEmulatedFrameIfNecessary(TasView.FirstSelectedIndex.Value);
								DoAutoRestore();
							}
							else
							{
								RefreshDialog();
							}
						}
					}
				}
			}
		}

		private void CutMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var wasPaused = Mainform.EmulatorPaused;
				var needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;
				var rollBackFrame = TasView.FirstSelectedIndex.Value;

				_tasClipboard.Clear();
				var list = TasView.SelectedRows.ToArray();
				var sb = new StringBuilder();

				foreach (var index in list) // copy of CopyMenuItem_Click()
				{
					var input = CurrentTasMovie.GetInputState(index);
					if (input == null)
					{
						break;
					}

					_tasClipboard.Add(new TasClipboardEntry(index, input));
					var lg = CurrentTasMovie.LogGeneratorInstance();
					lg.SetSource(input);
					sb.AppendLine(lg.GenerateLogEntry());
				}

				Clipboard.SetDataObject(sb.ToString());
				CurrentTasMovie.RemoveFrames(list);
				SetSplicer();
				////TasView.DeselectAll(); feos: what if I want to continuously cut?

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void ClearFramesMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				bool wasPaused = Mainform.EmulatorPaused;
				bool needsToRollback = !(TasView.FirstSelectedIndex > Emulator.Frame);
				int rollBackFrame = TasView.FirstSelectedIndex.Value;

				CurrentTasMovie.ChangeLog.BeginNewBatch($"Clear frames {TasView.SelectedRows.Min()}-{TasView.SelectedRows.Max()}");
				foreach (int frame in TasView.SelectedRows)
				{
					CurrentTasMovie.ClearFrame(frame);
				}

				CurrentTasMovie.ChangeLog.EndBatch();

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void DeleteFramesMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;
				var rollBackFrame = TasView.FirstSelectedIndex.Value;
				if (rollBackFrame >= CurrentTasMovie.InputLogLength)
				{
					// Cannot delete non-existant frames
					RefreshDialog();
					return;
				}

				CurrentTasMovie.RemoveFrames(TasView.SelectedRows.ToArray());
				SetSplicer();

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void CloneFramesMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var wasPaused = Mainform.EmulatorPaused;
				var framesToInsert = TasView.SelectedRows;
				var insertionFrame = Math.Min(TasView.LastSelectedIndex.Value + 1, CurrentTasMovie.InputLogLength);
				var needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;

				var inputLog = framesToInsert
					.Select(frame => CurrentTasMovie.GetInputLogEntry(frame));

				CurrentTasMovie.InsertInput(insertionFrame, inputLog);

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(insertionFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void InsertFrameMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var wasPaused = Mainform.EmulatorPaused;
				var insertionFrame = TasView.AnyRowsSelected ? TasView.FirstSelectedIndex.Value : 0;
				var needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;

				CurrentTasMovie.InsertEmptyFrame(insertionFrame);

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(insertionFrame);
					DoAutoRestore();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void InsertNumFramesMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				int insertionFrame = TasView.FirstSelectedIndex.Value;
				var framesPrompt = new FramesPrompt();
				DialogResult result = framesPrompt.ShowDialog();
				if (result == DialogResult.OK)
				{
					InsertNumFrames(insertionFrame, framesPrompt.Frames);
				}
			}
		}

		private void TruncateMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.AnyRowsSelected)
			{
				var rollbackFrame = TasView.LastSelectedIndex.Value;
				var needsToRollback = TasView.FirstSelectedIndex < Emulator.Frame;

				CurrentTasMovie.Truncate(rollbackFrame);
				MarkerControl.MarkerInputRoll.TruncateSelection(CurrentTasMovie.Markers.Count - 1);

				if (needsToRollback)
				{
					GoToFrame(rollbackFrame);
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void SetMarkersMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedRows.Count() > 50)
			{
				var result = MessageBox.Show("Are you sure you want to add more than 50 markers?", "Add markers", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if (result != DialogResult.OK)
				{
					return;
				}
			}

			foreach (var index in TasView.SelectedRows)
			{
				MarkerControl.AddMarker(false, index);
			}
		}

		private void SetMarkerWithTextMenuItem_Click(object sender, EventArgs e)
		{
			MarkerControl.AddMarker(true, TasView.SelectedRows.FirstOrDefault());
		}

		private void RemoveMarkersMenuItem_Click(object sender, EventArgs e)
		{
			CurrentTasMovie.Markers.RemoveAll(m => TasView.SelectedRows.Contains(m.Frame));
			MarkerControl.ShrinkSelection();
			RefreshDialog();
		}

		private void ClearGreenzoneMenuItem_Click(object sender, EventArgs e)
		{
			CurrentTasMovie.ClearGreenzone();
			RefreshDialog();
		}

		private void StateHistoryIntegrityCheckMenuItem_Click(object sender, EventArgs e)
		{
			if (!Emulator.DeterministicEmulation)
			{
				if (MessageBox.Show("The emulator is not deterministic. It might fail even if the difference isn't enough to cause a desync.\nContinue with check?", "Not Deterministic", MessageBoxButtons.YesNo) == DialogResult.No)
				{
					return;
				}
			}

			GoToFrame(0);
			int lastState = 0;
			int goToFrame = CurrentTasMovie.LastStatedFrame;
			do
			{
				Mainform.FrameAdvance();

				if (CurrentTasMovie.TasStateManager.HasState(Emulator.Frame))
				{
					byte[] state = (byte[])StatableEmulator.SaveStateBinary().Clone(); // Why is this cloning it?
					byte[] greenzone = CurrentTasMovie.TasStateManager[Emulator.Frame].Value;

					if (!state.SequenceEqual(greenzone))
					{
						MessageBox.Show($"Bad data between frames {lastState} and {Emulator.Frame}");
						return;
					}

					lastState = Emulator.Frame;
				}
			}
			while (Emulator.Frame < goToFrame);

			MessageBox.Show("Integrity Check passed");
		}

		#endregion

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DrawInputByDraggingMenuItem.Checked = Settings.DrawInput;
			AutopauseAtEndOfMovieMenuItem.Checked = Settings.AutoPause;
			AutoRestoreOnMouseUpOnlyMenuItem.Checked = Settings.AutoRestoreOnMouseUpOnly;
			EmptyNewMarkerNotesMenuItem.Checked = Settings.EmptyMarkers;
			AutosaveAsBk2MenuItem.Checked = Settings.AutosaveAsBk2;
			AutosaveAsBackupFileMenuItem.Checked = Settings.AutosaveAsBackupFile;
			BackupPerFileSaveMenuItem.Checked = Settings.BackupPerFileSave;
			SingleClickFloatEditMenuItem.Checked = Settings.SingleClickFloatEdit;
			OldControlSchemeForBranchesMenuItem.Checked = Settings.OldControlSchemeForBranches;
			LoadBranchOnDoubleclickMenuItem.Checked = Settings.LoadBranchOnDoubleClick;
			BindMarkersToInputMenuItem.Checked = CurrentTasMovie.BindMarkersToInput;
		}

		private void SetMaxUndoLevelsMenuItem_Click(object sender, EventArgs e)
		{
			using (var prompt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Number of Undo Levels to keep",
				InitialValue = CurrentTasMovie.ChangeLog.MaxSteps.ToString()
			})
			{
				DialogResult result = prompt.ShowDialog();
				if (result == DialogResult.OK)
				{
					int val = 0;
					try
					{
						val = int.Parse(prompt.PromptText);
					}
					catch
					{
						MessageBox.Show("Invalid Entry.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}

					if (val > 0)
					{
						CurrentTasMovie.ChangeLog.MaxSteps = val;
					}
				}
			}
		}

		private void SetBranchCellHoverIntervalMenuItem_Click(object sender, EventArgs e)
		{
			using (var prompt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "ScreenshotPopUp Delay",
				InitialValue = Settings.BranchCellHoverInterval.ToString()
			})
			{
				DialogResult result = prompt.ShowDialog();
				if (result == DialogResult.OK)
				{
					int val = int.Parse(prompt.PromptText);
					if (val > 0)
					{
						Settings.BranchCellHoverInterval = val;
						BookMarkControl.HoverInterval = val;
					}
				}
			}
		}

		private void SetSeekingCutoffIntervalMenuItem_Click(object sender, EventArgs e)
		{
			using (var prompt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Seeking Cutoff Interval",
				InitialValue = Settings.SeekingCutoffInterval.ToString()
			})
			{
				DialogResult result = prompt.ShowDialog();
				if (result == DialogResult.OK)
				{
					int val = int.Parse(prompt.PromptText);
					if (val > 0)
					{
						Settings.SeekingCutoffInterval = val;
						TasView.SeekingCutoffInterval = val;
					}
				}
			}
		}

		private void SetAutosaveIntervalMenuItem_Click(object sender, EventArgs e)
		{
			using (var prompt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Autosave Interval in seconds\nSet to 0 to disable",
				InitialValue = (Settings.AutosaveInterval / 1000).ToString()
			})
			{
				DialogResult result = prompt.ShowDialog();
				if (result == DialogResult.OK)
				{
					uint val = uint.Parse(prompt.PromptText) * 1000;
					Settings.AutosaveInterval = val;
					if (val > 0)
					{
						_autosaveTimer.Interval = (int)val;
						_autosaveTimer.Start();
					}
				}
			}
		}

		private void AutosaveAsBk2MenuItem_Click(object sender, EventArgs e)
		{
			Settings.AutosaveAsBk2 ^= true;
		}

		private void AutosaveAsBackupFileMenuItem_Click(object sender, EventArgs e)
		{
			Settings.AutosaveAsBackupFile ^= true;
		}

		private void BackupPerFileSaveMenuItem_Click(object sender, EventArgs e)
		{
			Settings.BackupPerFileSave ^= true;
		}

		private void DrawInputByDraggingMenuItem_Click(object sender, EventArgs e)
		{
			TasView.InputPaintingMode = Settings.DrawInput ^= true;
		}

		private void ApplyPatternToPaintedInputMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			onlyOnAutoFireColumnsToolStripMenuItem.Enabled = applyPatternToPaintedInputToolStripMenuItem.Checked;
		}

		private void SingleClickFloatEditMenuItem_Click(object sender, EventArgs e)
		{
			Settings.SingleClickFloatEdit ^= true;
		}

		private void BindMarkersToInputMenuItem_Click(object sender, EventArgs e)
		{
			CurrentTasMovie.BindMarkersToInput = BindMarkersToInputMenuItem.Checked;
		}

		private void EmptyNewMarkerNotesMenuItem_Click(object sender, EventArgs e)
		{
			Settings.EmptyMarkers ^= true;
		}

		private void AutopauseAtEndMenuItem_Click(object sender, EventArgs e)
		{
			Settings.AutoPause ^= true;
		}

		private void AutoRestoreOnMouseUpOnlyMenuItem_Click(object sender, EventArgs e)
		{
			Settings.AutoRestoreOnMouseUpOnly ^= true;
		}

		private void AutoHoldMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (autoHoldToolStripMenuItem.Checked)
			{
				autoFireToolStripMenuItem.Checked = false;
				customPatternToolStripMenuItem.Checked = false;

				if (!keepSetPatternsToolStripMenuItem.Checked)
				{
					UpdateAutoFire();
				}
			}
		}

		private void AutoFireMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (autoFireToolStripMenuItem.Checked)
			{
				autoHoldToolStripMenuItem.Checked = false;
				customPatternToolStripMenuItem.Checked = false;

				if (!keepSetPatternsToolStripMenuItem.Checked)
				{
					UpdateAutoFire();
				}
			}
		}

		private void CustomPatternMenuItem_CheckedChanged(object sender, EventArgs e)
		{
			if (customPatternToolStripMenuItem.Checked)
			{
				autoHoldToolStripMenuItem.Checked = false;
				autoFireToolStripMenuItem.Checked = false;

				if (!keepSetPatternsToolStripMenuItem.Checked)
				{
					UpdateAutoFire();
				}
			}
		}

		private void SetCustomsMenuItem_Click(object sender, EventArgs e)
		{
			// Exceptions in PatternsForm are not caught by the debugger, I have no idea why.
			// Exceptions in UndoForm are caught, which makes it weirder.
			var pForm = new PatternsForm(this) { Owner = this };
			pForm.Show();
		}

		private void OldControlSchemeForBranchesMenuItem_Click(object sender, EventArgs e)
		{
			Settings.OldControlSchemeForBranches ^= true;
		}

		private void LoadBranchOnDoubleclickMenuItem_Click(object sender, EventArgs e)
		{
			Settings.LoadBranchOnDoubleClick ^= true;
		}

		#endregion

		#region Metadata

		private void HeaderMenuItem_Click(object sender, EventArgs e)
		{
			new MovieHeaderEditor(CurrentTasMovie)
			{
				Owner = Mainform,
				Location = this.ChildPointToScreen(TasView)
			}.Show();
			UpdateChangesIndicator();
		}

		private void StateHistorySettingsMenuItem_Click(object sender, EventArgs e)
		{
			new StateHistorySettingsForm(CurrentTasMovie.TasStateManager.Settings)
			{
				Owner = Mainform,
				Location = this.ChildPointToScreen(TasView),
				Statable = this.StatableEmulator
			}.ShowDialog();
			CurrentTasMovie.TasStateManager.UpdateStateFrequency();
			CurrentTasMovie.TasStateManager.LimitStateCount();
			UpdateChangesIndicator();
		}

		private void CommentsMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditCommentsForm();
			form.GetMovie(CurrentTasMovie);
			form.ForceReadWrite = true;
			form.Show();
		}

		private void SubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditSubtitlesForm { ReadOnly = false };
			form.GetMovie(Global.MovieSession.Movie);
			form.ShowDialog();
		}

		private void DefaultStateSettingsMenuItem_Click(object sender, EventArgs e)
		{
			new DefaultGreenzoneSettings
			{
				Location = this.ChildPointToScreen(TasView)
			}.ShowDialog();
		}

		#endregion

		#region Settings Menu

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RotateMenuItem.ShortcutKeyDisplayString = TasView.RotateHotkeyStr;
		}

		private void HideLagFramesSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			HideLagFrames0.Checked = TasView.LagFramesToHide == 0;
			HideLagFrames1.Checked = TasView.LagFramesToHide == 1;
			HideLagFrames2.Checked = TasView.LagFramesToHide == 2;
			HideLagFrames3.Checked = TasView.LagFramesToHide == 3;
			hideWasLagFramesToolStripMenuItem.Checked = TasView.HideWasLagFrames;
		}

		private void IconsMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			DenoteStatesWithIconsToolStripMenuItem.Checked = Settings.DenoteStatesWithIcons;
			DenoteStatesWithBGColorToolStripMenuItem.Checked = Settings.DenoteStatesWithBGColor;
			DenoteMarkersWithIconsToolStripMenuItem.Checked = Settings.DenoteMarkersWithIcons;
			DenoteMarkersWithBGColorToolStripMenuItem.Checked = Settings.DenoteMarkersWithBGColor;
		}

		private void FollowCursorMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			alwaysScrollToolStripMenuItem.Checked = Settings.FollowCursorAlwaysScroll;
			scrollToViewToolStripMenuItem.Checked = false;
			scrollToTopToolStripMenuItem.Checked = false;
			scrollToBottomToolStripMenuItem.Checked = false;
			scrollToCenterToolStripMenuItem.Checked = false;
			if (TasView.ScrollMethod == "near")
			{
				scrollToViewToolStripMenuItem.Checked = true;
			}
			else if (TasView.ScrollMethod == "top")
			{
				scrollToTopToolStripMenuItem.Checked = true;
			}
			else if (TasView.ScrollMethod == "bottom")
			{
				scrollToBottomToolStripMenuItem.Checked = true;
			}
			else
			{
				scrollToCenterToolStripMenuItem.Checked = true;
			}
		}

		private void RotateMenuItem_Click(object sender, EventArgs e)
		{
			TasView.HorizontalOrientation ^= true;
			CurrentTasMovie.FlagChanges();
		}

		private void HideLagFramesX_Click(object sender, EventArgs e)
		{
			TasView.LagFramesToHide = (int)(sender as ToolStripMenuItem).Tag;
			MaybeFollowCursor();
			RefreshDialog();
		}

		private void HideWasLagFramesMenuItem_Click(object sender, EventArgs e)
		{
			TasView.HideWasLagFrames ^= true;
		}
		
		private void AlwaysScrollMenuItem_Click(object sender, EventArgs e)
		{
			TasView.AlwaysScroll = Settings.FollowCursorAlwaysScroll = alwaysScrollToolStripMenuItem.Checked;
		}
		
		private void ScrollToViewMenuItem_Click(object sender, EventArgs e)
		{
			TasView.ScrollMethod = Settings.FollowCursorScrollMethod = "near";
		}
		
		private void ScrollToTopMenuItem_Click(object sender, EventArgs e)
		{
			TasView.ScrollMethod = Settings.FollowCursorScrollMethod = "top";
		}

		private void ScrollToBottomMenuItem_Click(object sender, EventArgs e)
		{
			TasView.ScrollMethod = Settings.FollowCursorScrollMethod = "bottom";
		}

		private void ScrollToCenterMenuItem_Click(object sender, EventArgs e)
		{
			TasView.ScrollMethod = Settings.FollowCursorScrollMethod = "center";
		}

		private void DenoteStatesWithIconsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteStatesWithIcons = DenoteStatesWithIconsToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void DenoteStatesWithBGColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteStatesWithBGColor = DenoteStatesWithBGColorToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void DenoteMarkersWithIconsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteMarkersWithIcons = DenoteMarkersWithIconsToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void DenoteMarkersWithBGColorToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Settings.DenoteMarkersWithBGColor = DenoteMarkersWithBGColorToolStripMenuItem.Checked;
			RefreshDialog();
		}

		private void WheelScrollSpeedMenuItem_Click(object sender, EventArgs e)
		{
			var inputpromt = new InputPrompt
			{
				TextInputType = InputPrompt.InputType.Unsigned,
				Message = "Frames per tick:",
				InitialValue = TasView.ScrollSpeed.ToString()
			};
			if (inputpromt.ShowDialog() == DialogResult.OK)
			{
				TasView.ScrollSpeed = int.Parse(inputpromt.PromptText);
				Settings.ScrollSpeed = TasView.ScrollSpeed;
			}
		}

		#endregion

		#region Columns

		private void SetUpToolStripColumns()
		{
			ColumnsSubMenu.DropDownItems.Clear();

			var columns = TasView.AllColumns
				.Where(x => !string.IsNullOrWhiteSpace(x.Text))
				.Where(x => x.Name != "FrameColumn");

			int workingHeight = Screen.FromControl(this).WorkingArea.Height;
			int rowHeight = ColumnsSubMenu.Height + 4;
			int maxRows = workingHeight / rowHeight;
			int keyCount = columns.Where(c => c.Name.StartsWith("Key ")).Count();
			int keysMenusCount = (int)Math.Ceiling((double)keyCount / maxRows);

			ToolStripMenuItem[] keysMenus = new ToolStripMenuItem[keysMenusCount];

			for (int i = 0; i < keysMenus.Length; i++)
			{
				keysMenus[i] = new ToolStripMenuItem();
			}

			ToolStripMenuItem[] playerMenus = new ToolStripMenuItem[Emulator.ControllerDefinition.PlayerCount + 1];
			playerMenus[0] = ColumnsSubMenu;

			for (int i = 1; i < playerMenus.Length; i++)
			{
				playerMenus[i] = new ToolStripMenuItem($"Player {i}");
			}

			foreach (InputRoll.RollColumn column in columns)
			{
				ToolStripMenuItem menuItem = new ToolStripMenuItem
				{
					Text = $"{column.Text} ({column.Name})",
					Checked = column.Visible,
					CheckOnClick = true,
					Tag = column.Name
				};

				menuItem.CheckedChanged += (o, ev) =>
				{
					ToolStripMenuItem sender = o as ToolStripMenuItem;
					TasView.AllColumns.Find(c => c.Name == (string)sender.Tag).Visible = sender.Checked;
					TasView.AllColumns.ColumnsChanged();
					CurrentTasMovie.FlagChanges();
					RefreshTasView();
					ColumnsSubMenu.ShowDropDown();
					(sender.OwnerItem as ToolStripMenuItem).ShowDropDown();
				};

				if (column.Name.StartsWith("Key "))
				{
					keysMenus
						.Where(m => m.DropDownItems.Count < maxRows)
						.FirstOrDefault()
						.DropDownItems
						.Add(menuItem);
				}
				else
				{
					int player;

					if (column.Name.StartsWith("P") && column.Name.Length > 1 && char.IsNumber(column.Name, 1))
					{
						player = int.Parse(column.Name[1].ToString());
					}
					else
					{
						player = 0;
					}

					playerMenus[player].DropDownItems.Add(menuItem);
				}
			}

			for (int i = 0; i < keysMenus.Length; i++)
			{
				string text = $"Keys ({keysMenus[i].DropDownItems[0].Tag} - {keysMenus[i].DropDownItems[keysMenus[i].DropDownItems.Count - 1].Tag})";
				keysMenus[i].Text = text.Replace("Key ", "");
				ColumnsSubMenu.DropDownItems.Add(keysMenus[i]);
			}

			for (int i = 1; i < playerMenus.Length; i++)
			{
				if (playerMenus[i].HasDropDownItems)
				{
					ColumnsSubMenu.DropDownItems.Add(playerMenus[i]);
				}
			}

			for (int i = 1; i < playerMenus.Length; i++)
			{
				if (playerMenus[i].HasDropDownItems)
				{
					ColumnsSubMenu.DropDownItems.Add(new ToolStripSeparator());
					break;
				}
			}

			if (keysMenus.Length > 0)
			{
				var item = new ToolStripMenuItem("Show Keys")
				{
					CheckOnClick = true,
					Checked = false
				};

				for (int i = 0; i < keysMenus.Length; i++)
				{
					ToolStripMenuItem dummyObject = keysMenus[i];
					item.CheckedChanged += (o, ev) =>
					{
						ToolStripMenuItem sender = o as ToolStripMenuItem;
						foreach (ToolStripMenuItem menuItem in dummyObject.DropDownItems)
						{
							menuItem.Checked ^= true;
						}

						CurrentTasMovie.FlagChanges();
						TasView.AllColumns.ColumnsChanged();
						RefreshTasView();
					};

					ColumnsSubMenu.DropDownItems.Add(item);
				}
			}

			for (int i = 1; i < playerMenus.Length; i++)
			{
				if (playerMenus[i].HasDropDownItems)
				{
					var item = new ToolStripMenuItem($"Show Player {i}")
					{
						CheckOnClick = true,
						Checked = true
					};

					ToolStripMenuItem dummyObject = playerMenus[i];
					item.CheckedChanged += (o, ev) =>
					{
						ToolStripMenuItem sender = o as ToolStripMenuItem;
						foreach (ToolStripMenuItem menuItem in dummyObject.DropDownItems)
						{
							menuItem.Checked ^= true;
						}

						CurrentTasMovie.FlagChanges();
						TasView.AllColumns.ColumnsChanged();
						RefreshTasView();
					};

					ColumnsSubMenu.DropDownItems.Add(item);
				}
			}

			ColumnsSubMenu.DropDownItems.Add(new ToolStripSeparator());
			var defaults = new ToolStripMenuItem
			{
				Name = "RestoreDefaultColumnConfiguration",
				Text = "Restore defaults"
			};

			defaults.Click += (o, ev) =>
			{
				TasView.AllColumns.Clear();
				SetUpColumns();
				RefreshTasView();
				CurrentTasMovie.FlagChanges();
			};

			ColumnsSubMenu.DropDownItems.Add(defaults);
			TasView.AllColumns.ColumnsChanged();
		}

		#endregion

		#region Context Menu

		private void RightClickMenu_Opened(object sender, EventArgs e)
		{
			SetMarkersContextMenuItem.Enabled =
				SelectBetweenMarkersContextMenuItem.Enabled =
				RemoveMarkersContextMenuItem.Enabled =
				DeselectContextMenuItem.Enabled =
				ClearContextMenuItem.Enabled =
				DeleteFramesContextMenuItem.Enabled =
				CloneContextMenuItem.Enabled =
				InsertFrameContextMenuItem.Enabled =
				InsertNumFramesContextMenuItem.Enabled =
				TruncateContextMenuItem.Enabled =
				TasView.AnyRowsSelected;

			pasteToolStripMenuItem.Enabled =
				pasteInsertToolStripMenuItem.Enabled =
				Clipboard.GetDataObject().GetDataPresent(DataFormats.StringFormat) && TasView.AnyRowsSelected;

			StartNewProjectFromNowMenuItem.Visible =
				TasView.SelectedRows.Count() == 1
				&& TasView.SelectedRows.Contains(Emulator.Frame)
				&& !CurrentTasMovie.StartsFromSaveRam;

			StartANewProjectFromSaveRamMenuItem.Visible =
				TasView.SelectedRows.Count() == 1
				&& SaveRamEmulator != null
				&& !CurrentTasMovie.StartsFromSavestate;

			StartFromNowSeparator.Visible = StartNewProjectFromNowMenuItem.Visible || StartANewProjectFromSaveRamMenuItem.Visible;
			RemoveMarkersContextMenuItem.Enabled = CurrentTasMovie.Markers.Any(m => TasView.SelectedRows.Contains(m.Frame)); // Disable the option to remove markers if no markers are selected (FCEUX does this).
			CancelSeekContextMenuItem.Enabled = Mainform.PauseOnFrame.HasValue;
			BranchContextMenuItem.Visible = TasView.CurrentCell.RowIndex == Emulator.Frame;
		}

		private void CancelSeekContextMenuItem_Click(object sender, EventArgs e)
		{
			Mainform.PauseOnFrame = null;
			RefreshTasView();
		}

		private void BranchContextMenuItem_Click(object sender, EventArgs e)
		{
			BookMarkControl.Branch();
		}

		private void StartNewProjectFromNowMenuItem_Click(object sender, EventArgs e)
		{
			if (AskSaveChanges())
			{
				int index = Emulator.Frame;

				TasMovie newProject = CurrentTasMovie.ConvertToSavestateAnchoredMovie(
					index, (byte[])StatableEmulator.SaveStateBinary().Clone());

				Mainform.PauseEmulator();
				LoadFile(new FileInfo(newProject.Filename), true);
			}
		}

		private void StartANewProjectFromSaveRamMenuItem_Click(object sender, EventArgs e)
		{
			if (AskSaveChanges())
			{
				if (SaveRamEmulator.CloneSaveRam() != null)
				{
					int index = 0;
					if (TasView.SelectedRows.Count() > 0) { index = TasView.SelectedRows.First(); }
					GoToFrame(index);
					TasMovie newProject = CurrentTasMovie.ConvertToSaveRamAnchoredMovie(
						SaveRamEmulator.CloneSaveRam());
					Mainform.PauseEmulator();
					LoadFile(new FileInfo(newProject.Filename), true);
				}
				else
				{
					throw new Exception("No SaveRam");
				}
			}
		}

		#endregion

		#region Help

		private void TASEditorManualOnlineMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://www.fceux.com/web/help/taseditor/");
		}

		private void ForumThreadMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("http://tasvideos.org/forum/viewtopic.php?t=13505");
		}

		#endregion
	}
}
