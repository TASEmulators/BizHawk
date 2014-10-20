using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;

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
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				Global.Config.RecentTas.RecentMenu(DummyLoadProject));
		}

		private void NewTasMenuItem_Click(object sender, EventArgs e)
		{
			if (GlobalWin.MainForm.GameIsClosing)
			{
				Close();
			}
			else
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

				var file = ToolHelpers.GetTasProjFileFromUser(filename);
				if (file != null)
				{
					CurrentTasMovie.Filename = file.FullName;
					CurrentTasMovie.Load();
					Global.Config.RecentTas.Add(CurrentTasMovie.Filename);

					if (CurrentTasMovie.InputLogLength > 0) // TODO: this is probably reoccuring logic, break off into a function
					{
						CurrentTasMovie.SwitchToPlay();
					}
					else
					{
						CurrentTasMovie.SwitchToRecord();
					}

					RefreshDialog();
					MessageStatusLabel.Text = Path.GetFileName(CurrentTasMovie.Filename) + " loaded.";
				}
			}
		}

		private void SaveTasMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(CurrentTasMovie.Filename) ||
				CurrentTasMovie.Filename == DefaultTasProjName())
			{
				SaveAsTasMenuItem_Click(sender, e);
			}
			else
			{
				CurrentTasMovie.Save();
				MessageStatusLabel.Text = Path.GetFileName(CurrentTasMovie.Filename) + " saved.";
				Global.Config.RecentTas.Add(CurrentTasMovie.Filename);
			}
		}

		private void SaveAsTasMenuItem_Click(object sender, EventArgs e)
		{
			var filename = CurrentTasMovie.Filename;
			if (string.IsNullOrWhiteSpace(filename) || filename == DefaultTasProjName())
			{
				filename = SuggestedTasProjName();
			}

			var file = ToolHelpers.GetTasProjSaveFileFromUser(filename);
			if (file != null)
			{
				CurrentTasMovie.Filename = file.FullName;
				CurrentTasMovie.Save();
				Global.Config.RecentTas.Add(CurrentTasMovie.Filename);
				MessageStatusLabel.Text = Path.GetFileName(CurrentTasMovie.Filename) + " saved.";
				SetTextProperty();
			}
		}

		private void ToBk2MenuItem_Click(object sender, EventArgs e)
		{
			var bk2 = CurrentTasMovie.ToBk2();
			bk2.Save();
			MessageStatusLabel.Text = Path.GetFileName(bk2.Filename) + " created.";

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
			ClearMenuItem.Enabled =
			DeleteFramesMenuItem.Enabled =
			CloneMenuItem.Enabled =
			TruncateMenuItem.Enabled =
				TasView.SelectedRows.Any();
			ReselectClipboardMenuItem.Enabled =
				PasteMenuItem.Enabled =
				PasteInsertMenuItem.Enabled =
				_tasClipboard.Any();

			ClearGreenzoneMenuItem.Enabled =
				CurrentTasMovie != null && CurrentTasMovie.TasStateManager.Any();

			GreenzoneICheckSeparator.Visible =
				GreenZoneIntegrityCheckMenuItem.Visible =
				VersionInfo.DeveloperBuild;
		}

		private void DeselectMenuItem_Click(object sender, EventArgs e)
		{
			TasView.DeselectAll();
			TasView.Refresh();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			TasView.SelectAll();
			TasView.Refresh();
		}

		private void SelectBetweenMarkersMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedRows.Any())
			{
				var prevMarker = CurrentTasMovie.Markers.PreviousOrCurrent(TasView.LastSelectedIndex.Value);
				var nextMarker = CurrentTasMovie.Markers.Next(TasView.LastSelectedIndex.Value);

				int prev = prevMarker != null ? prevMarker.Frame : 0;
				int next = nextMarker != null ? nextMarker.Frame : CurrentTasMovie.InputLogLength;

				for (int i = prev; i < next; i++)
				{
					TasView.SelectRow(i, true);
				}
			}
		}

		private void ReselectClipboardMenuItem_Click(object sender, EventArgs e)
		{
			TasView.DeselectAll();
			foreach (var item in _tasClipboard)
			{
				TasView.SelectRow(item.Frame, true);
			}
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedRows.Any())
			{
				_tasClipboard.Clear();
				var list = TasView.SelectedRows.ToList();
				var sb = new StringBuilder();

				foreach (var index in list)
				{
					var input = CurrentTasMovie.GetInputState(index);
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
			// TODO: if highlighting 2 rows and pasting 3, only paste 2 of them
			// FCEUX Taseditor does't do this, but I think it is the expected behavior in editor programs
			if (_tasClipboard.Any())
			{
				var needsToRollback = !(TasView.FirstSelectedIndex > Global.Emulator.Frame);

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

		private void PasteInsertMenuItem_Click(object sender, EventArgs e)
		{
			if (_tasClipboard.Any())
			{
				var needsToRollback = !(TasView.FirstSelectedIndex > Global.Emulator.Frame);

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

		private void CutMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedRows.Any())
			{
				var needsToRollback = !(TasView.FirstSelectedIndex.Value > Global.Emulator.Frame);
				var rollBackFrame = TasView.FirstSelectedIndex.Value;

				_tasClipboard.Clear();
				var list = TasView.SelectedRows.ToArray();
				var sb = new StringBuilder();
				for (var i = 0; i < list.Length; i++)
				{
					var input = CurrentTasMovie.GetInputState(i);
					_tasClipboard.Add(new TasClipboardEntry(list[i], input));
					var lg = CurrentTasMovie.LogGeneratorInstance();
					lg.SetSource(input);
					sb.AppendLine(lg.GenerateLogEntry());
				}

				Clipboard.SetDataObject(sb.ToString());
				CurrentTasMovie.RemoveFrames(list);
				SetSplicer();
				TasView.DeselectAll();

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

		private void ClearMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedRows.Any())
			{
				var needsToRollback = !(TasView.FirstSelectedIndex > Global.Emulator.Frame);
				var rollBackFrame = TasView.FirstSelectedIndex.Value;

				foreach (var frame in TasView.SelectedRows)
				{
					CurrentTasMovie.ClearFrame(frame);
				}

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
			if (TasView.SelectedRows.Any())
			{
				var needsToRollback = !(TasView.FirstSelectedIndex > Global.Emulator.Frame);
				var rollBackFrame = TasView.FirstSelectedIndex.Value;

				_tasClipboard.Clear();
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

		private void CloneMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedRows.Any())
			{
				var framesToInsert = TasView.SelectedRows.ToList();
				var insertionFrame = TasView.LastSelectedIndex.Value + 1;
				var needsToRollback = !(insertionFrame > Global.Emulator.Frame);

				var inputLog = framesToInsert
					.Select(frame => CurrentTasMovie.GetInputLogEntry(frame))
					.ToList();

				CurrentTasMovie.InsertInput(insertionFrame, inputLog);

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(insertionFrame);
					DoAutoRestore();
					RefreshDialog();
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void InsertFrameMenuItem_Click(object sender, EventArgs e)
		{
			var insertionFrame = TasView.SelectedRows.Any() ? TasView.FirstSelectedIndex.Value : 0;
			var needsToRollback = insertionFrame <= Global.Emulator.Frame;

			CurrentTasMovie.InsertEmptyFrame(insertionFrame);

			if (needsToRollback)
			{
				GoToLastEmulatedFrameIfNecessary(insertionFrame);
				DoAutoRestore();
				RefreshDialog();
			}
			else
			{
				RefreshDialog();
			}
		}

		private void InsertNumFramesMenuItem_Click(object sender, EventArgs e)
		{
			var insertionFrame = TasView.SelectedRows.Any() ? TasView.FirstSelectedIndex.Value : 0;
			var needsToRollback = insertionFrame <= Global.Emulator.Frame;

			var framesPrompt = new FramesPrompt();
			var result = framesPrompt.ShowDialog();
			if (result == DialogResult.OK)
			{
				CurrentTasMovie.InsertEmptyFrame(insertionFrame, framesPrompt.Frames);
			}

			if (needsToRollback)
			{
				GoToLastEmulatedFrameIfNecessary(insertionFrame);
				DoAutoRestore();
				RefreshDialog();
			}
			else
			{
				RefreshDialog();
			}
		}

		private void TruncateMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedRows.Any())
			{
				var rollbackFrame = TasView.LastSelectedIndex.Value;
				var needsToRollback = !(rollbackFrame > Global.Emulator.Frame);

				CurrentTasMovie.Truncate(rollbackFrame);

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
			foreach (var index in TasView.SelectedRows)
			{
				CallAddMarkerPopUp(index);
			}
		}

		private void RemoveMarkersMenuItem_Click(object sender, EventArgs e)
		{
			CurrentTasMovie.Markers.RemoveAll(m => TasView.SelectedRows.Contains(m.Frame));
			RefreshDialog();
		}

		private void ClearGreenzoneMenuItem_Click(object sender, EventArgs e)
		{
			CurrentTasMovie.ClearGreenzone();
			RefreshDialog();
		}

		private void GreenZzoneIntegrityCheckMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.RebootCore();
			GlobalWin.MainForm.FrameAdvance();
			var frame = Global.Emulator.Frame;

			if (CurrentTasMovie.TasStateManager.HasState(frame))
			{
				var state = (byte[])Global.Emulator.SaveStateBinary().Clone();
				var greenzone = CurrentTasMovie.TasStateManager[frame];

				if (!state.SequenceEqual(greenzone))
				{
					MessageBox.Show("bad data at frame: " + frame);
					return;
				}
			}

			MessageBox.Show("Integrity Check passed");
		}

		#endregion

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DrawInputByDraggingMenuItem.Checked = Global.Config.TAStudioDrawInput;
			AutopauseAtEndOfMovieMenuItem.Checked = Global.Config.TAStudioAutoPause;
			EmptyNewMarkerNotesMenuItem.Checked = Global.Config.TAStudioEmptyMarkers;

			RotateMenuItem.ShortcutKeyDisplayString = TasView.RotateHotkeyStr;
		}

		private void DrawInputByDraggingMenuItem_Click(object sender, EventArgs e)
		{
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput ^= true;
		}

		private void EmptyNewMarkerNotesMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioEmptyMarkers ^= true;
		}

		private void AutopauseAtEndMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioAutoPause ^= true;
		}

		private void RotateMenuItem_Click(object sender, EventArgs e)
		{
			TasView.HorizontalOrientation ^= true;
			CurrentTasMovie.FlagChanges();
		}

		#endregion

		#region Metadata

		private void HeaderMenuItem_Click(object sender, EventArgs e)
		{
			new MovieHeaderEditor(CurrentTasMovie) { Owner = GlobalWin.MainForm }.Show();
			UpdateChangesIndicator();
		}

		private void GreenzoneSettingsMenuItem_Click(object sender, EventArgs e)
		{
			new GreenzoneSettingsForm(CurrentTasMovie.TasStateManager.Settings) { Owner = GlobalWin.MainForm }.Show();
			UpdateChangesIndicator();
		}

		private void CommentsMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditCommentsForm();
			form.GetMovie(CurrentTasMovie);
			form.ShowDialog();
		}

		private void SubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditSubtitlesForm { ReadOnly = false };
			form.GetMovie(Global.MovieSession.Movie);
			form.ShowDialog();
		}

		private void DefaultStateSettingsMenuItem_Click(object sender, EventArgs e)
		{
			new DefaultGreenzoneSettings().ShowDialog();
		}

		#endregion

		#region Settings Menu

		private void SettingsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			SaveWindowPositionMenuItem.Checked = Global.Config.TAStudioSettings.SaveWindowPosition;
			AutoloadMenuItem.Checked = Global.Config.AutoloadTAStudio;
			AutoloadProjectMenuItem.Checked = Global.Config.AutoloadTAStudioProject;
			AlwaysOnTopMenuItem.Checked = Global.Config.TAStudioSettings.TopMost;
			FloatingWindowMenuItem.Checked = Global.Config.TAStudioSettings.FloatingWindow;
		}

		private void AutoloadMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadTAStudio ^= true;
		}

		private void AutoloadProjectMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.AutoloadTAStudioProject ^= true;
		}

		private void SaveWindowPositionMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSettings.SaveWindowPosition ^= true;
		}

		private void AlwaysOnTopMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSettings.TopMost ^= true;
		}

		private void FloatingWindowMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioSettings.FloatingWindow ^= true;
			RefreshFloatingWindowControl();
		}

		private void RestoreDefaultSettingsMenuItem_Click(object sender, EventArgs e)
		{
			Size = new Size(_defaultWidth, _defaultHeight);

			Global.Config.TAStudioSettings.SaveWindowPosition = true;
			Global.Config.TAStudioSettings.TopMost = false;
			Global.Config.TAStudioSettings.FloatingWindow = false;

			RefreshFloatingWindowControl();
		}

		#endregion

		#region Columns

		private void ColumnsSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ColumnsSubMenu.DropDownItems.Clear();

			var columns = TasView.AllColumns
				.Where(x => !string.IsNullOrWhiteSpace(x.Text))
				.Where(x => x.Name != "FrameColumn");

			foreach (var column in columns)
			{
				var dummyColumnObject = column;

				var menuItem = new ToolStripMenuItem
				{
					Text = column.Text,
					Checked = column.Visible
				};

				menuItem.Click += (o, ev) =>
				{
					dummyColumnObject.Visible ^= true;
					TasView.AllColumns.ColumnsChanged();
					CurrentTasMovie.FlagChanges();
					TasView.Refresh();
				};

				ColumnsSubMenu.DropDownItems.Add(menuItem);
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
				TasView.Refresh();
				CurrentTasMovie.FlagChanges();
			};

			ColumnsSubMenu.DropDownItems.Add(defaults);
		}

		#endregion
	}
}
