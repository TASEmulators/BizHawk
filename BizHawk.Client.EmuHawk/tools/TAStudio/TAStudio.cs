using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		private MovieEndAction _originalEndAction; // The movie end behavior selected by the user (that is overridden by TAStudio)
		private Dictionary<string, string> GenerateColumnNames()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			return (lg as Bk2LogEntryGenerator).Map();
		}

		private int? _autoRestoreFrame; // The frame auto-restore will restore to, if set

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
			TasView.MaxCharactersInHorizontal = 1;
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

		private static void ConvertCurrentMovieToTasproj()
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
			_currentTasMovie = Global.MovieSession.Movie as TasMovie;
			SetTasMovieCallbacks();
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

		private void SetTasMovieCallbacks()
		{
			_currentTasMovie.ClientSettingsForSave = ClientSettingsForSave;
			_currentTasMovie.GetClientSettingsOnLoad = GetClientSettingsOnLoad;
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
			if (_currentTasMovie != null)
			{
				text += " - " + _currentTasMovie.Name + (_currentTasMovie.Changes ? "*" : "");
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

				var result = GlobalWin.MainForm.StartNewMovie(movie, shouldRecord);
				if (!result)
				{
					return false;
				}

				_currentTasMovie = Global.MovieSession.Movie as TasMovie;
				SetTasMovieCallbacks();

				WantsToControlStopMovie = true;
				Global.Config.RecentTas.Add(path);
				Text = "TAStudio - " + _currentTasMovie.Name;

				RefreshDialog();
				return true;
			}

			return false;
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

		private void DoAutoRestore()
		{
			if (Global.Config.TAStudioAutoRestoreLastPosition && _autoRestoreFrame.HasValue)
			{
				if (_autoRestoreFrame > Global.Emulator.Frame) // Don't unpause if we are already on the desired frame, else runaway seek
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
			_currentTasMovie.SwitchToPlay();
			var closestState = _currentTasMovie.TasStateManager.GetStateClosestToFrame(frame);
			if (closestState != null)
			{
				LoadState(closestState.ToArray());
				
			}

			GlobalWin.MainForm.PauseOnFrame = frame;
			GlobalWin.MainForm.UnpauseEmulator();
		}

		private void LoadState(byte[] state)
		{
			Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(state)));

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
			GlobalWin.MainForm.StartNewMovie(_currentTasMovie, record: true);
			_currentTasMovie.TasStateManager.Capture();
			_currentTasMovie.SwitchToRecord();
			_currentTasMovie.ClearChanges();
		}

		#region Dialog Events

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

			LoadConfigSettings();
			SetColumnsFromCurrentStickies();
			RightClickMenu.Items.AddRange(TasView.GenerateContextMenuItems().ToArray());
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
				TasView.SelectedRows.Any();

			RemoveMarkersContextMenuItem.Enabled = _currentTasMovie.Markers.Any(m => TasView.SelectedRows.Contains(m.Frame)); // Disable the option to remove markers if no markers are selected (FCEUX does this).
		}

		#endregion
	}
}
