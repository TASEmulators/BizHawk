using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

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

		// Indices Helpers
		private int FirstSelectedIndex
		{
			get
			{
				return TasView.SelectedIndices
					.OfType<int>()
					.OrderBy(frame => frame)
					.First();
			}
		}

		private int LastSelectedIndex
		{
			get
			{
				return TasView.SelectedIndices
					.OfType<int>()
					.OrderBy(frame => frame)
					.Last();
			}
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
			TasView.VirtualMode = true;

			TopMost = Global.Config.TAStudioSettings.TopMost;
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
		}

		private void ConvertCurrentMovieToTasproj()
		{
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie = Global.MovieSession.Movie.ToTasMovie();
			Global.MovieSession.Movie.Save();
		}

		private void EngageTastudio()
		{
			GlobalWin.MainForm.PauseOnFrame = null;
			GlobalWin.OSD.AddMessage("TAStudio engaged");
			_currentTasMovie = Global.MovieSession.Movie as TasMovie;
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
				Text = "TAStudio - " + _currentTasMovie.Name;
				RefreshDialog();
			}
		}

		public void LoadProject(string path)
		{
			if (AskSaveChanges())
			{
				var movie = new TasMovie
				{
					Filename = path
				};

				var file = new FileInfo(path);
				if (!file.Exists)
				{
					Global.Config.RecentTas.HandleLoadError(path);
				}

				WantsToControlStopMovie = false;
				GlobalWin.MainForm.StartNewMovie(movie, record: false);
				WantsToControlStopMovie = true;
				_currentTasMovie = Global.MovieSession.Movie as TasMovie;
				Global.Config.RecentTas.Add(path);
				Text = "TAStudio - " + _currentTasMovie.Name;
				RefreshDialog();
			}
		}

		public void RefreshDialog()
		{
			TasView.BlazingFast = true;
			TasView.ItemCount = _currentTasMovie.InputLogLength + 1;
			TasView.BlazingFast = false;
			if (MarkerControl != null)
			{
				MarkerControl.Refresh();
			}
		}

		// TODO: a better name
		private void GoToLastEmulatedFrameIfNecessary(int frame)
		{
			if (frame != Global.Emulator.Frame) // Don't go to a frame if you are already on it!
			{
				if (frame <= _currentTasMovie.LastEmulatedFrame)
				{
					GoToFrame(frame);
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
				var column = new ColumnHeader
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

					if (_currentTasMovie[goToFrame].HasState) // Go back 1 frame and emulate
					{
						_currentTasMovie.SwitchToPlay();
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[goToFrame].State.ToArray())));

						if (goToFrame > 0) // We can't emulate up to frame 0!
						{
							Global.Emulator.FrameAdvance(true);
						}

						GlobalWin.DisplayManager.NeedsToPaint = true;
						TasView.ensureVisible(frame);
						RefreshDialog();
					}
					else
					{
						_currentTasMovie.SwitchToPlay();
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[_currentTasMovie.LastEmulatedFrame].State.ToArray())));
						GlobalWin.MainForm.UnpauseEmulator();
						GlobalWin.MainForm.PauseOnFrame = frame;
					}
				}
				else // We are going foward
				{ 
					var goToFrame = frame - 1;
					if (_currentTasMovie[goToFrame].HasState) // Can we go directly there?
					{
						_currentTasMovie.SwitchToPlay();
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[goToFrame].State.ToArray())));
						Global.Emulator.FrameAdvance(true);
						GlobalWin.DisplayManager.NeedsToPaint = true;
						TasView.ensureVisible(frame);
					}
					else // TODO: this assume that there are no "gaps", instead of last emulated frame, we should do last frame from X
					{
						_currentTasMovie.SwitchToPlay();
						Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[_currentTasMovie.LastEmulatedFrame].State.ToArray())));
						GlobalWin.MainForm.UnpauseEmulator();
						GlobalWin.MainForm.PauseOnFrame = frame;
					}
				}
			}
			else // Emulate to a future frame
			{
				// TODO: get the last greenzone frame and go there
				_currentTasMovie.SwitchToPlay(); // TODO: stop copy/pasting this logic
				Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_currentTasMovie[_currentTasMovie.LastEmulatedFrame].State.ToArray())));
				GlobalWin.MainForm.UnpauseEmulator();
				GlobalWin.MainForm.PauseOnFrame = frame;
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
			var list = TasView.SelectedIndices;
			string message;

			if (list.Count > 0)
			{
				message = list.Count + " rows, 0 col, clipboard: ";
			}
			else
			{
				message = list.Count + " selected: none, clipboard: ";
			}

			message += _tasClipboard.Any() ? _tasClipboard.Count.ToString() : "empty";

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
				Message = "Enter a message"
			};

			var result = i.ShowHawkDialog();

			if (result == DialogResult.OK)
			{
				_currentTasMovie.Markers.Add(markerFrame, i.PromptText);
				MarkerControl.Refresh();
			}

			MarkerControl.Refresh();
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
				TasView.SelectedIndices().Any();
			ReselectClipboardMenuItem.Enabled =
				PasteMenuItem.Enabled =
				PasteInsertMenuItem.Enabled =
				_tasClipboard.Any();
		}

		private void DeselectMenuItem_Click(object sender, EventArgs e)
		{
			TasView.DeselectAll();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			TasView.SelectAll();
		}

		private void SelectBetweenMarkersMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedIndices().Any())
			{
				var prevMarker = _currentTasMovie.Markers.PreviousOrCurrent(LastSelectedIndex);
				var nextMarker = _currentTasMovie.Markers.Next(LastSelectedIndex);

				int prev = prevMarker != null ? prevMarker.Frame : 0;
				int next = nextMarker != null ? nextMarker.Frame : _currentTasMovie.InputLogLength;

				for (int i = prev; i < next; i++)
				{
					TasView.SelectItem(i, true);
				}
			}
		}

		private void ReselectClipboardMenuItem_Click(object sender, EventArgs e)
		{
			TasView.DeselectAll();
			foreach (var item in _tasClipboard)
			{
				TasView.SelectItem(item.Frame, true);
			}
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedIndices().Any())
			{
				_tasClipboard.Clear();
				var list = TasView.SelectedIndices;
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
				var needsToRollback = !(FirstSelectedIndex > Global.Emulator.Frame);

				_currentTasMovie.CopyOverInput(FirstSelectedIndex, _tasClipboard.Select(x => x.ControllerState));

				if (needsToRollback)
				{
					GoToFrame(FirstSelectedIndex);
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
				var needsToRollback = !(FirstSelectedIndex > Global.Emulator.Frame);

				_currentTasMovie.InsertInput(FirstSelectedIndex, _tasClipboard.Select(x => x.ControllerState));

				if (needsToRollback)
				{
					GoToFrame(FirstSelectedIndex);
				}
				else
				{
					RefreshDialog();
				}
			}
		}

		private void CutMenuItem_Click(object sender, EventArgs e)
		{
			if (TasView.SelectedIndices().Any())
			{
				var needsToRollback = !(FirstSelectedIndex > Global.Emulator.Frame);
				var rollBackFrame = FirstSelectedIndex;

				_tasClipboard.Clear();
				var list = TasView.SelectedIndices().ToArray();
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
			if (TasView.SelectedIndices().Any())
			{
				var needsToRollback = !(FirstSelectedIndex > Global.Emulator.Frame);
				var rollBackFrame = FirstSelectedIndex;

				foreach (var frame in TasView.SelectedIndices())
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
			if (TasView.SelectedIndices().Any())
			{
				var needsToRollback = !(FirstSelectedIndex > Global.Emulator.Frame);
				var rollBackFrame = FirstSelectedIndex;

				_tasClipboard.Clear();
				_currentTasMovie.RemoveFrames(TasView.SelectedIndices().ToArray());
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
			if (TasView.SelectedIndices().Any())
			{
				var framesToInsert = TasView.SelectedIndices().ToList();
				var insertionFrame = LastSelectedIndex + 1;
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
			var insertionFrame = TasView.SelectedIndices().Any() ? LastSelectedIndex + 1 : 0;
			var needsToRollback = !(insertionFrame > Global.Emulator.Frame);

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
			var insertionFrame = TasView.SelectedIndices().Any() ? LastSelectedIndex + 1 : 0;
			var needsToRollback = !(insertionFrame > Global.Emulator.Frame);

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
			if (TasView.SelectedIndices().Any())
			{
				var rollbackFrame = LastSelectedIndex + 1;
				var needsToRollback = !(rollbackFrame > Global.Emulator.Frame);

				_currentTasMovie.Truncate(LastSelectedIndex + 1);

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

		#endregion

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DrawInputByDraggingMenuItem.Checked = Global.Config.TAStudioDrawInput;
		}

		private void DrawInputByDraggingMenuItem_Click(object sender, EventArgs e)
		{
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput ^= true;
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

		#endregion

		#endregion
	}
}
