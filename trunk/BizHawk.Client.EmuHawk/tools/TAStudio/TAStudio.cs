using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : Form, IToolForm, IControlMainform
	{
		// TODO: UI flow that conveniently allows to start from savestate
		private const string MarkerColumnName = "MarkerColumn";
		private const string FrameColumnName = "FrameColumn";

		private readonly List<TasClipboardEntry> _tasClipboard = new List<TasClipboardEntry>();

		private int _defaultWidth;
		private int _defaultHeight;
		private TasMovie _currentTasMovie;
		private bool _originalRewindStatus; // The client rewind status before TAStudio was engaged (used to restore when disengaged)
		private MovieEndAction _originalEndAction; // The movie end behavior selected by the user (that is overridden by TAStudio)

		private Dictionary<string, string> GenerateColumnNames()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			return (lg as Bk2LogEntryGenerator).Map();
		}

		public TasMovie CurrentMovie
		{
			get { return _currentTasMovie; }
		}

		private void TastudioToStopMovie()
		{
			Global.MovieSession.StopMovie(false);
			GlobalWin.MainForm.SetMainformMovieInfo();
		}

		public TAStudio()
		{
			InitializeComponent();
			WantsToControlStopMovie = true;
			TasPlaybackBox.Tastudio = this;
			MarkerControl.Tastudio = this;
			TasView.QueryItemText += TasView_QueryItemText;
			TasView.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView.QueryItemIcon += TasView_QueryItemIcon;

			TopMost = Global.Config.TAStudioSettings.TopMost;
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
			TasView.MultiSelect = true;
			TasView.MaxCharactersInHorizontal = 5;
		}

		private void ConvertCurrentMovieToTasproj()
		{
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie = Global.MovieSession.Movie.ToTasMovie();
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie.SwitchToRecord();
		}

		private void EngageTastudio()
		{
			GlobalWin.MainForm.PauseOnFrame = null;
			GlobalWin.OSD.AddMessage("TAStudio engaged");
			_currentTasMovie = Global.MovieSession.Movie as TasMovie;
			SetTextProperty();
			GlobalWin.MainForm.PauseEmulator();
			GlobalWin.MainForm.RelinquishControl(this);
			_originalRewindStatus = Global.Rewinder.RewindActive;
			_originalEndAction = Global.Config.MovieEndAction;
			MarkerControl.Markers = _currentTasMovie.Markers;
			GlobalWin.MainForm.EnableRewind(false);
			Global.Config.MovieEndAction = MovieEndAction.Record;
			GlobalWin.MainForm.SetMainformMovieInfo();
		}

		private void DisengageTastudio()
		{
			GlobalWin.MainForm.PauseOnFrame = null;
			GlobalWin.OSD.AddMessage("TAStudio disengaged");
			Global.MovieSession.Movie = MovieService.DefaultInstance;
			GlobalWin.MainForm.TakeBackControl();
			GlobalWin.MainForm.EnableRewind(_originalRewindStatus);
			Global.Config.MovieEndAction = _originalEndAction;
			GlobalWin.MainForm.SetMainformMovieInfo();
		}

		private void NewTasMovie()
		{
			Global.MovieSession.Movie = new TasMovie();
			_currentTasMovie = Global.MovieSession.Movie as TasMovie;
			_currentTasMovie.PropertyChanged += new PropertyChangedEventHandler(this.TasMovie_OnPropertyChanged);
			_currentTasMovie.Filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
			_currentTasMovie.PopulateWithDefaultHeaderValues();
			_currentTasMovie.ClearChanges();
		}

		private static string DefaultTasProjName()
		{
			return Path.Combine(
				PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
				PathManager.FilesystemSafeName(Global.Game) + "." + TasMovie.Extension);
		}

		private void StartNewTasMovie()
		{
			if (AskSaveChanges())
			{
				NewTasMovie();
				WantsToControlStopMovie = false;
				GlobalWin.MainForm.StartNewMovie(_currentTasMovie, record: true);
				WantsToControlStopMovie = true;
				SetTextProperty();
				RefreshDialog();
			}
		}

		private void SetTextProperty()
		{
			var text = "TAStudio";
			if (_currentTasMovie != null)
			{
				text += " - " + _currentTasMovie.Name + (_currentTasMovie.Changes ? "*" : "");
			}

			Text = text;
		}

		public void LoadProject(string path)
		{
			if (AskSaveChanges())
			{
				var movie = new TasMovie()
				{
					Filename = path
				};
				movie.PropertyChanged += TasMovie_OnPropertyChanged;

				var file = new FileInfo(path);
				if (!file.Exists)
				{
					Global.Config.RecentTas.HandleLoadError(path);
				}

				WantsToControlStopMovie = false;

				var shouldRecord = false;
				if (Global.MovieSession.Movie.InputLogLength == 0) // An unusual but possible edge case
				{
					shouldRecord = true;
				}

				GlobalWin.MainForm.StartNewMovie(movie, record: shouldRecord);
				WantsToControlStopMovie = true;
				_currentTasMovie = Global.MovieSession.Movie as TasMovie;
				Global.Config.RecentTas.Add(path);
				Text = "TAStudio - " + _currentTasMovie.Name;
				RefreshDialog();
			}
		}

		public void RefreshDialog()
		{
			_currentTasMovie.FlushInputCache();
			_currentTasMovie.UseInputCache = true;
			TasView.RowCount = _currentTasMovie.InputLogLength + 1;
			TasView.Refresh();

			_currentTasMovie.FlushInputCache();
			_currentTasMovie.UseInputCache = false;

			if (MarkerControl != null)
			{
				MarkerControl.UpdateValues();
			}
		}

		// TODO: a better name
		private void GoToLastEmulatedFrameIfNecessary(int frame)
		{
			if (frame != Global.Emulator.Frame) // Don't go to a frame if you are already on it!
			{
				var restoreFrame = Global.Emulator.Frame;

				if (frame <= _currentTasMovie.LastEmulatedFrame)
				{
					GoToFrame(frame);

					if (Global.Config.TAStudioAutoRestoreLastPosition)
					{
						GlobalWin.MainForm.UnpauseEmulator();
						GlobalWin.MainForm.PauseOnFrame = restoreFrame;
					}
				}
			}
		}

		private void SetUpColumns()
		{
			TasView.Columns.Clear();
			AddColumn(MarkerColumnName, string.Empty, 18);
			AddColumn(FrameColumnName, "Frame#", 68);

			foreach (var kvp in GenerateColumnNames())
			{
				AddColumn(kvp.Key, kvp.Value, 20 * kvp.Value.Length);
			}
		}

		public void AddColumn(string columnName, string columnText, int columnWidth)
		{
			if (TasView.Columns[columnName] == null)
			{
				var column = new InputRoll.RollColumn
				{
					Name = columnName,
					Text = columnText,
					Width = columnWidth,
				};

				TasView.Columns.Add(column);
			}
		}

		private void LoadConfigSettings()
		{
			_defaultWidth = Size.Width;
			_defaultHeight = Size.Height;

			if (Global.Config.TAStudioSettings.UseWindowPosition)
			{
				Location = Global.Config.TAStudioSettings.WindowPosition;
			}

			if (Global.Config.TAStudioSettings.UseWindowSize)
			{
				Size = Global.Config.TAStudioSettings.WindowSize;
			}
		}

		private void SaveConfigSettings()
		{
			Global.Config.TAStudioSettings.Wndx = Location.X;
			Global.Config.TAStudioSettings.Wndy = Location.Y;
			Global.Config.TAStudioSettings.Width = Right - Left;
			Global.Config.TAStudioSettings.Height = Bottom - Top;
		}

		public void GoToMarker(TasMovieMarker marker)
		{
			GoToFrame(marker.Frame);
		}


		private void StartAtNearestFrameAndEmulate(int frame)
		{
			_currentTasMovie.SwitchToPlay();
			var closestState = _currentTasMovie.GetStateClosestToFrame(frame);
			if (closestState != null)
			{
				Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(closestState.ToArray())));
			}

			GlobalWin.MainForm.PauseOnFrame = frame;
			GlobalWin.MainForm.UnpauseEmulator();
		}

		private void GoToFrame(int frame)
		{
			// If past greenzone, emulate and capture states
			// If past greenzone AND movie, record input and capture states
			// If in greenzone, loadstate
			// If near a greenzone item, load and emulate
			// Do capturing and recording as needed

			if (frame < _currentTasMovie.InputLogLength)
			{
				if (frame < Global.Emulator.Frame) // We are rewinding
				{
					var goToFrame = frame == 0 ? 0 : frame - 1;

					if (_currentTasMovie[goToFrame].HasState) // Go back 1 frame and emulate to get the display (we don't store that)
					{
						_currentTasMovie.SwitchToPlay();
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[goToFrame].State.ToArray())));

						if (frame > 0) // We can't emulate up to frame 0!
						{
							Global.Emulator.FrameAdvance(true);
						}

						GlobalWin.DisplayManager.NeedsToPaint = true;
						TasView.LastVisibleRow = frame;
					}
					else // Get as close as we can then emulate there
					{
						StartAtNearestFrameAndEmulate(frame);
						return;
					}
				}
				else // We are going foward
				{
					var goToFrame = frame == 0 ? 0 : frame - 1;
					if (_currentTasMovie[goToFrame].HasState) // Can we go directly there?
					{
						_currentTasMovie.SwitchToPlay();
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[goToFrame].State.ToArray())));
						Global.Emulator.FrameAdvance(true);
						GlobalWin.DisplayManager.NeedsToPaint = true;
						TasView.LastVisibleRow = frame;
					}
					else
					{
						StartAtNearestFrameAndEmulate(frame);
						return;
					}
				}
			}
			else // Emulate to a future frame
			{
				// TODO: get the last greenzone frame and go there
				_currentTasMovie.SwitchToPlay();

				if (_currentTasMovie.LastEmulatedFrame > 0)
				{
					Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[_currentTasMovie.LastEmulatedFrame].State.ToArray())));
				}

				GlobalWin.MainForm.UnpauseEmulator();
				if (Global.Config.TAStudioAutoPause)
				{
					GlobalWin.MainForm.PauseOnFrame = _currentTasMovie.LastEmulatedFrame;
				}
				else
				{
					GlobalWin.MainForm.PauseOnFrame = frame;
				}
			}

			RefreshDialog();
		}

		#region Playback Controls

		public void GoToPreviousMarker()
		{
			if (Global.Emulator.Frame > 0)
			{
				var prevMarker = _currentTasMovie.Markers.Previous(Global.Emulator.Frame);
				var prev = prevMarker != null ? prevMarker.Frame : 0;
				GoToFrame(prev);
			}
		}

		public void GoToPreviousFrame()
		{
			if (Global.Emulator.Frame > 0)
			{
				GoToFrame(Global.Emulator.Frame - 1);
			}
		}

		public void TogglePause()
		{
			GlobalWin.MainForm.TogglePause();
		}

		public void GoToNextFrame()
		{
			GoToFrame(Global.Emulator.Frame + 1);
		}

		public void GoToNextMarker()
		{
			var nextMarker = _currentTasMovie.Markers.Next(Global.Emulator.Frame);
			var next = nextMarker != null ? nextMarker.Frame : _currentTasMovie.InputLogLength - 1;
			GoToFrame(next);
		}

		#endregion

		private void SetSplicer()
		{
			// TODO: columns selected
			// TODO: clipboard
			var list = TasView.SelectedRows;
			string message = "Selected: ";

			if (list.Count > 0)
			{
				message += list.Count + " rows 0 col, Clipboard: ";
			}
			else
			{
				message += list.Count + " none, Clipboard: ";
			}

			message += _tasClipboard.Any() ? _tasClipboard.Count.ToString() + " rows 0 col": "empty";

			SplicerStatusLabel.Text = message;
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.TAStudioSettings.FloatingWindow ? null : GlobalWin.MainForm;
		}

		public void CallAddMarkerPopUp(int? frame = null)
		{
			var markerFrame = frame ?? TasView.LastSelectedIndex ?? Global.Emulator.Frame;
			InputPrompt i = new InputPrompt
			{
				Text = "Marker for frame " + markerFrame,
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue = _currentTasMovie.Markers.IsMarker(markerFrame) ? _currentTasMovie.Markers.PreviousOrCurrent(markerFrame).Message : ""
			};

			var result = i.ShowHawkDialog();

			if (result == DialogResult.OK)
			{
				_currentTasMovie.Markers.Add(markerFrame, i.PromptText);
				MarkerControl.UpdateValues();
			}
		}

		private void UpdateChangesIndicator()
		{
			// TODO
		}

		// TODO: move me
		// Sets either the pending frame or the tas input log
		private void ToggleBoolState(int frame, string buttonName)
		{
			if (frame < _currentTasMovie.InputLogLength)
			{
				_currentTasMovie.ToggleBoolState(frame, buttonName);
			}
			else if (frame == Global.Emulator.Frame && frame == _currentTasMovie.InputLogLength)
			{
				Global.ClickyVirtualPadController.Toggle(buttonName);
			}
		}

		// TODO: move me
		// Sets either the pending frame or the tas input log
		private void SetBoolState(int frame, string buttonName, bool value)
		{
			if (frame < _currentTasMovie.InputLogLength)
			{
				_currentTasMovie.SetBoolState(frame, buttonName, value);
			}
			else if (frame == Global.Emulator.Frame && frame == _currentTasMovie.InputLogLength)
			{
				Global.ClickyVirtualPadController.SetBool(buttonName, value);
			}
		}

		#region Events

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
				Global.Config.RecentTas.RecentMenu(LoadProject));
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
				var list = TasView.SelectedRows;
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
					GoToFrame(TasView.FirstSelectedIndex.Value);
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
					GoToFrame(TasView.FirstSelectedIndex.Value);
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
					GoToFrame(rollBackFrame);
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
					GoToFrame(rollBackFrame);
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
					GoToFrame(rollBackFrame);
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
					GoToFrame(insertionFrame);
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
				GoToFrame(insertionFrame);
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
				GoToFrame(insertionFrame);
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
			foreach(int index in TasView.SelectedRows)
			{
				CallAddMarkerPopUp(index);
			}
		}

		private void RemoveMarkersMenuItem_Click(object sender, EventArgs e)
		{
			_currentTasMovie.Markers.RemoveAll(m => TasView.SelectedRows.Contains(m.Frame));
			RefreshDialog();
		}

		#endregion

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DrawInputByDraggingMenuItem.Checked = Global.Config.TAStudioDrawInput;
			AutopauseAtEndOfMovieMenuItem.Checked = Global.Config.TAStudioAutoPause;
		}

		private void DrawInputByDraggingMenuItem_Click(object sender, EventArgs e)
		{
			// TOOD: integrate this logic into input roll, have it save and load through its own load/save settings methods, Global.Config.TAStudioDrawInput will go away
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput ^= true;
		}

		private void AutopauseAtEndMenuItem_Click(object sender, EventArgs e)
		{
			Global.Config.TAStudioAutoPause ^= true;
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
			new GreenzoneSettings(_currentTasMovie.GreenzoneSettings).Show();
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

		#region Dialog Events

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		private void Tastudio_Load(object sender, EventArgs e)
		{
			// Start Scenario 1: A regular movie is active
			if (Global.MovieSession.Movie.IsActive && !(Global.MovieSession.Movie is TasMovie))
			{
				var result = MessageBox.Show("In order to use Tastudio, a new project must be created from the current movie\nThe current movie will be saved and closed, and a new project file will be created\nProceed?", "Convert movie", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if (result == DialogResult.OK)
				{
					ConvertCurrentMovieToTasproj();
				}
				else
				{
					Close();
					return;
				}
			}

			// Start Scenario 2: A tasproj is already active
			else if (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie is TasMovie)
			{
				// Nothing to do
			}

			// Start Scenario 3: No movie, but user wants to autload their last project
			else if (Global.Config.AutoloadTAStudioProject && !string.IsNullOrEmpty(Global.Config.RecentTas.MostRecent))
			{
				LoadProject(Global.Config.RecentTas.MostRecent);
			}

			// Start Scenario 4: No movie, default behavior of engaging tastudio with a new default project
			else
			{
				NewTasMovie();
				GlobalWin.MainForm.StartNewMovie(_currentTasMovie, record: true);
				_currentTasMovie.CaptureCurrentState();
			}

			EngageTastudio();
			SetUpColumns();
			LoadConfigSettings();
			RefreshDialog();
		}

		private void Tastudio_Closing(object sender, FormClosingEventArgs e)
		{
			if (AskSaveChanges())
			{
				SaveConfigSettings();
				GlobalWin.MainForm.StopMovie(saveChanges: false);
				DisengageTastudio();
			}
			else
			{
				e.Cancel = true;
			}
		}

		//This method is called everytime the Changes property is toggled on a TasMovie instance.
		private void TasMovie_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			SetTextProperty();
		}

		private void RightClickMenu_Opened(object sender, EventArgs e)
		{
			RemoveMarkersContextMenuItem.Enabled = _currentTasMovie.Markers.Any(m => TasView.SelectedRows.Contains(m.Frame)); // Disable the option to remove markers if no markers are selected (FCUEX does this).
		}

		#endregion

		#endregion
	}
}
