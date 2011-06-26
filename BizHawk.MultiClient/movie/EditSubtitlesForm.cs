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
				//Save subtitles to movie object & write to disk
			}
			this.Close();
		}

		public void GetMovie(Movie m)
		{
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
	}
}
