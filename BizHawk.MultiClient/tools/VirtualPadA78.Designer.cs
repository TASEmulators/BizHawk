namespace BizHawk.MultiClient
{
	partial class VirtualPadA78
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.PL = new System.Windows.Forms.CheckBox();
			this.PD = new System.Windows.Forms.CheckBox();
			this.PR = new System.Windows.Forms.CheckBox();
			this.PU = new System.Windows.Forms.CheckBox();
			this.B2 = new System.Windows.Forms.CheckBox();
			this.B1 = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// PL
			// 
			this.PL.Appearance = System.Windows.Forms.Appearance.Button;
			this.PL.AutoSize = true;
			this.PL.Image = global::BizHawk.MultiClient.Properties.Resources.Back;
			this.PL.Location = new System.Drawing.Point(32, 19);
			this.PL.Name = "PL";
			this.PL.Size = new System.Drawing.Size(22, 22);
			this.PL.TabIndex = 7;
			this.PL.UseVisualStyleBackColor = true;
			this.PL.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// PD
			// 
			this.PD.Appearance = System.Windows.Forms.Appearance.Button;
			this.PD.AutoSize = true;
			this.PD.Image = global::BizHawk.MultiClient.Properties.Resources.BlueDown;
			this.PD.Location = new System.Drawing.Point(53, 28);
			this.PD.Name = "PD";
			this.PD.Size = new System.Drawing.Size(22, 22);
			this.PD.TabIndex = 6;
			this.PD.UseVisualStyleBackColor = true;
			this.PD.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// PR
			// 
			this.PR.Appearance = System.Windows.Forms.Appearance.Button;
			this.PR.AutoSize = true;
			this.PR.Image = global::BizHawk.MultiClient.Properties.Resources.Forward;
			this.PR.Location = new System.Drawing.Point(74, 19);
			this.PR.Name = "PR";
			this.PR.Size = new System.Drawing.Size(22, 22);
			this.PR.TabIndex = 5;
			this.PR.UseVisualStyleBackColor = true;
			this.PR.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// PU
			// 
			this.PU.Appearance = System.Windows.Forms.Appearance.Button;
			this.PU.AutoSize = true;
			this.PU.Image = global::BizHawk.MultiClient.Properties.Resources.BlueUp;
			this.PU.Location = new System.Drawing.Point(53, 7);
			this.PU.Name = "PU";
			this.PU.Size = new System.Drawing.Size(22, 22);
			this.PU.TabIndex = 4;
			this.PU.UseVisualStyleBackColor = true;
			this.PU.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// B2
			// 
			this.B2.Appearance = System.Windows.Forms.Appearance.Button;
			this.B2.AutoSize = true;
			this.B2.Location = new System.Drawing.Point(93, 57);
			this.B2.Name = "B2";
			this.B2.Size = new System.Drawing.Size(23, 23);
			this.B2.TabIndex = 9;
			this.B2.Text = "2";
			this.B2.UseVisualStyleBackColor = true;
			this.B2.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// B1
			// 
			this.B1.Appearance = System.Windows.Forms.Appearance.Button;
			this.B1.AutoSize = true;
			this.B1.Location = new System.Drawing.Point(10, 57);
			this.B1.Name = "B1";
			this.B1.Size = new System.Drawing.Size(23, 23);
			this.B1.TabIndex = 8;
			this.B1.Text = "1";
			this.B1.UseVisualStyleBackColor = true;
			this.B1.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// VirtualPadA78
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.B2);
			this.Controls.Add(this.B1);
			this.Controls.Add(this.PL);
			this.Controls.Add(this.PD);
			this.Controls.Add(this.PR);
			this.Controls.Add(this.PU);
			this.Name = "VirtualPadA78";
			this.Size = new System.Drawing.Size(128, 89);
			this.Load += new System.EventHandler(this.VirtualPadA78_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox PL;
		private System.Windows.Forms.CheckBox PD;
		private System.Windows.Forms.CheckBox PR;
		private System.Windows.Forms.CheckBox PU;
		private System.Windows.Forms.CheckBox B2;
		private System.Windows.Forms.CheckBox B1;
	}
}
