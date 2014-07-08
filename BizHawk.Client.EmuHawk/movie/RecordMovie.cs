using System;
using System.IO;
using System.Windows.Forms;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RecordMovie : Form
	{
		// TODO
		// Allow relative paths in record textbox
		public RecordMovie()
		{
			InitializeComponent();
		}

		private string MakePath()
		{
			if (RecordBox.Text.Length == 0)
			{
				return string.Empty;
			}

			var path = RecordBox.Text;
			if (path.LastIndexOf(Path.DirectorySeparatorChar) == -1)
			{
				if (path[0] != Path.DirectorySeparatorChar)
				{
					path = path.Insert(0, Path.DirectorySeparatorChar.ToString());
				}

				path = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null) + path;

				if (path[path.Length - 4] != '.') // If no file extension, add movie extension
				{
					path += "." + Global.MovieSession.Movie.PreferredExtension;
				}
			}
			
			return path;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			var path = MakePath();
			if (!string.IsNullOrWhiteSpace(path))
			{
				var test = new FileInfo(path);
				if (test.Exists)
				{
					var result = MessageBox.Show(path + " already exists, overwrite?", "Confirm overwrite", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
					if (result == DialogResult.Cancel)
					{
						return;
					}
				}

				var movieToRecord = MovieService.Get(path);

				if (StartFromCombo.SelectedItem.ToString() == "Now")
				{
					var fileInfo = new FileInfo(path);
					if (!fileInfo.Exists)
					{
						Directory.CreateDirectory(fileInfo.DirectoryName);
					}

					movieToRecord.StartsFromSavestate = true;

					if (Global.Emulator.BinarySaveStatesPreferred)
					{
						movieToRecord.BinarySavestate = (byte[])Global.Emulator.SaveStateBinary().Clone();
					}
					else
					{
						using (var sw = new StringWriter())
						{
							Global.Emulator.SaveStateText(sw);
							movieToRecord.TextSavestate = sw.ToString();
						}
					}
				}

				// Header
				movieToRecord.PopulateWithDefaultHeaderValues(AuthorBox.Text);

				movieToRecord.Save();
				GlobalWin.MainForm.StartNewMovie(movieToRecord, true);

				Global.Config.UseDefaultAuthor = DefaultAuthorCheckBox.Checked;
				if (DefaultAuthorCheckBox.Checked)
				{
					Global.Config.DefaultAuthor = AuthorBox.Text;
				}

				Close();
			}
			else
			{
				MessageBox.Show("Please select a movie to record", "File selection error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void BrowseBtn_Click(object sender, EventArgs e)
		{
			var sfd = new SaveFileDialog
			{
				InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
				DefaultExt = "." + Global.MovieSession.Movie.PreferredExtension,
				FileName = RecordBox.Text,
				OverwritePrompt = false,
				Filter = "Movie Files (*." + Global.MovieSession.Movie.PreferredExtension + ")|*." + Global.MovieSession.Movie.PreferredExtension + "|All Files|*.*"
			};

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK
				&& !string.IsNullOrWhiteSpace(sfd.FileName))
			{
				RecordBox.Text = sfd.FileName;
			}
		}

		private void RecordMovie_Load(object sender, EventArgs e)
		{
			RecordBox.Text = PathManager.FilesystemSafeName(Global.Game);
			StartFromCombo.SelectedIndex = 0;
			DefaultAuthorCheckBox.Checked = Global.Config.UseDefaultAuthor;
			if (Global.Config.UseDefaultAuthor)
			{
				AuthorBox.Text = Global.Config.DefaultAuthor;
			}
		}

		private void RecordBox_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
		}

		private void RecordBox_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			RecordBox.Text = filePaths[0];
		}
	}
}
