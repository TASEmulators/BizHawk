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
	public partial class TAStudio : Form, IToolForm, IControlMainform
	{
		// TODO: UI flow that conveniently allows to start from savestate
		private const string MarkerColumnName = "MarkerColumn";
		private const string FrameColumnName = "FrameColumn";

		private readonly List<TasClipboardEntry> _tasClipboard = new List<TasClipboardEntry>();

		private int _defaultWidth;
		private int _defaultHeight;
		private MovieEndAction _originalEndAction; // The movie end behavior selected by the user (that is overridden by TAStudio)
		private Dictionary<string, string> GenerateColumnNames()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			return (lg as Bk2LogEntryGenerator).Map();
		}

		private int? _autoRestoreFrame; // The frame auto-restore will restore to, if set

		public TasMovie CurrentTasMovie
		{
			get { return Global.MovieSession.Movie as TasMovie; }
		}

		public TAStudio()
		{
			InitializeComponent();
			WantsToControlStopMovie = true;
			TasPlaybackBox.Tastudio = this;
			MarkerControl.Tastudio = this;
			MarkerControl.Emulator = this.Emulator;
			TasView.QueryItemText += TasView_QueryItemText;
			TasView.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView.QueryItemIcon += TasView_QueryItemIcon;

			TopMost = Global.Config.TAStudioSettings.TopMost;
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
			TasView.MultiSelect = true;
			TasView.MaxCharactersInHorizontal = 1;
			WantsToControlRestartMovie = true;
		}

		private void TastudioToStopMovie()
		{
			Global.MovieSession.StopMovie(false);
			GlobalWin.MainForm.SetMainformMovieInfo();
		}

		private static void ConvertCurrentMovieToTasproj()
		{
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie = Global.MovieSession.Movie.ToTasMovie();
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie.SwitchToRecord();
			Global.Config.RecentTas.Add(Global.MovieSession.Movie.Filename);
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
		}

		private void DisengageTastudio()
		{
			GlobalWin.MainForm.PauseOnFrame = null;
			GlobalWin.OSD.AddMessage("TAStudio disengaged");
			Global.MovieSession.Movie = MovieService.DefaultInstance;
			GlobalWin.MainForm.TakeBackControl();
			Global.Config.MovieEndAction = _originalEndAction;
			GlobalWin.MainForm.SetMainformMovieInfo();
		}

		private void NewTasMovie()
		{
			Global.MovieSession.Movie = new TasMovie();
			SetTasMovieCallbacks();
			CurrentTasMovie.PropertyChanged += new PropertyChangedEventHandler(this.TasMovie_OnPropertyChanged);
			CurrentTasMovie.Filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
			CurrentTasMovie.PopulateWithDefaultHeaderValues();
			CurrentTasMovie.ClearChanges();
			TasView.RowCount = 1;
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

		private void SetTasMovieCallbacks()
		{
			CurrentTasMovie.ClientSettingsForSave = ClientSettingsForSave;
			CurrentTasMovie.GetClientSettingsOnLoad = GetClientSettingsOnLoad;
		}

		private void StartNewTasMovie()
		{
			if (AskSaveChanges())
			{
				NewTasMovie();
				WantsToControlStopMovie = false;
				StartNewMovieWrapper(record: true);
				CurrentTasMovie.ClearChanges();
				WantsToControlStopMovie = true;
				SetTextProperty();
				RefreshDialog();
			}
		}

		private void DummyLoadProject(string path)
		{
			LoadProject(path);
		}

		private string ClientSettingsForSave()
		{
			return TasView.UserSettingsSerialized();
		}

		private void GetClientSettingsOnLoad(string settingsJson)
		{
			TasView.LoadSettingsSerialized(settingsJson);
			TasView.Refresh();
		}

		private void SetTextProperty()
		{
			var text = "TAStudio";
			if (CurrentTasMovie != null)
			{
				text += " - " + CurrentTasMovie.Name + (CurrentTasMovie.Changes ? "*" : "");
			}

			Text = text;
		}

		public bool LoadProject(string path)
		{
			if (AskSaveChanges())
			{
				var movie = new TasMovie
				{
					Filename = path,
					ClientSettingsForSave = ClientSettingsForSave,
					GetClientSettingsOnLoad = GetClientSettingsOnLoad
				};

				movie.PropertyChanged += TasMovie_OnPropertyChanged;
				movie.Load();

				var file = new FileInfo(path);
				if (!file.Exists)
				{
					Global.Config.RecentTas.HandleLoadError(path);
				}

				WantsToControlStopMovie = false;

				var shouldRecord = movie.InputLogLength == 0;

				var result = StartNewMovieWrapper(movie: movie, record: shouldRecord);
				if (!result)
				{
					return false;
				}

				SetTasMovieCallbacks();

				WantsToControlStopMovie = true;
				Global.Config.RecentTas.Add(path);
				Text = "TAStudio - " + CurrentTasMovie.Name;

				RefreshDialog();
				return true;
			}

			return false;
		}

		public void RefreshDialog()
		{
			CurrentTasMovie.FlushInputCache();
			CurrentTasMovie.UseInputCache = true;
			TasView.RowCount = CurrentTasMovie.InputLogLength + 1;
			TasView.Refresh();

			CurrentTasMovie.FlushInputCache();
			CurrentTasMovie.UseInputCache = false;

			if (MarkerControl != null)
			{
				MarkerControl.UpdateValues();
			}
		}

		private void DoAutoRestore()
		{
			if (Global.Config.TAStudioAutoRestoreLastPosition && _autoRestoreFrame.HasValue)
			{
				if (_autoRestoreFrame > Emulator.Frame) // Don't unpause if we are already on the desired frame, else runaway seek
				{
					GlobalWin.MainForm.UnpauseEmulator();
					GlobalWin.MainForm.PauseOnFrame = _autoRestoreFrame;
				}
			}

			_autoRestoreFrame = null;
		}

		private void SetUpColumns()
		{
			TasView.AllColumns.Clear();
			AddColumn(MarkerColumnName, string.Empty, 18);
			AddColumn(FrameColumnName, "Frame#", 68);

			foreach (var kvp in GenerateColumnNames())
			{
				AddColumn(kvp.Key, kvp.Value, 20 * kvp.Value.Length);
			}
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

		private void StartAtNearestFrameAndEmulate(int frame)
		{
			CurrentTasMovie.SwitchToPlay();
			KeyValuePair<int, byte[]> closestState = CurrentTasMovie.TasStateManager.GetStateClosestToFrame(frame);
			if (closestState.Value != null)
			{
				LoadState(closestState);
			}

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

			message += _tasClipboard.Any() ? _tasClipboard.Count + " rows 0 col": "empty";

			SplicerStatusLabel.Text = message;
		}

		private void RefreshFloatingWindowControl()
		{
			Owner = Global.Config.TAStudioSettings.FloatingWindow ? null : GlobalWin.MainForm;
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
				CurrentTasMovie.Markers.Add(markerFrame, i.PromptText);
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

		// TODO: move me
		// Sets either the pending frame or the tas input log
		private void ToggleBoolState(int frame, string buttonName)
		{
			if (frame < CurrentTasMovie.InputLogLength)
			{
				CurrentTasMovie.ToggleBoolState(frame, buttonName);
			}
			else if (frame == Emulator.Frame && frame == CurrentTasMovie.InputLogLength)
			{
				Global.ClickyVirtualPadController.Toggle(buttonName);
			}
		}

		// TODO: move me
		// Sets either the pending frame or the tas input log
		private void SetBoolState(int frame, string buttonName, bool value)
		{
			if (frame < CurrentTasMovie.InputLogLength)
			{
				CurrentTasMovie.SetBoolState(frame, buttonName, value);
			}
			else if (frame == Emulator.Frame && frame == CurrentTasMovie.InputLogLength)
			{
				Global.ClickyVirtualPadController.SetBool(buttonName, value);
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

		private void NewDefaultProject()
		{
			NewTasMovie();
			StartNewMovieWrapper(record: true);
			CurrentTasMovie.TasStateManager.Capture();
			CurrentTasMovie.SwitchToRecord();
			CurrentTasMovie.ClearChanges();
		}

		private bool StartNewMovieWrapper(bool record, IMovie movie = null)
		{
			_initializing = true;
			var result = GlobalWin.MainForm.StartNewMovie(movie != null ? movie : CurrentTasMovie, record);
			_initializing = false;

			return result;
		}

		private void DoTriggeredAutoRestoreIfNeeded()
		{
			if (_triggerAutoRestore)
			{
				GoToLastEmulatedFrameIfNecessary(_triggerAutoRestoreFromFrame.Value);

				if (GlobalWin.MainForm.PauseOnFrame.HasValue &&
				_autoRestoreFrame.HasValue &&
				_autoRestoreFrame < GlobalWin.MainForm.PauseOnFrame) // If we are already seeking to a later frame don't shorten that journey here
				{
					_autoRestoreFrame = GlobalWin.MainForm.PauseOnFrame;
				}

				DoAutoRestore();

				_triggerAutoRestore = false;
				_triggerAutoRestoreFromFrame = null;
				
			}
		}

		private void LoadFile(FileInfo file)
		{
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
				return;
			}
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

		#region Dialog Events

		private void Tastudio_Load(object sender, EventArgs e)
		{
			InitializeOnLoad();
			LoadConfigSettings();
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
		}

		private void InitializeOnLoad()
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
				var result = LoadProject(Global.Config.RecentTas.MostRecent);
				if (!result)
				{
					TasView.AllColumns.Clear();
					NewDefaultProject();
				}
			}

			// Start Scenario 4: No movie, default behavior of engaging tastudio with a new default project
			else
			{
				NewDefaultProject();
			}

			EngageTastudio();

			if (!TasView.AllColumns.Any()) // If a project with column settings has already been loaded we don't need to do this
			{
				SetUpColumns();
			}
		}

		private void Tastudio_Closing(object sender, FormClosingEventArgs e)
		{
			if (AskSaveChanges())
			{
				WantsToControlStopMovie = false;
				SaveConfigSettings();
				GlobalWin.MainForm.StopMovie(saveChanges: false);
				DisengageTastudio();
			}
			else
			{
				e.Cancel = true;
			}
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
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
				var file = new FileInfo(filePaths[0]);
				if (file.Exists)
				{
					LoadProject(file.FullName);
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

		private void MarkerContextMenu_Opening(object sender, CancelEventArgs e)
		{
			EditMarkerContextMenuItem.Enabled =
			RemoveMarkerContextMenuItem.Enabled =
				MarkerControl.MarkerInputRoll.SelectedRows.Any();
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
	}
}
