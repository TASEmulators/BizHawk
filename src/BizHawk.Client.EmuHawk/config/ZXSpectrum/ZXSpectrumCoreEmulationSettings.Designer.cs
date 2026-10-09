namespace BizHawk.Client.EmuHawk
{
    partial class ZxSpectrumCoreEmulationSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ZxSpectrumCoreEmulationSettings));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MachineSelectionComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.determEmucheckBox1 = new System.Windows.Forms.CheckBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.borderTypecomboBox1 = new System.Windows.Forms.ComboBox();
            this.lblBorderInfo = new BizHawk.WinForms.Controls.LocLabelEx();
            this.lblAutoLoadText = new BizHawk.WinForms.Controls.LocLabelEx();
            this.autoLoadcheckBox1 = new System.Windows.Forms.CheckBox();
            this.textBoxCoreDetails = new System.Windows.Forms.TextBox();
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
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // MachineSelectionComboBox
            // 
            resources.ApplyResources(this.MachineSelectionComboBox, "MachineSelectionComboBox");
            this.MachineSelectionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MachineSelectionComboBox.FormattingEnabled = true;
            this.MachineSelectionComboBox.Name = "MachineSelectionComboBox";
            this.MachineSelectionComboBox.SelectionChangeCommitted += new System.EventHandler(this.MachineSelectionComboBox_SelectionChangeCommitted);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // determEmucheckBox1
            // 
            resources.ApplyResources(this.determEmucheckBox1, "determEmucheckBox1");
            this.determEmucheckBox1.Name = "determEmucheckBox1";
            this.determEmucheckBox1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // borderTypecomboBox1
            // 
            resources.ApplyResources(this.borderTypecomboBox1, "borderTypecomboBox1");
            this.borderTypecomboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.borderTypecomboBox1.FormattingEnabled = true;
            this.borderTypecomboBox1.Name = "borderTypecomboBox1";
            this.borderTypecomboBox1.SelectedIndexChanged += new System.EventHandler(this.BorderTypeComboBox_SelectedIndexChanged);
            // 
            // lblBorderInfo
            // 
            resources.ApplyResources(this.lblBorderInfo, "lblBorderInfo");
            this.lblBorderInfo.Name = "lblBorderInfo";
            // 
            // lblAutoLoadText
            // 
            resources.ApplyResources(this.lblAutoLoadText, "lblAutoLoadText");
            this.lblAutoLoadText.Name = "lblAutoLoadText";
            // 
            // autoLoadcheckBox1
            // 
            resources.ApplyResources(this.autoLoadcheckBox1, "autoLoadcheckBox1");
            this.autoLoadcheckBox1.Name = "autoLoadcheckBox1";
            this.autoLoadcheckBox1.UseVisualStyleBackColor = true;
            // 
            // textBoxCoreDetails
            // 
            this.textBoxCoreDetails.AcceptsReturn = true;
            this.textBoxCoreDetails.AcceptsTab = true;
            resources.ApplyResources(this.textBoxCoreDetails, "textBoxCoreDetails");
            this.textBoxCoreDetails.Name = "textBoxCoreDetails";
            this.textBoxCoreDetails.ReadOnly = true;
            // 
            // ZxSpectrumCoreEmulationSettings
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.textBoxCoreDetails);
            this.Controls.Add(this.lblAutoLoadText);
            this.Controls.Add(this.autoLoadcheckBox1);
            this.Controls.Add(this.lblBorderInfo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.borderTypecomboBox1);
            this.Controls.Add(this.determEmucheckBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.MachineSelectionComboBox);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ZxSpectrumCoreEmulationSettings";
            this.Load += new System.EventHandler(this.IntvControllerSettings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OkBtn;
        private System.Windows.Forms.Button CancelBtn;
        private BizHawk.WinForms.Controls.LocLabelEx label4;
        private System.Windows.Forms.ComboBox MachineSelectionComboBox;
        private BizHawk.WinForms.Controls.LocLabelEx label1;
        private System.Windows.Forms.CheckBox determEmucheckBox1;
        private BizHawk.WinForms.Controls.LocLabelEx label2;
        private System.Windows.Forms.ComboBox borderTypecomboBox1;
        private BizHawk.WinForms.Controls.LocLabelEx lblBorderInfo;
        private BizHawk.WinForms.Controls.LocLabelEx lblAutoLoadText;
        private System.Windows.Forms.CheckBox autoLoadcheckBox1;
        private System.Windows.Forms.TextBox textBoxCoreDetails;
    }
}