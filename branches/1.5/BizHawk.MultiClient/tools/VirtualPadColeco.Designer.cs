namespace BizHawk.MultiClient
{
	partial class VirtualPadColeco
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
			this.PU = new System.Windows.Forms.CheckBox();
			this.PR = new System.Windows.Forms.CheckBox();
			this.PD = new System.Windows.Forms.CheckBox();
			this.PL = new System.Windows.Forms.CheckBox();
			this.KeyLeft = new System.Windows.Forms.CheckBox();
			this.KeyRight = new System.Windows.Forms.CheckBox();
			this.KP7 = new System.Windows.Forms.CheckBox();
			this.KP8 = new System.Windows.Forms.CheckBox();
			this.KP9 = new System.Windows.Forms.CheckBox();
			this.KP6 = new System.Windows.Forms.CheckBox();
			this.KP5 = new System.Windows.Forms.CheckBox();
			this.KP4 = new System.Windows.Forms.CheckBox();
			this.KP3 = new System.Windows.Forms.CheckBox();
			this.KP2 = new System.Windows.Forms.CheckBox();
			this.KP1 = new System.Windows.Forms.CheckBox();
			this.KPPound = new System.Windows.Forms.CheckBox();
			this.KP0 = new System.Windows.Forms.CheckBox();
			this.KPStar = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// PU
			// 
			this.PU.Appearance = System.Windows.Forms.Appearance.Button;
			this.PU.AutoSize = true;
			this.PU.Image = global::BizHawk.MultiClient.Properties.Resources.BlueUp;
			this.PU.Location = new System.Drawing.Point(43, 3);
			this.PU.Name = "PU";
			this.PU.Size = new System.Drawing.Size(22, 22);
			this.PU.TabIndex = 0;
			this.PU.UseVisualStyleBackColor = true;
			this.PU.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// PR
			// 
			this.PR.Appearance = System.Windows.Forms.Appearance.Button;
			this.PR.AutoSize = true;
			this.PR.Image = global::BizHawk.MultiClient.Properties.Resources.Forward;
			this.PR.Location = new System.Drawing.Point(64, 15);
			this.PR.Name = "PR";
			this.PR.Size = new System.Drawing.Size(22, 22);
			this.PR.TabIndex = 1;
			this.PR.UseVisualStyleBackColor = true;
			this.PR.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// PD
			// 
			this.PD.Appearance = System.Windows.Forms.Appearance.Button;
			this.PD.AutoSize = true;
			this.PD.Image = global::BizHawk.MultiClient.Properties.Resources.BlueDown;
			this.PD.Location = new System.Drawing.Point(43, 24);
			this.PD.Name = "PD";
			this.PD.Size = new System.Drawing.Size(22, 22);
			this.PD.TabIndex = 2;
			this.PD.UseVisualStyleBackColor = true;
			this.PD.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// PL
			// 
			this.PL.Appearance = System.Windows.Forms.Appearance.Button;
			this.PL.AutoSize = true;
			this.PL.Image = global::BizHawk.MultiClient.Properties.Resources.Back;
			this.PL.Location = new System.Drawing.Point(22, 15);
			this.PL.Name = "PL";
			this.PL.Size = new System.Drawing.Size(22, 22);
			this.PL.TabIndex = 3;
			this.PL.UseVisualStyleBackColor = true;
			this.PL.CheckStateChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KeyLeft
			// 
			this.KeyLeft.Appearance = System.Windows.Forms.Appearance.Button;
			this.KeyLeft.AutoSize = true;
			this.KeyLeft.Location = new System.Drawing.Point(5, 51);
			this.KeyLeft.Name = "KeyLeft";
			this.KeyLeft.Size = new System.Drawing.Size(23, 23);
			this.KeyLeft.TabIndex = 4;
			this.KeyLeft.Text = "L";
			this.KeyLeft.UseVisualStyleBackColor = true;
			this.KeyLeft.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KeyRight
			// 
			this.KeyRight.Appearance = System.Windows.Forms.Appearance.Button;
			this.KeyRight.AutoSize = true;
			this.KeyRight.Location = new System.Drawing.Point(82, 51);
			this.KeyRight.Name = "KeyRight";
			this.KeyRight.Size = new System.Drawing.Size(25, 23);
			this.KeyRight.TabIndex = 5;
			this.KeyRight.Text = "R";
			this.KeyRight.UseVisualStyleBackColor = true;
			this.KeyRight.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP7
			// 
			this.KP7.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP7.AutoSize = true;
			this.KP7.Location = new System.Drawing.Point(22, 129);
			this.KP7.Name = "KP7";
			this.KP7.Size = new System.Drawing.Size(23, 23);
			this.KP7.TabIndex = 6;
			this.KP7.Text = "7";
			this.KP7.UseVisualStyleBackColor = true;
			this.KP7.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP8
			// 
			this.KP8.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP8.AutoSize = true;
			this.KP8.Location = new System.Drawing.Point(45, 129);
			this.KP8.Name = "KP8";
			this.KP8.Size = new System.Drawing.Size(23, 23);
			this.KP8.TabIndex = 8;
			this.KP8.Text = "8";
			this.KP8.UseVisualStyleBackColor = true;
			this.KP8.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP9
			// 
			this.KP9.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP9.AutoSize = true;
			this.KP9.Location = new System.Drawing.Point(67, 129);
			this.KP9.Name = "KP9";
			this.KP9.Size = new System.Drawing.Size(23, 23);
			this.KP9.TabIndex = 8;
			this.KP9.Text = "9";
			this.KP9.UseVisualStyleBackColor = true;
			this.KP9.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP6
			// 
			this.KP6.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP6.AutoSize = true;
			this.KP6.Location = new System.Drawing.Point(67, 106);
			this.KP6.Name = "KP6";
			this.KP6.Size = new System.Drawing.Size(23, 23);
			this.KP6.TabIndex = 10;
			this.KP6.Text = "6";
			this.KP6.UseVisualStyleBackColor = true;
			this.KP6.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP5
			// 
			this.KP5.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP5.AutoSize = true;
			this.KP5.Location = new System.Drawing.Point(45, 106);
			this.KP5.Name = "KP5";
			this.KP5.Size = new System.Drawing.Size(23, 23);
			this.KP5.TabIndex = 11;
			this.KP5.Text = "5";
			this.KP5.UseVisualStyleBackColor = true;
			this.KP5.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP4
			// 
			this.KP4.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP4.AutoSize = true;
			this.KP4.Location = new System.Drawing.Point(22, 106);
			this.KP4.Name = "KP4";
			this.KP4.Size = new System.Drawing.Size(23, 23);
			this.KP4.TabIndex = 9;
			this.KP4.Text = "4";
			this.KP4.UseVisualStyleBackColor = true;
			this.KP4.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP3
			// 
			this.KP3.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP3.AutoSize = true;
			this.KP3.Location = new System.Drawing.Point(67, 83);
			this.KP3.Name = "KP3";
			this.KP3.Size = new System.Drawing.Size(23, 23);
			this.KP3.TabIndex = 13;
			this.KP3.Text = "3";
			this.KP3.UseVisualStyleBackColor = true;
			this.KP3.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP2
			// 
			this.KP2.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP2.AutoSize = true;
			this.KP2.Location = new System.Drawing.Point(45, 83);
			this.KP2.Name = "KP2";
			this.KP2.Size = new System.Drawing.Size(23, 23);
			this.KP2.TabIndex = 14;
			this.KP2.Text = "2";
			this.KP2.UseVisualStyleBackColor = true;
			this.KP2.CheckStateChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP1
			// 
			this.KP1.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP1.AutoSize = true;
			this.KP1.Location = new System.Drawing.Point(22, 83);
			this.KP1.Name = "KP1";
			this.KP1.Size = new System.Drawing.Size(23, 23);
			this.KP1.TabIndex = 12;
			this.KP1.Text = "1";
			this.KP1.UseVisualStyleBackColor = true;
			this.KP1.CheckStateChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KPPound
			// 
			this.KPPound.Appearance = System.Windows.Forms.Appearance.Button;
			this.KPPound.AutoSize = true;
			this.KPPound.Location = new System.Drawing.Point(67, 152);
			this.KPPound.Name = "KPPound";
			this.KPPound.Size = new System.Drawing.Size(24, 23);
			this.KPPound.TabIndex = 16;
			this.KPPound.Text = "#";
			this.KPPound.UseVisualStyleBackColor = true;
			this.KPPound.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KP0
			// 
			this.KP0.Appearance = System.Windows.Forms.Appearance.Button;
			this.KP0.AutoSize = true;
			this.KP0.Location = new System.Drawing.Point(45, 152);
			this.KP0.Name = "KP0";
			this.KP0.Size = new System.Drawing.Size(23, 23);
			this.KP0.TabIndex = 17;
			this.KP0.Text = "0";
			this.KP0.UseVisualStyleBackColor = true;
			this.KP0.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// KPStar
			// 
			this.KPStar.Appearance = System.Windows.Forms.Appearance.Button;
			this.KPStar.AutoSize = true;
			this.KPStar.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.KPStar.Location = new System.Drawing.Point(22, 152);
			this.KPStar.Name = "KPStar";
			this.KPStar.Size = new System.Drawing.Size(21, 23);
			this.KPStar.TabIndex = 15;
			this.KPStar.Text = "*";
			this.KPStar.UseVisualStyleBackColor = true;
			this.KPStar.CheckedChanged += new System.EventHandler(this.Buttons_CheckedChanged);
			// 
			// VirtualPadColeco
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.KPPound);
			this.Controls.Add(this.KP0);
			this.Controls.Add(this.KPStar);
			this.Controls.Add(this.KP3);
			this.Controls.Add(this.KP2);
			this.Controls.Add(this.KP1);
			this.Controls.Add(this.KP6);
			this.Controls.Add(this.KP5);
			this.Controls.Add(this.KP4);
			this.Controls.Add(this.KP9);
			this.Controls.Add(this.KP8);
			this.Controls.Add(this.KP7);
			this.Controls.Add(this.KeyRight);
			this.Controls.Add(this.KeyLeft);
			this.Controls.Add(this.PL);
			this.Controls.Add(this.PD);
			this.Controls.Add(this.PR);
			this.Controls.Add(this.PU);
			this.Name = "VirtualPadColeco";
			this.Size = new System.Drawing.Size(115, 193);
			this.Load += new System.EventHandler(this.VirtualPadColeco_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox PU;
		private System.Windows.Forms.CheckBox PR;
		private System.Windows.Forms.CheckBox PD;
		private System.Windows.Forms.CheckBox PL;
		private System.Windows.Forms.CheckBox KeyLeft;
		private System.Windows.Forms.CheckBox KeyRight;
		private System.Windows.Forms.CheckBox KP7;
		private System.Windows.Forms.CheckBox KP8;
		private System.Windows.Forms.CheckBox KP9;
		private System.Windows.Forms.CheckBox KP6;
		private System.Windows.Forms.CheckBox KP5;
		private System.Windows.Forms.CheckBox KP4;
		private System.Windows.Forms.CheckBox KP3;
		private System.Windows.Forms.CheckBox KP2;
		private System.Windows.Forms.CheckBox KP1;
		private System.Windows.Forms.CheckBox KPPound;
		private System.Windows.Forms.CheckBox KP0;
		private System.Windows.Forms.CheckBox KPStar;
	}
}
