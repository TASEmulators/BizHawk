namespace BizHawk.Client.EmuHawk
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
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label5 = new System.Windows.Forms.Label();
			this.btnAreaFull = new System.Windows.Forms.Button();
			this.btnAreaStandard = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.NTSC_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.NTSC_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OK.Location = new System.Drawing.Point(205, 279);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 4;
			this.OK.Text = "&OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(286, 279);
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
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.btnAreaFull);
			this.groupBox2.Controls.Add(this.btnAreaStandard);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.NTSC_LastLineNumeric);
			this.groupBox2.Controls.Add(this.NTSC_FirstLineNumeric);
			this.groupBox2.Location = new System.Drawing.Point(9, 100);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(352, 150);
			this.groupBox2.TabIndex = 6;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Drawing Area";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(62, 22);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(36, 13);
			this.label5.TabIndex = 41;
			this.label5.Text = "NTSC";
			// 
			// btnAreaFull
			// 
			this.btnAreaFull.Location = new System.Drawing.Point(6, 115);
			this.btnAreaFull.Name = "btnAreaFull";
			this.btnAreaFull.Size = new System.Drawing.Size(100, 23);
			this.btnAreaFull.TabIndex = 40;
			this.btnAreaFull.Text = "Full [0,262]";
			this.btnAreaFull.UseVisualStyleBackColor = true;
			this.btnAreaFull.Click += new System.EventHandler(this.BtnAreaFull_Click);
			// 
			// btnAreaStandard
			// 
			this.btnAreaStandard.Location = new System.Drawing.Point(6, 92);
			this.btnAreaStandard.Name = "btnAreaStandard";
			this.btnAreaStandard.Size = new System.Drawing.Size(100, 23);
			this.btnAreaStandard.TabIndex = 35;
			this.btnAreaStandard.Text = "Standard [18,252]";
			this.btnAreaStandard.UseVisualStyleBackColor = true;
			this.btnAreaStandard.Click += new System.EventHandler(this.BtnAreaStandard_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(4, 69);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(49, 13);
			this.label4.TabIndex = 24;
			this.label4.Text = "Last line:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(5, 43);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(48, 13);
			this.label3.TabIndex = 23;
			this.label3.Text = "First line:";
			// 
			// NTSC_LastLineNumeric
			// 
			this.NTSC_LastLineNumeric.Location = new System.Drawing.Point(59, 67);
			this.NTSC_LastLineNumeric.Maximum = new decimal(new int[] {
            262,
            0,
            0,
            0});
			this.NTSC_LastLineNumeric.Minimum = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this.NTSC_LastLineNumeric.Name = "NTSC_LastLineNumeric";
			this.NTSC_LastLineNumeric.Size = new System.Drawing.Size(47, 20);
			this.NTSC_LastLineNumeric.TabIndex = 28;
			this.NTSC_LastLineNumeric.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
			// 
			// NTSC_FirstLineNumeric
			// 
			this.NTSC_FirstLineNumeric.Location = new System.Drawing.Point(59, 41);
			this.NTSC_FirstLineNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
			this.NTSC_FirstLineNumeric.Name = "NTSC_FirstLineNumeric";
			this.NTSC_FirstLineNumeric.Size = new System.Drawing.Size(47, 20);
			this.NTSC_FirstLineNumeric.TabIndex = 21;
			// 
			// PCEGraphicsConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(373, 311);
			this.Controls.Add(this.groupBox2);
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
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "PC Engine Graphics Settings";
			this.Load += new System.EventHandler(this.PCEGraphicsConfig_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).EndInit();
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
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button btnAreaFull;
		private System.Windows.Forms.Button btnAreaStandard;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown NTSC_LastLineNumeric;
		private System.Windows.Forms.NumericUpDown NTSC_FirstLineNumeric;
	}
}