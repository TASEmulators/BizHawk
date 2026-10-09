namespace BizHawk.Client.EmuHawk
{
    partial class ZxSpectrumAudioSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ZxSpectrumAudioSettings));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.panTypecomboBox1 = new System.Windows.Forms.ComboBox();
            this.lblBorderInfo = new BizHawk.WinForms.Controls.LocLabelEx();
            this.tapeVolumetrackBar = new System.Windows.Forms.TrackBar();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.earVolumetrackBar = new System.Windows.Forms.TrackBar();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ayVolumetrackBar = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.tapeVolumetrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.earVolumetrackBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ayVolumetrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // OkBtn
            // 
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // panTypecomboBox1
            // 
            resources.ApplyResources(this.panTypecomboBox1, "panTypecomboBox1");
            this.panTypecomboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.panTypecomboBox1.FormattingEnabled = true;
            this.panTypecomboBox1.Name = "panTypecomboBox1";
            // 
            // lblBorderInfo
            // 
            resources.ApplyResources(this.lblBorderInfo, "lblBorderInfo");
            this.lblBorderInfo.Name = "lblBorderInfo";
            // 
            // tapeVolumetrackBar
            // 
            resources.ApplyResources(this.tapeVolumetrackBar, "tapeVolumetrackBar");
            this.tapeVolumetrackBar.Maximum = 100;
            this.tapeVolumetrackBar.Name = "tapeVolumetrackBar";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // earVolumetrackBar
            // 
            resources.ApplyResources(this.earVolumetrackBar, "earVolumetrackBar");
            this.earVolumetrackBar.Maximum = 100;
            this.earVolumetrackBar.Name = "earVolumetrackBar";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // ayVolumetrackBar
            // 
            resources.ApplyResources(this.ayVolumetrackBar, "ayVolumetrackBar");
            this.ayVolumetrackBar.Maximum = 100;
            this.ayVolumetrackBar.Name = "ayVolumetrackBar";
            // 
            // ZxSpectrumAudioSettings
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
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
            this.Name = "ZxSpectrumAudioSettings";
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
        private BizHawk.WinForms.Controls.LocLabelEx label1;
        private BizHawk.WinForms.Controls.LocLabelEx label2;
        private System.Windows.Forms.ComboBox panTypecomboBox1;
        private BizHawk.WinForms.Controls.LocLabelEx lblBorderInfo;
        private System.Windows.Forms.TrackBar tapeVolumetrackBar;
        private BizHawk.WinForms.Controls.LocLabelEx label3;
        private BizHawk.WinForms.Controls.LocLabelEx label4;
        private System.Windows.Forms.TrackBar earVolumetrackBar;
        private BizHawk.WinForms.Controls.LocLabelEx label5;
        private System.Windows.Forms.TrackBar ayVolumetrackBar;
    }
}