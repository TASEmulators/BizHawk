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
        private List<IMovie> _movieList = new List<IMovie>();
        private bool _sortReverse;
        private string _sortedCol;

        private bool _sortDetailsReverse;
        private string _sortedDetailsCol;
        
        public PlayMovie()
        {
            InitializeComponent();
            MovieView.QueryItemText += MovieView_QueryItemText;
            MovieView.VirtualMode = true;
            _sortReverse = false;
            _sortedCol = String.Empty;

            _sortDetailsReverse = false;
            _sortedDetailsCol = String.Empty;
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
                text = _movieList[index].Header.SystemID;
            }
            if (column == 2) //Game
            {
                text = _movieList[index].Header.GameName;
            }
            if (column == 3) //Time
            {
                text = _movieList[index].Time.ToString(@"hh\:mm\:ss\.fff");
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Run()
        {
            var indices = MovieView.SelectedIndices;
            if (indices.Count > 0) //Import file if necessary
            {
                GlobalWin.MainForm.StartNewMovie(_movieList[MovieView.SelectedIndices[0]], false);
            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            Run();
            Global.MovieSession.ReadOnly = ReadOnlyCheckBox.Checked;
            Close();
        }

        private void BrowseMovies_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog { InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null) };
            var filter = "Movie Files (*." + Global.Config.MovieExtension + ")|*." + Global.Config.MovieExtension + "|Savestates|*.state|All Files|*.*";
            ofd.Filter = filter;

            var result = ofd.ShowHawkDialog();
            if (result == DialogResult.OK)
            {
                var file = new FileInfo(ofd.FileName);
                if (!file.Exists)
                {
                    return;
                }
                else
                {
                    if (file.Extension.ToUpper() == "STATE")
                    {
                        var movie = new Movie(file.FullName);
                        movie.Load(); //State files will have to load everything unfortunately
                        if (movie.FrameCount == 0)
                        {
                            MessageBox.Show("No input log detected in this savestate, aborting", "Can not load file", MessageBoxButtons.OK,
                                            MessageBoxIcon.Hand);
                            return;
                        }
                    }

                    int? index = AddMovieToList(ofd.FileName, true);
                    if (index.HasValue)
                    {
                        MovieView.SelectedIndices.Clear();
                        MovieView.setSelection(index.Value);
                        MovieView.SelectItem(index.Value, true);
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
                    if (!IsDuplicateOf(filename).HasValue)
                    {
                        var movie = new Movie(file.CanonicalFullPath);
                        movie.Load(); //State files will have to load everything unfortunately
                        if (movie.FrameCount > 0)
                        {
                            _movieList.Add(movie);
                            _sortReverse = false;
                            _sortedCol = String.Empty;
                        }
                    }
                }
            }
        }

        private int? AddMovieToList(string filename, bool force)
        {
            using (var file = new HawkFile(filename))
            {
                if (!file.Exists)
                {
                    return null;
                }
                else
                {
                    int? index = IsDuplicateOf(filename);
                    if (!index.HasValue)
                    {
                        PreLoadMovieFile(file, force);
                        MovieView.ItemCount = _movieList.Count;
                        UpdateList();

                        _sortReverse = false;
                        _sortedCol = String.Empty;
                        index = _movieList.Count - 1;
                    }
                    return index;
                }
            }
        }

        private int? IsDuplicateOf(string filename)
        {
            for (int i = 0; i < _movieList.Count; i++)
            {
                if (_movieList[i].Filename == filename)
                {
                    return i;
                }
            }

            return null;
        }

        private void PreLoadMovieFile(HawkFile hf, bool force)
        {
            var movie = new Movie(hf.CanonicalFullPath);
            movie.PreLoadText(hf);
            try
            {
                //Don't do this from browse
                if (movie.Header[HeaderKeys.GAMENAME] == Global.Game.Name ||
                    Global.Config.PlayMovie_MatchGameName == false || force)
                {
                    _movieList.Add(movie);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void UpdateList()
        {
            MovieView.Refresh();
            UpdateMovieCount();
        }

        private void UpdateMovieCount()
        {
            MovieCount.Text = _movieList.Count + " movie"
                + (_movieList.Count != 1 ? "s" : String.Empty);
        }

        private void PreHighlightMovie()
        {
            if (Global.Game == null) return;
            var Indices = new List<int>();

            //Pull out matching names
            for (int i = 0; i < _movieList.Count; i++)
            {
                if (PathManager.FilesystemSafeName(Global.Game) == _movieList[i].Header.GameName)
                {
                    Indices.Add(i);
                }
            }
            if (Indices.Count == 0) return;
            if (Indices.Count == 1)
            {
                HighlightMovie(Indices[0]);
                return;
            }

            //Prefer tas files
            var TAS = new List<int>();
            for (int i = 0; i < Indices.Count; i++)
            {
                if (Path.GetExtension(_movieList[Indices[i]].Filename).ToUpper() == "." + Global.Config.MovieExtension)
                {
                    TAS.Add(i);
                }
            }

            if (TAS.Count == 1)
            {
                HighlightMovie(TAS[0]);
                return;
            }
            else if (TAS.Count > 1)
            {
                Indices = new List<int>(TAS);
            }

            //Final tie breaker - Last used file
            var file = new FileInfo(_movieList[Indices[0]].Filename);
            var time = file.LastAccessTime;
            int mostRecent = Indices.First();
            for (int i = 1; i < Indices.Count; i++)
            {
                file = new FileInfo(_movieList[Indices[0]].Filename);
                if (file.LastAccessTime > time)
                {
                    time = file.LastAccessTime;
                    mostRecent = Indices[i];
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

            var directory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Directory.GetFiles(directory, "*." + Global.Config.MovieExtension)
                    .ToList()
                    .ForEach(file => AddMovieToList(file, force: false));

            if (Global.Config.PlayMovie_ShowStateFiles)
            {
                Directory.GetFiles(directory, "*.state")
                    .ToList()
                    .ForEach(file => AddStateToList(file));
            }

            if (Global.Config.PlayMovie_IncludeSubdir)
            {
                var subs = Directory.GetDirectories(directory);
                foreach (var dir in subs)
                {
                    Directory.GetFiles(dir, "*." + Global.Config.MovieExtension)
                    .ToList()
                    .ForEach(file => AddMovieToList(file, force: false));

                    Directory.GetFiles(dir, "*.state")
                    .ToList()
                    .ForEach(file => AddStateToList(file));
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

            int firstIndex = MovieView.SelectedIndices[0];
            MovieView.ensureVisible(firstIndex);

            foreach (var kvp in _movieList[firstIndex].Header)
            {
                var item = new ListViewItem(kvp.Key);
                item.SubItems.Add(kvp.Value);

                switch (kvp.Key)
                {
                    case HeaderKeys.SHA1:
                        if (kvp.Value != Global.Game.Hash)
                        {
                            item.BackColor = Color.Pink;
                            toolTip1.SetToolTip(DetailsView, "Current SHA1: " + Global.Game.Hash);
                        }
                        break;
                    case HeaderKeys.MOVIEVERSION:
                        if (kvp.Value != HeaderKeys.MovieVersion1)
                        {
                            item.BackColor = Color.Yellow;
                        }
                        break;
                    case HeaderKeys.EMULATIONVERSION:
                        if (kvp.Value != VersionInfo.GetEmuVersion())
                        {
                            item.BackColor = Color.Yellow;
                        }
                        break;
                    case HeaderKeys.PLATFORM:
                        if (kvp.Value != Global.Game.System)
                        {
                            item.BackColor = Color.Pink;
                        }
                        break;
                }

                DetailsView.Items.Add(item);
            }

            var FpsItem = new ListViewItem("Fps");
            FpsItem.SubItems.Add(String.Format("{0:0.#######}", _movieList[firstIndex].Fps));
            DetailsView.Items.Add(FpsItem);

            var FramesItem = new ListViewItem("Frames");
            FramesItem.SubItems.Add(_movieList[firstIndex].FrameCount.ToString());
            DetailsView.Items.Add(FramesItem);
            CommentsBtn.Enabled = _movieList[firstIndex].Header.Comments.Any();
            SubtitlesBtn.Enabled = _movieList[firstIndex].Header.Subtitles.Any();
        }

      
        private void button1_Click(object sender, EventArgs e)
        {
            var indices = MovieView.SelectedIndices;
            if (indices.Count > 0)
            {
                var form = new EditCommentsForm();
                form.GetMovie(_movieList[MovieView.SelectedIndices[0]]);
                form.Show();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var indices = MovieView.SelectedIndices;
            if (indices.Count > 0)
            {
                var s = new EditSubtitlesForm { ReadOnly = true };
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
            var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

            filePaths
                .Where(path => Path.GetExtension(path) == "." + Global.Config.MovieExtension)
                .ToList()
                .ForEach(path => AddMovieToList(path, force: true));
        }

        private void MovieView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            OrderColumn(e.Column);
        }

        private void OrderColumn(int columnToOrder)
        {
            var columnName = MovieView.Columns[columnToOrder].Text;
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
                            .ThenBy(x => x.Header.SystemID)
                            .ThenBy(x => x.Header.GameName)
                            .ThenBy(x => x.FrameCount)
                            .ToList();
                    }
                    else
                    {
                        _movieList = _movieList
                            .OrderBy(x => Path.GetFileName(x.Filename))
                            .ThenBy(x => x.Header.SystemID)
                            .ThenBy(x => x.Header.GameName)
                            .ThenBy(x => x.FrameCount)
                            .ToList();
                    }
                    break;
                case "SysID":
                    if (_sortReverse)
                    {
                        _movieList = _movieList
                            .OrderByDescending(x => x.Header.SystemID)
                            .ThenBy(x => Path.GetFileName(x.Filename))
                            .ThenBy(x => x.Header.GameName)
                            .ThenBy(x => x.FrameCount)
                            .ToList();
                    }
                    else
                    {
                        _movieList = _movieList
                            .OrderBy(x => x.Header.SystemID)
                            .ThenBy(x => Path.GetFileName(x.Filename))
                            .ThenBy(x => x.Header.GameName)
                            .ThenBy(x => x.FrameCount)
                            .ToList();
                    }
                    break;
                case "Game":
                    if (_sortReverse)
                    {
                        _movieList = _movieList
                            .OrderByDescending(x => x.Header.GameName)
                            .ThenBy(x => Path.GetFileName(x.Filename))
                            .ThenBy(x => x.Header.SystemID)
                            .ThenBy(x => x.FrameCount)
                            .ToList();
                    }
                    else
                    {
                        _movieList = _movieList
                            .OrderBy(x => x.Header.GameName)
                            .ThenBy(x => Path.GetFileName(x.Filename))
                            .ThenBy(x => x.Header.SystemID)
                            .ThenBy(x => x.FrameCount)
                            .ToList();
                    }
                    break;
                case "Length (est.)":
                    if (_sortReverse)
                    {
                        _movieList = _movieList
                            .OrderByDescending(x => x.FrameCount)
                            .ThenBy(x => Path.GetFileName(x.Filename))
                            .ThenBy(x => x.Header.SystemID)
                            .ThenBy(x => x.FrameCount)
                            .ToList();
                    }
                    else
                    {
                        _movieList = _movieList
                            .OrderBy(x => x.FrameCount)
                            .ThenBy(x => Path.GetFileName(x.Filename))
                            .ThenBy(x => x.Header.SystemID)
                            .ThenBy(x => x.Header.GameName)
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
                var indexes = MovieView.SelectedIndices;
                if (indexes.Count > 0)
                {
                    var copyStr = new StringBuilder();
                    foreach (int index in indexes)
                    {
                        copyStr
                            .Append(_movieList[index].Filename).Append('\t')
                            .Append(_movieList[index].Header.SystemID).Append('\t')
                            .Append(_movieList[index].Header.GameName).Append('\t')
                            .Append(_movieList[index].Time.ToString(@"hh\:mm\:ss\.fff"))
                            .AppendLine();

                        Clipboard.SetDataObject(copyStr.ToString());
                    }
                }
            }
        }

        private void DetailsView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            OrderDetailsColumn(e.Column);
        }

        /// <summary>
        /// Takes the values from DetailView and puts them in a List of Detail classes, this list is then sorted and put back in the DetailView
        /// </summary>
        /// <param name="columnToOrder"></param>
        private void OrderDetailsColumn(int columnToOrder)
        {
            List<MovieDetails> detailsList = new List<MovieDetails>();
            for (int i = 0; i < DetailsView.Items.Count; i++)
            {
                detailsList.Add(new MovieDetails { keys = DetailsView.Items[i].Text, values = DetailsView.Items[i].SubItems[1].Text, backgroundColor = DetailsView.Items[i].BackColor });
            }
            var columnName = DetailsView.Columns[columnToOrder].Text;
            if (_sortedDetailsCol != columnName)
            {
                _sortDetailsReverse = false;
            }
            switch (columnName)
            {
                    //Header, Value
                case "Header":
                    if (_sortDetailsReverse)
                    {
                        detailsList = detailsList
                            .OrderByDescending(x => x.keys)
                            .ThenBy(x => x.values).ToList();
                    }
                    else
                    {
                        detailsList = detailsList
                           .OrderBy(x => x.keys)
                           .ThenBy(x => x.values).ToList();
                    }
                    break;
                case "Value":
                    if (_sortDetailsReverse)
                    {
                        detailsList = detailsList
                            .OrderByDescending(x => x.values)
                            .ThenBy(x => x.keys).ToList();
                    }
                    else
                    {
                        detailsList = detailsList
                            .OrderBy(x => x.values)
                            .ThenBy(x => x.keys).ToList();
                    }
                    break;
            }
            int firstIndex = MovieView.SelectedIndices[0];
            DetailsView.Items.Clear();
            foreach (var detail in detailsList)
            {
                ListViewItem item = new ListViewItem { Text = detail.keys, BackColor = detail.backgroundColor };
                item.SubItems.Add(detail.values);
                DetailsView.Items.Add(item);
            }
            _sortedDetailsCol = columnName;
            _sortDetailsReverse = !_sortDetailsReverse;
        }
    }
}
