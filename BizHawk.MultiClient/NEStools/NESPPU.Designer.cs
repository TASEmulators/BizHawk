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
            this.PalettesGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Location = new System.Drawing.Point(12, 26);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(415, 199);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Pattern Tables";
            // 
            // PalettesGroup
            // 
            this.PalettesGroup.Controls.Add(this.PaletteView);
            this.PalettesGroup.Location = new System.Drawing.Point(12, 262);
            this.PalettesGroup.Name = "PalettesGroup";
            this.PalettesGroup.Size = new System.Drawing.Size(415, 84);
            this.PalettesGroup.TabIndex = 1;
            this.PalettesGroup.TabStop = false;
            this.PalettesGroup.Text = "Palettes";
            // 
            // PaletteView
            // 
            this.PaletteView.BackColor = System.Drawing.Color.White;
            this.PaletteView.Location = new System.Drawing.Point(69, 35);
            this.PaletteView.Name = "PaletteView";
            this.PaletteView.Size = new System.Drawing.Size(257, 34);
            this.PaletteView.TabIndex = 0;
            // 
            // NESPPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(443, 359);
            this.Controls.Add(this.PalettesGroup);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "NESPPU";
            this.Text = "PPU Viewer";
            this.Load += new System.EventHandler(this.NESPPU_Load);
            this.PalettesGroup.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox PalettesGroup;
        private PaletteViewer PaletteView;
    }
}