namespace BizHawk.Client.EmuHawk
{
	partial class ColecoControllerSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColecoControllerSettings));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Port2ComboBox = new System.Windows.Forms.ComboBox();
            this.Port1ComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
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
            // Port2ComboBox
            // 
            resources.ApplyResources(this.Port2ComboBox, "Port2ComboBox");
            this.Port2ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Port2ComboBox.FormattingEnabled = true;
            this.Port2ComboBox.Name = "Port2ComboBox";
            // 
            // Port1ComboBox
            // 
            resources.ApplyResources(this.Port1ComboBox, "Port1ComboBox");
            this.Port1ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Port1ComboBox.FormattingEnabled = true;
            this.Port1ComboBox.Name = "Port1ComboBox";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // ColecoControllerSettings
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.Port2ComboBox);
            this.Controls.Add(this.Port1ComboBox);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Name = "ColecoControllerSettings";
            this.Load += new System.EventHandler(this.ColecoControllerSettings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.ComboBox Port2ComboBox;
		private System.Windows.Forms.ComboBox Port1ComboBox;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
	}
}