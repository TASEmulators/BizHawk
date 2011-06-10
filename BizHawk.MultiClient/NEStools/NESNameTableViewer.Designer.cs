namespace BizHawk.MultiClient
{
	partial class NESNameTableViewer
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.NameTableView = new BizHawk.MultiClient.NameTableViewer();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.txtScanline = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.rbNametableNW = new System.Windows.Forms.RadioButton();
			this.rbNametableNE = new System.Windows.Forms.RadioButton();
			this.rbNametableSW = new System.Windows.Forms.RadioButton();
			this.rbNametableSE = new System.Windows.Forms.RadioButton();
			this.label2 = new System.Windows.Forms.Label();
			this.rbNametableAll = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.NameTableView);
			this.groupBox1.Location = new System.Drawing.Point(12, 36);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(545, 513);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			// 
			// NameTableView
			// 
			this.NameTableView.BackColor = System.Drawing.Color.White;
			this.NameTableView.Location = new System.Drawing.Point(17, 19);
			this.NameTableView.Name = "NameTableView";
			this.NameTableView.Size = new System.Drawing.Size(512, 480);
			this.NameTableView.TabIndex = 0;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(668, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoloadToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.autoloadToolStripMenuItem.Text = "Auto-load";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(187, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// txtScanline
			// 
			this.txtScanline.Location = new System.Drawing.Point(578, 64);
			this.txtScanline.Name = "txtScanline";
			this.txtScanline.Size = new System.Drawing.Size(60, 20);
			this.txtScanline.TabIndex = 2;
			this.txtScanline.Text = "0";
			this.txtScanline.TextChanged += new System.EventHandler(this.txtScanline_TextChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(579, 45);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Scanline";
			// 
			// rbNametableNW
			// 
			this.rbNametableNW.AutoSize = true;
			this.rbNametableNW.Location = new System.Drawing.Point(592, 115);
			this.rbNametableNW.Name = "rbNametableNW";
			this.rbNametableNW.Size = new System.Drawing.Size(14, 13);
			this.rbNametableNW.TabIndex = 4;
			this.rbNametableNW.UseVisualStyleBackColor = true;
			this.rbNametableNW.CheckedChanged += new System.EventHandler(this.rbNametable_CheckedChanged);
			// 
			// rbNametableNE
			// 
			this.rbNametableNE.AutoSize = true;
			this.rbNametableNE.Location = new System.Drawing.Point(612, 115);
			this.rbNametableNE.Name = "rbNametableNE";
			this.rbNametableNE.Size = new System.Drawing.Size(14, 13);
			this.rbNametableNE.TabIndex = 5;
			this.rbNametableNE.UseVisualStyleBackColor = true;
			this.rbNametableNE.CheckedChanged += new System.EventHandler(this.rbNametable_CheckedChanged);
			// 
			// rbNametableSW
			// 
			this.rbNametableSW.AutoSize = true;
			this.rbNametableSW.Location = new System.Drawing.Point(592, 134);
			this.rbNametableSW.Name = "rbNametableSW";
			this.rbNametableSW.Size = new System.Drawing.Size(14, 13);
			this.rbNametableSW.TabIndex = 6;
			this.rbNametableSW.UseVisualStyleBackColor = true;
			this.rbNametableSW.CheckedChanged += new System.EventHandler(this.rbNametable_CheckedChanged);
			// 
			// rbNametableSE
			// 
			this.rbNametableSE.AutoSize = true;
			this.rbNametableSE.Location = new System.Drawing.Point(612, 134);
			this.rbNametableSE.Name = "rbNametableSE";
			this.rbNametableSE.Size = new System.Drawing.Size(14, 13);
			this.rbNametableSE.TabIndex = 7;
			this.rbNametableSE.UseVisualStyleBackColor = true;
			this.rbNametableSE.CheckedChanged += new System.EventHandler(this.rbNametable_CheckedChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(579, 99);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(58, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Nametable";
			// 
			// rbNametableAll
			// 
			this.rbNametableAll.AutoSize = true;
			this.rbNametableAll.Checked = true;
			this.rbNametableAll.Location = new System.Drawing.Point(582, 153);
			this.rbNametableAll.Name = "rbNametableAll";
			this.rbNametableAll.Size = new System.Drawing.Size(14, 13);
			this.rbNametableAll.TabIndex = 9;
			this.rbNametableAll.TabStop = true;
			this.rbNametableAll.UseVisualStyleBackColor = true;
			this.rbNametableAll.CheckedChanged += new System.EventHandler(this.rbNametable_CheckedChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(602, 153);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(30, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "<- All";
			// 
			// NESNameTableViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(668, 561);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.rbNametableAll);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.rbNametableSE);
			this.Controls.Add(this.rbNametableSW);
			this.Controls.Add(this.rbNametableNE);
			this.Controls.Add(this.rbNametableNW);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtScanline);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "NESNameTableViewer";
			this.Text = "NES Nametable Viewer";
			this.Load += new System.EventHandler(this.NESNameTableViewer_Load);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NESNameTableViewer_FormClosed);
			this.groupBox1.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private NameTableViewer NameTableView;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.TextBox txtScanline;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton rbNametableNW;
		private System.Windows.Forms.RadioButton rbNametableNE;
		private System.Windows.Forms.RadioButton rbNametableSW;
		private System.Windows.Forms.RadioButton rbNametableSE;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RadioButton rbNametableAll;
		private System.Windows.Forms.Label label3;
	}
}