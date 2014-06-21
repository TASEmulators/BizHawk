namespace BizHawk.Client.EmuHawk
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
            this.ManualX = new System.Windows.Forms.NumericUpDown();
            this.ManualY = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.CD = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.CR = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.CL = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.CU = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.BA = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.BB = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.BZ = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.BS = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.BR = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.BL = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.PL = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.PD = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.PR = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.PU = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.AnalogControl1 = new BizHawk.Client.EmuHawk.AnalogControlPanel();
            ((System.ComponentModel.ISupportInitialize)(this.ManualX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ManualY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            this.SuspendLayout();
            // 
            // ManualX
            // 
            this.ManualX.Location = new System.Drawing.Point(144, 30);
            this.ManualX.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.ManualX.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
            this.ManualX.Name = "ManualX";
            this.ManualX.Size = new System.Drawing.Size(47, 20);
            this.ManualX.TabIndex = 20;
            this.ManualX.ValueChanged += new System.EventHandler(this.ManualX_ValueChanged);
            this.ManualX.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ManualX_KeyUp);
            // 
            // ManualY
            // 
            this.ManualY.Location = new System.Drawing.Point(144, 69);
            this.ManualY.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.ManualY.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
            this.ManualY.Name = "ManualY";
            this.ManualY.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ManualY.Size = new System.Drawing.Size(47, 20);
            this.ManualY.TabIndex = 21;
            this.ManualY.ValueChanged += new System.EventHandler(this.ManualY_ValueChanged);
            this.ManualY.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ManualY_KeyUp);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(144, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(14, 13);
            this.label1.TabIndex = 22;
            this.label1.Text = "X";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(144, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(14, 13);
            this.label2.TabIndex = 23;
            this.label2.Text = "Y";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(4, 269);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(55, 20);
            this.numericUpDown1.TabIndex = 24;
            this.numericUpDown1.TabStop = false;
            this.numericUpDown1.Value = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.numericUpDown1.Visible = false;
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.Location = new System.Drawing.Point(4, 292);
            this.numericUpDown2.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.numericUpDown2.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(55, 20);
            this.numericUpDown2.TabIndex = 25;
            this.numericUpDown2.TabStop = false;
            this.numericUpDown2.Value = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
            this.numericUpDown2.Visible = false;
            this.numericUpDown2.ValueChanged += new System.EventHandler(this.numericUpDown2_ValueChanged);
            // 
            // CD
            // 
            this.CD.Appearance = System.Windows.Forms.Appearance.Button;
            this.CD.AutoSize = true;
            this.CD.ForeColor = System.Drawing.Color.Black;
            this.CD.Location = new System.Drawing.Point(147, 281);
            this.CD.Name = "CD";
            this.CD.Size = new System.Drawing.Size(31, 23);
            this.CD.TabIndex = 17;
            this.CD.TabStop = false;
            this.CD.Text = "cD";
            this.CD.UseVisualStyleBackColor = true;
            // 
            // CR
            // 
            this.CR.Appearance = System.Windows.Forms.Appearance.Button;
            this.CR.AutoSize = true;
            this.CR.ForeColor = System.Drawing.Color.Black;
            this.CR.Location = new System.Drawing.Point(164, 258);
            this.CR.Name = "CR";
            this.CR.Size = new System.Drawing.Size(31, 23);
            this.CR.TabIndex = 16;
            this.CR.TabStop = false;
            this.CR.Text = "cR";
            this.CR.UseVisualStyleBackColor = true;
            // 
            // CL
            // 
            this.CL.Appearance = System.Windows.Forms.Appearance.Button;
            this.CL.AutoSize = true;
            this.CL.ForeColor = System.Drawing.Color.Black;
            this.CL.Location = new System.Drawing.Point(129, 258);
            this.CL.Name = "CL";
            this.CL.Size = new System.Drawing.Size(29, 23);
            this.CL.TabIndex = 15;
            this.CL.TabStop = false;
            this.CL.Text = "cL";
            this.CL.UseVisualStyleBackColor = true;
            // 
            // CU
            // 
            this.CU.Appearance = System.Windows.Forms.Appearance.Button;
            this.CU.AutoSize = true;
            this.CU.ForeColor = System.Drawing.Color.Black;
            this.CU.Location = new System.Drawing.Point(147, 235);
            this.CU.Name = "CU";
            this.CU.Size = new System.Drawing.Size(31, 23);
            this.CU.TabIndex = 14;
            this.CU.TabStop = false;
            this.CU.Text = "cU";
            this.CU.UseVisualStyleBackColor = true;
            // 
            // BA
            // 
            this.BA.Appearance = System.Windows.Forms.Appearance.Button;
            this.BA.AutoSize = true;
            this.BA.ForeColor = System.Drawing.Color.Black;
            this.BA.Location = new System.Drawing.Point(113, 206);
            this.BA.Name = "BA";
            this.BA.Size = new System.Drawing.Size(24, 23);
            this.BA.TabIndex = 13;
            this.BA.TabStop = false;
            this.BA.Text = "A";
            this.BA.UseVisualStyleBackColor = true;
            // 
            // BB
            // 
            this.BB.Appearance = System.Windows.Forms.Appearance.Button;
            this.BB.AutoSize = true;
            this.BB.ForeColor = System.Drawing.Color.Black;
            this.BB.Location = new System.Drawing.Point(83, 195);
            this.BB.Name = "BB";
            this.BB.Size = new System.Drawing.Size(24, 23);
            this.BB.TabIndex = 12;
            this.BB.TabStop = false;
            this.BB.Text = "B";
            this.BB.UseVisualStyleBackColor = true;
            // 
            // BZ
            // 
            this.BZ.Appearance = System.Windows.Forms.Appearance.Button;
            this.BZ.AutoSize = true;
            this.BZ.ForeColor = System.Drawing.Color.Black;
            this.BZ.Location = new System.Drawing.Point(74, 245);
            this.BZ.Name = "BZ";
            this.BZ.Size = new System.Drawing.Size(24, 23);
            this.BZ.TabIndex = 11;
            this.BZ.TabStop = false;
            this.BZ.Text = "Z";
            this.BZ.UseVisualStyleBackColor = true;
            // 
            // BS
            // 
            this.BS.Appearance = System.Windows.Forms.Appearance.Button;
            this.BS.AutoSize = true;
            this.BS.ForeColor = System.Drawing.Color.Black;
            this.BS.Location = new System.Drawing.Point(87, 157);
            this.BS.Name = "BS";
            this.BS.Size = new System.Drawing.Size(24, 23);
            this.BS.TabIndex = 10;
            this.BS.TabStop = false;
            this.BS.Text = "S";
            this.BS.UseVisualStyleBackColor = true;
            // 
            // BR
            // 
            this.BR.Appearance = System.Windows.Forms.Appearance.Button;
            this.BR.AutoSize = true;
            this.BR.ForeColor = System.Drawing.Color.Black;
            this.BR.Location = new System.Drawing.Point(172, 148);
            this.BR.Name = "BR";
            this.BR.Size = new System.Drawing.Size(25, 23);
            this.BR.TabIndex = 9;
            this.BR.TabStop = false;
            this.BR.Text = "R";
            this.BR.UseVisualStyleBackColor = true;
            // 
            // BL
            // 
            this.BL.Appearance = System.Windows.Forms.Appearance.Button;
            this.BL.AutoSize = true;
            this.BL.ForeColor = System.Drawing.Color.Black;
            this.BL.Location = new System.Drawing.Point(3, 148);
            this.BL.Name = "BL";
            this.BL.Size = new System.Drawing.Size(23, 23);
            this.BL.TabIndex = 8;
            this.BL.TabStop = false;
            this.BL.Text = "L";
            this.BL.UseVisualStyleBackColor = true;
            // 
            // PL
            // 
            this.PL.Appearance = System.Windows.Forms.Appearance.Button;
            this.PL.AutoSize = true;
            this.PL.ForeColor = System.Drawing.Color.Black;
            this.PL.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Back;
            this.PL.Location = new System.Drawing.Point(3, 207);
            this.PL.Name = "PL";
            this.PL.Size = new System.Drawing.Size(22, 22);
            this.PL.TabIndex = 7;
            this.PL.TabStop = false;
            this.PL.UseVisualStyleBackColor = true;
            // 
            // PD
            // 
            this.PD.Appearance = System.Windows.Forms.Appearance.Button;
            this.PD.AutoSize = true;
            this.PD.ForeColor = System.Drawing.Color.Black;
            this.PD.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.BlueDown;
            this.PD.Location = new System.Drawing.Point(24, 216);
            this.PD.Name = "PD";
            this.PD.Size = new System.Drawing.Size(22, 22);
            this.PD.TabIndex = 6;
            this.PD.TabStop = false;
            this.PD.UseVisualStyleBackColor = true;
            // 
            // PR
            // 
            this.PR.Appearance = System.Windows.Forms.Appearance.Button;
            this.PR.AutoSize = true;
            this.PR.ForeColor = System.Drawing.Color.Black;
            this.PR.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Forward;
            this.PR.Location = new System.Drawing.Point(45, 207);
            this.PR.Name = "PR";
            this.PR.Size = new System.Drawing.Size(22, 22);
            this.PR.TabIndex = 5;
            this.PR.TabStop = false;
            this.PR.UseVisualStyleBackColor = true;
            // 
            // PU
            // 
            this.PU.Appearance = System.Windows.Forms.Appearance.Button;
            this.PU.AutoSize = true;
            this.PU.ForeColor = System.Drawing.Color.Black;
            this.PU.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.BlueUp;
            this.PU.Location = new System.Drawing.Point(24, 195);
            this.PU.Name = "PU";
            this.PU.Size = new System.Drawing.Size(22, 22);
            this.PU.TabIndex = 4;
            this.PU.TabStop = false;
            this.PU.UseVisualStyleBackColor = true;
            // 
            // AnalogControl1
            // 
            this.AnalogControl1.BackColor = System.Drawing.Color.Transparent;
            this.AnalogControl1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.AnalogControl1.Location = new System.Drawing.Point(6, 14);
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
            this.Controls.Add(this.numericUpDown2);
            this.Controls.Add(this.numericUpDown1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ManualY);
            this.Controls.Add(this.ManualX);
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
            this.Size = new System.Drawing.Size(200, 316);
            this.Load += new System.EventHandler(this.UserControl1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ManualX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ManualY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private AnalogControlPanel AnalogControl1;
		private VirtualPadButton PL;
		private VirtualPadButton PD;
		private VirtualPadButton PR;
		private VirtualPadButton PU;
		private VirtualPadButton BL;
		private VirtualPadButton BR;
		private VirtualPadButton BS;
		private VirtualPadButton BZ;
		private VirtualPadButton BB;
		private VirtualPadButton BA;
		private VirtualPadButton CU;
		private VirtualPadButton CL;
		private VirtualPadButton CR;
		private VirtualPadButton CD;
		private System.Windows.Forms.NumericUpDown ManualY;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown ManualX;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
	}
}
