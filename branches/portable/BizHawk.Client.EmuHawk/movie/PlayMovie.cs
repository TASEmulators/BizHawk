using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using BizHawk.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class PlayMovie : Form
	{
		private List<Movie> _movieList = new List<Movie>();
		private bool _sortReverse;
		private string _sortedCol;

		public PlayMovie()
		{
			InitializeComponent();
			MovieView.QueryItemText += MovieView_QueryItemText;
			MovieView.QueryItemBkColor += MovieView_QueryItemBkColor;
			MovieView.VirtualMode = true;
			_sortReverse = false;
			_sortedCol = String.Empty;
		}

		void MovieView_QueryItemText(int index, int column, out string text)
		{
			text = String.Empty;
			if (column == 0) //File
			{
				text = Path.GetFileName(_movieList[index].Filename);
			}
			if (column == 1) //System
			{
				text = _movieList[index].SysID;
			}
			if (column == 2) //Game
			{
				text = _movieList[index].GameName;
			}
			if (column == 3) //Time
			{
				text = _movieList[index].GetTime(true);
			}
		}

		private void MovieView_QueryItemBkColor(int index, int column, ref Color color)
		{

		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Run()
		{
			ListView.SelectedIndexCollection indexes = MovieView.SelectedIndices;
			if (indexes.Count > 0) //Import file if necessary
			{
				GlobalWin.MainForm.StartNewMovie(_movieList[MovieView.SelectedIndices[0]], false);
			}
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.ReadOnly = ReadOnlyCheckBox.Checked;
			Run();
			Close();
		}

		private void BrowseMovies_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog { InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPath, null) };
			string filter = "Movie Files (*." + Global.Config.MovieExtension + ")|*." + Global.Config.MovieExtension + "|Savestates|*.state|All Files|*.*";
			ofd.Filter = filter;

			GlobalWin.Sound.StopSound();
			var result = ofd.ShowDialog();
			GlobalWin.Sound.StartSound();
			if (result == DialogResult.OK)
			{
				var file = new FileInfo(ofd.FileName);
				if (!file.Exists)
					return;
				else
				{
					if (file.Extension.ToUpper() == "STATE")
					{
						Movie movie = new Movie(file.FullName);
						movie.LoadMovie(); //State files will have to load everything unfortunately
						if (movie.Frames == 0)
						{
							MessageBox.Show("No input log detected in this savestate, aborting", "Can not load file", MessageBoxButtons.OK, MessageBoxIcon.Hand);
							return;
						}
					}

					int x = AddMovieToList(ofd.FileName, true);
					if (x > 0)
					{
						MovieView.SelectedIndices.Clear();
						MovieView.setSelection(x);
						MovieView.SelectItem(x, true);
					}
				}
			}
		}

		private void AddStateToList(string filename)
		{
			using (var file = new HawkFile(filename))
			{
				if (file.Exists)
				{
					int x = IsDuplicate(filename);
					if (x == 0)
					{
						Movie movie = new Movie(file.CanonicalFullPath);
						movie.LoadMovie(); //State files will have to load everything unfortunately
						if (movie.Frames > 0)
						{
							_movieList.Add(movie);
							_sortReverse = false;
							_sortedCol = String.Empty;
						}
					}
				}
			}
		}

		private int AddMovieToList(string filename, bool force)
		{
			using (var file = new HawkFile(filename))
			{
				if (!file.Exists)
					return 0;
				else
				{
					int x = IsDuplicate(filename);
					if (x == 0)
					{
						PreLoadMovieFile(file, force);
						MovieView.ItemCount = _movieList.Count;
						UpdateList();

						_sortReverse = false;
						_sortedCol = String.Empty;
						x = _movieList.Count - 1;
					}
					return x;
				}
			}
		}

		private int IsDuplicate(string filename)
		{
			for (int x = 0; x < _movieList.Count; x++)
			{
				if (_movieList[x].Filename == filename)
				{
					return x;
				}
			}

			return 0;
		}

		private void PreLoadMovieFile(HawkFile hf, bool force)
		{
			Movie movie = new Movie(hf.CanonicalFullPath);
			movie.PreLoadText(hf);
			if (hf.Extension == ".FM2")
			{
				movie.Header.SetHeaderLine(MovieHeader.PLATFORM, "NES");
			}
			else if (hf.Extension == ".MC2")
			{
				movie.Header.SetHeaderLine(MovieHeader.PLATFORM, "PCE");
			}
			//Don't do this from browse
			if (movie.Header.GetHeaderLine(MovieHeader.GAMENAME) == Global.Game.Name ||
				Global.Config.PlayMovie_MatchGameName == false || force)
			{
				_movieList.Add(movie);
			}
		}

		private void UpdateList()
		{
			MovieView.Refresh();
			UpdateMovieCount();
		}

		private void UpdateMovieCount()
		{
			int x = _movieList.Count;
			if (x == 1)
			{
				MovieCount.Text = x.ToString() + " movie";
			}
			else
			{
				MovieCount.Text = x.ToString() + " movies";
			}
		}

		private void PreHighlightMovie()
		{
			if (Global.Game == null) return;
			List<int> Indexes = new List<int>();
			
			//Pull out matching names
			for (int x = 0; x < _movieList.Count; x++)
			{
				if (PathManager.FilesystemSafeName(Global.Game) == _movieList[x].GameName)
					Indexes.Add(x);
			}
			if (Indexes.Count == 0) return;
			if (Indexes.Count == 1)
			{
				HighlightMovie(Indexes[0]);
				return;
			}

			//Prefer tas files
			List<int> TAS = new List<int>();
			for (int x = 0; x < Indexes.Count; x++)
			{
				if (Path.GetExtension(_movieList[Indexes[x]].Filename).ToUpper() == "." + Global.Config.MovieExtension)
				{
					TAS.Add(x);
				}
			}

			if (TAS.Count == 1)
			{
				HighlightMovie(TAS[0]);
				return;
			}
			else if (TAS.Count > 1)
			{
				Indexes = new List<int>(TAS);
			}

			//Final tie breaker - Last used file
			FileInfo f = new FileInfo(_movieList[Indexes[0]].Filename);
			DateTime t = f.LastAccessTime;
			int mostRecent = Indexes[0];
			for (int x = 1; x < Indexes.Count; x++)
			{
				f = new FileInfo(_movieList[Indexes[0]].Filename);
				if (f.LastAccessTime > t)
				{
					t = f.LastAccessTime;
					mostRecent = Indexes[x];
				}
			}

			HighlightMovie(mostRecent);
			return;

		}

		private void HighlightMovie(int index)
		{
			MovieView.SelectedIndices.Clear();
			MovieView.setSelection(index);
			MovieView.SelectItem(index, true);
		}

		private void ClearList()
		{
			_movieList.Clear();
			MovieView.ItemCount = 0;
			MovieView.Update();
		}

		private void ScanFiles()
		{
			ClearList();

			string d = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPath, null);
			if (!Directory.Exists(d))
			{
				Directory.CreateDirectory(d);
			}

			foreach (string f in Directory.GetFiles(d, "*." + Global.Config.MovieExtension))
			{
				AddMovieToList(f, false);
			}

			if (Global.Config.MovieExtension != "*.tas")
			{
				foreach (string f in Directory.GetFiles(d, "*.tas"))
				{
					AddMovieToList(f, false);
				}
			}
			else if (Global.Config.MovieExtension != "*.bkm")
			{
				foreach (string f in Directory.GetFiles(d, "*.bkm"))
				{
					AddMovieToList(f, false);
				}
			}

			if (Global.Config.PlayMovie_ShowStateFiles)
			{
				foreach (string f in Directory.GetFiles(d, "*.state"))
				{
					AddStateToList(f);
				}
			}

			if (Global.Config.PlayMovie_IncludeSubdir)
			{
				string[] subs = Directory.GetDirectories(d);
				foreach (string dir in subs)
				{
					foreach (string f in Directory.GetFiles(dir, "*." + Global.Config.MovieExtension))
					{
						AddMovieToList(f, false);
					}

					if (Global.Config.PlayMovie_ShowStateFiles)
					{
						foreach (string f in Directory.GetFiles(d, "*.state"))
						{
							AddStateToList(f);
						}
					}
				}
			}
		}

		private void PlayMovie_Load(object sender, EventArgs e)
		{
			IncludeSubDirectories.Checked = Global.Config.PlayMovie_IncludeSubdir;
			ShowStateFiles.Checked = Global.Config.PlayMovie_ShowStateFiles;
			MatchGameNameCheckBox.Checked = Global.Config.PlayMovie_MatchGameName;
			ScanFiles();
			PreHighlightMovie();
		}

		private void MovieView_SelectedIndexChanged(object sender, EventArgs e)
		{
			toolTip1.SetToolTip(DetailsView, String.Empty);
			DetailsView.Items.Clear();
			if (MovieView.SelectedIndices.Count < 1)
			{
				OK.Enabled = false;
				return;
			}
			else
			{
				OK.Enabled = true;
			}

			int x = MovieView.SelectedIndices[0];
			MovieView.ensureVisible(x);
			Dictionary<string, string> h = _movieList[x].Header.HeaderParams;

			foreach (var kvp in h)
			{
				ListViewItem item = new ListViewItem(kvp.Key);
				item.SubItems.Add(kvp.Value);

				switch (kvp.Key)
				{
					case MovieHeader.SHA1:
						if (kvp.Value != Global.Game.Hash)
						{
							item.BackColor = Color.Pink;
							toolTip1.SetToolTip(DetailsView, "Current SHA1: " + Global.Game.Hash);
						}
						break;
					case MovieHeader.MOVIEVERSION:
						if (kvp.Value != MovieHeader.MovieVersion)
						{
							item.BackColor = Color.Yellow;
						}
						break;
					case MovieHeader.EMULATIONVERSION:
						if (kvp.Value != VersionInfo.GetEmuVersion())
						{
							item.BackColor = Color.Yellow;
						}
						break;
					case MovieHeader.PLATFORM:
						if (kvp.Value != Global.Game.System)
						{
							item.BackColor = Color.Pink;
						}
						break;
				}

				DetailsView.Items.Add(item);
			}

			var FpsItem = new ListViewItem("Fps");
			FpsItem.SubItems.Add(String.Format("{0:0.#######}", _movieList[x].Fps));
			DetailsView.Items.Add(FpsItem);

			var FramesItem = new ListViewItem("Frames");
			FramesItem.SubItems.Add(_movieList[x].RawFrames.ToString());
			DetailsView.Items.Add(FramesItem);

			CommentsBtn.Enabled = _movieList[x].Header.Comments.Count > 0;
			SubtitlesBtn.Enabled = _movieList[x].Subtitles.Count > 0;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = MovieView.SelectedIndices;
			if (indexes.Count > 0)
			{
				EditCommentsForm form = new EditCommentsForm();
				form.GetMovie(_movieList[MovieView.SelectedIndices[0]]);
				form.Show();
			}
		}

		private void button2_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = MovieView.SelectedIndices;
			if (indexes.Count > 0)
			{
				EditSubtitlesForm s = new EditSubtitlesForm { ReadOnly = true };
				s.GetMovie(_movieList[MovieView.SelectedIndices[0]]);
				s.Show();
			}
		}

		private void MovieView_DoubleClick(object sender, EventArgs e)
		{
			Run();
			Close();
		}

		private void MovieView_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void MovieView_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string path in filePaths)
			{
				if (Path.GetExtension(path) == "." + Global.Config.MovieExtension)
				{
					AddMovieToList(path, true);
				}
			}
		}

		private void MovieView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private void OrderColumn(int columnToOrder)
		{
			string columnName = MovieView.Columns[columnToOrder].Text;
			if (_sortedCol != columnName)
			{
				_sortReverse = false;
			}

			switch (columnName)
			{
				case "File":
					if (_sortReverse)
					{
						_movieList = _movieList
							.OrderByDescending(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.SysID)
							.ThenBy(x => x.GameName)
							.ThenBy(x => x.RawFrames)
							.ToList();
					}
					else
					{
						_movieList = _movieList
							.OrderBy(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.SysID)
							.ThenBy(x => x.GameName)
							.ThenBy(x => x.RawFrames)
							.ToList();
					}
					break;
				case "SysID":
					if (_sortReverse)
					{
						_movieList = _movieList
							.OrderByDescending(x => x.SysID)
							.ThenBy(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.GameName)
							.ThenBy(x => x.RawFrames)
							.ToList();
					}
					else
					{
						_movieList = _movieList
							.OrderBy(x => x.SysID)
							.ThenBy(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.GameName)
							.ThenBy(x => x.RawFrames)
							.ToList();
					}
					break;
				case "Game":
					if (_sortReverse)
					{
						_movieList = _movieList
							.OrderByDescending(x => x.GameName)
							.ThenBy(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.SysID)
							.ThenBy(x => x.RawFrames)
							.ToList();
					}
					else
					{
						_movieList = _movieList
							.OrderBy(x => x.GameName)
							.ThenBy(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.SysID)
							.ThenBy(x => x.RawFrames)
							.ToList();
					}
					break;
				case "Length (est.)":
					if (_sortReverse)
					{
						_movieList = _movieList
							.OrderByDescending(x => x.RawFrames)
							.ThenBy(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.SysID)
							.ThenBy(x => x.GameName)
							.ToList();
					}
					else
					{
						_movieList = _movieList
							.OrderBy(x => x.RawFrames)
							.ThenBy(x => Path.GetFileName(x.Filename))
							.ThenBy(x => x.SysID)
							.ThenBy(x => x.GameName)
							.ToList();
					}
					break;
			}

			_sortedCol = columnName;
			_sortReverse = !_sortReverse;
			MovieView.Refresh();
		}

		private void IncludeSubDirectories_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.PlayMovie_IncludeSubdir = IncludeSubDirectories.Checked;
			ScanFiles();
			PreHighlightMovie();
		}

		private void ShowStateFiles_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.PlayMovie_ShowStateFiles = ShowStateFiles.Checked;
			ScanFiles();
			PreHighlightMovie();
		}

		private void Scan_Click(object sender, EventArgs e)
		{
			ScanFiles();
			PreHighlightMovie();
		}

		private void MatchGameNameCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.PlayMovie_MatchGameName = MatchGameNameCheckBox.Checked;
			ScanFiles();
			PreHighlightMovie();
		}

		private void MovieView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Control && e.KeyCode == Keys.C)
			{
				ListView.SelectedIndexCollection indexes = MovieView.SelectedIndices;
				if (indexes.Count > 0)
				{
					StringBuilder copyStr = new StringBuilder();
					foreach (int index in indexes)
					{
						copyStr
							.Append(_movieList[index].Filename).Append('\t')
							.Append(_movieList[index].SysID).Append('\t')
							.Append(_movieList[index].GameName).Append('\t')
							.Append(_movieList[index].GetTime(true)).AppendLine();

						Clipboard.SetDataObject(copyStr.ToString());
					}
				}
			}
		}

	}
}
