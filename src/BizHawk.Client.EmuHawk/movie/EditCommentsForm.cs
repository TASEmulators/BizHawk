using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class EditCommentsForm : Form
	{
		private readonly IMovie _movie;
		private readonly bool _readOnly;
		private string _lastHeaderClicked;
		private bool _sortReverse;
		private readonly bool _dispose;

		public EditCommentsForm(IMovie movie, bool readOnly, bool disposeOnClose = false)
		{
			_movie = movie;
			_readOnly = readOnly;
			_lastHeaderClicked = "";
			_sortReverse = false;
			_dispose = disposeOnClose;

			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;
		}

		private void EditCommentsForm_Load(object sender, EventArgs e)
		{
			if (_movie.Comments.Any())
			{
				for (int i = 0; i < _movie.Comments.Count; i++)
				{
					CommentGrid.Rows.Add();
					var c = CommentGrid.Rows[i].Cells[0];
					c.Value = _movie.Comments[i];
				}
			}

			if (_readOnly)
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

		private void Save()
		{
			_movie.Comments.Clear();
			for (int i = 0; i < CommentGrid.Rows.Count - 1; i++)
			{
				var c = CommentGrid.Rows[i].Cells[0];
				_movie.Comments.Add(c.Value.ToString());
			}

			_movie.Save();
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			Save();
			Close();
		}

		private void SaveBtn_Click(object sender, EventArgs e)
		{
			Save();
		}

		private void OnColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			SortColumn(CommentGrid.Columns[e.ColumnIndex]);
		}

		private void SortColumn(DataGridViewColumn e)
		{
			DataGridViewColumn column = e;
			if (_lastHeaderClicked != column.Name)
			{
				_sortReverse = false;
			}

			var direction = !_sortReverse
				? ListSortDirection.Ascending
				: ListSortDirection.Descending;

			CommentGrid.Sort(column, direction);
			_lastHeaderClicked = column.Name;
			_sortReverse = !_sortReverse;
			CommentGrid.Refresh();
		}

		private void OnClosed(object sender, FormClosedEventArgs e)
		{
			if (_dispose && _movie is ITasMovie tasMovie)
			{
				tasMovie.Dispose();
			}
		}
	}
}
