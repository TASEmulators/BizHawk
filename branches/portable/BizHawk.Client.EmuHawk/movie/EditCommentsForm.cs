using System;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class EditCommentsForm : Form
	{
		private IMovie _selectedMovie;

		public EditCommentsForm()
		{
			InitializeComponent();
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
				_selectedMovie.Header.Comments.Clear();
				for (int i = 0; i < CommentGrid.Rows.Count - 1; i++)
				{
					var c = CommentGrid.Rows[i].Cells[0];
					_selectedMovie.Header.Comments.Add("comment " + c.Value);
				}
				_selectedMovie.Save();
			}
			Close();
		}

		public void GetMovie(IMovie m)
		{
			_selectedMovie = m;
			if (m.Header.Comments.Count == 0) return;

			for (int i = 0; i < m.Header.Comments.Count; i++)
			{
				var str = m.Header.Comments[i];
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
