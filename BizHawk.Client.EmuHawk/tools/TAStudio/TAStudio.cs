using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

using BizHawk.Client.EmuHawk.WinFormExtensions;
using BizHawk.Client.EmuHawk.ToolExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class TAStudio : Form, IToolFormAutoConfig, IControlMainform
	{
		// TODO: UI flow that conveniently allows to start from savestate
		private const string MarkerColumnName = "MarkerColumn";
		private const string FrameColumnName = "FrameColumn";

		private readonly List<TasClipboardEntry> _tasClipboard = new List<TasClipboardEntry>();

		private BackgroundWorker _saveBackgroundWorker;

		private MovieEndAction _originalEndAction; // The movie end behavior selected by the user (that is overridden by TAStudio)
		private Dictionary<string, string> GenerateColumnNames()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			return (lg as Bk2LogEntryGenerator).Map();
		}

		private UndoHistoryForm undoForm;

		private int? _autoRestoreFrame; // The frame auto-restore will restore to, if set

		[ConfigPersist]
		public TAStudioSettings Settings { get; set; }

		public class TAStudioSettings
		{
			public TAStudioSettings()
			{
				RecentTas = new RecentFiles(8);
				DrawInput = true;
				AutoPause = true;
				FollowCursor = true;
			}

			public RecentFiles RecentTas { get; set; }
			public bool DrawInput { get; set; }
			public bool AutoPause { get; set; }
			public bool AutoRestoreLastPosition { get; set; }
			public bool FollowCursor { get; set; }
			public bool EmptyMarkers { get; set; }
		}

		public TasMovie CurrentTasMovie
		{
			get { return Global.MovieSession.Movie as TasMovie; }
		}

		#region "Initializing"

		public TAStudio()
		{
			InitializeComponent();
			Settings = new TAStudioSettings();

			// TODO: show this at all times or hide it when saving is done?
			this.SavingProgressBar.Visible = false;

			InitializeSaveWorker();

			WantsToControlStopMovie = true;
			TasPlaybackBox.Tastudio = this;
			MarkerControl.Tastudio = this;
			MarkerControl.Emulator = this.Emulator;
			TasView.QueryItemText += TasView_QueryItemText;
			TasView.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView.QueryRowBkColor += TasView_QueryRowBkColor;
			TasView.QueryItemIcon += TasView_QueryItemIcon;
			TasView.QueryFrameLag += TasView_QueryFrameLag;
			TasView.InputPaintingMode = Settings.DrawInput;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
			TasView.MultiSelect = true;
			TasView.MaxCharactersInHorizontal = 1;
			WantsToControlRestartMovie = true;
		}

		private void InitializeSaveWorker()
		{
			if (_saveBackgroundWorker != null)
			{
				_saveBackgroundWorker.Dispose();
				_saveBackgroundWorker = null; // Idk if this line is even useful.
			}

			_saveBackgroundWorker = new BackgroundWorker();
			_saveBackgroundWorker.WorkerReportsProgress = true;
			_saveBackgroundWorker.DoWork += (s, e) =>
			{
				this.Invoke(() => this.MessageStatusLabel.Text = "Saving " + Path.GetFileName(CurrentTasMovie.Filename) + "...");
				this.Invoke(() => this.SavingProgressBar.Visible = true);
				CurrentTasMovie.Save();
			};

			_saveBackgroundWorker.ProgressChanged += (s, e) =>
			{
				SavingProgressBar.Value = e.ProgressPercentage;
			};

			_saveBackgroundWorker.RunWorkerCompleted += (s, e) =>
			{
				this.Invoke(() => this.MessageStatusLabel.Text = Path.GetFileName(CurrentTasMovie.Filename) + " saved.");
				this.Invoke(() => this.SavingProgressBar.Visible = false);

				InitializeSaveWorker(); // Required, or it will error when trying to report progress again.
			};

			if (CurrentTasMovie != null) // Again required. TasMovie has a separate reference.
				CurrentTasMovie.NewBGWorker(_saveBackgroundWorker);
		}

		private bool _initialized = false;
		private void Tastudio_Load(object sender, EventArgs e)
		{
			if (!InitializeOnLoad())
			{
				Close();
				this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
				return;
			}

			SetColumnsFromCurrentStickies();

			if (VersionInfo.DeveloperBuild)
			{
				RightClickMenu.Items.AddRange(TasView.GenerateContextMenuItems().ToArray());

				RightClickMenu.Items
				.OfType<ToolStripMenuItem>()
				.First(t => t.Name == "RotateMenuItem")
				.Click += (o, ov) =>
				{
					CurrentTasMovie.FlagChanges();
				};
			}

			RefreshDialog();
			_initialized = true;
		}

		private bool InitializeOnLoad()
		{
			// Start Scenario 1: A regular movie is active
			if (Global.MovieSession.Movie.IsActive && !(Global.MovieSession.Movie is TasMovie))
			{
				var result = MessageBox.Show("In order to use Tastudio, a new project must be created from the current movie\nThe current movie will be saved and closed, and a new project file will be created\nProceed?", "Convert movie", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if (result == DialogResult.OK)
				{
					ConvertCurrentMovieToTasproj();
					StartNewMovieWrapper(false);
				}
				else
				{
					return false;
				}
			}

			// Start Scenario 2: A tasproj is already active
			else if (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie is TasMovie)
			{
				// Nothing to do
			}

			// Start Scenario 3: No movie, but user wants to autload their last project
			else if (Settings.RecentTas.AutoLoad && !string.IsNullOrEmpty(Settings.RecentTas.MostRecent))
			{
				bool result = LoadFile(new FileInfo(Settings.RecentTas.MostRecent));
				if (!result)
				{
					TasView.AllColumns.Clear();
					StartNewTasMovie();
				}
			}

			// Start Scenario 4: No movie, default behavior of engaging tastudio with a new default project
			else
			{
				StartNewTasMovie();
			}

			EngageTastudio();

			if (!TasView.AllColumns.Any()) // If a project with column settings has already been loaded we don't need to do this
			{
				SetUpColumns();
			}
			return true;
		}

		private void SetTasMovieCallbacks()
		{
			CurrentTasMovie.ClientSettingsForSave = ClientSettingsForSave;
			CurrentTasMovie.GetClientSettingsOnLoad = GetClientSettingsOnLoad;
		}
		private string ClientSettingsForSave()
		{
			return TasView.UserSettingsSerialized();
		}
		private void GetClientSettingsOnLoad(string settingsJson)
		{
			TasView.LoadSettingsSerialized(settingsJson);
			RefreshTasView();

			SetUpToolStripColumns();
		}

		private void SetUpColumns()
		{
			TasView.AllColumns.Clear();
			AddColumn(MarkerColumnName, string.Empty, 18);
			AddColumn(FrameColumnName, "Frame#", 68);

			var columnNames = GenerateColumnNames();
			foreach (var kvp in columnNames)
			{
				// N64 hack for now, for fake analog
				if (Emulator.SystemId == "N64")
				{
					if (kvp.Key.Contains("A Up") || kvp.Key.Contains("A Down") ||
					kvp.Key.Contains("A Left") || kvp.Key.Contains("A Right"))
					{
						continue;
					}
				}

				AddColumn(kvp.Key, kvp.Value, 20 * kvp.Value.Length);
			}

			// Patterns
			int bStart = 0;
			int fStart = 0;
			if (BoolPatterns == null)
			{
				BoolPatterns = new AutoPatternBool[controllerType.BoolButtons.Count + 2];
				FloatPatterns = new AutoPatternFloat[controllerType.FloatControls.Count + 2];
			}
			else
			{
				bStart = BoolPatterns.Length - 2;
				fStart = FloatPatterns.Length - 2;
				Array.Resize(ref BoolPatterns, controllerType.BoolButtons.Count + 2);
				Array.Resize(ref FloatPatterns, controllerType.FloatControls.Count + 2);
			}

			for (int i = bStart; i < BoolPatterns.Length - 2; i++)
				BoolPatterns[i] = new AutoPatternBool(1, 1);
			BoolPatterns[BoolPatterns.Length - 2] = new AutoPatternBool(1, 0);
			BoolPatterns[BoolPatterns.Length - 1] = new AutoPatternBool(
				Global.Config.AutofireOn, Global.Config.AutofireOff);

			for (int i = fStart; i < FloatPatterns.Length - 2; i++)
				FloatPatterns[i] = new AutoPatternFloat(new float[] { 1f });
			FloatPatterns[FloatPatterns.Length - 2] = new AutoPatternFloat(new float[] { 1f });
			FloatPatterns[FloatPatterns.Length - 1] = new AutoPatternFloat(
				1f, Global.Config.AutofireOn, 0f, Global.Config.AutofireOff);

			SetUpToolStripColumns();
		}

		public void AddColumn(string columnName, string columnText, int columnWidth)
		{
			if (TasView.AllColumns[columnName] == null)
			{
				var column = new InputRoll.RollColumn
				{
					Name = columnName,
					Text = columnText,
					Width = columnWidth,
				};

				TasView.AllColumns.Add(column);
			}
		}

		private void EngageTastudio()
		{
			GlobalWin.MainForm.PauseOnFrame = null;
			GlobalWin.OSD.AddMessage("TAStudio engaged");
			SetTasMovieCallbacks();
			SetTextProperty();
			GlobalWin.MainForm.PauseEmulator();
			GlobalWin.MainForm.RelinquishControl(this);
			_originalEndAction = Global.Config.MovieEndAction;
			GlobalWin.MainForm.ClearRewindData();
			Global.Config.MovieEndAction = MovieEndAction.Record;
			GlobalWin.MainForm.SetMainformMovieInfo();
			Global.MovieSession.ReadOnly = true;
		}

		#endregion

		#region "Loading"

		private void ConvertCurrentMovieToTasproj()
		{
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie = Global.MovieSession.Movie.ToTasMovie();
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie.SwitchToRecord();
			Settings.RecentTas.Add(Global.MovieSession.Movie.Filename);
		}

		private bool LoadFile(FileInfo file)
		{
			if (!file.Exists)
			{
				Settings.RecentTas.HandleLoadError(file.FullName);
				return false;
			}

			CurrentTasMovie.Filename = file.FullName;
			try
			{
				CurrentTasMovie.Load();
			}
			catch
			{
				MessageBox.Show(
					"Tastudio could not open the file. Due to the loading process, the emulator/Tastudio may be in a unspecified state depending on the error.",
					"Tastudio",
					MessageBoxButtons.OK);
				return false;
			}
			Settings.RecentTas.Add(CurrentTasMovie.Filename);

			if (!HandleMovieLoadStuff())
				return false;

			RefreshDialog();
			return true;
		}

		private void StartNewTasMovie()
		{
			if (AskSaveChanges())
			{
				Global.MovieSession.Movie = new TasMovie(false, _saveBackgroundWorker);
				CurrentTasMovie.PropertyChanged += new PropertyChangedEventHandler(this.TasMovie_OnPropertyChanged);
				CurrentTasMovie.Filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
				CurrentTasMovie.PopulateWithDefaultHeaderValues();
				SetTasMovieCallbacks();
				CurrentTasMovie.ClearChanges(); // Don't ask to save changes here.
				HandleMovieLoadStuff();

				RefreshDialog();
			}
		}

		private bool HandleMovieLoadStuff(TasMovie movie = null)
		{
			if (movie == null)
				movie = CurrentTasMovie;

			WantsToControlStopMovie = false;
			bool result = StartNewMovieWrapper(movie.InputLogLength == 0, movie);
			if (!result)
				return false;
			WantsToControlStopMovie = true;

			CurrentTasMovie.ChangeLog.ClearLog();
			CurrentTasMovie.ClearChanges();

			SetTextProperty();
			MessageStatusLabel.Text = Path.GetFileName(CurrentTasMovie.Filename) + " loaded.";

			return true;
		}
		private bool StartNewMovieWrapper(bool record, IMovie movie = null)
		{
			_initializing = true;
			if (movie == null)
				movie = CurrentTasMovie;
			bool result = GlobalWin.MainForm.StartNewMovie(movie, record);
			_initializing = false;

			return result;
		}

		private void DummyLoadProject(string path)
		{
			if (AskSaveChanges())
				LoadFile(new FileInfo(path));
		}
		private void DummyLoadMacro(string path)
		{
			if (!TasView.SelectedRows.Any())
				return;

			MovieZone loadZone = new MovieZone(path);
			if (loadZone != null)
			{
				loadZone.Start = TasView.FirstSelectedIndex.Value;
				loadZone.PlaceZone(CurrentTasMovie);
			}
		}

		private void SetColumnsFromCurrentStickies()
		{
			foreach (var column in TasView.VisibleColumns)
			{
				if (Global.StickyXORAdapter.IsSticky(column.Name))
				{
					column.Emphasis = true;
				}
			}
		}

		#endregion

		private void TastudioToStopMovie()
		{
			Global.MovieSession.StopMovie(false);
			GlobalWin.MainForm.SetMainformMovieInfo();
		}

		private void DisengageTastudio()
		{
			GlobalWin.MainForm.PauseOnFrame = null;
			GlobalWin.OSD.AddMessage("TAStudio disengaged");
			Global.MovieSession.Movie = MovieService.DefaultInstance;
			GlobalWin.MainForm.TakeBackControl();
			Global.Config.MovieEndAction = _originalEndAction;
			GlobalWin.MainForm.SetMainformMovieInfo();
			// Do not keep TAStudio's disk save states.
			if (Directory.Exists(PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global", "TAStudio states"].Path, null)))
				Directory.Delete(PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global", "TAStudio states"].Path, null), true);
		}

		/// <summary>
		/// Used when starting a new project
		/// </summary>
		private static string DefaultTasProjName()
		{
			return Path.Combine(
				PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
				TasMovie.DefaultProjectName + "." + TasMovie.Extension);
		}

		/// <summary>
		/// Used for things like SaveFile dialogs to suggest a name to the user
		/// </summary>
		/// <returns></returns>
		private static string SuggestedTasProjName()
		{
			return Path.Combine(
				PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
				PathManager.FilesystemSafeName(Global.Game) + "." + TasMovie.Extension);
		}

		private void SetTextProperty()
		{
			var text = "TAStudio";
			if (CurrentTasMovie != null)
			{
				text += " - " + CurrentTasMovie.Name + (CurrentTasMovie.Changes ? "*" : "");
			}

			if (this.InvokeRequired)
			{
				this.Invoke(() => Text = text);
			}
			else
			{
				Text = text;
			}
		}

		public void RefreshDialog()
		{
			RefreshTasView();

			if (MarkerControl != null)
				MarkerControl.UpdateValues();

			if (undoForm != null && !undoForm.IsDisposed)
				undoForm.UpdateValues();
		}

		private void RefreshTasView()
		{
			CurrentTasMovie.UseInputCache = true;
			if (TasView.RowCount != CurrentTasMovie.InputLogLength + 1)
				TasView.RowCount = CurrentTasMovie.InputLogLength + 1;
			TasView.Refresh();

			CurrentTasMovie.FlushInputCache();
			CurrentTasMovie.UseInputCache = false;

			lastRefresh = Global.Emulator.Frame;
		}

		private void DoAutoRestore()
		{
			if (Settings.AutoRestoreLastPosition && _autoRestoreFrame.HasValue)
			{
				if (_autoRestoreFrame > Emulator.Frame) // Don't unpause if we are already on the desired frame, else runaway seek
				{
					GlobalWin.MainForm.PauseOnFrame = _autoRestoreFrame;
					GlobalWin.MainForm.UnpauseEmulator();
				}
			}

			_autoRestoreFrame = null;
		}

		private void StartAtNearestFrameAndEmulate(int frame)
		{
			CurrentTasMovie.SwitchToPlay();
			KeyValuePair<int, byte[]> closestState = CurrentTasMovie.TasStateManager.GetStateClosestToFrame(frame);
			if (closestState.Value != null)
			{
				LoadState(closestState);
			}

			if (GlobalWin.MainForm.EmulatorPaused)
				GlobalWin.MainForm.PauseOnFrame = frame;
			GlobalWin.MainForm.UnpauseEmulator();
		}

		private void LoadState(KeyValuePair<int, byte[]> state)
		{
			StatableEmulator.LoadStateBinary(new BinaryReader(new MemoryStream(state.Value.ToArray())));

			if (state.Key == 0 && CurrentTasMovie.StartsFromSavestate)
			{
				Emulator.ResetCounters();
			}

			_hackyDontUpdate = true;
			GlobalWin.Tools.UpdateBefore();
			GlobalWin.Tools.UpdateAfter();
			_hackyDontUpdate = false;
		}

		private void UpdateOtherTools() // a hack probably, surely there is a better way to do this
		{
			_hackyDontUpdate = true;
			GlobalWin.Tools.UpdateBefore();
			GlobalWin.Tools.UpdateAfter();
			_hackyDontUpdate = false;
		}

		public void TogglePause()
		{
			GlobalWin.MainForm.TogglePause();
		}

		private void SetSplicer()
		{
			// TODO: columns selected
			// TODO: clipboard
			var list = TasView.SelectedRows;
			string message = "Selected: ";

			if (list.Any())
			{
				message += list.Count() + " rows 0 col, Clipboard: ";
			}
			else
			{
				message += list.Count() + " none, Clipboard: ";
			}

			message += _tasClipboard.Any() ? _tasClipboard.Count + " rows 0 col" : "empty";

			SplicerStatusLabel.Text = message;
		}

		public void CallAddMarkerPopUp(int? frame = null)
		{
			var markerFrame = frame ?? TasView.LastSelectedIndex ?? Emulator.Frame;
			InputPrompt i = new InputPrompt
			{
				Text = "Marker for frame " + markerFrame,
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue = CurrentTasMovie.Markers.IsMarker(markerFrame) ? CurrentTasMovie.Markers.PreviousOrCurrent(markerFrame).Message : ""
			};

			var result = i.ShowHawkDialog();

			if (result == DialogResult.OK)
			{
				CurrentTasMovie.Markers.Add(new TasMovieMarker(markerFrame, i.PromptText));
				MarkerControl.UpdateValues();
			}
		}

		public void CallEditMarkerPopUp(TasMovieMarker marker)
		{
			var markerFrame = marker.Frame;
			InputPrompt i = new InputPrompt
			{
				Text = "Marker for frame " + markerFrame,
				TextInputType = InputPrompt.InputType.Text,
				Message = "Enter a message",
				InitialValue = CurrentTasMovie.Markers.IsMarker(markerFrame) ? CurrentTasMovie.Markers.PreviousOrCurrent(markerFrame).Message : ""
			};

			var result = i.ShowHawkDialog();

			if (result == DialogResult.OK)
			{
				marker.Message = i.PromptText;
				MarkerControl.UpdateValues();
			}
		}

		private void UpdateChangesIndicator()
		{
			// TODO
		}

		private void DoTriggeredAutoRestoreIfNeeded()
		{
			if (_triggerAutoRestore)
			{
				int? pauseOn = GlobalWin.MainForm.PauseOnFrame;
				GoToLastEmulatedFrameIfNecessary(_triggerAutoRestoreFromFrame.Value);

				if (pauseOn.HasValue && _autoRestoreFrame.HasValue && _autoRestoreFrame < pauseOn)
				{ // If we are already seeking to a later frame don't shorten that journey here
					_autoRestoreFrame = GlobalWin.MainForm.PauseOnFrame;
				}

				DoAutoRestore();

				_triggerAutoRestore = false;
				_triggerAutoRestoreFromFrame = null;
			}
		}

		#region Dialog Events

		private void Tastudio_Closing(object sender, FormClosingEventArgs e)
		{
			if (!_initialized)
				return;

			_exiting = true;
			if (AskSaveChanges())
			{
				WantsToControlStopMovie = false;
				GlobalWin.MainForm.StopMovie(saveChanges: false);
				DisengageTastudio();
			}
			else
			{
				e.Cancel = true;
				_exiting = false;
			}

			if (undoForm != null)
				undoForm.Close();
		}

		/// <summary>
		/// This method is called everytime the Changes property is toggled on a TasMovie instance.
		/// </summary>
		private void TasMovie_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			SetTextProperty();
		}

		private void TAStudio_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void TAStudio_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			if (Path.GetExtension(filePaths[0]) == "." + TasMovie.Extension)
			{
				FileInfo file = new FileInfo(filePaths[0]);
				if (file.Exists)
				{
					LoadFile(file);
				}
			}
		}

		private void TAStudio_MouseLeave(object sender, EventArgs e)
		{
			DoTriggeredAutoRestoreIfNeeded();
		}

		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == Keys.Tab ||
				keyData == (Keys.Shift | Keys.Tab) ||
				keyData == Keys.Space)
			{
				return true;
			}

			return base.ProcessCmdKey(ref msg, keyData);
		}

		#endregion

		#region "Marker Control right-click menu"
		private void MarkerContextMenu_Opening(object sender, CancelEventArgs e)
		{
			EditMarkerContextMenuItem.Enabled =
			RemoveMarkerContextMenuItem.Enabled =
			ScrollToMarkerToolStripMenuItem.Enabled =
				MarkerControl.MarkerInputRoll.SelectedRows.Any();
		}

		private void ScrollToMarkerToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SetVisibleIndex(MarkerControl.SelectedMarkerFrame());
			RefreshTasView();
		}

		private void EditMarkerContextMenuItem_Click(object sender, EventArgs e)
		{
			MarkerControl.EditMarker();
		}

		private void AddMarkerContextMenuItem_Click(object sender, EventArgs e)
		{
			MarkerControl.AddMarker();
		}

		private void RemoveMarkerContextMenuItem_Click(object sender, EventArgs e)
		{
			MarkerControl.RemoveMarker();
		}
		#endregion

		private bool AutoAdjustInput()
		{
			TasMovieRecord lagLog = CurrentTasMovie[Emulator.Frame - 1]; // Minus one because get frame is +1;
			bool isLag = Emulator.AsInputPollable().IsLagFrame;

			if (lagLog.WasLagged.HasValue)
			{
				if (lagLog.WasLagged.Value && !isLag)
				{ // Deleting this frame requires rewinding a frame.
					CurrentTasMovie.ChangeLog.AddInputBind(Global.Emulator.Frame - 1, true, "Bind Input; Delete " + (Global.Emulator.Frame - 1));
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;

					CurrentTasMovie.RemoveFrame(Global.Emulator.Frame - 1);
					CurrentTasMovie.RemoveLagHistory(Global.Emulator.Frame); // Removes from WasLag

					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;
					GoToFrame(Emulator.Frame - 1);
					return true;
				}
				else if (!lagLog.WasLagged.Value && isLag)
				{ // (it shouldn't need to rewind, since the inserted input wasn't polled)
					CurrentTasMovie.ChangeLog.AddInputBind(Global.Emulator.Frame - 1, false, "Bind Input; Insert " + (Global.Emulator.Frame - 1));
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;

					CurrentTasMovie.InsertInput(Global.Emulator.Frame - 1, CurrentTasMovie.GetInputLogEntry(Emulator.Frame - 2));
					CurrentTasMovie.InsertLagHistory(Global.Emulator.Frame, true);

					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;
					return true;
				}
			}

			return false;
		}

		private void TAStudio_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F)
				TasPlaybackBox.FollowCursor ^= true;
		}

	}
}
