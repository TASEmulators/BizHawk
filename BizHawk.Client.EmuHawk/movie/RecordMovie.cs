using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

using BizHawk.Client.EmuHawk.WinFormExtensions;

namespace BizHawk.Client.EmuHawk
{
	// TODO - Allow relative paths in record textbox
	public partial class RecordMovie : Form
	{
		private IEmulator Emulator;

		public RecordMovie(IEmulator core)
		{
			InitializeComponent();

			Emulator = core;

			if (!Emulator.HasSavestates())
			{
				StartFromCombo.Items.Remove(
					StartFromCombo.Items
						.OfType<object>()
						.First(i => i.ToString()
							.ToLower() == "now"));
			}

			if (!Emulator.HasSaveRam())
			{
				StartFromCombo.Items.Remove(
					StartFromCombo.Items
						.OfType<object>()
						.First(i => i.ToString()
							.ToLower() == "saveram"));
			}
		}

		private string MakePath()
		{
			var path = RecordBox.Text;

			if (!string.IsNullOrWhiteSpace(path))
			{
				if (path.LastIndexOf(Path.DirectorySeparatorChar) == -1)
				{
					if (path[0] != Path.DirectorySeparatorChar)
					{
						path = path.Insert(0, Path.DirectorySeparatorChar.ToString());
					}

					path = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null) + path;

					if (!MovieService.MovieExtensions.Contains(Path.GetExtension(path)))
					{
						// If no valid movie extension, add movie extension
						path += "." + MovieService.DefaultExtension;
					}
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

				var fileInfo = new FileInfo(path);
				if (!fileInfo.Exists)
				{
					Directory.CreateDirectory(fileInfo.DirectoryName);
				}

				if (StartFromCombo.SelectedItem.ToString() == "Now" && Emulator.HasSavestates())
				{
					var core = Emulator.AsStatable();

					movieToRecord.StartsFromSavestate = true;
					movieToRecord.StartsFromSaveRam = false;

					if (core.BinarySaveStatesPreferred)
					{
						movieToRecord.BinarySavestate = (byte[])core.SaveStateBinary().Clone();
					}
					else
					{
						using (var sw = new StringWriter())
						{
							core.SaveStateText(sw);
							movieToRecord.TextSavestate = sw.ToString();
						}
					}
					// TODO: do we want to support optionally not saving this?
					if (true)
					{
						// hack: some IMovies eat the framebuffer, so don't bother with them
						movieToRecord.SavestateFramebuffer = new int[0];
						if (movieToRecord.SavestateFramebuffer != null && Emulator.HasVideoProvider())
						{
							movieToRecord.SavestateFramebuffer = (int[])Emulator.AsVideoProvider().GetVideoBuffer().Clone();
						}
					}
				}
				else if (StartFromCombo.SelectedItem.ToString() == "SaveRam"  && Emulator.HasSaveRam())
				{
					var core = Emulator.AsSaveRam();
					movieToRecord.StartsFromSavestate = false;
					movieToRecord.StartsFromSaveRam = true;
					movieToRecord.SaveRam = core.CloneSaveRam();
				}

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
			string movieFolderPath = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null);
			
			// Create movie folder if it doesn't already exist
			try
			{
				if (!Directory.Exists(movieFolderPath))
				{
					Directory.CreateDirectory(movieFolderPath);
				}
			}
			catch (Exception movieDirException)
			{
				if (movieDirException is IOException ||
						movieDirException is UnauthorizedAccessException ||
						movieDirException is PathTooLongException
					)
				{
					//TO DO : Pass error to user?
				}
				else throw;
			}
			
			var sfd = new SaveFileDialog
			{
				InitialDirectory = movieFolderPath,
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
