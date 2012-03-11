namespace BizHawk.MultiClient
{
	partial class PCEGraphicsConfig
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.DispBG2 = new System.Windows.Forms.CheckBox();
			this.DispOBJ2 = new System.Windows.Forms.CheckBox();
			this.DispBG1 = new System.Windows.Forms.CheckBox();
			this.DispOBJ1 = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OK.Location = new System.Drawing.Point(213, 105);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 4;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(294, 105);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 5;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.DispBG2);
			this.groupBox1.Controls.Add(this.DispOBJ2);
			this.groupBox1.Controls.Add(this.DispBG1);
			this.groupBox1.Controls.Add(this.DispOBJ1);
			this.groupBox1.Location = new System.Drawing.Point(9, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(352, 73);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Background and Sprites";
			// 
			// DispBG2
			// 
			this.DispBG2.AutoSize = true;
			this.DispBG2.Checked = true;
			this.DispBG2.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DispBG2.Location = new System.Drawing.Point(108, 43);
			this.DispBG2.Name = "DispBG2";
			this.DispBG2.Size = new System.Drawing.Size(84, 17);
			this.DispBG2.TabIndex = 3;
			this.DispBG2.Text = "Display BG2";
			this.DispBG2.UseVisualStyleBackColor = true;
			// 
			// DispOBJ2
			// 
			this.DispOBJ2.AutoSize = true;
			this.DispOBJ2.Checked = true;
			this.DispOBJ2.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DispOBJ2.Location = new System.Drawing.Point(108, 21);
			this.DispOBJ2.Name = "DispOBJ2";
			this.DispOBJ2.Size = new System.Drawing.Size(89, 17);
			this.DispOBJ2.TabIndex = 2;
			this.DispOBJ2.Text = "Display OBJ2";
			this.DispOBJ2.UseVisualStyleBackColor = true;
			// 
			// DispBG1
			// 
			this.DispBG1.AutoSize = true;
			this.DispBG1.Checked = true;
			this.DispBG1.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DispBG1.Location = new System.Drawing.Point(9, 43);
			this.DispBG1.Name = "DispBG1";
			this.DispBG1.Size = new System.Drawing.Size(84, 17);
			this.DispBG1.TabIndex = 1;
			this.DispBG1.Text = "Display BG1";
			this.DispBG1.UseVisualStyleBackColor = true;
			// 
			// DispOBJ1
			// 
			this.DispOBJ1.AutoSize = true;
			this.DispOBJ1.Checked = true;
			this.DispOBJ1.CheckState = System.Windows.Forms.CheckState.Checked;
			this.DispOBJ1.Location = new System.Drawing.Point(9, 21);
			this.DispOBJ1.Name = "DispOBJ1";
			this.DispOBJ1.Size = new System.Drawing.Size(89, 17);
			this.DispOBJ1.TabIndex = 0;
			this.DispOBJ1.Text = "Display OBJ1";
			this.DispOBJ1.UseVisualStyleBackColor = true;
			// 
			// PCEGraphicsConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(373, 128);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(389, 433);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(389, 166);
			this.Name = "PCEGraphicsConfig";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Text = "PCE Graphics Settings";
			this.Load += new System.EventHandler(this.PCEGraphicsConfig_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox DispBG2;
		private System.Windows.Forms.CheckBox DispOBJ2;
		private System.Windows.Forms.CheckBox DispBG1;
		private System.Windows.Forms.CheckBox DispOBJ1;
	}
}