namespace BizHawk.Client.EmuHawk
{
    partial class ZXSpectrumAudioSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ZXSpectrumAudioSettings));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.panTypecomboBox1 = new System.Windows.Forms.ComboBox();
            this.lblBorderInfo = new System.Windows.Forms.Label();
            this.tapeVolumetrackBar = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.earVolumetrackBar = new System.Windows.Forms.TrackBar();
            this.label5 = new System.Windows.Forms.Label();
            this.ayVolumetrackBar = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.tapeVolumetrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.earVolumetrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ayVolumetrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // OkBtn
            // 
            this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkBtn.Location = new System.Drawing.Point(303, 298);
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.Size = new System.Drawing.Size(60, 23);
            this.OkBtn.TabIndex = 3;
            this.OkBtn.Text = "&OK";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(369, 298);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(60, 23);
            this.CancelBtn.TabIndex = 4;
            this.CancelBtn.Text = "&Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(140, 13);
            this.label1.TabIndex = 17;
            this.label1.Text = "ZX Spectrum Audio Settings";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 236);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(135, 13);
            this.label2.TabIndex = 23;
            this.label2.Text = "AY-3-8912 Panning Config:";
            // 
            // panTypecomboBox1
            // 
            this.panTypecomboBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panTypecomboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.panTypecomboBox1.FormattingEnabled = true;
            this.panTypecomboBox1.Location = new System.Drawing.Point(12, 252);
            this.panTypecomboBox1.Name = "panTypecomboBox1";
            this.panTypecomboBox1.Size = new System.Drawing.Size(157, 21);
            this.panTypecomboBox1.TabIndex = 22;
            // 
            // lblBorderInfo
            // 
            this.lblBorderInfo.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBorderInfo.Location = new System.Drawing.Point(175, 236);
            this.lblBorderInfo.Name = "lblBorderInfo";
            this.lblBorderInfo.Size = new System.Drawing.Size(254, 59);
            this.lblBorderInfo.TabIndex = 24;
            this.lblBorderInfo.Text = "Selects a particular panning configuration for the 3ch AY-3-8912 Programmable Sou" +
    "nd Generator (128K models only)";
            this.lblBorderInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tapeVolumetrackBar
            // 
            this.tapeVolumetrackBar.Location = new System.Drawing.Point(12, 60);
            this.tapeVolumetrackBar.Maximum = 100;
            this.tapeVolumetrackBar.Name = "tapeVolumetrackBar";
            this.tapeVolumetrackBar.Size = new System.Drawing.Size(417, 45);
            this.tapeVolumetrackBar.TabIndex = 25;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 44);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 13);
            this.label3.TabIndex = 26;
            this.label3.Text = "Tape Volume:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 108);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(80, 13);
            this.label4.TabIndex = 28;
            this.label4.Text = "Buzzer Volume:";
            // 
            // earVolumetrackBar
            // 
            this.earVolumetrackBar.Location = new System.Drawing.Point(12, 124);
            this.earVolumetrackBar.Maximum = 100;
            this.earVolumetrackBar.Name = "earVolumetrackBar";
            this.earVolumetrackBar.Size = new System.Drawing.Size(417, 45);
            this.earVolumetrackBar.TabIndex = 27;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 172);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 13);
            this.label5.TabIndex = 30;
            this.label5.Text = "AY-3-8912 Volume:";
            // 
            // ayVolumetrackBar
            // 
            this.ayVolumetrackBar.Location = new System.Drawing.Point(12, 188);
            this.ayVolumetrackBar.Maximum = 100;
            this.ayVolumetrackBar.Name = "ayVolumetrackBar";
            this.ayVolumetrackBar.Size = new System.Drawing.Size(417, 45);
            this.ayVolumetrackBar.TabIndex = 29;
            // 
            // ZXSpectrumAudioSettings
            // 
            this.AcceptButton = this.OkBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(441, 333);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.ayVolumetrackBar);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.earVolumetrackBar);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tapeVolumetrackBar);
            this.Controls.Add(this.lblBorderInfo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.panTypecomboBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ZXSpectrumAudioSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Audio Settings";
            this.Load += new System.EventHandler(this.IntvControllerSettings_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tapeVolumetrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.earVolumetrackBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ayVolumetrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OkBtn;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox panTypecomboBox1;
        private System.Windows.Forms.Label lblBorderInfo;
        private System.Windows.Forms.TrackBar tapeVolumetrackBar;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar earVolumetrackBar;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TrackBar ayVolumetrackBar;
    }
}