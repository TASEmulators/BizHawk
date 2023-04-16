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
			this.RegionBox = new System.Windows.Forms.ComboBox();
			this.cbGameHotfixes = new System.Windows.Forms.CheckBox();
			this.cbFastPPU = new System.Windows.Forms.CheckBox();
			this.cbCropSGBFrame = new System.Windows.Forms.CheckBox();
			this.cbUseSGB2 = new System.Windows.Forms.CheckBox();
			this.cbFastDSP = new System.Windows.Forms.CheckBox();
			this.cbFastCoprocessor = new System.Windows.Forms.CheckBox();
			this.cbNoPPUSpriteLimit = new System.Windows.Forms.CheckBox();
			this.AspectRatioCorrectionBox = new System.Windows.Forms.ComboBox();
			this.cbShowOverscan = new System.Windows.Forms.CheckBox();
			this.cbShowCursor = new System.Windows.Forms.CheckBox();
			this.SatellaviewCartridgeBox = new System.Windows.Forms.ComboBox();
			this.lblSatellaviewCartridge = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblAspectRatioCorrection = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblRegion = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblEntropy = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblDoubleSize = new BizHawk.WinForms.Controls.LocLabelEx();
			this.cbUseRealTime = new System.Windows.Forms.CheckBox();
			this.dtpInitialTime = new System.Windows.Forms.DateTimePicker();
			this.lblInitialTime = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(136, 439);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 26;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(217, 439);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 0;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
			// 
			// cbDoubleSize
			// 
			this.cbDoubleSize.AutoSize = true;
			this.cbDoubleSize.Location = new System.Drawing.Point(16, 16);
			this.cbDoubleSize.Name = "cbDoubleSize";
			this.cbDoubleSize.Size = new System.Drawing.Size(178, 17);
			this.cbDoubleSize.TabIndex = 1;
			this.cbDoubleSize.Text = "Always Double-Size Framebuffer";
			this.cbDoubleSize.UseVisualStyleBackColor = true;
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
			this.groupBox1.Location = new System.Drawing.Point(18, 316);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(274, 115);
			this.groupBox1.TabIndex = 13;
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
			this.Bg4_0Checkbox.Location = new System.Drawing.Point(128, 93);
			this.Bg4_0Checkbox.Name = "Bg4_0Checkbox";
			this.Bg4_0Checkbox.Size = new System.Drawing.Size(62, 17);
			this.Bg4_0Checkbox.TabIndex = 24;
			this.Bg4_0Checkbox.Text = "BG 4    ";
			this.Bg4_0Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg3_0Checkbox
			// 
			this.Bg3_0Checkbox.AutoSize = true;
			this.Bg3_0Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.Bg3_0Checkbox.Location = new System.Drawing.Point(128, 72);
			this.Bg3_0Checkbox.Name = "Bg3_0Checkbox";
			this.Bg3_0Checkbox.Size = new System.Drawing.Size(62, 17);
			this.Bg3_0Checkbox.TabIndex = 22;
			this.Bg3_0Checkbox.Text = "BG 3    ";
			this.Bg3_0Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg2_0Checkbox
			// 
			this.Bg2_0Checkbox.AutoSize = true;
			this.Bg2_0Checkbox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.Bg2_0Checkbox.Location = new System.Drawing.Point(128, 51);
			this.Bg2_0Checkbox.Name = "Bg2_0Checkbox";
			this.Bg2_0Checkbox.Size = new System.Drawing.Size(62, 17);
			this.Bg2_0Checkbox.TabIndex = 20;
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
			this.Bg1_0Checkbox.TabIndex = 18;
			this.Bg1_0Checkbox.Text = "BG 1    ";
			this.Bg1_0Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg4_1Checkbox
			// 
			this.Bg4_1Checkbox.AutoSize = true;
			this.Bg4_1Checkbox.Location = new System.Drawing.Point(234, 94);
			this.Bg4_1Checkbox.Name = "Bg4_1Checkbox";
			this.Bg4_1Checkbox.Size = new System.Drawing.Size(15, 14);
			this.Bg4_1Checkbox.TabIndex = 25;
			this.Bg4_1Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg3_1Checkbox
			// 
			this.Bg3_1Checkbox.AutoSize = true;
			this.Bg3_1Checkbox.Location = new System.Drawing.Point(234, 73);
			this.Bg3_1Checkbox.Name = "Bg3_1Checkbox";
			this.Bg3_1Checkbox.Size = new System.Drawing.Size(15, 14);
			this.Bg3_1Checkbox.TabIndex = 23;
			this.Bg3_1Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg2_1Checkbox
			// 
			this.Bg2_1Checkbox.AutoSize = true;
			this.Bg2_1Checkbox.Location = new System.Drawing.Point(234, 52);
			this.Bg2_1Checkbox.Name = "Bg2_1Checkbox";
			this.Bg2_1Checkbox.Size = new System.Drawing.Size(15, 14);
			this.Bg2_1Checkbox.TabIndex = 21;
			this.Bg2_1Checkbox.UseVisualStyleBackColor = true;
			// 
			// Bg1_1Checkbox
			// 
			this.Bg1_1Checkbox.AutoSize = true;
			this.Bg1_1Checkbox.Location = new System.Drawing.Point(234, 31);
			this.Bg1_1Checkbox.Name = "Bg1_1Checkbox";
			this.Bg1_1Checkbox.Size = new System.Drawing.Size(15, 14);
			this.Bg1_1Checkbox.TabIndex = 19;
			this.Bg1_1Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj4Checkbox
			// 
			this.Obj4Checkbox.AutoSize = true;
			this.Obj4Checkbox.Location = new System.Drawing.Point(21, 93);
			this.Obj4Checkbox.Name = "Obj4Checkbox";
			this.Obj4Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj4Checkbox.TabIndex = 17;
			this.Obj4Checkbox.Text = "OBJ 4";
			this.Obj4Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj3Checkbox
			// 
			this.Obj3Checkbox.AutoSize = true;
			this.Obj3Checkbox.Location = new System.Drawing.Point(21, 72);
			this.Obj3Checkbox.Name = "Obj3Checkbox";
			this.Obj3Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj3Checkbox.TabIndex = 16;
			this.Obj3Checkbox.Text = "OBJ 3";
			this.Obj3Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj2Checkbox
			// 
			this.Obj2Checkbox.AutoSize = true;
			this.Obj2Checkbox.Location = new System.Drawing.Point(21, 51);
			this.Obj2Checkbox.Name = "Obj2Checkbox";
			this.Obj2Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj2Checkbox.TabIndex = 15;
			this.Obj2Checkbox.Text = "OBJ 2";
			this.Obj2Checkbox.UseVisualStyleBackColor = true;
			// 
			// Obj1Checkbox
			// 
			this.Obj1Checkbox.AutoSize = true;
			this.Obj1Checkbox.Location = new System.Drawing.Point(21, 30);
			this.Obj1Checkbox.Name = "Obj1Checkbox";
			this.Obj1Checkbox.Size = new System.Drawing.Size(55, 17);
			this.Obj1Checkbox.TabIndex = 14;
			this.Obj1Checkbox.Text = "OBJ 1";
			this.Obj1Checkbox.UseVisualStyleBackColor = true;
			// 
			// EntropyBox
			// 
			this.EntropyBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.EntropyBox.FormattingEnabled = true;
			this.EntropyBox.Location = new System.Drawing.Point(16, 249);
			this.EntropyBox.Name = "EntropyBox";
			this.EntropyBox.Size = new System.Drawing.Size(128, 21);
			this.EntropyBox.TabIndex = 10;
			// 
			// RegionBox
			// 
			this.RegionBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.RegionBox.FormattingEnabled = true;
			this.RegionBox.Location = new System.Drawing.Point(161, 289);
			this.RegionBox.Name = "RegionBox";
			this.RegionBox.Size = new System.Drawing.Size(128, 21);
			this.RegionBox.TabIndex = 13;
			// 
			// cbGameHotfixes
			// 
			this.cbGameHotfixes.AutoSize = true;
			this.cbGameHotfixes.Location = new System.Drawing.Point(161, 81);
			this.cbGameHotfixes.Name = "cbGameHotfixes";
			this.cbGameHotfixes.Size = new System.Drawing.Size(93, 17);
			this.cbGameHotfixes.TabIndex = 3;
			this.cbGameHotfixes.Text = "Game hotfixes";
			this.cbGameHotfixes.UseVisualStyleBackColor = true;
			// 
			// cbFastPPU
			// 
			this.cbFastPPU.AutoSize = true;
			this.cbFastPPU.Location = new System.Drawing.Point(161, 129);
			this.cbFastPPU.Name = "cbFastPPU";
			this.cbFastPPU.Size = new System.Drawing.Size(90, 17);
			this.cbFastPPU.TabIndex = 7;
			this.cbFastPPU.Text = "Use fast PPU";
			this.cbFastPPU.UseVisualStyleBackColor = true;
			this.cbFastPPU.CheckedChanged += new System.EventHandler(this.FastPPU_CheckedChanged);
			// 
			// cbCropSGBFrame
			// 
			this.cbCropSGBFrame.AutoSize = true;
			this.cbCropSGBFrame.Location = new System.Drawing.Point(16, 81);
			this.cbCropSGBFrame.Name = "cbCropSGBFrame";
			this.cbCropSGBFrame.Size = new System.Drawing.Size(105, 17);
			this.cbCropSGBFrame.TabIndex = 2;
			this.cbCropSGBFrame.Text = "Crop SGB Frame";
			this.cbCropSGBFrame.UseVisualStyleBackColor = true;
			// 
			// cbUseSGB2
			// 
			this.cbUseSGB2.AutoSize = true;
			this.cbUseSGB2.Location = new System.Drawing.Point(16, 105);
			this.cbUseSGB2.Name = "cbUseSGB2";
			this.cbUseSGB2.Size = new System.Drawing.Size(76, 17);
			this.cbUseSGB2.TabIndex = 4;
			this.cbUseSGB2.Text = "Use SGB2";
			this.cbUseSGB2.UseVisualStyleBackColor = true;
			// 
			// cbFastDSP
			// 
			this.cbFastDSP.AutoSize = true;
			this.cbFastDSP.Location = new System.Drawing.Point(161, 105);
			this.cbFastDSP.Name = "cbFastDSP";
			this.cbFastDSP.Size = new System.Drawing.Size(101, 17);
			this.cbFastDSP.TabIndex = 5;
			this.cbFastDSP.Text = "DSP Fast Mode";
			this.cbFastDSP.UseVisualStyleBackColor = true;
			// 
			// cbFastCoprocessor
			// 
			this.cbFastCoprocessor.AutoSize = true;
			this.cbFastCoprocessor.Location = new System.Drawing.Point(161, 153);
			this.cbFastCoprocessor.Name = "cbFastCoprocessor";
			this.cbFastCoprocessor.Size = new System.Drawing.Size(138, 17);
			this.cbFastCoprocessor.TabIndex = 9;
			this.cbFastCoprocessor.Text = "Coprocessor Fast Mode";
			this.cbFastCoprocessor.UseVisualStyleBackColor = true;
			// 
			// cbNoPPUSpriteLimit
			// 
			this.cbNoPPUSpriteLimit.AutoSize = true;
			this.cbNoPPUSpriteLimit.Location = new System.Drawing.Point(16, 153);
			this.cbNoPPUSpriteLimit.Name = "cbNoPPUSpriteLimit";
			this.cbNoPPUSpriteLimit.Size = new System.Drawing.Size(113, 17);
			this.cbNoPPUSpriteLimit.TabIndex = 8;
			this.cbNoPPUSpriteLimit.Text = "No PPU sprite limit";
			this.cbNoPPUSpriteLimit.UseVisualStyleBackColor = true;
			// 
			// AspectRatioCorrectionBox
			// 
			this.AspectRatioCorrectionBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.AspectRatioCorrectionBox.FormattingEnabled = true;
			this.AspectRatioCorrectionBox.Location = new System.Drawing.Point(161, 249);
			this.AspectRatioCorrectionBox.Name = "AspectRatioCorrectionBox";
			this.AspectRatioCorrectionBox.Size = new System.Drawing.Size(128, 21);
			this.AspectRatioCorrectionBox.TabIndex = 11;
			// 
			// cbShowOverscan
			// 
			this.cbShowOverscan.AutoSize = true;
			this.cbShowOverscan.Location = new System.Drawing.Point(16, 129);
			this.cbShowOverscan.Name = "cbShowOverscan";
			this.cbShowOverscan.Size = new System.Drawing.Size(102, 17);
			this.cbShowOverscan.TabIndex = 6;
			this.cbShowOverscan.Text = "Show Overscan";
			this.cbShowOverscan.UseVisualStyleBackColor = true;
			// 
			// cbShowCursor
			// 
			this.cbShowCursor.AutoSize = true;
			this.cbShowCursor.Location = new System.Drawing.Point(16, 176);
			this.cbShowCursor.Name = "cbShowCursor";
			this.cbShowCursor.Size = new System.Drawing.Size(130, 17);
			this.cbShowCursor.TabIndex = 27;
			this.cbShowCursor.Text = "Show Lightgun Cursor";
			this.cbShowCursor.UseVisualStyleBackColor = true;
			// 
			// SatellaviewCartridgeBox
			// 
			this.SatellaviewCartridgeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.SatellaviewCartridgeBox.FormattingEnabled = true;
			this.SatellaviewCartridgeBox.Location = new System.Drawing.Point(16, 289);
			this.SatellaviewCartridgeBox.Name = "SatellaviewCartridgeBox";
			this.SatellaviewCartridgeBox.Size = new System.Drawing.Size(128, 21);
			this.SatellaviewCartridgeBox.TabIndex = 12;
			// 
			// lblSatellaviewCartridge
			// 
			this.lblSatellaviewCartridge.Location = new System.Drawing.Point(16, 273);
			this.lblSatellaviewCartridge.Name = "lblSatellaviewCartridge";
			this.lblSatellaviewCartridge.Text = "Satellaview cartridge";
			// 
			// lblAspectRatioCorrection
			// 
			this.lblAspectRatioCorrection.Location = new System.Drawing.Point(159, 233);
			this.lblAspectRatioCorrection.Name = "lblAspectRatioCorrection";
			this.lblAspectRatioCorrection.Text = "Aspect Ratio Correction";
			// 
			// lblRegion
			// 
			this.lblRegion.Location = new System.Drawing.Point(159, 273);
			this.lblRegion.Name = "lblRegion";
			this.lblRegion.Text = "Region";
			// 
			// lblEntropy
			// 
			this.lblEntropy.Location = new System.Drawing.Point(13, 233);
			this.lblEntropy.Name = "lblEntropy";
			this.lblEntropy.Text = "Entropy";
			// 
			// lblDoubleSize
			// 
			this.lblDoubleSize.Location = new System.Drawing.Point(31, 33);
			this.lblDoubleSize.MaximumSize = new System.Drawing.Size(260, 0);
			this.lblDoubleSize.Name = "lblDoubleSize";
			this.lblDoubleSize.Text = "Some games are changing the resolution constantly (e.g. SD3) so this option can f" +
	"orce the SNES output to stay double-size always.";
			// 
			// cbUseRealTime
			// 
			this.cbUseRealTime.AutoSize = true;
			this.cbUseRealTime.Location = new System.Drawing.Point(161, 176);
			this.cbUseRealTime.Name = "cbUseRealTime";
			this.cbUseRealTime.Size = new System.Drawing.Size(96, 17);
			this.cbUseRealTime.TabIndex = 27;
			this.cbUseRealTime.Text = "Use Real Time";
			this.cbUseRealTime.UseVisualStyleBackColor = true;
			// 
			// dtpInitialTime
			// 
			this.dtpInitialTime.CustomFormat = "yyyy-MM-dd HH:mm:ss";
			this.dtpInitialTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.dtpInitialTime.Location = new System.Drawing.Point(79, 203);
			this.dtpInitialTime.MinDate = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
			this.dtpInitialTime.Name = "dtpInitialTime";
			this.dtpInitialTime.Size = new System.Drawing.Size(206, 20);
			this.dtpInitialTime.TabIndex = 27;
			// 
			// lblInitialTime
			// 
			this.lblInitialTime.Location = new System.Drawing.Point(15, 202);
			this.lblInitialTime.MaximumSize = new System.Drawing.Size(260, 0);
			this.lblInitialTime.Name = "lblInitialTime";
			this.lblInitialTime.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
			this.lblInitialTime.Text = "Initial Time";
			// 
			// BSNESOptions
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(304, 470);
			this.Controls.Add(this.lblInitialTime);
			this.Controls.Add(this.SatellaviewCartridgeBox);
			this.Controls.Add(this.lblSatellaviewCartridge);
			this.Controls.Add(this.cbShowCursor);
			this.Controls.Add(this.cbShowOverscan);
			this.Controls.Add(this.AspectRatioCorrectionBox);
			this.Controls.Add(this.lblAspectRatioCorrection);
			this.Controls.Add(this.cbNoPPUSpriteLimit);
			this.Controls.Add(this.cbFastCoprocessor);
			this.Controls.Add(this.cbFastDSP);
			this.Controls.Add(this.cbUseSGB2);
			this.Controls.Add(this.cbCropSGBFrame);
			this.Controls.Add(this.cbFastPPU);
			this.Controls.Add(this.cbGameHotfixes);
			this.Controls.Add(this.lblRegion);
			this.Controls.Add(this.lblEntropy);
			this.Controls.Add(this.EntropyBox);
			this.Controls.Add(this.RegionBox);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.lblDoubleSize);
			this.Controls.Add(this.cbDoubleSize);
			this.Controls.Add(this.cbUseRealTime);
			this.Controls.Add(this.dtpInitialTime);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "BSNESOptions";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "BSNES Options";
			this.Load += new System.EventHandler(this.OnLoad);
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
		private System.Windows.Forms.ComboBox RegionBox;
		private WinForms.Controls.LocLabelEx lblRegion;
		private System.Windows.Forms.CheckBox cbGameHotfixes;
		private System.Windows.Forms.CheckBox cbFastPPU;
		private System.Windows.Forms.CheckBox Bg1_0Checkbox;
		private System.Windows.Forms.CheckBox Bg4_0Checkbox;
		private System.Windows.Forms.CheckBox Bg3_0Checkbox;
		private System.Windows.Forms.CheckBox Bg2_0Checkbox;
		private WinForms.Controls.LocLabelEx lblPriority1;
		private WinForms.Controls.LocLabelEx lblPriority0;
		private System.Windows.Forms.CheckBox cbCropSGBFrame;
		private System.Windows.Forms.CheckBox cbUseSGB2;
		private System.Windows.Forms.CheckBox cbFastDSP;
		private System.Windows.Forms.CheckBox cbFastCoprocessor;
		private System.Windows.Forms.CheckBox cbNoPPUSpriteLimit;
		private System.Windows.Forms.ComboBox AspectRatioCorrectionBox;
		private WinForms.Controls.LocLabelEx lblAspectRatioCorrection;
		private System.Windows.Forms.CheckBox cbShowOverscan;
		private System.Windows.Forms.CheckBox cbShowCursor;
		private WinForms.Controls.LocLabelEx lblSatellaviewCartridge;
		private System.Windows.Forms.ComboBox SatellaviewCartridgeBox;
		private System.Windows.Forms.CheckBox cbUseRealTime;
		private System.Windows.Forms.DateTimePicker dtpInitialTime;
		private WinForms.Controls.LocLabelEx lblInitialTime;
	}
}
