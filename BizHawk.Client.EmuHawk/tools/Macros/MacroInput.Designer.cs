namespace BizHawk.Client.EmuHawk
{
	partial class MacroInputTool
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
			this.MacroMenu = new System.Windows.Forms.MenuStrip();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadMacroToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.sepToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.NameTextbox = new System.Windows.Forms.TextBox();
			this.ReplaceBox = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.PlaceNum = new System.Windows.Forms.NumericUpDown();
			this.EndNum = new System.Windows.Forms.NumericUpDown();
			this.PlaceZoneButton = new System.Windows.Forms.Button();
			this.StartNum = new System.Windows.Forms.NumericUpDown();
			this.SetZoneButton = new System.Windows.Forms.Button();
			this.ZonesList = new System.Windows.Forms.ListBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.CurrentButton = new System.Windows.Forms.Button();
			this.OverlayBox = new System.Windows.Forms.CheckBox();
			this.MacroMenu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PlaceNum)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.EndNum)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.StartNum)).BeginInit();
			this.SuspendLayout();
			// 
			// MacroMenu
			// 
			this.MacroMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.MacroMenu.Location = new System.Drawing.Point(0, 0);
			this.MacroMenu.Name = "MacroMenu";
			this.MacroMenu.Size = new System.Drawing.Size(289, 24);
			this.MacroMenu.TabIndex = 0;
			this.MacroMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveAsToolStripMenuItem,
            this.loadMacroToolStripMenuItem,
            this.RecentToolStripMenuItem,
            this.sepToolStripMenuItem,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			// 
			// saveAsToolStripMenuItem
			// 
			this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
			this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
			this.saveAsToolStripMenuItem.Text = "Save Selected As...";
			this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
			// 
			// loadMacroToolStripMenuItem
			// 
			this.loadMacroToolStripMenuItem.Name = "loadMacroToolStripMenuItem";
			this.loadMacroToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
			this.loadMacroToolStripMenuItem.Text = "Load Macro...";
			this.loadMacroToolStripMenuItem.Click += new System.EventHandler(this.loadMacroToolStripMenuItem_Click);
			// 
			// RecentToolStripMenuItem
			// 
			this.RecentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1});
			this.RecentToolStripMenuItem.Name = "RecentToolStripMenuItem";
			this.RecentToolStripMenuItem.Size = new System.Drawing.Size(170, 22);
			this.RecentToolStripMenuItem.Text = "Recent";
			this.RecentToolStripMenuItem.DropDownOpened += new System.EventHandler(this.RecentToolStripMenuItem_DropDownOpened);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(57, 6);
			// 
			// sepToolStripMenuItem
			// 
			this.sepToolStripMenuItem.Name = "sepToolStripMenuItem";
			this.sepToolStripMenuItem.Size = new System.Drawing.Size(167, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(170, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// NameTextbox
			// 
			this.NameTextbox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.NameTextbox.Location = new System.Drawing.Point(77, 181);
			this.NameTextbox.Name = "NameTextbox";
			this.NameTextbox.Size = new System.Drawing.Size(99, 20);
			this.NameTextbox.TabIndex = 4;
			this.NameTextbox.Text = "Zone 0";
			this.NameTextbox.TextChanged += new System.EventHandler(this.NameTextbox_TextChanged);
			// 
			// ReplaceBox
			// 
			this.ReplaceBox.AutoSize = true;
			this.ReplaceBox.Checked = true;
			this.ReplaceBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ReplaceBox.Location = new System.Drawing.Point(88, 207);
			this.ReplaceBox.Name = "ReplaceBox";
			this.ReplaceBox.Size = new System.Drawing.Size(66, 17);
			this.ReplaceBox.TabIndex = 5;
			this.ReplaceBox.Text = "Replace";
			this.ReplaceBox.UseVisualStyleBackColor = true;
			this.ReplaceBox.CheckedChanged += new System.EventHandler(this.ReplaceBox_CheckedChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(127, 13);
			this.label2.TabIndex = 16;
			this.label2.Text = "macro start      macro end";
			// 
			// PlaceNum
			// 
			this.PlaceNum.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.PlaceNum.Location = new System.Drawing.Point(220, 182);
			this.PlaceNum.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
			this.PlaceNum.Name = "PlaceNum";
			this.PlaceNum.Size = new System.Drawing.Size(63, 20);
			this.PlaceNum.TabIndex = 6;
			this.PlaceNum.ValueChanged += new System.EventHandler(this.PlaceNum_ValueChanged);
			// 
			// EndNum
			// 
			this.EndNum.Location = new System.Drawing.Point(77, 40);
			this.EndNum.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
			this.EndNum.Name = "EndNum";
			this.EndNum.Size = new System.Drawing.Size(65, 20);
			this.EndNum.TabIndex = 1;
			// 
			// PlaceZoneButton
			// 
			this.PlaceZoneButton.Location = new System.Drawing.Point(7, 203);
			this.PlaceZoneButton.Name = "PlaceZoneButton";
			this.PlaceZoneButton.Size = new System.Drawing.Size(75, 23);
			this.PlaceZoneButton.TabIndex = 8;
			this.PlaceZoneButton.Text = "Place Zone";
			this.PlaceZoneButton.UseVisualStyleBackColor = true;
			this.PlaceZoneButton.Click += new System.EventHandler(this.PlaceZoneButton_Click);
			// 
			// StartNum
			// 
			this.StartNum.Location = new System.Drawing.Point(7, 40);
			this.StartNum.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
			this.StartNum.Name = "StartNum";
			this.StartNum.Size = new System.Drawing.Size(65, 20);
			this.StartNum.TabIndex = 0;
			// 
			// SetZoneButton
			// 
			this.SetZoneButton.Location = new System.Drawing.Point(148, 37);
			this.SetZoneButton.Name = "SetZoneButton";
			this.SetZoneButton.Size = new System.Drawing.Size(75, 23);
			this.SetZoneButton.TabIndex = 2;
			this.SetZoneButton.Text = "Set Macro";
			this.SetZoneButton.UseVisualStyleBackColor = true;
			this.SetZoneButton.Click += new System.EventHandler(this.SetZoneButton_Click);
			// 
			// ZonesList
			// 
			this.ZonesList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ZonesList.FormattingEnabled = true;
			this.ZonesList.Location = new System.Drawing.Point(7, 66);
			this.ZonesList.Name = "ZonesList";
			this.ZonesList.Size = new System.Drawing.Size(276, 108);
			this.ZonesList.TabIndex = 3;
			this.ZonesList.SelectedIndexChanged += new System.EventHandler(this.ZonesList_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(178, 184);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(39, 13);
			this.label3.TabIndex = 17;
			this.label3.Text = "Frame:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(4, 184);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(71, 13);
			this.label1.TabIndex = 22;
			this.label1.Text = "Macro Name:";
			// 
			// CurrentButton
			// 
			this.CurrentButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.CurrentButton.Location = new System.Drawing.Point(227, 203);
			this.CurrentButton.Name = "CurrentButton";
			this.CurrentButton.Size = new System.Drawing.Size(56, 23);
			this.CurrentButton.TabIndex = 7;
			this.CurrentButton.Text = "Current";
			this.CurrentButton.UseVisualStyleBackColor = true;
			this.CurrentButton.Click += new System.EventHandler(this.CurrentButton_Click);
			// 
			// OverlayBox
			// 
			this.OverlayBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.OverlayBox.AutoSize = true;
			this.OverlayBox.Location = new System.Drawing.Point(160, 207);
			this.OverlayBox.Name = "OverlayBox";
			this.OverlayBox.Size = new System.Drawing.Size(62, 17);
			this.OverlayBox.TabIndex = 24;
			this.OverlayBox.Text = "Overlay";
			this.OverlayBox.UseVisualStyleBackColor = true;
			this.OverlayBox.CheckedChanged += new System.EventHandler(this.OverlayBox_CheckedChanged);
			// 
			// MacroInputTool
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(289, 333);
			this.Controls.Add(this.OverlayBox);
			this.Controls.Add(this.CurrentButton);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.NameTextbox);
			this.Controls.Add(this.ReplaceBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.PlaceNum);
			this.Controls.Add(this.EndNum);
			this.Controls.Add(this.PlaceZoneButton);
			this.Controls.Add(this.StartNum);
			this.Controls.Add(this.SetZoneButton);
			this.Controls.Add(this.ZonesList);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.MacroMenu);
			this.MainMenuStrip = this.MacroMenu;
			this.Name = "MacroInputTool";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Macro Input";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MacroInputTool_FormClosing);
			this.Load += new System.EventHandler(this.MacroInputTool_Load);
			this.Resize += new System.EventHandler(this.MacroInputTool_Resize);
			this.MacroMenu.ResumeLayout(false);
			this.MacroMenu.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PlaceNum)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.EndNum)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.StartNum)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip MacroMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.TextBox NameTextbox;
		private System.Windows.Forms.CheckBox ReplaceBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown PlaceNum;
		private System.Windows.Forms.NumericUpDown EndNum;
		private System.Windows.Forms.Button PlaceZoneButton;
		private System.Windows.Forms.NumericUpDown StartNum;
		private System.Windows.Forms.Button SetZoneButton;
		private System.Windows.Forms.ListBox ZonesList;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadMacroToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RecentToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator sepToolStripMenuItem;
		private System.Windows.Forms.Button CurrentButton;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.CheckBox OverlayBox;

	}
}