namespace BizHawk.Client.MultiHawk
{
	partial class Mainform
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
			this.MainformMenu = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.NewSessionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OpenSessionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveSessionMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SaveSessionAsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentSessionSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
			this.OpenRomMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RecentRomSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.RebootCoresMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MovieSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.RecordMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.PlayMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StopMovieMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ToggleReadonlyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.configToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.controllerConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.hotkeyConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.saveConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.WorkspacePanel = new System.Windows.Forms.Panel();
			this.MainStatusBar = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.FameStatusBarLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.StatusBarMessageLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.PlayRecordStatusButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.ViewSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this._1xMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._2xMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._3xMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._4xMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.MainformMenu.SuspendLayout();
			this.WorkspacePanel.SuspendLayout();
			this.MainStatusBar.SuspendLayout();
			this.SuspendLayout();
			// 
			// MainformMenu
			// 
			this.MainformMenu.ClickThrough = true;
			this.MainformMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.ViewSubMenu,
            this.MovieSubMenu,
            this.configToolStripMenuItem});
			this.MainformMenu.Location = new System.Drawing.Point(0, 0);
			this.MainformMenu.Name = "MainformMenu";
			this.MainformMenu.Size = new System.Drawing.Size(655, 24);
			this.MainformMenu.TabIndex = 0;
			this.MainformMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewSessionMenuItem,
            this.OpenSessionMenuItem,
            this.SaveSessionMenuItem,
            this.SaveSessionAsMenuItem,
            this.RecentSessionSubMenu,
            this.toolStripSeparator7,
            this.OpenRomMenuItem,
            this.RecentRomSubMenu,
            this.toolStripSeparator5,
            this.RebootCoresMenuItem,
            this.toolStripSeparator3,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewSessionMenuItem
			// 
			this.NewSessionMenuItem.Name = "NewSessionMenuItem";
			this.NewSessionMenuItem.Size = new System.Drawing.Size(165, 22);
			this.NewSessionMenuItem.Text = "&New Session";
			this.NewSessionMenuItem.Click += new System.EventHandler(this.NewSessionMenuItem_Click);
			// 
			// OpenSessionMenuItem
			// 
			this.OpenSessionMenuItem.Name = "OpenSessionMenuItem";
			this.OpenSessionMenuItem.Size = new System.Drawing.Size(165, 22);
			this.OpenSessionMenuItem.Text = "&Open Session";
			this.OpenSessionMenuItem.Click += new System.EventHandler(this.OpenSessionMenuItem_Click);
			// 
			// SaveSessionMenuItem
			// 
			this.SaveSessionMenuItem.Name = "SaveSessionMenuItem";
			this.SaveSessionMenuItem.Size = new System.Drawing.Size(165, 22);
			this.SaveSessionMenuItem.Text = "&Save Session";
			this.SaveSessionMenuItem.Click += new System.EventHandler(this.SaveSessionMenuItem_Click);
			// 
			// SaveSessionAsMenuItem
			// 
			this.SaveSessionAsMenuItem.Name = "SaveSessionAsMenuItem";
			this.SaveSessionAsMenuItem.Size = new System.Drawing.Size(165, 22);
			this.SaveSessionAsMenuItem.Text = "Save Session &As...";
			this.SaveSessionAsMenuItem.Click += new System.EventHandler(this.SaveSessionAsMenuItem_Click);
			// 
			// RecentSessionSubMenu
			// 
			this.RecentSessionSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator6});
			this.RecentSessionSubMenu.Name = "RecentSessionSubMenu";
			this.RecentSessionSubMenu.Size = new System.Drawing.Size(165, 22);
			this.RecentSessionSubMenu.Text = "Recent Session";
			this.RecentSessionSubMenu.DropDownOpened += new System.EventHandler(this.RecentSessionSubMenu_DropDownOpened);
			// 
			// toolStripSeparator6
			// 
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(149, 6);
			// 
			// toolStripSeparator7
			// 
			this.toolStripSeparator7.Name = "toolStripSeparator7";
			this.toolStripSeparator7.Size = new System.Drawing.Size(162, 6);
			// 
			// OpenRomMenuItem
			// 
			this.OpenRomMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.OpenFile;
			this.OpenRomMenuItem.Name = "OpenRomMenuItem";
			this.OpenRomMenuItem.Size = new System.Drawing.Size(165, 22);
			this.OpenRomMenuItem.Text = "Open ROM";
			this.OpenRomMenuItem.Click += new System.EventHandler(this.OpenRomMenuItem_Click);
			// 
			// RecentRomSubMenu
			// 
			this.RecentRomSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator1});
			this.RecentRomSubMenu.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.Recent;
			this.RecentRomSubMenu.Name = "RecentRomSubMenu";
			this.RecentRomSubMenu.Size = new System.Drawing.Size(165, 22);
			this.RecentRomSubMenu.Text = "Recent";
			this.RecentRomSubMenu.DropDownOpened += new System.EventHandler(this.RecentRomSubMenu_DropDownOpened);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
			// 
			// toolStripSeparator5
			// 
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(162, 6);
			// 
			// RebootCoresMenuItem
			// 
			this.RebootCoresMenuItem.Name = "RebootCoresMenuItem";
			this.RebootCoresMenuItem.Size = new System.Drawing.Size(165, 22);
			this.RebootCoresMenuItem.Text = "Reboot Cores";
			this.RebootCoresMenuItem.Click += new System.EventHandler(this.RebootCoresMenuItem_Click);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(162, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(165, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// MovieSubMenu
			// 
			this.MovieSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.RecordMovieMenuItem,
            this.PlayMovieMenuItem,
            this.StopMovieMenuItem,
            this.toolStripSeparator2,
            this.ToggleReadonlyMenuItem});
			this.MovieSubMenu.Name = "MovieSubMenu";
			this.MovieSubMenu.Size = new System.Drawing.Size(52, 20);
			this.MovieSubMenu.Text = "&Movie";
			this.MovieSubMenu.DropDownOpened += new System.EventHandler(this.MovieSubMenu_DropDownOpened);
			// 
			// RecordMovieMenuItem
			// 
			this.RecordMovieMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.RecordHS;
			this.RecordMovieMenuItem.Name = "RecordMovieMenuItem";
			this.RecordMovieMenuItem.Size = new System.Drawing.Size(168, 22);
			this.RecordMovieMenuItem.Text = "Record Movie";
			this.RecordMovieMenuItem.Click += new System.EventHandler(this.RecordMovieMenuItem_Click);
			// 
			// PlayMovieMenuItem
			// 
			this.PlayMovieMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.Play;
			this.PlayMovieMenuItem.Name = "PlayMovieMenuItem";
			this.PlayMovieMenuItem.Size = new System.Drawing.Size(168, 22);
			this.PlayMovieMenuItem.Text = "Play Movie";
			this.PlayMovieMenuItem.Click += new System.EventHandler(this.PlayMovieMenuItem_Click);
			// 
			// StopMovieMenuItem
			// 
			this.StopMovieMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.Stop;
			this.StopMovieMenuItem.Name = "StopMovieMenuItem";
			this.StopMovieMenuItem.Size = new System.Drawing.Size(168, 22);
			this.StopMovieMenuItem.Text = "&Stop Movie";
			this.StopMovieMenuItem.Click += new System.EventHandler(this.StopMovieMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(165, 6);
			// 
			// ToggleReadonlyMenuItem
			// 
			this.ToggleReadonlyMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.ReadOnly;
			this.ToggleReadonlyMenuItem.Name = "ToggleReadonlyMenuItem";
			this.ToggleReadonlyMenuItem.Size = new System.Drawing.Size(168, 22);
			this.ToggleReadonlyMenuItem.Text = "Toggle Read-only";
			this.ToggleReadonlyMenuItem.Click += new System.EventHandler(this.ToggleReadonlyMenuItem_Click);
			// 
			// configToolStripMenuItem
			// 
			this.configToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.controllerConfigToolStripMenuItem,
            this.hotkeyConfigToolStripMenuItem,
            this.toolStripSeparator4,
            this.saveConfigToolStripMenuItem});
			this.configToolStripMenuItem.Name = "configToolStripMenuItem";
			this.configToolStripMenuItem.Size = new System.Drawing.Size(55, 20);
			this.configToolStripMenuItem.Text = "&Config";
			// 
			// controllerConfigToolStripMenuItem
			// 
			this.controllerConfigToolStripMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.GameController;
			this.controllerConfigToolStripMenuItem.Name = "controllerConfigToolStripMenuItem";
			this.controllerConfigToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			this.controllerConfigToolStripMenuItem.Text = "Controller Config";
			this.controllerConfigToolStripMenuItem.Click += new System.EventHandler(this.controllerConfigToolStripMenuItem_Click);
			// 
			// hotkeyConfigToolStripMenuItem
			// 
			this.hotkeyConfigToolStripMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.HotKeys;
			this.hotkeyConfigToolStripMenuItem.Name = "hotkeyConfigToolStripMenuItem";
			this.hotkeyConfigToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			this.hotkeyConfigToolStripMenuItem.Text = "Hotkey Config";
			this.hotkeyConfigToolStripMenuItem.Click += new System.EventHandler(this.hotkeyConfigToolStripMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(163, 6);
			// 
			// saveConfigToolStripMenuItem
			// 
			this.saveConfigToolStripMenuItem.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.Save;
			this.saveConfigToolStripMenuItem.Name = "saveConfigToolStripMenuItem";
			this.saveConfigToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
			this.saveConfigToolStripMenuItem.Text = "Save Config";
			this.saveConfigToolStripMenuItem.Click += new System.EventHandler(this.saveConfigToolStripMenuItem_Click);
			// 
			// WorkspacePanel
			// 
			this.WorkspacePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.WorkspacePanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.WorkspacePanel.Controls.Add(this.MainStatusBar);
			this.WorkspacePanel.Location = new System.Drawing.Point(0, 25);
			this.WorkspacePanel.Name = "WorkspacePanel";
			this.WorkspacePanel.Size = new System.Drawing.Size(655, 405);
			this.WorkspacePanel.TabIndex = 1;
			// 
			// MainStatusBar
			// 
			this.MainStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.FameStatusBarLabel,
            this.StatusBarMessageLabel,
            this.PlayRecordStatusButton});
			this.MainStatusBar.Location = new System.Drawing.Point(0, 379);
			this.MainStatusBar.Name = "MainStatusBar";
			this.MainStatusBar.Size = new System.Drawing.Size(651, 22);
			this.MainStatusBar.TabIndex = 0;
			this.MainStatusBar.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(46, 17);
			this.toolStripStatusLabel1.Text = "Frame: ";
			// 
			// FameStatusBarLabel
			// 
			this.FameStatusBarLabel.Name = "FameStatusBarLabel";
			this.FameStatusBarLabel.Size = new System.Drawing.Size(13, 17);
			this.FameStatusBarLabel.Text = "0";
			// 
			// StatusBarMessageLabel
			// 
			this.StatusBarMessageLabel.Name = "StatusBarMessageLabel";
			this.StatusBarMessageLabel.Size = new System.Drawing.Size(35, 17);
			this.StatusBarMessageLabel.Text = "Hello";
			// 
			// PlayRecordStatusButton
			// 
			this.PlayRecordStatusButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PlayRecordStatusButton.Image = global::BizHawk.Client.MultiHawk.Properties.Resources.Blank;
			this.PlayRecordStatusButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PlayRecordStatusButton.Name = "PlayRecordStatusButton";
			this.PlayRecordStatusButton.Size = new System.Drawing.Size(29, 20);
			this.PlayRecordStatusButton.Text = "No movie is active";
			// 
			// ViewSubMenu
			// 
			this.ViewSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._1xMenuItem,
            this._2xMenuItem,
            this._3xMenuItem,
            this._4xMenuItem});
			this.ViewSubMenu.Name = "ViewSubMenu";
			this.ViewSubMenu.Size = new System.Drawing.Size(44, 20);
			this.ViewSubMenu.Text = "&View";
			this.ViewSubMenu.DropDownOpened += new System.EventHandler(this.ViewSubMenu_DropDownOpened);
			// 
			// _1xMenuItem
			// 
			this._1xMenuItem.Name = "_1xMenuItem";
			this._1xMenuItem.Size = new System.Drawing.Size(152, 22);
			this._1xMenuItem.Text = "&1x";
			this._1xMenuItem.Click += new System.EventHandler(this._1xMenuItem_Click);
			// 
			// _2xMenuItem
			// 
			this._2xMenuItem.Name = "_2xMenuItem";
			this._2xMenuItem.Size = new System.Drawing.Size(152, 22);
			this._2xMenuItem.Text = "&2x";
			this._2xMenuItem.Click += new System.EventHandler(this._2xMenuItem_Click);
			// 
			// _3xMenuItem
			// 
			this._3xMenuItem.Name = "_3xMenuItem";
			this._3xMenuItem.Size = new System.Drawing.Size(152, 22);
			this._3xMenuItem.Text = "&3x";
			this._3xMenuItem.Click += new System.EventHandler(this._3xMenuItem_Click);
			// 
			// _4xMenuItem
			// 
			this._4xMenuItem.Name = "_4xMenuItem";
			this._4xMenuItem.Size = new System.Drawing.Size(152, 22);
			this._4xMenuItem.Text = "&4x";
			this._4xMenuItem.Click += new System.EventHandler(this._4xMenuItem_Click);
			// 
			// Mainform
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(655, 431);
			this.Controls.Add(this.WorkspacePanel);
			this.Controls.Add(this.MainformMenu);
			this.MainMenuStrip = this.MainformMenu;
			this.Name = "Mainform";
			this.Text = "MultiHawk";
			this.Load += new System.EventHandler(this.Mainform_Load);
			this.MainformMenu.ResumeLayout(false);
			this.MainformMenu.PerformLayout();
			this.WorkspacePanel.ResumeLayout(false);
			this.WorkspacePanel.PerformLayout();
			this.MainStatusBar.ResumeLayout(false);
			this.MainStatusBar.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx MainformMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem OpenRomMenuItem;
		private System.Windows.Forms.Panel WorkspacePanel;
		private System.Windows.Forms.StatusStrip MainStatusBar;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripStatusLabel FameStatusBarLabel;
		private System.Windows.Forms.ToolStripStatusLabel StatusBarMessageLabel;
		private System.Windows.Forms.ToolStripMenuItem RecentRomSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem MovieSubMenu;
		private System.Windows.Forms.ToolStripMenuItem RecordMovieMenuItem;
		private System.Windows.Forms.ToolStripMenuItem PlayMovieMenuItem;
		private System.Windows.Forms.ToolStripMenuItem StopMovieMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem ToggleReadonlyMenuItem;
		private System.Windows.Forms.ToolStripDropDownButton PlayRecordStatusButton;
		private System.Windows.Forms.ToolStripMenuItem configToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem controllerConfigToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem hotkeyConfigToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem saveConfigToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem RebootCoresMenuItem;
		private System.Windows.Forms.ToolStripMenuItem OpenSessionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveSessionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem RecentSessionSubMenu;
		private System.Windows.Forms.ToolStripMenuItem NewSessionMenuItem;
		private System.Windows.Forms.ToolStripMenuItem SaveSessionAsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
		private System.Windows.Forms.ToolStripMenuItem ViewSubMenu;
		private System.Windows.Forms.ToolStripMenuItem _1xMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _2xMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _3xMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _4xMenuItem;
	}
}

