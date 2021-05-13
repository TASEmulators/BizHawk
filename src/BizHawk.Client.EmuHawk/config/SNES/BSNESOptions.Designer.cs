namespace BizHawk.Client.EmuHawk
{
	partial class BSNESOptions
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbDoubleSize = new System.Windows.Forms.CheckBox();
            this.lblDoubleSize = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblPriority1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.lblPriority0 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Bg4_0Checkbox = new System.Windows.Forms.CheckBox();
            this.Bg3_0Checkbox = new System.Windows.Forms.CheckBox();
            this.Bg2_0Checkbox = new System.Windows.Forms.CheckBox();
            this.Bg1_0Checkbox = new System.Windows.Forms.CheckBox();
            this.Bg4_1Checkbox = new System.Windows.Forms.CheckBox();
            this.Bg3_1Checkbox = new System.Windows.Forms.CheckBox();
            this.Bg2_1Checkbox = new System.Windows.Forms.CheckBox();
            this.Bg1_1Checkbox = new System.Windows.Forms.CheckBox();
            this.Obj4Checkbox = new System.Windows.Forms.CheckBox();
            this.Obj3Checkbox = new System.Windows.Forms.CheckBox();
            this.Obj2Checkbox = new System.Windows.Forms.CheckBox();
            this.Obj1Checkbox = new System.Windows.Forms.CheckBox();
            this.EntropyBox = new System.Windows.Forms.ComboBox();
            this.lblEntropy = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cbGameHotfixes = new System.Windows.Forms.CheckBox();
            this.cbFastPPU = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(136, 303);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(217, 303);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // cbDoubleSize
            // 
            this.cbDoubleSize.AutoSize = true;
            this.cbDoubleSize.Location = new System.Drawing.Point(18, 16);
            this.cbDoubleSize.Name = "cbDoubleSize";
            this.cbDoubleSize.Size = new System.Drawing.Size(178, 17);
            this.cbDoubleSize.TabIndex = 6;
            this.cbDoubleSize.Text = "Always Double-Size Framebuffer";
            this.cbDoubleSize.UseVisualStyleBackColor = true;
            // 
            // lblDoubleSize
            // 
            this.lblDoubleSize.Location = new System.Drawing.Point(33, 34);
            this.lblDoubleSize.MaximumSize = new System.Drawing.Size(260, 0);
            this.lblDoubleSize.Name = "lblDoubleSize";
            this.lblDoubleSize.Text = "Some games are changing the resolution constantly (e.g. SD3) so this option can f" +
    "orce the SNES output to stay double-size always.";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.lblPriority1);
            this.groupBox1.Controls.Add(this.lblPriority0);
            this.groupBox1.Controls.Add(this.Bg4_0Checkbox);
            this.groupBox1.Controls.Add(this.Bg3_0Checkbox);
            this.groupBox1.Controls.Add(this.Bg2_0Checkbox);
            this.groupBox1.Controls.Add(this.Bg1_0Checkbox);
            this.groupBox1.Controls.Add(this.Bg4_1Checkbox);
            this.groupBox1.Controls.Add(this.Bg3_1Checkbox);
            this.groupBox1.Controls.Add(this.Bg2_1Checkbox);
            this.groupBox1.Controls.Add(this.Bg1_1Checkbox);
            this.groupBox1.Controls.Add(this.Obj4Checkbox);
            this.groupBox1.Controls.Add(this.Obj3Checkbox);
            this.groupBox1.Controls.Add(this.Obj2Checkbox);
            this.groupBox1.Controls.Add(this.Obj1Checkbox);
            this.groupBox1.Location = new System.Drawing.Point(18, 165);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(274, 132);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Display";
            // 
            // lblPriority1
            // 
            this.lblPriority1.Location = new System.Drawing.Point(220, 14);
            this.lblPriority1.MaximumSize = new System.Drawing.Size(100, 0);
            this.lblPriority1.Name = "lblPriority1";
            this.lblPriority1.Text = "Priority 1";
            // 
            // lblPriority0
            // 
            this.lblPriority0.Location = new System.Drawing.Point(162, 14);
            this.lblPriority0.MaximumSize = new System.Drawing.Size(100, 0);
            this.lblPriority0.Name = "lblPriority0";
            this.lblPriority0.Text = "Priority 0";
            this.lblPriority0.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Bg4_0Checkbox
            // 
            this.Bg4_0Checkbox.AutoSize = true;
            this.Bg4_0Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Bg4_0Checkbox.Location = new System.Drawing.Point(128, 99);
            this.Bg4_0Checkbox.Name = "Bg4_0Checkbox";
            this.Bg4_0Checkbox.Size = new System.Drawing.Size(62, 17);
            this.Bg4_0Checkbox.TabIndex = 11;
            this.Bg4_0Checkbox.Text = "BG 4    ";
            this.Bg4_0Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg3_0Checkbox
            // 
            this.Bg3_0Checkbox.AutoSize = true;
            this.Bg3_0Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Bg3_0Checkbox.Location = new System.Drawing.Point(128, 76);
            this.Bg3_0Checkbox.Name = "Bg3_0Checkbox";
            this.Bg3_0Checkbox.Size = new System.Drawing.Size(62, 17);
            this.Bg3_0Checkbox.TabIndex = 10;
            this.Bg3_0Checkbox.Text = "BG 3    ";
            this.Bg3_0Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg2_0Checkbox
            // 
            this.Bg2_0Checkbox.AutoSize = true;
            this.Bg2_0Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Bg2_0Checkbox.Location = new System.Drawing.Point(128, 53);
            this.Bg2_0Checkbox.Name = "Bg2_0Checkbox";
            this.Bg2_0Checkbox.Size = new System.Drawing.Size(62, 17);
            this.Bg2_0Checkbox.TabIndex = 9;
            this.Bg2_0Checkbox.Text = "BG 2    ";
            this.Bg2_0Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg1_0Checkbox
            // 
            this.Bg1_0Checkbox.AutoSize = true;
            this.Bg1_0Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.Bg1_0Checkbox.Location = new System.Drawing.Point(128, 30);
            this.Bg1_0Checkbox.Name = "Bg1_0Checkbox";
            this.Bg1_0Checkbox.Size = new System.Drawing.Size(62, 17);
            this.Bg1_0Checkbox.TabIndex = 8;
            this.Bg1_0Checkbox.Text = "BG 1    ";
            this.Bg1_0Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg4_1Checkbox
            // 
            this.Bg4_1Checkbox.AutoSize = true;
            this.Bg4_1Checkbox.Location = new System.Drawing.Point(234, 100);
            this.Bg4_1Checkbox.Name = "Bg4_1Checkbox";
            this.Bg4_1Checkbox.Size = new System.Drawing.Size(15, 14);
            this.Bg4_1Checkbox.TabIndex = 7;
            this.Bg4_1Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg3_1Checkbox
            // 
            this.Bg3_1Checkbox.AutoSize = true;
            this.Bg3_1Checkbox.Location = new System.Drawing.Point(234, 77);
            this.Bg3_1Checkbox.Name = "Bg3_1Checkbox";
            this.Bg3_1Checkbox.Size = new System.Drawing.Size(15, 14);
            this.Bg3_1Checkbox.TabIndex = 6;
            this.Bg3_1Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg2_1Checkbox
            // 
            this.Bg2_1Checkbox.AutoSize = true;
            this.Bg2_1Checkbox.Location = new System.Drawing.Point(234, 54);
            this.Bg2_1Checkbox.Name = "Bg2_1Checkbox";
            this.Bg2_1Checkbox.Size = new System.Drawing.Size(15, 14);
            this.Bg2_1Checkbox.TabIndex = 5;
            this.Bg2_1Checkbox.UseVisualStyleBackColor = true;
            // 
            // Bg1_1Checkbox
            // 
            this.Bg1_1Checkbox.AutoSize = true;
            this.Bg1_1Checkbox.Location = new System.Drawing.Point(234, 31);
            this.Bg1_1Checkbox.Name = "Bg1_1Checkbox";
            this.Bg1_1Checkbox.Size = new System.Drawing.Size(15, 14);
            this.Bg1_1Checkbox.TabIndex = 4;
            this.Bg1_1Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj4Checkbox
            // 
            this.Obj4Checkbox.AutoSize = true;
            this.Obj4Checkbox.Location = new System.Drawing.Point(21, 99);
            this.Obj4Checkbox.Name = "Obj4Checkbox";
            this.Obj4Checkbox.Size = new System.Drawing.Size(55, 17);
            this.Obj4Checkbox.TabIndex = 3;
            this.Obj4Checkbox.Text = "OBJ 4";
            this.Obj4Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj3Checkbox
            // 
            this.Obj3Checkbox.AutoSize = true;
            this.Obj3Checkbox.Location = new System.Drawing.Point(21, 76);
            this.Obj3Checkbox.Name = "Obj3Checkbox";
            this.Obj3Checkbox.Size = new System.Drawing.Size(55, 17);
            this.Obj3Checkbox.TabIndex = 2;
            this.Obj3Checkbox.Text = "OBJ 3";
            this.Obj3Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj2Checkbox
            // 
            this.Obj2Checkbox.AutoSize = true;
            this.Obj2Checkbox.Location = new System.Drawing.Point(21, 53);
            this.Obj2Checkbox.Name = "Obj2Checkbox";
            this.Obj2Checkbox.Size = new System.Drawing.Size(55, 17);
            this.Obj2Checkbox.TabIndex = 1;
            this.Obj2Checkbox.Text = "OBJ 2";
            this.Obj2Checkbox.UseVisualStyleBackColor = true;
            // 
            // Obj1Checkbox
            // 
            this.Obj1Checkbox.AutoSize = true;
            this.Obj1Checkbox.Location = new System.Drawing.Point(21, 30);
            this.Obj1Checkbox.Name = "Obj1Checkbox";
            this.Obj1Checkbox.Size = new System.Drawing.Size(55, 17);
            this.Obj1Checkbox.TabIndex = 0;
            this.Obj1Checkbox.Text = "OBJ 1";
            this.Obj1Checkbox.UseVisualStyleBackColor = true;
            // 
            // EntropyBox
            // 
            this.EntropyBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EntropyBox.FormattingEnabled = true;
            this.EntropyBox.Items.AddRange(new object[] {
            "None",
            "Low",
            "High"});
            this.EntropyBox.Location = new System.Drawing.Point(164, 138);
            this.EntropyBox.Name = "EntropyBox";
            this.EntropyBox.Size = new System.Drawing.Size(128, 21);
            this.EntropyBox.TabIndex = 14;
            // 
            // lblEntropy
            // 
            this.lblEntropy.Location = new System.Drawing.Point(249, 117);
            this.lblEntropy.Name = "lblEntropy";
            this.lblEntropy.Text = "Entropy";
            // 
            // cbGameHotfixes
            // 
            this.cbGameHotfixes.AutoSize = true;
            this.cbGameHotfixes.Location = new System.Drawing.Point(18, 91);
            this.cbGameHotfixes.Name = "cbGameHotfixes";
            this.cbGameHotfixes.Size = new System.Drawing.Size(93, 17);
            this.cbGameHotfixes.TabIndex = 22;
            this.cbGameHotfixes.Text = "Game hotfixes";
            this.cbGameHotfixes.UseVisualStyleBackColor = true;
            // 
            // cbFastPPU
            // 
            this.cbFastPPU.AutoSize = true;
            this.cbFastPPU.Location = new System.Drawing.Point(18, 128);
            this.cbFastPPU.Name = "cbFastPPU";
            this.cbFastPPU.Size = new System.Drawing.Size(90, 17);
            this.cbFastPPU.TabIndex = 23;
            this.cbFastPPU.Text = "Use fast PPU";
            this.cbFastPPU.UseVisualStyleBackColor = true;
            this.cbFastPPU.CheckedChanged += new System.EventHandler(this.FastPPU_CheckedChanged);
            // 
            // BSNESOptions
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(304, 338);
            this.Controls.Add(this.cbFastPPU);
            this.Controls.Add(this.cbGameHotfixes);
            this.Controls.Add(this.lblEntropy);
            this.Controls.Add(this.EntropyBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lblDoubleSize);
            this.Controls.Add(this.cbDoubleSize);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BSNESOptions";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BSNES Options";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.CheckBox cbDoubleSize;
		private BizHawk.WinForms.Controls.LocLabelEx lblDoubleSize;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox Bg4_1Checkbox;
		private System.Windows.Forms.CheckBox Bg3_1Checkbox;
		private System.Windows.Forms.CheckBox Bg2_1Checkbox;
		private System.Windows.Forms.CheckBox Bg1_1Checkbox;
		private System.Windows.Forms.CheckBox Obj4Checkbox;
		private System.Windows.Forms.CheckBox Obj3Checkbox;
		private System.Windows.Forms.CheckBox Obj2Checkbox;
		private System.Windows.Forms.CheckBox Obj1Checkbox;
		private System.Windows.Forms.ComboBox EntropyBox;
		private WinForms.Controls.LocLabelEx lblEntropy;
		private System.Windows.Forms.CheckBox cbGameHotfixes;
		private System.Windows.Forms.CheckBox cbFastPPU;
		private System.Windows.Forms.CheckBox Bg1_0Checkbox;
		private System.Windows.Forms.CheckBox Bg4_0Checkbox;
		private System.Windows.Forms.CheckBox Bg3_0Checkbox;
		private System.Windows.Forms.CheckBox Bg2_0Checkbox;
		private WinForms.Controls.LocLabelEx lblPriority1;
		private WinForms.Controls.LocLabelEx lblPriority0;
	}
}
