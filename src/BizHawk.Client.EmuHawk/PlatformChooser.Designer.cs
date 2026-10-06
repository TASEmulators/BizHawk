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
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Location = new System.Drawing.Point(12, 11);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(414, 32);
            this.textBox1.TabIndex = 2;
            this.textBox1.Text = "数据库中没有找到该 ROM ，无法通过扩展名来确定使用哪个平台。";
            // 
            // OkBtn
            // 
            this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkBtn.Location = new System.Drawing.Point(300, 414);
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.Size = new System.Drawing.Size(60, 21);
            this.OkBtn.TabIndex = 4;
            this.OkBtn.Text = "确定(&O)";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(366, 414);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(60, 21);
            this.CancelBtn.TabIndex = 5;
            this.CancelBtn.Text = "取消(&C)";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // PlatformsGroupBox
            // 
            this.PlatformsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PlatformsGroupBox.AutoScroll = true;
            this.PlatformsGroupBox.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PlatformsGroupBox.Location = new System.Drawing.Point(12, 61);
            this.PlatformsGroupBox.Name = "PlatformsGroupBox";
            this.PlatformsGroupBox.Size = new System.Drawing.Size(270, 374);
            this.PlatformsGroupBox.TabIndex = 6;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(288, 46);
            this.label1.Name = "label1";
            this.label1.Text = "ROM 详情：";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 46);
            this.label2.Name = "label2";
            this.label2.Text = "请选择打开此 ROM 的平台：";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.Location = new System.Drawing.Point(288, 68);
            this.label3.Name = "label3";
            this.label3.Text = "扩展名：";
            // 
            // ExtensionLabel
            // 
            this.ExtensionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ExtensionLabel.Location = new System.Drawing.Point(288, 82);
            this.ExtensionLabel.Name = "ExtensionLabel";
            this.ExtensionLabel.Text = ".bin";
            // 
            // RomSizeLabel
            // 
            this.RomSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RomSizeLabel.Location = new System.Drawing.Point(288, 124);
            this.RomSizeLabel.Name = "RomSizeLabel";
            this.RomSizeLabel.Text = "4kb";
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.Location = new System.Drawing.Point(288, 107);
            this.label6.Name = "label6";
            this.label6.Text = "文件大小：";
            // 
            // AlwaysCheckbox
            // 
            this.AlwaysCheckbox.AutoSize = true;
            this.AlwaysCheckbox.Location = new System.Drawing.Point(300, 366);
            this.AlwaysCheckbox.Name = "AlwaysCheckbox";
            this.AlwaysCheckbox.Size = new System.Drawing.Size(114, 16);
            this.AlwaysCheckbox.TabIndex = 13;
            this.AlwaysCheckbox.Text = " 始终使用该平台";
            this.AlwaysCheckbox.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.Location = new System.Drawing.Point(300, 384);
            this.label4.Name = "label4";
            this.label4.Text = "打开该扩展名的文件";
            this.label4.Click += new System.EventHandler(this.label4_Click);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(288, 150);
            this.label5.Name = "label5";
            this.label5.Text = "哈希：";
            // 
            // HashBox
            // 
            this.HashBox.Location = new System.Drawing.Point(291, 164);
            this.HashBox.Name = "HashBox";
            this.HashBox.ReadOnly = true;
            this.HashBox.Size = new System.Drawing.Size(145, 21);
            this.HashBox.TabIndex = 16;
            // 
            // PlatformChooser
            // 
            this.AcceptButton = this.OkBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(438, 446);
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
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "选择平台";
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