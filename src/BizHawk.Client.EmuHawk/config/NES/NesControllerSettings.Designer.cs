namespace BizHawk.Client.EmuHawk
{
	partial class NesControllerSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NesControllerSettings));
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OkBtn = new System.Windows.Forms.Button();
            this.checkBoxFamicom = new System.Windows.Forms.CheckBox();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.comboBoxNESR = new System.Windows.Forms.ComboBox();
            this.comboBoxNESL = new System.Windows.Forms.ComboBox();
            this.comboBoxFamicom = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // OkBtn
            // 
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // checkBoxFamicom
            // 
            resources.ApplyResources(this.checkBoxFamicom, "checkBoxFamicom");
            this.checkBoxFamicom.Name = "checkBoxFamicom";
            this.checkBoxFamicom.UseVisualStyleBackColor = true;
            this.checkBoxFamicom.CheckedChanged += new System.EventHandler(this.CheckBoxFamicom_CheckedChanged);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // comboBoxNESR
            // 
            resources.ApplyResources(this.comboBoxNESR, "comboBoxNESR");
            this.comboBoxNESR.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNESR.FormattingEnabled = true;
            this.comboBoxNESR.Name = "comboBoxNESR";
            // 
            // comboBoxNESL
            // 
            resources.ApplyResources(this.comboBoxNESL, "comboBoxNESL");
            this.comboBoxNESL.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNESL.FormattingEnabled = true;
            this.comboBoxNESL.Name = "comboBoxNESL";
            // 
            // comboBoxFamicom
            // 
            resources.ApplyResources(this.comboBoxFamicom, "comboBoxFamicom");
            this.comboBoxFamicom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxFamicom.FormattingEnabled = true;
            this.comboBoxFamicom.Name = "comboBoxFamicom";
            // 
            // NesControllerSettings
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBoxNESR);
            this.Controls.Add(this.comboBoxNESL);
            this.Controls.Add(this.comboBoxFamicom);
            this.Controls.Add(this.checkBoxFamicom);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Name = "NesControllerSettings";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.CheckBox checkBoxFamicom;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.ComboBox comboBoxNESR;
		private System.Windows.Forms.ComboBox comboBoxNESL;
		private System.Windows.Forms.ComboBox comboBoxFamicom;
	}
}