namespace BizHawk.Client.EmuHawk
{
	partial class NESGraphicsConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NESGraphicsConfig));
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.AllowMoreSprites = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.pictureBoxPalette = new System.Windows.Forms.PictureBox();
            this.AutoLoadPalette = new System.Windows.Forms.CheckBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PalettePath = new System.Windows.Forms.TextBox();
            this.BrowsePalette = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PAL_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.PAL_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnAreaFull = new System.Windows.Forms.Button();
            this.btnAreaStandard = new System.Windows.Forms.Button();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.NTSC_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.NTSC_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
            this.ClipLeftAndRightCheckBox = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.checkUseBackdropColor = new System.Windows.Forms.CheckBox();
            this.ChangeBGColor = new System.Windows.Forms.Button();
            this.BackGroundColorNumber = new System.Windows.Forms.TextBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.BackgroundColorPanel = new System.Windows.Forms.Panel();
            this.DispBackground = new System.Windows.Forms.CheckBox();
            this.DispSprites = new System.Windows.Forms.CheckBox();
            this.BGColorDialog = new System.Windows.Forms.ColorDialog();
            this.RestoreDefaultsButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPalette)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.SuspendLayout();
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            // 
            // AllowMoreSprites
            // 
            resources.ApplyResources(this.AllowMoreSprites, "AllowMoreSprites");
            this.AllowMoreSprites.Name = "AllowMoreSprites";
            this.AllowMoreSprites.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.pictureBoxPalette);
            this.groupBox1.Controls.Add(this.AutoLoadPalette);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.PalettePath);
            this.groupBox1.Controls.Add(this.BrowsePalette);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // pictureBoxPalette
            // 
            resources.ApplyResources(this.pictureBoxPalette, "pictureBoxPalette");
            this.pictureBoxPalette.Name = "pictureBoxPalette";
            this.pictureBoxPalette.TabStop = false;
            // 
            // AutoLoadPalette
            // 
            resources.ApplyResources(this.AutoLoadPalette, "AutoLoadPalette");
            this.AutoLoadPalette.Name = "AutoLoadPalette";
            this.AutoLoadPalette.UseVisualStyleBackColor = true;
            this.AutoLoadPalette.Click += new System.EventHandler(this.AutoLoadPalette_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // PalettePath
            // 
            resources.ApplyResources(this.PalettePath, "PalettePath");
            this.PalettePath.Name = "PalettePath";
            // 
            // BrowsePalette
            // 
            resources.ApplyResources(this.BrowsePalette, "BrowsePalette");
            this.BrowsePalette.Name = "BrowsePalette";
            this.BrowsePalette.UseVisualStyleBackColor = true;
            this.BrowsePalette.Click += new System.EventHandler(this.BrowsePalette_Click);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.PAL_LastLineNumeric);
            this.groupBox2.Controls.Add(this.PAL_FirstLineNumeric);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.btnAreaFull);
            this.groupBox2.Controls.Add(this.btnAreaStandard);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.NTSC_LastLineNumeric);
            this.groupBox2.Controls.Add(this.NTSC_FirstLineNumeric);
            this.groupBox2.Controls.Add(this.ClipLeftAndRightCheckBox);
            this.groupBox2.Controls.Add(this.AllowMoreSprites);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // PAL_LastLineNumeric
            // 
            resources.ApplyResources(this.PAL_LastLineNumeric, "PAL_LastLineNumeric");
            this.PAL_LastLineNumeric.Maximum = new decimal(new int[] {
            239,
            0,
            0,
            0});
            this.PAL_LastLineNumeric.Minimum = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.PAL_LastLineNumeric.Name = "PAL_LastLineNumeric";
            this.PAL_LastLineNumeric.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            // 
            // PAL_FirstLineNumeric
            // 
            resources.ApplyResources(this.PAL_FirstLineNumeric, "PAL_FirstLineNumeric");
            this.PAL_FirstLineNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.PAL_FirstLineNumeric.Name = "PAL_FirstLineNumeric";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // btnAreaFull
            // 
            resources.ApplyResources(this.btnAreaFull, "btnAreaFull");
            this.btnAreaFull.Name = "btnAreaFull";
            this.btnAreaFull.UseVisualStyleBackColor = true;
            this.btnAreaFull.Click += new System.EventHandler(this.BtnAreaFull_Click);
            // 
            // btnAreaStandard
            // 
            resources.ApplyResources(this.btnAreaStandard, "btnAreaStandard");
            this.btnAreaStandard.Name = "btnAreaStandard";
            this.btnAreaStandard.UseVisualStyleBackColor = true;
            this.btnAreaStandard.Click += new System.EventHandler(this.BtnAreaStandard_Click);
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // NTSC_LastLineNumeric
            // 
            resources.ApplyResources(this.NTSC_LastLineNumeric, "NTSC_LastLineNumeric");
            this.NTSC_LastLineNumeric.Maximum = new decimal(new int[] {
            239,
            0,
            0,
            0});
            this.NTSC_LastLineNumeric.Minimum = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.NTSC_LastLineNumeric.Name = "NTSC_LastLineNumeric";
            this.NTSC_LastLineNumeric.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
            // 
            // NTSC_FirstLineNumeric
            // 
            resources.ApplyResources(this.NTSC_FirstLineNumeric, "NTSC_FirstLineNumeric");
            this.NTSC_FirstLineNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.NTSC_FirstLineNumeric.Name = "NTSC_FirstLineNumeric";
            // 
            // ClipLeftAndRightCheckBox
            // 
            resources.ApplyResources(this.ClipLeftAndRightCheckBox, "ClipLeftAndRightCheckBox");
            this.ClipLeftAndRightCheckBox.Name = "ClipLeftAndRightCheckBox";
            this.ClipLeftAndRightCheckBox.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.checkUseBackdropColor);
            this.groupBox3.Controls.Add(this.ChangeBGColor);
            this.groupBox3.Controls.Add(this.BackGroundColorNumber);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.groupBox4);
            this.groupBox3.Controls.Add(this.DispBackground);
            this.groupBox3.Controls.Add(this.DispSprites);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // checkUseBackdropColor
            // 
            resources.ApplyResources(this.checkUseBackdropColor, "checkUseBackdropColor");
            this.checkUseBackdropColor.Checked = true;
            this.checkUseBackdropColor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkUseBackdropColor.Name = "checkUseBackdropColor";
            this.checkUseBackdropColor.UseVisualStyleBackColor = true;
            // 
            // ChangeBGColor
            // 
            resources.ApplyResources(this.ChangeBGColor, "ChangeBGColor");
            this.ChangeBGColor.Name = "ChangeBGColor";
            this.ChangeBGColor.UseVisualStyleBackColor = true;
            this.ChangeBGColor.Click += new System.EventHandler(this.ChangeBGColor_Click);
            // 
            // BackGroundColorNumber
            // 
            resources.ApplyResources(this.BackGroundColorNumber, "BackGroundColorNumber");
            this.BackGroundColorNumber.Name = "BackGroundColorNumber";
            this.BackGroundColorNumber.ReadOnly = true;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.BackgroundColorPanel);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // BackgroundColorPanel
            // 
            resources.ApplyResources(this.BackgroundColorPanel, "BackgroundColorPanel");
            this.BackgroundColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.BackgroundColorPanel.Name = "BackgroundColorPanel";
            this.BackgroundColorPanel.DoubleClick += new System.EventHandler(this.BackgroundColorPanel_DoubleClick);
            // 
            // DispBackground
            // 
            resources.ApplyResources(this.DispBackground, "DispBackground");
            this.DispBackground.Checked = true;
            this.DispBackground.CheckState = System.Windows.Forms.CheckState.Checked;
            this.DispBackground.Name = "DispBackground";
            this.DispBackground.UseVisualStyleBackColor = true;
            // 
            // DispSprites
            // 
            resources.ApplyResources(this.DispSprites, "DispSprites");
            this.DispSprites.Checked = true;
            this.DispSprites.CheckState = System.Windows.Forms.CheckState.Checked;
            this.DispSprites.Name = "DispSprites";
            this.DispSprites.UseVisualStyleBackColor = true;
            // 
            // RestoreDefaultsButton
            // 
            resources.ApplyResources(this.RestoreDefaultsButton, "RestoreDefaultsButton");
            this.RestoreDefaultsButton.Name = "RestoreDefaultsButton";
            this.RestoreDefaultsButton.UseVisualStyleBackColor = true;
            this.RestoreDefaultsButton.Click += new System.EventHandler(this.RestoreDefaultsButton_Click);
            // 
            // NESGraphicsConfig
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.RestoreDefaultsButton);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NESGraphicsConfig";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Load += new System.EventHandler(this.NESGraphicsConfig_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPalette)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.CheckBox AllowMoreSprites;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TextBox PalettePath;
		private System.Windows.Forms.Button BrowsePalette;
		private System.Windows.Forms.CheckBox AutoLoadPalette;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox ClipLeftAndRightCheckBox;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.CheckBox DispSprites;
		private System.Windows.Forms.CheckBox DispBackground;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Panel BackgroundColorPanel;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.TextBox BackGroundColorNumber;
		private System.Windows.Forms.Button ChangeBGColor;
		private System.Windows.Forms.ColorDialog BGColorDialog;
		private System.Windows.Forms.CheckBox checkUseBackdropColor;
		private System.Windows.Forms.NumericUpDown NTSC_FirstLineNumeric;
		private System.Windows.Forms.NumericUpDown NTSC_LastLineNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.Button btnAreaFull;
		private System.Windows.Forms.Button btnAreaStandard;
		private System.Windows.Forms.Button RestoreDefaultsButton;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.NumericUpDown PAL_LastLineNumeric;
		private System.Windows.Forms.NumericUpDown PAL_FirstLineNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.PictureBox pictureBoxPalette;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
	}
}