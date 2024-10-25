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
		private const string START_FROM_POWERON = "Power-on (clean)";

		private const string START_FROM_SAVERAM = "SaveRAM";

		private const string START_FROM_SAVESTATE = "SaveRAM + savestate";

		private readonly IMainFormForTools _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;

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
			IMovieSession movieSession)
		{
			if (game.IsNullInstance()) throw new InvalidOperationException("how is the traditional Record dialog open with no game loaded? please report this including as much detail as possible");

			_mainForm = mainForm;
			_config = config;
			_game = game;
			_emulator = core;
			_movieSession = movieSession;

			SuspendLayout();

			Button Cancel = new()
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				DialogResult = DialogResult.Cancel,
				Location = new(391, 135),
				Size = new(75, 23),
				Text = "&Cancel",
				UseVisualStyleBackColor = true,
			};
			Cancel.Click += Cancel_Click;

			Button OK = new()
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				Location = new(310, 135),
				Size = new(75, 23),
				Text = "&OK",
				UseVisualStyleBackColor = true,
			};
			OK.Click += Ok_Click;

			Button BrowseBtn = new()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Right,
				Image = Properties.Resources.OpenFile,
				Location = new(423, 13),
				Size = new(25, 23),
				UseVisualStyleBackColor = true,
			};
			BrowseBtn.Click += BrowseBtn_Click;

			RecordBox = new()
			{
				AllowDrop = true,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Location = new(83, 13),
				Size = new(334, 20),
			};
			RecordBox.DragDrop += RecordBox_DragDrop;
			RecordBox.DragEnter += RecordBox_DragEnter;

			StartFromCombo = new()
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				FormattingEnabled = true,
				Items = { START_FROM_POWERON },
				Location = new(83, 65),
				MaxDropDownItems = 32,
				Size = new(152, 21),
			};
			if (_emulator.HasSaveRam()) StartFromCombo.Items.Add(START_FROM_SAVERAM);
			if (_emulator.HasSavestates()) StartFromCombo.Items.Add(START_FROM_SAVESTATE);

			DefaultAuthorCheckBox = new()
			{
				Anchor = AnchorStyles.Right,
				AutoSize = true,
				Location = new(327, 64),
				Size = new(121, 17),
				Text = "Make default author",
				UseVisualStyleBackColor = true,
			};

			AuthorBox = new()
			{
				AllowDrop = true,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Location = new(83, 39),
				Size = new(365, 20),
			};

			GroupBox groupBox1 = new()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Location = new(12, 12),
				Size = new(454, 112),
			};
			groupBox1.SuspendLayout();
			groupBox1.Controls.Add(new LocLabelEx { Location = new(51, 16), Text = "File:" });
			groupBox1.Controls.Add(RecordBox);
			groupBox1.Controls.Add(BrowseBtn);
			groupBox1.Controls.Add(new LocLabelEx { Location = new(36, 41), Text = "Author:" });
			groupBox1.Controls.Add(AuthorBox);
			groupBox1.Controls.Add(new LocLabelEx { Location = new(6, 68), Text = "Record From:" });
			groupBox1.Controls.Add(StartFromCombo);
			groupBox1.Controls.Add(DefaultAuthorCheckBox);

			AcceptButton = OK;
			AutoScaleDimensions = new(6.0f, 13.0f);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = Cancel;
			ClientSize = new(478, 163);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			Icon = Properties.Resources.TAStudioIcon;
			MaximizeBox = false;
			MinimizeBox = false;
			StartPosition = FormStartPosition.CenterParent;
			Text = "Record Movie";
			Controls.Add(new FlowLayoutPanel
			{
				Controls =
				{
					groupBox1,
					new SingleRowFLP
					{
						Controls = { OK, Cancel },
					},
				},
				FlowDirection = FlowDirection.RightToLeft, // going for two rows so the buttons are right-aligned
				Margin = Padding.Empty,
				Size = new(464, 144),
			});
			Load += RecordMovie_Load;
			if (OSTailoredCode.IsUnixHost) Load += (_, _) =>
			{
				//HACK to make this usable on Linux. No clue why this Form in particular is so much worse, maybe the GroupBox? --yoshi
				groupBox1.Height -= 24;
				DefaultAuthorCheckBox.Location += new Size(0, 32);
			};

			groupBox1.ResumeLayout(performLayout: false);
			groupBox1.PerformLayout();
			ResumeLayout(performLayout: false);
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
				movieToRecord.Author = AuthorBox.Text ?? _config.DefaultAuthor;

				var fileInfo = new FileInfo(path);
				if (!fileInfo.Exists)
				{
					Directory.CreateDirectory(fileInfo.DirectoryName);
				}

				var selectedStartFromValue = StartFromCombo.SelectedItem.ToString();
				if (selectedStartFromValue is START_FROM_SAVESTATE && _emulator.HasSavestates())
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
						movieToRecord.SavestateFramebuffer = _emulator.AsVideoProvider().GetVideoBufferCopy();
					}
				}
				else if (selectedStartFromValue is START_FROM_SAVERAM && _emulator.HasSaveRam())
				{
					var core = _emulator.AsSaveRam();
					movieToRecord.StartsFromSaveRam = true;
					movieToRecord.SaveRam = core.CloneSaveRam();
				}

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
