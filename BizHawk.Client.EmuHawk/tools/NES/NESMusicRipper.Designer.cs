namespace BizHawk.Client.EmuHawk
{
	partial class NESMusicRipper
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NESMusicRipper));
			this.btnControl = new System.Windows.Forms.Button();
			this.txtDivider = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.btnExport = new System.Windows.Forms.Button();
			this.lblContents = new System.Windows.Forms.Label();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.txtPatternLength = new System.Windows.Forms.TextBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.menuStrip1 = new MenuStripEx();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnControl
			// 
			this.btnControl.Location = new System.Drawing.Point(6, 57);
			this.btnControl.Name = "btnControl";
			this.btnControl.Size = new System.Drawing.Size(75, 23);
			this.btnControl.TabIndex = 0;
			this.btnControl.Text = "Start";
			this.btnControl.UseVisualStyleBackColor = true;
			this.btnControl.Click += new System.EventHandler(this.btnControl_Click);
			// 
			// txtDivider
			// 
			this.txtDivider.Location = new System.Drawing.Point(9, 32);
			this.txtDivider.Name = "txtDivider";
			this.txtDivider.Size = new System.Drawing.Size(100, 20);
			this.txtDivider.TabIndex = 1;
			this.txtDivider.Text = "29824";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(135, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "APU Divider (trace interval)";
			// 
			// btnExport
			// 
			this.btnExport.AutoSize = true;
			this.btnExport.Location = new System.Drawing.Point(6, 118);
			this.btnExport.Name = "btnExport";
			this.btnExport.Size = new System.Drawing.Size(100, 23);
			this.btnExport.TabIndex = 3;
			this.btnExport.Text = "Export XRNS File";
			this.btnExport.UseVisualStyleBackColor = true;
			this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
			// 
			// lblContents
			// 
			this.lblContents.AutoSize = true;
			this.lblContents.Location = new System.Drawing.Point(6, 102);
			this.lblContents.Name = "lblContents";
			this.lblContents.Size = new System.Drawing.Size(55, 13);
			this.lblContents.TabIndex = 4;
			this.lblContents.Text = "(Contents)";
			// 
			// textBox1
			// 
			this.textBox1.Location = new System.Drawing.Point(12, 211);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(390, 80);
			this.textBox1.TabIndex = 6;
			this.textBox1.Text = resources.GetString("textBox1.Text");
			// 
			// txtPatternLength
			// 
			this.txtPatternLength.Location = new System.Drawing.Point(12, 37);
			this.txtPatternLength.Name = "txtPatternLength";
			this.txtPatternLength.Size = new System.Drawing.Size(100, 20);
			this.txtPatternLength.TabIndex = 7;
			this.txtPatternLength.Text = "512";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.txtPatternLength);
			this.groupBox1.Location = new System.Drawing.Point(0, 27);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(200, 156);
			this.groupBox1.TabIndex = 8;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Config";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 21);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(126, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Pattern Length (512 max)";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.btnControl);
			this.groupBox2.Controls.Add(this.txtDivider);
			this.groupBox2.Controls.Add(this.btnExport);
			this.groupBox2.Controls.Add(this.lblContents);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Location = new System.Drawing.Point(206, 32);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(200, 151);
			this.groupBox2.TabIndex = 9;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Log Control";
			// 
			// menuStrip1
			// 
			this.menuStrip1.ClickThrough = true;
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(437, 24);
			this.menuStrip1.TabIndex = 5;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2,
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(131, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.ExitMenuItem.Size = new System.Drawing.Size(134, 22);
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// NESMusicRipper
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(437, 305);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "NESMusicRipper";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Music Ripper";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NESMusicRipper_FormClosed);
			this.Load += new System.EventHandler(this.NESMusicRipper_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnControl;
		private System.Windows.Forms.TextBox txtDivider;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnExport;
		private System.Windows.Forms.Label lblContents;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.TextBox txtPatternLength;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox2;
	}
}