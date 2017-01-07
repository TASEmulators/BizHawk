using System;
using System.Collections.Generic;
using System.Drawing;
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
	public partial class TAStudio : ToolFormBase, IToolFormAutoConfig, IControlMainform
	{
		// TODO: UI flow that conveniently allows to start from savestate
		private const string CursorColumnName = "CursorColumn";
		private const string FrameColumnName = "FrameColumn";

		private readonly List<TasClipboardEntry> _tasClipboard = new List<TasClipboardEntry>();

		private BackgroundWorker _saveBackgroundWorker;
		private BackgroundWorker _seekBackgroundWorker;

		private MovieEndAction _originalEndAction; // The movie end behavior selected by the user (that is overridden by TAStudio)
		private Dictionary<string, string> GenerateColumnNames()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			return (lg as Bk2LogEntryGenerator).Map();
		}

		private UndoHistoryForm _undoForm;
		private Timer _autosaveTimer = new Timer();

		public ScreenshotPopupControl ScreenshotControl = new ScreenshotPopupControl
		{
			Size = new Size(256, 240),
		};

		public string statesPath
		{
			get { return PathManager.MakeAbsolutePath(Global.Config.PathEntries["Global", "TAStudio states"].Path, null); }
		}

		public bool IsInMenuLoop { get; private set; }

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
				ScrollSpeed = 6;
				FollowCursorAlwaysScroll = false;
				FollowCursorScrollMethod = "near";
				BranchCellHoverInterval = 1;
				SeekingCutoffInterval = 2; // unused, relying on VisibleRows is smarter
				AutoRestoreOnMouseUpOnly = false; // default to taseditor way, must be harmless since we suspend rerecord counting while drawing
				AutosaveInterval = 120000;
				AutosaveAsBk2 = false;
				AutosaveAsBackupFile = false;
				BackupPerFileSave = false;
				SingleClickFloatEdit = false;
                // default to taseditor fashion
                DenoteStatesWithIcons = false;
                DenoteStatesWithBGColor = true;
                DenoteMarkersWithIcons = false;
                DenoteMarkersWithBGColor = true;
			}

			public RecentFiles RecentTas { get; set; }
			public bool DrawInput { get; set; }
			public bool AutoPause { get; set; }
			public bool AutoRestoreLastPosition { get; set; }
			public bool FollowCursor { get; set; }
			public bool EmptyMarkers { get; set; }
			public int ScrollSpeed { get; set; }
			public bool FollowCursorAlwaysScroll { get; set; }
            public string FollowCursorScrollMethod { get; set; }
			public int BranchCellHoverInterval { get; set; }
			public int SeekingCutoffInterval { get; set; }
			public bool AutoRestoreOnMouseUpOnly { get; set; }
			public uint AutosaveInterval { get; set; }
			public bool AutosaveAsBk2 { get; set; }
			public bool AutosaveAsBackupFile { get; set; }
			public bool BackupPerFileSave { get; set; }
			public bool SingleClickFloatEdit { get; set; }
            public bool DenoteStatesWithIcons { get; set; }
            public bool DenoteStatesWithBGColor { get; set; }
            public bool DenoteMarkersWithIcons { get; set; }
            public bool DenoteMarkersWithBGColor { get; set; }
			public int MainVerticalSplitDistance { get; set; }
			public int BranchMarkerSplitDistance { get; set; }
		}

		public TasMovie CurrentTasMovie
		{
			get { return Global.MovieSession.Movie as TasMovie; }
		}

		public MainForm Mainform
		{
			get { return GlobalWin.MainForm; }
		}

		/// <summary>
		/// Separates "restore last position" logic from seeking caused by navigation.
		/// TASEditor never kills LastPositionFrame, and it only pauses on it,
		/// if it hasn't been greenzoned beforehand and middle mouse button was pressed.
		/// </summary>
		public int LastPositionFrame { get; set; }

		#region "Initializing"

		public TAStudio()
		{
			Settings = new TAStudioSettings();
			InitializeComponent();

			// TODO: show this at all times or hide it when saving is done?
			this.SavingProgressBar.Visible = false;

			InitializeSaveWorker();
			InitializeSeekWorker();

			WantsToControlStopMovie = true;
			WantsToControlRestartMovie = true;
			TasPlaybackBox.Tastudio = this;
			MarkerControl.Tastudio = this;
			BookMarkControl.Tastudio = this;
			MarkerControl.Emulator = this.Emulator;
			TasView.QueryItemText += TasView_QueryItemText;
			TasView.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView.QueryRowBkColor += TasView_QueryRowBkColor;
			TasView.QueryItemIcon += TasView_QueryItemIcon;
			TasView.QueryFrameLag += TasView_QueryFrameLag;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
			TasView.MultiSelect = true;
			TasView.MaxCharactersInHorizontal = 1;
			LastPositionFrame = -1;
		}

		private void AutosaveTimerEventProcessor(object sender, EventArgs e)
		{
			if (CurrentTasMovie == null)
				return;

			if (!CurrentTasMovie.Changes || Settings.AutosaveInterval == 0)
				return;

			if (Settings.AutosaveAsBackupFile)
			{
				if (Settings.AutosaveAsBk2)
					SaveBk2BackupMenuItem_Click(sender, e);
				else
					SaveBackupMenuItem_Click(sender, e);
			}
			else
			{
				if (Settings.AutosaveAsBk2)
					ToBk2MenuItem_Click(sender, e);
				else
					SaveTas(sender, e);
			}
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

		private void InitializeSeekWorker()
		{
			if (_seekBackgroundWorker != null)
			{
				_seekBackgroundWorker.Dispose();
				_seekBackgroundWorker = null; // Idk if this line is even useful.
			}

			_seekBackgroundWorker = new BackgroundWorker();
			_seekBackgroundWorker.WorkerReportsProgress = true;
			_seekBackgroundWorker.WorkerSupportsCancellation = true;

			_seekBackgroundWorker.DoWork += (s, e) =>
			{
				this.Invoke(() => this.MessageStatusLabel.Text = "Seeking...");
				this.Invoke(() => this.SavingProgressBar.Visible = true);
				for ( ; ; )
				{
					if (_seekBackgroundWorker.CancellationPending || !this.IsHandleCreated)
					{
						e.Cancel = true;
						break;
					}

					int diff = Emulator.Frame - _seekStartFrame.Value;
					int unit = Mainform.PauseOnFrame.Value - _seekStartFrame.Value;
					double progress = 0;

					if (diff != 0 && unit != 0)
						progress = (double)100d / unit * diff;
					if (progress < 0)
						progress = 0;

					_seekBackgroundWorker.ReportProgress((int)progress);
					System.Threading.Thread.Sleep(1);
				}
			};

			_seekBackgroundWorker.ProgressChanged += (s, e) =>
			{
				this.Invoke(() => this.SavingProgressBar.Value = e.ProgressPercentage);
			};

			_seekBackgroundWorker.RunWorkerCompleted += (s, e) =>
			{
				this.Invoke(() => this.SavingProgressBar.Visible = false);
				this.Invoke(() => this.MessageStatusLabel.Text = "");
				InitializeSeekWorker(); // Required, or it will error when trying to report progress again.
			};
		}

		private bool _initialized = false;
		private void Tastudio_Load(object sender, EventArgs e)
		{
			if (!InitializeOnLoad())
			{
				Close();
				DialogResult = DialogResult.Cancel;
				return;
			}

			// Set the screenshot to "1x" resolution of the core
			// cores like n64 and psx are going to still have sizes too big for the control, so cap them
			int width = VideoProvider.BufferWidth;
			int height = VideoProvider.BufferHeight;
			if (width > 320)
			{
				double ratio = 320.0 / (double)width;
				width = 320;
				height = (int)((double)(height) * ratio);
			}
			ScreenshotControl.DrawingHeight = height;
			ScreenshotControl.Size = new Size(width, ScreenshotControl.DrawingHeight + ScreenshotControl.UserPadding);


			ScreenshotControl.Visible = false;
			Controls.Add(ScreenshotControl);
			ScreenshotControl.BringToFront();

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

			TasView.InputPaintingMode = Settings.DrawInput;
			TasView.ScrollSpeed = Settings.ScrollSpeed;
			TasView.AlwaysScroll = Settings.FollowCursorAlwaysScroll;
			TasView.ScrollMethod = Settings.FollowCursorScrollMethod;
			TasView.SeekingCutoffInterval = Settings.SeekingCutoffInterval;
			BookMarkControl.HoverInterval = Settings.BranchCellHoverInterval;

			_autosaveTimer.Tick += AutosaveTimerEventProcessor;
			if (Settings.AutosaveInterval > 0)
			{
				_autosaveTimer.Interval = (int)Settings.AutosaveInterval;
				_autosaveTimer.Start();
			}

			// Remembering Split container logic
			int defaultMainSplitDistance = MainVertialSplit.SplitterDistance;
			int defaultBranchMarkerSplitDistance = BranchesMarkersSplit.SplitterDistance;

			ToolStripMenuItem restoreDefaults = TASMenu.Items
				.OfType<ToolStripMenuItem>()
				.Single(t => t.Name == "SettingsSubMenu")
				.DropDownItems
				.OfType<ToolStripMenuItem>()
				.Single(t => t.Text == "Restore &Defaults");

			restoreDefaults.Click += (o, ev) =>
			{
				MainVertialSplit.SplitterDistance = defaultMainSplitDistance;
				BranchesMarkersSplit.SplitterDistance = defaultBranchMarkerSplitDistance;
			};

			if (Settings.MainVerticalSplitDistance > 0)
			{
				MainVertialSplit.SplitterDistance = Settings.MainVerticalSplitDistance;
			}

			if (Settings.BranchMarkerSplitDistance > 0)
			{
				BranchesMarkersSplit.SplitterDistance = Settings.BranchMarkerSplitDistance;
			}

			////////////////

			RefreshDialog();
			_initialized = true;
		}

		private bool InitializeOnLoad()
		{
			Mainform.PauseOnFrame = null;
			Mainform.PauseEmulator();

			// Start Scenario 1: A regular movie is active
			if (Global.MovieSession.Movie.IsActive && !(Global.MovieSession.Movie is TasMovie))
			{
				var result = MessageBox.Show("In order to use Tastudio, a new project must be created from the current movie\nThe current movie will be saved and closed, and a new project file will be created\nProceed?", "Convert movie", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if (result == DialogResult.OK)
				{
					ConvertCurrentMovieToTasproj();
					StartNewMovieWrapper(false);
					SetUpColumns();
				}
				else
				{
					return false;
				}
			}

			// Start Scenario 2: A tasproj is already active
			else if (Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie is TasMovie)
			{
				bool result = LoadFile(new FileInfo(CurrentTasMovie.Filename), gotoFrame: Emulator.Frame);
				if (!result)
				{
					TasView.AllColumns.Clear();
					StartNewTasMovie();
				}
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
			return true;
		}

		private void SetTasMovieCallbacks(TasMovie movie = null)
		{
			if (movie == null)
				movie = CurrentTasMovie;
			movie.ClientSettingsForSave = ClientSettingsForSave;
			movie.GetClientSettingsOnLoad = GetClientSettingsOnLoad;
		}

		private string ClientSettingsForSave()
		{
			return TasView.UserSettingsSerialized();
		}

		private void GetClientSettingsOnLoad(string settingsJson)
		{
			TasView.LoadSettingsSerialized(settingsJson);
		}

		private void SetUpColumns()
		{
			TasView.AllColumns.Clear();
			AddColumn(CursorColumnName, string.Empty, 18);
			AddColumn(FrameColumnName, "Frame#", 68);

			var columnNames = GenerateColumnNames();
			InputRoll.RollColumn.InputType type;
			int digits = 1;
			foreach (var kvp in columnNames)
			{
				if (Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.Contains(kvp.Key))
				{
					Emulation.Common.ControllerDefinition.FloatRange range = Global.MovieSession.MovieControllerAdapter.Definition.FloatRanges
						[Global.MovieSession.MovieControllerAdapter.Definition.FloatControls.IndexOf(kvp.Key)];
					type = InputRoll.RollColumn.InputType.Float;
					digits = Math.Max(kvp.Value.Length, range.MaxDigits());
				}
				else
				{
					type = InputRoll.RollColumn.InputType.Boolean;
					digits = kvp.Value.Length;
				}
				AddColumn(kvp.Key, kvp.Value, (digits * 6) + 14, type);
			}

			var columnsToHide = TasView.AllColumns
				.Where(c =>
					// todo: make a proper user editable list?
					c.Name == "Power" ||
					c.Name == "Reset" ||
					c.Name.StartsWith("Tilt") ||
					c.Name == "Light Sensor" ||
					c.Name == "Open" ||
					c.Name == "Close" ||
					c.Name == "Disc Select"
				);

			foreach (var column in columnsToHide)
			{
				column.Visible = false;
			}

			TasView.AllColumns.ColumnsChanged();

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

		public void AddColumn(string columnName, string columnText, int columnWidth, InputRoll.RollColumn.InputType columnType = InputRoll.RollColumn.InputType.Boolean)
		{
			if (TasView.AllColumns[columnName] == null)
			{
				var column = new InputRoll.RollColumn
				{
					Name = columnName,
					Text = columnText,
					Width = columnWidth,
					Type = columnType
				};

				TasView.AllColumns.Add(column);
			}
		}

		private void EngageTastudio()
		{
			GlobalWin.OSD.AddMessage("TAStudio engaged");
			SetTasMovieCallbacks();
			SetTextProperty();
			Mainform.RelinquishControl(this);
			_originalEndAction = Global.Config.MovieEndAction;
			Mainform.ClearRewindData();
			Global.Config.MovieEndAction = MovieEndAction.Record;
			Mainform.SetMainformMovieInfo();
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

		private bool LoadFile(FileInfo file, bool startsFromSavestate = false, int gotoFrame = 0)
		{
			if (!file.Exists)
			{
				Settings.RecentTas.HandleLoadError(file.FullName);
				return false;
			}

			TasMovie newMovie = new TasMovie(startsFromSavestate, _saveBackgroundWorker);
			newMovie.TasStateManager.InvalidateCallback = GreenzoneInvalidated;
			newMovie.Filename = file.FullName;

			if (!HandleMovieLoadStuff(newMovie))
				return false;

			Settings.RecentTas.Add(newMovie.Filename); // only add if it did load

			if (startsFromSavestate)
				GoToFrame(0);
			else if (gotoFrame > 0)
				GoToFrame(gotoFrame);
			else
				GoToFrame(CurrentTasMovie.Session.CurrentFrame);

			if (TasView.AllColumns.Count == 0 || file.Extension != "." + TasMovie.Extension)
				SetUpColumns();
			else
				SetUpToolStripColumns();

			CurrentTasMovie.PropertyChanged += new PropertyChangedEventHandler(this.TasMovie_OnPropertyChanged);
			CurrentTasMovie.CurrentBranch = CurrentTasMovie.Session.CurrentBranch;
			
			// clear all selections
			TasView.DeselectAll();
			BookMarkControl.Restart();
			MarkerControl.Restart();

			RefreshDialog();
			return true;
		}

		private void StartNewTasMovie()
		{
			if (AskSaveChanges())
			{
				Global.MovieSession.Movie = new TasMovie(false, _saveBackgroundWorker);
				var stateManager = (Global.MovieSession.Movie as TasMovie).TasStateManager;
				stateManager.MountWriteAccess();
				stateManager.InvalidateCallback = GreenzoneInvalidated;
				CurrentTasMovie.PropertyChanged += new PropertyChangedEventHandler(this.TasMovie_OnPropertyChanged);
				CurrentTasMovie.Filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
				CurrentTasMovie.PopulateWithDefaultHeaderValues();
				SetTasMovieCallbacks();
				CurrentTasMovie.ClearChanges(); // Don't ask to save changes here.
				HandleMovieLoadStuff();
				CurrentTasMovie.TasStateManager.Capture(); // Capture frame 0 always.
				// clear all selections
				TasView.DeselectAll();
				BookMarkControl.Restart();
				MarkerControl.Restart();
				SetUpColumns();
				RefreshDialog();
			}
		}

		private bool HandleMovieLoadStuff(TasMovie movie = null)
		{
			bool result;
			WantsToControlStopMovie = false;

			if (movie == null)
			{
				movie = CurrentTasMovie;
				result = StartNewMovieWrapper(movie.InputLogLength == 0, movie);
			}
			else
			{
				if (movie.Filename.EndsWith(TasMovie.Extension))
				{
				
				}
				else if (movie.Filename.EndsWith(".bkm") || movie.Filename.EndsWith(".bk2")) // was loaded using "All Files" filter. todo: proper extention iteration
				{
					var result1 = MessageBox.Show("This is a regular movie, a new project must be created from it, in order to use in TAStudio\nProceed?", "Convert movie", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
					if (result1 == DialogResult.OK)
					{
						ConvertCurrentMovieToTasproj();
					}
					else
					{
						return false;
					}
				}
				else 
				{
					MessageBox.Show("This is not a BizHawk movie!", "Movie load error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
				result = StartNewMovieWrapper(false, movie);
			}

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
			SetTasMovieCallbacks(movie as TasMovie);
			bool result = Mainform.StartNewMovie(movie, record);
			TastudioPlayMode();
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
			if (!TasView.AnyRowsSelected)
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

		private void TastudioPlayMode()
		{
			TasPlaybackBox.RecordingMode = false;
		}

		private void TastudioRecordMode()
		{
			TasPlaybackBox.RecordingMode = true;
		}

		private void TastudioStopMovie()
		{
			Global.MovieSession.StopMovie(false);
			Mainform.SetMainformMovieInfo();
		}

		private void DisengageTastudio()
		{
			Mainform.PauseOnFrame = null;
			GlobalWin.OSD.AddMessage("TAStudio disengaged");
			Global.MovieSession.Movie = MovieService.DefaultInstance;
			Mainform.TakeBackControl();
			Global.Config.MovieEndAction = _originalEndAction;
			Mainform.SetMainformMovieInfo();
			// Do not keep TAStudio's disk save states.
			//if (Directory.Exists(statesPath)) Directory.Delete(statesPath, true);
			//TODO - do we need to dispose something here instead?
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

		public void RefreshDialog(bool refreshTasView = true)
		{
			if (refreshTasView)
				RefreshTasView();

			if (MarkerControl != null)
				MarkerControl.UpdateValues();

			if (BookMarkControl != null)
				BookMarkControl.UpdateValues();

			if (_undoForm != null && !_undoForm.IsDisposed)
				_undoForm.UpdateValues();
		}

		private void RefreshTasView()
		{
			CurrentTasMovie.UseInputCache = true;
			if (TasView.RowCount != CurrentTasMovie.InputLogLength + 1)
				TasView.RowCount = CurrentTasMovie.InputLogLength + 1;
			TasView.Refresh();

			CurrentTasMovie.FlushInputCache();
			CurrentTasMovie.UseInputCache = false;

			lastRefresh = Emulator.Frame;
		}

		private void DoAutoRestore()
		{
			if (Settings.AutoRestoreLastPosition && LastPositionFrame != -1)
			{
				if (LastPositionFrame > Emulator.Frame) // Don't unpause if we are already on the desired frame, else runaway seek
				{
					StartSeeking(LastPositionFrame);
				}
			}
			else
			{
				if (_autoRestorePaused.HasValue && !_autoRestorePaused.Value)
				{
					// this happens when we're holding the left button while unpaused - view scrolls down, new input gets drawn, seek pauses
					Mainform.UnpauseEmulator();
				}
				_autoRestorePaused = null;
			}
			//_autoRestoreFrame = null;
		}

		private void StartAtNearestFrameAndEmulate(int frame)
		{
			if (frame == Emulator.Frame)
				return;

			_wasRecording = CurrentTasMovie.IsRecording || _wasRecording;
			TastudioPlayMode();
			KeyValuePair<int, byte[]> closestState = CurrentTasMovie.TasStateManager.GetStateClosestToFrame(frame);
			if (closestState.Value != null && (frame < Emulator.Frame || closestState.Key > Emulator.Frame))
			{
				LoadState(closestState);
			}

			// frame == Emualtor.Frame when frame == 0
			if (frame > Emulator.Frame)
			{
				if (Mainform.EmulatorPaused || Mainform.IsSeeking) // make seek frame keep up with emulation on fast scrolls
				{
					StartSeeking(frame);
				}
			}
		}

		public void LoadState(KeyValuePair<int, byte[]> state)
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

		public void AddBranchExternal()
		{
			BookMarkControl.AddBranchExternal();
		}

		public void RemoveBranchExtrenal()
		{
			BookMarkControl.RemoveBranchExtrenal();
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
			Mainform.TogglePause();
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

		private void UpdateChangesIndicator()
		{
			// TODO
		}

		private void DoTriggeredAutoRestoreIfNeeded()
		{
			if (_triggerAutoRestore)
			{
				DoAutoRestore();

				_triggerAutoRestore = false;
				_autoRestorePaused = null;
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
				TastudioStopMovie();
				DisengageTastudio();
			}
			else
			{
				e.Cancel = true;
				_exiting = false;
			}

			if (_undoForm != null)
				_undoForm.Close();
		}

		/// <summary>
		/// This method is called everytime the Changes property is toggled on a TasMovie instance.
		/// </summary>
		private void TasMovie_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			SetTextProperty();
		}

		private void LuaConsole_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}
		private void TAStudio_DragDrop(object sender, DragEventArgs e)
		{
			if (!AskSaveChanges())
				return;

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

		private bool AutoAdjustInput()
		{
			TasMovieRecord lagLog = CurrentTasMovie[Emulator.Frame - 1]; // Minus one because get frame is +1;
			bool isLag = Emulator.AsInputPollable().IsLagFrame;

			if (lagLog.WasLagged.HasValue)
			{
				if (lagLog.WasLagged.Value && !isLag)
				{ // Deleting this frame requires rewinding a frame.
					CurrentTasMovie.ChangeLog.AddInputBind(Emulator.Frame - 1, true, "Bind Input; Delete " + (Emulator.Frame - 1));
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;

					CurrentTasMovie.RemoveFrame(Emulator.Frame - 1);
					CurrentTasMovie.RemoveLagHistory(Emulator.Frame); // Removes from WasLag

					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;
					GoToFrame(Emulator.Frame - 1);
					return true;
				}
				else if (!lagLog.WasLagged.Value && isLag)
				{ // (it shouldn't need to rewind, since the inserted input wasn't polled)
					CurrentTasMovie.ChangeLog.AddInputBind(Emulator.Frame - 1, false, "Bind Input; Insert " + (Emulator.Frame - 1));
					bool wasRecording = CurrentTasMovie.ChangeLog.IsRecording;
					CurrentTasMovie.ChangeLog.IsRecording = false;

					CurrentTasMovie.InsertInput(Emulator.Frame - 1, CurrentTasMovie.GetInputLogEntry(Emulator.Frame - 2));
					CurrentTasMovie.InsertLagHistory(Emulator.Frame, true);

					CurrentTasMovie.ChangeLog.IsRecording = wasRecording;
					return true;
				}
			}

			return false;
		}

		private void TAStudio_KeyDown(object sender, KeyEventArgs e)
		{
			//if (e.KeyCode == Keys.F)
			//	TasPlaybackBox.FollowCursor ^= true;
		}

		private void MainVertialSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.MainVerticalSplitDistance = MainVertialSplit.SplitterDistance;
		}

		private void BranchesMarkersSplit_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Settings.BranchMarkerSplitDistance = BranchesMarkersSplit.SplitterDistance;
		}

		private void TasView_CellDropped(object sender, InputRoll.CellEventArgs e)
		{
			if (e.NewCell != null && e.NewCell.RowIndex.HasValue &&
				!CurrentTasMovie.Markers.IsMarker(e.NewCell.RowIndex.Value))
			{
				var currentMarker = CurrentTasMovie.Markers.Single(m => m.Frame == e.OldCell.RowIndex.Value);
				int newFrame = e.NewCell.RowIndex.Value;
				var newMarker = new TasMovieMarker(newFrame, currentMarker.Message);
				CurrentTasMovie.Markers.Remove(currentMarker);
				CurrentTasMovie.Markers.Add(newMarker);
				RefreshDialog();
			}
		}

		private void NewFromSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			NewFromNowMenuItem.Enabled =
				CurrentTasMovie.InputLogLength > 0
				&& !CurrentTasMovie.StartsFromSaveRam;

			NewFromCurrentSaveRamMenuItem.Enabled =
				CurrentTasMovie.InputLogLength > 0
				&& SaveRamEmulator != null;
		}

		private void TASMenu_MenuActivate(object sender, EventArgs e)
		{
			IsInMenuLoop = true;
		}

		private void TASMenu_MenuDeactivate(object sender, EventArgs e)
		{
			IsInMenuLoop = false;
		}

		// Stupid designer
		protected void DragEnterWrapper(object sender, DragEventArgs e)
		{
			base.GenericDragEnter(sender, e);
		}

		private void TasPlaybackBox_Load(object sender, EventArgs e)
		{

		}
	}
}
