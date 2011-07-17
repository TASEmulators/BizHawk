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
		// Option to include savestate files (that have an input log)
		
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
				text = Path.GetFileName(MovieList[index].Filename);
			if (column == 1) //System
				text = MovieList[index].GetSysID();
			if (column == 2) //Game
				text = MovieList[index].GetGameName();
			if (column == 3) //Time
				text = MovieList[index].GetTime(true);
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
			if (indexes.Count == 0) return;
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
			ofd.Filter = "Movie files (*.tas)|*.TAS;*.ZIP;*.7z|FCEUX Movies|*.FM2|PCEjin Movies|*.MC2|Archive Files|*.zip;*.7z|All Files|*.*";

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
					int x = AddMovieToList(ofd.FileName);
					if (x > 0)
					{
						MovieView.SelectedIndices.Clear();
						MovieView.setSelection(x);
						MovieView.SelectItem(x, true);
					}
				}
			}
		}

		private int AddMovieToList(string filename)
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
						PreLoadMovieFile(file);
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
				if (MovieList[x].Filename == filename)
					return x;
			return 0;
		}

		private void PreLoadMovieFile(HawkFile path)
		{
			Movie m = new Movie(path.CanonicalFullPath, MOVIEMODE.INACTIVE);
			m.PreLoadText();
			if (path.Extension == ".FM2")
				m.SetHeaderLine(MovieHeader.PLATFORM, "NES");
			else if (path.Extension == ".MC2")
				m.SetHeaderLine(MovieHeader.PLATFORM, "PCE");
			MovieList.Add(m);
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
				if (Global.Game.FilesystemSafeName == MovieList[x].GetGameName())
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
				if (Path.GetExtension(MovieList[Indexes[x]].Filename).ToUpper() == ".TAS")
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

		private void PlayMovie_Load(object sender, EventArgs e)
		{
			IncludeSubDirectories.Checked = Global.Config.PlayMovie_IncludeSubdir;
			string d = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "");
			if (!Directory.Exists(d))
				Directory.CreateDirectory(d);

			foreach (string f in Directory.GetFiles(d, "*.tas"))
				AddMovieToList(f);
			foreach (string f in Directory.GetFiles(d, "*.fm2"))
				AddMovieToList(f);
			foreach (string f in Directory.GetFiles(d, "*.mc2"))
				AddMovieToList(f);

			if (Global.Config.PlayMovie_IncludeSubdir)
			{
				string[] subs = Directory.GetDirectories(d);
				foreach (string dir in subs)
				{
					foreach (string f in Directory.GetFiles(dir, "*.tas"))
						AddMovieToList(f);
					foreach (string f in Directory.GetFiles(dir, "*.fm2"))
						AddMovieToList(f);
					foreach (string f in Directory.GetFiles(dir, "*.mc2"))
						AddMovieToList(f);
				}
			}

			PreHighlightMovie();
		}

		private void MovieView_SelectedIndexChanged(object sender, EventArgs e)
		{
			DetailsView.Items.Clear();
			if (MovieView.SelectedIndices.Count < 1) return;

			int x = MovieView.SelectedIndices[0];
			Dictionary<string, string> h = MovieList[x].GetHeaderInfo();

			foreach (var kvp in h)
			{
				ListViewItem item = new ListViewItem(kvp.Key);
				item.SubItems.Add(kvp.Value);
				DetailsView.Items.Add(item);
			}
			if (MovieList[x].HasComments())
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
				if (Path.GetExtension(path) == ".tas" || Path.GetExtension(path) == ".fm2" ||
					Path.GetExtension(path) == ".mc2")
					AddMovieToList(path);
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

	}
}
