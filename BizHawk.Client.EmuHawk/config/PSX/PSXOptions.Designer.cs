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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PSXOptions));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.lblTweakedMednafen = new System.Windows.Forms.Label();
			this.rbTweakedMednafenMode = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.rbDebugMode = new System.Windows.Forms.RadioButton();
			this.btnNiceDisplayConfig = new System.Windows.Forms.Button();
			this.lblMednafen = new System.Windows.Forms.Label();
			this.rbMednafenMode = new System.Windows.Forms.RadioButton();
			this.lblPixelPro = new System.Windows.Forms.Label();
			this.rbPixelPro = new System.Windows.Forms.RadioButton();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.rbClipNone = new System.Windows.Forms.RadioButton();
			this.rbClipToFramebuffer = new System.Windows.Forms.RadioButton();
			this.rbClipBasic = new System.Windows.Forms.RadioButton();
			this.lblPAL = new System.Windows.Forms.Label();
			this.PAL_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.PAL_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.lblNTSC = new System.Windows.Forms.Label();
			this.btnAreaFull = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.NTSC_LastLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.NTSC_FirstLineNumeric = new System.Windows.Forms.NumericUpDown();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.rbWeave = new System.Windows.Forms.RadioButton();
			this.rbBobOffset = new System.Windows.Forms.RadioButton();
			this.rbBob = new System.Windows.Forms.RadioButton();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.cbLEC = new System.Windows.Forms.CheckBox();
			this.cbGpuLag = new System.Windows.Forms.CheckBox();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.groupBox6.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(622, 370);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(541, 370);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 2;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.linkLabel1);
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
			this.groupBox1.Size = new System.Drawing.Size(474, 293);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Resolution Management";
			// 
			// linkLabel1
			// 
			this.linkLabel1.AutoSize = true;
			this.linkLabel1.Location = new System.Drawing.Point(326, 254);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(53, 13);
			this.linkLabel1.TabIndex = 29;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "About Me";
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// lblTweakedMednafen
			// 
			this.lblTweakedMednafen.Location = new System.Drawing.Point(249, 134);
			this.lblTweakedMednafen.Name = "lblTweakedMednafen";
			this.lblTweakedMednafen.Size = new System.Drawing.Size(213, 93);
			this.lblTweakedMednafen.TabIndex = 28;
			this.lblTweakedMednafen.Text = resources.GetString("lblTweakedMednafen.Text");
			// 
			// rbTweakedMednafenMode
			// 
			this.rbTweakedMednafenMode.AutoSize = true;
			this.rbTweakedMednafenMode.Location = new System.Drawing.Point(246, 118);
			this.rbTweakedMednafenMode.Name = "rbTweakedMednafenMode";
			this.rbTweakedMednafenMode.Size = new System.Drawing.Size(193, 17);
			this.rbTweakedMednafenMode.TabIndex = 27;
			this.rbTweakedMednafenMode.TabStop = true;
			this.rbTweakedMednafenMode.Text = "Tweaked Mednafen Mode (4:3 AR)";
			this.rbTweakedMednafenMode.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(249, 35);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(213, 82);
			this.label3.TabIndex = 26;
			this.label3.Text = "Displays all content unmodified\r\n • Window size will constantly change\r\n • Aspect" +
    " ratio is usually wrong\r\n • Recommended for hacking\r\n • Ideal for segmented AV d" +
    "umping\r\n • Ideal for screen shots\r\n\r\n";
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
			this.btnNiceDisplayConfig.Location = new System.Drawing.Point(145, 244);
			this.btnNiceDisplayConfig.Name = "btnNiceDisplayConfig";
			this.btnNiceDisplayConfig.Size = new System.Drawing.Size(173, 23);
			this.btnNiceDisplayConfig.TabIndex = 24;
			this.btnNiceDisplayConfig.Text = "Change My Display Options";
			this.btnNiceDisplayConfig.UseVisualStyleBackColor = true;
			this.btnNiceDisplayConfig.Click += new System.EventHandler(this.btnNiceDisplayConfig_Click);
			// 
			// lblMednafen
			// 
			this.lblMednafen.Location = new System.Drawing.Point(6, 134);
			this.lblMednafen.Name = "lblMednafen";
			this.lblMednafen.Size = new System.Drawing.Size(213, 93);
			this.lblMednafen.TabIndex = 23;
			this.lblMednafen.Text = resources.GetString("lblMednafen.Text");
			// 
			// rbMednafenMode
			// 
			this.rbMednafenMode.AutoSize = true;
			this.rbMednafenMode.Location = new System.Drawing.Point(6, 118);
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
			this.lblPixelPro.Text = resources.GetString("lblPixelPro.Text");
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
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.groupBox3);
			this.groupBox2.Controls.Add(this.lblPAL);
			this.groupBox2.Controls.Add(this.PAL_LastLineNumeric);
			this.groupBox2.Controls.Add(this.PAL_FirstLineNumeric);
			this.groupBox2.Controls.Add(this.lblNTSC);
			this.groupBox2.Controls.Add(this.btnAreaFull);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.NTSC_LastLineNumeric);
			this.groupBox2.Controls.Add(this.NTSC_FirstLineNumeric);
			this.groupBox2.Location = new System.Drawing.Point(492, 7);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(212, 239);
			this.groupBox2.TabIndex = 31;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Drawing Area";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.rbClipNone);
			this.groupBox3.Controls.Add(this.rbClipToFramebuffer);
			this.groupBox3.Controls.Add(this.rbClipBasic);
			this.groupBox3.Location = new System.Drawing.Point(7, 131);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(197, 88);
			this.groupBox3.TabIndex = 46;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Horizontal Overscan Clipping";
			// 
			// rbClipNone
			// 
			this.rbClipNone.AutoSize = true;
			this.rbClipNone.Location = new System.Drawing.Point(6, 19);
			this.rbClipNone.Name = "rbClipNone";
			this.rbClipNone.Size = new System.Drawing.Size(51, 17);
			this.rbClipNone.TabIndex = 48;
			this.rbClipNone.TabStop = true;
			this.rbClipNone.Text = "None";
			this.toolTip1.SetToolTip(this.rbClipNone, resources.GetString("rbClipNone.ToolTip"));
			this.rbClipNone.UseVisualStyleBackColor = true;
			this.rbClipNone.CheckedChanged += new System.EventHandler(this.rbClipNone_CheckedChanged);
			// 
			// rbClipToFramebuffer
			// 
			this.rbClipToFramebuffer.AutoSize = true;
			this.rbClipToFramebuffer.Location = new System.Drawing.Point(6, 65);
			this.rbClipToFramebuffer.Name = "rbClipToFramebuffer";
			this.rbClipToFramebuffer.Size = new System.Drawing.Size(117, 17);
			this.rbClipToFramebuffer.TabIndex = 47;
			this.rbClipToFramebuffer.TabStop = true;
			this.rbClipToFramebuffer.Text = "Clip To Framebuffer";
			this.toolTip1.SetToolTip(this.rbClipToFramebuffer, "Subverts mednafen\'s internal video display field emulation to show only the game\'" +
        "s framebuffer.\r\nHorizontal letterbox bars may be re-added in Mednafen-style reso" +
        "lution modes to maintain correct AR.");
			this.rbClipToFramebuffer.UseVisualStyleBackColor = true;
			this.rbClipToFramebuffer.CheckedChanged += new System.EventHandler(this.rbClipToFramebuffer_CheckedChanged);
			// 
			// rbClipBasic
			// 
			this.rbClipBasic.AutoSize = true;
			this.rbClipBasic.Location = new System.Drawing.Point(6, 42);
			this.rbClipBasic.Name = "rbClipBasic";
			this.rbClipBasic.Size = new System.Drawing.Size(91, 17);
			this.rbClipBasic.TabIndex = 46;
			this.rbClipBasic.TabStop = true;
			this.rbClipBasic.Text = "Basic Clipping";
			this.toolTip1.SetToolTip(this.rbClipBasic, "A mednafen option -- appears to be 5.5% horizontally");
			this.rbClipBasic.UseVisualStyleBackColor = true;
			this.rbClipBasic.CheckedChanged += new System.EventHandler(this.rbClipHorizontal_CheckedChanged);
			// 
			// lblPAL
			// 
			this.lblPAL.AutoSize = true;
			this.lblPAL.Location = new System.Drawing.Point(131, 17);
			this.lblPAL.Name = "lblPAL";
			this.lblPAL.Size = new System.Drawing.Size(27, 13);
			this.lblPAL.TabIndex = 44;
			this.lblPAL.Text = "PAL";
			// 
			// PAL_LastLineNumeric
			// 
			this.PAL_LastLineNumeric.Location = new System.Drawing.Point(124, 62);
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
			this.PAL_FirstLineNumeric.Location = new System.Drawing.Point(124, 36);
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
			this.lblNTSC.Location = new System.Drawing.Point(62, 17);
			this.lblNTSC.Name = "lblNTSC";
			this.lblNTSC.Size = new System.Drawing.Size(36, 13);
			this.lblNTSC.TabIndex = 41;
			this.lblNTSC.Text = "NTSC";
			// 
			// btnAreaFull
			// 
			this.btnAreaFull.Location = new System.Drawing.Point(8, 94);
			this.btnAreaFull.Name = "btnAreaFull";
			this.btnAreaFull.Size = new System.Drawing.Size(163, 23);
			this.btnAreaFull.TabIndex = 40;
			this.btnAreaFull.Text = "Full [0,239] and [0,287]";
			this.btnAreaFull.UseVisualStyleBackColor = true;
			this.btnAreaFull.Click += new System.EventHandler(this.btnAreaFull_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(4, 64);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(49, 13);
			this.label4.TabIndex = 24;
			this.label4.Text = "Last line:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(5, 38);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(48, 13);
			this.label1.TabIndex = 23;
			this.label1.Text = "First line:";
			// 
			// NTSC_LastLineNumeric
			// 
			this.NTSC_LastLineNumeric.Location = new System.Drawing.Point(59, 62);
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
			this.NTSC_FirstLineNumeric.Location = new System.Drawing.Point(59, 36);
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
			// rbWeave
			// 
			this.rbWeave.AutoSize = true;
			this.rbWeave.Location = new System.Drawing.Point(6, 19);
			this.rbWeave.Name = "rbWeave";
			this.rbWeave.Size = new System.Drawing.Size(60, 17);
			this.rbWeave.TabIndex = 48;
			this.rbWeave.TabStop = true;
			this.rbWeave.Text = "Weave";
			this.toolTip1.SetToolTip(this.rbWeave, "Good for low-motion video");
			this.rbWeave.UseVisualStyleBackColor = true;
			// 
			// rbBobOffset
			// 
			this.rbBobOffset.AutoSize = true;
			this.rbBobOffset.Location = new System.Drawing.Point(122, 19);
			this.rbBobOffset.Name = "rbBobOffset";
			this.rbBobOffset.Size = new System.Drawing.Size(75, 17);
			this.rbBobOffset.TabIndex = 47;
			this.rbBobOffset.TabStop = true;
			this.rbBobOffset.Text = "Bob Offset";
			this.toolTip1.SetToolTip(this.rbBobOffset, "Good for high-motion video, but is a bit flickery; reduces the subjective vertica" +
        "l resolution.");
			this.rbBobOffset.UseVisualStyleBackColor = true;
			// 
			// rbBob
			// 
			this.rbBob.AutoSize = true;
			this.rbBob.Location = new System.Drawing.Point(72, 19);
			this.rbBob.Name = "rbBob";
			this.rbBob.Size = new System.Drawing.Size(44, 17);
			this.rbBob.TabIndex = 46;
			this.rbBob.TabStop = true;
			this.rbBob.Text = "Bob";
			this.toolTip1.SetToolTip(this.rbBob, "Good for causing a headache. All glory to Bob.");
			this.rbBob.UseVisualStyleBackColor = true;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.rbWeave);
			this.groupBox4.Controls.Add(this.rbBobOffset);
			this.groupBox4.Controls.Add(this.rbBob);
			this.groupBox4.Location = new System.Drawing.Point(492, 251);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(212, 49);
			this.groupBox4.TabIndex = 50;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Deinterlacing";
			// 
			// groupBox5
			// 
			this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox5.Controls.Add(this.cbLEC);
			this.groupBox5.Location = new System.Drawing.Point(12, 306);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(238, 85);
			this.groupBox5.TabIndex = 47;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Emulation Sync Settings";
			// 
			// cbLEC
			// 
			this.cbLEC.AutoSize = true;
			this.cbLEC.Location = new System.Drawing.Point(9, 19);
			this.cbLEC.Name = "cbLEC";
			this.cbLEC.Size = new System.Drawing.Size(222, 30);
			this.cbLEC.TabIndex = 0;
			this.cbLEC.Text = "Emulate Sector Error Correction\r\n(usually unneeded; breaks some patches)";
			this.cbLEC.UseVisualStyleBackColor = true;
			// 
			// cbGpuLag
			// 
			this.cbGpuLag.AutoSize = true;
			this.cbGpuLag.Location = new System.Drawing.Point(16, 19);
			this.cbGpuLag.Name = "cbGpuLag";
			this.cbGpuLag.Size = new System.Drawing.Size(181, 17);
			this.cbGpuLag.TabIndex = 1;
			this.cbGpuLag.Text = "Determine Lag from GPU Frames";
			this.cbGpuLag.UseVisualStyleBackColor = true;
			// 
			// groupBox6
			// 
			this.groupBox6.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox6.Controls.Add(this.cbGpuLag);
			this.groupBox6.Location = new System.Drawing.Point(264, 308);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(238, 85);
			this.groupBox6.TabIndex = 48;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Emulation User Settings";
			// 
			// PSXOptions
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(713, 405);
			this.Controls.Add(this.groupBox6);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox2);
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
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.PAL_LastLineNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.PAL_FirstLineNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_LastLineNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.NTSC_FirstLineNumeric)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.groupBox6.ResumeLayout(false);
			this.groupBox6.PerformLayout();
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
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown NTSC_LastLineNumeric;
		private System.Windows.Forms.NumericUpDown NTSC_FirstLineNumeric;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.RadioButton rbClipNone;
		private System.Windows.Forms.RadioButton rbClipToFramebuffer;
		private System.Windows.Forms.RadioButton rbClipBasic;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.RadioButton rbWeave;
		private System.Windows.Forms.RadioButton rbBobOffset;
		private System.Windows.Forms.RadioButton rbBob;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.CheckBox cbLEC;
		private System.Windows.Forms.CheckBox cbGpuLag;
		private System.Windows.Forms.GroupBox groupBox6;
	}
}