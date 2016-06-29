using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;

using BizHawk.Client.Common;
using System.IO;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class EditSubtitlesForm : Form
	{
		public bool ReadOnly;
		private IMovie _selectedMovie;

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
				var x = Height + ((SubGrid.Rows.Count - 8) * 21);
				Height = x < 600 ? x : 600;
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void ShowError(int row, int column)
		{
			var c = SubGrid.Rows[row].Cells[column];
			var error = "Unable to parse value: " + c.Value;
			var caption = "Parse Error Row " + row + " Column " + column;
			MessageBox.Show(error, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void OK_Click(object sender, EventArgs e)
		{
			if (!ReadOnly)
			{
				_selectedMovie.Subtitles.Clear();
				for (int i = 0; i < SubGrid.Rows.Count - 1; i++)
				{
					var s = new Subtitle();
					
					var c = SubGrid.Rows[i].Cells[0];
					try { s.Frame = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 0); return; }
					c = SubGrid.Rows[i].Cells[1];
					try { s.X = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 1); return; }
					c = SubGrid.Rows[i].Cells[2];
					try { s.Y = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 2); return; }
					c = SubGrid.Rows[i].Cells[3];
					try { s.Duration = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 3); return; }
					c = SubGrid.Rows[i].Cells[4];
					try { s.Color = uint.Parse(c.Value.ToString(), NumberStyles.HexNumber); }
					catch { ShowError(i, 4); return; }
					try { c = SubGrid.Rows[i].Cells[5]; }
					catch { ShowError(i, 5); return; }
					s.Message = c.Value.ToString();
					_selectedMovie.Subtitles.Add(s);
				}
				_selectedMovie.Save();
			}
			Close();
		}

		public void GetMovie(IMovie m)
		{
			_selectedMovie = m;
			var subs = new SubtitleList();
			subs.AddRange(m.Subtitles);

			for (int x = 0; x < subs.Count; x++)
			{
				var s = subs[x];
				SubGrid.Rows.Add();
				var c = SubGrid.Rows[x].Cells[0];
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
			var c = SubGrid.Rows[index].Cells[0];
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
			
			var s = new Subtitle();
			var c = SubGrid.Rows[index].Cells[0];

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
			_selectedMovie.Subtitles.Add(s);

			return s;
		}

		private void SubGrid_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (ReadOnly) return;
			var c = SubGrid.SelectedRows;
			if (c.Count == 0) return;
			var s = new SubtitleMaker {Sub = GetRow(c[0].Index)};
			if (s.ShowDialog() == DialogResult.OK)
			{
				ChangeRow(s.Sub, SubGrid.SelectedRows[0].Index);
			}
		}

        private void Export_Click(object sender, EventArgs e)
        {
            // Get file to save as
            var form = new SaveFileDialog();
            form.AddExtension = true;
            form.Filter = "SubRip Files (*.srt)|*.srt|All files (*.*)|*.*";

            var result = form.ShowDialog();
            var fileName = form.FileName;

            form.Dispose();

            if (result != System.Windows.Forms.DialogResult.OK)
                return;

            // Fetch fps
            var system = _selectedMovie.HeaderEntries[HeaderKeys.PLATFORM];
            var pal = _selectedMovie.HeaderEntries.ContainsKey(HeaderKeys.PAL)
                && _selectedMovie.HeaderEntries[HeaderKeys.PAL] == "1";
            var pfr = new PlatformFrameRates();
            double fps = 1;

            try
            {
                fps = pfr[system, pal];
            }
            catch
            {
                MessageBox.Show(
                    "Could not determine movie fps, export failed.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                    );

                return;
            }

            // Create string and write to file
            var str = _selectedMovie.Subtitles.ToSubRip(fps);
            File.WriteAllText(fileName, str);

            // Display success
            MessageBox.Show(
                string.Format("Subtitles succesfully exported to {0}.", fileName),
                "Success"
                );
        }

		private void SubGrid_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
		{
			e.Row.Cells["Frame"].Value      = 0;
			e.Row.Cells["X"].Value          = 0;
			e.Row.Cells["Y"].Value          = 0;
			e.Row.Cells["Length"].Value     = 0;
			e.Row.Cells["DispColor"].Value  = "FFFFFFFF";
		}

		private void ConcatMultilines_CheckedChanged(object sender, EventArgs e)
		{
			_selectedMovie.Subtitles.ConcatMultilines = ConcatMultilines.Checked;
		}

		private void AddColorTag_CheckedChanged(object sender, EventArgs e)
		{
			_selectedMovie.Subtitles.AddColorTag = AddColorTag.Checked;
		}
	}
}
