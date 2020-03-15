using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Client.Common.MovieConversionExtensions;

namespace BizHawk.Client.EmuHawk
{
	// TODO - Allow relative paths in record TextBox
	public partial class RecordMovie : Form
	{
		private readonly MainForm _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;

		public RecordMovie(
			MainForm mainForm,
			Config config,
			GameInfo game,
			IEmulator core,
			IMovieSession movieSession)
		{
			_mainForm = mainForm;
			_config = config;
			_game = game;
			_emulator = core;
			_movieSession = movieSession;
			InitializeComponent();

			if (!_emulator.HasSavestates())
			{
				StartFromCombo.Items.Remove(
					StartFromCombo.Items
						.OfType<object>()
						.First(i => i.ToString()
							.ToLower() == "now"));
			}

			if (!_emulator.HasSaveRam())
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

					path = _config.PathEntries.MovieAbsolutePath() + path;

					if (!MovieService.MovieExtensions.Contains(Path.GetExtension(path)))
					{
						// If no valid movie extension, add movie extension
						path += $".{MovieService.DefaultExtension}";
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
					var result = MessageBox.Show($"{path} already exists, overwrite?", "Confirm overwrite", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
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

				if (StartFromCombo.SelectedItem.ToString() == "Now" && _emulator.HasSavestates())
				{
					var core = _emulator.AsStatable();

					movieToRecord.StartsFromSavestate = true;
					movieToRecord.StartsFromSaveRam = false;

					if (_config.SaveStateType == SaveStateTypeE.Binary)
					{
						movieToRecord.BinarySavestate = (byte[])core.SaveStateBinary().Clone();
					}
					else
					{
						using var sw = new StringWriter();
						core.SaveStateText(sw);
						movieToRecord.TextSavestate = sw.ToString();
					}

					// TODO: do we want to support optionally not saving this?
					movieToRecord.SavestateFramebuffer = new int[0];
					if (_emulator.HasVideoProvider())
					{
						movieToRecord.SavestateFramebuffer = (int[])_emulator.AsVideoProvider().GetVideoBuffer().Clone();
					}
				}
				else if (StartFromCombo.SelectedItem.ToString() == "SaveRam"  && _emulator.HasSaveRam())
				{
					var core = _emulator.AsSaveRam();
					movieToRecord.StartsFromSavestate = false;
					movieToRecord.StartsFromSaveRam = true;
					movieToRecord.SaveRam = core.CloneSaveRam();
				}

				movieToRecord.PopulateWithDefaultHeaderValues(AuthorBox.Text);
				movieToRecord.Save();
				_mainForm.StartNewMovie(movieToRecord, true);

				_config.UseDefaultAuthor = DefaultAuthorCheckBox.Checked;
				if (DefaultAuthorCheckBox.Checked)
				{
					_config.DefaultAuthor = AuthorBox.Text;
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
			string movieFolderPath = _config.PathEntries.MovieAbsolutePath();
			
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
				if (movieDirException is IOException
					|| movieDirException is UnauthorizedAccessException)
				{
					//TO DO : Pass error to user?
				}
				else throw;
			}
			
			using var sfd = new SaveFileDialog
			{
				InitialDirectory = movieFolderPath,
				DefaultExt = $".{_movieSession.Movie.PreferredExtension}",
				FileName = RecordBox.Text,
				OverwritePrompt = false,
				Filter = new FilesystemFilterSet(new FilesystemFilter("Movie Files", new[] { _movieSession.Movie.PreferredExtension })).ToString()
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
			RecordBox.Text = PathManager.FilesystemSafeName(_game);
			StartFromCombo.SelectedIndex = 0;
			DefaultAuthorCheckBox.Checked = _config.UseDefaultAuthor;
			if (_config.UseDefaultAuthor)
			{
				AuthorBox.Text = _config.DefaultAuthor;
			}
		}

		private void RecordBox_DragEnter(object sender, DragEventArgs e)
		{
			e.Set(DragDropEffects.Copy);
		}

		private void RecordBox_DragDrop(object sender, DragEventArgs e)
		{
			var filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			RecordBox.Text = filePaths[0];
		}
	}
}
