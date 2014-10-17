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
				SaveTASMenuItem.Enabled =
				!string.IsNullOrWhiteSpace(_currentTasMovie.Filename);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				Global.Config.RecentTas.RecentMenu(DummyLoadProject));
		}

		private void DummyLoadProject(string path)
		{
			LoadProject(path);
		}

		private void NewTasMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("new TAStudio session started");
			StartNewTasMovie();
		}

		private void OpenTasMenuItem_Click(object sender, EventArgs e)
		{
			if (AskSaveChanges())
			{
				var file = ToolHelpers.GetTasProjFileFromUser(_currentTasMovie.Filename);
				if (file != null)
				{
					_currentTasMovie.Filename = file.FullName;
					_currentTasMovie.Load();
					Global.Config.RecentTas.Add(_currentTasMovie.Filename);

					if (_currentTasMovie.InputLogLength > 0) // TODO: this is probably reoccuring logic, break off into a function
					{
						_currentTasMovie.SwitchToPlay();
					}
					else
					{
						_currentTasMovie.SwitchToRecord();
					}

					RefreshDialog();
					MessageStatusLabel.Text = Path.GetFileName(_currentTasMovie.Filename) + " loaded.";
				}
			}
		}

		private void SaveTasMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(_currentTasMovie.Filename))
			{
				SaveAsTasMenuItem_Click(sender, e);
			}
			else
			{
				_currentTasMovie.Save();
				MessageStatusLabel.Text = Path.GetFileName(_currentTasMovie.Filename) + " saved.";
				Global.Config.RecentTas.Add(_currentTasMovie.Filename);
			}
		}

		private void SaveAsTasMenuItem_Click(object sender, EventArgs e)
		{
			var file = ToolHelpers.GetTasProjSaveFileFromUser(_currentTasMovie.Filename);
			if (file != null)
			{
				_currentTasMovie.Filename = file.FullName;
				_currentTasMovie.Save();
				Global.Config.RecentTas.Add(_currentTasMovie.Filename);
				MessageStatusLabel.Text = Path.GetFileName(_currentTasMovie.Filename) + " saved.";
			}
		}

		private void ToBk2MenuItem_Click(object sender, EventArgs e)
		{
			var bk2 = _currentTasMovie.ToBk2();
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
				_currentTasMovie != null && _currentTasMovie.TasStateManager.Any();

			GreenzoneICheckSeparator.Visible =
				GreenZzoneIntegrityCheckMenuItem.Visible =
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
				var prevMarker = _currentTasMovie.Markers.PreviousOrCurrent(TasView.LastSelectedIndex.Value);
				var nextMarker = _currentTasMovie.Markers.Next(TasView.LastSelectedIndex.Value);

				int prev = prevMarker != null ? prevMarker.Frame : 0;
				int next = nextMarker != null ? nextMarker.Frame : _currentTasMovie.InputLogLength;

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

				for (var i = 0; i < list.Count; i++)
				{
					var input = _currentTasMovie.GetInputState(list[i]);
					_tasClipboard.Add(new TasClipboardEntry(list[i], input));
					var lg = _currentTasMovie.LogGeneratorInstance();
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

				_currentTasMovie.CopyOverInput(TasView.FirstSelectedIndex.Value, _tasClipboard.Select(x => x.ControllerState));

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(TasView.FirstSelectedIndex.Value);
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

				_currentTasMovie.InsertInput(TasView.FirstSelectedIndex.Value, _tasClipboard.Select(x => x.ControllerState));

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(TasView.FirstSelectedIndex.Value);
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
					var input = _currentTasMovie.GetInputState(i);
					_tasClipboard.Add(new TasClipboardEntry(list[i], input));
					var lg = _currentTasMovie.LogGeneratorInstance();
					lg.SetSource(input);
					sb.AppendLine(lg.GenerateLogEntry());
				}

				Clipboard.SetDataObject(sb.ToString());
				_currentTasMovie.RemoveFrames(list);
				SetSplicer();
				TasView.DeselectAll();

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
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
					_currentTasMovie.ClearFrame(frame);
				}

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
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
				_currentTasMovie.RemoveFrames(TasView.SelectedRows.ToArray());
				SetSplicer();
				TasView.DeselectAll();

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(rollBackFrame);
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
				var inputLog = new List<string>();

				foreach (var frame in framesToInsert)
				{
					inputLog.Add(_currentTasMovie.GetInputLogEntry(frame));
				}

				_currentTasMovie.InsertInput(insertionFrame, inputLog);

				if (needsToRollback)
				{
					GoToLastEmulatedFrameIfNecessary(insertionFrame);
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
			bool needsToRollback = insertionFrame <= Global.Emulator.Frame;

			_currentTasMovie.InsertEmptyFrame(insertionFrame);

			if (needsToRollback)
			{
				GoToLastEmulatedFrameIfNecessary(insertionFrame);
			}
			else
			{
				RefreshDialog();
			}
		}

		private void InsertNumFramesMenuItem_Click(object sender, EventArgs e)
		{
			var insertionFrame = TasView.SelectedRows.Any() ? TasView.FirstSelectedIndex.Value : 0;
			bool needsToRollback = insertionFrame <= Global.Emulator.Frame;

			var framesPrompt = new FramesPrompt();
			var result = framesPrompt.ShowDialog();
			if (result == DialogResult.OK)
			{
				_currentTasMovie.InsertEmptyFrame(insertionFrame, framesPrompt.Frames);
			}

			if (needsToRollback)
			{
				GoToLastEmulatedFrameIfNecessary(insertionFrame);
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
				var rollbackFrame = TasView.LastSelectedIndex.Value + 1;
				var needsToRollback = !(rollbackFrame > Global.Emulator.Frame);

				_currentTasMovie.Truncate(rollbackFrame);

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
			foreach (int index in TasView.SelectedRows)
			{
				CallAddMarkerPopUp(index);
			}
		}

		private void RemoveMarkersMenuItem_Click(object sender, EventArgs e)
		{
			_currentTasMovie.Markers.RemoveAll(m => TasView.SelectedRows.Contains(m.Frame));
			RefreshDialog();
		}

		private void ClearGreenzoneMenuItem_Click(object sender, EventArgs e)
		{
			_currentTasMovie.ClearGreenzone();
			RefreshDialog();
		}

		private void GreenZzoneIntegrityCheckMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.MainForm.RebootCore();

			GlobalWin.MainForm.FrameAdvance();
			var frame = Global.Emulator.Frame;

			if (_currentTasMovie.TasStateManager.HasState(frame))
			{
				var state = (byte[])Global.Emulator.SaveStateBinary().Clone();
				var greenzone = _currentTasMovie.TasStateManager[frame];

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
		}

		#endregion

		#region Metadata

		private void HeaderMenuItem_Click(object sender, EventArgs e)
		{
			new MovieHeaderEditor(_currentTasMovie).Show();
			UpdateChangesIndicator();
		}

		private void GreenzoneSettingsMenuItem_Click(object sender, EventArgs e)
		{
			new GreenzoneSettingsForm(_currentTasMovie.TasStateManager.Settings).Show();
			UpdateChangesIndicator();
		}

		private void CommentsMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditCommentsForm();
			form.GetMovie(_currentTasMovie);
			form.ShowDialog();
		}

		private void SubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditSubtitlesForm { ReadOnly = false };
			form.GetMovie(Global.MovieSession.Movie);
			form.ShowDialog();
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
					TasView.Refresh();
				};

				ColumnsSubMenu.DropDownItems.Add(menuItem);
			}
		}

		#endregion
	}
}
