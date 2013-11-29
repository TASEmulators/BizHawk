using System;
using System.Windows.Forms;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.ColecoVision;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.Nintendo.N64;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RecordMovie : Form
	{
		//TODO
		//Allow relative paths in record textbox

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
				path = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPath, null) + path;

				if (path[path.Length - 4] != '.') //If no file extension, add movie extension
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

		private void OK_Click(object sender, EventArgs e)
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
					_movieToRecord = new Movie(path, startsFromSavestate: true);
					var temppath = path;
					var writer = new StreamWriter(temppath);
					Global.Emulator.SaveStateText(writer);
					writer.Close();

					var file = new FileInfo(temppath);
					using (var sr = file.OpenText())
					{
						string str;
						while ((str = sr.ReadLine()) != null)
						{
							if (!String.IsNullOrWhiteSpace(str))
							{
								_movieToRecord.Header.Comments.Add(str);
							}
						}
					}
				}
				else
				{
					_movieToRecord = new Movie(path);
				}

				//Header
				_movieToRecord.Header.SetHeaderLine(MovieHeader.AUTHOR, AuthorBox.Text);
				_movieToRecord.Header.SetHeaderLine(MovieHeader.EMULATIONVERSION, VersionInfo.GetEmuVersion());
				_movieToRecord.Header.SetHeaderLine(MovieHeader.MOVIEVERSION, MovieHeader.MovieVersion);
				_movieToRecord.Header.SetHeaderLine(MovieHeader.GUID, MovieHeader.MakeGUID());
				_movieToRecord.Header.SetHeaderLine(MovieHeader.PLATFORM, Global.Game.System);
				if (Global.Game != null)
				{
					_movieToRecord.Header.SetHeaderLine(MovieHeader.GAMENAME, PathManager.FilesystemSafeName(Global.Game));
					_movieToRecord.Header.SetHeaderLine(MovieHeader.SHA1, Global.Game.Hash);
					if (Global.Game.FirmwareHash != null)
					{
						_movieToRecord.Header.SetHeaderLine(MovieHeader.FIRMWARESHA1, Global.Game.FirmwareHash);
					}
				}
				else
				{
					_movieToRecord.Header.SetHeaderLine(MovieHeader.GAMENAME, "NULL");
				}

				if (Global.Emulator.BoardName != null)
				{
					_movieToRecord.Header.SetHeaderLine(MovieHeader.BOARDNAME, Global.Emulator.BoardName);
				}

				if (Global.Emulator is Gameboy)
				{
					_movieToRecord.Header.SetHeaderLine(MovieHeader.GB_FORCEDMG, Global.Config.GB_ForceDMG.ToString());
					_movieToRecord.Header.SetHeaderLine(MovieHeader.GB_GBA_IN_CGB, Global.Config.GB_GBACGB.ToString());
				}

				if (Global.Emulator is LibsnesCore)
				{
					_movieToRecord.Header.SetHeaderLine(MovieHeader.SGB, ((Global.Emulator) as LibsnesCore).IsSGB.ToString());
					if ((Global.Emulator as LibsnesCore).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header.SetHeaderLine(MovieHeader.PAL, "1");
					}
				}
				else if (Global.Emulator is SMS)
				{
					if ((Global.Emulator as SMS).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header.SetHeaderLine(MovieHeader.PAL, "1");
					}
				}
				else if (Global.Emulator is NES)
				{
					if ((Global.Emulator as NES).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header.SetHeaderLine(MovieHeader.PAL, "1");
					}
				}
				else if (Global.Emulator is ColecoVision)
				{
					_movieToRecord.Header.SetHeaderLine(MovieHeader.SKIPBIOS, Global.Config.ColecoSkipBiosIntro.ToString());
				}

				else if (Global.Emulator is N64)
				{
					_movieToRecord.Header.SetHeaderLine(MovieHeader.VIDEOPLUGIN, Global.Config.N64VidPlugin);

					if (Global.Config.N64VidPlugin == "Rice")
					{
						var rice_settings = Global.Config.RicePlugin.GetPluginSettings();
						foreach(var setting in rice_settings)
						{
							_movieToRecord.Header.SetHeaderLine(setting.Key, setting.Value.ToString());
						}
					}
					else if (Global.Config.N64VidPlugin == "Glide64")
					{
						var glide_settings = Global.Config.GlidePlugin.GetPluginSettings();
						foreach (var setting in glide_settings)
						{
							_movieToRecord.Header.SetHeaderLine(setting.Key, setting.Value.ToString());
						}
					}

					if ((Global.Emulator as N64).DisplayType == DisplayType.PAL)
					{
						_movieToRecord.Header.SetHeaderLine(MovieHeader.PAL, "1");
					}
				}

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

		private void button1_Click(object sender, EventArgs e)
		{
			var filename = String.Empty;
			var sfd = new SaveFileDialog
				{
					InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.PathEntries.MoviesPath, null),
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
				RecordBox.Text = filename;
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
