using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.DiscoHawk
{
	public class MainDiscoForm : Form
	{
		private readonly MultiMessageContext _i18n;

		private readonly RadioButton ccdOutputButton;

		private readonly RadioButton chdOutputButton;

		private readonly Panel lblMagicDragArea;

		private readonly Panel lblMp3ExtractMagicArea;

		private readonly ListView lvCompareTargets;

		// Release TODO:
		// An input (queue) list
		// An outputted list showing new file name
		// Progress bar should show file being converted
		// Add disc button, which puts it on the progress cue (converts it)
		public MainDiscoForm(MultiMessageContext i18n)
		{
			_i18n = i18n;

			SuspendLayout();

			Button ExitButton = new();
			ExitButton.Location = new(434, 414);
			ExitButton.Name = "ExitButton";
			ExitButton.Size = new(75, 23);
			ExitButton.TabIndex = 0;
			ExitButton.Text = i18n.GetWithMnemonic("maindiscoform-4723-btn-exit");
			ExitButton.UseVisualStyleBackColor = true;
			ExitButton.Click += (_, _) => Close();

			lblMagicDragArea = new();
			lblMagicDragArea.SuspendLayout();
			Label label1 = new();
			lblMagicDragArea.AllowDrop = true;
			lblMagicDragArea.BorderStyle = BorderStyle.Fixed3D;
			lblMagicDragArea.Controls.Add(label1);
			lblMagicDragArea.Location = new(290, 31);
			lblMagicDragArea.Name = "lblMagicDragArea";
			lblMagicDragArea.Size = new(223, 109);
			lblMagicDragArea.TabIndex = 1;
			lblMagicDragArea.DragDrop += lblMagicDragArea_DragDrop;
			lblMagicDragArea.DragEnter += LblMagicDragArea_DragEnter;

			label1.Location = new(17, 25);
			label1.Name = "label1";
			label1.Size = new(166, 47);
			label1.TabIndex = 0;
			label1.Text = i18n["maindiscoform-1872-area-hawkdisc"];

			lblMp3ExtractMagicArea = new();
			lblMp3ExtractMagicArea.SuspendLayout();
			Label label2 = new();
			lblMp3ExtractMagicArea.AllowDrop = true;
			lblMp3ExtractMagicArea.BorderStyle = BorderStyle.Fixed3D;
			lblMp3ExtractMagicArea.Controls.Add(label2);
			lblMp3ExtractMagicArea.Location = new(290, 146);
			lblMp3ExtractMagicArea.Name = "lblMp3ExtractMagicArea";
			lblMp3ExtractMagicArea.Size = new(223, 100);
			lblMp3ExtractMagicArea.TabIndex = 2;
			lblMp3ExtractMagicArea.DragDrop += LblMp3ExtractMagicArea_DragDrop;
			lblMp3ExtractMagicArea.DragEnter += LblMagicDragArea_DragEnter;

			label2.Location = new(20, 25);
			label2.Name = "label2";
			label2.Size = new(163, 39);
			label2.TabIndex = 0;
			label2.Text = i18n["maindiscoform-6011-area-mp3extract"];

			Button btnAbout = new();
			btnAbout.Location = new(353, 414);
			btnAbout.Name = "btnAbout";
			btnAbout.Size = new(75, 23);
			btnAbout.TabIndex = 3;
			btnAbout.Text = i18n.GetWithMnemonic("maindiscoform-5766-btn-about");
			btnAbout.UseVisualStyleBackColor = true;
			btnAbout.Click += (_, _) => new About(_i18n).ShowDialog();

			RadioButton radioButton1 = new();
			radioButton1.AutoSize = true;
			radioButton1.Checked = true;
			radioButton1.Location = new(6, 19);
			radioButton1.Name = "radioButton1";
			radioButton1.Size = new(67, 17);
			radioButton1.TabIndex = 4;
			radioButton1.TabStop = true;
			radioButton1.Text = i18n.GetWithMnemonic("maindiscoform-4559-radio-engine-hawk");
			radioButton1.UseVisualStyleBackColor = true;

			GroupBox groupBox1 = new();
			groupBox1.SuspendLayout();
			Label label4 = new();
			Label label3 = new();
			RadioButton radioButton2 = new();
			groupBox1.Controls.Add(label4);
			groupBox1.Controls.Add(label3);
			groupBox1.Controls.Add(radioButton2);
			groupBox1.Controls.Add(radioButton1);
			groupBox1.Enabled = true;
			groupBox1.Location = new(9, 12);
			groupBox1.Name = "groupBox1";
			groupBox1.Size = new(276, 234);
			groupBox1.TabIndex = 5;
			groupBox1.TabStop = false;
			groupBox1.Text = i18n["maindiscoform-7187-group-engine"];

			label4.Location = new(20, 95);
			label4.Name = "label4";
			label4.Size = new(216, 43);
			label4.TabIndex = 8;
			label4.Text = i18n["maindiscoform-7205-radio-engine-mednafen-longdesc"];

			label3.Location = new(20, 39);
			label3.Name = "label3";
			label3.Size = new(253, 33);
			label3.TabIndex = 7;
			label3.Text = i18n["maindiscoform-4559-radio-engine-hawk-longdesc"];

			radioButton2.AutoSize = true;
			radioButton2.Enabled = false;
			radioButton2.Location = new(6, 75);
			radioButton2.Name = "radioButton2";
			radioButton2.Size = new(73, 17);
			radioButton2.TabIndex = 5;
			radioButton2.Text = i18n.GetWithMnemonic("maindiscoform-7205-radio-engine-mednafen");
			radioButton2.UseVisualStyleBackColor = true;

			GroupBox groupBox2 = new();
			groupBox2.SuspendLayout();
			ccdOutputButton = new();
			chdOutputButton = new();
			groupBox2.Controls.Add(ccdOutputButton);
			groupBox2.Controls.Add(chdOutputButton);
			groupBox2.Enabled = true;
			groupBox2.Location = new(9, 252);
			groupBox2.Name = "groupBox2";
			groupBox2.Size = new(271, 69);
			groupBox2.TabIndex = 6;
			groupBox2.TabStop = false;
			groupBox2.Text = i18n["maindiscoform-5561-group-hawkoutput"];

			ccdOutputButton.AutoSize = true;
			ccdOutputButton.Checked = true;
			ccdOutputButton.Location = new(12, 19);
			ccdOutputButton.Name = "ccdOutputButton";
			ccdOutputButton.Size = new(47, 17);
			ccdOutputButton.TabIndex = 5;
			ccdOutputButton.TabStop = true;
			ccdOutputButton.Text = i18n.GetWithMnemonic("maindiscoform-7576-radio-hawkoutput-ccd");
			ccdOutputButton.UseVisualStyleBackColor = true;

			chdOutputButton.AutoSize = true;
			chdOutputButton.Checked = false;
			chdOutputButton.Location = new(65, 19);
			chdOutputButton.Name = "chdOutputButton";
			chdOutputButton.Size = new(47, 17);
			chdOutputButton.TabIndex = 6;
			chdOutputButton.TabStop = true;
			chdOutputButton.Text = i18n.GetWithMnemonic("maindiscoform-2884-radio-hawkoutput-chd");
			chdOutputButton.UseVisualStyleBackColor = true;

			Label label6 = new();
			label6.AutoSize = true;
			label6.Enabled = false;
			label6.Location = new(9, 324);
			label6.Name = "label6";
			label6.Size = new(111, 13);
			label6.TabIndex = 2;
			label6.Text = i18n["maindiscoform-4639-group-compare-list"];

			Label label7 = new();
			label7.AutoSize = true;
			label7.Location = new(358, 12);
			label7.Name = "label7";
			label7.Size = new(70, 13);
			label7.TabIndex = 10;
			label7.Text = i18n["maindiscoform-7426-pane-operations"];

			lvCompareTargets = new();
			lvCompareTargets.Columns.Add(new ColumnHeader());
			lvCompareTargets.Enabled = false;
			lvCompareTargets.FullRowSelect = true;
			lvCompareTargets.GridLines = true;
			lvCompareTargets.HeaderStyle = ColumnHeaderStyle.None;
			lvCompareTargets.HideSelection = false;
			lvCompareTargets.Items.Add(i18n["maindiscoform-5267-compare-hawk"]);
			lvCompareTargets.Items.Add(i18n["maindiscoform-5267-compare-mednafen"]);
			lvCompareTargets.Location = new(9, 340);
			lvCompareTargets.Name = "lvCompareTargets";
			lvCompareTargets.Size = new(121, 97);
			lvCompareTargets.TabIndex = 11;
			lvCompareTargets.UseCompatibleStateImageBehavior = false;
			lvCompareTargets.View = View.Details;

			AutoScaleDimensions = new(6.0f, 13.0f);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new(521, 449);
			Controls.Add(lvCompareTargets);
			Controls.Add(label6);
			Controls.Add(label7);
			Controls.Add(groupBox2);
			Controls.Add(groupBox1);
			Controls.Add(btnAbout);
			Controls.Add(lblMp3ExtractMagicArea);
			Controls.Add(lblMagicDragArea);
			Controls.Add(ExitButton);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			var icoStream = typeof(MainDiscoForm).Assembly.GetManifestResourceStream("BizHawk.Client.DiscoHawk.discohawk.ico");
			if (icoStream != null) Icon = new Icon(icoStream);
			else Console.WriteLine("couldn't load .ico EmbeddedResource?");
			MaximizeBox = false;
			MinimizeBox = false;
			Name = "MainDiscoForm";
			Text = i18n["maindiscoform-3997-windowtitlestatic"];
			Load += (_, _) => lvCompareTargets.Columns[0].Width = lvCompareTargets.ClientSize.Width;
			lblMagicDragArea.ResumeLayout(performLayout: false);
			lblMp3ExtractMagicArea.ResumeLayout(performLayout: false);
			groupBox1.ResumeLayout(performLayout: false);
			groupBox1.PerformLayout();
			groupBox2.ResumeLayout(performLayout: false);
			groupBox2.PerformLayout();
			ResumeLayout(performLayout: false);
			PerformLayout();
		}

		private void lblMagicDragArea_DragDrop(object sender, DragEventArgs e)
		{
			lblMagicDragArea.AllowDrop = false;
			Cursor = Cursors.WaitCursor;
			var outputFormat = DiscoHawkLogic.HawkedFormats.CCD;
			if (ccdOutputButton.Checked) outputFormat = DiscoHawkLogic.HawkedFormats.CCD;
			if (chdOutputButton.Checked) outputFormat = DiscoHawkLogic.HawkedFormats.CHD;
			try
			{
				foreach (var file in ValidateDrop(e.Data))
				{
					var success = DiscoHawkLogic.HawkAndWriteFile(
						inputPath: file,
						errorCallback: err => MessageBox.Show(
							caption: _i18n.GetDialogText("discodischawking-6945-errbox-hawk").Caption,
							text: err),
						hawkedFormat: outputFormat);
					if (!success) break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					caption: _i18n.GetDialogText("discodischawking-3654-errbox-misc").Caption,
					text: ex.ToString());
				throw;
			}
			finally
			{
				lblMagicDragArea.AllowDrop = true;
				Cursor = Cursors.Default;
			}
		}

#if false // API has changed
		bool Dump(CueBin cueBin, string directoryTo, CueBinPrefs prefs)
		{
			ProgressReport pr = new ProgressReport();
			Thread workThread = new Thread(() =>
			{
				cueBin.Dump(directoryTo, prefs, pr);
			});

			ProgressDialog pd = new ProgressDialog(pr);
			pd.Show(this);
			this.Enabled = false;
			workThread.Start();
			for (; ; )
			{
				Application.DoEvents();
				Thread.Sleep(10);
				if (workThread.ThreadState != ThreadState.Running)
					break;
				pd.Update();
			}
			this.Enabled = true;
			pd.Dispose();
			return !pr.CancelSignal;
		}
#endif

		private void LblMagicDragArea_DragEnter(object sender, DragEventArgs e)
		{
			var files = ValidateDrop(e.Data);
			e.Effect = files.Count > 0
				? DragDropEffects.Link
				: DragDropEffects.None;
		}

		private static List<string> ValidateDrop(IDataObject ido)
		{
			var ret = new List<string>();
			var files = (string[])ido.GetData(DataFormats.FileDrop);
			if (files == null) return new();
			foreach (var str in files)
			{
				var ext = Path.GetExtension(str) ?? string.Empty;
				if (!Disc.IsValidExtension(ext))
				{
					return new();
				}

				ret.Add(str);
			}

			return ret;
		}

		private void LblMp3ExtractMagicArea_DragDrop(object sender, DragEventArgs e)
		{
			if (!FFmpegService.QueryServiceAvailable())
			{
#if true
				var (caption, text) = _i18n.GetDialogText("discomp3extract-5715-errbox-noffmpeg");
				MessageBox.Show(caption: caption, text: text);
				return;
#else
				using EmuHawk.FFmpegDownloaderForm dialog = new(); // builds fine when <Compile Include/>'d, but the .resx won't load even if it's also included
				dialog.ShowDialog(owner: this);
				if (!FFmpegService.QueryServiceAvailable()) return;
#endif
			}
			lblMp3ExtractMagicArea.AllowDrop = false;
			Cursor = Cursors.WaitCursor;
			try
			{
				var files = ValidateDrop(e.Data);
				if (files.Count == 0) return;
				foreach (var file in files)
				{
					using var disc = Disc.LoadAutomagic(file);
					var (path, filename, _) = file.SplitPathToDirFileAndExt();
					bool? PromptForOverwrite(string mp3Path)
					{
						var (caption, text) = _i18n.GetDialogText(
							"discomp3extract-3418-prompt-overwrite",
							new ArgsDict(filePath: mp3Path));
						return MessageBox.Show(caption: caption, text: text, buttons: MessageBoxButtons.YesNoCancel) switch
						{
							DialogResult.Yes => true,
							DialogResult.No => false,
							_ => null,
						};
					}
					AudioExtractor.Extract(disc, path, filename, PromptForOverwrite);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(
					caption: _i18n.GetDialogText("discomp3extract-7691-errbox-misc").Caption,
					text: ex.ToString());
				throw;
			}
			finally
			{
				lblMp3ExtractMagicArea.AllowDrop = true;
				Cursor = Cursors.Default;
			}
		}
	}
}
