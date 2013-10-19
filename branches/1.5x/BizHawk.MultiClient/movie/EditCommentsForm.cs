using System;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class EditCommentsForm : Form
	{
		public bool ReadOnly;
		private Movie selectedMovie = new Movie();

		public EditCommentsForm()
		{
			InitializeComponent();
		}

		private void EditCommentsForm_Load(object sender, EventArgs e)
		{
			if (ReadOnly)
			{
				CommentGrid.Columns[0].ReadOnly = true;
				Text = "View Comments";
			}

			if (CommentGrid.Rows.Count > 8)
			{
				int x = Height + ((CommentGrid.Rows.Count - 8) * 21);
				if (x < 600)
					Height = x;
				else
					Height = 600;
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (!ReadOnly)
			{
				selectedMovie.Header.Comments.Clear();
				for (int x = 0; x < CommentGrid.Rows.Count - 1; x++)
				{
					DataGridViewCell c = CommentGrid.Rows[x].Cells[0];
					selectedMovie.Header.Comments.Add("comment " + c.Value);
				}
				selectedMovie.WriteMovie();
			}
			Close();
		}

		public void GetMovie(Movie m)
		{
			selectedMovie = m;
			if (m.Header.Comments.Count == 0) return;

			for (int x = 0; x < m.Header.Comments.Count; x++)
			{
				string str = m.Header.Comments[x];
				if (str.Length >= 7 && str.Substring(0, 7) == "comment")
					str = str.Remove(0, 7);
				CommentGrid.Rows.Add();
				DataGridViewCell c = CommentGrid.Rows[x].Cells[0];
				
				c.Value = str;
			}
		}
	}
}
