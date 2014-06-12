using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

using Newtonsoft.Json;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

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

				return path;
			}
			else
			{
				return path;
			}
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			var path = MakePath();
			if (!String.IsNullOrWhiteSpace(path))
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

				// Movies 2.0 TODO
				IMovie _movieToRecord = MovieService.Get(path);

				if (StartFromCombo.SelectedItem.ToString() == "Now")
				{
					var fileInfo = new FileInfo(path);
					if (!fileInfo.Exists)
					{
						Directory.CreateDirectory(fileInfo.DirectoryName);
					}

					_movieToRecord.StartsFromSavestate = true;

					//TODO - some emulators (c++ cores) are just returning a hex string already
					//theres no sense hexifying those again. we need to record that fact in the IEmulator somehow
					var bytestate = Global.Emulator.SaveStateBinary();
					string stringstate = Convert.ToBase64String(bytestate);
					_movieToRecord.SavestateBinaryBase64Blob = stringstate;
				}
				else
				{
					
				}

				// Header

				_movieToRecord.Author = AuthorBox.Text;
				_movieToRecord.EmulatorVersion = VersionInfo.GetEmuVersion();
				_movieToRecord.Platform = Global.Game.System;

				// Sync Settings, for movies 1.0, just dump a json blob into a header line
				_movieToRecord.SyncSettingsJson = ConfigService.SaveWithType(Global.Emulator.GetSyncSettings());

				if (Global.Game != null)
				{
					_movieToRecord.GameName = PathManager.FilesystemSafeName(Global.Game);
					_movieToRecord.Hash = Global.Game.Hash;
					if (Global.Game.FirmwareHash != null)
					{
						_movieToRecord.FirmwareHash = Global.Game.FirmwareHash;
					}
				}
				else
				{
					_movieToRecord.GameName = "NULL";
				}

				if (Global.Emulator.BoardName != null)
				{
					_movieToRecord.BoardName = Global.Emulator.BoardName;
				}

				if (Global.Emulator.HasPublicProperty("DisplayType"))
				{
					var region = Global.Emulator.GetPropertyValue("DisplayType");
					if ((DisplayType)region == DisplayType.PAL)
					{
						_movieToRecord.HeaderEntries.Add(HeaderKeys.PAL, "1");
					}
				}

				if (Global.Emulator is LibsnesCore)
				{
					// TODO: shouldn't the Boardname property have sgb?
					_movieToRecord.HeaderEntries[HeaderKeys.SGB] = (Global.Emulator as LibsnesCore).IsSGB.ToString();
				}

				_movieToRecord.Core = ((CoreAttributes)Attribute
					.GetCustomAttribute(Global.Emulator.GetType(), typeof(CoreAttributes)))
					.CoreName;

				GlobalWin.MainForm.StartNewMovie(_movieToRecord, true);

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
			var filename = String.Empty;
			var sfd = new SaveFileDialog
				{
					InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPathFragment, null),
					DefaultExt = "." + Global.MovieSession.Movie.PreferredExtension,
					FileName = RecordBox.Text,
					OverwritePrompt = false
				};
			var filter = "Movie Files (*." + Global.MovieSession.Movie.PreferredExtension + ")|*." + Global.MovieSession.Movie.PreferredExtension + "|Savestates|*.state|All Files|*.*";
			sfd.Filter = filter;

			var result = sfd.ShowHawkDialog();
			if (result == DialogResult.OK
				&& !String.IsNullOrWhiteSpace(sfd.FileName))
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
