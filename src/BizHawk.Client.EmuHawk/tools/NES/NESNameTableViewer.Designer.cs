using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
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
			this.components = new System.ComponentModel.Container();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.NameTableView = new BizHawk.Client.EmuHawk.NameTableViewer();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ScreenshotAsContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.SaveImageClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.RefreshImageContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.menuStrip1 = new MenuStripEx();
			this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ScreenshotMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.ScreenshotToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ExitMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.txtScanline = new System.Windows.Forms.TextBox();
			this.rbNametableNW = new System.Windows.Forms.RadioButton();
			this.rbNametableNE = new System.Windows.Forms.RadioButton();
			this.rbNametableSW = new System.Windows.Forms.RadioButton();
			this.rbNametableSE = new System.Windows.Forms.RadioButton();
			this.rbNametableAll = new System.Windows.Forms.RadioButton();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.PaletteLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.TableLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.PPUAddressLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.XYLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.TileIDLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.RefreshRate = new System.Windows.Forms.TrackBar();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.groupBox1.SuspendLayout();
			this.contextMenuStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).BeginInit();
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
			this.NameTableView.BackColor = System.Drawing.Color.Transparent;
			this.NameTableView.ContextMenuStrip = this.contextMenuStrip1;
			this.NameTableView.Location = new System.Drawing.Point(17, 19);
			this.NameTableView.Name = "NameTableView";
			this.NameTableView.Size = new System.Drawing.Size(512, 480);
			this.NameTableView.TabIndex = 0;
			this.NameTableView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NesNameTableViewer_KeyDown);
			this.NameTableView.MouseLeave += new System.EventHandler(this.NameTableView_MouseLeave);
			this.NameTableView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.NameTableView_MouseMove);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScreenshotAsContextMenuItem,
            this.SaveImageClipboardMenuItem,
            this.RefreshImageContextMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(248, 70);
			// 
			// ScreenshotAsContextMenuItem
			// 
			this.ScreenshotAsContextMenuItem.Text = "&Save Image...";
			this.ScreenshotAsContextMenuItem.Click += new System.EventHandler(this.ScreenshotMenuItem_Click);
			// 
			// SaveImageClipboardMenuItem
			// 
			this.SaveImageClipboardMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
			this.SaveImageClipboardMenuItem.Text = "&Copy Image to clipboard";
			this.SaveImageClipboardMenuItem.Click += new System.EventHandler(this.ScreenshotToClipboardMenuItem_Click);
			// 
			// RefreshImageContextMenuItem
			// 
			this.RefreshImageContextMenuItem.Text = "&Refresh Image";
			this.RefreshImageContextMenuItem.Click += new System.EventHandler(this.RefreshImageContextMenuItem_Click);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScreenshotMenuItem,
            this.ScreenshotToClipboardMenuItem,
            this.toolStripSeparator2,
            this.ExitMenuItem});
			this.FileSubMenu.Text = "&File";
			// 
			// ScreenshotMenuItem
			// 
			this.ScreenshotMenuItem.Text = "Save Screenshot &As...";
			this.ScreenshotMenuItem.Click += new System.EventHandler(this.ScreenshotMenuItem_Click);
			// 
			// ScreenshotToClipboardMenuItem
			// 
			this.ScreenshotToClipboardMenuItem.ShortcutKeyDisplayString = "Ctrl+C";
			this.ScreenshotToClipboardMenuItem.Text = "Screenshot to &Clipboard";
			this.ScreenshotToClipboardMenuItem.Click += new System.EventHandler(this.ScreenshotToClipboardMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(240, 6);
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.ExitMenuItem.Text = "E&xit";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// txtScanline
			// 
			this.txtScanline.Location = new System.Drawing.Point(4, 19);
			this.txtScanline.Name = "txtScanline";
			this.txtScanline.Size = new System.Drawing.Size(60, 20);
			this.txtScanline.TabIndex = 2;
			this.txtScanline.Text = "0";
			this.txtScanline.TextChanged += new System.EventHandler(this.ScanlineTextBox_TextChanged);
			// 
			// rbNametableNW
			// 
			this.rbNametableNW.AutoSize = true;
			this.rbNametableNW.Location = new System.Drawing.Point(6, 19);
			this.rbNametableNW.Name = "rbNametableNW";
			this.rbNametableNW.Size = new System.Drawing.Size(14, 13);
			this.rbNametableNW.TabIndex = 4;
			this.toolTip1.SetToolTip(this.rbNametableNW, "0x2000");
			this.rbNametableNW.UseVisualStyleBackColor = true;
			this.rbNametableNW.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
			// 
			// rbNametableNE
			// 
			this.rbNametableNE.AutoSize = true;
			this.rbNametableNE.Location = new System.Drawing.Point(56, 19);
			this.rbNametableNE.Name = "rbNametableNE";
			this.rbNametableNE.Size = new System.Drawing.Size(14, 13);
			this.rbNametableNE.TabIndex = 5;
			this.toolTip1.SetToolTip(this.rbNametableNE, "0x2400");
			this.rbNametableNE.UseVisualStyleBackColor = true;
			this.rbNametableNE.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
			// 
			// rbNametableSW
			// 
			this.rbNametableSW.AutoSize = true;
			this.rbNametableSW.Location = new System.Drawing.Point(6, 57);
			this.rbNametableSW.Name = "rbNametableSW";
			this.rbNametableSW.Size = new System.Drawing.Size(14, 13);
			this.rbNametableSW.TabIndex = 6;
			this.toolTip1.SetToolTip(this.rbNametableSW, "0x2800");
			this.rbNametableSW.UseVisualStyleBackColor = true;
			this.rbNametableSW.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
			// 
			// rbNametableSE
			// 
			this.rbNametableSE.AutoSize = true;
			this.rbNametableSE.Location = new System.Drawing.Point(56, 57);
			this.rbNametableSE.Name = "rbNametableSE";
			this.rbNametableSE.Size = new System.Drawing.Size(14, 13);
			this.rbNametableSE.TabIndex = 7;
			this.toolTip1.SetToolTip(this.rbNametableSE, "0x2C00");
			this.rbNametableSE.UseVisualStyleBackColor = true;
			this.rbNametableSE.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
			// 
			// rbNametableAll
			// 
			this.rbNametableAll.AutoSize = true;
			this.rbNametableAll.Checked = true;
			this.rbNametableAll.Location = new System.Drawing.Point(31, 38);
			this.rbNametableAll.Name = "rbNametableAll";
			this.rbNametableAll.Size = new System.Drawing.Size(14, 13);
			this.rbNametableAll.TabIndex = 9;
			this.rbNametableAll.TabStop = true;
			this.toolTip1.SetToolTip(this.rbNametableAll, "All");
			this.rbNametableAll.UseVisualStyleBackColor = true;
			this.rbNametableAll.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.rbNametableNW);
			this.groupBox2.Controls.Add(this.rbNametableNE);
			this.groupBox2.Controls.Add(this.rbNametableAll);
			this.groupBox2.Controls.Add(this.rbNametableSW);
			this.groupBox2.Controls.Add(this.rbNametableSE);
			this.groupBox2.Location = new System.Drawing.Point(563, 94);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(76, 79);
			this.groupBox2.TabIndex = 11;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Nametable";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.txtScanline);
			this.groupBox3.Location = new System.Drawing.Point(563, 36);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(76, 52);
			this.groupBox3.TabIndex = 12;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Scanline";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.PaletteLabel);
			this.groupBox4.Controls.Add(this.label5);
			this.groupBox4.Controls.Add(this.TableLabel);
			this.groupBox4.Controls.Add(this.label4);
			this.groupBox4.Controls.Add(this.PPUAddressLabel);
			this.groupBox4.Controls.Add(this.XYLabel);
			this.groupBox4.Controls.Add(this.TileIDLabel);
			this.groupBox4.Controls.Add(this.label3);
			this.groupBox4.Controls.Add(this.label2);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Location = new System.Drawing.Point(563, 179);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(108, 128);
			this.groupBox4.TabIndex = 13;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Properties";
			// 
			// PaletteLabel
			// 
			this.PaletteLabel.Location = new System.Drawing.Point(64, 96);
			this.PaletteLabel.Name = "PaletteLabel";
			this.PaletteLabel.Text = "     ";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(6, 96);
			this.label5.Name = "label5";
			this.label5.Text = "Palette:";
			// 
			// TableLabel
			// 
			this.TableLabel.Location = new System.Drawing.Point(64, 78);
			this.TableLabel.Name = "TableLabel";
			this.TableLabel.Text = "     ";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(6, 78);
			this.label4.Name = "label4";
			this.label4.Text = "Table:";
			// 
			// PPUAddressLabel
			// 
			this.PPUAddressLabel.Location = new System.Drawing.Point(64, 60);
			this.PPUAddressLabel.Name = "PPUAddressLabel";
			this.PPUAddressLabel.Text = "     ";
			// 
			// XYLabel
			// 
			this.XYLabel.Location = new System.Drawing.Point(64, 43);
			this.XYLabel.Name = "XYLabel";
			this.XYLabel.Text = "     ";
			// 
			// TileIDLabel
			// 
			this.TileIDLabel.Location = new System.Drawing.Point(64, 26);
			this.TileIDLabel.Name = "TileIDLabel";
			this.TileIDLabel.Text = "     ";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(6, 60);
			this.label3.Name = "label3";
			this.label3.Text = "PPU Addr:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(6, 43);
			this.label2.Name = "label2";
			this.label2.Text = "X / Y:";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 26);
			this.label1.Name = "label1";
			this.label1.Text = "Tile ID:";
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.label7);
			this.groupBox5.Controls.Add(this.label6);
			this.groupBox5.Controls.Add(this.RefreshRate);
			this.groupBox5.Location = new System.Drawing.Point(563, 313);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(108, 236);
			this.groupBox5.TabIndex = 14;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Refresh";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(7, 186);
			this.label7.Name = "label7";
			this.label7.Text = "More";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(7, 32);
			this.label6.Name = "label6";
			this.label6.Text = "Less";
			// 
			// RefreshRate
			// 
			this.RefreshRate.LargeChange = 2;
			this.RefreshRate.Location = new System.Drawing.Point(9, 47);
			this.RefreshRate.Maximum = 8;
			this.RefreshRate.Minimum = 1;
			this.RefreshRate.Name = "RefreshRate";
			this.RefreshRate.Orientation = System.Windows.Forms.Orientation.Vertical;
			this.RefreshRate.Size = new System.Drawing.Size(45, 136);
			this.RefreshRate.TabIndex = 0;
			this.RefreshRate.TickFrequency = 4;
			this.RefreshRate.Value = 1;
			// 
			// NESNameTableViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(679, 561);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.MinimumSize = new System.Drawing.Size(687, 588);
			this.Name = "NESNameTableViewer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Nametable Viewer";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NESNameTableViewer_FormClosed);
			this.Load += new System.EventHandler(this.NESNameTableViewer_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NesNameTableViewer_KeyDown);
			this.groupBox1.ResumeLayout(false);
			this.contextMenuStrip1.ResumeLayout(false);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private NameTableViewer NameTableView;
		private MenuStripEx menuStrip1;
		private System.Windows.Forms.TextBox txtScanline;
		private System.Windows.Forms.RadioButton rbNametableNW;
		private System.Windows.Forms.RadioButton rbNametableNE;
		private System.Windows.Forms.RadioButton rbNametableSW;
		private System.Windows.Forms.RadioButton rbNametableSE;
		private System.Windows.Forms.RadioButton rbNametableAll;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox4;
		private BizHawk.WinForms.Controls.LocLabelEx PPUAddressLabel;
		private BizHawk.WinForms.Controls.LocLabelEx XYLabel;
		private BizHawk.WinForms.Controls.LocLabelEx TileIDLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx TableLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx PaletteLabel;
		private System.Windows.Forms.GroupBox groupBox5;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.TrackBar RefreshRate;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScreenshotMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExitMenuItem;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScreenshotAsContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RefreshImageContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveImageClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScreenshotToClipboardMenuItem;
		private System.Windows.Forms.ToolTip toolTip1;
	}
}