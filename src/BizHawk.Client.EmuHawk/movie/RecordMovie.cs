using System;
using System.IO;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	// TODO - Allow relative paths in record TextBox
	public partial class RecordMovie : Form
	{
		private readonly IMainFormForTools _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;
		private readonly FirmwareManager _firmwareManager;

		public RecordMovie(
			IMainFormForTools mainForm,
			Config config,
			GameInfo game,
			IEmulator core,
			IMovieSession movieSession,
			FirmwareManager firmwareManager)
		{
			_mainForm = mainForm;
			_config = config;
			_game = game;
			_emulator = core;
			_movieSession = movieSession;
			_firmwareManager = firmwareManager;
			InitializeComponent();
			Icon = Properties.Resources.TAStudioIcon;
			BrowseBtn.Image = Properties.Resources.OpenFile;

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
						path += $".{MovieService.StandardMovieExtension}";
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

				var movieToRecord = _movieSession.Get(path);

				var fileInfo = new FileInfo(path);
				if (!fileInfo.Exists)
				{
					Directory.CreateDirectory(fileInfo.DirectoryName);
				}

				if (StartFromCombo.SelectedItem.ToString() == "Now" && _emulator.HasSavestates())
				{
					var core = _emulator.AsStatable();

					movieToRecord.StartsFromSavestate = true;

					if (_config.Savestates.Type == SaveStateType.Binary)
					{
						movieToRecord.BinarySavestate = core.CloneSavestate();
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
					movieToRecord.StartsFromSaveRam = true;
					movieToRecord.SaveRam = core.CloneSaveRam();
				}

				movieToRecord.PopulateWithDefaultHeaderValues(
					_emulator,
					_game,
					_firmwareManager,
					AuthorBox.Text ?? _config.DefaultAuthor);
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
			
			var preferredExt = _movieSession.Movie?.PreferredExtension ?? "bk2";
			using var sfd = new SaveFileDialog
			{
				InitialDirectory = movieFolderPath,
				DefaultExt = $".{preferredExt}",
				FileName = RecordBox.Text,
				OverwritePrompt = false,
				Filter = new FilesystemFilterSet(new FilesystemFilter("Movie Files", new[] { preferredExt })).ToString()
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
			RecordBox.Text = _game.FilesystemSafeName();
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
