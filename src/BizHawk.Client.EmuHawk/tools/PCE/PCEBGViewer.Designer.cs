using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class PceBgViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PceBgViewer));
            this.PceBgViewerMenu = new BizHawk.WinForms.Controls.MenuStripEx();
            this.ViewerSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.VDC1MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.VDC2MenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.ExitMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.canvas = new BizHawk.Client.EmuHawk.PceBgCanvas();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.RefreshRate = new System.Windows.Forms.TrackBar();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.PaletteLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PPUAddressLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.XYLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.TileIDLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PceBgViewerMenu.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // PceBgViewerMenu
            // 
            resources.ApplyResources(this.PceBgViewerMenu, "PceBgViewerMenu");
            this.PceBgViewerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ViewerSubMenu});
            // 
            // ViewerSubMenu
            // 
            resources.ApplyResources(this.ViewerSubMenu, "ViewerSubMenu");
            this.ViewerSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.VDC1MenuItem,
            this.VDC2MenuItem,
            this.toolStripSeparator1,
            this.ExitMenuItem});
            this.ViewerSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // VDC1MenuItem
            // 
            resources.ApplyResources(this.VDC1MenuItem, "VDC1MenuItem");
            this.VDC1MenuItem.Click += new System.EventHandler(this.VDC1MenuItem_Click);
            // 
            // VDC2MenuItem
            // 
            resources.ApplyResources(this.VDC2MenuItem, "VDC2MenuItem");
            this.VDC2MenuItem.Click += new System.EventHandler(this.VDC2MenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // ExitMenuItem
            // 
            resources.ApplyResources(this.ExitMenuItem, "ExitMenuItem");
            this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.canvas);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // canvas
            // 
            resources.ApplyResources(this.canvas, "canvas");
            this.canvas.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.canvas.Bat = ((System.Drawing.Bitmap)(resources.GetObject("canvas.Bat")));
            this.canvas.Name = "canvas";
            this.canvas.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Canvas_MouseMove);
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.label7);
            this.groupBox5.Controls.Add(this.label6);
            this.groupBox5.Controls.Add(this.RefreshRate);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // RefreshRate
            // 
            resources.ApplyResources(this.RefreshRate, "RefreshRate");
            this.RefreshRate.LargeChange = 2;
            this.RefreshRate.Maximum = 16;
            this.RefreshRate.Minimum = 1;
            this.RefreshRate.Name = "RefreshRate";
            this.RefreshRate.TickFrequency = 4;
            this.RefreshRate.Value = 16;
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.PaletteLabel);
            this.groupBox4.Controls.Add(this.label5);
            this.groupBox4.Controls.Add(this.PPUAddressLabel);
            this.groupBox4.Controls.Add(this.XYLabel);
            this.groupBox4.Controls.Add(this.TileIDLabel);
            this.groupBox4.Controls.Add(this.label2);
            this.groupBox4.Controls.Add(this.label1);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // PaletteLabel
            // 
            resources.ApplyResources(this.PaletteLabel, "PaletteLabel");
            this.PaletteLabel.Name = "PaletteLabel";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // PPUAddressLabel
            // 
            resources.ApplyResources(this.PPUAddressLabel, "PPUAddressLabel");
            this.PPUAddressLabel.Name = "PPUAddressLabel";
            // 
            // XYLabel
            // 
            resources.ApplyResources(this.XYLabel, "XYLabel");
            this.XYLabel.Name = "XYLabel";
            // 
            // TileIDLabel
            // 
            resources.ApplyResources(this.TileIDLabel, "TileIDLabel");
            this.TileIDLabel.Name = "TileIDLabel";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // PceBgViewer
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.PceBgViewerMenu);
            this.MainMenuStrip = this.PceBgViewerMenu;
            this.Name = "PceBgViewer";
            this.PceBgViewerMenu.ResumeLayout(false);
            this.PceBgViewerMenu.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RefreshRate)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private PceBgCanvas canvas;
        private MenuStripEx PceBgViewerMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ViewerSubMenu;
        private BizHawk.WinForms.Controls.ToolStripMenuItemEx ExitMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx VDC1MenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx VDC2MenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox5;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.TrackBar RefreshRate;
		private System.Windows.Forms.GroupBox groupBox4;
		private BizHawk.WinForms.Controls.LocLabelEx PaletteLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx PPUAddressLabel;
		private BizHawk.WinForms.Controls.LocLabelEx XYLabel;
		private BizHawk.WinForms.Controls.LocLabelEx TileIDLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
	}
}