namespace BizHawk.MultiClient
{
	partial class NESSoundConfig
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
			this.Square1 = new System.Windows.Forms.CheckBox();
			this.Triangle = new System.Windows.Forms.CheckBox();
			this.Noise = new System.Windows.Forms.CheckBox();
			this.Square2 = new System.Windows.Forms.CheckBox();
			this.DMC = new System.Windows.Forms.CheckBox();
			this.SelectAll = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(52, 158);
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
			this.Cancel.Location = new System.Drawing.Point(133, 158);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// Square1
			// 
			this.Square1.AutoSize = true;
			this.Square1.Location = new System.Drawing.Point(12, 12);
			this.Square1.Name = "Square1";
			this.Square1.Size = new System.Drawing.Size(69, 17);
			this.Square1.TabIndex = 2;
			this.Square1.Text = "Square 1";
			this.Square1.UseVisualStyleBackColor = true;
			// 
			// Triangle
			// 
			this.Triangle.AutoSize = true;
			this.Triangle.Location = new System.Drawing.Point(12, 58);
			this.Triangle.Name = "Triangle";
			this.Triangle.Size = new System.Drawing.Size(64, 17);
			this.Triangle.TabIndex = 3;
			this.Triangle.Text = "Triangle";
			this.Triangle.UseVisualStyleBackColor = true;
			// 
			// Noise
			// 
			this.Noise.AutoSize = true;
			this.Noise.Location = new System.Drawing.Point(12, 81);
			this.Noise.Name = "Noise";
			this.Noise.Size = new System.Drawing.Size(53, 17);
			this.Noise.TabIndex = 5;
			this.Noise.Text = "Noise";
			this.Noise.UseVisualStyleBackColor = true;
			// 
			// Square2
			// 
			this.Square2.AutoSize = true;
			this.Square2.Location = new System.Drawing.Point(12, 35);
			this.Square2.Name = "Square2";
			this.Square2.Size = new System.Drawing.Size(69, 17);
			this.Square2.TabIndex = 4;
			this.Square2.Text = "Square 2";
			this.Square2.UseVisualStyleBackColor = true;
			// 
			// DMC
			// 
			this.DMC.AutoSize = true;
			this.DMC.Location = new System.Drawing.Point(12, 104);
			this.DMC.Name = "DMC";
			this.DMC.Size = new System.Drawing.Size(50, 17);
			this.DMC.TabIndex = 6;
			this.DMC.Text = "DMC";
			this.DMC.UseVisualStyleBackColor = true;
			// 
			// SelectAll
			// 
			this.SelectAll.AutoSize = true;
			this.SelectAll.Location = new System.Drawing.Point(12, 127);
			this.SelectAll.Name = "SelectAll";
			this.SelectAll.Size = new System.Drawing.Size(69, 17);
			this.SelectAll.TabIndex = 7;
			this.SelectAll.Text = "Select all";
			this.SelectAll.UseVisualStyleBackColor = true;
			this.SelectAll.Click += new System.EventHandler(this.SelectAll_Click);
			// 
			// NESSoundConfig
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(220, 193);
			this.Controls.Add(this.SelectAll);
			this.Controls.Add(this.DMC);
			this.Controls.Add(this.Noise);
			this.Controls.Add(this.Square2);
			this.Controls.Add(this.Triangle);
			this.Controls.Add(this.Square1);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Name = "NESSoundConfig";
			this.ShowIcon = false;
			this.Text = "NES Sound Channels";
			this.Load += new System.EventHandler(this.NESSoundConfig_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.CheckBox Square1;
		private System.Windows.Forms.CheckBox Triangle;
		private System.Windows.Forms.CheckBox Noise;
		private System.Windows.Forms.CheckBox Square2;
		private System.Windows.Forms.CheckBox DMC;
		private System.Windows.Forms.CheckBox SelectAll;
	}
}