using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlayMovie : Form
	{
		private readonly MainForm _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;
		private readonly PlatformFrameRates _platformFrameRates = new PlatformFrameRates();

		private List<IMovie> _movieList = new List<IMovie>();
		private bool _sortReverse;
		private string _sortedCol;

		private bool _sortDetailsReverse;
		private string _sortedDetailsCol;

		public PlayMovie(
			MainForm mainForm,
			Config config,
			GameInfo game,
			IEmulator emulator,
			IMovieSession movieSession)
		{
			_mainForm = mainForm;
			_config = config;
			_game = game;
			_emulator = emulator;
			_movieSession = movieSession;
			InitializeComponent();
			MovieView.RetrieveVirtualItem += MovieView_QueryItemText;
			MovieView.VirtualMode = true;
			_sortReverse = false;
			_sortedCol = "";

			_sortDetailsReverse = false;
			_sortedDetailsCol = "";
		}

		private void PlayMovie_Load(object sender, EventArgs e)
		{
			IncludeSubDirectories.Checked = _config.PlayMovieIncludeSubDir;
			MatchHashCheckBox.Checked = _config.PlayMovieMatchHash;
			ScanFiles();
			PreHighlightMovie();
			TurboCheckbox.Checked = _config.TurboSeek;
		}

		private void MovieView_QueryItemText(object sender, RetrieveVirtualItemEventArgs e)
		{
			var entry = _movieList[e.ItemIndex];
			e.Item = new ListViewItem(entry.Filename);
			e.Item.SubItems.Add(entry.SystemID);
			e.Item.SubItems.Add(entry.GameName);
			e.Item.SubItems.Add(_platformFrameRates.MovieTime(entry).ToString(@"hh\:mm\:ss\.fff"));
		}

		private void Run()
		{
			var indices = MovieView.SelectedIndices;
			if (indices.Count > 0) // Import file if necessary
			{
				_mainForm.StartNewMovie(_movieList[MovieView.SelectedIndices[0]], false);
			}
		}

		private int? AddMovieToList(string filename, bool force)
		{
			using var file = new HawkFile(filename);
			if (!file.Exists)
			{
				return null;
			}
				
			var movie = PreLoadMovieFile(file, force);
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

		private IMovie PreLoadMovieFile(HawkFile hf, bool force)
		{
			var movie = MovieService.Get(hf.CanonicalFullPath);

			try
			{
				movie.PreLoadHeaderAndLength(hf);

				// Don't do this from browse
				if (movie.Hash == _game.Hash
					|| _config.PlayMovieMatchHash == false || force)
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
				if (PathManager.FilesystemSafeName(_game) == _movieList[i].GameName)
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
			var tas = new List<int>();
			for (var i = 0; i < indices.Count; i++)
			{
				foreach (var ext in MovieService.MovieExtensions)
				{
					if (Path.GetExtension(_movieList[indices[i]].Filename)?.ToUpper() == $".{ext}")
					{
						tas.Add(i);
					}
				}
			}

			if (tas.Count == 1)
			{
				HighlightMovie(tas[0]);
				return;
			}
			
			if (tas.Count > 1)
			{
				indices = new List<int>(tas);
			}

			// Final tie breaker - Last used file
			var file = new FileInfo(_movieList[indices[0]].Filename);
			var time = file.LastAccessTime;
			var mostRecent = indices.First();
			for (var i = 1; i < indices.Count; i++)
			{
				file = new FileInfo(_movieList[indices[0]].Filename);
				if (file.LastAccessTime > time)
				{
					time = file.LastAccessTime;
					mostRecent = indices[i];
				}
			}

			HighlightMovie(mostRecent);
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

			var directory = PathManager.MakeAbsolutePath(_config.PathEntries.MoviesPathFragment, null);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

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
				fpTodo.AddRange(Directory.GetFiles(dp, $"*.{MovieService.DefaultExtension}"));
				fpTodo.AddRange(Directory.GetFiles(dp, $"*.{TasMovie.Extension}"));
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

		#region Events

		#region Movie List

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
							.Append(_platformFrameRates.MovieTime(_movieList[index]).ToString(@"hh\:mm\:ss\.fff"))
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

		private void MovieView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			var columnName = MovieView.Columns[e.Column].Text;
			switch (columnName)
			{
				case "File":
				default:
					_movieList = _movieList.OrderBy(x => Path.GetFileName(x.Filename))
						.ThenBy(x => x.SystemID)
						.ThenBy(x => x.GameName)
						.ThenBy(x => x.FrameCount)
						.ToList();
					break;
				case "SysID":
					_movieList = _movieList.OrderBy(x => x.SystemID)
						.ThenBy(x => Path.GetFileName(x.Filename))
						.ThenBy(x => x.GameName)
						.ThenBy(x => x.FrameCount)
						.ToList();
					break;
				case "Game":
					_movieList = _movieList.OrderBy(x => x.GameName)
						.ThenBy(x => Path.GetFileName(x.Filename))
						.ThenBy(x => x.SystemID)
						.ThenBy(x => x.FrameCount)
						.ToList();
					break;
				case "Length (est.)":
					_movieList = _movieList.OrderBy(x => x.FrameCount)
						.ThenBy(x => Path.GetFileName(x.Filename))
						.ThenBy(x => x.SystemID)
						.ThenBy(x => x.GameName)
						.ToList();
					break;
			}
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
			toolTip1.SetToolTip(DetailsView, "");
			DetailsView.Items.Clear();
			if (MovieView.SelectedIndices.Count < 1)
			{
				OK.Enabled = false;
				return;
			}

			OK.Enabled = true;

			var firstIndex = MovieView.SelectedIndices[0];
			MovieView.EnsureVisible(firstIndex);

			foreach (var kvp in _movieList[firstIndex].HeaderEntries)
			{
				var item = new ListViewItem(kvp.Key);
				item.SubItems.Add(kvp.Value);

				switch (kvp.Key)
				{
					case HeaderKeys.SHA1:
						if (kvp.Value != _game.Hash)
						{
							item.BackColor = Color.Pink;
							toolTip1.SetToolTip(DetailsView, $"Current SHA1: {_game.Hash}");
						}
						break;
					case HeaderKeys.EMULATIONVERSION:
						if (kvp.Value != VersionInfo.GetEmuVersion())
						{
							item.BackColor = Color.Yellow;
						}
						break;
					case HeaderKeys.PLATFORM:
						// feos: previously it was compared against _game.System, but when the movie is created
						// its platform is copied from _emulator.SystemId, see PopulateWithDefaultHeaderValues()
						// the problem is that for GameGear and SG100, those mismatch, resulting in false positive here
						// I have a patch to make GG and SG appear as platforms in movie header (issue #1246)
						// but even with it, for all the old movies, this false positive would have to be worked around anyway
						// TODO: actually check header flags like "IsGGMode" and "IsSegaCDMode" (those are never parsed by BizHawk)
						if (kvp.Value != _emulator.SystemId)
						{
							item.BackColor = Color.Pink;
						}
						break;
				}

				DetailsView.Items.Add(item);
			}

			var fpsItem = new ListViewItem("Fps");
			fpsItem.SubItems.Add($"{Fps(_movieList[firstIndex]):0.#######}");
			DetailsView.Items.Add(fpsItem);

			var framesItem = new ListViewItem("Frames");
			framesItem.SubItems.Add(_movieList[firstIndex].FrameCount.ToString());
			DetailsView.Items.Add(framesItem);
			CommentsBtn.Enabled = _movieList[firstIndex].Comments.Any();
			SubtitlesBtn.Enabled = _movieList[firstIndex].Subtitles.Any();
		}

		public double Fps(IMovie movie)
		{
			var system = movie.HeaderEntries[HeaderKeys.PLATFORM];
			var pal = movie.HeaderEntries.ContainsKey(HeaderKeys.PAL)
				&& movie.HeaderEntries[HeaderKeys.PAL] == "1";

			return new PlatformFrameRates()[system, pal];
			
		}

		private void EditMenuItem_Click(object sender, EventArgs e)
		{
			foreach (var movie in MovieView.SelectedIndices.Cast<int>()
				.Select(index => _movieList[index]))
			{
				System.Diagnostics.Process.Start(movie.Filename);
			}
		}

		#endregion

		#region Details

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

			switch (columnName)
			{
				// Header, Value
				case "Header":
					if (_sortDetailsReverse)
					{
						detailsList = detailsList
							.OrderByDescending(x => x.Keys)
							.ThenBy(x => x.Values).ToList();
					}
					else
					{
						detailsList = detailsList
						   .OrderBy(x => x.Keys)
						   .ThenBy(x => x.Values).ToList();
					}

					break;
				case "Value":
					if (_sortDetailsReverse)
					{
						detailsList = detailsList
							.OrderByDescending(x => x.Values)
							.ThenBy(x => x.Keys).ToList();
					}
					else
					{
						detailsList = detailsList
							.OrderBy(x => x.Values)
							.ThenBy(x => x.Keys).ToList();
					}

					break;
			}

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
				var form = new EditCommentsForm();
				form.GetMovie(_movieList[MovieView.SelectedIndices[0]]);
				form.Show();
			}
		}

		private void SubtitlesBtn_Click(object sender, EventArgs e)
		{
			var indices = MovieView.SelectedIndices;
			if (indices.Count > 0)
			{
				var s = new EditSubtitlesForm { ReadOnly = true };
				s.GetMovie(_movieList[MovieView.SelectedIndices[0]]);
				s.Show();
			}
		}

		#endregion

		#region Misc Widgets

		private void BrowseMovies_Click(object sender, EventArgs e)
		{
			using var ofd = new OpenFileDialog
			{
				Filter = new FilesystemFilterSet(FilesystemFilter.BizHawkMovies, FilesystemFilter.TAStudioProjects).ToString(),
				InitialDirectory = PathManager.MakeAbsolutePath(_config.PathEntries.MoviesPathFragment, null)
			};

			var result = ofd.ShowHawkDialog();
			if (result == DialogResult.OK)
			{
				var file = new FileInfo(ofd.FileName);
				if (!file.Exists)
				{
					return;
				}

				int? index = AddMovieToList(ofd.FileName, true);
				RefreshMovieList();
				if (index.HasValue)
				{
					MovieView.SelectedIndices.Clear();
					MovieView.Items[index.Value].Selected = true;
				}
			}
		}

		private void Scan_Click(object sender, EventArgs e)
		{
			ScanFiles();
			PreHighlightMovie();
		}

		private void IncludeSubDirectories_CheckedChanged(object sender, EventArgs e)
		{
			_config.PlayMovieIncludeSubDir = IncludeSubDirectories.Checked;
			ScanFiles();
			PreHighlightMovie();
		}

		private void MatchHashCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			_config.PlayMovieMatchHash = MatchHashCheckBox.Checked;
			ScanFiles();
			PreHighlightMovie();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			_config.TurboSeek = TurboCheckbox.Checked;
			Run();
			_movieSession.ReadOnly = ReadOnlyCheckBox.Checked;

			if (StopOnFrameCheckbox.Checked && 
				(StopOnFrameTextBox.ToRawInt().HasValue || LastFrameCheckbox.Checked))
			{
				_mainForm.PauseOnFrame = LastFrameCheckbox.Checked
					? _movieSession.Movie.InputLogLength
					: StopOnFrameTextBox.ToRawInt();
			}

			Close();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		#endregion

		private bool _programmaticallyChangingStopFrameCheckbox;
		private void StopOnFrameCheckbox_CheckedChanged(object sender, EventArgs e)
		{
			if (!_programmaticallyChangingStopFrameCheckbox)
			{
				StopOnFrameTextBox.Focus();
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

		#endregion
	}
}
