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
	public partial class EditSubtitlesForm : Form
	{
		public bool ReadOnly;
		private Movie selectedMovie = new Movie();

		//TODO: Tooltips on cells explaining format
		//TODO: Parse hex on color when saving
		//TODO: try/catch on parsing int
		//TODO: display color in hex when loading from movie
		//TODO: color if color cell = value of color cell

		public EditSubtitlesForm()
		{
			InitializeComponent();
		}

		private void EditSubtitlesForm_Load(object sender, EventArgs e)
		{
			if (ReadOnly)
			{
				//Set all columns to read only
				for (int x = 0; x < SubGrid.Columns.Count; x++)
					SubGrid.Columns[x].ReadOnly = true;
				Text = "View Subtitles";
			}

			if (SubGrid.Rows.Count > 8)
				this.Height = Height + ((SubGrid.Rows.Count-8) * 21);
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (!ReadOnly)
			{
				selectedMovie.Subtitles.ClearSubtitles();
				for (int x = 0; x < SubGrid.Rows.Count - 1; x++)
				{
					Subtitle s = new Subtitle();
					DataGridViewCell c = SubGrid.Rows[x].Cells[0];
					//TODO: try/catch parsing
					s.Frame = int.Parse(c.Value.ToString());
					c = SubGrid.Rows[x].Cells[1];
					s.X = int.Parse(c.Value.ToString());
					c = SubGrid.Rows[x].Cells[2];
					s.Y = int.Parse(c.Value.ToString());
					c = SubGrid.Rows[x].Cells[3];
					s.Duration = int.Parse(c.Value.ToString());
					c = SubGrid.Rows[x].Cells[4];
					s.Color = uint.Parse(c.Value.ToString());
					c = SubGrid.Rows[x].Cells[5];
					s.Message = c.Value.ToString();
					selectedMovie.Subtitles.AddSubtitle(s);
				}
				selectedMovie.WriteMovie();
			}
			this.Close();
		}

		public void GetMovie(Movie m)
		{
			selectedMovie = m;
			SubtitleList subs = new SubtitleList(m);
			if (subs.Count() == 0) return;

			for (int x = 0; x < subs.Count(); x++)
			{
				Subtitle s = subs.GetSubtitleByIndex(x);
				SubGrid.Rows.Add();
				DataGridViewCell c = SubGrid.Rows[x].Cells[0];
				c.Value = s.Frame;
				c = SubGrid.Rows[x].Cells[1];
				c.Value = s.X;
				c = SubGrid.Rows[x].Cells[2];
				c.Value = s.Y;
				c = SubGrid.Rows[x].Cells[3];
				c.Value = s.Duration;
				c = SubGrid.Rows[x].Cells[4];
				c.Value = s.Color; //TODO: view in hex
				c = SubGrid.Rows[x].Cells[5];
				c.Value = s.Message;
			}
		}

		private void ChangeRow(Subtitle s, int index)
		{
			if (index >= SubGrid.Rows.Count) return;
			DataGridViewCell c = SubGrid.Rows[index].Cells[0];
			c.Value = s.Frame;
			c = SubGrid.Rows[index].Cells[1];
			c.Value = s.X;
			c = SubGrid.Rows[index].Cells[2];
			c.Value = s.Y;
			c = SubGrid.Rows[index].Cells[3];
			c.Value = s.Duration;
			c = SubGrid.Rows[index].Cells[4];
			c.Value = s.Color; //TODO: view in hex
			c = SubGrid.Rows[index].Cells[5];
			c.Value = s.Message;
		}

		private Subtitle GetRow(int index)
		{
			if (index >= SubGrid.Rows.Count) return new Subtitle();
			
			Subtitle s = new Subtitle();
			DataGridViewCell c = SubGrid.Rows[index].Cells[0];

			//Empty catch because it should default to subtitle default value
			try { s.Frame = int.Parse(c.Value.ToString()); }
			catch { }
			c = SubGrid.Rows[index].Cells[1];
			try { s.X = int.Parse(c.Value.ToString()); }
			catch { }
			c = SubGrid.Rows[index].Cells[2];
			try { s.Y = int.Parse(c.Value.ToString()); }
			catch { }
			c = SubGrid.Rows[index].Cells[3];
			try { s.Duration = int.Parse(c.Value.ToString()); }
			catch { }
			c = SubGrid.Rows[index].Cells[4];
			try { s.Color = uint.Parse(c.Value.ToString()); }
			catch { }
			c = SubGrid.Rows[index].Cells[5];
			try { s.Message = c.Value.ToString(); }
			catch { }
			selectedMovie.Subtitles.AddSubtitle(s);

			return s;
		}

		private void SubGrid_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			DataGridViewSelectedRowCollection c = SubGrid.SelectedRows;
			if (c.Count == 0) return;
			SubtitleMaker s = new SubtitleMaker();
			s.sub = GetRow(c[0].Index);
			if (s.ShowDialog() == DialogResult.OK)
			{
				ChangeRow(s.sub, SubGrid.SelectedRows[0].Index);
			}
		}
	}
}
