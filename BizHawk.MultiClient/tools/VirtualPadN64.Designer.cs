namespace BizHawk.MultiClient
{
	partial class VirtualPadN64
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
			this.BL = new System.Windows.Forms.CheckBox();
			this.BR = new System.Windows.Forms.CheckBox();
			this.BS = new System.Windows.Forms.CheckBox();
			this.BZ = new System.Windows.Forms.CheckBox();
			this.BB = new System.Windows.Forms.CheckBox();
			this.BA = new System.Windows.Forms.CheckBox();
			this.CU = new System.Windows.Forms.CheckBox();
			this.CL = new System.Windows.Forms.CheckBox();
			this.CR = new System.Windows.Forms.CheckBox();
			this.CD = new System.Windows.Forms.CheckBox();
			this.AnalogControl1 = new BizHawk.MultiClient.AnalogControlPanel();
			this.SuspendLayout();
			// 
			// PL
			// 
			this.PL.Appearance = System.Windows.Forms.Appearance.Button;
			this.PL.AutoSize = true;
			this.PL.Image = global::BizHawk.MultiClient.Properties.Resources.Back;
			this.PL.Location = new System.Drawing.Point(3, 207);
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
			this.PD.Location = new System.Drawing.Point(24, 216);
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
			this.PR.Location = new System.Drawing.Point(45, 207);
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
			this.PU.Location = new System.Drawing.Point(24, 195);
			this.PU.Name = "PU";
			this.PU.Size = new System.Drawing.Size(22, 22);
			this.PU.TabIndex = 4;
			this.PU.UseVisualStyleBackColor = true;
			this.PU.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// BL
			// 
			this.BL.Appearance = System.Windows.Forms.Appearance.Button;
			this.BL.AutoSize = true;
			this.BL.Location = new System.Drawing.Point(3, 148);
			this.BL.Name = "BL";
			this.BL.Size = new System.Drawing.Size(23, 23);
			this.BL.TabIndex = 8;
			this.BL.Text = "L";
			this.BL.UseVisualStyleBackColor = true;
			this.BL.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// BR
			// 
			this.BR.Appearance = System.Windows.Forms.Appearance.Button;
			this.BR.AutoSize = true;
			this.BR.Location = new System.Drawing.Point(138, 148);
			this.BR.Name = "BR";
			this.BR.Size = new System.Drawing.Size(25, 23);
			this.BR.TabIndex = 9;
			this.BR.Text = "R";
			this.BR.UseVisualStyleBackColor = true;
			this.BR.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// BS
			// 
			this.BS.Appearance = System.Windows.Forms.Appearance.Button;
			this.BS.AutoSize = true;
			this.BS.Location = new System.Drawing.Point(74, 157);
			this.BS.Name = "BS";
			this.BS.Size = new System.Drawing.Size(24, 23);
			this.BS.TabIndex = 10;
			this.BS.Text = "S";
			this.BS.UseVisualStyleBackColor = true;
			this.BS.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// BZ
			// 
			this.BZ.Appearance = System.Windows.Forms.Appearance.Button;
			this.BZ.AutoSize = true;
			this.BZ.Location = new System.Drawing.Point(74, 245);
			this.BZ.Name = "BZ";
			this.BZ.Size = new System.Drawing.Size(24, 23);
			this.BZ.TabIndex = 11;
			this.BZ.Text = "Z";
			this.BZ.UseVisualStyleBackColor = true;
			this.BZ.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// BB
			// 
			this.BB.Appearance = System.Windows.Forms.Appearance.Button;
			this.BB.AutoSize = true;
			this.BB.Location = new System.Drawing.Point(98, 195);
			this.BB.Name = "BB";
			this.BB.Size = new System.Drawing.Size(24, 23);
			this.BB.TabIndex = 12;
			this.BB.Text = "B";
			this.BB.UseVisualStyleBackColor = true;
			this.BB.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// BA
			// 
			this.BA.Appearance = System.Windows.Forms.Appearance.Button;
			this.BA.AutoSize = true;
			this.BA.Location = new System.Drawing.Point(128, 206);
			this.BA.Name = "BA";
			this.BA.Size = new System.Drawing.Size(24, 23);
			this.BA.TabIndex = 13;
			this.BA.Text = "A";
			this.BA.UseVisualStyleBackColor = true;
			this.BA.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// CU
			// 
			this.CU.Appearance = System.Windows.Forms.Appearance.Button;
			this.CU.AutoSize = true;
			this.CU.Location = new System.Drawing.Point(138, 235);
			this.CU.Name = "CU";
			this.CU.Size = new System.Drawing.Size(31, 23);
			this.CU.TabIndex = 14;
			this.CU.Text = "cU";
			this.CU.UseVisualStyleBackColor = true;
			this.CU.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// CL
			// 
			this.CL.Appearance = System.Windows.Forms.Appearance.Button;
			this.CL.AutoSize = true;
			this.CL.Location = new System.Drawing.Point(116, 258);
			this.CL.Name = "CL";
			this.CL.Size = new System.Drawing.Size(29, 23);
			this.CL.TabIndex = 15;
			this.CL.Text = "cL";
			this.CL.UseVisualStyleBackColor = true;
			this.CL.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// CR
			// 
			this.CR.Appearance = System.Windows.Forms.Appearance.Button;
			this.CR.AutoSize = true;
			this.CR.Location = new System.Drawing.Point(151, 259);
			this.CR.Name = "CR";
			this.CR.Size = new System.Drawing.Size(31, 23);
			this.CR.TabIndex = 16;
			this.CR.Text = "cR";
			this.CR.UseVisualStyleBackColor = true;
			this.CR.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// CD
			// 
			this.CD.Appearance = System.Windows.Forms.Appearance.Button;
			this.CD.AutoSize = true;
			this.CD.Location = new System.Drawing.Point(138, 284);
			this.CD.Name = "CD";
			this.CD.Size = new System.Drawing.Size(31, 23);
			this.CD.TabIndex = 17;
			this.CD.Text = "cD";
			this.CD.UseVisualStyleBackColor = true;
			this.CD.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// AnalogControl1
			// 
			this.AnalogControl1.BackColor = System.Drawing.Color.Transparent;
			this.AnalogControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.AnalogControl1.Location = new System.Drawing.Point(24, 14);
			this.AnalogControl1.Name = "AnalogControl1";
			this.AnalogControl1.Size = new System.Drawing.Size(132, 132);
			this.AnalogControl1.TabIndex = 0;
			this.AnalogControl1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.AnalogControl1_MouseClick);
			this.AnalogControl1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AnalogControl1_MouseMove);
			// 
			// VirtualPadN64
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.CD);
			this.Controls.Add(this.CR);
			this.Controls.Add(this.CL);
			this.Controls.Add(this.CU);
			this.Controls.Add(this.BA);
			this.Controls.Add(this.BB);
			this.Controls.Add(this.BZ);
			this.Controls.Add(this.BS);
			this.Controls.Add(this.BR);
			this.Controls.Add(this.BL);
			this.Controls.Add(this.PL);
			this.Controls.Add(this.PD);
			this.Controls.Add(this.PR);
			this.Controls.Add(this.PU);
			this.Controls.Add(this.AnalogControl1);
			this.Name = "VirtualPadN64";
			this.Size = new System.Drawing.Size(200, 332);
			this.Load += new System.EventHandler(this.UserControl1_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private AnalogControlPanel AnalogControl1;
		private System.Windows.Forms.CheckBox PL;
		private System.Windows.Forms.CheckBox PD;
		private System.Windows.Forms.CheckBox PR;
		private System.Windows.Forms.CheckBox PU;
		private System.Windows.Forms.CheckBox BL;
		private System.Windows.Forms.CheckBox BR;
		private System.Windows.Forms.CheckBox BS;
		private System.Windows.Forms.CheckBox BZ;
		private System.Windows.Forms.CheckBox BB;
		private System.Windows.Forms.CheckBox BA;
		private System.Windows.Forms.CheckBox CU;
		private System.Windows.Forms.CheckBox CL;
		private System.Windows.Forms.CheckBox CR;
		private System.Windows.Forms.CheckBox CD;
	}
}
