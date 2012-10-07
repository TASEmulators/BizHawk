using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

using BizHawk.Emulation.Consoles.GB;
using BizHawk.Emulation.Consoles.Nintendo.SNES;
using BizHawk.Emulation.Consoles.Sega;
using BizHawk.Emulation.Consoles.Nintendo;

namespace BizHawk.MultiClient
{
	public partial class RecordMovie : Form
	{
		//TODO
		//Allow relative paths in record textbox

		Movie MovieToRecord;

		public RecordMovie()
		{
			InitializeComponent();
		}

		private string MakePath()
		{
			if (RecordBox.Text.Length == 0)
				return "";
			string path = RecordBox.Text;
			int x = path.LastIndexOf(Path.DirectorySeparatorChar);
			if (path.LastIndexOf(Path.DirectorySeparatorChar) == -1)
			{
				if (path[0] != Path.DirectorySeparatorChar)
					path = path.Insert(0, ""+Path.DirectorySeparatorChar);
				path = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "") + path;

				if (path[path.Length - 4] != '.') //If no file extension, add movie extension
					path += "." + Global.Config.MovieExtension;
				return path;
			}
			else
				return path;
		}

		private void OK_Click(object sender, EventArgs e)
		{
			string path = MakePath();

			if (path.Length > 0)
			{
				FileInfo test = new FileInfo(path);
				if (test.Exists)
				{
					var result = MessageBox.Show(path + " already exists, overwrite?", "Confirm overwrite", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
					if (result == System.Windows.Forms.DialogResult.Cancel)
						return;
				}


				MovieToRecord = new Movie(path);

				//Header
				MovieToRecord.Header.SetHeaderLine(MovieHeader.AUTHOR, AuthorBox.Text);
				MovieToRecord.Header.SetHeaderLine(MovieHeader.EMULATIONVERSION, MainForm.EMUVERSION);
				MovieToRecord.Header.SetHeaderLine(MovieHeader.MOVIEVERSION, MovieHeader.MovieVersion);
				MovieToRecord.Header.SetHeaderLine(MovieHeader.GUID, MovieHeader.MakeGUID());
				MovieToRecord.Header.SetHeaderLine(MovieHeader.PLATFORM, Global.Game.System);
				if (Global.Game != null)
				{
					MovieToRecord.Header.SetHeaderLine(MovieHeader.GAMENAME, PathManager.FilesystemSafeName(Global.Game));
					MovieToRecord.Header.SetHeaderLine(MovieHeader.SHA1, Global.Game.Hash);
					if (Global.Game.FirmwareHash != null)
						MovieToRecord.Header.SetHeaderLine(MovieHeader.FIRMWARESHA1, Global.Game.FirmwareHash);
				}
				else
				{
					MovieToRecord.Header.SetHeaderLine(MovieHeader.GAMENAME, "NULL");
				}

				if (Global.Emulator is Gameboy)
				{
					MovieToRecord.Header.SetHeaderLine(MovieHeader.GB_FORCEDMG, Global.Config.GB_ForceDMG.ToString());
					MovieToRecord.Header.SetHeaderLine(MovieHeader.GB_GBA_IN_CGB, Global.Config.GB_GBACGB.ToString());
				}

				if (Global.Emulator is LibsnesCore)
				{
					MovieToRecord.Header.SetHeaderLine(MovieHeader.SGB, ((Global.Emulator) as LibsnesCore).IsSGB.ToString());
					if ((Global.Emulator as LibsnesCore).DisplayType == DisplayType.PAL)
					{
						MovieToRecord.Header.SetHeaderLine(MovieHeader.PAL, "1");
					}
				}
				else if (Global.Emulator is SMS)
				{
					if ((Global.Emulator as SMS).DisplayType == DisplayType.PAL)
					{
						MovieToRecord.Header.SetHeaderLine(MovieHeader.PAL, "1");
					}
				}
				else if (Global.Emulator is NES)
				{
					if ((Global.Emulator as NES).DisplayType == DisplayType.PAL)
					{
						MovieToRecord.Header.SetHeaderLine(MovieHeader.PAL, "1");
					}
				}


				if (StartFromCombo.SelectedItem.ToString() == "Now")
				{
					MovieToRecord.StartsFromSavestate = true;
					var temppath = path + ".tmp";
					var writer = new StreamWriter(temppath);
					Global.Emulator.SaveStateText(writer);
					writer.Close();

					var file = new FileInfo(temppath);
					using (StreamReader sr = file.OpenText())
					{
						string str = "";

						while ((str = sr.ReadLine()) != null)
						{
							if (str == "")
							{
								continue;
							}
							else
								MovieToRecord.Header.Comments.Add(str);
						}
					}
					file.Delete();
				}
				Global.MainForm.StartNewMovie(MovieToRecord, true);

				Global.Config.UseDefaultAuthor = DefaultAuthorCheckBox.Checked;
				if (DefaultAuthorCheckBox.Checked)
					Global.Config.DefaultAuthor = AuthorBox.Text;
				this.Close();
			}
			else
				MessageBox.Show("Please select a movie to record", "File selection error", MessageBoxButtons.OK, MessageBoxIcon.Error);

		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			string filename = "";
			SaveFileDialog sfd = new SaveFileDialog();
			sfd.InitialDirectory = PathManager.MakeAbsolutePath(Global.Config.MoviesPath, "");
			sfd.DefaultExt = "." + Global.Config.MovieExtension;
			sfd.FileName = RecordBox.Text;
			sfd.OverwritePrompt = false;
			sfd.Filter = "Generic Movie Files (*." + Global.Config.MovieExtension + ")|*." + Global.Config.MovieExtension + "|" + Global.MainForm.GetMovieExtName() + "|All Files (*.*)|*.*";

			Global.Sound.StopSound();
			var result = sfd.ShowDialog();
			Global.Sound.StartSound();
			if (result == DialogResult.OK)
			{
				filename = sfd.FileName;
			}

			if ("" != filename)
			{
				RecordBox.Text = filename;
			}
		}

		private void RecordMovie_Load(object sender, EventArgs e)
		{
			RecordBox.Text = PathManager.FilesystemSafeName(Global.Game);
			StartFromCombo.SelectedIndex = 0;
			DefaultAuthorCheckBox.Checked = Global.Config.UseDefaultAuthor;
			if (Global.Config.UseDefaultAuthor)
				AuthorBox.Text = Global.Config.DefaultAuthor;
		}

		private void RecordBox_DragEnter(object sender, DragEventArgs e)
		{
			e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None; string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
		}

		private void RecordBox_DragDrop(object sender, DragEventArgs e)
		{
			string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
			RecordBox.Text = filePaths[0];
		}
	}
}
