namespace BizHawk.Client.EmuHawk
{
	partial class PlatformChooser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlatformChooser));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.PlatformsGroupBox = new System.Windows.Forms.Panel();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ExtensionLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.RomSizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AlwaysCheckbox = new System.Windows.Forms.CheckBox();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.HashBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
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
            this.CancelBtn.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // PlatformsGroupBox
            // 
            resources.ApplyResources(this.PlatformsGroupBox, "PlatformsGroupBox");
            this.PlatformsGroupBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PlatformsGroupBox.Name = "PlatformsGroupBox";
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
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // ExtensionLabel
            // 
            resources.ApplyResources(this.ExtensionLabel, "ExtensionLabel");
            this.ExtensionLabel.Name = "ExtensionLabel";
            // 
            // RomSizeLabel
            // 
            resources.ApplyResources(this.RomSizeLabel, "RomSizeLabel");
            this.RomSizeLabel.Name = "RomSizeLabel";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // AlwaysCheckbox
            // 
            resources.ApplyResources(this.AlwaysCheckbox, "AlwaysCheckbox");
            this.AlwaysCheckbox.Name = "AlwaysCheckbox";
            this.AlwaysCheckbox.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // HashBox
            // 
            resources.ApplyResources(this.HashBox, "HashBox");
            this.HashBox.Name = "HashBox";
            this.HashBox.ReadOnly = true;
            // 
            // PlatformChooser
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.HashBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.AlwaysCheckbox);
            this.Controls.Add(this.RomSizeLabel);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.ExtensionLabel);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.PlatformsGroupBox);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Controls.Add(this.textBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "PlatformChooser";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.PlatformChooser_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Panel PlatformsGroupBox;
        private BizHawk.WinForms.Controls.LocLabelEx label1;
        private BizHawk.WinForms.Controls.LocLabelEx label2;
        private BizHawk.WinForms.Controls.LocLabelEx label3;
        private BizHawk.WinForms.Controls.LocLabelEx ExtensionLabel;
        private BizHawk.WinForms.Controls.LocLabelEx RomSizeLabel;
        private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.CheckBox AlwaysCheckbox;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.TextBox HashBox;
	}
}