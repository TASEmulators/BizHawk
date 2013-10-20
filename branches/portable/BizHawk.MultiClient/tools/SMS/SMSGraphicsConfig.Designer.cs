namespace BizHawk.MultiClient
{
	partial class SMSGraphicsConfig
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
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.DispOBJ = new System.Windows.Forms.CheckBox();
			this.DispBG = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(62, 89);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(143, 89);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			// 
			// DispOBJ
			// 
			this.DispOBJ.AutoSize = true;
			this.DispOBJ.Checked = true;
			this.DispOBJ.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DispOBJ.Location = new System.Drawing.Point(6, 18);
			this.DispOBJ.Name = "DispOBJ";
			this.DispOBJ.Size = new System.Drawing.Size(89, 16);
			this.DispOBJ.TabIndex = 2;
			this.DispOBJ.Text = "Display OBJ";
			this.DispOBJ.UseVisualStyleBackColor = true;
			// 
			// DispBG
			// 
			this.DispBG.AutoSize = true;
			this.DispBG.Checked = true;
			this.DispBG.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DispBG.Location = new System.Drawing.Point(6, 40);
			this.DispBG.Name = "DispBG";
			this.DispBG.Size = new System.Drawing.Size(82, 16);
			this.DispBG.TabIndex = 3;
			this.DispBG.Text = "Display BG";
			this.DispBG.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.DispOBJ);
			this.groupBox1.Controls.Add(this.DispBG);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(200, 64);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Background and Sprites";
			// 
			// SMSGraphicsConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(230, 124);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SMSGraphicsConfig";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "SMS Graphics Settings";
			this.Load += new System.EventHandler(this.SMSGraphicsConfig_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.CheckBox DispOBJ;
		private System.Windows.Forms.CheckBox DispBG;
		private System.Windows.Forms.GroupBox groupBox1;
	}
}