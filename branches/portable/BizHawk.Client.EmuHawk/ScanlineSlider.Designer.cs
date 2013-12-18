namespace BizHawk.Client.EmuHawk
{
    partial class ScanlineSlider
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
            this.scanlinetrackbar = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.scanlinetrackbar)).BeginInit();
            this.SuspendLayout();
            // 
            // scanlinetrackbar
            // 
            this.scanlinetrackbar.Location = new System.Drawing.Point(2, 7);
            this.scanlinetrackbar.Maximum = 100;
            this.scanlinetrackbar.Name = "scanlinetrackbar";
            this.scanlinetrackbar.Size = new System.Drawing.Size(207, 45);
            this.scanlinetrackbar.TabIndex = 0;
            this.scanlinetrackbar.TickFrequency = 5;
            this.scanlinetrackbar.TickStyle = System.Windows.Forms.TickStyle.Both;
            this.scanlinetrackbar.Scroll += new System.EventHandler(this.scanlinetrackbar_Scroll);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(206, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "0%";
            // 
            // ScanlineSlider
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(237, 58);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.scanlinetrackbar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ScanlineSlider";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Scanline Intensity";
            this.Load += new System.EventHandler(this.ScanlineSlider_Load);
            ((System.ComponentModel.ISupportInitialize)(this.scanlinetrackbar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TrackBar scanlinetrackbar;
        private System.Windows.Forms.Label label1;
    }
}