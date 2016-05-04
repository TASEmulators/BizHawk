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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.ExtensionLabel = new System.Windows.Forms.Label();
			this.RomSizeLabel = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.AlwaysCheckbox = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.HashBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBox1.Location = new System.Drawing.Point(12, 12);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(414, 35);
			this.textBox1.TabIndex = 2;
			this.textBox1.Text = "This Rom was not found in the database.  Furthermore, the extension leaves no clu" +
    "e as to which platform should be chosen.";
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(300, 448);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 4;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(366, 448);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 5;
			this.CancelBtn.Text = "&Cancel";
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
			this.PlatformsGroupBox.Location = new System.Drawing.Point(12, 66);
			this.PlatformsGroupBox.Name = "PlatformsGroupBox";
			this.PlatformsGroupBox.Size = new System.Drawing.Size(270, 405);
			this.PlatformsGroupBox.TabIndex = 6;
			// 
			// label1
			// 
			this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(288, 50);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(67, 13);
			this.label1.TabIndex = 7;
			this.label1.Text = "Rom Details:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 50);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(270, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Please choose the intended platform to use for this Rom";
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(288, 74);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(56, 13);
			this.label3.TabIndex = 9;
			this.label3.Text = "Extension:";
			// 
			// ExtensionLabel
			// 
			this.ExtensionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ExtensionLabel.AutoSize = true;
			this.ExtensionLabel.Location = new System.Drawing.Point(288, 89);
			this.ExtensionLabel.Name = "ExtensionLabel";
			this.ExtensionLabel.Size = new System.Drawing.Size(24, 13);
			this.ExtensionLabel.TabIndex = 10;
			this.ExtensionLabel.Text = ".bin";
			// 
			// RomSizeLabel
			// 
			this.RomSizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.RomSizeLabel.AutoSize = true;
			this.RomSizeLabel.Location = new System.Drawing.Point(288, 134);
			this.RomSizeLabel.Name = "RomSizeLabel";
			this.RomSizeLabel.Size = new System.Drawing.Size(25, 13);
			this.RomSizeLabel.TabIndex = 12;
			this.RomSizeLabel.Text = "4kb";
			// 
			// label6
			// 
			this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(288, 116);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(30, 13);
			this.label6.TabIndex = 11;
			this.label6.Text = "Size:";
			// 
			// AlwaysCheckbox
			// 
			this.AlwaysCheckbox.AutoSize = true;
			this.AlwaysCheckbox.Location = new System.Drawing.Point(300, 396);
			this.AlwaysCheckbox.Name = "AlwaysCheckbox";
			this.AlwaysCheckbox.Size = new System.Drawing.Size(138, 17);
			this.AlwaysCheckbox.TabIndex = 13;
			this.AlwaysCheckbox.Text = "Always use this platform";
			this.AlwaysCheckbox.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(300, 416);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(86, 13);
			this.label4.TabIndex = 14;
			this.label4.Text = "for this extension";
			this.label4.Click += new System.EventHandler(this.label4_Click);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(288, 162);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(35, 13);
			this.label5.TabIndex = 15;
			this.label5.Text = "Hash:";
			// 
			// HashBox
			// 
			this.HashBox.Location = new System.Drawing.Point(291, 178);
			this.HashBox.Name = "HashBox";
			this.HashBox.ReadOnly = true;
			this.HashBox.Size = new System.Drawing.Size(145, 20);
			this.HashBox.TabIndex = 16;
			// 
			// PlatformChooser
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(438, 483);
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
			this.Text = "Choose a Platform";
			this.Load += new System.EventHandler(this.PlatformChooser_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Panel PlatformsGroupBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label ExtensionLabel;
        private System.Windows.Forms.Label RomSizeLabel;
        private System.Windows.Forms.Label label6;
		private System.Windows.Forms.CheckBox AlwaysCheckbox;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox HashBox;
	}
}