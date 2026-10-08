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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MacroInputTool));
            this.MacroMenu = new System.Windows.Forms.MenuStrip();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.saveAsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.loadMacroToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RecentToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.NameTextbox = new System.Windows.Forms.TextBox();
            this.ReplaceBox = new System.Windows.Forms.CheckBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PlaceNum = new System.Windows.Forms.NumericUpDown();
            this.EndNum = new System.Windows.Forms.NumericUpDown();
            this.PlaceZoneButton = new System.Windows.Forms.Button();
            this.StartNum = new System.Windows.Forms.NumericUpDown();
            this.SetZoneButton = new System.Windows.Forms.Button();
            this.ZonesList = new System.Windows.Forms.ListBox();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
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
            resources.ApplyResources(this.MacroMenu, "MacroMenu");
            this.MacroMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
            this.MacroMenu.Name = "MacroMenu";
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveAsToolStripMenuItem,
            this.loadMacroToolStripMenuItem,
            this.RecentToolStripMenuItem});
            // 
            // saveAsToolStripMenuItem
            // 
            resources.ApplyResources(this.saveAsToolStripMenuItem, "saveAsToolStripMenuItem");
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.SaveAsToolStripMenuItem_Click);
            // 
            // loadMacroToolStripMenuItem
            // 
            resources.ApplyResources(this.loadMacroToolStripMenuItem, "loadMacroToolStripMenuItem");
            this.loadMacroToolStripMenuItem.Click += new System.EventHandler(this.LoadMacroToolStripMenuItem_Click);
            // 
            // RecentToolStripMenuItem
            // 
            resources.ApplyResources(this.RecentToolStripMenuItem, "RecentToolStripMenuItem");
            this.RecentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1});
            this.RecentToolStripMenuItem.DropDownOpened += new System.EventHandler(this.RecentToolStripMenuItem_DropDownOpened);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // NameTextbox
            // 
            resources.ApplyResources(this.NameTextbox, "NameTextbox");
            this.NameTextbox.Name = "NameTextbox";
            this.NameTextbox.TextChanged += new System.EventHandler(this.NameTextBox_TextChanged);
            // 
            // ReplaceBox
            // 
            resources.ApplyResources(this.ReplaceBox, "ReplaceBox");
            this.ReplaceBox.Checked = true;
            this.ReplaceBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ReplaceBox.Name = "ReplaceBox";
            this.ReplaceBox.UseVisualStyleBackColor = true;
            this.ReplaceBox.CheckedChanged += new System.EventHandler(this.ReplaceBox_CheckedChanged);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // PlaceNum
            // 
            resources.ApplyResources(this.PlaceNum, "PlaceNum");
            this.PlaceNum.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.PlaceNum.Name = "PlaceNum";
            // 
            // EndNum
            // 
            resources.ApplyResources(this.EndNum, "EndNum");
            this.EndNum.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.EndNum.Name = "EndNum";
            // 
            // PlaceZoneButton
            // 
            resources.ApplyResources(this.PlaceZoneButton, "PlaceZoneButton");
            this.PlaceZoneButton.Name = "PlaceZoneButton";
            this.PlaceZoneButton.UseVisualStyleBackColor = true;
            this.PlaceZoneButton.Click += new System.EventHandler(this.PlaceZoneButton_Click);
            // 
            // StartNum
            // 
            resources.ApplyResources(this.StartNum, "StartNum");
            this.StartNum.Maximum = new decimal(new int[] {
            10000000,
            0,
            0,
            0});
            this.StartNum.Name = "StartNum";
            // 
            // SetZoneButton
            // 
            resources.ApplyResources(this.SetZoneButton, "SetZoneButton");
            this.SetZoneButton.Name = "SetZoneButton";
            this.SetZoneButton.UseVisualStyleBackColor = true;
            this.SetZoneButton.Click += new System.EventHandler(this.SetZoneButton_Click);
            // 
            // ZonesList
            // 
            resources.ApplyResources(this.ZonesList, "ZonesList");
            this.ZonesList.FormattingEnabled = true;
            this.ZonesList.Name = "ZonesList";
            this.ZonesList.SelectedIndexChanged += new System.EventHandler(this.ZonesList_SelectedIndexChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // CurrentButton
            // 
            resources.ApplyResources(this.CurrentButton, "CurrentButton");
            this.CurrentButton.Name = "CurrentButton";
            this.CurrentButton.UseVisualStyleBackColor = true;
            this.CurrentButton.Click += new System.EventHandler(this.CurrentButton_Click);
            // 
            // OverlayBox
            // 
            resources.ApplyResources(this.OverlayBox, "OverlayBox");
            this.OverlayBox.Name = "OverlayBox";
            this.OverlayBox.UseVisualStyleBackColor = true;
            this.OverlayBox.CheckedChanged += new System.EventHandler(this.OverlayBox_CheckedChanged);
            // 
            // MacroInputTool
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private System.Windows.Forms.TextBox NameTextbox;
		private System.Windows.Forms.CheckBox ReplaceBox;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.NumericUpDown PlaceNum;
		private System.Windows.Forms.NumericUpDown EndNum;
		private System.Windows.Forms.Button PlaceZoneButton;
		private System.Windows.Forms.NumericUpDown StartNum;
		private System.Windows.Forms.Button SetZoneButton;
		private System.Windows.Forms.ListBox ZonesList;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx saveAsToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx loadMacroToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentToolStripMenuItem;
		private System.Windows.Forms.Button CurrentButton;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private System.Windows.Forms.CheckBox OverlayBox;

	}
}