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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NESNameTableViewer));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.NameTableView = new BizHawk.Client.EmuHawk.NameTableViewer();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ScreenshotAsContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveImageClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RefreshImageContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ScreenshotMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ScreenshotToClipboardMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
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
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.NameTableView);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox1, resources.GetString("groupBox1.ToolTip"));
            // 
            // NameTableView
            // 
            resources.ApplyResources(this.NameTableView, "NameTableView");
            this.NameTableView.BackColor = System.Drawing.Color.Transparent;
            this.NameTableView.ContextMenuStrip = this.contextMenuStrip1;
            this.NameTableView.Name = "NameTableView";
            this.toolTip1.SetToolTip(this.NameTableView, resources.GetString("NameTableView.ToolTip"));
            this.NameTableView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NesNameTableViewer_KeyDown);
            this.NameTableView.MouseLeave += new System.EventHandler(this.NameTableView_MouseLeave);
            this.NameTableView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.NameTableView_MouseMove);
            // 
            // contextMenuStrip1
            // 
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScreenshotAsContextMenuItem,
            this.SaveImageClipboardMenuItem,
            this.RefreshImageContextMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.toolTip1.SetToolTip(this.contextMenuStrip1, resources.GetString("contextMenuStrip1.ToolTip"));
            // 
            // ScreenshotAsContextMenuItem
            // 
            resources.ApplyResources(this.ScreenshotAsContextMenuItem, "ScreenshotAsContextMenuItem");
            this.ScreenshotAsContextMenuItem.Click += new System.EventHandler(this.ScreenshotMenuItem_Click);
            // 
            // SaveImageClipboardMenuItem
            // 
            resources.ApplyResources(this.SaveImageClipboardMenuItem, "SaveImageClipboardMenuItem");
            this.SaveImageClipboardMenuItem.Click += new System.EventHandler(this.ScreenshotToClipboardMenuItem_Click);
            // 
            // RefreshImageContextMenuItem
            // 
            resources.ApplyResources(this.RefreshImageContextMenuItem, "RefreshImageContextMenuItem");
            this.RefreshImageContextMenuItem.Click += new System.EventHandler(this.RefreshImageContextMenuItem_Click);
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
            this.toolTip1.SetToolTip(this.menuStrip1, resources.GetString("menuStrip1.ToolTip"));
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ScreenshotMenuItem,
            this.ScreenshotToClipboardMenuItem});
            // 
            // ScreenshotMenuItem
            // 
            resources.ApplyResources(this.ScreenshotMenuItem, "ScreenshotMenuItem");
            this.ScreenshotMenuItem.Click += new System.EventHandler(this.ScreenshotMenuItem_Click);
            // 
            // ScreenshotToClipboardMenuItem
            // 
            resources.ApplyResources(this.ScreenshotToClipboardMenuItem, "ScreenshotToClipboardMenuItem");
            this.ScreenshotToClipboardMenuItem.Click += new System.EventHandler(this.ScreenshotToClipboardMenuItem_Click);
            // 
            // txtScanline
            // 
            resources.ApplyResources(this.txtScanline, "txtScanline");
            this.txtScanline.Name = "txtScanline";
            this.toolTip1.SetToolTip(this.txtScanline, resources.GetString("txtScanline.ToolTip"));
            this.txtScanline.TextChanged += new System.EventHandler(this.ScanlineTextBox_TextChanged);
            // 
            // rbNametableNW
            // 
            resources.ApplyResources(this.rbNametableNW, "rbNametableNW");
            this.rbNametableNW.Name = "rbNametableNW";
            this.toolTip1.SetToolTip(this.rbNametableNW, resources.GetString("rbNametableNW.ToolTip"));
            this.rbNametableNW.UseVisualStyleBackColor = true;
            this.rbNametableNW.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
            // 
            // rbNametableNE
            // 
            resources.ApplyResources(this.rbNametableNE, "rbNametableNE");
            this.rbNametableNE.Name = "rbNametableNE";
            this.toolTip1.SetToolTip(this.rbNametableNE, resources.GetString("rbNametableNE.ToolTip"));
            this.rbNametableNE.UseVisualStyleBackColor = true;
            this.rbNametableNE.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
            // 
            // rbNametableSW
            // 
            resources.ApplyResources(this.rbNametableSW, "rbNametableSW");
            this.rbNametableSW.Name = "rbNametableSW";
            this.toolTip1.SetToolTip(this.rbNametableSW, resources.GetString("rbNametableSW.ToolTip"));
            this.rbNametableSW.UseVisualStyleBackColor = true;
            this.rbNametableSW.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
            // 
            // rbNametableSE
            // 
            resources.ApplyResources(this.rbNametableSE, "rbNametableSE");
            this.rbNametableSE.Name = "rbNametableSE";
            this.toolTip1.SetToolTip(this.rbNametableSE, resources.GetString("rbNametableSE.ToolTip"));
            this.rbNametableSE.UseVisualStyleBackColor = true;
            this.rbNametableSE.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
            // 
            // rbNametableAll
            // 
            resources.ApplyResources(this.rbNametableAll, "rbNametableAll");
            this.rbNametableAll.Checked = true;
            this.rbNametableAll.Name = "rbNametableAll";
            this.rbNametableAll.TabStop = true;
            this.toolTip1.SetToolTip(this.rbNametableAll, resources.GetString("rbNametableAll.ToolTip"));
            this.rbNametableAll.UseVisualStyleBackColor = true;
            this.rbNametableAll.CheckedChanged += new System.EventHandler(this.NametableRadio_CheckedChanged);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.rbNametableNW);
            this.groupBox2.Controls.Add(this.rbNametableNE);
            this.groupBox2.Controls.Add(this.rbNametableAll);
            this.groupBox2.Controls.Add(this.rbNametableSW);
            this.groupBox2.Controls.Add(this.rbNametableSE);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox2, resources.GetString("groupBox2.ToolTip"));
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.txtScanline);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox3, resources.GetString("groupBox3.ToolTip"));
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
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
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox4, resources.GetString("groupBox4.ToolTip"));
            // 
            // PaletteLabel
            // 
            resources.ApplyResources(this.PaletteLabel, "PaletteLabel");
            this.PaletteLabel.Name = "PaletteLabel";
            this.toolTip1.SetToolTip(this.PaletteLabel, resources.GetString("PaletteLabel.ToolTip"));
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            this.toolTip1.SetToolTip(this.label5, resources.GetString("label5.ToolTip"));
            // 
            // TableLabel
            // 
            resources.ApplyResources(this.TableLabel, "TableLabel");
            this.TableLabel.Name = "TableLabel";
            this.toolTip1.SetToolTip(this.TableLabel, resources.GetString("TableLabel.ToolTip"));
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // PPUAddressLabel
            // 
            resources.ApplyResources(this.PPUAddressLabel, "PPUAddressLabel");
            this.PPUAddressLabel.Name = "PPUAddressLabel";
            this.toolTip1.SetToolTip(this.PPUAddressLabel, resources.GetString("PPUAddressLabel.ToolTip"));
            // 
            // XYLabel
            // 
            resources.ApplyResources(this.XYLabel, "XYLabel");
            this.XYLabel.Name = "XYLabel";
            this.toolTip1.SetToolTip(this.XYLabel, resources.GetString("XYLabel.ToolTip"));
            // 
            // TileIDLabel
            // 
            resources.ApplyResources(this.TileIDLabel, "TileIDLabel");
            this.TileIDLabel.Name = "TileIDLabel";
            this.toolTip1.SetToolTip(this.TileIDLabel, resources.GetString("TileIDLabel.ToolTip"));
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.label7);
            this.groupBox5.Controls.Add(this.label6);
            this.groupBox5.Controls.Add(this.RefreshRate);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox5, resources.GetString("groupBox5.ToolTip"));
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            this.toolTip1.SetToolTip(this.label7, resources.GetString("label7.ToolTip"));
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            this.toolTip1.SetToolTip(this.label6, resources.GetString("label6.ToolTip"));
            // 
            // RefreshRate
            // 
            resources.ApplyResources(this.RefreshRate, "RefreshRate");
            this.RefreshRate.LargeChange = 2;
            this.RefreshRate.Maximum = 8;
            this.RefreshRate.Minimum = 1;
            this.RefreshRate.Name = "RefreshRate";
            this.RefreshRate.TickFrequency = 4;
            this.toolTip1.SetToolTip(this.RefreshRate, resources.GetString("RefreshRate.ToolTip"));
            this.RefreshRate.Value = 1;
            // 
            // NESNameTableViewer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "NESNameTableViewer";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
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
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScreenshotAsContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RefreshImageContextMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveImageClipboardMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScreenshotToClipboardMenuItem;
		private System.Windows.Forms.ToolTip toolTip1;
	}
}