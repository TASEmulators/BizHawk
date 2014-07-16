using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;
using System.Text;

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
		private TasMovie _tas;
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

		private IEnumerable<int> SelectedIndices
		{
			get
			{
				return TasView.SelectedIndices
					.OfType<int>()
					.OrderBy(frame => frame);
			}
		}

		public TAStudio()
		{
			InitializeComponent();
			TasView.QueryItemText += TasView_QueryItemText;
			TasView.QueryItemBkColor += TasView_QueryItemBkColor;
			TasView.VirtualMode = true;
			Closing += (o, e) =>
			{
				if (AskSave())
				{
					SaveConfigSettings();
					GlobalWin.MainForm.StopMovie(saveChanges: false);
					DisengageTastudio();
				}
				else
				{
					e.Cancel = true;
				}
			};

			TopMost = Global.Config.TAStudioSettings.TopMost;
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput;
			TasView.PointedCellChanged += TasView_PointedCellChanged;
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
				GlobalWin.MainForm.StartNewMovie(_tas, record: true);
			}

			EngageTastudio();
			SetUpColumns();
			LoadConfigSettings();
			RefreshDialog();
		}

		private void ConvertCurrentMovieToTasproj()
		{
			Global.MovieSession.Movie.Save();
			Global.MovieSession.Movie = Global.MovieSession.Movie.ToTasMovie();
			Global.MovieSession.Movie.Save();
		}

		private void EngageTastudio()
		{
			GlobalWin.OSD.AddMessage("TAStudio engaged");
			_tas = Global.MovieSession.Movie as TasMovie;
			GlobalWin.MainForm.PauseEmulator();
			GlobalWin.MainForm.RelinquishControl(this);
			_originalRewindStatus = Global.Rewinder.RewindActive;
			_originalEndAction = Global.Config.MovieEndAction;
			MarkerControl.Markers = _tas.Markers;
			GlobalWin.MainForm.EnableRewind(false);
			Global.Config.MovieEndAction = MovieEndAction.Record;
		}

		private void DisengageTastudio()
		{
			GlobalWin.OSD.AddMessage("TAStudio disengaged");
			Global.MovieSession.Movie = MovieService.DefaultInstance;
			GlobalWin.MainForm.TakeControl();
			GlobalWin.MainForm.EnableRewind(_originalRewindStatus);
			Global.Config.MovieEndAction = _originalEndAction;
		}

		private void NewTasMovie()
		{
			Global.MovieSession.Movie = new TasMovie();
			_tas = Global.MovieSession.Movie as TasMovie;
			_tas.Filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
			_tas.PopulateWithDefaultHeaderValues();
			_tas.ClearChanges();
		}

		private static string DefaultTasProjName()
		{
			return Path.Combine(
				PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
				PathManager.FilesystemSafeName(Global.Game) + "." + TasMovie.Extension);
		}

		private void StartNewTasMovie()
		{
			if (AskSave())
			{
				NewTasMovie();
				GlobalWin.MainForm.StartNewMovie(_tas, record: true);
				RefreshDialog();
			}
		}

		public void LoadProject(string path)
		{
			if (AskSave())
			{
				var movie = new TasMovie
				{
					Filename = path
				};

				var file = new FileInfo(path);
				if (!file.Exists)
				{
					ToolHelpers.HandleLoadError(Global.Config.RecentTas, path);
				}

				GlobalWin.MainForm.StartNewMovie(movie, record: false);
				_tas = Global.MovieSession.Movie as TasMovie;
				Global.Config.RecentTas.Add(path);
				RefreshDialog();
			}
		}

		public void RefreshDialog()
		{
			TasView.ItemCount = _tas.InputLogLength;
			if (MarkerControl != null)
			{
				MarkerControl.Refresh();
			}
		}

		// TODO: a better name
		private void GoToLastEmulatedFrameIfNecessary(int frame)
		{
			if (frame <= _tas.LastEmulatedFrame)
			{
				GoToFrame(frame);
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

		private void GoToFrame(int frame)
		{
			// If past greenzone, emulate and capture states
			// If past greenzone AND movie, record input and capture states
			// If in greenzone, loadstate
			// If near a greenzone item, load and emulate
			// Do capturing and recording as needed
			if (_tas[frame - 1].HasState) // Go back 1 frame and emulate
			{
				_tas.SwitchToPlay();
				Global.Emulator.LoadStateBinary(new BinaryReader(new MemoryStream(_tas[frame].State.ToArray())));
				Global.Emulator.FrameAdvance(true);
				GlobalWin.DisplayManager.NeedsToPaint = true;
				TasView.ensureVisible(frame);
				RefreshDialog();
			}
			else
			{
				// Find the earliest frame before this state
			}
		}

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
				TextInputType = InputPrompt.InputType.Text
			};

			i.SetMessage("Enter a message");
			var result = i.ShowHawkDialog();

			if (result == DialogResult.OK)
			{
				_tas.Markers.Add(markerFrame, i.UserText);
				MarkerControl.Refresh();
			}

			MarkerControl.Refresh();
		}

		private void UpdateChangesIndicator()
		{
			// TODO
		}

		#region Events

		#region File Menu

		private void FileSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			ToBk2MenuItem.Enabled =
				SaveTASMenuItem.Enabled =
				!string.IsNullOrWhiteSpace(_tas.Filename);
		}

		private void RecentSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			RecentSubMenu.DropDownItems.Clear();
			RecentSubMenu.DropDownItems.AddRange(
				ToolHelpers.GenerateRecentMenu(Global.Config.RecentTas, LoadProject)
			);
		}

		private void NewTasMenuItem_Click(object sender, EventArgs e)
		{
			GlobalWin.OSD.AddMessage("new TAStudio session started");
			StartNewTasMovie();
		}

		private void OpenTasMenuItem_Click(object sender, EventArgs e)
		{
			if (AskSave())
			{
				var file = ToolHelpers.GetTasProjFileFromUser(_tas.Filename);
				if (file != null)
				{
					_tas.Filename = file.FullName;
					_tas.Load();
					Global.Config.RecentTas.Add(_tas.Filename);
					RefreshDialog();
					MessageStatusLabel.Text = Path.GetFileName(_tas.Filename) + " loaded.";
				}
			}
		}

		private void SaveTasMenuItem_Click(object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty(_tas.Filename))
			{
				SaveAsTasMenuItem_Click(sender, e);
			}
			else
			{
				_tas.Save();
				MessageStatusLabel.Text = Path.GetFileName(_tas.Filename) + " saved.";
				Global.Config.RecentTas.Add(_tas.Filename);
			}
		}

		private void SaveAsTasMenuItem_Click(object sender, EventArgs e)
		{
			var file = ToolHelpers.GetTasProjSaveFileFromUser(_tas.Filename);
			if (file != null)
			{
				_tas.Filename = file.FullName;
				_tas.Save();
				Global.Config.RecentTas.Add(_tas.Filename);
				MessageStatusLabel.Text = Path.GetFileName(_tas.Filename) + " saved.";
			}
		}

		private void ToBk2MenuItem_Click(object sender, EventArgs e)
		{
			var bk2 = _tas.ToBk2();
			bk2.Save();
			MessageStatusLabel.Text = Path.GetFileName(bk2.Filename) + " created.";

		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		#region Edit

		private void DeselectMenuItem_Click(object sender, EventArgs e)
		{
			TasView.DeselectAll();
		}

		private void SelectAllMenuItem_Click(object sender, EventArgs e)
		{
			TasView.SelectAll();
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
			if (SelectedIndices.Any())
			{
				_tasClipboard.Clear();
				var list = TasView.SelectedIndices;
				var sb = new StringBuilder();
				for (var i = 0; i < list.Count; i++)
				{
					var input = _tas.GetInputState(list[i]);
					_tasClipboard.Add(new TasClipboardEntry(list[i], input));
					var lg = _tas.LogGeneratorInstance();
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
				_tas.CopyOverInput(FirstSelectedIndex, _tasClipboard.Select(x => x.ControllerState));
				RefreshDialog();
			}
		}

		private void PasteInsertMenuItem_Click(object sender, EventArgs e)
		{
			if (_tasClipboard.Any())
			{
				_tas.InsertInput(FirstSelectedIndex, _tasClipboard.Select(x => x.ControllerState));
				RefreshDialog();
			}
		}

		private void CutMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				_tasClipboard.Clear();
				var list = SelectedIndices.ToArray();
				var sb = new StringBuilder();
				for (var i = 0; i < list.Length; i++)
				{
					var input = _tas.GetInputState(i);
					_tasClipboard.Add(new TasClipboardEntry(list[i], input));
					var lg = _tas.LogGeneratorInstance();
					lg.SetSource(input);
					sb.AppendLine(lg.GenerateLogEntry());
				}

				Clipboard.SetDataObject(sb.ToString());

				_tas.RemoveFrames(list);


				SetSplicer();
				TasView.DeselectAll();
				RefreshDialog();
			}
		}

		private void ClearMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var frame in SelectedIndices)
			{
				_tas.ClearFrame(frame);
			}

			RefreshDialog();
		}

		private void DeleteFramesMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				_tasClipboard.Clear();
				_tas.RemoveFrames(SelectedIndices.ToArray());
				SetSplicer();
				TasView.DeselectAll();
				RefreshDialog();
			}
		}

		private void CloneMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				var framesToInsert = SelectedIndices.ToList();
				var insertionFrame = LastSelectedIndex + 1;
				var inputLog = new List<string>();

				foreach (var frame in framesToInsert)
				{
					inputLog.Add(_tas.GetInputLogEntry(frame));
				}

				_tas.InsertInput(insertionFrame, inputLog);

				RefreshDialog();
			}
		}

		private void InsertFrameMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				_tas.InsertEmptyFrame(LastSelectedIndex + 1);
				RefreshDialog();
			}
		}

		private void InsertNumFramesMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				var framesPrompt = new FramesPrompt();
				var result = framesPrompt.ShowDialog();
				if (result == DialogResult.OK)
				{
					_tas.InsertEmptyFrame(LastSelectedIndex + 1, framesPrompt.Frames);
				}
			}

			RefreshDialog();
		}

		private void TruncateMenuItem_Click(object sender, EventArgs e)
		{
			if (SelectedIndices.Any())
			{
				_tas.Truncate(LastSelectedIndex + 1);
				RefreshDialog();
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
			new MovieHeaderEditor(_tas).Show();
			UpdateChangesIndicator();

		}

		private void GreenzoneSettingsMenuItem_Click(object sender, EventArgs e)
		{
			new GreenzoneSettings(_tas.GreenzoneSettings).Show();
			UpdateChangesIndicator();
		}

		private void CommentsMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditCommentsForm();
			form.GetMovie(_tas);
			form.ShowDialog();
		}

		private void SubtitlesMenuItem_Click(object sender, EventArgs e)
		{
			var form = new EditSubtitlesForm { ReadOnly = true };
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

		private void TAStudio_KeyDown(object sender, KeyEventArgs e)
		{
			if (!e.Control && !e.Shift && !e.Alt && e.KeyCode == Keys.Delete)
			{
				DeleteFramesMenuItem_Click(null, null);
			}
		}

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		#endregion

		#endregion
	}
}
