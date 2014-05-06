namespace BizHawk.Client.EmuHawk
{
	partial class EmuHawkOptions
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.OkBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.label3 = new System.Windows.Forms.Label();
			this.SingleInstanceModeCheckbox = new System.Windows.Forms.CheckBox();
			this.NeverAskSaveCheckbox = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.AcceptBackgroundInputCheckbox = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.RunInBackgroundCheckbox = new System.Windows.Forms.CheckBox();
			this.ShowMenuInFullScreenCheckbox = new System.Windows.Forms.CheckBox();
			this.SaveWindowPositionCheckbox = new System.Windows.Forms.CheckBox();
			this.EnableContextMenuCheckbox = new System.Windows.Forms.CheckBox();
			this.StartPausedCheckbox = new System.Windows.Forms.CheckBox();
			this.PauseWhenMenuActivatedCheckbox = new System.Windows.Forms.CheckBox();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.BackupSRamCheckbox = new System.Windows.Forms.CheckBox();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.ScreenshotInStatesCheckbox = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.BackupSavestatesCheckbox = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.LogWindowAsConsoleCheckbox = new System.Windows.Forms.CheckBox();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.FrameAdvSkipLagCheckbox = new System.Windows.Forms.CheckBox();
			this.label11 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(280, 367);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 0;
			this.OkBtn.Text = "&Ok";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(346, 367);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 1;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage3);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Location = new System.Drawing.Point(12, 12);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(394, 349);
			this.tabControl1.TabIndex = 2;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.label14);
			this.tabPage1.Controls.Add(this.label3);
			this.tabPage1.Controls.Add(this.SingleInstanceModeCheckbox);
			this.tabPage1.Controls.Add(this.NeverAskSaveCheckbox);
			this.tabPage1.Controls.Add(this.label2);
			this.tabPage1.Controls.Add(this.AcceptBackgroundInputCheckbox);
			this.tabPage1.Controls.Add(this.label1);
			this.tabPage1.Controls.Add(this.RunInBackgroundCheckbox);
			this.tabPage1.Controls.Add(this.ShowMenuInFullScreenCheckbox);
			this.tabPage1.Controls.Add(this.SaveWindowPositionCheckbox);
			this.tabPage1.Controls.Add(this.EnableContextMenuCheckbox);
			this.tabPage1.Controls.Add(this.StartPausedCheckbox);
			this.tabPage1.Controls.Add(this.PauseWhenMenuActivatedCheckbox);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(386, 323);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "General";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(26, 285);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(272, 13);
			this.label3.TabIndex = 11;
			this.label3.Text = "Enable to force only one instance of EmuHawk at a time";
			// 
			// SingleInstanceModeCheckbox
			// 
			this.SingleInstanceModeCheckbox.AutoSize = true;
			this.SingleInstanceModeCheckbox.Location = new System.Drawing.Point(6, 265);
			this.SingleInstanceModeCheckbox.Name = "SingleInstanceModeCheckbox";
			this.SingleInstanceModeCheckbox.Size = new System.Drawing.Size(127, 17);
			this.SingleInstanceModeCheckbox.TabIndex = 10;
			this.SingleInstanceModeCheckbox.Text = "Single instance mode";
			this.SingleInstanceModeCheckbox.UseVisualStyleBackColor = true;
			// 
			// NeverAskSaveCheckbox
			// 
			this.NeverAskSaveCheckbox.AutoSize = true;
			this.NeverAskSaveCheckbox.Location = new System.Drawing.Point(6, 146);
			this.NeverAskSaveCheckbox.Name = "NeverAskSaveCheckbox";
			this.NeverAskSaveCheckbox.Size = new System.Drawing.Size(184, 17);
			this.NeverAskSaveCheckbox.TabIndex = 9;
			this.NeverAskSaveCheckbox.Text = "Never be asked to save changes";
			this.NeverAskSaveCheckbox.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(26, 245);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(349, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "When this is set, the client will receive user input even when focus is lost";
			// 
			// AcceptBackgroundInputCheckbox
			// 
			this.AcceptBackgroundInputCheckbox.AutoSize = true;
			this.AcceptBackgroundInputCheckbox.Location = new System.Drawing.Point(6, 225);
			this.AcceptBackgroundInputCheckbox.Name = "AcceptBackgroundInputCheckbox";
			this.AcceptBackgroundInputCheckbox.Size = new System.Drawing.Size(146, 17);
			this.AcceptBackgroundInputCheckbox.TabIndex = 7;
			this.AcceptBackgroundInputCheckbox.Text = "Accept background input";
			this.AcceptBackgroundInputCheckbox.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(26, 201);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(315, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "When this is set, the client will continue to run when it loses focus";
			// 
			// RunInBackgroundCheckbox
			// 
			this.RunInBackgroundCheckbox.AutoSize = true;
			this.RunInBackgroundCheckbox.Location = new System.Drawing.Point(6, 181);
			this.RunInBackgroundCheckbox.Name = "RunInBackgroundCheckbox";
			this.RunInBackgroundCheckbox.Size = new System.Drawing.Size(117, 17);
			this.RunInBackgroundCheckbox.TabIndex = 5;
			this.RunInBackgroundCheckbox.Text = "Run in background";
			this.RunInBackgroundCheckbox.UseVisualStyleBackColor = true;
			// 
			// ShowMenuInFullScreenCheckbox
			// 
			this.ShowMenuInFullScreenCheckbox.AutoSize = true;
			this.ShowMenuInFullScreenCheckbox.Location = new System.Drawing.Point(6, 40);
			this.ShowMenuInFullScreenCheckbox.Name = "ShowMenuInFullScreenCheckbox";
			this.ShowMenuInFullScreenCheckbox.Size = new System.Drawing.Size(147, 17);
			this.ShowMenuInFullScreenCheckbox.TabIndex = 4;
			this.ShowMenuInFullScreenCheckbox.Text = "Show Menu in FullScreen";
			this.ShowMenuInFullScreenCheckbox.UseVisualStyleBackColor = true;
			// 
			// SaveWindowPositionCheckbox
			// 
			this.SaveWindowPositionCheckbox.AutoSize = true;
			this.SaveWindowPositionCheckbox.Location = new System.Drawing.Point(6, 123);
			this.SaveWindowPositionCheckbox.Name = "SaveWindowPositionCheckbox";
			this.SaveWindowPositionCheckbox.Size = new System.Drawing.Size(133, 17);
			this.SaveWindowPositionCheckbox.TabIndex = 3;
			this.SaveWindowPositionCheckbox.Text = "Save Window Position";
			this.SaveWindowPositionCheckbox.UseVisualStyleBackColor = true;
			// 
			// EnableContextMenuCheckbox
			// 
			this.EnableContextMenuCheckbox.AutoSize = true;
			this.EnableContextMenuCheckbox.Location = new System.Drawing.Point(6, 63);
			this.EnableContextMenuCheckbox.Name = "EnableContextMenuCheckbox";
			this.EnableContextMenuCheckbox.Size = new System.Drawing.Size(128, 17);
			this.EnableContextMenuCheckbox.TabIndex = 2;
			this.EnableContextMenuCheckbox.Text = "Enable Context Menu";
			this.EnableContextMenuCheckbox.UseVisualStyleBackColor = true;
			// 
			// StartPausedCheckbox
			// 
			this.StartPausedCheckbox.AutoSize = true;
			this.StartPausedCheckbox.Location = new System.Drawing.Point(6, 100);
			this.StartPausedCheckbox.Name = "StartPausedCheckbox";
			this.StartPausedCheckbox.Size = new System.Drawing.Size(86, 17);
			this.StartPausedCheckbox.TabIndex = 1;
			this.StartPausedCheckbox.Text = "Start paused";
			this.StartPausedCheckbox.UseVisualStyleBackColor = true;
			// 
			// PauseWhenMenuActivatedCheckbox
			// 
			this.PauseWhenMenuActivatedCheckbox.AutoSize = true;
			this.PauseWhenMenuActivatedCheckbox.Location = new System.Drawing.Point(6, 17);
			this.PauseWhenMenuActivatedCheckbox.Name = "PauseWhenMenuActivatedCheckbox";
			this.PauseWhenMenuActivatedCheckbox.Size = new System.Drawing.Size(161, 17);
			this.PauseWhenMenuActivatedCheckbox.TabIndex = 0;
			this.PauseWhenMenuActivatedCheckbox.Text = "Pause when menu activated";
			this.PauseWhenMenuActivatedCheckbox.UseVisualStyleBackColor = true;
			// 
			// tabPage3
			// 
			this.tabPage3.Controls.Add(this.label12);
			this.tabPage3.Controls.Add(this.label13);
			this.tabPage3.Controls.Add(this.FrameAdvSkipLagCheckbox);
			this.tabPage3.Controls.Add(this.label9);
			this.tabPage3.Controls.Add(this.label10);
			this.tabPage3.Controls.Add(this.BackupSRamCheckbox);
			this.tabPage3.Controls.Add(this.label8);
			this.tabPage3.Controls.Add(this.label7);
			this.tabPage3.Controls.Add(this.ScreenshotInStatesCheckbox);
			this.tabPage3.Controls.Add(this.label6);
			this.tabPage3.Controls.Add(this.label5);
			this.tabPage3.Controls.Add(this.BackupSavestatesCheckbox);
			this.tabPage3.Controls.Add(this.label4);
			this.tabPage3.Controls.Add(this.LogWindowAsConsoleCheckbox);
			this.tabPage3.Location = new System.Drawing.Point(4, 22);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.Size = new System.Drawing.Size(386, 323);
			this.tabPage3.TabIndex = 2;
			this.tabPage3.Text = "Advanced";
			this.tabPage3.UseVisualStyleBackColor = true;
			// 
			// BackupSRamCheckbox
			// 
			this.BackupSRamCheckbox.AutoSize = true;
			this.BackupSRamCheckbox.Location = new System.Drawing.Point(4, 195);
			this.BackupSRamCheckbox.Name = "BackupSRamCheckbox";
			this.BackupSRamCheckbox.Size = new System.Drawing.Size(108, 17);
			this.BackupSRamCheckbox.TabIndex = 9;
			this.BackupSRamCheckbox.Text = "Backup Saveram";
			this.BackupSRamCheckbox.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(24, 167);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(183, 13);
			this.label8.TabIndex = 8;
			this.label8.Text = "black screen on the frame it is loaded";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(24, 153);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(290, 13);
			this.label7.TabIndex = 7;
			this.label7.Text = "Saves a screenshot and loads it on loadstate so there isn\'t a";
			// 
			// ScreenshotInStatesCheckbox
			// 
			this.ScreenshotInStatesCheckbox.AutoSize = true;
			this.ScreenshotInStatesCheckbox.Location = new System.Drawing.Point(4, 134);
			this.ScreenshotInStatesCheckbox.Name = "ScreenshotInStatesCheckbox";
			this.ScreenshotInStatesCheckbox.Size = new System.Drawing.Size(180, 17);
			this.ScreenshotInStatesCheckbox.TabIndex = 6;
			this.ScreenshotInStatesCheckbox.Text = "Save a screenshot in savestates";
			this.ScreenshotInStatesCheckbox.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(24, 107);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(99, 13);
			this.label6.TabIndex = 5;
			this.label6.Text = "before overwriting it";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(24, 94);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(292, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "When set, the client will make a backup copy of a savestate";
			// 
			// BackupSavestatesCheckbox
			// 
			this.BackupSavestatesCheckbox.AutoSize = true;
			this.BackupSavestatesCheckbox.Location = new System.Drawing.Point(6, 74);
			this.BackupSavestatesCheckbox.Name = "BackupSavestatesCheckbox";
			this.BackupSavestatesCheckbox.Size = new System.Drawing.Size(119, 17);
			this.BackupSavestatesCheckbox.TabIndex = 3;
			this.BackupSavestatesCheckbox.Text = "Backup Savestates";
			this.BackupSavestatesCheckbox.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(24, 37);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(234, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = "If off, the log window will be a dialog box instead";
			// 
			// LogWindowAsConsoleCheckbox
			// 
			this.LogWindowAsConsoleCheckbox.AutoSize = true;
			this.LogWindowAsConsoleCheckbox.Location = new System.Drawing.Point(6, 17);
			this.LogWindowAsConsoleCheckbox.Name = "LogWindowAsConsoleCheckbox";
			this.LogWindowAsConsoleCheckbox.Size = new System.Drawing.Size(233, 17);
			this.LogWindowAsConsoleCheckbox.TabIndex = 1;
			this.LogWindowAsConsoleCheckbox.Text = "Create the log window as a console window";
			this.LogWindowAsConsoleCheckbox.UseVisualStyleBackColor = true;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.label11);
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(386, 313);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Recent Menus";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(24, 224);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(99, 13);
			this.label9.TabIndex = 11;
			this.label9.Text = "before overwriting it";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(24, 211);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(277, 13);
			this.label10.TabIndex = 10;
			this.label10.Text = "When set, the client will make a backup copy of saveram";
			// 
			// FrameAdvSkipLagCheckbox
			// 
			this.FrameAdvSkipLagCheckbox.AutoSize = true;
			this.FrameAdvSkipLagCheckbox.Location = new System.Drawing.Point(6, 250);
			this.FrameAdvSkipLagCheckbox.Name = "FrameAdvSkipLagCheckbox";
			this.FrameAdvSkipLagCheckbox.Size = new System.Drawing.Size(241, 17);
			this.FrameAdvSkipLagCheckbox.TabIndex = 12;
			this.FrameAdvSkipLagCheckbox.Text = "Frame advance button skips non-input frames";
			this.FrameAdvSkipLagCheckbox.UseVisualStyleBackColor = true;
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(17, 16);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(35, 13);
			this.label11.TabIndex = 0;
			this.label11.Text = "To do";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(24, 283);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(231, 13);
			this.label12.TabIndex = 14;
			this.label12.Text = "frames in which no input was polled (lag frames)";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(24, 270);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(268, 13);
			this.label13.TabIndex = 13;
			this.label13.Text = "When enabled, the frame advance button will skip over";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(26, 299);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(303, 13);
			this.label14.TabIndex = 12;
			this.label14.Text = "Note: Requires closing and reopening EmuHawk to take effect";
			// 
			// EmuHawkOptions
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(418, 402);
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.Name = "EmuHawkOptions";
			this.ShowIcon = false;
			this.Text = "Gui Options";
			this.Load += new System.EventHandler(this.GuiOptions_Load);
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage3.ResumeLayout(false);
			this.tabPage3.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.CheckBox StartPausedCheckbox;
		private System.Windows.Forms.CheckBox PauseWhenMenuActivatedCheckbox;
		private System.Windows.Forms.CheckBox EnableContextMenuCheckbox;
		private System.Windows.Forms.CheckBox SaveWindowPositionCheckbox;
		private System.Windows.Forms.CheckBox ShowMenuInFullScreenCheckbox;
		private System.Windows.Forms.CheckBox RunInBackgroundCheckbox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox AcceptBackgroundInputCheckbox;
		private System.Windows.Forms.CheckBox NeverAskSaveCheckbox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox SingleInstanceModeCheckbox;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.CheckBox LogWindowAsConsoleCheckbox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox BackupSavestatesCheckbox;
		private System.Windows.Forms.CheckBox ScreenshotInStatesCheckbox;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.CheckBox BackupSRamCheckbox;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.CheckBox FrameAdvSkipLagCheckbox;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label14;
	}
}