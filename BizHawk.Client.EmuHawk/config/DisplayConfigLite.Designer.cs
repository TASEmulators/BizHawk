namespace BizHawk.Client.EmuHawk
{
	partial class DisplayConfigLite
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DisplayConfigLite));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lblScanlines = new System.Windows.Forms.Label();
			this.lblUserFilterName = new System.Windows.Forms.Label();
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
			this.label12 = new System.Windows.Forms.Label();
			this.txtCustomARX = new System.Windows.Forms.TextBox();
			this.rbUseCustomRatio = new System.Windows.Forms.RadioButton();
			this.label4 = new System.Windows.Forms.Label();
			this.txtCustomARHeight = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.txtCustomARWidth = new System.Windows.Forms.TextBox();
			this.rbUseCustom = new System.Windows.Forms.RadioButton();
			this.label2 = new System.Windows.Forms.Label();
			this.checkSnowyNullEmulator = new System.Windows.Forms.CheckBox();
			this.rbOpenGL = new System.Windows.Forms.RadioButton();
			this.label5 = new System.Windows.Forms.Label();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tpAR = new System.Windows.Forms.TabPage();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.label16 = new System.Windows.Forms.Label();
			this.label15 = new System.Windows.Forms.Label();
			this.txtCropBottom = new System.Windows.Forms.TextBox();
			this.label17 = new System.Windows.Forms.Label();
			this.txtCropRight = new System.Windows.Forms.TextBox();
			this.txtCropTop = new System.Windows.Forms.TextBox();
			this.label14 = new System.Windows.Forms.Label();
			this.txtCropLeft = new System.Windows.Forms.TextBox();
			this.btnDefaults = new System.Windows.Forms.Button();
			this.cbAutoPrescale = new System.Windows.Forms.CheckBox();
			this.label11 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.nudPrescale = new System.Windows.Forms.NumericUpDown();
			this.tpDispMethod = new System.Windows.Forms.TabPage();
			this.label6 = new System.Windows.Forms.Label();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.label13 = new System.Windows.Forms.Label();
			this.cbAlternateVsync = new System.Windows.Forms.CheckBox();
			this.label8 = new System.Windows.Forms.Label();
			this.rbD3D9 = new System.Windows.Forms.RadioButton();
			this.label7 = new System.Windows.Forms.Label();
			this.rbGDIPlus = new System.Windows.Forms.RadioButton();
			this.tpMisc = new System.Windows.Forms.TabPage();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.rbDisplayAbsoluteZero = new System.Windows.Forms.RadioButton();
			this.rbDisplayMinimal = new System.Windows.Forms.RadioButton();
			this.rbDisplayFull = new System.Windows.Forms.RadioButton();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.cbAllowDoubleclickFullscreen = new System.Windows.Forms.CheckBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.cbFSAutohideMouse = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.cbFullscreenHacks = new System.Windows.Forms.CheckBox();
			this.cbStatusBarFullscreen = new System.Windows.Forms.CheckBox();
			this.cbMenuFullscreen = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lblFrameTypeWindowed = new System.Windows.Forms.Label();
			this.cbStatusBarWindowed = new System.Windows.Forms.CheckBox();
			this.label9 = new System.Windows.Forms.Label();
			this.cbMenuWindowed = new System.Windows.Forms.CheckBox();
			this.trackbarFrameSizeWindowed = new BizHawk.Client.EmuHawk.TransparentTrackBar();
			this.cbCaptionWindowed = new System.Windows.Forms.CheckBox();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.flpDispFeatures = new System.Windows.Forms.FlowLayoutPanel();
			this.flpCropBottomLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpCropRightLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpDispMethodTab = new System.Windows.Forms.FlowLayoutPanel();
			this.flpScalingTab = new System.Windows.Forms.FlowLayoutPanel();
			this.flpPrescaleLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpDispMethodRadios = new System.Windows.Forms.FlowLayoutPanel();
			this.flpD3DSuboptions = new System.Windows.Forms.FlowLayoutPanel();
			this.flpD3DAltVSyncLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpMiscTab = new System.Windows.Forms.FlowLayoutPanel();
			this.flpCropOptions = new System.Windows.Forms.FlowLayoutPanel();
			this.flpCropTopLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpCropLeftLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpScanlinesSliderLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpFinalFilterRadios = new System.Windows.Forms.FlowLayoutPanel();
			this.flpARSelection = new System.Windows.Forms.FlowLayoutPanel();
			this.flpWindowTab = new System.Windows.Forms.FlowLayoutPanel();
			this.flpWindowGroups = new System.Windows.Forms.FlowLayoutPanel();
			this.flpWindowed = new System.Windows.Forms.FlowLayoutPanel();
			this.flpWindowFrameLabel = new System.Windows.Forms.FlowLayoutPanel();
			this.flpFullscreen = new System.Windows.Forms.FlowLayoutPanel();
			this.flpFullscreenCheckboxes = new System.Windows.Forms.FlowLayoutPanel();
			this.tlpScalingFilter = new System.Windows.Forms.TableLayoutPanel();
			this.tlpNonSquareAR = new System.Windows.Forms.TableLayoutPanel();
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
			this.groupBox5.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackbarFrameSizeWindowed)).BeginInit();
			this.flpCropOptions.SuspendLayout();
			this.flpCropTopLabel.SuspendLayout();
			this.flpCropLeftLabel.SuspendLayout();
			this.flpScalingTab.SuspendLayout();
			this.flpPrescaleLabel.SuspendLayout();
			this.flpCropBottomLabel.SuspendLayout();
			this.flpCropRightLabel.SuspendLayout();
			this.flpScanlinesSliderLabel.SuspendLayout();
			this.flpDispMethodTab.SuspendLayout();
			this.flpDispMethodRadios.SuspendLayout();
			this.flpD3DSuboptions.SuspendLayout();
			this.flpD3DAltVSyncLabel.SuspendLayout();
			this.flpFinalFilterRadios.SuspendLayout();
			this.flpMiscTab.SuspendLayout();
			this.flpDispFeatures.SuspendLayout();
			this.flpWindowTab.SuspendLayout();
			this.flpWindowGroups.SuspendLayout();
			this.flpWindowed.SuspendLayout();
			this.flpWindowFrameLabel.SuspendLayout();
			this.flpFullscreen.SuspendLayout();
			this.flpFullscreenCheckboxes.SuspendLayout();
			this.flpARSelection.SuspendLayout();
			this.tlpScalingFilter.SuspendLayout();
			this.tlpNonSquareAR.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(489, 399);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(408, 399);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 4;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.AutoSize = true;
			this.groupBox1.Controls.Add(this.tlpScalingFilter);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(218, 179);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Scaling Filter";
			// 
			// lblScanlines
			// 
			this.lblScanlines.Anchor = System.Windows.Forms.AnchorStyles.Top;
			this.lblScanlines.AutoSize = true;
			this.lblScanlines.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblScanlines.Name = "lblScanlines";
			this.lblScanlines.Size = new System.Drawing.Size(15, 13);
			this.lblScanlines.TabIndex = 11;
			this.lblScanlines.Text = "%";
			// 
			// lblUserFilterName
			// 
			this.lblUserFilterName.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.tlpScalingFilter.SetColumnSpan(this.lblUserFilterName, 2);
			this.lblUserFilterName.Location = new System.Drawing.Point(3, 29);
			this.lblUserFilterName.Name = "lblUserFilterName";
			this.lblUserFilterName.Size = new System.Drawing.Size(120, 15);
			this.lblUserFilterName.TabIndex = 10;
			this.lblUserFilterName.Text = "Will contain user filter name";
			// 
			// btnSelectUserFilter
			// 
			this.btnSelectUserFilter.Name = "btnSelectUserFilter";
			this.btnSelectUserFilter.Size = new System.Drawing.Size(75, 23);
			this.btnSelectUserFilter.TabIndex = 5;
			this.btnSelectUserFilter.Text = "Select";
			this.btnSelectUserFilter.UseVisualStyleBackColor = true;
			this.btnSelectUserFilter.Click += new System.EventHandler(this.btnSelectUserFilter_Click);
			// 
			// rbUser
			// 
			this.rbUser.AutoSize = true;
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
			this.tbScanlineIntensity.Maximum = 256;
			this.tbScanlineIntensity.Name = "tbScanlineIntensity";
			this.tbScanlineIntensity.Size = new System.Drawing.Size(70, 30);
			this.tbScanlineIntensity.TabIndex = 3;
			this.tbScanlineIntensity.TickFrequency = 32;
			this.tbScanlineIntensity.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
			this.tbScanlineIntensity.Scroll += new System.EventHandler(this.tbScanlineIntensity_Scroll);
			this.tbScanlineIntensity.ValueChanged += new System.EventHandler(this.tbScanlineIntensity_Scroll);
			// 
			// rbNone
			// 
			this.rbNone.AutoSize = true;
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
			this.checkLetterbox.Name = "checkLetterbox";
			this.checkLetterbox.Size = new System.Drawing.Size(173, 17);
			this.checkLetterbox.TabIndex = 8;
			this.checkLetterbox.Text = "Maintain aspect ratio (letterbox)";
			this.checkLetterbox.UseVisualStyleBackColor = true;
			this.checkLetterbox.CheckedChanged += new System.EventHandler(this.checkLetterbox_CheckedChanged);
			// 
			// checkPadInteger
			// 
			this.checkPadInteger.AutoSize = true;
			this.checkPadInteger.Name = "checkPadInteger";
			this.checkPadInteger.Size = new System.Drawing.Size(250, 17);
			this.checkPadInteger.TabIndex = 9;
			this.checkPadInteger.Text = "Expand pixels by integers only (e.g. no 1.3333x)";
			this.checkPadInteger.UseVisualStyleBackColor = true;
			this.checkPadInteger.CheckedChanged += new System.EventHandler(this.checkPadInteger_CheckedChanged);
			// 
			// grpFinalFilter
			// 
			this.grpFinalFilter.AutoSize = true;
			this.grpFinalFilter.Controls.Add(this.flpFinalFilterRadios);
			this.grpFinalFilter.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.grpFinalFilter.Name = "grpFinalFilter";
			this.grpFinalFilter.TabIndex = 8;
			this.grpFinalFilter.TabStop = false;
			this.grpFinalFilter.Text = "Final Filter";
			// 
			// rbFinalFilterBicubic
			// 
			this.rbFinalFilterBicubic.AutoSize = true;
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
			this.rbUseRaw.Name = "rbUseRaw";
			this.rbUseRaw.Size = new System.Drawing.Size(240, 17);
			this.rbUseRaw.TabIndex = 11;
			this.rbUseRaw.TabStop = true;
			this.rbUseRaw.Text = "Use 1:1 pixel size (for crispness or debugging)";
			this.rbUseRaw.UseVisualStyleBackColor = true;
			this.rbUseRaw.CheckedChanged += new System.EventHandler(this.rbUseRaw_CheckedChanged);
			// 
			// rbUseSystem
			// 
			this.rbUseSystem.AutoSize = true;
			this.tlpNonSquareAR.SetColumnSpan(this.rbUseSystem, 4);
			this.rbUseSystem.Name = "rbUseSystem";
			this.rbUseSystem.Size = new System.Drawing.Size(167, 17);
			this.rbUseSystem.TabIndex = 12;
			this.rbUseSystem.TabStop = true;
			this.rbUseSystem.Text = "Use system\'s recommendation";
			this.rbUseSystem.UseVisualStyleBackColor = true;
			this.rbUseSystem.CheckedChanged += new System.EventHandler(this.rbUseSystem_CheckedChanged);
			// 
			// grpARSelection
			// 
			this.grpARSelection.AutoSize = true;
			this.grpARSelection.Controls.Add(this.flpARSelection);
			this.grpARSelection.Name = "grpARSelection";
			this.grpARSelection.Size = new System.Drawing.Size(64, 64);
			this.grpARSelection.TabIndex = 13;
			this.grpARSelection.TabStop = false;
			this.grpARSelection.Text = "Aspect Ratio Selection";
			// 
			// txtCustomARY
			// 
			this.txtCustomARY.Name = "txtCustomARY";
			this.txtCustomARY.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARY.TabIndex = 19;
			// 
			// label12
			// 
			this.label12.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label12.AutoSize = true;
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(10, 13);
			this.label12.TabIndex = 17;
			this.label12.Text = ":";
			// 
			// txtCustomARX
			// 
			this.txtCustomARX.Name = "txtCustomARX";
			this.txtCustomARX.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARX.TabIndex = 18;
			// 
			// rbUseCustomRatio
			// 
			this.rbUseCustomRatio.AutoSize = true;
			this.rbUseCustomRatio.Name = "rbUseCustomRatio";
			this.rbUseCustomRatio.Size = new System.Drawing.Size(102, 17);
			this.rbUseCustomRatio.TabIndex = 16;
			this.rbUseCustomRatio.TabStop = true;
			this.rbUseCustomRatio.Text = "Use custom AR:";
			this.rbUseCustomRatio.UseVisualStyleBackColor = true;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(257, 13);
			this.label4.TabIndex = 12;
			this.label4.Text = "Allow pixel distortion (e.g. 2x1 pixels, for better AR fit):";
			// 
			// txtCustomARHeight
			// 
			this.txtCustomARHeight.Name = "txtCustomARHeight";
			this.txtCustomARHeight.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARHeight.TabIndex = 15;
			// 
			// label3
			// 
			this.label3.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label3.AutoSize = true;
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(12, 13);
			this.label3.TabIndex = 12;
			this.label3.Text = "x";
			// 
			// txtCustomARWidth
			// 
			this.txtCustomARWidth.Name = "txtCustomARWidth";
			this.txtCustomARWidth.Size = new System.Drawing.Size(72, 20);
			this.txtCustomARWidth.TabIndex = 14;
			// 
			// rbUseCustom
			// 
			this.rbUseCustom.AutoSize = true;
			this.rbUseCustom.Name = "rbUseCustom";
			this.rbUseCustom.Size = new System.Drawing.Size(105, 17);
			this.rbUseCustom.TabIndex = 13;
			this.rbUseCustom.TabStop = true;
			this.rbUseCustom.Text = "Use custom size:";
			this.rbUseCustom.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(398, 27);
			this.label2.TabIndex = 17;
			this.label2.Text = "Some people think the white noise is a great idea, and some people don\'t. Disabli" +
    "ng this displays black instead.";
			// 
			// checkSnowyNullEmulator
			// 
			this.checkSnowyNullEmulator.AutoSize = true;
			this.checkSnowyNullEmulator.Name = "checkSnowyNullEmulator";
			this.checkSnowyNullEmulator.Size = new System.Drawing.Size(159, 17);
			this.checkSnowyNullEmulator.TabIndex = 16;
			this.checkSnowyNullEmulator.Text = "Enable Snowy Null Emulator";
			this.checkSnowyNullEmulator.UseVisualStyleBackColor = true;
			// 
			// rbOpenGL
			// 
			this.rbOpenGL.AutoSize = true;
			this.rbOpenGL.Checked = true;
			this.rbOpenGL.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.rbOpenGL.Name = "rbOpenGL";
			this.rbOpenGL.Size = new System.Drawing.Size(65, 17);
			this.rbOpenGL.TabIndex = 3;
			this.rbOpenGL.TabStop = true;
			this.rbOpenGL.Text = "OpenGL";
			this.rbOpenGL.UseVisualStyleBackColor = true;
			// 
			// label5
			// 
			this.label5.Margin = new System.Windows.Forms.Padding(24, 0, 0, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(359, 47);
			this.label5.TabIndex = 16;
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
			this.tabControl1.Size = new System.Drawing.Size(552, 377);
			this.tabControl1.TabIndex = 17;
			// 
			// tpAR
			// 
			this.tpAR.Controls.Add(this.flpScalingTab);
			this.tpAR.Controls.Add(this.linkLabel1);
			this.tpAR.Name = "tpAR";
			this.tpAR.Padding = new System.Windows.Forms.Padding(3);
			this.tpAR.Size = new System.Drawing.Size(542, 351);
			this.tpAR.TabIndex = 0;
			this.tpAR.Text = "Scaling && Filtering";
			this.tpAR.UseVisualStyleBackColor = true;
			// 
			// groupBox6
			// 
			this.groupBox6.AutoSize = true;
			this.groupBox6.Controls.Add(this.flpCropOptions);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.TabIndex = 9;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Cropping";
			// 
			// label16
			// 
			this.label16.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label16.AutoSize = true;
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(43, 13);
			this.label16.TabIndex = 30;
			this.label16.Text = "Bottom:";
			// 
			// label15
			// 
			this.label15.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label15.AutoSize = true;
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(29, 13);
			this.label15.TabIndex = 29;
			this.label15.Text = "Top:";
			// 
			// txtCropBottom
			// 
			this.txtCropBottom.Margin = new System.Windows.Forms.Padding(0);
			this.txtCropBottom.Name = "txtCropBottom";
			this.txtCropBottom.Size = new System.Drawing.Size(34, 20);
			this.txtCropBottom.TabIndex = 28;
			this.txtCropBottom.Text = "8000";
			// 
			// label17
			// 
			this.label17.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label17.AutoSize = true;
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(35, 13);
			this.label17.TabIndex = 26;
			this.label17.Text = "Right:";
			// 
			// txtCropRight
			// 
			this.txtCropRight.Margin = new System.Windows.Forms.Padding(0);
			this.txtCropRight.Name = "txtCropRight";
			this.txtCropRight.Size = new System.Drawing.Size(34, 20);
			this.txtCropRight.TabIndex = 25;
			this.txtCropRight.Text = "8000";
			// 
			// txtCropTop
			// 
			this.txtCropTop.Margin = new System.Windows.Forms.Padding(0);
			this.txtCropTop.Name = "txtCropTop";
			this.txtCropTop.Size = new System.Drawing.Size(34, 20);
			this.txtCropTop.TabIndex = 24;
			this.txtCropTop.Text = "8000";
			// 
			// label14
			// 
			this.label14.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label14.AutoSize = true;
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(28, 13);
			this.label14.TabIndex = 16;
			this.label14.Text = "Left:";
			// 
			// txtCropLeft
			// 
			this.txtCropLeft.Margin = new System.Windows.Forms.Padding(0);
			this.txtCropLeft.Name = "txtCropLeft";
			this.txtCropLeft.Size = new System.Drawing.Size(34, 20);
			this.txtCropLeft.TabIndex = 15;
			this.txtCropLeft.Text = "8000";
			// 
			// btnDefaults
			// 
			this.btnDefaults.Location = new System.Drawing.Point(454, 275);
			this.btnDefaults.Name = "btnDefaults";
			this.btnDefaults.Size = new System.Drawing.Size(75, 23);
			this.btnDefaults.TabIndex = 18;
			this.btnDefaults.Text = "Defaults";
			this.toolTip1.SetToolTip(this.btnDefaults, "Unless I forgot to update the button\'s code when I changed a default");
			this.btnDefaults.UseVisualStyleBackColor = true;
			this.btnDefaults.Click += new System.EventHandler(this.btnDefaults_Click);
			// 
			// cbAutoPrescale
			// 
			this.cbAutoPrescale.AutoSize = true;
			this.cbAutoPrescale.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.cbAutoPrescale.Name = "cbAutoPrescale";
			this.cbAutoPrescale.Size = new System.Drawing.Size(92, 17);
			this.cbAutoPrescale.TabIndex = 17;
			this.cbAutoPrescale.Text = "Auto Prescale";
			this.cbAutoPrescale.UseVisualStyleBackColor = true;
			// 
			// label11
			// 
			this.label11.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label11.AutoSize = true;
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(14, 13);
			this.label11.TabIndex = 16;
			this.label11.Text = "X";
			// 
			// label10
			// 
			this.label10.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.label10.AutoSize = true;
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(76, 13);
			this.label10.TabIndex = 15;
			this.label10.Text = "User Prescale:";
			// 
			// nudPrescale
			// 
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
			this.tpDispMethod.Controls.Add(this.flpDispMethodTab);
			this.tpDispMethod.Name = "tpDispMethod";
			this.tpDispMethod.Size = new System.Drawing.Size(542, 351);
			this.tpDispMethod.TabIndex = 2;
			this.tpDispMethod.Text = "Display Method";
			this.tpDispMethod.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(359, 23);
			this.label6.TabIndex = 18;
			this.label6.Text = "Changes require restart of program to take effect.\r\n";
			// 
			// groupBox3
			// 
			this.groupBox3.AutoSize = true;
			this.groupBox3.Controls.Add(this.flpDispMethodRadios);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(410, 260);
			this.groupBox3.TabIndex = 16;
			this.groupBox3.TabStop = false;
			// 
			// label13
			// 
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(359, 43);
			this.label13.TabIndex = 22;
			this.label13.Text = resources.GetString("label13.Text");
			this.label13.Click += new System.EventHandler(this.label13_Click);
			this.label13.DoubleClick += new System.EventHandler(this.label13_Click);
			// 
			// cbAlternateVsync
			// 
			this.cbAlternateVsync.AutoSize = true;
			this.cbAlternateVsync.Name = "cbAlternateVsync";
			this.cbAlternateVsync.Size = new System.Drawing.Size(15, 14);
			this.cbAlternateVsync.TabIndex = 21;
			this.cbAlternateVsync.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(359, 27);
			this.label8.TabIndex = 20;
			this.label8.Text = " • Best compatibility\r\n • May have trouble with OpenGL-based cores (N64)\r\n";
			// 
			// rbD3D9
			// 
			this.rbD3D9.AutoSize = true;
			this.rbD3D9.Checked = true;
			this.rbD3D9.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.rbD3D9.Name = "rbD3D9";
			this.rbD3D9.Size = new System.Drawing.Size(73, 17);
			this.rbD3D9.TabIndex = 19;
			this.rbD3D9.TabStop = true;
			this.rbD3D9.Text = "Direct3D9";
			this.rbD3D9.UseVisualStyleBackColor = true;
			// 
			// label7
			// 
			this.label7.Margin = new System.Windows.Forms.Padding(24, 0, 0, 0);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(359, 47);
			this.label7.TabIndex = 18;
			this.label7.Text = " • Slow; Mainly for compatibility purposes\r\n • Missing many features\r\n • Works be" +
    "tter over Remote Desktop, etc.\r\n";
			// 
			// rbGDIPlus
			// 
			this.rbGDIPlus.AutoSize = true;
			this.rbGDIPlus.Checked = true;
			this.rbGDIPlus.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.rbGDIPlus.Name = "rbGDIPlus";
			this.rbGDIPlus.Size = new System.Drawing.Size(50, 17);
			this.rbGDIPlus.TabIndex = 17;
			this.rbGDIPlus.TabStop = true;
			this.rbGDIPlus.Text = "GDI+";
			this.rbGDIPlus.UseVisualStyleBackColor = true;
			// 
			// tpMisc
			// 
			this.tpMisc.Controls.Add(this.flpMiscTab);
			this.tpMisc.Name = "tpMisc";
			this.tpMisc.Size = new System.Drawing.Size(542, 351);
			this.tpMisc.TabIndex = 3;
			this.tpMisc.Text = "Misc";
			this.tpMisc.UseVisualStyleBackColor = true;
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.flpDispFeatures);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(371, 96);
			this.groupBox5.TabIndex = 20;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Display Features (for speeding up replays)";
			// 
			// rbDisplayAbsoluteZero
			// 
			this.rbDisplayAbsoluteZero.AutoSize = true;
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
			this.rbDisplayFull.Name = "rbDisplayFull";
			this.rbDisplayFull.Size = new System.Drawing.Size(137, 17);
			this.rbDisplayFull.TabIndex = 0;
			this.rbDisplayFull.TabStop = true;
			this.rbDisplayFull.Text = "Full - Display Everything";
			this.rbDisplayFull.UseVisualStyleBackColor = true;
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.flpWindowTab);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(542, 351);
			this.tabPage1.TabIndex = 4;
			this.tabPage1.Text = "Window";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// cbAllowDoubleclickFullscreen
			// 
			this.cbAllowDoubleclickFullscreen.AutoSize = true;
			this.cbAllowDoubleclickFullscreen.Name = "cbAllowDoubleclickFullscreen";
			this.cbAllowDoubleclickFullscreen.Size = new System.Drawing.Size(471, 17);
			this.cbAllowDoubleclickFullscreen.TabIndex = 27;
			this.cbAllowDoubleclickFullscreen.Text = "Allow Double-Click Fullscreen (hold shift to force fullscreen to toggle in case u" +
    "sing zapper, etc.)";
			this.cbAllowDoubleclickFullscreen.UseVisualStyleBackColor = true;
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.flpFullscreen);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(266, 211);
			this.groupBox4.TabIndex = 27;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Fullscreen";
			// 
			// cbFSAutohideMouse
			// 
			this.cbFSAutohideMouse.AutoSize = true;
			this.cbFSAutohideMouse.Name = "cbFSAutohideMouse";
			this.cbFSAutohideMouse.Size = new System.Drawing.Size(141, 17);
			this.cbFSAutohideMouse.TabIndex = 28;
			this.cbFSAutohideMouse.Text = "Auto-Hide Mouse Cursor";
			this.cbFSAutohideMouse.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(240, 115);
			this.label1.TabIndex = 27;
			this.label1.Text = resources.GetString("label1.Text");
			// 
			// cbFullscreenHacks
			// 
			this.cbFullscreenHacks.AutoSize = true;
			this.cbFullscreenHacks.Name = "cbFullscreenHacks";
			this.cbFullscreenHacks.Size = new System.Drawing.Size(191, 17);
			this.cbFullscreenHacks.TabIndex = 26;
			this.cbFullscreenHacks.Text = "Enable Windows Fullscreen Hacks";
			this.cbFullscreenHacks.UseVisualStyleBackColor = true;
			// 
			// cbStatusBarFullscreen
			// 
			this.cbStatusBarFullscreen.AutoSize = true;
			this.cbStatusBarFullscreen.Name = "cbStatusBarFullscreen";
			this.cbStatusBarFullscreen.Size = new System.Drawing.Size(75, 17);
			this.cbStatusBarFullscreen.TabIndex = 23;
			this.cbStatusBarFullscreen.Text = "Status Bar";
			this.cbStatusBarFullscreen.UseVisualStyleBackColor = true;
			// 
			// cbMenuFullscreen
			// 
			this.cbMenuFullscreen.AutoSize = true;
			this.cbMenuFullscreen.Name = "cbMenuFullscreen";
			this.cbMenuFullscreen.Size = new System.Drawing.Size(53, 17);
			this.cbMenuFullscreen.TabIndex = 25;
			this.cbMenuFullscreen.Text = "Menu";
			this.cbMenuFullscreen.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.flpWindowed);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(131, 211);
			this.groupBox2.TabIndex = 26;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Windowed";
			// 
			// lblFrameTypeWindowed
			// 
			this.lblFrameTypeWindowed.AutoSize = true;
			this.lblFrameTypeWindowed.Name = "lblFrameTypeWindowed";
			this.lblFrameTypeWindowed.Size = new System.Drawing.Size(62, 13);
			this.lblFrameTypeWindowed.TabIndex = 26;
			this.lblFrameTypeWindowed.Text = "(frame type)";
			// 
			// cbStatusBarWindowed
			// 
			this.cbStatusBarWindowed.AutoSize = true;
			this.cbStatusBarWindowed.Name = "cbStatusBarWindowed";
			this.cbStatusBarWindowed.Size = new System.Drawing.Size(75, 17);
			this.cbStatusBarWindowed.TabIndex = 23;
			this.cbStatusBarWindowed.Text = "Status Bar";
			this.cbStatusBarWindowed.UseVisualStyleBackColor = true;
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(39, 13);
			this.label9.TabIndex = 22;
			this.label9.Text = "Frame:";
			// 
			// cbMenuWindowed
			// 
			this.cbMenuWindowed.AutoSize = true;
			this.cbMenuWindowed.Name = "cbMenuWindowed";
			this.cbMenuWindowed.Size = new System.Drawing.Size(53, 17);
			this.cbMenuWindowed.TabIndex = 25;
			this.cbMenuWindowed.Text = "Menu";
			this.cbMenuWindowed.UseVisualStyleBackColor = true;
			// 
			// trackbarFrameSizeWindowed
			// 
			this.trackbarFrameSizeWindowed.LargeChange = 1;
			this.trackbarFrameSizeWindowed.Maximum = 2;
			this.trackbarFrameSizeWindowed.Name = "trackbarFrameSizeWindowed";
			this.trackbarFrameSizeWindowed.Size = new System.Drawing.Size(113, 45);
			this.trackbarFrameSizeWindowed.TabIndex = 21;
			this.trackbarFrameSizeWindowed.Value = 1;
			this.trackbarFrameSizeWindowed.ValueChanged += new System.EventHandler(this.trackbarFrameSizeWindowed_ValueChanged);
			// 
			// cbCaptionWindowed
			// 
			this.cbCaptionWindowed.AutoSize = true;
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
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// flpScanlinesSliderLabel
			// 
			this.flpScanlinesSliderLabel.AutoSize = true;
			this.flpScanlinesSliderLabel.Controls.Add(this.lblScanlines);
			this.flpScanlinesSliderLabel.Controls.Add(this.tbScanlineIntensity);
			this.flpScanlinesSliderLabel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpScanlinesSliderLabel.Location = new System.Drawing.Point(80, 49);
			this.flpScanlinesSliderLabel.Name = "flpScanlinesSliderLabel";
			this.tlpScalingFilter.SetRowSpan(this.flpScanlinesSliderLabel, 2);
			this.flpScanlinesSliderLabel.Size = new System.Drawing.Size(76, 64);
			this.flpScanlinesSliderLabel.WrapContents = false;
			// 
			// flpFinalFilterRadios
			// 
			this.flpFinalFilterRadios.AutoSize = true;
			this.flpFinalFilterRadios.Controls.Add(this.rbFinalFilterNone);
			this.flpFinalFilterRadios.Controls.Add(this.rbFinalFilterBilinear);
			this.flpFinalFilterRadios.Controls.Add(this.rbFinalFilterBicubic);
			this.flpFinalFilterRadios.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpFinalFilterRadios.Location = new System.Drawing.Point(3, 17);
			this.flpFinalFilterRadios.Name = "flpFinalFilterRadios";
			this.flpFinalFilterRadios.Size = new System.Drawing.Size(50, 50);
			this.flpFinalFilterRadios.WrapContents = false;
			// 
			// flpARSelection
			// 
			this.flpARSelection.AutoSize = true;
			this.flpARSelection.Controls.Add(this.rbUseRaw);
			this.flpARSelection.Controls.Add(this.label4);
			this.flpARSelection.Controls.Add(this.tlpNonSquareAR);
			this.flpARSelection.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpARSelection.Location = new System.Drawing.Point(3, 17);
			this.flpARSelection.Name = "flpARSelection";
			this.flpARSelection.Size = new System.Drawing.Size(291, 117);
			this.flpARSelection.WrapContents = false;
			// 
			// flpCropOptions
			// 
			this.flpCropOptions.AutoSize = true;
			this.flpCropOptions.Controls.Add(this.flpCropLeftLabel);
			this.flpCropOptions.Controls.Add(this.flpCropTopLabel);
			this.flpCropOptions.Controls.Add(this.flpCropRightLabel);
			this.flpCropOptions.Controls.Add(this.flpCropBottomLabel);
			this.flpCropOptions.Location = new System.Drawing.Point(3, 17);
			this.flpCropOptions.Name = "flpCropOptions";
			this.flpCropOptions.Size = new System.Drawing.Size(295, 20);
			// 
			// flpCropTopLabel
			// 
			this.flpCropTopLabel.AutoSize = true;
			this.flpCropTopLabel.Controls.Add(this.label15);
			this.flpCropTopLabel.Controls.Add(this.txtCropTop);
			this.flpCropTopLabel.Margin = new System.Windows.Forms.Padding(0);
			this.flpCropTopLabel.Name = "flpCropTopLabel";
			this.flpCropTopLabel.Size = new System.Drawing.Size(69, 20);
			// 
			// flpScalingTab
			// 
			this.flpScalingTab.Controls.Add(this.flpPrescaleLabel);
			this.flpScalingTab.Controls.Add(this.groupBox1);
			this.flpScalingTab.Controls.Add(this.cbAutoPrescale);
			this.flpScalingTab.Controls.Add(this.grpFinalFilter);
			this.flpScalingTab.Controls.Add(this.checkLetterbox);
			this.flpScalingTab.Controls.Add(this.grpARSelection);
			this.flpScalingTab.Controls.Add(this.checkPadInteger);
			this.flpScalingTab.Controls.Add(this.groupBox6);
			this.flpScalingTab.Controls.Add(this.btnDefaults);
			this.flpScalingTab.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpScalingTab.Name = "flpScalingTab";
			this.flpScalingTab.SetFlowBreak(this.grpFinalFilter, true);
			this.flpScalingTab.Size = new System.Drawing.Size(535, 370);
			// 
			// flpPrescaleLabel
			// 
			this.flpPrescaleLabel.AutoSize = true;
			this.flpPrescaleLabel.Controls.Add(this.label10);
			this.flpPrescaleLabel.Controls.Add(this.nudPrescale);
			this.flpPrescaleLabel.Controls.Add(this.label11);
			this.flpPrescaleLabel.Location = new System.Drawing.Point(3, 3);
			this.flpPrescaleLabel.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
			this.flpPrescaleLabel.Name = "flpPrescaleLabel";
			this.flpPrescaleLabel.Size = new System.Drawing.Size(153, 26);
			this.flpPrescaleLabel.WrapContents = false;
			// 
			// flpD3DSuboptions
			// 
			this.flpD3DSuboptions.AutoSize = true;
			this.flpD3DSuboptions.Controls.Add(this.label8);
			this.flpD3DSuboptions.Controls.Add(this.flpD3DAltVSyncLabel);
			this.flpD3DSuboptions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpD3DSuboptions.Location = new System.Drawing.Point(3, 20);
			this.flpD3DSuboptions.Margin = new System.Windows.Forms.Padding(24, 0, 0, 0);
			this.flpD3DSuboptions.Name = "flpD3DSuboptions";
			this.flpD3DSuboptions.Size = new System.Drawing.Size(392, 76);
			this.flpD3DSuboptions.WrapContents = false;
			// 
			// flpDispMethodRadios
			// 
			this.flpDispMethodRadios.AutoSize = true;
			this.flpDispMethodRadios.Controls.Add(this.rbD3D9);
			this.flpDispMethodRadios.Controls.Add(this.flpD3DSuboptions);
			this.flpDispMethodRadios.Controls.Add(this.rbOpenGL);
			this.flpDispMethodRadios.Controls.Add(this.label5);
			this.flpDispMethodRadios.Controls.Add(this.rbGDIPlus);
			this.flpDispMethodRadios.Controls.Add(this.label7);
			this.flpDispMethodRadios.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpDispMethodRadios.Location = new System.Drawing.Point(6, 13);
			this.flpDispMethodRadios.Name = "flpDispMethodRadios";
			this.flpDispMethodRadios.Size = new System.Drawing.Size(398, 228);
			// 
			// flpDispMethodTab
			// 
			this.flpDispMethodTab.AutoSize = true;
			this.flpDispMethodTab.Controls.Add(this.groupBox3);
			this.flpDispMethodTab.Controls.Add(this.label6);
			this.flpDispMethodTab.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpDispMethodTab.Name = "flpDispMethodTab";
			this.flpDispMethodTab.Size = new System.Drawing.Size(416, 306);
			// 
			// flpCropBottomLabel
			// 
			this.flpCropBottomLabel.AutoSize = true;
			this.flpCropBottomLabel.Controls.Add(this.label16);
			this.flpCropBottomLabel.Controls.Add(this.txtCropBottom);
			this.flpCropBottomLabel.Location = new System.Drawing.Point(137, 0);
			this.flpCropBottomLabel.Margin = new System.Windows.Forms.Padding(0);
			this.flpCropBottomLabel.Name = "flpCropBottomLabel";
			this.flpCropBottomLabel.Size = new System.Drawing.Size(83, 20);
			// 
			// flpCropRightLabel
			// 
			this.flpCropRightLabel.AutoSize = true;
			this.flpCropRightLabel.Controls.Add(this.label17);
			this.flpCropRightLabel.Controls.Add(this.txtCropRight);
			this.flpCropRightLabel.Location = new System.Drawing.Point(220, 0);
			this.flpCropRightLabel.Margin = new System.Windows.Forms.Padding(0);
			this.flpCropRightLabel.Name = "flpCropRightLabel";
			this.flpCropRightLabel.Size = new System.Drawing.Size(75, 20);
			// 
			// flpCropLeftLabel
			// 
			this.flpCropLeftLabel.AutoSize = true;
			this.flpCropLeftLabel.Controls.Add(this.label14);
			this.flpCropLeftLabel.Controls.Add(this.txtCropLeft);
			this.flpCropLeftLabel.Location = new System.Drawing.Point(69, 0);
			this.flpCropLeftLabel.Margin = new System.Windows.Forms.Padding(0);
			this.flpCropLeftLabel.Name = "flpCropLeftLabel";
			this.flpCropLeftLabel.Size = new System.Drawing.Size(68, 20);
			// 
			// flpD3DAltVSyncLabel
			// 
			this.flpD3DAltVSyncLabel.AutoSize = true;
			this.flpD3DAltVSyncLabel.Controls.Add(this.cbAlternateVsync);
			this.flpD3DAltVSyncLabel.Controls.Add(this.label13);
			this.flpD3DAltVSyncLabel.Location = new System.Drawing.Point(3, 30);
			this.flpD3DAltVSyncLabel.Name = "flpD3DAltVSyncLabel";
			this.flpD3DAltVSyncLabel.Size = new System.Drawing.Size(386, 43);
			this.flpD3DAltVSyncLabel.WrapContents = false;
			// 
			// flpWindowed
			// 
			this.flpWindowed.Controls.Add(this.flpWindowFrameLabel);
			this.flpWindowed.Controls.Add(this.trackbarFrameSizeWindowed);
			this.flpWindowed.Controls.Add(this.cbStatusBarWindowed);
			this.flpWindowed.Controls.Add(this.cbCaptionWindowed);
			this.flpWindowed.Controls.Add(this.cbMenuWindowed);
			this.flpWindowed.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flpWindowed.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpWindowed.Location = new System.Drawing.Point(3, 16);
			this.flpWindowed.Name = "flpWindowed";
			this.flpWindowed.Size = new System.Drawing.Size(125, 192);
			// 
			// flpWindowFrameLabel
			// 
			this.flpWindowFrameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
			this.flpWindowFrameLabel.AutoSize = true;
			this.flpWindowFrameLabel.Controls.Add(this.label9);
			this.flpWindowFrameLabel.Controls.Add(this.lblFrameTypeWindowed);
			this.flpWindowFrameLabel.Location = new System.Drawing.Point(3, 3);
			this.flpWindowFrameLabel.Name = "flpWindowFrameLabel";
			this.flpWindowFrameLabel.Size = new System.Drawing.Size(113, 13);
			// 
			// flpMiscTab
			// 
			this.flpMiscTab.AutoSize = true;
			this.flpMiscTab.Controls.Add(this.groupBox5);
			this.flpMiscTab.Controls.Add(this.checkSnowyNullEmulator);
			this.flpMiscTab.Controls.Add(this.label2);
			this.flpMiscTab.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpMiscTab.Location = new System.Drawing.Point(3, 3);
			this.flpMiscTab.Name = "flpMiscTab";
			this.flpMiscTab.Size = new System.Drawing.Size(404, 168);
			// 
			// flpDispFeatures
			// 
			this.flpDispFeatures.Controls.Add(this.rbDisplayFull);
			this.flpDispFeatures.Controls.Add(this.rbDisplayMinimal);
			this.flpDispFeatures.Controls.Add(this.rbDisplayAbsoluteZero);
			this.flpDispFeatures.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flpDispFeatures.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpDispFeatures.Location = new System.Drawing.Point(3, 16);
			this.flpDispFeatures.Name = "flpDispFeatures";
			this.flpDispFeatures.Size = new System.Drawing.Size(365, 77);
			this.flpDispFeatures.WrapContents = false;
			// 
			// flpWindowTab
			// 
			this.flpWindowTab.Controls.Add(this.flpWindowGroups);
			this.flpWindowTab.Controls.Add(this.cbAllowDoubleclickFullscreen);
			this.flpWindowTab.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flpWindowTab.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpWindowTab.Location = new System.Drawing.Point(3, 3);
			this.flpWindowTab.Name = "flpWindowTab";
			this.flpWindowTab.Size = new System.Drawing.Size(536, 375);
			// 
			// flpWindowGroups
			// 
			this.flpWindowGroups.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
			this.flpWindowGroups.AutoSize = true;
			this.flpWindowGroups.Controls.Add(this.groupBox2);
			this.flpWindowGroups.Controls.Add(this.groupBox4);
			this.flpWindowGroups.Location = new System.Drawing.Point(3, 3);
			this.flpWindowGroups.Name = "flpWindowGroups";
			this.flpWindowGroups.Size = new System.Drawing.Size(471, 217);
			// 
			// flpFullscreen
			// 
			this.flpFullscreen.Controls.Add(this.flpFullscreenCheckboxes);
			this.flpFullscreen.Controls.Add(this.label1);
			this.flpFullscreen.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flpFullscreen.Location = new System.Drawing.Point(3, 16);
			this.flpFullscreen.Name = "flpFullscreen";
			this.flpFullscreen.Size = new System.Drawing.Size(260, 192);
			// 
			// flpFullscreenCheckboxes
			// 
			this.flpFullscreenCheckboxes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
			this.flpFullscreenCheckboxes.AutoSize = true;
			this.flpFullscreenCheckboxes.Controls.Add(this.cbStatusBarFullscreen);
			this.flpFullscreenCheckboxes.Controls.Add(this.cbFSAutohideMouse);
			this.flpFullscreenCheckboxes.Controls.Add(this.cbMenuFullscreen);
			this.flpFullscreenCheckboxes.Controls.Add(this.cbFullscreenHacks);
			this.flpFullscreenCheckboxes.Location = new System.Drawing.Point(3, 3);
			this.flpFullscreenCheckboxes.Name = "flpFullscreenCheckboxes";
			this.flpFullscreenCheckboxes.Size = new System.Drawing.Size(228, 69);
			// 
			// tlpScalingFilter
			// 
			this.tlpScalingFilter.AutoSize = true;
			this.tlpScalingFilter.ColumnCount = 2;
			this.tlpScalingFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpScalingFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpScalingFilter.Controls.Add(this.rbNone, 0, 0);
			this.tlpScalingFilter.Controls.Add(this.rbHq2x, 0, 1);
			this.tlpScalingFilter.Controls.Add(this.flpScanlinesSliderLabel, 1, 1);
			this.tlpScalingFilter.Controls.Add(this.rbScanlines, 0, 2);
			this.tlpScalingFilter.Controls.Add(this.rbUser, 0, 3);
			this.tlpScalingFilter.Controls.Add(this.btnSelectUserFilter, 1, 3);
			this.tlpScalingFilter.Controls.Add(this.lblUserFilterName, 0, 4);
			this.tlpScalingFilter.Location = new System.Drawing.Point(3, 17);
			this.tlpScalingFilter.Name = "tlpScalingFilter";
			this.tlpScalingFilter.RowCount = 4;
			this.tlpScalingFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpScalingFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpScalingFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpScalingFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpScalingFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpScalingFilter.Size = new System.Drawing.Size(209, 143);
			// 
			// tlpNonSquareAR
			// 
			this.tlpNonSquareAR.AutoSize = true;
			this.tlpNonSquareAR.ColumnCount = 4;
			this.tlpNonSquareAR.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpNonSquareAR.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpNonSquareAR.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpNonSquareAR.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpNonSquareAR.Controls.Add(this.rbUseCustomRatio, 0, 2);
			this.tlpNonSquareAR.Controls.Add(this.rbUseSystem, 0, 0);
			this.tlpNonSquareAR.Controls.Add(this.txtCustomARX, 1, 2);
			this.tlpNonSquareAR.Controls.Add(this.label12, 2, 2);
			this.tlpNonSquareAR.Controls.Add(this.rbUseCustom, 0, 1);
			this.tlpNonSquareAR.Controls.Add(this.txtCustomARWidth, 1, 1);
			this.tlpNonSquareAR.Controls.Add(this.label3, 2, 1);
			this.tlpNonSquareAR.Controls.Add(this.txtCustomARY, 3, 2);
			this.tlpNonSquareAR.Controls.Add(this.txtCustomARHeight, 3, 1);
			this.tlpNonSquareAR.Location = new System.Drawing.Point(3, 39);
			this.tlpNonSquareAR.Name = "tlpNonSquareAR";
			this.tlpNonSquareAR.RowCount = 3;
			this.tlpNonSquareAR.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpNonSquareAR.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpNonSquareAR.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpNonSquareAR.Size = new System.Drawing.Size(285, 75);
			// 
			// DisplayConfigLite
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(580, 434);
			this.Controls.Add(this.tabControl1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "DisplayConfigLite";
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
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackbarFrameSizeWindowed)).EndInit();
			this.flpScalingTab.ResumeLayout(false);
			this.flpScalingTab.PerformLayout();
			this.flpPrescaleLabel.ResumeLayout(false);
			this.flpPrescaleLabel.PerformLayout();
			this.flpCropOptions.ResumeLayout(false);
			this.flpCropOptions.PerformLayout();
			this.flpCropTopLabel.ResumeLayout(false);
			this.flpCropTopLabel.PerformLayout();
			this.flpCropLeftLabel.ResumeLayout(false);
			this.flpCropLeftLabel.PerformLayout();
			this.flpCropBottomLabel.ResumeLayout(false);
			this.flpCropBottomLabel.PerformLayout();
			this.flpCropRightLabel.ResumeLayout(false);
			this.flpCropRightLabel.PerformLayout();
			this.flpDispMethodTab.ResumeLayout(false);
			this.flpDispMethodTab.PerformLayout();
			this.flpDispMethodRadios.ResumeLayout(false);
			this.flpDispMethodRadios.PerformLayout();
			this.flpD3DSuboptions.ResumeLayout(false);
			this.flpD3DSuboptions.PerformLayout();
			this.flpD3DAltVSyncLabel.ResumeLayout(false);
			this.flpD3DAltVSyncLabel.PerformLayout();
			this.flpMiscTab.ResumeLayout(false);
			this.flpMiscTab.PerformLayout();
			this.flpDispFeatures.ResumeLayout(false);
			this.flpDispFeatures.PerformLayout();
			this.flpWindowTab.ResumeLayout(false);
			this.flpWindowTab.PerformLayout();
			this.flpWindowGroups.ResumeLayout(false);
			this.flpWindowed.ResumeLayout(false);
			this.flpWindowed.PerformLayout();
			this.flpWindowFrameLabel.ResumeLayout(false);
			this.flpWindowFrameLabel.PerformLayout();
			this.flpFullscreen.ResumeLayout(false);
			this.flpFullscreen.PerformLayout();
			this.flpFullscreenCheckboxes.ResumeLayout(false);
			this.flpFullscreenCheckboxes.PerformLayout();
			this.flpScanlinesSliderLabel.ResumeLayout(false);
			this.flpScanlinesSliderLabel.PerformLayout();
			this.flpFinalFilterRadios.ResumeLayout(false);
			this.flpFinalFilterRadios.PerformLayout();
			this.flpARSelection.ResumeLayout(false);
			this.flpARSelection.PerformLayout();
			this.tlpNonSquareAR.ResumeLayout(false);
			this.tlpNonSquareAR.PerformLayout();
			this.tlpScalingFilter.ResumeLayout(false);
			this.tlpScalingFilter.PerformLayout();
			this.ResumeLayout(false);

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
		private System.Windows.Forms.Label lblUserFilterName;
		private System.Windows.Forms.RadioButton rbUseRaw;
		private System.Windows.Forms.RadioButton rbUseSystem;
		private System.Windows.Forms.GroupBox grpARSelection;
		private System.Windows.Forms.CheckBox checkSnowyNullEmulator;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label lblScanlines;
		private System.Windows.Forms.TextBox txtCustomARHeight;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox txtCustomARWidth;
		private System.Windows.Forms.RadioButton rbUseCustom;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.RadioButton rbOpenGL;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TabControl tabControl1;
		private System.Windows.Forms.TabPage tpAR;
		private System.Windows.Forms.TabPage tpDispMethod;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.RadioButton rbGDIPlus;
		private System.Windows.Forms.TabPage tpMisc;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.RadioButton rbD3D9;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.CheckBox cbStatusBarWindowed;
		private System.Windows.Forms.Label label9;
		private BizHawk.Client.EmuHawk.TransparentTrackBar trackbarFrameSizeWindowed;
		private System.Windows.Forms.CheckBox cbMenuWindowed;
		private System.Windows.Forms.CheckBox cbCaptionWindowed;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.CheckBox cbStatusBarFullscreen;
		private System.Windows.Forms.CheckBox cbMenuFullscreen;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label lblFrameTypeWindowed;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label10;
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
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox txtCustomARX;
		private System.Windows.Forms.CheckBox cbAutoPrescale;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.CheckBox cbAlternateVsync;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox cbFullscreenHacks;
		private System.Windows.Forms.Button btnDefaults;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.TextBox txtCropBottom;
		private System.Windows.Forms.Label label17;
		private System.Windows.Forms.TextBox txtCropRight;
		private System.Windows.Forms.TextBox txtCropTop;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.TextBox txtCropLeft;
		private System.Windows.Forms.FlowLayoutPanel flpDispMethodTab;
		private System.Windows.Forms.FlowLayoutPanel flpDispMethodRadios;
		private System.Windows.Forms.FlowLayoutPanel flpFinalFilterRadios;
		private System.Windows.Forms.FlowLayoutPanel flpPrescaleLabel;
		private System.Windows.Forms.FlowLayoutPanel flpFullscreen;
		private System.Windows.Forms.FlowLayoutPanel flpFullscreenCheckboxes;
		private System.Windows.Forms.FlowLayoutPanel flpMiscTab;
		private System.Windows.Forms.FlowLayoutPanel flpDispFeatures;
		private System.Windows.Forms.FlowLayoutPanel flpCropOptions;
		private System.Windows.Forms.FlowLayoutPanel flpWindowed;
		private System.Windows.Forms.FlowLayoutPanel flpWindowFrameLabel;
		private System.Windows.Forms.FlowLayoutPanel flpCropTopLabel;
		private System.Windows.Forms.FlowLayoutPanel flpCropLeftLabel;
		private System.Windows.Forms.FlowLayoutPanel flpD3DSuboptions;
		private System.Windows.Forms.FlowLayoutPanel flpD3DAltVSyncLabel;
		private System.Windows.Forms.FlowLayoutPanel flpWindowTab;
		private System.Windows.Forms.FlowLayoutPanel flpWindowGroups;
		private System.Windows.Forms.FlowLayoutPanel flpCropBottomLabel;
		private System.Windows.Forms.FlowLayoutPanel flpCropRightLabel;
		private System.Windows.Forms.FlowLayoutPanel flpScanlinesSliderLabel;
		private System.Windows.Forms.FlowLayoutPanel flpARSelection;
		private System.Windows.Forms.FlowLayoutPanel flpScalingTab;
		private System.Windows.Forms.TableLayoutPanel tlpScalingFilter;
		private System.Windows.Forms.TableLayoutPanel tlpNonSquareAR;
	}
}