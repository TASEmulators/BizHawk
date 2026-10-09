namespace BizHawk.Client.EmuHawk
{
    partial class AmstradCpcNonSyncSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AmstradCpcNonSyncSettings));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.lblOSDVerbinfo = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.osdMessageVerbositycomboBox1 = new System.Windows.Forms.ComboBox();
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
            // lblOSDVerbinfo
            // 
            resources.ApplyResources(this.lblOSDVerbinfo, "lblOSDVerbinfo");
            this.lblOSDVerbinfo.Name = "lblOSDVerbinfo";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // osdMessageVerbositycomboBox1
            // 
            resources.ApplyResources(this.osdMessageVerbositycomboBox1, "osdMessageVerbositycomboBox1");
            this.osdMessageVerbositycomboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.osdMessageVerbositycomboBox1.FormattingEnabled = true;
            this.osdMessageVerbositycomboBox1.Name = "osdMessageVerbositycomboBox1";
            this.osdMessageVerbositycomboBox1.SelectionChangeCommitted += new System.EventHandler(this.OSDComboBox_SelectionChangeCommitted);
            // 
            // AmstradCpcNonSyncSettings
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.lblOSDVerbinfo);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.osdMessageVerbositycomboBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "AmstradCpcNonSyncSettings";
            this.Load += new System.EventHandler(this.IntvControllerSettings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OkBtn;
        private System.Windows.Forms.Button CancelBtn;
        private BizHawk.WinForms.Controls.LocLabelEx label1;
        private BizHawk.WinForms.Controls.LocLabelEx lblOSDVerbinfo;
        private BizHawk.WinForms.Controls.LocLabelEx label4;
        private System.Windows.Forms.ComboBox osdMessageVerbositycomboBox1;
    }
}
