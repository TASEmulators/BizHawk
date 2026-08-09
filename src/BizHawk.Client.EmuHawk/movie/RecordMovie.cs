using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Bizware.Graphics;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public sealed class RecordMovie : Form, IDialogParent
	{
		private const string START_FROM_POWERON = "Power-on (clean)";

		private const string START_FROM_SAVERAM = "SaveRAM";

		private const string START_FROM_SAVESTATE = "Savestate";

		private readonly IMainFormForTools _mainForm;
		private readonly Config _config;
		private readonly GameInfo _game;
		private readonly IEmulator _emulator;
		private readonly IMovieSession _movieSession;

		private readonly TextBox AuthorBox;

		private readonly CheckBox DefaultAuthorCheckBox;

		private readonly TextBox RecordBox;

		private readonly ComboBox StartFromCombo;

		private readonly TextBox SramBox;
		private readonly CheckBox SramCheckbox;
		private readonly Panel SramPanel;

		private readonly TextBox SavestateBox;
		private readonly CheckBox SavestateCheckbox;
		private readonly Panel SavestatePanel;

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
				Location = new(391, 145),
				Size = new(75, 23),
				Text = "&Cancel",
				UseVisualStyleBackColor = true,
			};
			Cancel.Click += Cancel_Click;

			Button OK = new()
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				Location = new(310, 145),
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
			if (_emulator.HasSaveRam() && _emulator.AsSaveRam().CloneSaveRam(clearDirty: false) is not null) StartFromCombo.Items.Add(START_FROM_SAVERAM);
			if (_emulator.HasSavestates()) StartFromCombo.Items.Add(START_FROM_SAVESTATE);
			StartFromCombo.SelectedIndexChanged += StartFromCombo_SelectedIndexChanged;

			SramBox = new()
			{
				AllowDrop = true,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Enabled = false,
				Location = new(133, 2),
				Size = new(274, 20),
			};
			SramCheckbox = new()
			{
				AutoSize = true,
				Checked = true,
				Location = new(73, 4),
				Text = "current",
			};
			SramCheckbox.CheckedChanged += (s, e) => SramBox.Enabled = !SramCheckbox.Checked;
			Button SramBrowseBtn = new()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Right,
				Image = Properties.Resources.OpenFile,
				Location = new(413, 2),
				Size = new(25, 23),
				UseVisualStyleBackColor = true,
			};
			SramBrowseBtn.Click += BrowseSramBtn_Click;
			LocLabelEx sramLabel = new() { Location = new(25, 4), Text = "SRAM:" };
			SramPanel = new()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Location = new(10, 89),
				Size = new(438, 30),
				BorderStyle = BorderStyle.None,
				Visible = false,
			};
			SramPanel.Controls.Add(sramLabel);
			SramPanel.Controls.Add(SramCheckbox);
			SramPanel.Controls.Add(SramBox);
			SramPanel.Controls.Add(SramBrowseBtn);

			SavestateBox = new()
			{
				AllowDrop = true,
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Enabled = false,
				Location = new(133, 2),
				Size = new(274, 20),
			};
			SavestateCheckbox = new()
			{
				AutoSize = true,
				Checked = true,
				Location = new(73, 4),
				Text = "current",
			};
			SavestateCheckbox.CheckedChanged += (s, e) => SavestateBox.Enabled = !SavestateCheckbox.Checked;
			Button SavestateBrowseBtn = new()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Right,
				Image = Properties.Resources.OpenFile,
				Location = new(413, 2),
				Size = new(25, 23),
				UseVisualStyleBackColor = true,
			};
			SavestateBrowseBtn.Click += BrowseSavestateBtn_Click;
			LocLabelEx savestateLabel = new() { Location = new(9, 4), Text = "Savestate:" };
			SavestatePanel = new()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
				Location = new(10, 89),
				Size = new(438, 30),
				BorderStyle = BorderStyle.None,
				Visible = false,
			};
			SavestatePanel.Controls.Add(savestateLabel);
			SavestatePanel.Controls.Add(SavestateCheckbox);
			SavestatePanel.Controls.Add(SavestateBox);
			SavestatePanel.Controls.Add(SavestateBrowseBtn);

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
				Size = new(454, 122),
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
			groupBox1.Controls.Add(SramPanel);
			groupBox1.Controls.Add(SavestatePanel);

			AcceptButton = OK;
			AutoScaleDimensions = new(6.0f, 13.0f);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = Cancel;
			ClientSize = new(478, 173);
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
				Size = new(464, 154),
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

		private void StartFromCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			SramPanel.Visible = (StartFromCombo.SelectedItem as string) == START_FROM_SAVERAM;
			SavestatePanel.Visible = (StartFromCombo.SelectedItem as string) == START_FROM_SAVESTATE;
		}

		private string MakePath()
		{
			var path = RecordBox.Text;

			if (!string.IsNullOrWhiteSpace(path))
			{
				path = Path.IsPathRooted(path)
					? Path.GetFullPath(path)
					: Path.Combine(_config.PathEntries.MovieAbsolutePath(), path);

				if (!MovieService.MovieExtensions.Select(static ext => $".{ext}").Contains(Path.GetExtension(path)))
				{
					// If no valid movie extension, add movie extension
					path += $".{MovieService.StandardMovieExtension}";
				}
			}

			return path;
		}

		private void Ok_Click(object sender, EventArgs e)
		{
			string selectedStartFromValue = StartFromCombo.SelectedItem.ToString();
			if (selectedStartFromValue == START_FROM_SAVESTATE && !SavestateCheckbox.Checked && !File.Exists(SavestateBox.Text))
			{
				DialogController.ShowMessageBox2($"Savestate file {SavestateBox.Text} does not exist.");
				return;
			}
			if (selectedStartFromValue == START_FROM_SAVERAM && !SramCheckbox.Checked && !File.Exists(SramBox.Text))
			{
				DialogController.ShowMessageBox2($"SaveRam file {SramBox.Text} does not exist.");
				return;
			}

			var path = MakePath();
			if (!string.IsNullOrWhiteSpace(path))
			{
				if (File.Exists(path))
				{
					var result = DialogController.ShowMessageBox2($"{path} already exists, overwrite?", "Confirm overwrite", EMsgBoxIcon.Warning, useOKCancel: true);
					if (!result)
					{
						return;
					}
				}

				var movieToRecord = _movieSession.Get(path);
				movieToRecord.Author = AuthorBox.Text ?? _config.DefaultAuthor;

				if (selectedStartFromValue is START_FROM_SAVESTATE && _emulator.HasSavestates())
				{
					movieToRecord.StartsFromSavestate = true;
					if (SavestateCheckbox.Checked)
					{
						IStatable core = _emulator.AsStatable();
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

						if (_emulator.HasVideoProvider())
						{
							var v = _emulator.AsVideoProvider();
							movieToRecord.SavestateFramebuffer = new BitmapBuffer(v.BufferWidth, v.BufferHeight, v.GetVideoBuffer());
						}
					}
					else
					{
						SavestateFile file = new(_emulator, _movieSession, new Dictionary<string, object>());
						Savestate state = file.GetSavestate(SavestateBox.Text, this);
						if (state == null) return; // error message will have already been displayed
						movieToRecord.BinarySavestate = state.coreData;
						movieToRecord.SavestateFramebuffer = state.screenshot;
					}
				}
				else if (selectedStartFromValue is START_FROM_SAVERAM && _emulator.HasSaveRam())
				{
					var core = SramCheckbox.Checked ? _emulator.AsSaveRam() : new SaveRamByFile(SramBox.Text);
					movieToRecord.StartsFromSaveRam = true;
					movieToRecord.SaveRam = core.CloneSaveRam(clearDirty: false);
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
			catch (IOException)
			{
				// ignored
				//TODO present to user?
			}
			catch (UnauthorizedAccessException)
			{
				// ignored
				//TODO present to user?
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

		private void BrowseSramBtn_Click(object sender, EventArgs e)
		{
			string sramFolderPath = _config.PathEntries.SaveRamAbsolutePath(_game.System);

			// Create folder if it doesn't already exist
			try
			{
				Directory.CreateDirectory(sramFolderPath);
			}
			catch (IOException) { /* ignored */ }
			catch (UnauthorizedAccessException) { /* ignored */ }

			var filterset = FilesystemFilterSet.SaveRams;
			var result = this.ShowFileSaveDialog(
				fileExt: $".{filterset.Filters[0].Extensions.First()}",
				filter: filterset,
				initDir: sramFolderPath,
				initFileName: SramBox.Text,
				muteOverwriteWarning: true);
			if (!string.IsNullOrWhiteSpace(result))
			{
				SramBox.Text = result;
				SramCheckbox.Checked = false;
			}
		}

		private void BrowseSavestateBtn_Click(object sender, EventArgs e)
		{
			string stateFolderPath = _config.PathEntries.SaveStateAbsolutePath(_game.System);

			// Create folder if it doesn't already exist
			try
			{
				Directory.CreateDirectory(stateFolderPath);
			}
			catch (IOException) { /* ignored */ }
			catch (UnauthorizedAccessException) { /* ignored */ }

			var filterset = FilesystemFilterSet.Savetates;
			var result = this.ShowFileSaveDialog(
				fileExt: $".{filterset.Filters[0].Extensions.First()}",
				filter: filterset,
				initDir: stateFolderPath,
				initFileName: SavestateBox.Text,
				muteOverwriteWarning: true);
			if (!string.IsNullOrWhiteSpace(result))
			{
				SavestateBox.Text = result;
				SavestateCheckbox.Checked = false;
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
