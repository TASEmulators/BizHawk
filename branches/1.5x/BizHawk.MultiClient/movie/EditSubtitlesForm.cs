using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public partial class EditSubtitlesForm : Form
	{
		public bool ReadOnly;
		private Movie selectedMovie = new Movie();


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
			{
				int x = Height + ((SubGrid.Rows.Count - 8) * 21);
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

		private void ShowError(int row, int column)
		{
			DataGridViewCell c = SubGrid.Rows[row].Cells[column];
			string error = "Unable to parse value: " + c.Value;
			string caption = "Parse Error Row " + row.ToString() + " Column " + column.ToString();
			MessageBox.Show(error, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
					try { s.Frame = int.Parse(c.Value.ToString()); }
					catch { ShowError(x, 0); return; }
					c = SubGrid.Rows[x].Cells[1];
					try { s.X = int.Parse(c.Value.ToString()); }
					catch { ShowError(x, 1); return; }
					c = SubGrid.Rows[x].Cells[2];
					try { s.Y = int.Parse(c.Value.ToString()); }
					catch { ShowError(x, 2); return; }
					c = SubGrid.Rows[x].Cells[3];
					try { s.Duration = int.Parse(c.Value.ToString()); }
					catch { ShowError(x, 3); return; }
					c = SubGrid.Rows[x].Cells[4];
					try { s.Color = uint.Parse(c.Value.ToString(), NumberStyles.HexNumber); }
					catch { ShowError(x, 4); return; }
					try { c = SubGrid.Rows[x].Cells[5]; }
					catch { ShowError(x, 5); return; }
					s.Message = c.Value.ToString();
					selectedMovie.Subtitles.AddSubtitle(s);
				}
				selectedMovie.WriteMovie();
			}
			Close();
		}

		public void GetMovie(Movie m)
		{
			selectedMovie = m;
			SubtitleList subs = new SubtitleList(m);
			if (subs.Count == 0) return;

			for (int x = 0; x < subs.Count; x++)
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
				c.Value = String.Format("{0:X8}", s.Color);
				c.Style.BackColor = Color.FromArgb((int)s.Color);
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
			c.Value = String.Format("{0:X8}", s.Color);
			c.Style.BackColor = Color.FromArgb((int)s.Color);
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
			if (ReadOnly) return;
			DataGridViewSelectedRowCollection c = SubGrid.SelectedRows;
			if (c.Count == 0) return;
			SubtitleMaker s = new SubtitleMaker();
			s.sub = GetRow(c[0].Index);
			if (s.ShowDialog() == DialogResult.OK)
			{
				ChangeRow(s.sub, SubGrid.SelectedRows[0].Index);
				//if (SubGrid.Rows.Count == SubGrid.SelectedRows[0].Index + 1)
				//	SubGrid.Rows.Add(); //Why does this case ChangeRow to edit the new changed row?
			}
		}
	}
}
