using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlayMovie : Form, IDialogParent
	{
		private static readonly FilesystemFilterSet MoviesFSFilterSet = new(FilesystemFilter.BizHawkMovies, FilesystemFilter.TAStudioProjects);

		private readonly Func<FirmwareID, string, string> _canProvideFirmware;

		private readonly IMainFormForTools _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;

		private List<IBasicMovieInfo> _movieList = new();
		private bool _sortReverse;
		private string _sortedCol;

		private bool _sortDetailsReverse;
		private string _sortedDetailsCol;

		public IDialogController DialogController => _mainForm;

		public PlayMovie(
			IMainFormForTools mainForm,
			Config config,
			GameInfo game,
			IEmulator emulator,
			IMovieSession movieSession,
			Func<FirmwareID, string, string> canProvideFirmware)
		{
			_mainForm = mainForm;
			_canProvideFirmware = canProvideFirmware;
			_config = config;
			_game = game;
			_emulator = emulator;
			_movieSession = movieSession;
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;
			BrowseMovies.Image = Properties.Resources.OpenFile;
			Scan.Image = Properties.Resources.Scan;
			editToolStripMenuItem.Image = Properties.Resources.Cut;
			MovieView.RetrieveVirtualItem += MovieView_QueryItemText;
			MovieView.ShowItemToolTips = true;
			MovieView.VirtualMode = true;
			_sortReverse = false;
			_sortedCol = "";

			_sortDetailsReverse = false;
			_sortedDetailsCol = "";
		}

		private void PlayMovie_Load(object sender, EventArgs e)
		{
			_suppressCheckedChanged = true;
			IncludeSubDirectories.Checked = _config.PlayMovieIncludeSubDir;
			MatchHashCheckBox.Checked = _config.PlayMovieMatchHash;
			_suppressCheckedChanged = false;
			ScanFiles();
			PreHighlightMovie();
			TurboCheckbox.Checked = _config.TurboSeek;
		}

		private static string MovieTimeLengthStr(TimeSpan movieLength)
			=> movieLength.ToString(movieLength.Days == 0 ? @"hh\:mm\:ss\.fff" : @"dd\:hh\:mm\:ss\.fff", DateTimeFormatInfo.InvariantInfo);

		private void MovieView_QueryItemText(object sender, RetrieveVirtualItemEventArgs e)
		{
			var entry = _movieList[e.ItemIndex];
			// don't display the common movie path prefix in the dialog
			string displayedPath = entry.Filename.RemovePrefix(_config.PathEntries.MovieAbsolutePath() + Path.DirectorySeparatorChar);
			e.Item = new ListViewItem(displayedPath)
			{
				ToolTipText = entry.Filename
			};
			e.Item.SubItems.Add(entry.SystemID);
			e.Item.SubItems.Add(entry.GameName);
			e.Item.SubItems.Add(MovieTimeLengthStr(entry.TimeLength));
		}

		private void Run()
		{
			var indices = MovieView.SelectedIndices;
			if (indices.Count > 0) // Import file if necessary
			{
				var movie = _movieSession.Get(_movieList[MovieView.SelectedIndices[0]].Filename, true);
				_mainForm.StartNewMovie(movie, false);
			}
		}

		private int? AddMovieToList(string filename, bool force)
		{
			using var file = new HawkFile(filename);
			if (!file.Exists)
			{
				return null;
			}

			var movie = LoadMovieInfo(file, force);
			if (movie == null)
			{
				return null;
			}

			int? index;
			lock (_movieList)
			{
				// need to check IsDuplicateOf within the lock
				index = IsDuplicateOf(filename);
				if (index.HasValue)
				{
					return index;
				}

				_movieList.Add(movie);
				index = _movieList.Count - 1;
			}

			_sortReverse = false;
			_sortedCol = "";

			return index;
		}

		private int? IsDuplicateOf(string filename)
		{
			for (var i = 0; i < _movieList.Count; i++)
			{
				if (_movieList[i].Filename == filename)
				{
					return i;
				}
			}

			return null;
		}

		private IBasicMovieInfo LoadMovieInfo(HawkFile hf, bool force)
		{
			IBasicMovieInfo movie = new BasicMovieInfo(hf.CanonicalFullPath);

			try
			{
				movie.Load();

				// Don't do this from browse
				if (movie.Hash == _game.Hash
					|| !_config.PlayMovieMatchHash || force)
				{
					return movie;
				}
			}
			catch (Exception ex)
			{
				// TODO: inform the user that a movie failed to parse in some way
				Console.WriteLine(ex.Message);
			}

			return null;
		}

		private void UpdateList()
		{
			MovieView.Refresh();
			MovieCount.Text = $"{_movieList.Count} {(_movieList.Count == 1 ? "movie" : "movies")}";
		}

		private void PreHighlightMovie()
		{
			if (_game.IsNullInstance())
			{
				return;
			}

			var indices = new List<int>();

			// Pull out matching names
			for (var i = 0; i < _movieList.Count; i++)
			{
				if (_game.FilesystemSafeName() == _movieList[i].GameName)
				{
					indices.Add(i);
				}
			}

			if (indices.Count == 0)
			{
				return;
			}

			if (indices.Count == 1)
			{
				HighlightMovie(indices[0]);
				return;
			}

			// Prefer tas files
			// but `_movieList` should only contain `.bk2` and `.tasproj` so... isn't that all of them? or, it is now I've fixed the case-sensitivity bug --yoshi
			var tas = new List<int>();
			for (var i = 0; i < indices.Count; i++)
			{
				foreach (var ext in MovieService.MovieExtensions)
				{
					if ($".{ext}".Equals(Path.GetExtension(_movieList[indices[i]].Filename), StringComparison.OrdinalIgnoreCase))
					{
						tas.Add(i);
					}
				}
			}

			if (tas.Count is 0)
			{
				if (_movieList.Count is not 0) HighlightMovie(0);
				return;
			}

			if (tas.Count == 1)
			{
				HighlightMovie(tas[0]);
				return;
			}

			// Final tie breaker - Last used file
			HighlightMovie(tas.Select(movieIndex => (Index: movieIndex, Timestamp: new FileInfo(_movieList[movieIndex].Filename).LastAccessTime))
				.OrderByDescending(static tuple => tuple.Timestamp)
				.First().Index);
		}

		private void HighlightMovie(int index)
		{
			MovieView.SelectedIndices.Clear();
			MovieView.Items[index].Selected = true;
		}

		private void ScanFiles()
		{
			_movieList.Clear();
			MovieView.VirtualListSize = 0;
			MovieView.Update();

			var directory = _config.PathEntries.MovieAbsolutePath();
			Directory.CreateDirectory(directory);
			var dpTodo = new Queue<string>();
			var fpTodo = new List<string>();
			dpTodo.Enqueue(directory);
			var ordinals = new Dictionary<string, int>();

			while (dpTodo.Count > 0)
			{
				string dp = dpTodo.Dequeue();

				// enqueue subdirectories if appropriate
				if (_config.PlayMovieIncludeSubDir)
				{
					foreach (var subDir in Directory.GetDirectories(dp))
					{
						dpTodo.Enqueue(subDir);
					}
				}

				// add movies
				foreach (var extension in MovieService.MovieExtensions)
				{
					fpTodo.AddRange(Directory.GetFiles(dp, $"*.{extension}"));
				}
			}

			// in parallel, scan each movie
			Parallel.For(0, fpTodo.Count, i =>
			{
				var file = fpTodo[i];
				lock (ordinals)
				{
					ordinals[file] = i;
				}

				AddMovieToList(file, force: false);
			});

			// sort by the ordinal key to maintain relatively stable results when rescanning
			_movieList.Sort((a, b) => ordinals[a.Filename].CompareTo(ordinals[b.Filename]));

			RefreshMovieList();
		}

		private void RefreshMovieList()
		{
			MovieView.VirtualListSize = _movieList.Count;
			UpdateList();
		}

		private void MovieView_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		private void MovieView_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

			foreach (var path in filePaths.Where(path => MovieService.MovieExtensions.Contains(Path.GetExtension(path)?.Replace(".", ""))))
			{
				AddMovieToList(path, force: true);
			}

			RefreshMovieList();
		}

		private void MovieView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.C)
			{
				var indexes = MovieView.SelectedIndices;
				if (indexes.Count > 0)
				{
					var copyStr = new StringBuilder();
					foreach (int index in indexes)
					{
						copyStr
							.Append(_movieList[index].Filename).Append('\t')
							.Append(_movieList[index].SystemID).Append('\t')
							.Append(_movieList[index].GameName).Append('\t')
							.Append(MovieTimeLengthStr(_movieList[index].TimeLength))
							.AppendLine();
					}

					Clipboard.SetDataObject(copyStr.ToString());
				}
			}
		}

		private void MovieView_DoubleClick(object sender, EventArgs e)
		{
			Run();
			Close();
		}

		private static readonly RigidMultiPredicateSort<IBasicMovieInfo> ColumnSorts
			= new(new Dictionary<string, Func<IBasicMovieInfo, IComparable>>
			{
				["File"] = x => Path.GetFileName(x.Filename),
				["SysID"] = x => x.SystemID,
				["Game"] = x => x.GameName,
				["Length (est.)"] = x => x.FrameCount
			});

		private void MovieView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			var columnName = MovieView.Columns[e.Column].Text;
			_movieList = ColumnSorts.AppliedTo(_movieList, columnName);
			if (_sortedCol == columnName && _sortReverse)
			{
				_movieList.Reverse();
				_sortReverse = false;
			}
			else
			{
				_sortReverse = true;
				_sortedCol = columnName;
			}
			MovieView.Refresh();
		}

		private void MovieView_SelectedIndexChanged(object sender, EventArgs e)
		{
			DetailsView.Items.Clear();
			if (MovieView.SelectedIndices.Count < 1)
			{
				OK.Enabled = false;
				return;
			}

			OK.Enabled = true;

			var firstIndex = MovieView.SelectedIndices[0];
			MovieView.EnsureVisible(firstIndex);

			foreach (var (k, v) in _movieList[firstIndex].HeaderEntries)
			{
				var item = new ListViewItem(k);
				item.SubItems.Add(v);
				item.ToolTipText = v;
				switch (k)
				{
					case HeaderKeys.Sha1:
						if (_game.Hash != v)
						{
							item.BackColor = Color.Pink;
							item.ToolTipText = $"Expected: {v}\nActual: {_game.Hash}";
						}
						break;
					case HeaderKeys.EmulatorVersion:
						if (VersionInfo.GetEmuVersion() != v)
						{
							item.BackColor = Color.Yellow;
							item.ToolTipText = $"Expected: {v}\nActual: {VersionInfo.GetEmuVersion()}";
						}
						break;
					case HeaderKeys.Platform:
						if (_emulator.SystemId != v)
						{
							item.BackColor = Color.Pink;
							item.ToolTipText = $"Expected: {v}\n Actual: {_emulator.SystemId}";
						}
						break;
					case HeaderKeys.VsyncAttoseconds:
						if (_emulator is MAME mame && mame.VsyncAttoseconds != long.Parse(v))
						{
							item.BackColor = Color.Pink;
							item.ToolTipText = $"Expected: {v}\n Actual: {mame.VsyncAttoseconds}";
						}
						break;
					default:
						if (k.Contains("_Firmware_"))
						{
							var split = k.Split(new[] { "_Firmware_" }, StringSplitOptions.None);
							var actualHash = _canProvideFirmware(new(split[0], split[1]), v);
							if (actualHash != v)
							{
								item.BackColor = Color.Yellow;
								item.ToolTipText = $"Expected: {v}\nActual: {actualHash}";
							}
						}
						break;
				}

				DetailsView.Items.Add(item);
			}

			var fpsItem = new ListViewItem("Fps");
			fpsItem.SubItems.Add($"{_movieList[firstIndex].FrameRate:0.#######}");
			DetailsView.Items.Add(fpsItem);

			var framesItem = new ListViewItem("Frames");
			framesItem.SubItems.Add(_movieList[firstIndex].FrameCount.ToString());
			DetailsView.Items.Add(framesItem);
			CommentsBtn.Enabled = _movieList[firstIndex].Comments.Any();
			SubtitlesBtn.Enabled = _movieList[firstIndex].Subtitles.Any();
		}

		private void EditMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				foreach (var movie in MovieView.SelectedIndices.Cast<int>().Select(index => _movieList[index]))
				{
					Process.Start(movie.Filename);
				}
			}
			catch (Win32Exception ex) // "Access denied" when cancelling "Open With" dialog on Linux
			{
				Console.WriteLine(ex);
				// and stop trying to open files
			}
		}

		private void DetailsView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			var detailsList = new List<MovieDetails>();
			for (var i = 0; i < DetailsView.Items.Count; i++)
			{
				detailsList.Add(new MovieDetails
				{
					Keys = DetailsView.Items[i].Text,
					Values = DetailsView.Items[i].SubItems[1].Text,
					BackgroundColor = DetailsView.Items[i].BackColor
				});
			}

			var columnName = DetailsView.Columns[e.Column].Text;
			if (_sortedDetailsCol != columnName)
			{
				_sortDetailsReverse = false;
			}

			detailsList = columnName switch
			{
				"Header" => detailsList
					.OrderBy(x => x.Keys, _sortDetailsReverse)
					.ThenBy(x => x.Values)
					.ToList(),
				"Value" => detailsList
					.OrderBy(x => x.Values, _sortDetailsReverse)
					.ThenBy(x => x.Keys)
					.ToList(),
				_ => detailsList
			};

			DetailsView.Items.Clear();
			foreach (var detail in detailsList)
			{
				var item = new ListViewItem { Text = detail.Keys, BackColor = detail.BackgroundColor };
				item.SubItems.Add(detail.Values);
				DetailsView.Items.Add(item);
			}

			_sortedDetailsCol = columnName;
			_sortDetailsReverse = !_sortDetailsReverse;
		}

		private void CommentsBtn_Click(object sender, EventArgs e)
		{
			var indices = MovieView.SelectedIndices;
			if (indices.Count > 0)
			{
				// TODO this will allocate unnecessary memory when this movie is a TasMovie due to TasStateManager
				var movie = _movieSession.Get(_movieList[MovieView.SelectedIndices[0]].Filename, true);
				var form = new EditCommentsForm(movie, readOnly: false, disposeOnClose: true);
				form.Show();
			}
		}

		private void SubtitlesBtn_Click(object sender, EventArgs e)
		{
			var indices = MovieView.SelectedIndices;
			if (indices.Count > 0)
			{
				// TODO this will allocate unnecessary memory when this movie is a TasMovie due to TasStateManager
				var movie = _movieSession.Get(_movieList[MovieView.SelectedIndices[0]].Filename, true);
				var form = new EditSubtitlesForm(DialogController, movie, _config.PathEntries, readOnly: false, disposeOnClose: true);
				form.Show();
			}
		}

		private void BrowseMovies_Click(object sender, EventArgs e)
		{
			var result = this.ShowFileOpenDialog(
				filter: MoviesFSFilterSet,
				initDir: _config.PathEntries.MovieAbsolutePath());
			if (result is null) return;
			FileInfo file = new(result);
			if (!file.Exists)
			{
				return;
			}

			int? index = AddMovieToList(result, true);
			RefreshMovieList();
			if (index.HasValue)
			{
				MovieView.SelectedIndices.Clear();
				MovieView.Items[index.Value].Selected = true;
			}
		}

		private void Scan_Click(object sender, EventArgs e)
		{
			ScanFiles();
			PreHighlightMovie();
		}

		private void IncludeSubDirectories_CheckedChanged(object sender, EventArgs e)
		{
			if (_suppressCheckedChanged) return;

			_config.PlayMovieIncludeSubDir = IncludeSubDirectories.Checked;
			ScanFiles();
			PreHighlightMovie();
		}

		private void MatchHashCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			if (_suppressCheckedChanged) return;

			_config.PlayMovieMatchHash = MatchHashCheckBox.Checked;
			ScanFiles();
			PreHighlightMovie();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_config.TurboSeek = TurboCheckbox.Checked;
			Run();
			_movieSession.ReadOnly = ReadOnlyCheckBox.Checked;
			if (StopOnFrameCheckbox.Checked)
			{
				if (LastFrameCheckbox.Checked) _mainForm.PauseOnFrame = _movieSession.Movie.InputLogLength;
				else if (StopOnFrameTextBox.ToRawInt() is int i) _mainForm.PauseOnFrame = i;
			}
			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private bool _programmaticallyChangingStopFrameCheckbox;
		private bool _suppressCheckedChanged;

		private void StopOnFrameCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingStopFrameCheckbox)
			{
				StopOnFrameTextBox.Select();
			}
		}

		private void StopOnFrameTextBox_TextChanged_1(object sender, EventArgs e)
		{
			_programmaticallyChangingStopFrameCheckbox = true;
			StopOnFrameCheckbox.Checked = !string.IsNullOrWhiteSpace(StopOnFrameTextBox.Text);
			_programmaticallyChangingStopFrameCheckbox = false;
		}

		private void LastFrameCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (LastFrameCheckbox.Checked)
			{
				_programmaticallyChangingStopFrameCheckbox = true;
				StopOnFrameCheckbox.Checked = true;
				_programmaticallyChangingStopFrameCheckbox = false;
			}

			StopOnFrameTextBox.Enabled = !LastFrameCheckbox.Checked;
		}
	}
}
