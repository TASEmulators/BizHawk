using System;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

using System.Reflection;

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
				return String.Empty;
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
					path += "." + Global.Config.MovieExtension;
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

				Movie _movieToRecord;

				if (StartFromCombo.SelectedItem.ToString() == "Now")
				{
					var fileInfo = new FileInfo(path);
					if (!fileInfo.Exists)
					{
						Directory.CreateDirectory(fileInfo.DirectoryName);
					}

					_movieToRecord = new Movie(path, startsFromSavestate: true);
					//TODO - some emulators (c++ cores) are just returning a hex string already
					//theres no sense hexifying those again. we need to record that fact in the IEmulator somehow
					var bytestate = Global.Emulator.SaveStateBinary();
					string stringstate = Convert.ToBase64String(bytestate);
					_movieToRecord.Header.SavestateBinaryBase64Blob = stringstate;
				}
				else
				{
					_movieToRecord = new Movie(path);
				}

				// Header
				_movieToRecord.Header[HeaderKeys.AUTHOR] = AuthorBox.Text;
				_movieToRecord.Header[HeaderKeys.EMULATIONVERSION] = VersionInfo.GetEmuVersion();
				_movieToRecord.Header[HeaderKeys.MOVIEVERSION] = HeaderKeys.MovieVersion1;
				_movieToRecord.Header[HeaderKeys.PLATFORM] = Global.Game.System;
				if (Global.Game != null)
				{
					_movieToRecord.Header[HeaderKeys.GAMENAME] = PathManager.FilesystemSafeName(Global.Game);
					_movieToRecord.Header[HeaderKeys.SHA1] = Global.Game.Hash;
					if (Global.Game.FirmwareHash != null)
					{
						_movieToRecord.Header[HeaderKeys.FIRMWARESHA1] = Global.Game.FirmwareHash;
					}
				}
				else
				{
					_movieToRecord.Header[HeaderKeys.GAMENAME] = "NULL";
				}

				if (Global.Emulator.BoardName != null)
				{
					_movieToRecord.Header[HeaderKeys.BOARDNAME] = Global.Emulator.BoardName;
				}

				if (Global.Emulator is Gameboy)
				{
					// probably won't fix any of this in movie 1.0?? (movie 2.0 only??)
					// FIXME: the multicartcompat is in the syncsettings object.  is that supposed to go here?
					// FIXME: these are never read back and given to the core, anywhere
					var s = (Gameboy.GambatteSyncSettings)Global.Emulator.GetSyncSettings();
					_movieToRecord.Header[HeaderKeys.GB_FORCEDMG] = s.ForceDMG.ToString();
					_movieToRecord.Header[HeaderKeys.GB_GBA_IN_CGB] = s.GBACGB.ToString();
				}

				if (Global.Emulator is LibsnesCore)
				{
					_movieToRecord.Header[HeaderKeys.SGB] = (Global.Emulator as LibsnesCore).IsSGB.ToString();
					if ((Global.Emulator as LibsnesCore).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header[HeaderKeys.PAL] = "1";
					}
				}
				else if (Global.Emulator is SMS)
				{
					if ((Global.Emulator as SMS).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header[HeaderKeys.PAL] = "1";
					}
				}
				else if (Global.Emulator is NES)
				{
					if ((Global.Emulator as NES).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header[HeaderKeys.PAL] = "1";
					}
				}
				else if (Global.Emulator is ColecoVision)
				{
					var s = (ColecoVision.ColecoSyncSettings)Global.Emulator.GetSyncSettings();
					_movieToRecord.Header[HeaderKeys.SKIPBIOS] = s.SkipBiosIntro.ToString();
				}
				else if (Global.Emulator is N64)
				{
					if ((Global.Emulator as N64).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header[HeaderKeys.PAL] = "1";
					}
				}
				else if (Global.Emulator is GPGX)
				{
					if ((Global.Emulator as GPGX).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header[HeaderKeys.PAL] = "1";
					}
				}

				_movieToRecord.Header[HeaderKeys.CORE] = ((CoreAttributes)Attribute
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
					DefaultExt = "." + Global.Config.MovieExtension,
					FileName = RecordBox.Text,
					OverwritePrompt = false
				};
			var filter = "Movie Files (*." + Global.Config.MovieExtension + ")|*." + Global.Config.MovieExtension + "|Savestates|*.state|All Files|*.*";
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
			var name = PathManager.FilesystemSafeName(Global.Game);
			name = Path.GetFileNameWithoutExtension(name);
			RecordBox.Text = name;
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
