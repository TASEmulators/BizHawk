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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.PalettesGroup = new System.Windows.Forms.GroupBox();
            this.PaletteView = new BizHawk.MultiClient.PaletteViewer();
            this.DetailsBox = new System.Windows.Forms.GroupBox();
            this.SectionLabel = new System.Windows.Forms.Label();
            this.AddressLabel = new System.Windows.Forms.Label();
            this.ValueLabel = new System.Windows.Forms.Label();
            this.PalettesGroup.SuspendLayout();
            this.DetailsBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
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
            // SectionLabel
            // 
            this.SectionLabel.AutoSize = true;
            this.SectionLabel.Location = new System.Drawing.Point(6, 26);
            this.SectionLabel.Name = "SectionLabel";
            this.SectionLabel.Size = new System.Drawing.Size(35, 13);
            this.SectionLabel.TabIndex = 0;
            this.SectionLabel.Text = "label1";
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
            // ValueLabel
            // 
            this.ValueLabel.AutoSize = true;
            this.ValueLabel.Location = new System.Drawing.Point(6, 73);
            this.ValueLabel.Name = "ValueLabel";
            this.ValueLabel.Size = new System.Drawing.Size(35, 13);
            this.ValueLabel.TabIndex = 2;
            this.ValueLabel.Text = "label1";
            // 
            // NESPPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 359);
            this.Controls.Add(this.DetailsBox);
            this.Controls.Add(this.PalettesGroup);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "NESPPU";
            this.Text = "PPU Viewer";
            this.Load += new System.EventHandler(this.NESPPU_Load);
            this.PalettesGroup.ResumeLayout(false);
            this.DetailsBox.ResumeLayout(false);
            this.DetailsBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox PalettesGroup;
        private PaletteViewer PaletteView;
        private System.Windows.Forms.GroupBox DetailsBox;
        private System.Windows.Forms.Label ValueLabel;
        private System.Windows.Forms.Label AddressLabel;
        private System.Windows.Forms.Label SectionLabel;
    }
}