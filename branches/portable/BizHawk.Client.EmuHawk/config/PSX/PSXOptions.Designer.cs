namespace BizHawk.Client.EmuHawk
{
	partial class PSXOptions
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PSXOptions));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lblTweakedMednafen = new System.Windows.Forms.Label();
			this.rbTweakedMednafenMode = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.rbDebugMode = new System.Windows.Forms.RadioButton();
			this.btnNiceDisplayConfig = new System.Windows.Forms.Button();
			this.lblMednafen = new System.Windows.Forms.Label();
			this.rbMednafenMode = new System.Windows.Forms.RadioButton();
			this.lblPixelPro = new System.Windows.Forms.Label();
			this.rbPixelPro = new System.Windows.Forms.RadioButton();
			this.label7 = new System.Windows.Forms.Label();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lblPAL = new System.Windows.Forms.Label();
			this.PAL_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.PAL_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.lblNTSC = new System.Windows.Forms.Label();
			this.btnAreaFull = new System.Windows.Forms.Button();
			this.checkClipHorizontal = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.NTSC_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.NTSC_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(622, 240);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(541, 240);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 2;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.lblTweakedMednafen);
			this.groupBox1.Controls.Add(this.rbTweakedMednafenMode);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.rbDebugMode);
			this.groupBox1.Controls.Add(this.btnNiceDisplayConfig);
			this.groupBox1.Controls.Add(this.lblMednafen);
			this.groupBox1.Controls.Add(this.rbMednafenMode);
			this.groupBox1.Controls.Add(this.lblPixelPro);
			this.groupBox1.Controls.Add(this.rbPixelPro);
			this.groupBox1.Location = new System.Drawing.Point(12, 7);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(474, 256);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Resolution Management";
			// 
			// lblTweakedMednafen
			// 
			this.lblTweakedMednafen.Location = new System.Drawing.Point(255, 132);
			this.lblTweakedMednafen.Name = "lblTweakedMednafen";
			this.lblTweakedMednafen.Size = new System.Drawing.Size(213, 79);
			this.lblTweakedMednafen.TabIndex = 28;
			this.lblTweakedMednafen.Text = "Displays all content at as multiple of 400x300.\r\n • Correct aspect ratio\r\n • Gene" +
    "rally enjoyable game presentation\r\n • Detail loss at 1x in fewer cases\r\n • Requi" +
    "res certain display configuration:\r\n";
			// 
			// rbTweakedMednafenMode
			// 
			this.rbTweakedMednafenMode.AutoSize = true;
			this.rbTweakedMednafenMode.Location = new System.Drawing.Point(246, 116);
			this.rbTweakedMednafenMode.Name = "rbTweakedMednafenMode";
			this.rbTweakedMednafenMode.Size = new System.Drawing.Size(193, 17);
			this.rbTweakedMednafenMode.TabIndex = 27;
			this.rbTweakedMednafenMode.TabStop = true;
			this.rbTweakedMednafenMode.Text = "Tweaked Mednafen Mode (4:3 AR)";
			this.rbTweakedMednafenMode.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(246, 39);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(213, 63);
			this.label3.TabIndex = 26;
			this.label3.Text = "Displays all content unmodified\r\n • Window size will constantly change\r\n • Aspect" +
    " ratio is usually wrong\r\n • Ideal for segmented AV dumping";
			// 
			// rbDebugMode
			// 
			this.rbDebugMode.AutoSize = true;
			this.rbDebugMode.Location = new System.Drawing.Point(246, 19);
			this.rbDebugMode.Name = "rbDebugMode";
			this.rbDebugMode.Size = new System.Drawing.Size(134, 17);
			this.rbDebugMode.TabIndex = 25;
			this.rbDebugMode.TabStop = true;
			this.rbDebugMode.Text = "Hardcore Debug Mode";
			this.rbDebugMode.UseVisualStyleBackColor = true;
			// 
			// btnNiceDisplayConfig
			// 
			this.btnNiceDisplayConfig.AutoSize = true;
			this.btnNiceDisplayConfig.Location = new System.Drawing.Point(140, 221);
			this.btnNiceDisplayConfig.Name = "btnNiceDisplayConfig";
			this.btnNiceDisplayConfig.Size = new System.Drawing.Size(173, 23);
			this.btnNiceDisplayConfig.TabIndex = 24;
			this.btnNiceDisplayConfig.Text = "Change My Display Options";
			this.btnNiceDisplayConfig.UseVisualStyleBackColor = true;
			this.btnNiceDisplayConfig.Click += new System.EventHandler(this.btnNiceDisplayConfig_Click);
			// 
			// lblMednafen
			// 
			this.lblMednafen.Location = new System.Drawing.Point(6, 132);
			this.lblMednafen.Name = "lblMednafen";
			this.lblMednafen.Size = new System.Drawing.Size(213, 82);
			this.lblMednafen.TabIndex = 23;
			this.lblMednafen.Text = resources.GetString("lblMednafen.Text");
			// 
			// rbMednafenMode
			// 
			this.rbMednafenMode.AutoSize = true;
			this.rbMednafenMode.Location = new System.Drawing.Point(6, 116);
			this.rbMednafenMode.Name = "rbMednafenMode";
			this.rbMednafenMode.Size = new System.Drawing.Size(145, 17);
			this.rbMednafenMode.TabIndex = 22;
			this.rbMednafenMode.TabStop = true;
			this.rbMednafenMode.Text = "Mednafen Mode (4:3 AR)";
			this.rbMednafenMode.UseVisualStyleBackColor = true;
			// 
			// lblPixelPro
			// 
			this.lblPixelPro.Location = new System.Drawing.Point(6, 35);
			this.lblPixelPro.Name = "lblPixelPro";
			this.lblPixelPro.Size = new System.Drawing.Size(252, 78);
			this.lblPixelPro.TabIndex = 21;
			this.lblPixelPro.Text = "Converts content with nearest neighbor to \r\nfit gracefully in a 800x480 window.\r\n" +
    " • Content is pixel perfect\r\n • Aspect ratio is usually wrong\r\n • Game may seen " +
    "to have scale varying by mode\r\n\r\n\r\n";
			// 
			// rbPixelPro
			// 
			this.rbPixelPro.AutoSize = true;
			this.rbPixelPro.Location = new System.Drawing.Point(6, 19);
			this.rbPixelPro.Name = "rbPixelPro";
			this.rbPixelPro.Size = new System.Drawing.Size(96, 17);
			this.rbPixelPro.TabIndex = 0;
			this.rbPixelPro.TabStop = true;
			this.rbPixelPro.Text = "Pixel Pro Mode";
			this.rbPixelPro.UseVisualStyleBackColor = true;
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(500, 192);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(197, 29);
			this.label7.TabIndex = 30;
			this.label7.Text = "Restart the core to take effect.\r\nSorry, its still in development";
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.lblPAL);
			this.groupBox2.Controls.Add(this.PAL_LastLineNumeric);
			this.groupBox2.Controls.Add(this.PAL_FirstLineNumeric);
			this.groupBox2.Controls.Add(this.lblNTSC);
			this.groupBox2.Controls.Add(this.btnAreaFull);
			this.groupBox2.Controls.Add(this.checkClipHorizontal);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.NTSC_LastLineNumeric);
			this.groupBox2.Controls.Add(this.NTSC_FirstLineNumeric);
			this.groupBox2.Location = new System.Drawing.Point(492, 7);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(212, 160);
			this.groupBox2.TabIndex = 31;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Drawing Area";
			// 
			// lblPAL
			// 
			this.lblPAL.AutoSize = true;
			this.lblPAL.Location = new System.Drawing.Point(131, 22);
			this.lblPAL.Name = "lblPAL";
			this.lblPAL.Size = new System.Drawing.Size(27, 13);
			this.lblPAL.TabIndex = 44;
			this.lblPAL.Text = "PAL";
			// 
			// PAL_LastLineNumeric
			// 
			this.PAL_LastLineNumeric.Location = new System.Drawing.Point(124, 67);
			this.PAL_LastLineNumeric.Maximum = new decimal(new int[] {
            287,
            0,
            0,
            0});
			this.PAL_LastLineNumeric.Name = "PAL_LastLineNumeric";
			this.PAL_LastLineNumeric.Size = new System.Drawing.Size(47, 20);
			this.PAL_LastLineNumeric.TabIndex = 43;
			this.PAL_LastLineNumeric.Value = new decimal(new int[] {
            128,
            0,
            0,
            0});
			this.PAL_LastLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
			// 
			// PAL_FirstLineNumeric
			// 
			this.PAL_FirstLineNumeric.Location = new System.Drawing.Point(124, 41);
			this.PAL_FirstLineNumeric.Maximum = new decimal(new int[] {
            287,
            0,
            0,
            0});
			this.PAL_FirstLineNumeric.Name = "PAL_FirstLineNumeric";
			this.PAL_FirstLineNumeric.Size = new System.Drawing.Size(47, 20);
			this.PAL_FirstLineNumeric.TabIndex = 42;
			this.PAL_FirstLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
			// 
			// lblNTSC
			// 
			this.lblNTSC.AutoSize = true;
			this.lblNTSC.Location = new System.Drawing.Point(62, 22);
			this.lblNTSC.Name = "lblNTSC";
			this.lblNTSC.Size = new System.Drawing.Size(36, 13);
			this.lblNTSC.TabIndex = 41;
			this.lblNTSC.Text = "NTSC";
			// 
			// btnAreaFull
			// 
			this.btnAreaFull.Location = new System.Drawing.Point(6, 98);
			this.btnAreaFull.Name = "btnAreaFull";
			this.btnAreaFull.Size = new System.Drawing.Size(136, 23);
			this.btnAreaFull.TabIndex = 40;
			this.btnAreaFull.Text = "Full [0,239] and [0,287]";
			this.btnAreaFull.UseVisualStyleBackColor = true;
			this.btnAreaFull.Click += new System.EventHandler(this.btnAreaFull_Click);
			// 
			// checkClipHorizontal
			// 
			this.checkClipHorizontal.AutoSize = true;
			this.checkClipHorizontal.Location = new System.Drawing.Point(7, 127);
			this.checkClipHorizontal.Name = "checkClipHorizontal";
			this.checkClipHorizontal.Size = new System.Drawing.Size(142, 17);
			this.checkClipHorizontal.TabIndex = 30;
			this.checkClipHorizontal.Text = "Clip Horizontal Overscan";
			this.checkClipHorizontal.UseVisualStyleBackColor = true;
			this.checkClipHorizontal.CheckedChanged += new System.EventHandler(this.checkClipHorizontal_CheckedChanged);
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
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(5, 43);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 23;
			this.label1.Text = "First line:";
			// 
			// NTSC_LastLineNumeric
			// 
			this.NTSC_LastLineNumeric.Location = new System.Drawing.Point(59, 67);
			this.NTSC_LastLineNumeric.Maximum = new decimal(new int[] {
            239,
            0,
            0,
            0});
			this.NTSC_LastLineNumeric.Name = "NTSC_LastLineNumeric";
			this.NTSC_LastLineNumeric.Size = new System.Drawing.Size(47, 20);
			this.NTSC_LastLineNumeric.TabIndex = 28;
			this.NTSC_LastLineNumeric.Value = new decimal(new int[] {
            239,
            0,
            0,
            0});
			this.NTSC_LastLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
			// 
			// NTSC_FirstLineNumeric
			// 
			this.NTSC_FirstLineNumeric.Location = new System.Drawing.Point(59, 41);
			this.NTSC_FirstLineNumeric.Maximum = new decimal(new int[] {
            239,
            0,
            0,
            0});
			this.NTSC_FirstLineNumeric.Name = "NTSC_FirstLineNumeric";
			this.NTSC_FirstLineNumeric.Size = new System.Drawing.Size(47, 20);
			this.NTSC_FirstLineNumeric.TabIndex = 21;
			this.NTSC_FirstLineNumeric.ValueChanged += new System.EventHandler(this.DrawingArea_ValueChanged);
			// 
			// PSXOptions
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(713, 275);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PSXOptions";
			this.Text = "PSX Options";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbPixelPro;
		private System.Windows.Forms.Button btnNiceDisplayConfig;
		private System.Windows.Forms.Label lblMednafen;
		private System.Windows.Forms.RadioButton rbMednafenMode;
		private System.Windows.Forms.Label lblPixelPro;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.RadioButton rbDebugMode;
		private System.Windows.Forms.Label lblTweakedMednafen;
		private System.Windows.Forms.RadioButton rbTweakedMednafenMode;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label lblPAL;
		private System.Windows.Forms.NumericUpDown PAL_LastLineNumeric;
		private System.Windows.Forms.NumericUpDown PAL_FirstLineNumeric;
		private System.Windows.Forms.Label lblNTSC;
		private System.Windows.Forms.Button btnAreaFull;
		private System.Windows.Forms.CheckBox checkClipHorizontal;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown NTSC_LastLineNumeric;
		private System.Windows.Forms.NumericUpDown NTSC_FirstLineNumeric;
	}
}