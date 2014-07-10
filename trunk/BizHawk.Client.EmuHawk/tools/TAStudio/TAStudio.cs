using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

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

		// Input Painting
		private string _startDrawColumn = string.Empty;
		private bool _startOn;
		private bool _startMarkerDrag;
		private bool _startFrameDrag;

		private Dictionary<string, string> GenerateColumnNames()
		{
			var lg = Global.MovieSession.LogGeneratorInstance();
			lg.SetSource(Global.MovieSession.MovieControllerAdapter);
			return (lg as Bk2LogEntryGenerator).Map();
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
			else if (Global.Config.AutoloadTAStudioProject)
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
			GlobalWin.MainForm.RelinquishControl(this);
		}

		private void DisengageTastudio()
		{
			GlobalWin.OSD.AddMessage("TAStudio disengaged");
			Global.MovieSession.Movie = MovieService.DefaultInstance;
			GlobalWin.MainForm.TakeControl();
		}

		private void NewTasMovie()
		{
			Global.MovieSession.Movie = new TasMovie();
			_tas = Global.MovieSession.Movie as TasMovie;
			_tas.Filename = DefaultTasProjName(); // TODO don't do this, take over any mainform actions that can crash without a filename
			_tas.PopulateWithDefaultHeaderValues();
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

		private void RefreshDialog()
		{
			TasView.ItemCount = _tas.InputLogLength;
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
				TasView.Refresh();
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

		#region Config

		private void ConfigSubMenu_DropDownOpened(object sender, EventArgs e)
		{
			DrawInputByDraggingMenuItem.Checked = Global.Config.TAStudioDrawInput;
		}

		private void DrawInputByDraggingMenuItem_Click(object sender, EventArgs e)
		{
			TasView.InputPaintingMode = Global.Config.TAStudioDrawInput ^= true;
		}

		private void CopyMenuItem_Click(object sender, EventArgs e)
		{
			_tasClipboard.Clear();
			var list = TasView.SelectedIndices;
			for (var i = 0; i < list.Count; i++)
			{
				//Serialize TODO
				//_tasClipboard.Add(new TasClipboardEntry(list[i], _tas[i].Buttons));
			}

			SetSplicer();
		}

		#endregion

		#region Metadata

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

		protected override void OnShown(EventArgs e)
		{
			RefreshFloatingWindowControl();
			base.OnShown(e);
		}

		#endregion

		#endregion
	}
}
