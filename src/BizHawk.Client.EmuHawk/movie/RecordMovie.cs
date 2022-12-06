using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	// TODO - Allow relative paths in record TextBox
	public sealed class RecordMovie : Form, IDialogParent
	{
		private const string START_FROM_POWERON = "Power-On";

		private const string START_FROM_SAVERAM = "SaveRam";

		private const string START_FROM_SAVESTATE = "Now";

		private readonly IMainFormForTools _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;
		private readonly FirmwareManager _firmwareManager;

		private readonly TextBox AuthorBox;

		private readonly CheckBox DefaultAuthorCheckBox;

		private readonly TextBox RecordBox;

		private readonly ComboBox StartFromCombo;

		public IDialogController DialogController => _mainForm;

		public RecordMovie(
			IMainFormForTools mainForm,
			Config config,
			GameInfo game,
			IEmulator core,
			IMovieSession movieSession,
			FirmwareManager firmwareManager)
		{
			if (game.IsNullInstance()) throw new InvalidOperationException("how is the traditional Record dialog open with no game loaded? please report this including as much detail as possible");

			_mainForm = mainForm;
			_config = config;
			_game = game;
			_emulator = core;
			_movieSession = movieSession;
			_firmwareManager = firmwareManager;

			Button Cancel = new();
			Button OK = new();
			Button BrowseBtn = new();
			RecordBox = new();
			StartFromCombo = new();
			GroupBox groupBox1 = new();
			DefaultAuthorCheckBox = new();
			AuthorBox = new();
			LocLabelEx label3 = new();
			LocLabelEx label2 = new();
			LocLabelEx label1 = new();
			groupBox1.SuspendLayout();
			SuspendLayout();

			Cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			Cancel.DialogResult = DialogResult.Cancel;
			Cancel.Location = new(391, 135);
			Cancel.Name = "Cancel";
			Cancel.Size = new(75, 23);
			Cancel.TabIndex = 1;
			Cancel.Text = "&Cancel";
			Cancel.UseVisualStyleBackColor = true;
			Cancel.Click += Cancel_Click;

			OK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
			OK.Location = new(310, 135);
			OK.Name = "OK";
			OK.Size = new(75, 23);
			OK.TabIndex = 0;
			OK.Text = "&OK";
			OK.UseVisualStyleBackColor = true;
			OK.Click += Ok_Click;

			BrowseBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			BrowseBtn.Image = Properties.Resources.OpenFile;
			BrowseBtn.Location = new(423, 13);
			BrowseBtn.Name = "BrowseBtn";
			BrowseBtn.Size = new(25, 23);
			BrowseBtn.TabIndex = 1;
			BrowseBtn.UseVisualStyleBackColor = true;
			BrowseBtn.Click += BrowseBtn_Click;

			RecordBox.AllowDrop = true;
			RecordBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			RecordBox.Location = new(83, 13);
			RecordBox.Name = "RecordBox";
			RecordBox.Size = new(334, 20);
			RecordBox.TabIndex = 0;
			RecordBox.DragDrop += RecordBox_DragDrop;
			RecordBox.DragEnter += RecordBox_DragEnter;

			StartFromCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			StartFromCombo.FormattingEnabled = true;
			StartFromCombo.Location = new(83, 65);
			StartFromCombo.MaxDropDownItems = 32;
			StartFromCombo.Name = "StartFromCombo";
			StartFromCombo.Size = new(152, 21);
			StartFromCombo.TabIndex = 3;
			StartFromCombo.Items.Add(START_FROM_POWERON);
			StartFromCombo.Items.Add(START_FROM_SAVESTATE);
			StartFromCombo.Items.Add(START_FROM_SAVERAM);

			groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			groupBox1.Location = new(12, 12);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new(454, 112);
			groupBox1.TabIndex = 0;
			groupBox1.TabStop = false;
			groupBox1.Controls.Add(DefaultAuthorCheckBox);
			groupBox1.Controls.Add(AuthorBox);
			groupBox1.Controls.Add(StartFromCombo);
			groupBox1.Controls.Add(BrowseBtn);
			groupBox1.Controls.Add(label3);
			groupBox1.Controls.Add(label2);
			groupBox1.Controls.Add(label1);
			groupBox1.Controls.Add(RecordBox);

			DefaultAuthorCheckBox.Anchor = AnchorStyles.Right;
			DefaultAuthorCheckBox.AutoSize = true;
			DefaultAuthorCheckBox.Location = new(327, 64);
			DefaultAuthorCheckBox.Name = "DefaultAuthorCheckBox";
			DefaultAuthorCheckBox.Size = new(121, 17);
			DefaultAuthorCheckBox.TabIndex = 6;
			DefaultAuthorCheckBox.Text = "Make default author";
			DefaultAuthorCheckBox.UseVisualStyleBackColor = true;

			AuthorBox.AllowDrop = true;
			AuthorBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			AuthorBox.Location = new(83, 39);
			AuthorBox.Name = "AuthorBox";
			AuthorBox.Size = new(365, 20);
			AuthorBox.TabIndex = 2;

			label3.Location = new(36, 41);
			label3.Name = "label3";
			label3.Text = "Author:";

			label2.Location = new(6, 68);
			label2.Name = "label2";
			label2.Text = "Record From:";

			label1.Location = new(51, 16);
			label1.Name = "label1";
			label1.Text = "File:";

			AcceptButton = OK;
			AutoScaleDimensions = new(6.0f, 13.0f);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = Cancel;
			ClientSize = new(478, 163);
			Icon = Properties.Resources.TAStudioIcon;
			MaximizeBox = false;
			MaximumSize = new(1440, 201);
			MinimizeBox = false;
			MinimumSize = new(425, 201);
			Name = "RecordMovie";
			StartPosition = FormStartPosition.CenterParent;
			Text = "Record Movie";
			Controls.Add(groupBox1);
			Controls.Add(OK);
			Controls.Add(Cancel);
			Load += RecordMovie_Load;
			if (OSTailoredCode.IsUnixHost) Load += (_, _) =>
			{
				//HACK to make this usable on Linux. No clue why this Form in particular is so much worse, maybe the GroupBox? --yoshi
				groupBox1.Height -= 24;
				DefaultAuthorCheckBox.Location += new Size(0, 32);
				var s = new Size(0, 40);
				OK.Location += s;
				Cancel.Location += s;
			};

			groupBox1.ResumeLayout(performLayout: false);
			groupBox1.PerformLayout();
			ResumeLayout(performLayout: false);

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
					var result = DialogController.ShowMessageBox2($"{path} already exists, overwrite?", "Confirm overwrite", EMsgBoxIcon.Warning, useOKCancel: true);
					if (!result)
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

				if (StartFromCombo.SelectedItem.ToString() is START_FROM_SAVESTATE && _emulator.HasSavestates())
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
					movieToRecord.SavestateFramebuffer = Array.Empty<int>();
					if (_emulator.HasVideoProvider())
					{
						movieToRecord.SavestateFramebuffer = (int[])_emulator.AsVideoProvider().GetVideoBuffer().Clone();
					}
				}
				else if (StartFromCombo.SelectedItem.ToString() is START_FROM_SAVERAM && _emulator.HasSaveRam())
				{
					var core = _emulator.AsSaveRam();
					movieToRecord.StartsFromSaveRam = true;
					movieToRecord.SaveRam = core.CloneSaveRam();
				}

				movieToRecord.PopulateWithDefaultHeaderValues(
					_emulator,
					((MainForm) _mainForm).GetSettingsAdapterForLoadedCoreUntyped(), //HACK
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
				DialogController.ShowMessageBox("Please select a movie to record", "File selection error", EMsgBoxIcon.Error);
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
				Directory.CreateDirectory(movieFolderPath);
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
			
			var filterset = _movieSession.Movie.GetFSFilterSet();
			var result = this.ShowFileSaveDialog(
				fileExt: $".{filterset.Filters[0].Extensions.First()}",
				filter: filterset,
				initDir: movieFolderPath,
				initFileName: RecordBox.Text,
				muteOverwriteWarning: true);
			if (!string.IsNullOrWhiteSpace(result)) RecordBox.Text = result;
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
