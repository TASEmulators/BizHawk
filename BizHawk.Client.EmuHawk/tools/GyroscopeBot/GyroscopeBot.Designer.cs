namespace BizHawk.Client.EmuHawk
{
	partial class GyroscopeBot
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GyroscopeBot));
			this.BotMenu = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.OptionsSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.MemoryDomainsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.DataSizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._1ByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._2ByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._4ByteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BigEndianMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.TurboWhileBottingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RunBtn = new System.Windows.Forms.Button();
			this.BotStatusStrip = new System.Windows.Forms.StatusStrip();
			this.BotStatusButton = new System.Windows.Forms.ToolStripStatusLabel();
			this.MessageLabel = new System.Windows.Forms.ToolStripStatusLabel();

			this.label16 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.AttemptsLabel = new System.Windows.Forms.Label();
			this.FramesLabel = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.panel3 = new System.Windows.Forms.Panel();


			this.label5 = new System.Windows.Forms.Label();

			this.label10 = new System.Windows.Forms.Label();
			this.panel5 = new System.Windows.Forms.Panel();

			this.label11 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();

			this.panel6 = new System.Windows.Forms.Panel();
			this.label12 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();


			this.StopBtn = new System.Windows.Forms.Button();
			this.label8 = new System.Windows.Forms.Label();
			this.ControlGroupBox = new System.Windows.Forms.GroupBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.StatsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ClearStatsContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BotMenu.SuspendLayout();
			this.BotStatusStrip.SuspendLayout();


			this.ControlGroupBox.SuspendLayout();
			this.panel2.SuspendLayout();
			this.StatsContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// BotMenu
			// 
			this.BotMenu.ClickThrough = true;
			this.BotMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.FileSubMenu,
			this.OptionsSubMenu});
			this.BotMenu.Location = new System.Drawing.Point(0, 0);
			this.BotMenu.Name = "BotMenu";
			this.BotMenu.Size = new System.Drawing.Size(687, 24);
			this.BotMenu.TabIndex = 0;
			this.BotMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripSeparator1,
			this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			//
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(57, 6);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(192, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(195, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.MemoryDomainsMenuItem,
			this.DataSizeMenuItem,
			this.BigEndianMenuItem,
			this.toolStripSeparator4,
			this.TurboWhileBottingMenuItem});
			this.OptionsSubMenu.Name = "OptionsSubMenu";
			this.OptionsSubMenu.Size = new System.Drawing.Size(61, 20);
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// MemoryDomainsMenuItem
			// 
			this.MemoryDomainsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripSeparator3});
			this.MemoryDomainsMenuItem.Name = "MemoryDomainsMenuItem";
			this.MemoryDomainsMenuItem.Size = new System.Drawing.Size(181, 22);
			this.MemoryDomainsMenuItem.Text = "Memory Domains";
			this.MemoryDomainsMenuItem.DropDownOpened += new System.EventHandler(this.MemoryDomainsMenuItem_DropDownOpened);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(57, 6);
			// 
			// DataSizeMenuItem
			// 
			this.DataSizeMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this._1ByteMenuItem,
			this._2ByteMenuItem,
			this._4ByteMenuItem});
			this.DataSizeMenuItem.Name = "DataSizeMenuItem";
			this.DataSizeMenuItem.Size = new System.Drawing.Size(181, 22);
			this.DataSizeMenuItem.Text = "Data Size";
			this.DataSizeMenuItem.DropDownOpened += new System.EventHandler(this.DataSizeMenuItem_DropDownOpened);
			// 
			// _1ByteMenuItem
			// 
			this._1ByteMenuItem.Name = "_1ByteMenuItem";
			this._1ByteMenuItem.Size = new System.Drawing.Size(111, 22);
			this._1ByteMenuItem.Text = "1 Byte";
			this._1ByteMenuItem.Click += new System.EventHandler(this._1ByteMenuItem_Click);
			// 
			// _2ByteMenuItem
			// 
			this._2ByteMenuItem.Name = "_2ByteMenuItem";
			this._2ByteMenuItem.Size = new System.Drawing.Size(111, 22);
			this._2ByteMenuItem.Text = "2 Bytes";
			this._2ByteMenuItem.Click += new System.EventHandler(this._2ByteMenuItem_Click);
			// 
			// _4ByteMenuItem
			// 
			this._4ByteMenuItem.Name = "_4ByteMenuItem";
			this._4ByteMenuItem.Size = new System.Drawing.Size(111, 22);
			this._4ByteMenuItem.Text = "4 Bytes";
			this._4ByteMenuItem.Click += new System.EventHandler(this._4ByteMenuItem_Click);
			// 
			// BigEndianMenuItem
			// 
			this.BigEndianMenuItem.Name = "BigEndianMenuItem";
			this.BigEndianMenuItem.Size = new System.Drawing.Size(181, 22);
			this.BigEndianMenuItem.Text = "Big Endian";
			this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
			// 
			// toolStripSeparator4
			// 
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(178, 6);
			// 
			// TurboWhileBottingMenuItem
			// 
			this.TurboWhileBottingMenuItem.Name = "TurboWhileBottingMenuItem";
			this.TurboWhileBottingMenuItem.Size = new System.Drawing.Size(181, 22);
			this.TurboWhileBottingMenuItem.Text = "Turbo While Botting";
			this.TurboWhileBottingMenuItem.Click += new System.EventHandler(this.TurboWhileBottingMenuItem_Click);
			// 
			// RunBtn
			// 
			this.RunBtn.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Play;
			this.RunBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.RunBtn.Location = new System.Drawing.Point(6, 56);
			this.RunBtn.Name = "RunBtn";
			this.RunBtn.Size = new System.Drawing.Size(75, 23);
			this.RunBtn.TabIndex = 2001;
			this.RunBtn.Text = "&Run";
			this.RunBtn.UseVisualStyleBackColor = true;
			this.RunBtn.Click += new System.EventHandler(this.RunBtn_Click);
			// 
			// BotStatusStrip
			// 
			this.BotStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.BotStatusButton,
			this.MessageLabel});
			this.BotStatusStrip.Location = new System.Drawing.Point(0, 565);
			this.BotStatusStrip.Name = "BotStatusStrip";
			this.BotStatusStrip.Size = new System.Drawing.Size(687, 22);
			this.BotStatusStrip.TabIndex = 2;
			this.BotStatusStrip.Text = "statusStrip1";
			// 
			// BotStatusButton
			// 
			this.BotStatusButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.BotStatusButton.Image = ((System.Drawing.Image)(resources.GetObject("BotStatusButton.Image")));
			this.BotStatusButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.BotStatusButton.Name = "BotStatusButton";
			this.BotStatusButton.RightToLeftAutoMirrorImage = true;
			this.BotStatusButton.Size = new System.Drawing.Size(16, 17);
			this.BotStatusButton.Text = " ";
			this.BotStatusButton.ToolTipText = " ";
			// 
			// MessageLabel
			// 
			this.MessageLabel.Name = "MessageLabel";
			this.MessageLabel.Size = new System.Drawing.Size(109, 17);
			this.MessageLabel.Text = "                                  ";
			
			
		
			
			

			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 2);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(51, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "Attempts:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(10, 17);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(44, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Frames:";
			// 
			// AttemptsLabel
			// 
			this.AttemptsLabel.AutoSize = true;
			this.AttemptsLabel.Location = new System.Drawing.Point(61, 2);
			this.AttemptsLabel.Name = "AttemptsLabel";
			this.AttemptsLabel.Size = new System.Drawing.Size(13, 13);
			this.AttemptsLabel.TabIndex = 7;
			this.AttemptsLabel.Text = "0";
			// 
			// FramesLabel
			// 
			this.FramesLabel.AutoSize = true;
			this.FramesLabel.Location = new System.Drawing.Point(61, 17);
			this.FramesLabel.Name = "FramesLabel";
			this.FramesLabel.Size = new System.Drawing.Size(13, 13);
			this.FramesLabel.TabIndex = 8;
			this.FramesLabel.Text = "0";

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
			// StopBtn
			// 
			this.StopBtn.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Stop;
			this.StopBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.StopBtn.Location = new System.Drawing.Point(6, 56);
			this.StopBtn.Name = "StopBtn";
			this.StopBtn.Size = new System.Drawing.Size(75, 23);
			this.StopBtn.TabIndex = 2002;
			this.StopBtn.Text = "&Stop";
			this.StopBtn.UseVisualStyleBackColor = true;
			this.StopBtn.Visible = false;
			this.StopBtn.Click += new System.EventHandler(this.StopBtn_Click);

			// 
			// ControlGroupBox
			// 
			this.ControlGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ControlGroupBox.Controls.Add(this.panel2);
			this.ControlGroupBox.Controls.Add(this.StopBtn);
			this.ControlGroupBox.Controls.Add(this.RunBtn);
			this.ControlGroupBox.Controls.Add(this.label8);
			this.ControlGroupBox.Location = new System.Drawing.Point(440, 27);
			this.ControlGroupBox.Name = "ControlGroupBox";
			this.ControlGroupBox.Size = new System.Drawing.Size(230, 150);
			this.ControlGroupBox.TabIndex = 2004;
			this.ControlGroupBox.TabStop = false;
			this.ControlGroupBox.Text = "Control";
			// 
			// panel2
			// 
			this.panel2.ContextMenuStrip = this.StatsContextMenu;
			this.panel2.Controls.Add(this.label1);
			this.panel2.Controls.Add(this.label2);
			this.panel2.Controls.Add(this.FramesLabel);
			this.panel2.Controls.Add(this.AttemptsLabel);
			this.panel2.Location = new System.Drawing.Point(6, 85);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(140, 33);
			this.panel2.TabIndex = 2003;
			// 
			// StatsContextMenu
			// 
			this.StatsContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.ClearStatsContextMenuItem});
			this.StatsContextMenu.Name = "StatsContextMenu";
			this.StatsContextMenu.Size = new System.Drawing.Size(102, 26);
			// 
			// ClearStatsContextMenuItem
			// 
			this.ClearStatsContextMenuItem.Name = "ClearStatsContextMenuItem";
			this.ClearStatsContextMenuItem.Size = new System.Drawing.Size(101, 22);
			this.ClearStatsContextMenuItem.Text = "&Clear";
			this.ClearStatsContextMenuItem.Click += new System.EventHandler(this.ClearStatsContextMenuItem_Click);
			// 
			// GyroscopeBasicBot
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(687, 587);
			this.Controls.Add(this.ControlGroupBox);

			this.Controls.Add(this.BotStatusStrip);
			this.Controls.Add(this.BotMenu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.BotMenu;
			this.Name = "GyroscopeBot";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Gyroscope Bot";
			this.Load += new System.EventHandler(this.GyroscopeBot_Load);
			this.BotMenu.ResumeLayout(false);
			this.BotMenu.PerformLayout();
			this.BotStatusStrip.ResumeLayout(false);
			this.BotStatusStrip.PerformLayout();

	

			this.ControlGroupBox.ResumeLayout(false);
			this.ControlGroupBox.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.StatsContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private MenuStripEx BotMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.Button RunBtn;

		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.StatusStrip BotStatusStrip;
	
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label AttemptsLabel;
		private System.Windows.Forms.Label FramesLabel;
		private System.Windows.Forms.ToolStripMenuItem OptionsSubMenu;
		private System.Windows.Forms.Label label6;

		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button StopBtn;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label10;

		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.Label label13;

		
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		
		private System.Windows.Forms.ToolStripStatusLabel MessageLabel;
		private System.Windows.Forms.GroupBox ControlGroupBox;
		private System.Windows.Forms.ToolStripMenuItem TurboWhileBottingMenuItem;
		private System.Windows.Forms.ToolStripMenuItem MemoryDomainsMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.ContextMenuStrip StatsContextMenu;
		private System.Windows.Forms.ToolStripMenuItem ClearStatsContextMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel BotStatusButton;
		private System.Windows.Forms.ToolStripMenuItem BigEndianMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem DataSizeMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _1ByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _2ByteMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _4ByteMenuItem;

		private System.Windows.Forms.Panel panel6;

		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label7;
	
		private System.Windows.Forms.Panel panel5;

		private System.Windows.Forms.Panel panel3;

	}
}
