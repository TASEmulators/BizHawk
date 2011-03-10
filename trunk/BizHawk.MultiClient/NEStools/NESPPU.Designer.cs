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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.PalettesGroup = new System.Windows.Forms.GroupBox();
            this.DetailsBox = new System.Windows.Forms.GroupBox();
            this.ValueLabel = new System.Windows.Forms.Label();
            this.AddressLabel = new System.Windows.Forms.Label();
            this.SectionLabel = new System.Windows.Forms.Label();
            this.PaletteView = new BizHawk.MultiClient.PaletteViewer();
            this.PatternView = new BizHawk.MultiClient.PatternViewer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBox1.SuspendLayout();
            this.PalettesGroup.SuspendLayout();
            this.DetailsBox.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.PatternView);
            this.groupBox1.Location = new System.Drawing.Point(12, 26);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(281, 199);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Pattern Tables";
            // 
            // PalettesGroup
            // 
            this.PalettesGroup.Controls.Add(this.PaletteView);
            this.PalettesGroup.Location = new System.Drawing.Point(12, 262);
            this.PalettesGroup.Name = "PalettesGroup";
            this.PalettesGroup.Size = new System.Drawing.Size(281, 66);
            this.PalettesGroup.TabIndex = 1;
            this.PalettesGroup.TabStop = false;
            this.PalettesGroup.Text = "Palettes";
            // 
            // DetailsBox
            // 
            this.DetailsBox.Controls.Add(this.ValueLabel);
            this.DetailsBox.Controls.Add(this.AddressLabel);
            this.DetailsBox.Controls.Add(this.SectionLabel);
            this.DetailsBox.Location = new System.Drawing.Point(300, 26);
            this.DetailsBox.Name = "DetailsBox";
            this.DetailsBox.Size = new System.Drawing.Size(177, 129);
            this.DetailsBox.TabIndex = 2;
            this.DetailsBox.TabStop = false;
            this.DetailsBox.Text = "Details";
            // 
            // ValueLabel
            // 
            this.ValueLabel.AutoSize = true;
            this.ValueLabel.Location = new System.Drawing.Point(6, 73);
            this.ValueLabel.Name = "ValueLabel";
            this.ValueLabel.Size = new System.Drawing.Size(35, 13);
            this.ValueLabel.TabIndex = 2;
            this.ValueLabel.Text = "label1";
            // 
            // AddressLabel
            // 
            this.AddressLabel.AutoSize = true;
            this.AddressLabel.Location = new System.Drawing.Point(6, 49);
            this.AddressLabel.Name = "AddressLabel";
            this.AddressLabel.Size = new System.Drawing.Size(35, 13);
            this.AddressLabel.TabIndex = 1;
            this.AddressLabel.Text = "label1";
            // 
            // SectionLabel
            // 
            this.SectionLabel.AutoSize = true;
            this.SectionLabel.Location = new System.Drawing.Point(6, 26);
            this.SectionLabel.Name = "SectionLabel";
            this.SectionLabel.Size = new System.Drawing.Size(35, 13);
            this.SectionLabel.TabIndex = 0;
            this.SectionLabel.Text = "label1";
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
            this.PatternView.Location = new System.Drawing.Point(17, 26);
            this.PatternView.Name = "PatternView";
            this.PatternView.Size = new System.Drawing.Size(35, 13);
            this.PatternView.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(504, 25);
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
            // NESPPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 359);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.DetailsBox);
            this.Controls.Add(this.PalettesGroup);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "NESPPU";
            this.Text = "PPU Viewer";
            this.Load += new System.EventHandler(this.NESPPU_Load);
            this.groupBox1.ResumeLayout(false);
            this.PalettesGroup.ResumeLayout(false);
            this.DetailsBox.ResumeLayout(false);
            this.DetailsBox.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox PalettesGroup;
        private PaletteViewer PaletteView;
        private System.Windows.Forms.GroupBox DetailsBox;
        private System.Windows.Forms.Label ValueLabel;
        private System.Windows.Forms.Label AddressLabel;
        private System.Windows.Forms.Label SectionLabel;
        private BizHawk.MultiClient.PatternViewer PatternView;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
    }
}