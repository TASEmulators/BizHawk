using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
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
				this.Height = Height + ((CommentGrid.Rows.Count - 8) * 21);
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (!ReadOnly)
			{
				selectedMovie.ClearComments();
				for (int x = 0; x < CommentGrid.Rows.Count - 1; x++)
				{
					DataGridViewCell c = CommentGrid.Rows[x].Cells[0];
					selectedMovie.AddComment("comment " + c.Value.ToString());
				}
				selectedMovie.WriteMovie();
			}
			this.Close();
		}

		public void GetMovie(Movie m)
		{
			selectedMovie = m;
			List<string> comments = m.GetComments();
			if (comments.Count == 0) return;

			for (int x = 0; x < comments.Count; x++)
			{
				string str = comments[x];
				if (str.Length >= 7 && str.Substring(0, 7) == "comment")
					str = str.Remove(0, 7);
				CommentGrid.Rows.Add();
				DataGridViewCell c = CommentGrid.Rows[x].Cells[0];
				
				c.Value = str;
			}
		}
	}
}
