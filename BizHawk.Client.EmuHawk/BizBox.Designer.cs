namespace BizHawk.Client.EmuHawk
{
	partial class BizBox
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BizBox));
			this.OK = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.CoreInfoPanel = new System.Windows.Forms.Panel();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.VersionLabel = new System.Windows.Forms.Label();
			this.btnCopyHash = new System.Windows.Forms.Button();
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.linkLabel3 = new System.Windows.Forms.LinkLabel();
			this.DateLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.OK.Location = new System.Drawing.Point(361, 504);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.corphawk;
			this.pictureBox1.Location = new System.Drawing.Point(12, 10);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(164, 164);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox1.TabIndex = 1;
			this.pictureBox1.TabStop = false;
			// 
			// linkLabel1
			// 
			this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.linkLabel1.AutoSize = true;
			this.linkLabel1.Location = new System.Drawing.Point(245, 509);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(102, 13);
			this.linkLabel1.TabIndex = 2;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "Bizhawk Homepage";
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(197, 10);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(78, 20);
			this.label3.TabIndex = 5;
			this.label3.Text = "BizHawk";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label4.Location = new System.Drawing.Point(207, 31);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(168, 32);
			this.label4.TabIndex = 6;
			this.label4.Text = "\"A multi-platform emulator...\r\nfor productive people.\"";
			// 
			// CoreInfoPanel
			// 
			this.CoreInfoPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.CoreInfoPanel.AutoScroll = true;
			this.CoreInfoPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.CoreInfoPanel.Location = new System.Drawing.Point(12, 197);
			this.CoreInfoPanel.Name = "CoreInfoPanel";
			this.CoreInfoPanel.Size = new System.Drawing.Size(423, 259);
			this.CoreInfoPanel.TabIndex = 14;
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(13, 462);
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.Size = new System.Drawing.Size(423, 33);
			this.textBox1.TabIndex = 16;
			this.textBox1.Text = "jabo_direct3d8_patched.dll is distributed with the special permission of the auth" +
    "or.";
			// 
			// VersionLabel
			// 
			this.VersionLabel.AutoSize = true;
			this.VersionLabel.Location = new System.Drawing.Point(198, 75);
			this.VersionLabel.Name = "VersionLabel";
			this.VersionLabel.Size = new System.Drawing.Size(108, 13);
			this.VersionLabel.TabIndex = 7;
			this.VersionLabel.Text = "versioninfo goes here";
			// 
			// btnCopyHash
			// 
			this.btnCopyHash.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnCopyHash.AutoSize = true;
			this.btnCopyHash.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.btnCopyHash.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Duplicate;
			this.btnCopyHash.Location = new System.Drawing.Point(12, 505);
			this.btnCopyHash.Name = "btnCopyHash";
			this.btnCopyHash.Size = new System.Drawing.Size(22, 22);
			this.btnCopyHash.TabIndex = 18;
			this.btnCopyHash.UseVisualStyleBackColor = true;
			this.btnCopyHash.Click += new System.EventHandler(this.btnCopyHash_Click);
			// 
			// linkLabel2
			// 
			this.linkLabel2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.linkLabel2.AutoSize = true;
			this.linkLabel2.Location = new System.Drawing.Point(40, 509);
			this.linkLabel2.Name = "linkLabel2";
			this.linkLabel2.Size = new System.Drawing.Size(100, 13);
			this.linkLabel2.TabIndex = 19;
			this.linkLabel2.TabStop = true;
			this.linkLabel2.Text = "Commit #XXXXXXX";
			this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
			// 
			// linkLabel3
			// 
			this.linkLabel3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.linkLabel3.AutoSize = true;
			this.linkLabel3.Location = new System.Drawing.Point(198, 112);
			this.linkLabel3.Name = "linkLabel3";
			this.linkLabel3.Size = new System.Drawing.Size(63, 13);
			this.linkLabel3.TabIndex = 20;
			this.linkLabel3.TabStop = true;
			this.linkLabel3.Text = "Contributors";
			this.linkLabel3.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel3_LinkClicked);
			// 
			// DateLabel
			// 
			this.DateLabel.AutoSize = true;
			this.DateLabel.Location = new System.Drawing.Point(198, 91);
			this.DateLabel.Name = "DateLabel";
			this.DateLabel.Size = new System.Drawing.Size(104, 13);
			this.DateLabel.TabIndex = 21;
			this.DateLabel.Text = "timestamp goes here";
			// 
			// BizBox
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.OK;
			this.ClientSize = new System.Drawing.Size(448, 536);
			this.Controls.Add(this.DateLabel);
			this.Controls.Add(this.linkLabel3);
			this.Controls.Add(this.linkLabel2);
			this.Controls.Add(this.btnCopyHash);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.CoreInfoPanel);
			this.Controls.Add(this.VersionLabel);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.OK);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(453, 253);
			this.Name = "BizBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = " BizHawk";
			this.Load += new System.EventHandler(this.BizBox_Load);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Panel CoreInfoPanel;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Label VersionLabel;
		private System.Windows.Forms.Button btnCopyHash;
		private System.Windows.Forms.LinkLabel linkLabel2;
        private System.Windows.Forms.LinkLabel linkLabel3;
		private System.Windows.Forms.Label DateLabel;
	}
}