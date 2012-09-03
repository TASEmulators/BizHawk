using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.MultiClient
{
	public partial class PlayMovie : Form
	{
		List<Movie> MovieList = new List<Movie>();
		bool sortReverse;
		string sortedCol;

		public PlayMovie()
		{
			InitializeComponent();
			MovieView.QueryItemText += new QueryItemTextHandler(MovieView_QueryItemText);
			MovieView.QueryItemBkColor += new QueryItemBkColorHandler(MovieView_QueryItemBkColor);
			MovieView.VirtualMode = true;
			sortReverse = false;
			sortedCol = "";
		}

		void MovieView_QueryItemText(int index, int column, out string text)
		{
			text = "";
			if (column == 0) //File
			{
				text = Path.GetFileName(MovieList[index].Filename);
			}
			if (column == 1) //System
			{
				text = MovieList[index].SysID;
			}
			if (column == 2) //Game
			{
				text = MovieList[index].GameName;
			}
			if (column == 3) //Time
			{
				text = MovieList[index].GetTime(true);
			}
		}

		private void MovieView_QueryItemBkColor(int index, int column, ref Color color)
		{

		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void Run()
		{
			ListView.SelectedIndexCollection indexes = MovieView.SelectedIndices;
			if (indexes.Count == 0) 
				return;
			
			//Import file if necessary

			
			Global.MainForm.StartNewMovie(MovieList[MovieView.SelectedIndices[0]], false);
		}

		private void OK_Click(object sender, EventArgs e)
		{
			Global.MainForm.ReadOnly = ReadOnlyCheckBox.Checked;
			Run();
			this.Close();
		}

		private void BrowseMovies_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "");
			ofd.Filter = "Generic Movie Files (*." + Global.Config.MovieExtension + ")|*." + Global.Config.MovieExtension + ";*.zip;*.7z|" + Global.MainForm.GetMovieExtName() + "|Savestates|*.state|Archive Files|*.zip;*.7z|All Files|*.*";

			Global.Sound.StopSound();
			var result = ofd.ShowDialog();
			Global.Sound.StartSound();
			if (result == DialogResult.OK)
			{
				var file = new FileInfo(ofd.FileName);
				if (!file.Exists)
					return;
				else
				{
					if (file.Extension.ToUpper() == "STATE")
					{
						Movie m = new Movie(file.FullName);
						m.LoadMovie(); //State files will have to load everything unfortunately
						if (m.Frames == 0)
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

		private int AddStateToList(string filename)
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
						Movie m = new Movie(file.CanonicalFullPath);
						m.LoadMovie(); //State files will have to load everything unfortunately
						if (m.Frames > 0)
						{
							MovieList.Add(m);
							sortReverse = false;
							sortedCol = "";
							x = MovieList.Count - 1;
						}
					}
					return x;
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
						MovieView.ItemCount = MovieList.Count;
						UpdateList();

						sortReverse = false;
						sortedCol = "";
						x = MovieList.Count - 1;
					}
					return x;
				}
			}
		}

		private int IsDuplicate(string filename)
		{
			for (int x = 0; x < MovieList.Count; x++)
			{
				if (MovieList[x].Filename == filename)
				{
					return x;
				}
			}

			return 0;
		}

		private void PreLoadMovieFile(HawkFile path, bool force)
		{
			Movie m = new Movie(path.CanonicalFullPath);
			m.PreLoadText();
			if (path.Extension == ".FM2")
			{
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "NES");
			}
			else if (path.Extension == ".MC2")
			{
				m.Header.SetHeaderLine(MovieHeader.PLATFORM, "PCE");
			}
			//Don't do this from browse
			if (m.Header.GetHeaderLine(MovieHeader.GAMENAME) == Global.Game.Name ||
				Global.Config.PlayMovie_MatchGameName == false || force)
			{
				MovieList.Add(m);
			}
		}

		private void UpdateList()
		{
			MovieView.Refresh();
			UpdateMovieCount();
		}

		private void UpdateMovieCount()
		{
			int x = MovieList.Count;
			if (x == 1)
				MovieCount.Text = x.ToString() + " movie";
			else
				MovieCount.Text = x.ToString() + " movies";
		}

		private void PreHighlightMovie()
		{
			if (Global.Game == null) return;
			List<int> Indexes = new List<int>();
			
			//Pull out matching names
			for (int x = 0; x < MovieList.Count; x++)
			{
				if (PathManager.FilesystemSafeName(Global.Game) == MovieList[x].GameName)
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
				if (Path.GetExtension(MovieList[Indexes[x]].Filename).ToUpper() == "." + Global.Config.MovieExtension)
					TAS.Add(x);
			}
			if (TAS.Count == 1)
			{
				HighlightMovie(TAS[0]);
				return;
			}
			if (TAS.Count > 1)
				Indexes = new List<int>(TAS);

			//Final tie breaker - Last used file
			DateTime t = new DateTime();
			FileInfo f = new FileInfo(MovieList[Indexes[0]].Filename);
			t = f.LastAccessTime;
			int mostRecent = Indexes[0];
			for (int x = 1; x < Indexes.Count; x++)
			{
				f = new FileInfo(MovieList[Indexes[0]].Filename);
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
			MovieList.Clear();
			MovieView.ItemCount = 0;
			MovieView.Update();
		}

		private void ScanFiles()
		{
			ClearList();

			string d = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "");
			if (!Directory.Exists(d))
				Directory.CreateDirectory(d);
			string extension = "*." + Global.Config.MovieExtension;
			foreach (string f in Directory.GetFiles(d, "*." + Global.Config.MovieExtension))
				AddMovieToList(f, false);
			foreach (string f in Directory.GetFiles(d, "*.tas"))
				AddMovieToList(f, false);
			foreach (string f in Directory.GetFiles(d, "*.bkm"))
				AddMovieToList(f, false);
			if (Global.Config.PlayMovie_ShowStateFiles)
			{
				foreach (string f in Directory.GetFiles(d, "*.state"))
					AddStateToList(f);
			}

			if (Global.Config.PlayMovie_IncludeSubdir)
			{
				string[] subs = Directory.GetDirectories(d);
				foreach (string dir in subs)
				{
					foreach (string f in Directory.GetFiles(dir, "*." + Global.Config.MovieExtension))
						AddMovieToList(f, false);
					if (Global.Config.PlayMovie_ShowStateFiles)
					{
						foreach (string f in Directory.GetFiles(d, "*.state"))
							AddStateToList(f);
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
			toolTip1.SetToolTip(DetailsView, "");
			DetailsView.Items.Clear();
			if (MovieView.SelectedIndices.Count < 1)
			{
				OK.Enabled = false;
				return;
			}
			else
				OK.Enabled = true;

			int x = MovieView.SelectedIndices[0];
			MovieView.ensureVisible(x);
			Dictionary<string, string> h = MovieList[x].Header.HeaderParams;

			foreach (var kvp in h)
			{
				ListViewItem item = new ListViewItem(kvp.Key);
				item.SubItems.Add(kvp.Value);

				switch (kvp.Key.ToString())
				{
					case MovieHeader.SHA1:
						if (kvp.Value.ToString() != Global.Game.Hash)
						{
							item.BackColor = Color.Pink;
							toolTip1.SetToolTip(DetailsView, "Current SHA1: " + Global.Game.Hash);
						}
						break;
					case MovieHeader.MOVIEVERSION:
						if (kvp.Value.ToString() != MovieHeader.MovieVersion)
						{
							item.BackColor = Color.Yellow;
						}
						break;
					case MovieHeader.EMULATIONVERSION:
						if (kvp.Value.ToString() != MainForm.EMUVERSION)
						{
							item.BackColor = Color.Yellow;
						}
						break;
					case MovieHeader.PLATFORM:
						if (kvp.Value.ToString() != Global.Game.System)
						{
							item.BackColor = Color.Pink;
						}
						break;
				}
				

				DetailsView.Items.Add(item);
			}
			if (MovieList[x].Header.Comments.Count > 0)
				button1.Enabled = true;
			else
				button1.Enabled = false;

			if (MovieList[x].Subtitles.Count() > 0)
				button2.Enabled = true;
			else
				button2.Enabled = false;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = MovieView.SelectedIndices;
			if (indexes.Count == 0) return;
			EditCommentsForm c = new EditCommentsForm();
			c.ReadOnly = true;
			c.GetMovie(MovieList[MovieView.SelectedIndices[0]]);
			c.Show();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			ListView.SelectedIndexCollection indexes = MovieView.SelectedIndices;
			if (indexes.Count == 0) return;
			EditSubtitlesForm s = new EditSubtitlesForm();
			s.ReadOnly = true;
			s.GetMovie(MovieList[MovieView.SelectedIndices[0]]);
			s.Show();
		}

		private void MovieView_DoubleClick(object sender, EventArgs e)
		{
			Run();
			this.Close();
		}

		private void MovieView_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
		}

		private void MovieView_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			foreach (string path in filePaths)
			{
				if (Path.GetExtension(path) == "." + Global.Config.MovieExtension)
					AddMovieToList(path, true);
			}
		}

		private void MovieView_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			OrderColumn(e.Column);
		}

		private void OrderColumn(int columnToOrder)
		{
			string columnName = MovieView.Columns[columnToOrder].Text;
			if (sortedCol.CompareTo(columnName) != 0)
				sortReverse = false;
			MovieList.Sort((x, y) => x.CompareTo(y, columnName) * (sortReverse ? -1 : 1));
			sortedCol = columnName;
			sortReverse = !(sortReverse);
			MovieView.Refresh();
		}

		private void IncludeSubDirectories_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.PlayMovie_IncludeSubdir = IncludeSubDirectories.Checked;
		}

		private void ShowStateFiles_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.PlayMovie_ShowStateFiles = ShowStateFiles.Checked;
		}

		private void Scan_Click(object sender, EventArgs e)
		{
			ScanFiles();
			PreHighlightMovie();
		}

		private void MatchGameNameCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			Global.Config.PlayMovie_MatchGameName = MatchGameNameCheckBox.Checked;
		}

	}
}
