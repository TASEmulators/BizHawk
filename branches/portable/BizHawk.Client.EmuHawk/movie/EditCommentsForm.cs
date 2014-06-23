using System;
using System.Windows.Forms;
using System.ComponentModel;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class EditCommentsForm : Form
	{
		private IMovie _selectedMovie;
        private String _lastHeaderClicked;
        private Boolean _sortReverse;
        
        public EditCommentsForm()
        {
            InitializeComponent();
            _lastHeaderClicked = "";
            _sortReverse = false;
        }

        private void OnColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            SortColumn(CommentGrid.Columns[e.ColumnIndex]);
        }

        private void SortColumn(DataGridViewColumn e)
        {
            ListSortDirection _direction;
            DataGridViewColumn _column = e;
            if (_lastHeaderClicked != _column.Name)
            {
                _sortReverse = false;
            }
            if (!_sortReverse)
            {
                _direction = ListSortDirection.Ascending;
            }
            else
            {
                _direction = ListSortDirection.Descending;
            }
            CommentGrid.Sort(_column, _direction);
            _lastHeaderClicked = _column.Name;
            _sortReverse = !_sortReverse;
            CommentGrid.Refresh();
        }

		private void EditCommentsForm_Load(object sender, EventArgs e)
		{
			if (Global.MovieSession.ReadOnly)
			{
				CommentGrid.Columns[0].ReadOnly = true;
				Text = "View Comments";
			}

			if (CommentGrid.Rows.Count > 8)
			{
				var x = Height + ((CommentGrid.Rows.Count - 8) * 21);
				Height = x < 600 ? x : 600;
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (!Global.MovieSession.ReadOnly)
			{
				_selectedMovie.Comments.Clear();
				for (int i = 0; i < CommentGrid.Rows.Count - 1; i++)
				{
					var c = CommentGrid.Rows[i].Cells[0];
					_selectedMovie.Comments.Add("comment " + c.Value);
				}
				_selectedMovie.Save();
			}
			Close();
		}

		public void GetMovie(IMovie m)
		{
			_selectedMovie = m;
			if (m.Comments.Count == 0) return;

			for (int i = 0; i < m.Comments.Count; i++)
			{
				var str = m.Comments[i];
				if (str.Length >= 7 && str.Substring(0, 7) == "comment")
				{
					str = str.Remove(0, 7);
				}
				CommentGrid.Rows.Add();
				var c = CommentGrid.Rows[i].Cells[0];
				c.Value = str;
			}
		}
	}
}
