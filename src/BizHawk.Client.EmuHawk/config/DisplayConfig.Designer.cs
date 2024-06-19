namespace BizHawk.Client.EmuHawk
{
	partial class DisplayConfig
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DisplayConfig));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lblScanlines = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblUserFilterName = new BizHawk.WinForms.Controls.LocLabelEx();
			this.btnSelectUserFilter = new System.Windows.Forms.Button();
			this.rbUser = new System.Windows.Forms.RadioButton();
			this.tbScanlineIntensity = new BizHawk.Client.EmuHawk.TransparentTrackBar();
			this.rbNone = new System.Windows.Forms.RadioButton();
			this.rbScanlines = new System.Windows.Forms.RadioButton();
			this.rbHq2x = new System.Windows.Forms.RadioButton();
			this.checkLetterbox = new System.Windows.Forms.CheckBox();
			this.checkPadInteger = new System.Windows.Forms.CheckBox();
			this.grpFinalFilter = new System.Windows.Forms.GroupBox();
			this.rbFinalFilterBicubic = new System.Windows.Forms.RadioButton();
			this.rbFinalFilterNone = new System.Windows.Forms.RadioButton();
			this.rbFinalFilterBilinear = new System.Windows.Forms.RadioButton();
			this.rbUseRaw = new System.Windows.Forms.RadioButton();
			this.rbUseSystem = new System.Windows.Forms.RadioButton();
			this.grpARSelection = new System.Windows.Forms.GroupBox();
			this.txtCustomARY = new System.Windows.Forms.TextBox();
			this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.txtCustomARX = new System.Windows.Forms.TextBox();
			this.rbUseCustomRatio = new System.Windows.Forms.RadioButton();
			this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.txtCustomARHeight = new System.Windows.Forms.TextBox();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.txtCustomARWidth = new System.Windows.Forms.TextBox();
			this.rbUseCustom = new System.Windows.Forms.RadioButton();
			this.rbOpenGL = new System.Windows.Forms.RadioButton();
			this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tpAR = new System.Windows.Forms.TabPage();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.label16 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label15 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.txtCropBottom = new System.Windows.Forms.TextBox();
			this.label17 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.txtCropRight = new System.Windows.Forms.TextBox();
			this.txtCropTop = new System.Windows.Forms.TextBox();
			this.label14 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.txtCropLeft = new System.Windows.Forms.TextBox();
			this.btnDefaults = new System.Windows.Forms.Button();
			this.cbAutoPrescale = new System.Windows.Forms.CheckBox();
			this.label11 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label10 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.nudPrescale = new System.Windows.Forms.NumericUpDown();
			this.tpDispMethod = new System.Windows.Forms.TabPage();
			this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label13 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.cbAllowTearing = new System.Windows.Forms.CheckBox();
			this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.rbD3D11 = new System.Windows.Forms.RadioButton();
			this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.rbGDIPlus = new System.Windows.Forms.RadioButton();
			this.tpMisc = new System.Windows.Forms.TabPage();
			this.flpStaticWindowTitles = new BizHawk.WinForms.Controls.LocSzSingleColumnFLP();
			this.cbStaticWindowTitles = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblStaticWindowTitles = new BizHawk.WinForms.Controls.LocLabelEx();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.rbDisplayAbsoluteZero = new System.Windows.Forms.RadioButton();
			this.rbDisplayMinimal = new System.Windows.Forms.RadioButton();
			this.rbDisplayFull = new System.Windows.Forms.RadioButton();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.cbAllowDoubleclickFullscreen = new System.Windows.Forms.CheckBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.cbFSAutohideMouse = new System.Windows.Forms.CheckBox();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.cbFullscreenHacks = new System.Windows.Forms.CheckBox();
			this.cbStatusBarFullscreen = new System.Windows.Forms.CheckBox();
			this.cbMenuFullscreen = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lblFrameTypeWindowed = new BizHawk.WinForms.Controls.LocLabelEx();
			this.cbStatusBarWindowed = new System.Windows.Forms.CheckBox();
			this.label9 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.cbMenuWindowed = new System.Windows.Forms.CheckBox();
			this.trackbarFrameSizeWindowed = new BizHawk.Client.EmuHawk.TransparentTrackBar();
			this.cbCaptionWindowed = new System.Windows.Forms.CheckBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.cbMainFormSaveWindowPosition = new System.Windows.Forms.CheckBox();
			this.cbMainFormStayOnTop = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).BeginInit();
			this.grpFinalFilter.SuspendLayout();
			this.grpARSelection.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.tpAR.SuspendLayout();
			this.groupBox6.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudPrescale)).BeginInit();
			this.tpDispMethod.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.tpMisc.SuspendLayout();
			this.flpStaticWindowTitles.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackbarFrameSizeWindowed)).BeginInit();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(473, 339);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(392, 339);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 4;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.lblScanlines);
			this.groupBox1.Controls.Add(this.lblUserFilterName);
			this.groupBox1.Controls.Add(this.btnSelectUserFilter);
			this.groupBox1.Controls.Add(this.rbUser);
			this.groupBox1.Controls.Add(this.tbScanlineIntensity);
			this.groupBox1.Controls.Add(this.rbNone);
			this.groupBox1.Controls.Add(this.rbScanlines);
			this.groupBox1.Controls.Add(this.rbHq2x);
			this.groupBox1.Location = new System.Drawing.Point(6, 33);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(193, 132);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Scaling Filter";
			// 
			// lblScanlines
			// 
			this.lblScanlines.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblScanlines.Location = new System.Drawing.Point(104, 39);
			this.lblScanlines.Name = "lblScanlines";
			this.lblScanlines.Text = "%";
			// 
			// lblUserFilterName
			// 
			this.lblUserFilterName.Location = new System.Drawing.Point(6, 114);
			this.lblUserFilterName.Name = "lblUserFilterName";
			this.lblUserFilterName.Text = "Will contain user filter name";
			// 
			// btnSelectUserFilter
			// 
			this.btnSelectUserFilter.Location = new System.Drawing.Point(83, 88);
			this.btnSelectUserFilter.Name = "btnSelectUserFilter";
			this.btnSelectUserFilter.Size = new System.Drawing.Size(75, 23);
			this.btnSelectUserFilter.TabIndex = 5;
			this.btnSelectUserFilter.Text = "Select";
			this.btnSelectUserFilter.UseVisualStyleBackColor = true;
			this.btnSelectUserFilter.Click += new System.EventHandler(this.BtnSelectUserFilter_Click);
			// 
			// rbUser
			// 
			this.rbUser.AutoSize = true;
			this.rbUser.Location = new System.Drawing.Point(6, 88);
			this.rbUser.Name = "rbUser";
			this.rbUser.Size = new System.Drawing.Size(47, 17);
			this.rbUser.TabIndex = 4;
			this.rbUser.TabStop = true;
			this.rbUser.Text = "User";
			this.rbUser.UseVisualStyleBackColor = true;
			// 
			// tbScanlineIntensity
			// 
			this.tbScanlineIntensity.LargeChange = 32;
			this.tbScanlineIntensity.Location = new System.Drawing.Point(83, 55);
			this.tbScanlineIntensity.Maximum = 256;
			this.tbScanlineIntensity.Name = "tbScanlineIntensity";
			this.tbScanlineIntensity.Size = new System.Drawing.Size(70, 42);
			this.tbScanlineIntensity.TabIndex = 3;
			this.tbScanlineIntensity.TickFrequency = 32;
			this.tbScanlineIntensity.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
			this.tbScanlineIntensity.Scroll += new System.EventHandler(this.TbScanlineIntensity_Scroll);
			this.tbScanlineIntensity.ValueChanged += new System.EventHandler(this.TbScanlineIntensity_Scroll);
			// 
			// rbNone
			// 
			this.rbNone.AutoSize = true;
			this.rbNone.Location = new System.Drawing.Point(6, 19);
			this.rbNone.Name = "rbNone";
			this.rbNone.Size = new System.Drawing.Size(51, 17);
			this.rbNone.TabIndex = 2;
			this.rbNone.TabStop = true;
			this.rbNone.Text = "None";
			this.rbNone.UseVisualStyleBackColor = true;
			// 
			// rbScanlines
			// 
			this.rbScanlines.AutoSize = true;
			this.rbScanlines.Location = new System.Drawing.Point(6, 65);
			this.rbScanlines.Name = "rbScanlines";
			this.rbScanlines.Size = new System.Drawing.Size(71, 17);
			this.rbScanlines.TabIndex = 1;
			this.rbScanlines.TabStop = true;
			this.rbScanlines.Text = "Scanlines";
			this.rbScanlines.UseVisualStyleBackColor = true;
			// 
			// rbHq2x
			// 
			this.rbHq2x.AutoSize = true;
			this.rbHq2x.Location = new System.Drawing.Point(6, 42);
			this.rbHq2x.Name = "rbHq2x";
			this.rbHq2x.Size = new System.Drawing.Size(50, 17);
			this.rbHq2x.TabIndex = 0;
			this.rbHq2x.TabStop = true;
			this.rbHq2x.Text = "Hq2x";
			this.rbHq2x.UseVisualStyleBackColor = true;
			// 
			// checkLetterbox
			// 
			this.checkLetterbox.AutoSize = true;
			this.checkLetterbox.Location = new System.Drawing.Point(209, 12);
			this.checkLetterbox.Name = "checkLetterbox";
			this.checkLetterbox.Size = new System.Drawing.Size(173, 17);
			this.checkLetterbox.TabIndex = 8;
			this.checkLetterbox.Text = "Maintain aspect ratio (letterbox)";
			this.checkLetterbox.UseVisualStyleBackColor = true;
			this.checkLetterbox.CheckedChanged += new System.EventHandler(this.CheckLetterbox_CheckedChanged);
			// 
			// checkPadInteger
			// 
			this.checkPadInteger.AutoSize = true;
			this.checkPadInteger.Location = new System.Drawing.Point(218, 171);
			this.checkPadInteger.Name = "checkPadInteger";
			this.checkPadInteger.Size = new System.Drawing.Size(250, 17);
			this.checkPadInteger.TabIndex = 9;
			this.checkPadInteger.Text = "Expand pixels by integers only (e.g. no 1.3333x)";
			this.checkPadInteger.UseVisualStyleBackColor = true;
			this.checkPadInteger.CheckedChanged += new System.EventHandler(this.CheckPadInteger_CheckedChanged);
			// 
			// grpFinalFilter
			// 
			this.grpFinalFilter.Controls.Add(this.rbFinalFilterBicubic);
			this.grpFinalFilter.Controls.Add(this.rbFinalFilterNone);
			this.grpFinalFilter.Controls.Add(this.rbFinalFilterBilinear);
			this.grpFinalFilter.Location = new System.Drawing.Point(6, 194);
			this.grpFinalFilter.Name = "grpFinalFilter";
			this.grpFinalFilter.Size = new System.Drawing.Size(187, 90);
			this.grpFinalFilter.TabIndex = 8;
			this.grpFinalFilter.TabStop = false;
			this.grpFinalFilter.Text = "Final Filter";
			// 
			// rbFinalFilterBicubic
			// 
			this.rbFinalFilterBicubic.AutoSize = true;
			this.rbFinalFilterBicubic.Location = new System.Drawing.Point(6, 64);
			this.rbFinalFilterBicubic.Name = "rbFinalFilterBicubic";
			this.rbFinalFilterBicubic.Size = new System.Drawing.Size(142, 17);
			this.rbFinalFilterBicubic.TabIndex = 3;
			this.rbFinalFilterBicubic.TabStop = true;
			this.rbFinalFilterBicubic.Text = "Bicubic (shader. buggy?)";
			this.rbFinalFilterBicubic.UseVisualStyleBackColor = true;
			// 
			// rbFinalFilterNone
			// 
			this.rbFinalFilterNone.AutoSize = true;
			this.rbFinalFilterNone.Location = new System.Drawing.Point(6, 18);
			this.rbFinalFilterNone.Name = "rbFinalFilterNone";
			this.rbFinalFilterNone.Size = new System.Drawing.Size(51, 17);
			this.rbFinalFilterNone.TabIndex = 2;
			this.rbFinalFilterNone.TabStop = true;
			this.rbFinalFilterNone.Text = "None";
			this.rbFinalFilterNone.UseVisualStyleBackColor = true;
			// 
			// rbFinalFilterBilinear
			// 
			this.rbFinalFilterBilinear.AutoSize = true;
			this.rbFinalFilterBilinear.Location = new System.Drawing.Point(6, 41);
			this.rbFinalFilterBilinear.Name = "rbFinalFilterBilinear";
			this.rbFinalFilterBilinear.Size = new System.Drawing.Size(59, 17);
			this.rbFinalFilterBilinear.TabIndex = 0;
			this.rbFinalFilterBilinear.TabStop = true;
			this.rbFinalFilterBilinear.Text = "Bilinear";
			this.rbFinalFilterBilinear.UseVisualStyleBackColor = true;
			// 
			// rbUseRaw
			// 
			this.rbUseRaw.AutoSize = true;
			this.rbUseRaw.Location = new System.Drawing.Point(6, 19);
			this.rbUseRaw.Name = "rbUseRaw";
			this.rbUseRaw.Size = new System.Drawing.Size(240, 17);
			this.rbUseRaw.TabIndex = 11;
			this.rbUseRaw.TabStop = true;
			this.rbUseRaw.Text = "Use 1:1 pixel size (for crispness or debugging)";
			this.rbUseRaw.UseVisualStyleBackColor = true;
			this.rbUseRaw.CheckedChanged += new System.EventHandler(this.RbUseRaw_CheckedChanged);
			// 
			// rbUseSystem
			// 
			this.rbUseSystem.AutoSize = true;
			this.rbUseSystem.Location = new System.Drawing.Point(16, 58);
			this.rbUseSystem.Name = "rbUseSystem";
			this.rbUseSystem.Size = new System.Drawing.Size(167, 17);
			this.rbUseSystem.TabIndex = 12;
			this.rbUseSystem.TabStop = true;
			this.rbUseSystem.Text = "Use system\'s recommendation";
			this.rbUseSystem.UseVisualStyleBackColor = true;
			this.rbUseSystem.CheckedChanged += new System.EventHandler(this.RbUseSystem_CheckedChanged);
			// 
			// grpARSelection
			// 
			this.grpARSelection.Controls.Add(this.txtCustomARY);
			this.grpARSelection.Controls.Add(this.label12);
			this.grpARSelection.Controls.Add(this.txtCustomARX);
			this.grpARSelection.Controls.Add(this.rbUseCustomRatio);
			this.grpARSelection.Controls.Add(this.label4);
			this.grpARSelection.Controls.Add(this.txtCustomARHeight);
			this.grpARSelection.Controls.Add(this.label3);
			this.grpARSelection.Controls.Add(this.txtCustomARWidth);
			this.grpARSelection.Controls.Add(this.rbUseCustom);
			this.grpARSelection.Controls.Add(this.rbUseRaw);
			this.grpARSelection.Controls.Add(this.rbUseSystem);
			this.grpARSelection.Location = new System.Drawing.Point(218, 35);
			this.grpARSelection.Name = "grpARSelection";
			this.grpARSelection.Size = new System.Drawing.Size(302, 130);
			this.grpARSelection.TabIndex = 13;
			this.grpARSelection.TabStop = false;
			this.grpARSelection.Text = "Aspect Ratio Selection";
			// 
			// txtCustomARY
			// 
			this.txtCustomARY.Location = new System.Drawing.Point(220, 102);
			this.txtCustomARY.Name = "txtCustomARY";
			this.txtCustomARY.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARY.TabIndex = 19;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(202, 107);
			this.label12.Name = "label12";
			this.label12.Text = ":";
			// 
			// txtCustomARX
			// 
			this.txtCustomARX.Location = new System.Drawing.Point(124, 102);
			this.txtCustomARX.Name = "txtCustomARX";
			this.txtCustomARX.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARX.TabIndex = 18;
			// 
			// rbUseCustomRatio
			// 
			this.rbUseCustomRatio.AutoSize = true;
			this.rbUseCustomRatio.Location = new System.Drawing.Point(16, 103);
			this.rbUseCustomRatio.Name = "rbUseCustomRatio";
			this.rbUseCustomRatio.Size = new System.Drawing.Size(102, 17);
			this.rbUseCustomRatio.TabIndex = 16;
			this.rbUseCustomRatio.TabStop = true;
			this.rbUseCustomRatio.Text = "Use custom AR:";
			this.rbUseCustomRatio.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(13, 41);
			this.label4.Name = "label4";
			this.label4.Text = "Allow pixel distortion (e.g. 2x1 pixels, for better AR fit):";
			// 
			// txtCustomARHeight
			// 
			this.txtCustomARHeight.Location = new System.Drawing.Point(220, 79);
			this.txtCustomARHeight.Name = "txtCustomARHeight";
			this.txtCustomARHeight.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARHeight.TabIndex = 15;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(202, 84);
			this.label3.Name = "label3";
			this.label3.Text = "x";
			// 
			// txtCustomARWidth
			// 
			this.txtCustomARWidth.Location = new System.Drawing.Point(124, 79);
			this.txtCustomARWidth.Name = "txtCustomARWidth";
			this.txtCustomARWidth.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARWidth.TabIndex = 14;
			// 
			// rbUseCustom
			// 
			this.rbUseCustom.AutoSize = true;
			this.rbUseCustom.Location = new System.Drawing.Point(16, 80);
			this.rbUseCustom.Name = "rbUseCustom";
			this.rbUseCustom.Size = new System.Drawing.Size(105, 17);
			this.rbUseCustom.TabIndex = 13;
			this.rbUseCustom.TabStop = true;
			this.rbUseCustom.Text = "Use custom size:";
			this.rbUseCustom.UseVisualStyleBackColor = true;
			// 
			// rbOpenGL
			// 
			this.rbOpenGL.AutoSize = true;
			this.rbOpenGL.Checked = true;
			this.rbOpenGL.Location = new System.Drawing.Point(6, 103);
			this.rbOpenGL.Name = "rbOpenGL";
			this.rbOpenGL.Size = new System.Drawing.Size(65, 17);
			this.rbOpenGL.TabIndex = 3;
			this.rbOpenGL.TabStop = true;
			this.rbOpenGL.Text = "OpenGL";
			this.rbOpenGL.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(21, 123);
			this.label5.Name = "label5";
			this.label5.Text = " • May malfunction on some systems.\r\n • May have increased performance for OpenGL" +
    "-based emulation cores.\r\n • May have reduced performance on some systems.\r\n";
			// 
			// tabControl1
			// 
			this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tabControl1.Controls.Add(this.tpAR);
			this.tabControl1.Controls.Add(this.tpDispMethod);
			this.tabControl1.Controls.Add(this.tpMisc);
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Location = new System.Drawing.Point(12, 12);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(536, 317);
			this.tabControl1.TabIndex = 17;
			// 
			// tpAR
			// 
			this.tpAR.Controls.Add(this.groupBox6);
			this.tpAR.Controls.Add(this.btnDefaults);
			this.tpAR.Controls.Add(this.cbAutoPrescale);
			this.tpAR.Controls.Add(this.label11);
			this.tpAR.Controls.Add(this.groupBox1);
			this.tpAR.Controls.Add(this.label10);
			this.tpAR.Controls.Add(this.checkLetterbox);
			this.tpAR.Controls.Add(this.nudPrescale);
			this.tpAR.Controls.Add(this.checkPadInteger);
			this.tpAR.Controls.Add(this.grpARSelection);
			this.tpAR.Controls.Add(this.grpFinalFilter);
			this.tpAR.Location = new System.Drawing.Point(4, 22);
			this.tpAR.Name = "tpAR";
			this.tpAR.Padding = new System.Windows.Forms.Padding(3);
			this.tpAR.Size = new System.Drawing.Size(528, 291);
			this.tpAR.TabIndex = 0;
			this.tpAR.Text = "Scaling & Filtering";
			this.tpAR.UseVisualStyleBackColor = true;
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.label16);
			this.groupBox6.Controls.Add(this.label15);
			this.groupBox6.Controls.Add(this.txtCropBottom);
			this.groupBox6.Controls.Add(this.label17);
			this.groupBox6.Controls.Add(this.txtCropRight);
			this.groupBox6.Controls.Add(this.txtCropTop);
			this.groupBox6.Controls.Add(this.label14);
			this.groupBox6.Controls.Add(this.txtCropLeft);
			this.groupBox6.Location = new System.Drawing.Point(218, 195);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(302, 61);
			this.groupBox6.TabIndex = 9;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Cropping";
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(217, 25);
			this.label16.Name = "label16";
			this.label16.Text = "Bottom:";
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point(74, 25);
			this.label15.Name = "label15";
			this.label15.Text = "Top:";
			// 
			// txtCropBottom
			// 
			this.txtCropBottom.Location = new System.Drawing.Point(261, 22);
			this.txtCropBottom.Name = "txtCropBottom";
			this.txtCropBottom.Size = new System.Drawing.Size(34, 20);
			this.txtCropBottom.TabIndex = 28;
			this.txtCropBottom.Text = "8000";
			// 
			// label17
			// 
			this.label17.Location = new System.Drawing.Point(144, 25);
			this.label17.Name = "label17";
			this.label17.Text = "Right:";
			// 
			// txtCropRight
			// 
			this.txtCropRight.Location = new System.Drawing.Point(180, 22);
			this.txtCropRight.Name = "txtCropRight";
			this.txtCropRight.Size = new System.Drawing.Size(34, 20);
			this.txtCropRight.TabIndex = 25;
			this.txtCropRight.Text = "8000";
			// 
			// txtCropTop
			// 
			this.txtCropTop.Location = new System.Drawing.Point(104, 22);
			this.txtCropTop.Name = "txtCropTop";
			this.txtCropTop.Size = new System.Drawing.Size(34, 20);
			this.txtCropTop.TabIndex = 24;
			this.txtCropTop.Text = "8000";
			// 
			// label14
			// 
			this.label14.Location = new System.Drawing.Point(6, 25);
			this.label14.Name = "label14";
			this.label14.Text = "Left:";
			// 
			// txtCropLeft
			// 
			this.txtCropLeft.Location = new System.Drawing.Point(34, 22);
			this.txtCropLeft.Name = "txtCropLeft";
			this.txtCropLeft.Size = new System.Drawing.Size(34, 20);
			this.txtCropLeft.TabIndex = 15;
			this.txtCropLeft.Text = "8000";
			// 
			// btnDefaults
			// 
			this.btnDefaults.Location = new System.Drawing.Point(447, 262);
			this.btnDefaults.Name = "btnDefaults";
			this.btnDefaults.Size = new System.Drawing.Size(75, 23);
			this.btnDefaults.TabIndex = 18;
			this.btnDefaults.Text = "Defaults";
			this.toolTip1.SetToolTip(this.btnDefaults, "Unless I forgot to update the button\'s code when I changed a default");
			this.btnDefaults.UseVisualStyleBackColor = true;
			this.btnDefaults.Click += new System.EventHandler(this.BtnDefaults_Click);
			// 
			// cbAutoPrescale
			// 
			this.cbAutoPrescale.AutoSize = true;
			this.cbAutoPrescale.Location = new System.Drawing.Point(6, 171);
			this.cbAutoPrescale.Name = "cbAutoPrescale";
			this.cbAutoPrescale.Size = new System.Drawing.Size(92, 17);
			this.cbAutoPrescale.TabIndex = 17;
			this.cbAutoPrescale.Text = "Auto Prescale";
			this.cbAutoPrescale.UseVisualStyleBackColor = true;
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(140, 11);
			this.label11.Name = "label11";
			this.label11.Text = "X";
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(7, 11);
			this.label10.Name = "label10";
			this.label10.Text = "User Prescale:";
			// 
			// nudPrescale
			// 
			this.nudPrescale.Location = new System.Drawing.Point(93, 7);
			this.nudPrescale.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.nudPrescale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudPrescale.Name = "nudPrescale";
			this.nudPrescale.Size = new System.Drawing.Size(45, 20);
			this.nudPrescale.TabIndex = 14;
			this.nudPrescale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// tpDispMethod
			// 
			this.tpDispMethod.Controls.Add(this.label6);
			this.tpDispMethod.Controls.Add(this.groupBox3);
			this.tpDispMethod.Location = new System.Drawing.Point(4, 22);
			this.tpDispMethod.Name = "tpDispMethod";
			this.tpDispMethod.Size = new System.Drawing.Size(528, 291);
			this.tpDispMethod.TabIndex = 2;
			this.tpDispMethod.Text = "Display Method";
			this.tpDispMethod.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(3, 258);
			this.label6.Name = "label6";
			this.label6.Text = "Changes require restart of program to take effect.\r\n";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.label13);
			this.groupBox3.Controls.Add(this.cbAllowTearing);
			this.groupBox3.Controls.Add(this.label8);
			this.groupBox3.Controls.Add(this.rbD3D11);
			this.groupBox3.Controls.Add(this.label7);
			this.groupBox3.Controls.Add(this.rbGDIPlus);
			this.groupBox3.Controls.Add(this.label5);
			this.groupBox3.Controls.Add(this.rbOpenGL);
			this.groupBox3.Location = new System.Drawing.Point(6, 5);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(415, 241);
			this.groupBox3.TabIndex = 16;
			this.groupBox3.TabStop = false;
			// 
			// label13
			// 
			this.label13.Location = new System.Drawing.Point(45, 60);
			this.label13.Name = "label13";
			this.label13.Text = resources.GetString("label13.Text");
			this.label13.Click += new System.EventHandler(this.Label13_Click);
			this.label13.DoubleClick += new System.EventHandler(this.Label13_Click);
			// 
			// cbAllowTearing
			// 
			this.cbAllowTearing.AutoSize = true;
			this.cbAllowTearing.Location = new System.Drawing.Point(28, 60);
			this.cbAllowTearing.Name = "cbAllowTearing";
			this.cbAllowTearing.Size = new System.Drawing.Size(15, 14);
			this.cbAllowTearing.TabIndex = 21;
			this.cbAllowTearing.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(21, 30);
			this.label8.Name = "label8";
			this.label8.Text = " • Best compatibility\r\n • May have decreased performance for OpenGL-based cores (NDS, 3DS)\r\n";
			// 
			// rbD3D11
			// 
			this.rbD3D11.AutoSize = true;
			this.rbD3D11.Checked = true;
			this.rbD3D11.Location = new System.Drawing.Point(6, 10);
			this.rbD3D11.Name = "rbD3D11";
			this.rbD3D11.Size = new System.Drawing.Size(73, 17);
			this.rbD3D11.TabIndex = 19;
			this.rbD3D11.TabStop = true;
			this.rbD3D11.Text = "Direct3D11";
			this.rbD3D11.UseVisualStyleBackColor = true;
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(21, 191);
			this.label7.Name = "label7";
			this.label7.Text = " • Slow; Mainly for compatibility purposes\r\n • Missing many features\r\n • Works be" +
    "tter over Remote Desktop, etc.\r\n";
			// 
			// rbGDIPlus
			// 
			this.rbGDIPlus.AutoSize = true;
			this.rbGDIPlus.Checked = true;
			this.rbGDIPlus.Location = new System.Drawing.Point(6, 171);
			this.rbGDIPlus.Name = "rbGDIPlus";
			this.rbGDIPlus.Size = new System.Drawing.Size(50, 17);
			this.rbGDIPlus.TabIndex = 17;
			this.rbGDIPlus.TabStop = true;
			this.rbGDIPlus.Text = "GDI+";
			this.rbGDIPlus.UseVisualStyleBackColor = true;
			// 
			// tpMisc
			// 
			this.tpMisc.Controls.Add(this.flpStaticWindowTitles);
			this.tpMisc.Controls.Add(this.groupBox5);
			this.tpMisc.Location = new System.Drawing.Point(4, 22);
			this.tpMisc.Name = "tpMisc";
			this.tpMisc.Size = new System.Drawing.Size(528, 291);
			this.tpMisc.TabIndex = 3;
			this.tpMisc.Text = "Misc";
			this.tpMisc.UseVisualStyleBackColor = true;
			// 
			// flpStaticWindowTitles
			// 
			this.flpStaticWindowTitles.Controls.Add(this.cbStaticWindowTitles);
			this.flpStaticWindowTitles.Controls.Add(this.lblStaticWindowTitles);
			this.flpStaticWindowTitles.Location = new System.Drawing.Point(6, 109);
			this.flpStaticWindowTitles.Name = "flpStaticWindowTitles";
			this.flpStaticWindowTitles.Size = new System.Drawing.Size(490, 52);
			// 
			// cbStaticWindowTitles
			// 
			this.cbStaticWindowTitles.Name = "cbStaticWindowTitles";
			this.cbStaticWindowTitles.Text = "Keep window titles static";
			// 
			// lblStaticWindowTitles
			// 
			this.lblStaticWindowTitles.Location = new System.Drawing.Point(19, 23);
			this.lblStaticWindowTitles.Margin = new System.Windows.Forms.Padding(19, 0, 3, 0);
			this.lblStaticWindowTitles.Name = "lblStaticWindowTitles";
			this.lblStaticWindowTitles.Text = "Some tools put filenames, status, etc. in their window titles.\nChecking this disa" +
    "bles those features, but may fix problems with window capture (i.e. in OBS).";
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.rbDisplayAbsoluteZero);
			this.groupBox5.Controls.Add(this.rbDisplayMinimal);
			this.groupBox5.Controls.Add(this.rbDisplayFull);
			this.groupBox5.Location = new System.Drawing.Point(6, 6);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(371, 96);
			this.groupBox5.TabIndex = 20;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Display Features (for speeding up replays)";
			// 
			// rbDisplayAbsoluteZero
			// 
			this.rbDisplayAbsoluteZero.AutoSize = true;
			this.rbDisplayAbsoluteZero.Location = new System.Drawing.Point(7, 66);
			this.rbDisplayAbsoluteZero.Name = "rbDisplayAbsoluteZero";
			this.rbDisplayAbsoluteZero.Size = new System.Drawing.Size(174, 17);
			this.rbDisplayAbsoluteZero.TabIndex = 2;
			this.rbDisplayAbsoluteZero.TabStop = true;
			this.rbDisplayAbsoluteZero.Text = "Absolute Zero - Display Nothing";
			this.rbDisplayAbsoluteZero.UseVisualStyleBackColor = true;
			// 
			// rbDisplayMinimal
			// 
			this.rbDisplayMinimal.AutoSize = true;
			this.rbDisplayMinimal.Enabled = false;
			this.rbDisplayMinimal.Location = new System.Drawing.Point(7, 43);
			this.rbDisplayMinimal.Name = "rbDisplayMinimal";
			this.rbDisplayMinimal.Size = new System.Drawing.Size(185, 17);
			this.rbDisplayMinimal.TabIndex = 1;
			this.rbDisplayMinimal.TabStop = true;
			this.rbDisplayMinimal.Text = "Minimal - Display HUD Only (TBD)";
			this.rbDisplayMinimal.UseVisualStyleBackColor = true;
			// 
			// rbDisplayFull
			// 
			this.rbDisplayFull.AutoSize = true;
			this.rbDisplayFull.Location = new System.Drawing.Point(7, 20);
			this.rbDisplayFull.Name = "rbDisplayFull";
			this.rbDisplayFull.Size = new System.Drawing.Size(137, 17);
			this.rbDisplayFull.TabIndex = 0;
			this.rbDisplayFull.TabStop = true;
			this.rbDisplayFull.Text = "Full - Display Everything";
			this.rbDisplayFull.UseVisualStyleBackColor = true;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.cbAllowDoubleclickFullscreen);
			this.tabPage1.Controls.Add(this.groupBox4);
			this.tabPage1.Controls.Add(this.groupBox2);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(528, 291);
			this.tabPage1.TabIndex = 4;
			this.tabPage1.Text = "Window";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// cbAllowDoubleclickFullscreen
			// 
			this.cbAllowDoubleclickFullscreen.AutoSize = true;
			this.cbAllowDoubleclickFullscreen.Location = new System.Drawing.Point(12, 223);
			this.cbAllowDoubleclickFullscreen.Name = "cbAllowDoubleclickFullscreen";
			this.cbAllowDoubleclickFullscreen.Size = new System.Drawing.Size(471, 17);
			this.cbAllowDoubleclickFullscreen.TabIndex = 27;
			this.cbAllowDoubleclickFullscreen.Text = "Allow Double-Click Fullscreen (hold shift to force fullscreen to toggle in case u" +
    "sing zapper, etc.)";
			this.cbAllowDoubleclickFullscreen.UseVisualStyleBackColor = true;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.cbFSAutohideMouse);
			this.groupBox4.Controls.Add(this.label1);
			this.groupBox4.Controls.Add(this.cbFullscreenHacks);
			this.groupBox4.Controls.Add(this.cbStatusBarFullscreen);
			this.groupBox4.Controls.Add(this.cbMenuFullscreen);
			this.groupBox4.Location = new System.Drawing.Point(153, 6);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(266, 211);
			this.groupBox4.TabIndex = 27;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Fullscreen";
			// 
			// cbFSAutohideMouse
			// 
			this.cbFSAutohideMouse.AutoSize = true;
			this.cbFSAutohideMouse.Location = new System.Drawing.Point(87, 19);
			this.cbFSAutohideMouse.Name = "cbFSAutohideMouse";
			this.cbFSAutohideMouse.Size = new System.Drawing.Size(141, 17);
			this.cbFSAutohideMouse.TabIndex = 28;
			this.cbFSAutohideMouse.Text = "Auto-Hide Mouse Cursor";
			this.cbFSAutohideMouse.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(7, 88);
			this.label1.Name = "label1";
			this.label1.Text = resources.GetString("label1.Text");
			// 
			// cbFullscreenHacks
			// 
			this.cbFullscreenHacks.AutoSize = true;
			this.cbFullscreenHacks.Location = new System.Drawing.Point(6, 65);
			this.cbFullscreenHacks.Name = "cbFullscreenHacks";
			this.cbFullscreenHacks.Size = new System.Drawing.Size(191, 17);
			this.cbFullscreenHacks.TabIndex = 26;
			this.cbFullscreenHacks.Text = "Enable Windows Fullscreen Hacks";
			this.cbFullscreenHacks.UseVisualStyleBackColor = true;
			// 
			// cbStatusBarFullscreen
			// 
			this.cbStatusBarFullscreen.AutoSize = true;
			this.cbStatusBarFullscreen.Location = new System.Drawing.Point(6, 19);
			this.cbStatusBarFullscreen.Name = "cbStatusBarFullscreen";
			this.cbStatusBarFullscreen.Size = new System.Drawing.Size(75, 17);
			this.cbStatusBarFullscreen.TabIndex = 23;
			this.cbStatusBarFullscreen.Text = "Status Bar";
			this.cbStatusBarFullscreen.UseVisualStyleBackColor = true;
			// 
			// cbMenuFullscreen
			// 
			this.cbMenuFullscreen.AutoSize = true;
			this.cbMenuFullscreen.Location = new System.Drawing.Point(6, 42);
			this.cbMenuFullscreen.Name = "cbMenuFullscreen";
			this.cbMenuFullscreen.Size = new System.Drawing.Size(53, 17);
			this.cbMenuFullscreen.TabIndex = 25;
			this.cbMenuFullscreen.Text = "Menu";
			this.cbMenuFullscreen.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cbMainFormStayOnTop);
			this.groupBox2.Controls.Add(this.cbMainFormSaveWindowPosition);
			this.groupBox2.Controls.Add(this.lblFrameTypeWindowed);
			this.groupBox2.Controls.Add(this.cbStatusBarWindowed);
			this.groupBox2.Controls.Add(this.label9);
			this.groupBox2.Controls.Add(this.cbMenuWindowed);
			this.groupBox2.Controls.Add(this.trackbarFrameSizeWindowed);
			this.groupBox2.Controls.Add(this.cbCaptionWindowed);
			this.groupBox2.Location = new System.Drawing.Point(6, 6);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(141, 211);
			this.groupBox2.TabIndex = 26;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Windowed";
			// 
			// lblFrameTypeWindowed
			// 
			this.lblFrameTypeWindowed.Location = new System.Drawing.Point(51, 17);
			this.lblFrameTypeWindowed.Name = "lblFrameTypeWindowed";
			this.lblFrameTypeWindowed.Text = "(frame type)";
			// 
			// cbStatusBarWindowed
			// 
			this.cbStatusBarWindowed.AutoSize = true;
			this.cbStatusBarWindowed.Location = new System.Drawing.Point(9, 81);
			this.cbStatusBarWindowed.Name = "cbStatusBarWindowed";
			this.cbStatusBarWindowed.Size = new System.Drawing.Size(75, 17);
			this.cbStatusBarWindowed.TabIndex = 23;
			this.cbStatusBarWindowed.Text = "Status Bar";
			this.cbStatusBarWindowed.UseVisualStyleBackColor = true;
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(6, 17);
			this.label9.Name = "label9";
			this.label9.Text = "Frame:";
			// 
			// cbMenuWindowed
			// 
			this.cbMenuWindowed.AutoSize = true;
			this.cbMenuWindowed.Location = new System.Drawing.Point(9, 127);
			this.cbMenuWindowed.Name = "cbMenuWindowed";
			this.cbMenuWindowed.Size = new System.Drawing.Size(53, 17);
			this.cbMenuWindowed.TabIndex = 25;
			this.cbMenuWindowed.Text = "Menu";
			this.cbMenuWindowed.UseVisualStyleBackColor = true;
			// 
			// trackbarFrameSizeWindowed
			// 
			this.trackbarFrameSizeWindowed.LargeChange = 1;
			this.trackbarFrameSizeWindowed.Location = new System.Drawing.Point(6, 33);
			this.trackbarFrameSizeWindowed.Maximum = 2;
			this.trackbarFrameSizeWindowed.Name = "trackbarFrameSizeWindowed";
			this.trackbarFrameSizeWindowed.Size = new System.Drawing.Size(99, 42);
			this.trackbarFrameSizeWindowed.TabIndex = 21;
			this.trackbarFrameSizeWindowed.Value = 1;
			this.trackbarFrameSizeWindowed.ValueChanged += new System.EventHandler(this.TrackBarFrameSizeWindowed_ValueChanged);
			// 
			// cbCaptionWindowed
			// 
			this.cbCaptionWindowed.AutoSize = true;
			this.cbCaptionWindowed.Location = new System.Drawing.Point(9, 104);
			this.cbCaptionWindowed.Name = "cbCaptionWindowed";
			this.cbCaptionWindowed.Size = new System.Drawing.Size(62, 17);
			this.cbCaptionWindowed.TabIndex = 24;
			this.cbCaptionWindowed.Text = "Caption";
			this.cbCaptionWindowed.UseVisualStyleBackColor = true;
			// 
			// linkLabel1
			// 
			this.linkLabel1.AutoSize = true;
			this.linkLabel1.Location = new System.Drawing.Point(12, 404);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(79, 13);
			this.linkLabel1.TabIndex = 18;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "Documentation";
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1_LinkClicked);
			// 
			// cbMainFormSaveWindowPosition
			// 
			this.cbMainFormSaveWindowPosition.AutoSize = true;
			this.cbMainFormSaveWindowPosition.Location = new System.Drawing.Point(9, 150);
			this.cbMainFormSaveWindowPosition.Name = "cbMainFormSaveWindowPosition";
			this.cbMainFormSaveWindowPosition.Size = new System.Drawing.Size(133, 17);
			this.cbMainFormSaveWindowPosition.TabIndex = 26;
			this.cbMainFormSaveWindowPosition.Text = "Save Window Position";
			this.cbMainFormSaveWindowPosition.UseVisualStyleBackColor = true;
			// 
			// cbMainFormStayOnTop
			// 
			this.cbMainFormStayOnTop.AutoSize = true;
			this.cbMainFormStayOnTop.Location = new System.Drawing.Point(9, 174);
			this.cbMainFormStayOnTop.Name = "cbMainFormStayOnTop";
			this.cbMainFormStayOnTop.Size = new System.Drawing.Size(84, 17);
			this.cbMainFormStayOnTop.TabIndex = 27;
			this.cbMainFormStayOnTop.Text = "Stay on Top";
			this.cbMainFormStayOnTop.UseVisualStyleBackColor = true;
			// 
			// DisplayConfigLite
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(564, 374);
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "DisplayConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Display Configuration";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).EndInit();
			this.grpFinalFilter.ResumeLayout(false);
			this.grpFinalFilter.PerformLayout();
			this.grpARSelection.ResumeLayout(false);
			this.grpARSelection.PerformLayout();
			this.tabControl1.ResumeLayout(false);
			this.tpAR.ResumeLayout(false);
			this.tpAR.PerformLayout();
			this.groupBox6.ResumeLayout(false);
			this.groupBox6.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudPrescale)).EndInit();
			this.tpDispMethod.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.tpMisc.ResumeLayout(false);
			this.tpMisc.PerformLayout();
			this.flpStaticWindowTitles.ResumeLayout(false);
			this.flpStaticWindowTitles.PerformLayout();
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackbarFrameSizeWindowed)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbNone;
		private System.Windows.Forms.RadioButton rbScanlines;
		private System.Windows.Forms.RadioButton rbHq2x;
		private BizHawk.Client.EmuHawk.TransparentTrackBar tbScanlineIntensity;
		private System.Windows.Forms.CheckBox checkLetterbox;
		private System.Windows.Forms.CheckBox checkPadInteger;
		private System.Windows.Forms.GroupBox grpFinalFilter;
		private System.Windows.Forms.RadioButton rbFinalFilterBicubic;
		private System.Windows.Forms.RadioButton rbFinalFilterNone;
		private System.Windows.Forms.RadioButton rbFinalFilterBilinear;
		private System.Windows.Forms.Button btnSelectUserFilter;
		private System.Windows.Forms.RadioButton rbUser;
		private BizHawk.WinForms.Controls.LocLabelEx lblUserFilterName;
		private System.Windows.Forms.RadioButton rbUseRaw;
		private System.Windows.Forms.RadioButton rbUseSystem;
		private System.Windows.Forms.GroupBox grpARSelection;
		private BizHawk.WinForms.Controls.LocLabelEx lblScanlines;
		private System.Windows.Forms.TextBox txtCustomARHeight;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.TextBox txtCustomARWidth;
		private System.Windows.Forms.RadioButton rbUseCustom;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.RadioButton rbOpenGL;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tpAR;
		private System.Windows.Forms.TabPage tpDispMethod;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.GroupBox groupBox3;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private System.Windows.Forms.RadioButton rbGDIPlus;
		private System.Windows.Forms.TabPage tpMisc;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private System.Windows.Forms.RadioButton rbD3D11;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.CheckBox cbStatusBarWindowed;
		private BizHawk.WinForms.Controls.LocLabelEx label9;
		private BizHawk.Client.EmuHawk.TransparentTrackBar trackbarFrameSizeWindowed;
		private System.Windows.Forms.CheckBox cbMenuWindowed;
		private System.Windows.Forms.CheckBox cbCaptionWindowed;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.CheckBox cbStatusBarFullscreen;
		private System.Windows.Forms.CheckBox cbMenuFullscreen;
		private System.Windows.Forms.GroupBox groupBox2;
		private BizHawk.WinForms.Controls.LocLabelEx lblFrameTypeWindowed;
		private BizHawk.WinForms.Controls.LocLabelEx label11;
		private BizHawk.WinForms.Controls.LocLabelEx label10;
		private System.Windows.Forms.NumericUpDown nudPrescale;
		private System.Windows.Forms.CheckBox cbFSAutohideMouse;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.RadioButton rbDisplayAbsoluteZero;
		private System.Windows.Forms.RadioButton rbDisplayMinimal;
		private System.Windows.Forms.RadioButton rbDisplayFull;
		private System.Windows.Forms.CheckBox cbAllowDoubleclickFullscreen;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.RadioButton rbUseCustomRatio;
		private System.Windows.Forms.TextBox txtCustomARY;
		private BizHawk.WinForms.Controls.LocLabelEx label12;
		private System.Windows.Forms.TextBox txtCustomARX;
		private System.Windows.Forms.CheckBox cbAutoPrescale;
		private BizHawk.WinForms.Controls.LocLabelEx label13;
		private System.Windows.Forms.CheckBox cbAllowTearing;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.CheckBox cbFullscreenHacks;
		private System.Windows.Forms.Button btnDefaults;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.GroupBox groupBox6;
		private BizHawk.WinForms.Controls.LocLabelEx label16;
		private BizHawk.WinForms.Controls.LocLabelEx label15;
		private System.Windows.Forms.TextBox txtCropBottom;
		private BizHawk.WinForms.Controls.LocLabelEx label17;
		private System.Windows.Forms.TextBox txtCropRight;
		private System.Windows.Forms.TextBox txtCropTop;
		private BizHawk.WinForms.Controls.LocLabelEx label14;
		private System.Windows.Forms.TextBox txtCropLeft;
		private WinForms.Controls.LocSzSingleColumnFLP flpStaticWindowTitles;
		private WinForms.Controls.CheckBoxEx cbStaticWindowTitles;
		private WinForms.Controls.LocLabelEx lblStaticWindowTitles;
		private System.Windows.Forms.CheckBox cbMainFormStayOnTop;
		private System.Windows.Forms.CheckBox cbMainFormSaveWindowPosition;
	}
}