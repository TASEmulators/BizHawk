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
            this.cbScaleOSD = new System.Windows.Forms.CheckBox();
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
            this.cbMainFormMouseCaptureForcesTopmost = new System.Windows.Forms.CheckBox();
            this.cbMainFormStayOnTop = new System.Windows.Forms.CheckBox();
            this.cbMainFormSaveWindowPosition = new System.Windows.Forms.CheckBox();
            this.lblFrameTypeWindowed = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cbStatusBarWindowed = new System.Windows.Forms.CheckBox();
            this.label9 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cbMenuWindowed = new System.Windows.Forms.CheckBox();
            this.cbCaptionWindowed = new System.Windows.Forms.CheckBox();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tbScanlineIntensity = new BizHawk.Client.EmuHawk.TransparentTrackBar();
            this.trackbarFrameSizeWindowed = new BizHawk.Client.EmuHawk.TransparentTrackBar();
            this.groupBox1.SuspendLayout();
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
            ((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackbarFrameSizeWindowed)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.toolTip1.SetToolTip(this.btnCancel, resources.GetString("btnCancel.ToolTip"));
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            resources.ApplyResources(this.btnOk, "btnOk");
            this.btnOk.Name = "btnOk";
            this.toolTip1.SetToolTip(this.btnOk, resources.GetString("btnOk.ToolTip"));
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.lblScanlines);
            this.groupBox1.Controls.Add(this.lblUserFilterName);
            this.groupBox1.Controls.Add(this.btnSelectUserFilter);
            this.groupBox1.Controls.Add(this.rbUser);
            this.groupBox1.Controls.Add(this.tbScanlineIntensity);
            this.groupBox1.Controls.Add(this.rbNone);
            this.groupBox1.Controls.Add(this.rbScanlines);
            this.groupBox1.Controls.Add(this.rbHq2x);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox1, resources.GetString("groupBox1.ToolTip"));
            // 
            // lblScanlines
            // 
            resources.ApplyResources(this.lblScanlines, "lblScanlines");
            this.lblScanlines.Name = "lblScanlines";
            this.toolTip1.SetToolTip(this.lblScanlines, resources.GetString("lblScanlines.ToolTip"));
            // 
            // lblUserFilterName
            // 
            resources.ApplyResources(this.lblUserFilterName, "lblUserFilterName");
            this.lblUserFilterName.Name = "lblUserFilterName";
            this.toolTip1.SetToolTip(this.lblUserFilterName, resources.GetString("lblUserFilterName.ToolTip"));
            // 
            // btnSelectUserFilter
            // 
            resources.ApplyResources(this.btnSelectUserFilter, "btnSelectUserFilter");
            this.btnSelectUserFilter.Name = "btnSelectUserFilter";
            this.toolTip1.SetToolTip(this.btnSelectUserFilter, resources.GetString("btnSelectUserFilter.ToolTip"));
            this.btnSelectUserFilter.UseVisualStyleBackColor = true;
            this.btnSelectUserFilter.Click += new System.EventHandler(this.BtnSelectUserFilter_Click);
            // 
            // rbUser
            // 
            resources.ApplyResources(this.rbUser, "rbUser");
            this.rbUser.Name = "rbUser";
            this.rbUser.TabStop = true;
            this.toolTip1.SetToolTip(this.rbUser, resources.GetString("rbUser.ToolTip"));
            this.rbUser.UseVisualStyleBackColor = true;
            // 
            // rbNone
            // 
            resources.ApplyResources(this.rbNone, "rbNone");
            this.rbNone.Name = "rbNone";
            this.rbNone.TabStop = true;
            this.toolTip1.SetToolTip(this.rbNone, resources.GetString("rbNone.ToolTip"));
            this.rbNone.UseVisualStyleBackColor = true;
            // 
            // rbScanlines
            // 
            resources.ApplyResources(this.rbScanlines, "rbScanlines");
            this.rbScanlines.Name = "rbScanlines";
            this.rbScanlines.TabStop = true;
            this.toolTip1.SetToolTip(this.rbScanlines, resources.GetString("rbScanlines.ToolTip"));
            this.rbScanlines.UseVisualStyleBackColor = true;
            // 
            // rbHq2x
            // 
            resources.ApplyResources(this.rbHq2x, "rbHq2x");
            this.rbHq2x.Name = "rbHq2x";
            this.rbHq2x.TabStop = true;
            this.toolTip1.SetToolTip(this.rbHq2x, resources.GetString("rbHq2x.ToolTip"));
            this.rbHq2x.UseVisualStyleBackColor = true;
            // 
            // checkLetterbox
            // 
            resources.ApplyResources(this.checkLetterbox, "checkLetterbox");
            this.checkLetterbox.Name = "checkLetterbox";
            this.toolTip1.SetToolTip(this.checkLetterbox, resources.GetString("checkLetterbox.ToolTip"));
            this.checkLetterbox.UseVisualStyleBackColor = true;
            this.checkLetterbox.CheckedChanged += new System.EventHandler(this.CheckLetterbox_CheckedChanged);
            // 
            // checkPadInteger
            // 
            resources.ApplyResources(this.checkPadInteger, "checkPadInteger");
            this.checkPadInteger.Name = "checkPadInteger";
            this.toolTip1.SetToolTip(this.checkPadInteger, resources.GetString("checkPadInteger.ToolTip"));
            this.checkPadInteger.UseVisualStyleBackColor = true;
            this.checkPadInteger.CheckedChanged += new System.EventHandler(this.CheckPadInteger_CheckedChanged);
            // 
            // grpFinalFilter
            // 
            resources.ApplyResources(this.grpFinalFilter, "grpFinalFilter");
            this.grpFinalFilter.Controls.Add(this.rbFinalFilterBicubic);
            this.grpFinalFilter.Controls.Add(this.rbFinalFilterNone);
            this.grpFinalFilter.Controls.Add(this.rbFinalFilterBilinear);
            this.grpFinalFilter.Name = "grpFinalFilter";
            this.grpFinalFilter.TabStop = false;
            this.toolTip1.SetToolTip(this.grpFinalFilter, resources.GetString("grpFinalFilter.ToolTip"));
            // 
            // rbFinalFilterBicubic
            // 
            resources.ApplyResources(this.rbFinalFilterBicubic, "rbFinalFilterBicubic");
            this.rbFinalFilterBicubic.Name = "rbFinalFilterBicubic";
            this.rbFinalFilterBicubic.TabStop = true;
            this.toolTip1.SetToolTip(this.rbFinalFilterBicubic, resources.GetString("rbFinalFilterBicubic.ToolTip"));
            this.rbFinalFilterBicubic.UseVisualStyleBackColor = true;
            // 
            // rbFinalFilterNone
            // 
            resources.ApplyResources(this.rbFinalFilterNone, "rbFinalFilterNone");
            this.rbFinalFilterNone.Name = "rbFinalFilterNone";
            this.rbFinalFilterNone.TabStop = true;
            this.toolTip1.SetToolTip(this.rbFinalFilterNone, resources.GetString("rbFinalFilterNone.ToolTip"));
            this.rbFinalFilterNone.UseVisualStyleBackColor = true;
            // 
            // rbFinalFilterBilinear
            // 
            resources.ApplyResources(this.rbFinalFilterBilinear, "rbFinalFilterBilinear");
            this.rbFinalFilterBilinear.Name = "rbFinalFilterBilinear";
            this.rbFinalFilterBilinear.TabStop = true;
            this.toolTip1.SetToolTip(this.rbFinalFilterBilinear, resources.GetString("rbFinalFilterBilinear.ToolTip"));
            this.rbFinalFilterBilinear.UseVisualStyleBackColor = true;
            // 
            // rbUseRaw
            // 
            resources.ApplyResources(this.rbUseRaw, "rbUseRaw");
            this.rbUseRaw.Name = "rbUseRaw";
            this.rbUseRaw.TabStop = true;
            this.toolTip1.SetToolTip(this.rbUseRaw, resources.GetString("rbUseRaw.ToolTip"));
            this.rbUseRaw.UseVisualStyleBackColor = true;
            this.rbUseRaw.CheckedChanged += new System.EventHandler(this.RbUseRaw_CheckedChanged);
            // 
            // rbUseSystem
            // 
            resources.ApplyResources(this.rbUseSystem, "rbUseSystem");
            this.rbUseSystem.Name = "rbUseSystem";
            this.rbUseSystem.TabStop = true;
            this.toolTip1.SetToolTip(this.rbUseSystem, resources.GetString("rbUseSystem.ToolTip"));
            this.rbUseSystem.UseVisualStyleBackColor = true;
            this.rbUseSystem.CheckedChanged += new System.EventHandler(this.RbUseSystem_CheckedChanged);
            // 
            // grpARSelection
            // 
            resources.ApplyResources(this.grpARSelection, "grpARSelection");
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
            this.grpARSelection.Name = "grpARSelection";
            this.grpARSelection.TabStop = false;
            this.toolTip1.SetToolTip(this.grpARSelection, resources.GetString("grpARSelection.ToolTip"));
            // 
            // txtCustomARY
            // 
            resources.ApplyResources(this.txtCustomARY, "txtCustomARY");
            this.txtCustomARY.Name = "txtCustomARY";
            this.toolTip1.SetToolTip(this.txtCustomARY, resources.GetString("txtCustomARY.ToolTip"));
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            this.toolTip1.SetToolTip(this.label12, resources.GetString("label12.ToolTip"));
            // 
            // txtCustomARX
            // 
            resources.ApplyResources(this.txtCustomARX, "txtCustomARX");
            this.txtCustomARX.Name = "txtCustomARX";
            this.toolTip1.SetToolTip(this.txtCustomARX, resources.GetString("txtCustomARX.ToolTip"));
            // 
            // rbUseCustomRatio
            // 
            resources.ApplyResources(this.rbUseCustomRatio, "rbUseCustomRatio");
            this.rbUseCustomRatio.Name = "rbUseCustomRatio";
            this.rbUseCustomRatio.TabStop = true;
            this.toolTip1.SetToolTip(this.rbUseCustomRatio, resources.GetString("rbUseCustomRatio.ToolTip"));
            this.rbUseCustomRatio.UseVisualStyleBackColor = true;
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // txtCustomARHeight
            // 
            resources.ApplyResources(this.txtCustomARHeight, "txtCustomARHeight");
            this.txtCustomARHeight.Name = "txtCustomARHeight";
            this.toolTip1.SetToolTip(this.txtCustomARHeight, resources.GetString("txtCustomARHeight.ToolTip"));
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // txtCustomARWidth
            // 
            resources.ApplyResources(this.txtCustomARWidth, "txtCustomARWidth");
            this.txtCustomARWidth.Name = "txtCustomARWidth";
            this.toolTip1.SetToolTip(this.txtCustomARWidth, resources.GetString("txtCustomARWidth.ToolTip"));
            // 
            // rbUseCustom
            // 
            resources.ApplyResources(this.rbUseCustom, "rbUseCustom");
            this.rbUseCustom.Name = "rbUseCustom";
            this.rbUseCustom.TabStop = true;
            this.toolTip1.SetToolTip(this.rbUseCustom, resources.GetString("rbUseCustom.ToolTip"));
            this.rbUseCustom.UseVisualStyleBackColor = true;
            // 
            // rbOpenGL
            // 
            resources.ApplyResources(this.rbOpenGL, "rbOpenGL");
            this.rbOpenGL.Checked = true;
            this.rbOpenGL.Name = "rbOpenGL";
            this.rbOpenGL.TabStop = true;
            this.toolTip1.SetToolTip(this.rbOpenGL, resources.GetString("rbOpenGL.ToolTip"));
            this.rbOpenGL.UseVisualStyleBackColor = true;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            this.toolTip1.SetToolTip(this.label5, resources.GetString("label5.ToolTip"));
            // 
            // tabControl1
            // 
            resources.ApplyResources(this.tabControl1, "tabControl1");
            this.tabControl1.Controls.Add(this.tpAR);
            this.tabControl1.Controls.Add(this.tpDispMethod);
            this.tabControl1.Controls.Add(this.tpMisc);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.toolTip1.SetToolTip(this.tabControl1, resources.GetString("tabControl1.ToolTip"));
            // 
            // tpAR
            // 
            resources.ApplyResources(this.tpAR, "tpAR");
            this.tpAR.Controls.Add(this.cbScaleOSD);
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
            this.tpAR.Name = "tpAR";
            this.toolTip1.SetToolTip(this.tpAR, resources.GetString("tpAR.ToolTip"));
            this.tpAR.UseVisualStyleBackColor = true;
            // 
            // cbScaleOSD
            // 
            resources.ApplyResources(this.cbScaleOSD, "cbScaleOSD");
            this.cbScaleOSD.Name = "cbScaleOSD";
            this.toolTip1.SetToolTip(this.cbScaleOSD, resources.GetString("cbScaleOSD.ToolTip"));
            this.cbScaleOSD.UseVisualStyleBackColor = true;
            // 
            // groupBox6
            // 
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Controls.Add(this.label16);
            this.groupBox6.Controls.Add(this.label15);
            this.groupBox6.Controls.Add(this.txtCropBottom);
            this.groupBox6.Controls.Add(this.label17);
            this.groupBox6.Controls.Add(this.txtCropRight);
            this.groupBox6.Controls.Add(this.txtCropTop);
            this.groupBox6.Controls.Add(this.label14);
            this.groupBox6.Controls.Add(this.txtCropLeft);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox6, resources.GetString("groupBox6.ToolTip"));
            // 
            // label16
            // 
            resources.ApplyResources(this.label16, "label16");
            this.label16.Name = "label16";
            this.toolTip1.SetToolTip(this.label16, resources.GetString("label16.ToolTip"));
            // 
            // label15
            // 
            resources.ApplyResources(this.label15, "label15");
            this.label15.Name = "label15";
            this.toolTip1.SetToolTip(this.label15, resources.GetString("label15.ToolTip"));
            // 
            // txtCropBottom
            // 
            resources.ApplyResources(this.txtCropBottom, "txtCropBottom");
            this.txtCropBottom.Name = "txtCropBottom";
            this.toolTip1.SetToolTip(this.txtCropBottom, resources.GetString("txtCropBottom.ToolTip"));
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.Name = "label17";
            this.toolTip1.SetToolTip(this.label17, resources.GetString("label17.ToolTip"));
            // 
            // txtCropRight
            // 
            resources.ApplyResources(this.txtCropRight, "txtCropRight");
            this.txtCropRight.Name = "txtCropRight";
            this.toolTip1.SetToolTip(this.txtCropRight, resources.GetString("txtCropRight.ToolTip"));
            // 
            // txtCropTop
            // 
            resources.ApplyResources(this.txtCropTop, "txtCropTop");
            this.txtCropTop.Name = "txtCropTop";
            this.toolTip1.SetToolTip(this.txtCropTop, resources.GetString("txtCropTop.ToolTip"));
            // 
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            this.toolTip1.SetToolTip(this.label14, resources.GetString("label14.ToolTip"));
            // 
            // txtCropLeft
            // 
            resources.ApplyResources(this.txtCropLeft, "txtCropLeft");
            this.txtCropLeft.Name = "txtCropLeft";
            this.toolTip1.SetToolTip(this.txtCropLeft, resources.GetString("txtCropLeft.ToolTip"));
            // 
            // btnDefaults
            // 
            resources.ApplyResources(this.btnDefaults, "btnDefaults");
            this.btnDefaults.Name = "btnDefaults";
            this.toolTip1.SetToolTip(this.btnDefaults, resources.GetString("btnDefaults.ToolTip"));
            this.btnDefaults.UseVisualStyleBackColor = true;
            this.btnDefaults.Click += new System.EventHandler(this.BtnDefaults_Click);
            // 
            // cbAutoPrescale
            // 
            resources.ApplyResources(this.cbAutoPrescale, "cbAutoPrescale");
            this.cbAutoPrescale.Name = "cbAutoPrescale";
            this.toolTip1.SetToolTip(this.cbAutoPrescale, resources.GetString("cbAutoPrescale.ToolTip"));
            this.cbAutoPrescale.UseVisualStyleBackColor = true;
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            this.toolTip1.SetToolTip(this.label11, resources.GetString("label11.ToolTip"));
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            this.toolTip1.SetToolTip(this.label10, resources.GetString("label10.ToolTip"));
            // 
            // nudPrescale
            // 
            resources.ApplyResources(this.nudPrescale, "nudPrescale");
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
            this.toolTip1.SetToolTip(this.nudPrescale, resources.GetString("nudPrescale.ToolTip"));
            this.nudPrescale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // tpDispMethod
            // 
            resources.ApplyResources(this.tpDispMethod, "tpDispMethod");
            this.tpDispMethod.Controls.Add(this.label6);
            this.tpDispMethod.Controls.Add(this.groupBox3);
            this.tpDispMethod.Name = "tpDispMethod";
            this.toolTip1.SetToolTip(this.tpDispMethod, resources.GetString("tpDispMethod.ToolTip"));
            this.tpDispMethod.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            this.toolTip1.SetToolTip(this.label6, resources.GetString("label6.ToolTip"));
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.label13);
            this.groupBox3.Controls.Add(this.cbAllowTearing);
            this.groupBox3.Controls.Add(this.label8);
            this.groupBox3.Controls.Add(this.rbD3D11);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.rbGDIPlus);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.rbOpenGL);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox3, resources.GetString("groupBox3.ToolTip"));
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            this.toolTip1.SetToolTip(this.label13, resources.GetString("label13.ToolTip"));
            this.label13.Click += new System.EventHandler(this.Label13_Click);
            this.label13.DoubleClick += new System.EventHandler(this.Label13_Click);
            // 
            // cbAllowTearing
            // 
            resources.ApplyResources(this.cbAllowTearing, "cbAllowTearing");
            this.cbAllowTearing.Name = "cbAllowTearing";
            this.toolTip1.SetToolTip(this.cbAllowTearing, resources.GetString("cbAllowTearing.ToolTip"));
            this.cbAllowTearing.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            this.toolTip1.SetToolTip(this.label8, resources.GetString("label8.ToolTip"));
            // 
            // rbD3D11
            // 
            resources.ApplyResources(this.rbD3D11, "rbD3D11");
            this.rbD3D11.Checked = true;
            this.rbD3D11.Name = "rbD3D11";
            this.rbD3D11.TabStop = true;
            this.toolTip1.SetToolTip(this.rbD3D11, resources.GetString("rbD3D11.ToolTip"));
            this.rbD3D11.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            this.toolTip1.SetToolTip(this.label7, resources.GetString("label7.ToolTip"));
            // 
            // rbGDIPlus
            // 
            resources.ApplyResources(this.rbGDIPlus, "rbGDIPlus");
            this.rbGDIPlus.Checked = true;
            this.rbGDIPlus.Name = "rbGDIPlus";
            this.rbGDIPlus.TabStop = true;
            this.toolTip1.SetToolTip(this.rbGDIPlus, resources.GetString("rbGDIPlus.ToolTip"));
            this.rbGDIPlus.UseVisualStyleBackColor = true;
            // 
            // tpMisc
            // 
            resources.ApplyResources(this.tpMisc, "tpMisc");
            this.tpMisc.Controls.Add(this.flpStaticWindowTitles);
            this.tpMisc.Controls.Add(this.groupBox5);
            this.tpMisc.Name = "tpMisc";
            this.toolTip1.SetToolTip(this.tpMisc, resources.GetString("tpMisc.ToolTip"));
            this.tpMisc.UseVisualStyleBackColor = true;
            // 
            // flpStaticWindowTitles
            // 
            resources.ApplyResources(this.flpStaticWindowTitles, "flpStaticWindowTitles");
            this.flpStaticWindowTitles.Controls.Add(this.cbStaticWindowTitles);
            this.flpStaticWindowTitles.Controls.Add(this.lblStaticWindowTitles);
            this.flpStaticWindowTitles.Name = "flpStaticWindowTitles";
            this.toolTip1.SetToolTip(this.flpStaticWindowTitles, resources.GetString("flpStaticWindowTitles.ToolTip"));
            // 
            // cbStaticWindowTitles
            // 
            resources.ApplyResources(this.cbStaticWindowTitles, "cbStaticWindowTitles");
            this.cbStaticWindowTitles.Name = "cbStaticWindowTitles";
            this.toolTip1.SetToolTip(this.cbStaticWindowTitles, resources.GetString("cbStaticWindowTitles.ToolTip"));
            // 
            // lblStaticWindowTitles
            // 
            resources.ApplyResources(this.lblStaticWindowTitles, "lblStaticWindowTitles");
            this.lblStaticWindowTitles.Name = "lblStaticWindowTitles";
            this.toolTip1.SetToolTip(this.lblStaticWindowTitles, resources.GetString("lblStaticWindowTitles.ToolTip"));
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.rbDisplayAbsoluteZero);
            this.groupBox5.Controls.Add(this.rbDisplayMinimal);
            this.groupBox5.Controls.Add(this.rbDisplayFull);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox5, resources.GetString("groupBox5.ToolTip"));
            // 
            // rbDisplayAbsoluteZero
            // 
            resources.ApplyResources(this.rbDisplayAbsoluteZero, "rbDisplayAbsoluteZero");
            this.rbDisplayAbsoluteZero.Name = "rbDisplayAbsoluteZero";
            this.rbDisplayAbsoluteZero.TabStop = true;
            this.toolTip1.SetToolTip(this.rbDisplayAbsoluteZero, resources.GetString("rbDisplayAbsoluteZero.ToolTip"));
            this.rbDisplayAbsoluteZero.UseVisualStyleBackColor = true;
            // 
            // rbDisplayMinimal
            // 
            resources.ApplyResources(this.rbDisplayMinimal, "rbDisplayMinimal");
            this.rbDisplayMinimal.Name = "rbDisplayMinimal";
            this.rbDisplayMinimal.TabStop = true;
            this.toolTip1.SetToolTip(this.rbDisplayMinimal, resources.GetString("rbDisplayMinimal.ToolTip"));
            this.rbDisplayMinimal.UseVisualStyleBackColor = true;
            // 
            // rbDisplayFull
            // 
            resources.ApplyResources(this.rbDisplayFull, "rbDisplayFull");
            this.rbDisplayFull.Name = "rbDisplayFull";
            this.rbDisplayFull.TabStop = true;
            this.toolTip1.SetToolTip(this.rbDisplayFull, resources.GetString("rbDisplayFull.ToolTip"));
            this.rbDisplayFull.UseVisualStyleBackColor = true;
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Controls.Add(this.cbAllowDoubleclickFullscreen);
            this.tabPage1.Controls.Add(this.groupBox4);
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Name = "tabPage1";
            this.toolTip1.SetToolTip(this.tabPage1, resources.GetString("tabPage1.ToolTip"));
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // cbAllowDoubleclickFullscreen
            // 
            resources.ApplyResources(this.cbAllowDoubleclickFullscreen, "cbAllowDoubleclickFullscreen");
            this.cbAllowDoubleclickFullscreen.Name = "cbAllowDoubleclickFullscreen";
            this.toolTip1.SetToolTip(this.cbAllowDoubleclickFullscreen, resources.GetString("cbAllowDoubleclickFullscreen.ToolTip"));
            this.cbAllowDoubleclickFullscreen.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.cbFSAutohideMouse);
            this.groupBox4.Controls.Add(this.label1);
            this.groupBox4.Controls.Add(this.cbFullscreenHacks);
            this.groupBox4.Controls.Add(this.cbStatusBarFullscreen);
            this.groupBox4.Controls.Add(this.cbMenuFullscreen);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox4, resources.GetString("groupBox4.ToolTip"));
            // 
            // cbFSAutohideMouse
            // 
            resources.ApplyResources(this.cbFSAutohideMouse, "cbFSAutohideMouse");
            this.cbFSAutohideMouse.Name = "cbFSAutohideMouse";
            this.toolTip1.SetToolTip(this.cbFSAutohideMouse, resources.GetString("cbFSAutohideMouse.ToolTip"));
            this.cbFSAutohideMouse.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // cbFullscreenHacks
            // 
            resources.ApplyResources(this.cbFullscreenHacks, "cbFullscreenHacks");
            this.cbFullscreenHacks.Name = "cbFullscreenHacks";
            this.toolTip1.SetToolTip(this.cbFullscreenHacks, resources.GetString("cbFullscreenHacks.ToolTip"));
            this.cbFullscreenHacks.UseVisualStyleBackColor = true;
            // 
            // cbStatusBarFullscreen
            // 
            resources.ApplyResources(this.cbStatusBarFullscreen, "cbStatusBarFullscreen");
            this.cbStatusBarFullscreen.Name = "cbStatusBarFullscreen";
            this.toolTip1.SetToolTip(this.cbStatusBarFullscreen, resources.GetString("cbStatusBarFullscreen.ToolTip"));
            this.cbStatusBarFullscreen.UseVisualStyleBackColor = true;
            // 
            // cbMenuFullscreen
            // 
            resources.ApplyResources(this.cbMenuFullscreen, "cbMenuFullscreen");
            this.cbMenuFullscreen.Name = "cbMenuFullscreen";
            this.toolTip1.SetToolTip(this.cbMenuFullscreen, resources.GetString("cbMenuFullscreen.ToolTip"));
            this.cbMenuFullscreen.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.cbMainFormMouseCaptureForcesTopmost);
            this.groupBox2.Controls.Add(this.cbMainFormStayOnTop);
            this.groupBox2.Controls.Add(this.cbMainFormSaveWindowPosition);
            this.groupBox2.Controls.Add(this.lblFrameTypeWindowed);
            this.groupBox2.Controls.Add(this.cbStatusBarWindowed);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.cbMenuWindowed);
            this.groupBox2.Controls.Add(this.trackbarFrameSizeWindowed);
            this.groupBox2.Controls.Add(this.cbCaptionWindowed);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            this.toolTip1.SetToolTip(this.groupBox2, resources.GetString("groupBox2.ToolTip"));
            // 
            // cbMainFormMouseCaptureForcesTopmost
            // 
            resources.ApplyResources(this.cbMainFormMouseCaptureForcesTopmost, "cbMainFormMouseCaptureForcesTopmost");
            this.cbMainFormMouseCaptureForcesTopmost.Name = "cbMainFormMouseCaptureForcesTopmost";
            this.toolTip1.SetToolTip(this.cbMainFormMouseCaptureForcesTopmost, resources.GetString("cbMainFormMouseCaptureForcesTopmost.ToolTip"));
            this.cbMainFormMouseCaptureForcesTopmost.UseVisualStyleBackColor = true;
            // 
            // cbMainFormStayOnTop
            // 
            resources.ApplyResources(this.cbMainFormStayOnTop, "cbMainFormStayOnTop");
            this.cbMainFormStayOnTop.Name = "cbMainFormStayOnTop";
            this.toolTip1.SetToolTip(this.cbMainFormStayOnTop, resources.GetString("cbMainFormStayOnTop.ToolTip"));
            this.cbMainFormStayOnTop.UseVisualStyleBackColor = true;
            // 
            // cbMainFormSaveWindowPosition
            // 
            resources.ApplyResources(this.cbMainFormSaveWindowPosition, "cbMainFormSaveWindowPosition");
            this.cbMainFormSaveWindowPosition.Name = "cbMainFormSaveWindowPosition";
            this.toolTip1.SetToolTip(this.cbMainFormSaveWindowPosition, resources.GetString("cbMainFormSaveWindowPosition.ToolTip"));
            this.cbMainFormSaveWindowPosition.UseVisualStyleBackColor = true;
            // 
            // lblFrameTypeWindowed
            // 
            resources.ApplyResources(this.lblFrameTypeWindowed, "lblFrameTypeWindowed");
            this.lblFrameTypeWindowed.Name = "lblFrameTypeWindowed";
            this.toolTip1.SetToolTip(this.lblFrameTypeWindowed, resources.GetString("lblFrameTypeWindowed.ToolTip"));
            // 
            // cbStatusBarWindowed
            // 
            resources.ApplyResources(this.cbStatusBarWindowed, "cbStatusBarWindowed");
            this.cbStatusBarWindowed.Name = "cbStatusBarWindowed";
            this.toolTip1.SetToolTip(this.cbStatusBarWindowed, resources.GetString("cbStatusBarWindowed.ToolTip"));
            this.cbStatusBarWindowed.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            this.toolTip1.SetToolTip(this.label9, resources.GetString("label9.ToolTip"));
            // 
            // cbMenuWindowed
            // 
            resources.ApplyResources(this.cbMenuWindowed, "cbMenuWindowed");
            this.cbMenuWindowed.Name = "cbMenuWindowed";
            this.toolTip1.SetToolTip(this.cbMenuWindowed, resources.GetString("cbMenuWindowed.ToolTip"));
            this.cbMenuWindowed.UseVisualStyleBackColor = true;
            // 
            // cbCaptionWindowed
            // 
            resources.ApplyResources(this.cbCaptionWindowed, "cbCaptionWindowed");
            this.cbCaptionWindowed.Name = "cbCaptionWindowed";
            this.toolTip1.SetToolTip(this.cbCaptionWindowed, resources.GetString("cbCaptionWindowed.ToolTip"));
            this.cbCaptionWindowed.UseVisualStyleBackColor = true;
            // 
            // linkLabel1
            // 
            resources.ApplyResources(this.linkLabel1, "linkLabel1");
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.TabStop = true;
            this.toolTip1.SetToolTip(this.linkLabel1, resources.GetString("linkLabel1.ToolTip"));
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.LinkLabel1_LinkClicked);
            // 
            // tbScanlineIntensity
            // 
            resources.ApplyResources(this.tbScanlineIntensity, "tbScanlineIntensity");
            this.tbScanlineIntensity.LargeChange = 32;
            this.tbScanlineIntensity.Maximum = 256;
            this.tbScanlineIntensity.Name = "tbScanlineIntensity";
            this.tbScanlineIntensity.TickFrequency = 32;
            this.tbScanlineIntensity.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.toolTip1.SetToolTip(this.tbScanlineIntensity, resources.GetString("tbScanlineIntensity.ToolTip"));
            this.tbScanlineIntensity.Scroll += new System.EventHandler(this.TbScanlineIntensity_Scroll);
            this.tbScanlineIntensity.ValueChanged += new System.EventHandler(this.TbScanlineIntensity_Scroll);
            // 
            // trackbarFrameSizeWindowed
            // 
            resources.ApplyResources(this.trackbarFrameSizeWindowed, "trackbarFrameSizeWindowed");
            this.trackbarFrameSizeWindowed.LargeChange = 1;
            this.trackbarFrameSizeWindowed.Maximum = 2;
            this.trackbarFrameSizeWindowed.Name = "trackbarFrameSizeWindowed";
            this.toolTip1.SetToolTip(this.trackbarFrameSizeWindowed, resources.GetString("trackbarFrameSizeWindowed.ToolTip"));
            this.trackbarFrameSizeWindowed.Value = 1;
            this.trackbarFrameSizeWindowed.ValueChanged += new System.EventHandler(this.TrackBarFrameSizeWindowed_ValueChanged);
            // 
            // DisplayConfig
            // 
            this.AcceptButton = this.btnOk;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.linkLabel1);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "DisplayConfig";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
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
            this.tpDispMethod.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tpMisc.ResumeLayout(false);
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
            ((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).EndInit();
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
		private System.Windows.Forms.CheckBox cbScaleOSD;
		private System.Windows.Forms.CheckBox cbMainFormMouseCaptureForcesTopmost;
	}
}
