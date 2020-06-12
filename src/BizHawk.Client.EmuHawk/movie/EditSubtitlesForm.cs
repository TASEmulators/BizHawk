using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class EditSubtitlesForm : Form
	{
		
		private readonly IMovie _selectedMovie;
		private readonly bool _readOnly;

		public EditSubtitlesForm(IMovie movie, bool readOnly)
		{
			_selectedMovie = movie;
			_readOnly = readOnly;
			InitializeComponent();
		}

		private void EditSubtitlesForm_Load(object sender, EventArgs e)
		{
			var subs = new SubtitleList();
			subs.AddRange(_selectedMovie.Subtitles);

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
				c.Value = $"{s.Color:X8}";
				c.Style.BackColor = Color.FromArgb((int)s.Color);
				c = SubGrid.Rows[x].Cells[5];
				c.Value = s.Message;
			}

			if (_readOnly)
			{
				// Set all columns to read only
				for (int i = 0; i < SubGrid.Columns.Count; i++)
				{
					SubGrid.Columns[i].ReadOnly = true;
				}

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
			var error = $"Unable to parse value: {c.Value}";
			var caption = $"Parse Error Row {row} Column {column}";
			MessageBox.Show(error, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			if (!_readOnly)
			{
				_selectedMovie.Subtitles.Clear();
				for (int i = 0; i < SubGrid.Rows.Count - 1; i++)
				{
					var sub = new Subtitle();
					
					var c = SubGrid.Rows[i].Cells[0];
					try { sub.Frame = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 0); return; }
					c = SubGrid.Rows[i].Cells[1];
					try { sub.X = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 1); return; }
					c = SubGrid.Rows[i].Cells[2];
					try { sub.Y = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 2); return; }
					c = SubGrid.Rows[i].Cells[3];
					try { sub.Duration = int.Parse(c.Value.ToString()); }
					catch { ShowError(i, 3); return; }
					c = SubGrid.Rows[i].Cells[4];
					try { sub.Color = uint.Parse(c.Value.ToString(), NumberStyles.HexNumber); }
					catch { ShowError(i, 4); return; }
					try { c = SubGrid.Rows[i].Cells[5]; }
					catch { ShowError(i, 5); return; }
					sub.Message = c.Value?.ToString();
					_selectedMovie.Subtitles.Add(sub);
				}
				_selectedMovie.Save();
			}

			Close();
		}

		private void ChangeRow(Subtitle s, int index)
		{
			if (index >= SubGrid.Rows.Count)
			{
				return;
			}

			var c = SubGrid.Rows[index].Cells[0];
			c.Value = s.Frame;
			c = SubGrid.Rows[index].Cells[1];
			c.Value = s.X;
			c = SubGrid.Rows[index].Cells[2];
			c.Value = s.Y;
			c = SubGrid.Rows[index].Cells[3];
			c.Value = s.Duration;
			c = SubGrid.Rows[index].Cells[4];
			c.Value = $"{s.Color:X8}";
			c.Style.BackColor = Color.FromArgb((int)s.Color);
			c = SubGrid.Rows[index].Cells[5];
			c.Value = s.Message;
		}

		private Subtitle GetRow(int index)
		{
			if (index >= SubGrid.Rows.Count)
			{
				return new Subtitle();
			}
			
			var sub = new Subtitle();

			if (int.TryParse(SubGrid.Rows[index].Cells[0].Value.ToString(), out int frame))
			{
				sub.Frame = frame;
			}

			if (int.TryParse(SubGrid.Rows[index].Cells[1].Value.ToString(), out int x))
			{
				sub.X = x;
			}

			if (int.TryParse(SubGrid.Rows[index].Cells[2].Value.ToString(), out int y))
			{
				sub.Y = y;
			}

			if (int.TryParse(SubGrid.Rows[index].Cells[3].Value.ToString(), out int duration))
			{
				sub.Duration = duration;
			}
			
			if (uint.TryParse(SubGrid.Rows[index].Cells[4].Value.ToString(), out uint color))
			{
				sub.Color = color;
			}
			
			sub.Message = SubGrid.Rows[index].Cells[5].Value?.ToString() ?? "";

			_selectedMovie.Subtitles.Add(sub);
			return sub;
		}

		private void SubGrid_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (_readOnly)
			{
				return;
			}

			var c = SubGrid.SelectedRows;
			if (c.Count == 0)
			{
				return;
			}

			using var s = new SubtitleMaker { Sub = GetRow(c[0].Index) };
			if (s.ShowDialog() == DialogResult.OK)
			{
				ChangeRow(s.Sub, SubGrid.SelectedRows[0].Index);
			}
		}

		private void Export_Click(object sender, EventArgs e)
		{
			// Get file to save as
			using var form = new SaveFileDialog
			{
				AddExtension = true,
				Filter = new FilesystemFilterSet(new FilesystemFilter("SubRip Files", new[] { "srt" })).ToString()
			};

			var result = form.ShowDialog();
			var fileName = form.FileName;

			form.Dispose();

			if (result != DialogResult.OK)
			{
				return;
			}

			// Fetch fps
			var system = _selectedMovie.HeaderEntries[HeaderKeys.Platform];
			var pal = _selectedMovie.HeaderEntries.ContainsKey(HeaderKeys.Pal)
				&& _selectedMovie.HeaderEntries[HeaderKeys.Pal] == "1";
			var pfr = new PlatformFrameRates();
			double fps;

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
					MessageBoxIcon.Error);

				return;
			}

			// Create string and write to file
			var str = _selectedMovie.Subtitles.ToSubRip(fps);
			File.WriteAllText(fileName, str);

			// Display success
			MessageBox.Show($"Subtitles successfully exported to {fileName}.", "Success");
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
