namespace BizHawk.MultiClient
{
    partial class NESPPU
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NESPPU));
			this.PatternGroup = new System.Windows.Forms.GroupBox();
			this.Table1PaletteLabel = new System.Windows.Forms.Label();
			this.Table0PaletteLabel = new System.Windows.Forms.Label();
			this.PalettesGroup = new System.Windows.Forms.GroupBox();
			this.DetailsBox = new System.Windows.Forms.GroupBox();
			this.Value2Label = new System.Windows.Forms.Label();
			this.ValueLabel = new System.Windows.Forms.Label();
			this.AddressLabel = new System.Windows.Forms.Label();
			this.SectionLabel = new System.Windows.Forms.Label();
			this.toolStrip1 = new ToolStripEx();
			this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripDropDownButton2 = new System.Windows.Forms.ToolStripDropDownButton();
			this.table0PToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P0 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P1 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P2 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P3 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P4 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P5 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P6 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table0P7 = new System.Windows.Forms.ToolStripMenuItem();
			this.table1PaletteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P0 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P1 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P2 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P3 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P4 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P5 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P6 = new System.Windows.Forms.ToolStripMenuItem();
			this.Table1P7 = new System.Windows.Forms.ToolStripMenuItem();
			this.SpriteViewerBox = new System.Windows.Forms.GroupBox();
			this.SpriteView = new BizHawk.MultiClient.SpriteViewer();
			this.PaletteView = new BizHawk.MultiClient.PaletteViewer();
			this.PatternView = new BizHawk.MultiClient.PatternViewer();
			this.label1 = new System.Windows.Forms.Label();
			this.txtScanline = new System.Windows.Forms.TextBox();
			this.PatternGroup.SuspendLayout();
			this.PalettesGroup.SuspendLayout();
			this.DetailsBox.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SpriteViewerBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// PatternGroup
			// 
			this.PatternGroup.Controls.Add(this.Table1PaletteLabel);
			this.PatternGroup.Controls.Add(this.Table0PaletteLabel);
			this.PatternGroup.Controls.Add(this.PatternView);
			this.PatternGroup.Location = new System.Drawing.Point(12, 37);
			this.PatternGroup.Name = "PatternGroup";
			this.PatternGroup.Size = new System.Drawing.Size(272, 169);
			this.PatternGroup.TabIndex = 0;
			this.PatternGroup.TabStop = false;
			this.PatternGroup.Text = "Pattern Tables";
			// 
			// Table1PaletteLabel
			// 
			this.Table1PaletteLabel.AutoSize = true;
			this.Table1PaletteLabel.Location = new System.Drawing.Point(129, 150);
			this.Table1PaletteLabel.Name = "Table1PaletteLabel";
			this.Table1PaletteLabel.Size = new System.Drawing.Size(52, 13);
			this.Table1PaletteLabel.TabIndex = 2;
			this.Table1PaletteLabel.Text = "Palette: 0";
			// 
			// Table0PaletteLabel
			// 
			this.Table0PaletteLabel.AutoSize = true;
			this.Table0PaletteLabel.Location = new System.Drawing.Point(6, 150);
			this.Table0PaletteLabel.Name = "Table0PaletteLabel";
			this.Table0PaletteLabel.Size = new System.Drawing.Size(52, 13);
			this.Table0PaletteLabel.TabIndex = 1;
			this.Table0PaletteLabel.Text = "Palette: 0";
			// 
			// PalettesGroup
			// 
			this.PalettesGroup.Controls.Add(this.PaletteView);
			this.PalettesGroup.Location = new System.Drawing.Point(12, 225);
			this.PalettesGroup.Name = "PalettesGroup";
			this.PalettesGroup.Size = new System.Drawing.Size(272, 65);
			this.PalettesGroup.TabIndex = 1;
			this.PalettesGroup.TabStop = false;
			this.PalettesGroup.Text = "Palettes";
			// 
			// DetailsBox
			// 
			this.DetailsBox.Controls.Add(this.Value2Label);
			this.DetailsBox.Controls.Add(this.ValueLabel);
			this.DetailsBox.Controls.Add(this.AddressLabel);
			this.DetailsBox.Controls.Add(this.SectionLabel);
			this.DetailsBox.Location = new System.Drawing.Point(299, 200);
			this.DetailsBox.Name = "DetailsBox";
			this.DetailsBox.Size = new System.Drawing.Size(177, 90);
			this.DetailsBox.TabIndex = 2;
			this.DetailsBox.TabStop = false;
			this.DetailsBox.Text = "Details";
			// 
			// Value2Label
			// 
			this.Value2Label.AutoSize = true;
			this.Value2Label.Location = new System.Drawing.Point(6, 72);
			this.Value2Label.Name = "Value2Label";
			this.Value2Label.Size = new System.Drawing.Size(35, 13);
			this.Value2Label.TabIndex = 3;
			this.Value2Label.Text = "label1";
			// 
			// ValueLabel
			// 
			this.ValueLabel.AutoSize = true;
			this.ValueLabel.Location = new System.Drawing.Point(6, 53);
			this.ValueLabel.Name = "ValueLabel";
			this.ValueLabel.Size = new System.Drawing.Size(35, 13);
			this.ValueLabel.TabIndex = 2;
			this.ValueLabel.Text = "label1";
			// 
			// AddressLabel
			// 
			this.AddressLabel.AutoSize = true;
			this.AddressLabel.Location = new System.Drawing.Point(6, 35);
			this.AddressLabel.Name = "AddressLabel";
			this.AddressLabel.Size = new System.Drawing.Size(35, 13);
			this.AddressLabel.TabIndex = 1;
			this.AddressLabel.Text = "label1";
			// 
			// SectionLabel
			// 
			this.SectionLabel.AutoSize = true;
			this.SectionLabel.Location = new System.Drawing.Point(6, 18);
			this.SectionLabel.Name = "SectionLabel";
			this.SectionLabel.Size = new System.Drawing.Size(35, 13);
			this.SectionLabel.TabIndex = 0;
			this.SectionLabel.Text = "label1";
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton1,
            this.toolStripDropDownButton2});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(587, 25);
			this.toolStrip1.TabIndex = 3;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// toolStripDropDownButton1
			// 
			this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoloadToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem});
			this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
			this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
			this.toolStripDropDownButton1.Size = new System.Drawing.Size(59, 22);
			this.toolStripDropDownButton1.Text = "Settings";
			this.toolStripDropDownButton1.DropDownOpened += new System.EventHandler(this.toolStripDropDownButton1_DropDownOpened);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
			this.autoloadToolStripMenuItem.Text = "Auto-load";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save window position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// toolStripDropDownButton2
			// 
			this.toolStripDropDownButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.toolStripDropDownButton2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.table0PToolStripMenuItem,
            this.table1PaletteToolStripMenuItem});
			this.toolStripDropDownButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton2.Image")));
			this.toolStripDropDownButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripDropDownButton2.Name = "toolStripDropDownButton2";
			this.toolStripDropDownButton2.Size = new System.Drawing.Size(56, 22);
			this.toolStripDropDownButton2.Text = "Pattern";
			this.toolStripDropDownButton2.DropDownOpened += new System.EventHandler(this.toolStripDropDownButton2_DropDownOpened);
			// 
			// table0PToolStripMenuItem
			// 
			this.table0PToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table0P0,
            this.Table0P1,
            this.Table0P2,
            this.Table0P3,
            this.Table0P4,
            this.Table0P5,
            this.Table0P6,
            this.Table0P7});
			this.table0PToolStripMenuItem.Name = "table0PToolStripMenuItem";
			this.table0PToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.table0PToolStripMenuItem.Text = "Table 0 Palette";
			// 
			// Table0P0
			// 
			this.Table0P0.Name = "Table0P0";
			this.Table0P0.Size = new System.Drawing.Size(91, 22);
			this.Table0P0.Text = "0";
			this.Table0P0.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P1
			// 
			this.Table0P1.Name = "Table0P1";
			this.Table0P1.Size = new System.Drawing.Size(91, 22);
			this.Table0P1.Text = "1";
			this.Table0P1.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P2
			// 
			this.Table0P2.Name = "Table0P2";
			this.Table0P2.Size = new System.Drawing.Size(91, 22);
			this.Table0P2.Text = "2";
			this.Table0P2.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P3
			// 
			this.Table0P3.Name = "Table0P3";
			this.Table0P3.Size = new System.Drawing.Size(91, 22);
			this.Table0P3.Text = "3";
			this.Table0P3.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P4
			// 
			this.Table0P4.Name = "Table0P4";
			this.Table0P4.Size = new System.Drawing.Size(91, 22);
			this.Table0P4.Text = "4";
			this.Table0P4.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P5
			// 
			this.Table0P5.Name = "Table0P5";
			this.Table0P5.Size = new System.Drawing.Size(91, 22);
			this.Table0P5.Text = "5";
			this.Table0P5.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P6
			// 
			this.Table0P6.Name = "Table0P6";
			this.Table0P6.Size = new System.Drawing.Size(91, 22);
			this.Table0P6.Text = "6";
			this.Table0P6.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table0P7
			// 
			this.Table0P7.Name = "Table0P7";
			this.Table0P7.Size = new System.Drawing.Size(91, 22);
			this.Table0P7.Text = "7";
			this.Table0P7.Click += new System.EventHandler(this.Palette_Click);
			// 
			// table1PaletteToolStripMenuItem
			// 
			this.table1PaletteToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Table1P0,
            this.Table1P1,
            this.Table1P2,
            this.Table1P3,
            this.Table1P4,
            this.Table1P5,
            this.Table1P6,
            this.Table1P7});
			this.table1PaletteToolStripMenuItem.Name = "table1PaletteToolStripMenuItem";
			this.table1PaletteToolStripMenuItem.Size = new System.Drawing.Size(157, 22);
			this.table1PaletteToolStripMenuItem.Text = "Table 1 Palette";
			// 
			// Table1P0
			// 
			this.Table1P0.Name = "Table1P0";
			this.Table1P0.Size = new System.Drawing.Size(91, 22);
			this.Table1P0.Text = "0";
			this.Table1P0.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P1
			// 
			this.Table1P1.Name = "Table1P1";
			this.Table1P1.Size = new System.Drawing.Size(91, 22);
			this.Table1P1.Text = "1";
			this.Table1P1.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P2
			// 
			this.Table1P2.Name = "Table1P2";
			this.Table1P2.Size = new System.Drawing.Size(91, 22);
			this.Table1P2.Text = "2";
			this.Table1P2.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P3
			// 
			this.Table1P3.Name = "Table1P3";
			this.Table1P3.Size = new System.Drawing.Size(91, 22);
			this.Table1P3.Text = "3";
			this.Table1P3.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P4
			// 
			this.Table1P4.Name = "Table1P4";
			this.Table1P4.Size = new System.Drawing.Size(91, 22);
			this.Table1P4.Text = "4";
			this.Table1P4.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P5
			// 
			this.Table1P5.Name = "Table1P5";
			this.Table1P5.Size = new System.Drawing.Size(91, 22);
			this.Table1P5.Text = "5";
			this.Table1P5.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P6
			// 
			this.Table1P6.Name = "Table1P6";
			this.Table1P6.Size = new System.Drawing.Size(91, 22);
			this.Table1P6.Text = "6";
			this.Table1P6.Click += new System.EventHandler(this.Palette_Click);
			// 
			// Table1P7
			// 
			this.Table1P7.Name = "Table1P7";
			this.Table1P7.Size = new System.Drawing.Size(91, 22);
			this.Table1P7.Text = "7";
			this.Table1P7.Click += new System.EventHandler(this.Palette_Click);
			// 
			// SpriteViewerBox
			// 
			this.SpriteViewerBox.Controls.Add(this.SpriteView);
			this.SpriteViewerBox.Location = new System.Drawing.Point(299, 37);
			this.SpriteViewerBox.Name = "SpriteViewerBox";
			this.SpriteViewerBox.Size = new System.Drawing.Size(272, 155);
			this.SpriteViewerBox.TabIndex = 5;
			this.SpriteViewerBox.TabStop = false;
			this.SpriteViewerBox.Text = "Sprites";
			// 
			// SpriteView
			// 
			this.SpriteView.BackColor = System.Drawing.Color.White;
			this.SpriteView.Location = new System.Drawing.Point(6, 18);
			this.SpriteView.Name = "SpriteView";
			this.SpriteView.Size = new System.Drawing.Size(256, 128);
			this.SpriteView.TabIndex = 0;
			// 
			// PaletteView
			// 
			this.PaletteView.BackColor = System.Drawing.Color.White;
			this.PaletteView.Location = new System.Drawing.Point(6, 19);
			this.PaletteView.Name = "PaletteView";
			this.PaletteView.Size = new System.Drawing.Size(257, 34);
			this.PaletteView.TabIndex = 0;
			this.PaletteView.MouseLeave += new System.EventHandler(this.PaletteView_MouseLeave);
			this.PaletteView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PaletteView_MouseMove);
			this.PaletteView.MouseEnter += new System.EventHandler(this.PaletteView_MouseEnter);
			// 
			// PatternView
			// 
			this.PatternView.BackColor = System.Drawing.Color.White;
			this.PatternView.Location = new System.Drawing.Point(7, 20);
			this.PatternView.Name = "PatternView";
			this.PatternView.Size = new System.Drawing.Size(256, 128);
			this.PatternView.TabIndex = 0;
			this.PatternView.MouseLeave += new System.EventHandler(this.PatternView_MouseLeave);
			this.PatternView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PatternView_MouseMove);
			this.PatternView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PatternView_Click);
			this.PatternView.MouseEnter += new System.EventHandler(this.PatternView_MouseEnter);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(502, 204);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Scanline";
			// 
			// txtScanline
			// 
			this.txtScanline.Location = new System.Drawing.Point(501, 223);
			this.txtScanline.Name = "txtScanline";
			this.txtScanline.Size = new System.Drawing.Size(60, 20);
			this.txtScanline.TabIndex = 6;
			this.txtScanline.Text = "0";
			this.txtScanline.TextChanged += new System.EventHandler(this.txtScanline_TextChanged);
			// 
			// NESPPU
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(587, 317);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtScanline);
			this.Controls.Add(this.SpriteViewerBox);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.DetailsBox);
			this.Controls.Add(this.PalettesGroup);
			this.Controls.Add(this.PatternGroup);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "NESPPU";
			this.Text = "NES PPU Viewer";
			this.Load += new System.EventHandler(this.NESPPU_Load);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NESPPU_FormClosed);
			this.PatternGroup.ResumeLayout(false);
			this.PatternGroup.PerformLayout();
			this.PalettesGroup.ResumeLayout(false);
			this.DetailsBox.ResumeLayout(false);
			this.DetailsBox.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.SpriteViewerBox.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox PatternGroup;
        private System.Windows.Forms.GroupBox PalettesGroup;
        private PaletteViewer PaletteView;
        private System.Windows.Forms.GroupBox DetailsBox;
        private System.Windows.Forms.Label ValueLabel;
        private System.Windows.Forms.Label AddressLabel;
        private System.Windows.Forms.Label SectionLabel;
        private BizHawk.MultiClient.PatternViewer PatternView;
        private ToolStripEx toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.Label Table1PaletteLabel;
        private System.Windows.Forms.Label Table0PaletteLabel;
        private System.Windows.Forms.Label Value2Label;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton2;
        private System.Windows.Forms.ToolStripMenuItem table0PToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem Table0P0;
        private System.Windows.Forms.ToolStripMenuItem Table0P1;
        private System.Windows.Forms.ToolStripMenuItem Table0P2;
        private System.Windows.Forms.ToolStripMenuItem Table0P3;
        private System.Windows.Forms.ToolStripMenuItem Table0P4;
        private System.Windows.Forms.ToolStripMenuItem Table0P5;
        private System.Windows.Forms.ToolStripMenuItem Table0P6;
        private System.Windows.Forms.ToolStripMenuItem Table0P7;
        private System.Windows.Forms.ToolStripMenuItem table1PaletteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem Table1P0;
        private System.Windows.Forms.ToolStripMenuItem Table1P1;
        private System.Windows.Forms.ToolStripMenuItem Table1P2;
        private System.Windows.Forms.ToolStripMenuItem Table1P3;
        private System.Windows.Forms.ToolStripMenuItem Table1P4;
        private System.Windows.Forms.ToolStripMenuItem Table1P5;
        private System.Windows.Forms.ToolStripMenuItem Table1P6;
        private System.Windows.Forms.ToolStripMenuItem Table1P7;
        private System.Windows.Forms.GroupBox SpriteViewerBox;
        private SpriteViewer SpriteView;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtScanline;
    }
}