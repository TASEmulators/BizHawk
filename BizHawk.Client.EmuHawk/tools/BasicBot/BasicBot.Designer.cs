namespace BizHawk.Client.EmuHawk
{
	partial class BasicBot
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
			this.BotMenu = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RunBtn = new System.Windows.Forms.Button();
			this.BotStatusStrip = new System.Windows.Forms.StatusStrip();
			this.ControlsBox = new System.Windows.Forms.GroupBox();
			this.ControlProbabilityPanel = new System.Windows.Forms.Panel();
			this.BestGroupBox = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.AttemptsLabel = new System.Windows.Forms.Label();
			this.FramesLabel = new System.Windows.Forms.Label();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.countRerecordsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GoalGroupBox = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.FrameLengthNumeric = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.maximizeLabeltext = new System.Windows.Forms.Label();
			this.MaximizeAddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.TieBreaker3Box = new BizHawk.Client.EmuHawk.HexTextBox();
			this.TieBreaker2Box = new BizHawk.Client.EmuHawk.HexTextBox();
			this.TieBreaker1Box = new BizHawk.Client.EmuHawk.HexTextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.BotMenu.SuspendLayout();
			this.ControlsBox.SuspendLayout();
			this.GoalGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.FrameLengthNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// BotMenu
			// 
			this.BotMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem});
			this.BotMenu.Location = new System.Drawing.Point(0, 0);
			this.BotMenu.Name = "BotMenu";
			this.BotMenu.Size = new System.Drawing.Size(574, 24);
			this.BotMenu.TabIndex = 0;
			this.BotMenu.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.recentToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// openToolStripMenuItem
			// 
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.openToolStripMenuItem.Text = "Open";
			// 
			// saveToolStripMenuItem
			// 
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.saveToolStripMenuItem.Text = "Save";
			// 
			// recentToolStripMenuItem
			// 
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.recentToolStripMenuItem.Text = "Recent";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(131, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(134, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// RunBtn
			// 
			this.RunBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.RunBtn.Location = new System.Drawing.Point(487, 494);
			this.RunBtn.Name = "RunBtn";
			this.RunBtn.Size = new System.Drawing.Size(75, 23);
			this.RunBtn.TabIndex = 1;
			this.RunBtn.Text = "&Run";
			this.RunBtn.UseVisualStyleBackColor = true;
			this.RunBtn.Click += new System.EventHandler(this.RunBtn_Click);
			// 
			// BotStatusStrip
			// 
			this.BotStatusStrip.Location = new System.Drawing.Point(0, 530);
			this.BotStatusStrip.Name = "BotStatusStrip";
			this.BotStatusStrip.Size = new System.Drawing.Size(574, 22);
			this.BotStatusStrip.TabIndex = 2;
			this.BotStatusStrip.Text = "statusStrip1";
			// 
			// ControlsBox
			// 
			this.ControlsBox.Controls.Add(this.ControlProbabilityPanel);
			this.ControlsBox.Location = new System.Drawing.Point(12, 27);
			this.ControlsBox.Name = "ControlsBox";
			this.ControlsBox.Size = new System.Drawing.Size(311, 344);
			this.ControlsBox.TabIndex = 3;
			this.ControlsBox.TabStop = false;
			this.ControlsBox.Text = "Controls";
			// 
			// ControlProbabilityPanel
			// 
			this.ControlProbabilityPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.ControlProbabilityPanel.AutoScroll = true;
			this.ControlProbabilityPanel.Location = new System.Drawing.Point(6, 19);
			this.ControlProbabilityPanel.Name = "ControlProbabilityPanel";
			this.ControlProbabilityPanel.Size = new System.Drawing.Size(299, 319);
			this.ControlProbabilityPanel.TabIndex = 0;
			// 
			// BestGroupBox
			// 
			this.BestGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BestGroupBox.Location = new System.Drawing.Point(329, 86);
			this.BestGroupBox.Name = "BestGroupBox";
			this.BestGroupBox.Size = new System.Drawing.Size(245, 285);
			this.BestGroupBox.TabIndex = 4;
			this.BestGroupBox.TabStop = false;
			this.BestGroupBox.Text = "Best";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(339, 46);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(51, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "Attempts:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(346, 63);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(44, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Frames:";
			// 
			// AttemptsLabel
			// 
			this.AttemptsLabel.AutoSize = true;
			this.AttemptsLabel.Location = new System.Drawing.Point(397, 46);
			this.AttemptsLabel.Name = "AttemptsLabel";
			this.AttemptsLabel.Size = new System.Drawing.Size(13, 13);
			this.AttemptsLabel.TabIndex = 7;
			this.AttemptsLabel.Text = "0";
			// 
			// FramesLabel
			// 
			this.FramesLabel.AutoSize = true;
			this.FramesLabel.Location = new System.Drawing.Point(397, 63);
			this.FramesLabel.Name = "FramesLabel";
			this.FramesLabel.Size = new System.Drawing.Size(13, 13);
			this.FramesLabel.TabIndex = 8;
			this.FramesLabel.Text = "0";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.countRerecordsToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			// 
			// countRerecordsToolStripMenuItem
			// 
			this.countRerecordsToolStripMenuItem.Enabled = false;
			this.countRerecordsToolStripMenuItem.Name = "countRerecordsToolStripMenuItem";
			this.countRerecordsToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
			this.countRerecordsToolStripMenuItem.Text = "Count Rerecords";
			// 
			// GoalGroupBox
			// 
			this.GoalGroupBox.Controls.Add(this.label7);
			this.GoalGroupBox.Controls.Add(this.label6);
			this.GoalGroupBox.Controls.Add(this.TieBreaker1Box);
			this.GoalGroupBox.Controls.Add(this.TieBreaker2Box);
			this.GoalGroupBox.Controls.Add(this.TieBreaker3Box);
			this.GoalGroupBox.Controls.Add(this.label5);
			this.GoalGroupBox.Controls.Add(this.MaximizeAddressBox);
			this.GoalGroupBox.Controls.Add(this.maximizeLabeltext);
			this.GoalGroupBox.Controls.Add(this.label4);
			this.GoalGroupBox.Controls.Add(this.FrameLengthNumeric);
			this.GoalGroupBox.Controls.Add(this.label3);
			this.GoalGroupBox.Location = new System.Drawing.Point(12, 377);
			this.GoalGroupBox.Name = "GoalGroupBox";
			this.GoalGroupBox.Size = new System.Drawing.Size(311, 150);
			this.GoalGroupBox.TabIndex = 9;
			this.GoalGroupBox.TabStop = false;
			this.GoalGroupBox.Text = "Goal";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(7, 29);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(50, 13);
			this.label3.TabIndex = 0;
			this.label3.Text = "End after";
			// 
			// FrameLengthNumeric
			// 
			this.FrameLengthNumeric.Location = new System.Drawing.Point(60, 25);
			this.FrameLengthNumeric.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
			this.FrameLengthNumeric.Name = "FrameLengthNumeric";
			this.FrameLengthNumeric.Size = new System.Drawing.Size(46, 20);
			this.FrameLengthNumeric.TabIndex = 1;
			this.FrameLengthNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(113, 29);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(38, 13);
			this.label4.TabIndex = 2;
			this.label4.Text = "frames";
			// 
			// maximizeLabeltext
			// 
			this.maximizeLabeltext.AutoSize = true;
			this.maximizeLabeltext.Location = new System.Drawing.Point(9, 55);
			this.maximizeLabeltext.Name = "maximizeLabeltext";
			this.maximizeLabeltext.Size = new System.Drawing.Size(94, 13);
			this.maximizeLabeltext.TabIndex = 3;
			this.maximizeLabeltext.Text = "Maximize Address:";
			// 
			// MaximizeAddressBox
			// 
			this.MaximizeAddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.MaximizeAddressBox.Location = new System.Drawing.Point(116, 52);
			this.MaximizeAddressBox.Name = "MaximizeAddressBox";
			this.MaximizeAddressBox.Nullable = true;
			this.MaximizeAddressBox.Size = new System.Drawing.Size(95, 20);
			this.MaximizeAddressBox.TabIndex = 4;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(42, 78);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(61, 13);
			this.label5.TabIndex = 5;
			this.label5.Text = "Tiebreak 1:";
			// 
			// TieBreaker3Box
			// 
			this.TieBreaker3Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.TieBreaker3Box.Location = new System.Drawing.Point(116, 120);
			this.TieBreaker3Box.Name = "TieBreaker3Box";
			this.TieBreaker3Box.Nullable = true;
			this.TieBreaker3Box.Size = new System.Drawing.Size(95, 20);
			this.TieBreaker3Box.TabIndex = 6;
			// 
			// TieBreaker2Box
			// 
			this.TieBreaker2Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.TieBreaker2Box.Location = new System.Drawing.Point(116, 98);
			this.TieBreaker2Box.Name = "TieBreaker2Box";
			this.TieBreaker2Box.Nullable = true;
			this.TieBreaker2Box.Size = new System.Drawing.Size(95, 20);
			this.TieBreaker2Box.TabIndex = 7;
			// 
			// TieBreaker1Box
			// 
			this.TieBreaker1Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.TieBreaker1Box.Location = new System.Drawing.Point(116, 75);
			this.TieBreaker1Box.Name = "TieBreaker1Box";
			this.TieBreaker1Box.Nullable = true;
			this.TieBreaker1Box.Size = new System.Drawing.Size(95, 20);
			this.TieBreaker1Box.TabIndex = 8;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(45, 101);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(61, 13);
			this.label6.TabIndex = 9;
			this.label6.Text = "Tiebreak 2:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(42, 123);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(61, 13);
			this.label7.TabIndex = 10;
			this.label7.Text = "Tiebreak 3:";
			// 
			// BasicBot
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(574, 552);
			this.Controls.Add(this.GoalGroupBox);
			this.Controls.Add(this.FramesLabel);
			this.Controls.Add(this.AttemptsLabel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.BestGroupBox);
			this.Controls.Add(this.ControlsBox);
			this.Controls.Add(this.BotStatusStrip);
			this.Controls.Add(this.RunBtn);
			this.Controls.Add(this.BotMenu);
			this.MainMenuStrip = this.BotMenu;
			this.Name = "BasicBot";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Basic Bot";
			this.Load += new System.EventHandler(this.BasicBot_Load);
			this.BotMenu.ResumeLayout(false);
			this.BotMenu.PerformLayout();
			this.ControlsBox.ResumeLayout(false);
			this.GoalGroupBox.ResumeLayout(false);
			this.GoalGroupBox.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.FrameLengthNumeric)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip BotMenu;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.Button RunBtn;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.StatusStrip BotStatusStrip;
		private System.Windows.Forms.GroupBox ControlsBox;
		private System.Windows.Forms.Panel ControlProbabilityPanel;
		private System.Windows.Forms.GroupBox BestGroupBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label AttemptsLabel;
		private System.Windows.Forms.Label FramesLabel;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem countRerecordsToolStripMenuItem;
		private System.Windows.Forms.GroupBox GoalGroupBox;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label6;
		private HexTextBox TieBreaker1Box;
		private HexTextBox TieBreaker2Box;
		private HexTextBox TieBreaker3Box;
		private System.Windows.Forms.Label label5;
		private HexTextBox MaximizeAddressBox;
		private System.Windows.Forms.Label maximizeLabeltext;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.NumericUpDown FrameLengthNumeric;
		private System.Windows.Forms.Label label3;
	}
}