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
			this.btnDialogCancel = new BizHawk.WinForms.Controls.SzButtonEx();
			this.btnDialogOK = new BizHawk.WinForms.Controls.SzButtonEx();
			this.rbDispMethodOpenGL = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.lblDispMethodOpenGL = new BizHawk.WinForms.Controls.LocLabelEx();
			this.tcDialog = new System.Windows.Forms.TabControl();
			this.tpScaling = new BizHawk.WinForms.Controls.TabPageEx();
			this.flpTpScaling = new BizHawk.WinForms.Controls.SzColumnsToRightFLP();
			this.flpUserPrescale = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.lblUserPrescale = new BizHawk.WinForms.Controls.LabelEx();
			this.nudUserPrescale = new BizHawk.WinForms.Controls.SzNUDEx();
			this.lblUserPrescaleUnits = new BizHawk.WinForms.Controls.LabelEx();
			this.grpFilter = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.tlpGrpFilter = new System.Windows.Forms.TableLayoutPanel();
			this.rbFilterNone = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbFilterUser = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbFilterHq2x = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbFilterScanline = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.flpFilterUser = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.btnFilterUser = new BizHawk.WinForms.Controls.SzButtonEx();
			this.lblFilterUser = new BizHawk.WinForms.Controls.LabelEx();
			this.flpFilterScanlineAlpha = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.lblFilterScanlineAlpha = new BizHawk.WinForms.Controls.LabelEx();
			this.tbFilterScanlineAlpha = new BizHawk.Client.EmuHawk.TransparentTrackBar();
			this.cbAutoPrescale = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpFinalFilter = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpFinalFilter = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.rbFinalFilterNone = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbFinalFilterBilinear = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbFinalFilterBicubic = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.cbLetterbox = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpAspectRatio = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpAspectRatio = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.rbARSquare = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.lblAspectRatioNonSquare = new BizHawk.WinForms.Controls.LabelEx();
			this.rbARBySystem = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.flpCustomSize = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.rbARCustomSize = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.txtARCustomWidth = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.lblARCustomSizeSeparator = new BizHawk.WinForms.Controls.LabelEx();
			this.txtARCustomHeight = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.flpCustomAR = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.rbARCustomRatio = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.txtARCustomRatioH = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.lblARCustomRatioSeparator = new BizHawk.WinForms.Controls.LabelEx();
			this.txtARCustomRatioV = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.cbScaleByInteger = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpCrop = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpCrop = new BizHawk.WinForms.Controls.LocSingleRowFLP();
			this.lblCropLeft = new BizHawk.WinForms.Controls.LabelEx();
			this.txtCropLeft = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.lblCropTop = new BizHawk.WinForms.Controls.LabelEx();
			this.txtCropTop = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.lblCropRight = new BizHawk.WinForms.Controls.LabelEx();
			this.txtCropRight = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.lblCropBottom = new BizHawk.WinForms.Controls.LabelEx();
			this.txtCropBottom = new BizHawk.WinForms.Controls.SzTextBoxEx();
			this.btnDefaults = new BizHawk.WinForms.Controls.LocSzButtonEx();
			this.tpDispMethod = new BizHawk.WinForms.Controls.TabPageEx();
			this.flpTpDispMethod = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.grpDispMethod = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpDispMethod = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.flpD3DSection = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.rbDispMethodD3D = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.lblDispMethodD3D = new BizHawk.WinForms.Controls.LocLabelEx();
			this.flpD3DAltVSync = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.cbD3DAltVSync = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblD3DAltVSync = new BizHawk.WinForms.Controls.SzLabelEx();
			this.rbDispMethodGDIPlus = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.lblDispMethodGDIPlus = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblDispMethodRestartWarning = new BizHawk.WinForms.Controls.LabelEx();
			this.tpMisc = new BizHawk.WinForms.Controls.TabPageEx();
			this.grpDispFeatures = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpDispFeatures = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.rbDispFeaturesFull = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbDispFeaturesMinimal = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.rbDispFeaturesNothing = new BizHawk.WinForms.Controls.RadioButtonEx();
			this.tpWindow = new BizHawk.WinForms.Controls.TabPageEx();
			this.flpTpWindow = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.flpWindowFSGroups = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.grpWindowed = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpWindowed = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.flpWindowedFrameType = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.lblWindowedFrameTypeDesc = new BizHawk.WinForms.Controls.LabelEx();
			this.lblWindowedFrameTypeReadout = new BizHawk.WinForms.Controls.LabelEx();
			this.tbWindowedFrameType = new BizHawk.Client.EmuHawk.TransparentTrackBar();
			this.cbWindowedStatusBar = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbWindowedCaption = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbWindowedMenu = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpFS = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpFS = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.flpFSCheckBoxes = new BizHawk.WinForms.Controls.SzRowsToBottomFLP();
			this.cbFSStatusBar = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbFSAutohideMouse = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbFSMenu = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbFSWinHacks = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblFSWinHacks = new BizHawk.WinForms.Controls.SzLabelEx();
			this.cbDoubleClickFS = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lnkDocs = new BizHawk.WinForms.Controls.LocLinkLabelEx();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.flpDialogButtons = new BizHawk.WinForms.Controls.LocSzSingleRowFLP();
			this.tcDialog.SuspendLayout();
			this.tpScaling.SuspendLayout();
			this.flpTpScaling.SuspendLayout();
			this.flpUserPrescale.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudUserPrescale)).BeginInit();
			this.grpFilter.SuspendLayout();
			this.tlpGrpFilter.SuspendLayout();
			this.flpFilterUser.SuspendLayout();
			this.flpFilterScanlineAlpha.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbFilterScanlineAlpha)).BeginInit();
			this.grpFinalFilter.SuspendLayout();
			this.flpGrpFinalFilter.SuspendLayout();
			this.grpAspectRatio.SuspendLayout();
			this.flpGrpAspectRatio.SuspendLayout();
			this.flpCustomSize.SuspendLayout();
			this.flpCustomAR.SuspendLayout();
			this.grpCrop.SuspendLayout();
			this.flpGrpCrop.SuspendLayout();
			this.tpDispMethod.SuspendLayout();
			this.flpTpDispMethod.SuspendLayout();
			this.grpDispMethod.SuspendLayout();
			this.flpGrpDispMethod.SuspendLayout();
			this.flpD3DSection.SuspendLayout();
			this.flpD3DAltVSync.SuspendLayout();
			this.tpMisc.SuspendLayout();
			this.grpDispFeatures.SuspendLayout();
			this.flpGrpDispFeatures.SuspendLayout();
			this.tpWindow.SuspendLayout();
			this.flpTpWindow.SuspendLayout();
			this.flpWindowFSGroups.SuspendLayout();
			this.grpWindowed.SuspendLayout();
			this.flpWindowed.SuspendLayout();
			this.flpWindowedFrameType.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbWindowedFrameType)).BeginInit();
			this.grpFS.SuspendLayout();
			this.flpGrpFS.SuspendLayout();
			this.flpFSCheckBoxes.SuspendLayout();
			this.flpDialogButtons.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnDialogCancel
			// 
			this.btnDialogCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDialogCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnDialogCancel.Name = "btnDialogCancel";
			this.btnDialogCancel.Size = new System.Drawing.Size(75, 23);
			this.btnDialogCancel.Text = "Cancel";
			// 
			// btnDialogOK
			// 
			this.btnDialogOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDialogOK.Name = "btnDialogOK";
			this.btnDialogOK.Size = new System.Drawing.Size(75, 23);
			this.btnDialogOK.Text = "OK";
			this.btnDialogOK.Click += new System.EventHandler(this.btnDialogOK_Click);
			// 
			// rbDispMethodOpenGL
			// 
			this.rbDispMethodOpenGL.Checked = true;
			this.rbDispMethodOpenGL.Name = "rbDispMethodOpenGL";
			this.rbDispMethodOpenGL.Text = "OpenGL";
			// 
			// lblDispMethodOpenGL
			// 
			this.lblDispMethodOpenGL.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblDispMethodOpenGL.Location = new System.Drawing.Point(3, 115);
			this.lblDispMethodOpenGL.Name = "lblDispMethodOpenGL";
			this.lblDispMethodOpenGL.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblDispMethodOpenGL.Text = " • May malfunction on some systems.\r\n • May have increased performance for OpenGL" +
    "-based emulation cores.\r\n • May have reduced performance on some systems.\r\n";
			// 
			// tcDialog
			// 
			this.tcDialog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tcDialog.Controls.Add(this.tpScaling);
			this.tcDialog.Controls.Add(this.tpDispMethod);
			this.tcDialog.Controls.Add(this.tpMisc);
			this.tcDialog.Controls.Add(this.tpWindow);
			this.tcDialog.Location = new System.Drawing.Point(4, 4);
			this.tcDialog.Name = "tcDialog";
			this.tcDialog.SelectedIndex = 0;
			this.tcDialog.Size = new System.Drawing.Size(524, 345);
			this.tcDialog.TabIndex = 17;
			// 
			// tpScaling
			// 
			this.tpScaling.Controls.Add(this.flpTpScaling);
			this.tpScaling.Name = "tpScaling";
			this.tpScaling.Padding = new System.Windows.Forms.Padding(3);
			this.tpScaling.Text = "Scaling & Filtering";
			// 
			// flpTpScaling
			// 
			this.flpTpScaling.Controls.Add(this.flpUserPrescale);
			this.flpTpScaling.Controls.Add(this.grpFilter);
			this.flpTpScaling.Controls.Add(this.cbAutoPrescale);
			this.flpTpScaling.Controls.Add(this.grpFinalFilter);
			this.flpTpScaling.Controls.Add(this.cbLetterbox);
			this.flpTpScaling.Controls.Add(this.grpAspectRatio);
			this.flpTpScaling.Controls.Add(this.cbScaleByInteger);
			this.flpTpScaling.Controls.Add(this.grpCrop);
			this.flpTpScaling.Controls.Add(this.btnDefaults);
			this.flpTpScaling.MinimumSize = new System.Drawing.Size(24, 24);
			this.flpTpScaling.Name = "flpTpScaling";
			this.flpTpScaling.Size = new System.Drawing.Size(516, 319);
			// 
			// flpUserPrescale
			// 
			this.flpUserPrescale.Controls.Add(this.lblUserPrescale);
			this.flpUserPrescale.Controls.Add(this.nudUserPrescale);
			this.flpUserPrescale.Controls.Add(this.lblUserPrescaleUnits);
			this.flpUserPrescale.Name = "flpUserPrescale";
			// 
			// lblUserPrescale
			// 
			this.lblUserPrescale.Name = "lblUserPrescale";
			this.lblUserPrescale.Text = "User Prescale:";
			// 
			// nudUserPrescale
			// 
			this.nudUserPrescale.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.nudUserPrescale.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.nudUserPrescale.Name = "nudUserPrescale";
			this.nudUserPrescale.Size = new System.Drawing.Size(45, 20);
			this.nudUserPrescale.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// lblUserPrescaleUnits
			// 
			this.lblUserPrescaleUnits.Name = "lblUserPrescaleUnits";
			this.lblUserPrescaleUnits.Text = "X";
			// 
			// grpFilter
			// 
			this.grpFilter.Controls.Add(this.tlpGrpFilter);
			this.grpFilter.Name = "grpFilter";
			this.grpFilter.Size = new System.Drawing.Size(176, 170);
			this.grpFilter.Text = "Scaling Filter";
			// 
			// tlpGrpFilter
			// 
			this.tlpGrpFilter.ColumnCount = 2;
			this.tlpGrpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpGrpFilter.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tlpGrpFilter.Controls.Add(this.rbFilterNone, 0, 0);
			this.tlpGrpFilter.Controls.Add(this.rbFilterUser, 0, 3);
			this.tlpGrpFilter.Controls.Add(this.rbFilterHq2x, 0, 1);
			this.tlpGrpFilter.Controls.Add(this.rbFilterScanline, 0, 2);
			this.tlpGrpFilter.Controls.Add(this.flpFilterUser, 1, 3);
			this.tlpGrpFilter.Controls.Add(this.flpFilterScanlineAlpha, 1, 2);
			this.tlpGrpFilter.Location = new System.Drawing.Point(2, 15);
			this.tlpGrpFilter.Name = "tlpGrpFilter";
			this.tlpGrpFilter.RowCount = 4;
			this.tlpGrpFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpGrpFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpGrpFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpGrpFilter.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tlpGrpFilter.Size = new System.Drawing.Size(173, 155);
			this.tlpGrpFilter.TabIndex = 1;
			// 
			// rbFilterNone
			// 
			this.rbFilterNone.Name = "rbFilterNone";
			this.rbFilterNone.Text = "None";
			// 
			// rbFilterUser
			// 
			this.rbFilterUser.Name = "rbFilterUser";
			this.rbFilterUser.Text = "User";
			// 
			// rbFilterHq2x
			// 
			this.rbFilterHq2x.Name = "rbFilterHq2x";
			this.rbFilterHq2x.Text = "Hq2x";
			// 
			// rbFilterScanline
			// 
			this.rbFilterScanline.Name = "rbFilterScanline";
			this.rbFilterScanline.Text = "Scanlines";
			// 
			// flpFilterUser
			// 
			this.flpFilterUser.Controls.Add(this.btnFilterUser);
			this.flpFilterUser.Controls.Add(this.lblFilterUser);
			this.flpFilterUser.Name = "flpFilterUser";
			// 
			// btnFilterUser
			// 
			this.btnFilterUser.Name = "btnFilterUser";
			this.btnFilterUser.Size = new System.Drawing.Size(75, 23);
			this.btnFilterUser.Text = "Select";
			this.btnFilterUser.Click += new System.EventHandler(this.btnFilterUser_Click);
			// 
			// lblFilterUser
			// 
			this.lblFilterUser.Name = "lblFilterUser";
			this.lblFilterUser.Text = "Will contain user filter name";
			// 
			// flpFilterScanlineAlpha
			// 
			this.flpFilterScanlineAlpha.Controls.Add(this.lblFilterScanlineAlpha);
			this.flpFilterScanlineAlpha.Controls.Add(this.tbFilterScanlineAlpha);
			this.flpFilterScanlineAlpha.Name = "flpFilterScanlineAlpha";
			// 
			// lblFilterScanlineAlpha
			// 
			this.lblFilterScanlineAlpha.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblFilterScanlineAlpha.Name = "lblFilterScanlineAlpha";
			this.lblFilterScanlineAlpha.Text = "%";
			// 
			// tbFilterScanlineAlpha
			// 
			this.tbFilterScanlineAlpha.LargeChange = 32;
			this.tbFilterScanlineAlpha.Location = new System.Drawing.Point(3, 16);
			this.tbFilterScanlineAlpha.Maximum = 256;
			this.tbFilterScanlineAlpha.Name = "tbFilterScanlineAlpha";
			this.tbFilterScanlineAlpha.Size = new System.Drawing.Size(70, 45);
			this.tbFilterScanlineAlpha.TabIndex = 3;
			this.tbFilterScanlineAlpha.TickFrequency = 32;
			this.tbFilterScanlineAlpha.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
			this.tbFilterScanlineAlpha.Scroll += new System.EventHandler(this.tbFilterScanlineAlpha_Scroll);
			this.tbFilterScanlineAlpha.ValueChanged += new System.EventHandler(this.tbFilterScanlineAlpha_Scroll);
			// 
			// cbAutoPrescale
			// 
			this.cbAutoPrescale.Name = "cbAutoPrescale";
			this.cbAutoPrescale.Text = "Auto Prescale";
			// 
			// grpFinalFilter
			// 
			this.grpFinalFilter.Controls.Add(this.flpGrpFinalFilter);
			this.grpFinalFilter.Name = "grpFinalFilter";
			this.grpFinalFilter.Size = new System.Drawing.Size(158, 88);
			this.grpFinalFilter.Text = "Final Filter";
			// 
			// flpGrpFinalFilter
			// 
			this.flpGrpFinalFilter.Controls.Add(this.rbFinalFilterNone);
			this.flpGrpFinalFilter.Controls.Add(this.rbFinalFilterBilinear);
			this.flpGrpFinalFilter.Controls.Add(this.rbFinalFilterBicubic);
			this.flpGrpFinalFilter.Location = new System.Drawing.Point(6, 15);
			this.flpGrpFinalFilter.Name = "flpGrpFinalFilter";
			// 
			// rbFinalFilterNone
			// 
			this.rbFinalFilterNone.Name = "rbFinalFilterNone";
			this.rbFinalFilterNone.Text = "None";
			// 
			// rbFinalFilterBilinear
			// 
			this.rbFinalFilterBilinear.Name = "rbFinalFilterBilinear";
			this.rbFinalFilterBilinear.Text = "Bilinear";
			// 
			// rbFinalFilterBicubic
			// 
			this.rbFinalFilterBicubic.Name = "rbFinalFilterBicubic";
			this.rbFinalFilterBicubic.Text = "Bicubic (shader. buggy?)";
			// 
			// cbLetterbox
			// 
			this.cbLetterbox.Name = "cbLetterbox";
			this.cbLetterbox.Text = "Maintain aspect ratio (letterbox)";
			this.cbLetterbox.CheckedChanged += new System.EventHandler(this.cbLetterbox_CheckedChanged);
			// 
			// grpAspectRatio
			// 
			this.grpAspectRatio.Controls.Add(this.flpGrpAspectRatio);
			this.grpAspectRatio.Name = "grpAspectRatio";
			this.grpAspectRatio.Size = new System.Drawing.Size(295, 128);
			this.grpAspectRatio.Text = "Aspect Ratio Selection";
			// 
			// flpGrpAspectRatio
			// 
			this.flpGrpAspectRatio.Controls.Add(this.rbARSquare);
			this.flpGrpAspectRatio.Controls.Add(this.lblAspectRatioNonSquare);
			this.flpGrpAspectRatio.Controls.Add(this.rbARBySystem);
			this.flpGrpAspectRatio.Controls.Add(this.flpCustomSize);
			this.flpGrpAspectRatio.Controls.Add(this.flpCustomAR);
			this.flpGrpAspectRatio.Location = new System.Drawing.Point(6, 15);
			this.flpGrpAspectRatio.Name = "flpGrpAspectRatio";
			// 
			// rbARSquare
			// 
			this.rbARSquare.Name = "rbARSquare";
			this.rbARSquare.Text = "Use 1:1 pixel size (for crispness or debugging)";
			this.rbARSquare.CheckedChanged += new System.EventHandler(this.rbARSquare_CheckedChanged);
			// 
			// lblAspectRatioNonSquare
			// 
			this.lblAspectRatioNonSquare.Name = "lblAspectRatioNonSquare";
			this.lblAspectRatioNonSquare.Text = "Allow pixel distortion (e.g. 2x1 pixels, for better AR fit):";
			// 
			// rbARBySystem
			// 
			this.rbARBySystem.Name = "rbARBySystem";
			this.rbARBySystem.Text = "Use system\'s recommendation";
			this.rbARBySystem.CheckedChanged += new System.EventHandler(this.rbARBySystem_CheckedChanged);
			// 
			// flpCustomSize
			// 
			this.flpCustomSize.Controls.Add(this.rbARCustomSize);
			this.flpCustomSize.Controls.Add(this.txtARCustomWidth);
			this.flpCustomSize.Controls.Add(this.lblARCustomSizeSeparator);
			this.flpCustomSize.Controls.Add(this.txtARCustomHeight);
			this.flpCustomSize.Name = "flpCustomSize";
			// 
			// rbARCustomSize
			// 
			this.rbARCustomSize.Name = "rbARCustomSize";
			this.rbARCustomSize.Text = "Use custom size:";
			// 
			// txtARCustomWidth
			// 
			this.txtARCustomWidth.Name = "txtARCustomWidth";
			this.txtARCustomWidth.Size = new System.Drawing.Size(72, 20);
			// 
			// lblARCustomSizeSeparator
			// 
			this.lblARCustomSizeSeparator.Name = "lblARCustomSizeSeparator";
			this.lblARCustomSizeSeparator.Text = "x";
			// 
			// txtARCustomHeight
			// 
			this.txtARCustomHeight.Name = "txtARCustomHeight";
			this.txtARCustomHeight.Size = new System.Drawing.Size(72, 20);
			// 
			// flpCustomAR
			// 
			this.flpCustomAR.Controls.Add(this.rbARCustomRatio);
			this.flpCustomAR.Controls.Add(this.txtARCustomRatioH);
			this.flpCustomAR.Controls.Add(this.lblARCustomRatioSeparator);
			this.flpCustomAR.Controls.Add(this.txtARCustomRatioV);
			this.flpCustomAR.Name = "flpCustomAR";
			// 
			// rbARCustomRatio
			// 
			this.rbARCustomRatio.Name = "rbARCustomRatio";
			this.rbARCustomRatio.Text = "Use custom AR:";
			// 
			// txtARCustomRatioH
			// 
			this.txtARCustomRatioH.Name = "txtARCustomRatioH";
			this.txtARCustomRatioH.Size = new System.Drawing.Size(72, 20);
			// 
			// lblARCustomRatioSeparator
			// 
			this.lblARCustomRatioSeparator.Name = "lblARCustomRatioSeparator";
			this.lblARCustomRatioSeparator.Text = ":";
			// 
			// txtARCustomRatioV
			// 
			this.txtARCustomRatioV.Name = "txtARCustomRatioV";
			this.txtARCustomRatioV.Size = new System.Drawing.Size(72, 20);
			// 
			// cbScaleByInteger
			// 
			this.cbScaleByInteger.Name = "cbScaleByInteger";
			this.cbScaleByInteger.Text = "Expand pixels by integers only (e.g. no 1.3333x)";
			this.cbScaleByInteger.CheckedChanged += new System.EventHandler(this.cbScaleByInteger_CheckedChanged);
			// 
			// grpCrop
			// 
			this.grpCrop.Controls.Add(this.flpGrpCrop);
			this.grpCrop.Name = "grpCrop";
			this.grpCrop.Size = new System.Drawing.Size(329, 43);
			this.grpCrop.Text = "Cropping";
			// 
			// flpGrpCrop
			// 
			this.flpGrpCrop.Controls.Add(this.lblCropLeft);
			this.flpGrpCrop.Controls.Add(this.txtCropLeft);
			this.flpGrpCrop.Controls.Add(this.lblCropTop);
			this.flpGrpCrop.Controls.Add(this.txtCropTop);
			this.flpGrpCrop.Controls.Add(this.lblCropRight);
			this.flpGrpCrop.Controls.Add(this.txtCropRight);
			this.flpGrpCrop.Controls.Add(this.lblCropBottom);
			this.flpGrpCrop.Controls.Add(this.txtCropBottom);
			this.flpGrpCrop.Location = new System.Drawing.Point(6, 15);
			this.flpGrpCrop.Name = "flpGrpCrop";
			// 
			// lblCropLeft
			// 
			this.lblCropLeft.Name = "lblCropLeft";
			this.lblCropLeft.Text = "Left:";
			// 
			// txtCropLeft
			// 
			this.txtCropLeft.Name = "txtCropLeft";
			this.txtCropLeft.Size = new System.Drawing.Size(34, 20);
			this.txtCropLeft.Text = "8000";
			// 
			// lblCropTop
			// 
			this.lblCropTop.Name = "lblCropTop";
			this.lblCropTop.Text = "Top:";
			// 
			// txtCropTop
			// 
			this.txtCropTop.Name = "txtCropTop";
			this.txtCropTop.Size = new System.Drawing.Size(34, 20);
			this.txtCropTop.Text = "8000";
			// 
			// lblCropRight
			// 
			this.lblCropRight.Name = "lblCropRight";
			this.lblCropRight.Text = "Right:";
			// 
			// txtCropRight
			// 
			this.txtCropRight.Name = "txtCropRight";
			this.txtCropRight.Size = new System.Drawing.Size(34, 20);
			this.txtCropRight.Text = "8000";
			// 
			// lblCropBottom
			// 
			this.lblCropBottom.Name = "lblCropBottom";
			this.lblCropBottom.Text = "Bottom:";
			// 
			// txtCropBottom
			// 
			this.txtCropBottom.Name = "txtCropBottom";
			this.txtCropBottom.Size = new System.Drawing.Size(34, 20);
			this.txtCropBottom.Text = "8000";
			// 
			// btnDefaults
			// 
			this.btnDefaults.Anchor = System.Windows.Forms.AnchorStyles.Right;
			this.btnDefaults.Location = new System.Drawing.Point(439, 232);
			this.btnDefaults.Name = "btnDefaults";
			this.btnDefaults.Size = new System.Drawing.Size(75, 23);
			this.btnDefaults.Text = "Defaults";
			this.toolTip1.SetToolTip(this.btnDefaults, "Unless I forgot to update the button\'s code when I changed a default");
			this.btnDefaults.Click += new System.EventHandler(this.btnDefaults_Click);
			// 
			// tpDispMethod
			// 
			this.tpDispMethod.Controls.Add(this.flpTpDispMethod);
			this.tpDispMethod.Name = "tpDispMethod";
			this.tpDispMethod.Text = "Display Method";
			// 
			// flpTpDispMethod
			// 
			this.flpTpDispMethod.Controls.Add(this.grpDispMethod);
			this.flpTpDispMethod.Controls.Add(this.lblDispMethodRestartWarning);
			this.flpTpDispMethod.Name = "flpTpDispMethod";
			// 
			// grpDispMethod
			// 
			this.grpDispMethod.Controls.Add(this.flpGrpDispMethod);
			this.grpDispMethod.Name = "grpDispMethod";
			this.grpDispMethod.Size = new System.Drawing.Size(419, 241);
			// 
			// flpGrpDispMethod
			// 
			this.flpGrpDispMethod.Controls.Add(this.flpD3DSection);
			this.flpGrpDispMethod.Controls.Add(this.rbDispMethodOpenGL);
			this.flpGrpDispMethod.Controls.Add(this.lblDispMethodOpenGL);
			this.flpGrpDispMethod.Controls.Add(this.rbDispMethodGDIPlus);
			this.flpGrpDispMethod.Controls.Add(this.lblDispMethodGDIPlus);
			this.flpGrpDispMethod.Location = new System.Drawing.Point(6, 11);
			this.flpGrpDispMethod.Name = "flpGrpDispMethod";
			// 
			// flpD3DSection
			// 
			this.flpD3DSection.Controls.Add(this.rbDispMethodD3D);
			this.flpD3DSection.Controls.Add(this.lblDispMethodD3D);
			this.flpD3DSection.Controls.Add(this.flpD3DAltVSync);
			this.flpD3DSection.Name = "flpD3DSection";
			// 
			// rbDispMethodD3D
			// 
			this.rbDispMethodD3D.Checked = true;
			this.rbDispMethodD3D.Name = "rbDispMethodD3D";
			this.rbDispMethodD3D.Text = "Direct3D9";
			// 
			// lblDispMethodD3D
			// 
			this.lblDispMethodD3D.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblDispMethodD3D.Location = new System.Drawing.Point(3, 23);
			this.lblDispMethodD3D.Name = "lblDispMethodD3D";
			this.lblDispMethodD3D.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblDispMethodD3D.Text = " • Best compatibility\r\n • May have trouble with OpenGL-based cores (N64)\r\n";
			// 
			// flpD3DAltVSync
			// 
			this.flpD3DAltVSync.Controls.Add(this.cbD3DAltVSync);
			this.flpD3DAltVSync.Controls.Add(this.lblD3DAltVSync);
			this.flpD3DAltVSync.Name = "flpD3DAltVSync";
			this.flpD3DAltVSync.Padding = new System.Windows.Forms.Padding(32, 0, 0, 0);
			// 
			// cbD3DAltVSync
			// 
			this.cbD3DAltVSync.Name = "cbD3DAltVSync";
			// 
			// lblD3DAltVSync
			// 
			this.lblD3DAltVSync.Name = "lblD3DAltVSync";
			this.lblD3DAltVSync.Size = new System.Drawing.Size(359, 43);
			this.lblD3DAltVSync.Text = resources.GetString("lblD3DAltVSync.Text");
			this.lblD3DAltVSync.Click += new System.EventHandler(this.lblD3DAltVSync_Click);
			this.lblD3DAltVSync.DoubleClick += new System.EventHandler(this.lblD3DAltVSync_Click);
			// 
			// rbDispMethodGDIPlus
			// 
			this.rbDispMethodGDIPlus.Checked = true;
			this.rbDispMethodGDIPlus.Name = "rbDispMethodGDIPlus";
			this.rbDispMethodGDIPlus.Text = "GDI+";
			// 
			// lblDispMethodGDIPlus
			// 
			this.lblDispMethodGDIPlus.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblDispMethodGDIPlus.Location = new System.Drawing.Point(3, 177);
			this.lblDispMethodGDIPlus.Name = "lblDispMethodGDIPlus";
			this.lblDispMethodGDIPlus.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblDispMethodGDIPlus.Text = " • Slow; Mainly for compatibility purposes\r\n • Missing many features\r\n • Works be" +
    "tter over Remote Desktop, etc.\r\n";
			// 
			// lblDispMethodRestartWarning
			// 
			this.lblDispMethodRestartWarning.Name = "lblDispMethodRestartWarning";
			this.lblDispMethodRestartWarning.Text = "Changes require restart of program to take effect.\r\n";
			// 
			// tpMisc
			// 
			this.tpMisc.Controls.Add(this.grpDispFeatures);
			this.tpMisc.Name = "tpMisc";
			this.tpMisc.Text = "Misc";
			// 
			// grpDispFeatures
			// 
			this.grpDispFeatures.Controls.Add(this.flpGrpDispFeatures);
			this.grpDispFeatures.Name = "grpDispFeatures";
			this.grpDispFeatures.Size = new System.Drawing.Size(217, 87);
			this.grpDispFeatures.Text = "Display Features (for speeding up replays)";
			// 
			// flpGrpDispFeatures
			// 
			this.flpGrpDispFeatures.Controls.Add(this.rbDispFeaturesFull);
			this.flpGrpDispFeatures.Controls.Add(this.rbDispFeaturesMinimal);
			this.flpGrpDispFeatures.Controls.Add(this.rbDispFeaturesNothing);
			this.flpGrpDispFeatures.Location = new System.Drawing.Point(6, 15);
			this.flpGrpDispFeatures.Name = "flpGrpDispFeatures";
			// 
			// rbDispFeaturesFull
			// 
			this.rbDispFeaturesFull.Name = "rbDispFeaturesFull";
			this.rbDispFeaturesFull.Text = "Full - Display Everything";
			// 
			// rbDispFeaturesMinimal
			// 
			this.rbDispFeaturesMinimal.Enabled = false;
			this.rbDispFeaturesMinimal.Name = "rbDispFeaturesMinimal";
			this.rbDispFeaturesMinimal.Text = "Minimal - Display HUD Only (TBD)";
			// 
			// rbDispFeaturesNothing
			// 
			this.rbDispFeaturesNothing.Name = "rbDispFeaturesNothing";
			this.rbDispFeaturesNothing.Text = "Absolute Zero - Display Nothing";
			// 
			// tpWindow
			// 
			this.tpWindow.Controls.Add(this.flpTpWindow);
			this.tpWindow.Name = "tpWindow";
			this.tpWindow.Padding = new System.Windows.Forms.Padding(3);
			this.tpWindow.Text = "Window";
			// 
			// flpTpWindow
			// 
			this.flpTpWindow.Controls.Add(this.flpWindowFSGroups);
			this.flpTpWindow.Controls.Add(this.cbDoubleClickFS);
			this.flpTpWindow.Name = "flpTpWindow";
			// 
			// flpWindowFSGroups
			// 
			this.flpWindowFSGroups.Controls.Add(this.grpWindowed);
			this.flpWindowFSGroups.Controls.Add(this.grpFS);
			this.flpWindowFSGroups.Name = "flpWindowFSGroups";
			// 
			// grpWindowed
			// 
			this.grpWindowed.Controls.Add(this.flpWindowed);
			this.grpWindowed.Name = "grpWindowed";
			this.grpWindowed.Size = new System.Drawing.Size(121, 168);
			this.grpWindowed.Text = "Windowed";
			// 
			// flpWindowed
			// 
			this.flpWindowed.Controls.Add(this.flpWindowedFrameType);
			this.flpWindowed.Controls.Add(this.tbWindowedFrameType);
			this.flpWindowed.Controls.Add(this.cbWindowedStatusBar);
			this.flpWindowed.Controls.Add(this.cbWindowedCaption);
			this.flpWindowed.Controls.Add(this.cbWindowedMenu);
			this.flpWindowed.Location = new System.Drawing.Point(6, 15);
			this.flpWindowed.Name = "flpWindowed";
			// 
			// flpWindowedFrameType
			// 
			this.flpWindowedFrameType.Controls.Add(this.lblWindowedFrameTypeDesc);
			this.flpWindowedFrameType.Controls.Add(this.lblWindowedFrameTypeReadout);
			this.flpWindowedFrameType.Name = "flpWindowedFrameType";
			// 
			// lblWindowedFrameTypeDesc
			// 
			this.lblWindowedFrameTypeDesc.Name = "lblWindowedFrameTypeDesc";
			this.lblWindowedFrameTypeDesc.Text = "Frame:";
			// 
			// lblWindowedFrameTypeReadout
			// 
			this.lblWindowedFrameTypeReadout.Name = "lblWindowedFrameTypeReadout";
			this.lblWindowedFrameTypeReadout.Text = "(frame type)";
			// 
			// tbWindowedFrameType
			// 
			this.tbWindowedFrameType.LargeChange = 1;
			this.tbWindowedFrameType.Location = new System.Drawing.Point(3, 27);
			this.tbWindowedFrameType.Maximum = 2;
			this.tbWindowedFrameType.Name = "tbWindowedFrameType";
			this.tbWindowedFrameType.Size = new System.Drawing.Size(99, 45);
			this.tbWindowedFrameType.TabIndex = 21;
			this.tbWindowedFrameType.Value = 1;
			this.tbWindowedFrameType.ValueChanged += new System.EventHandler(this.tbWidowedFrameType_ValueChanged);
			// 
			// cbWindowedStatusBar
			// 
			this.cbWindowedStatusBar.Name = "cbWindowedStatusBar";
			this.cbWindowedStatusBar.Text = "Status Bar";
			// 
			// cbWindowedCaption
			// 
			this.cbWindowedCaption.Name = "cbWindowedCaption";
			this.cbWindowedCaption.Text = "Caption";
			// 
			// cbWindowedMenu
			// 
			this.cbWindowedMenu.Name = "cbWindowedMenu";
			this.cbWindowedMenu.Text = "Menu";
			// 
			// grpFS
			// 
			this.grpFS.Controls.Add(this.flpGrpFS);
			this.grpFS.Name = "grpFS";
			this.grpFS.Size = new System.Drawing.Size(344, 168);
			this.grpFS.Text = "Fullscreen";
			// 
			// flpGrpFS
			// 
			this.flpGrpFS.Controls.Add(this.flpFSCheckBoxes);
			this.flpGrpFS.Controls.Add(this.lblFSWinHacks);
			this.flpGrpFS.Location = new System.Drawing.Point(6, 15);
			this.flpGrpFS.Name = "flpGrpFS";
			// 
			// flpFSCheckBoxes
			// 
			this.flpFSCheckBoxes.Controls.Add(this.cbFSStatusBar);
			this.flpFSCheckBoxes.Controls.Add(this.cbFSAutohideMouse);
			this.flpFSCheckBoxes.Controls.Add(this.cbFSMenu);
			this.flpFSCheckBoxes.Controls.Add(this.cbFSWinHacks);
			this.flpFSCheckBoxes.MinimumSize = new System.Drawing.Size(24, 24);
			this.flpFSCheckBoxes.Name = "flpFSCheckBoxes";
			this.flpFSCheckBoxes.Size = new System.Drawing.Size(228, 72);
			// 
			// cbFSStatusBar
			// 
			this.cbFSStatusBar.Name = "cbFSStatusBar";
			this.cbFSStatusBar.Text = "Status Bar";
			// 
			// cbFSAutohideMouse
			// 
			this.cbFSAutohideMouse.Name = "cbFSAutohideMouse";
			this.cbFSAutohideMouse.Text = "Auto-Hide Mouse Cursor";
			// 
			// cbFSMenu
			// 
			this.cbFSMenu.Name = "cbFSMenu";
			this.cbFSMenu.Text = "Menu";
			// 
			// cbFSWinHacks
			// 
			this.cbFSWinHacks.Name = "cbFSWinHacks";
			this.cbFSWinHacks.Text = "Enable Windows Fullscreen Hacks";
			// 
			// lblFSWinHacks
			// 
			this.lblFSWinHacks.Name = "lblFSWinHacks";
			this.lblFSWinHacks.Size = new System.Drawing.Size(329, 80);
			this.lblFSWinHacks.Text = resources.GetString("lblFSWinHacks.Text");
			// 
			// cbDoubleClickFS
			// 
			this.cbDoubleClickFS.Name = "cbDoubleClickFS";
			this.cbDoubleClickFS.Text = "Allow Double-Click Fullscreen (hold shift to force fullscreen to toggle in case u" +
    "sing zapper, etc.)";
			// 
			// lnkDocs
			// 
			this.lnkDocs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lnkDocs.Location = new System.Drawing.Point(11, 359);
			this.lnkDocs.Name = "lnkDocs";
			this.lnkDocs.Text = "Documentation";
			this.lnkDocs.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkDocs_LinkClicked);
			// 
			// flpDialogButtons
			// 
			this.flpDialogButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.flpDialogButtons.Controls.Add(this.btnDialogOK);
			this.flpDialogButtons.Controls.Add(this.btnDialogCancel);
			this.flpDialogButtons.Location = new System.Drawing.Point(362, 351);
			this.flpDialogButtons.Name = "flpDialogButtons";
			this.flpDialogButtons.Size = new System.Drawing.Size(162, 29);
			// 
			// DisplayConfig
			// 
			this.AcceptButton = this.btnDialogOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnDialogCancel;
			this.ClientSize = new System.Drawing.Size(530, 385);
			this.Controls.Add(this.flpDialogButtons);
			this.Controls.Add(this.lnkDocs);
			this.Controls.Add(this.tcDialog);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "DisplayConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Display Configuration";
			this.tcDialog.ResumeLayout(false);
			this.tpScaling.ResumeLayout(false);
			this.flpTpScaling.ResumeLayout(false);
			this.flpTpScaling.PerformLayout();
			this.flpUserPrescale.ResumeLayout(false);
			this.flpUserPrescale.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudUserPrescale)).EndInit();
			this.grpFilter.ResumeLayout(false);
			this.tlpGrpFilter.ResumeLayout(false);
			this.tlpGrpFilter.PerformLayout();
			this.flpFilterUser.ResumeLayout(false);
			this.flpFilterUser.PerformLayout();
			this.flpFilterScanlineAlpha.ResumeLayout(false);
			this.flpFilterScanlineAlpha.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbFilterScanlineAlpha)).EndInit();
			this.grpFinalFilter.ResumeLayout(false);
			this.grpFinalFilter.PerformLayout();
			this.flpGrpFinalFilter.ResumeLayout(false);
			this.flpGrpFinalFilter.PerformLayout();
			this.grpAspectRatio.ResumeLayout(false);
			this.grpAspectRatio.PerformLayout();
			this.flpGrpAspectRatio.ResumeLayout(false);
			this.flpGrpAspectRatio.PerformLayout();
			this.flpCustomSize.ResumeLayout(false);
			this.flpCustomSize.PerformLayout();
			this.flpCustomAR.ResumeLayout(false);
			this.flpCustomAR.PerformLayout();
			this.grpCrop.ResumeLayout(false);
			this.grpCrop.PerformLayout();
			this.flpGrpCrop.ResumeLayout(false);
			this.flpGrpCrop.PerformLayout();
			this.tpDispMethod.ResumeLayout(false);
			this.tpDispMethod.PerformLayout();
			this.flpTpDispMethod.ResumeLayout(false);
			this.flpTpDispMethod.PerformLayout();
			this.grpDispMethod.ResumeLayout(false);
			this.grpDispMethod.PerformLayout();
			this.flpGrpDispMethod.ResumeLayout(false);
			this.flpGrpDispMethod.PerformLayout();
			this.flpD3DSection.ResumeLayout(false);
			this.flpD3DSection.PerformLayout();
			this.flpD3DAltVSync.ResumeLayout(false);
			this.flpD3DAltVSync.PerformLayout();
			this.tpMisc.ResumeLayout(false);
			this.grpDispFeatures.ResumeLayout(false);
			this.grpDispFeatures.PerformLayout();
			this.flpGrpDispFeatures.ResumeLayout(false);
			this.flpGrpDispFeatures.PerformLayout();
			this.tpWindow.ResumeLayout(false);
			this.tpWindow.PerformLayout();
			this.flpTpWindow.ResumeLayout(false);
			this.flpTpWindow.PerformLayout();
			this.flpWindowFSGroups.ResumeLayout(false);
			this.grpWindowed.ResumeLayout(false);
			this.grpWindowed.PerformLayout();
			this.flpWindowed.ResumeLayout(false);
			this.flpWindowed.PerformLayout();
			this.flpWindowedFrameType.ResumeLayout(false);
			this.flpWindowedFrameType.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbWindowedFrameType)).EndInit();
			this.grpFS.ResumeLayout(false);
			this.grpFS.PerformLayout();
			this.flpGrpFS.ResumeLayout(false);
			this.flpFSCheckBoxes.ResumeLayout(false);
			this.flpFSCheckBoxes.PerformLayout();
			this.flpDialogButtons.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.SzButtonEx btnDialogCancel;
		private BizHawk.WinForms.Controls.SzButtonEx btnDialogOK;
		private BizHawk.WinForms.Controls.RadioButtonEx rbDispMethodOpenGL;
		private BizHawk.WinForms.Controls.LocLabelEx lblDispMethodOpenGL;
		private System.Windows.Forms.TabControl tcDialog;
		private BizHawk.WinForms.Controls.TabPageEx tpScaling;
		private BizHawk.WinForms.Controls.TabPageEx tpDispMethod;
		private BizHawk.WinForms.Controls.LabelEx lblDispMethodRestartWarning;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpDispMethod;
		private BizHawk.WinForms.Controls.LocLabelEx lblDispMethodGDIPlus;
		private BizHawk.WinForms.Controls.RadioButtonEx rbDispMethodGDIPlus;
		private BizHawk.WinForms.Controls.TabPageEx tpMisc;
		private BizHawk.WinForms.Controls.TabPageEx tpWindow;
		private BizHawk.WinForms.Controls.CheckBoxEx cbWindowedStatusBar;
		private BizHawk.WinForms.Controls.LabelEx lblWindowedFrameTypeDesc;
		private BizHawk.Client.EmuHawk.TransparentTrackBar tbWindowedFrameType;
		private BizHawk.WinForms.Controls.CheckBoxEx cbWindowedMenu;
		private BizHawk.WinForms.Controls.CheckBoxEx cbWindowedCaption;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpFS;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpWindowed;
		private BizHawk.WinForms.Controls.LabelEx lblWindowedFrameTypeReadout;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpDispFeatures;
		private BizHawk.WinForms.Controls.RadioButtonEx rbDispFeaturesNothing;
		private BizHawk.WinForms.Controls.RadioButtonEx rbDispFeaturesMinimal;
		private BizHawk.WinForms.Controls.RadioButtonEx rbDispFeaturesFull;
		private BizHawk.WinForms.Controls.CheckBoxEx cbDoubleClickFS;
		private BizHawk.WinForms.Controls.LocLinkLabelEx lnkDocs;
		private System.Windows.Forms.ToolTip toolTip1;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpDispFeatures;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpWindowed;
		private BizHawk.WinForms.Controls.SingleRowFLP flpWindowedFrameType;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpTpWindow;
		private BizHawk.WinForms.Controls.SingleRowFLP flpWindowFSGroups;
		private BizHawk.WinForms.Controls.LocSzSingleRowFLP flpDialogButtons;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpFS;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpTpDispMethod;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpDispMethod;
		private BizHawk.WinForms.Controls.SzColumnsToRightFLP flpTpScaling;
		private BizHawk.WinForms.Controls.SingleRowFLP flpUserPrescale;
		private BizHawk.WinForms.Controls.LabelEx lblUserPrescale;
		private BizHawk.WinForms.Controls.SzNUDEx nudUserPrescale;
		private BizHawk.WinForms.Controls.LabelEx lblUserPrescaleUnits;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpFilter;
		private System.Windows.Forms.TableLayoutPanel tlpGrpFilter;
		private BizHawk.WinForms.Controls.RadioButtonEx rbFilterNone;
		private BizHawk.WinForms.Controls.RadioButtonEx rbFilterUser;
		private BizHawk.WinForms.Controls.RadioButtonEx rbFilterHq2x;
		private BizHawk.WinForms.Controls.RadioButtonEx rbFilterScanline;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpFilterUser;
		private BizHawk.WinForms.Controls.SzButtonEx btnFilterUser;
		private BizHawk.WinForms.Controls.LabelEx lblFilterUser;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpFilterScanlineAlpha;
		private BizHawk.WinForms.Controls.LabelEx lblFilterScanlineAlpha;
		private TransparentTrackBar tbFilterScanlineAlpha;
		private BizHawk.WinForms.Controls.CheckBoxEx cbAutoPrescale;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpFinalFilter;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpFinalFilter;
		private BizHawk.WinForms.Controls.RadioButtonEx rbFinalFilterNone;
		private BizHawk.WinForms.Controls.RadioButtonEx rbFinalFilterBilinear;
		private BizHawk.WinForms.Controls.RadioButtonEx rbFinalFilterBicubic;
		private BizHawk.WinForms.Controls.CheckBoxEx cbLetterbox;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpAspectRatio;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpAspectRatio;
		private BizHawk.WinForms.Controls.RadioButtonEx rbARSquare;
		private BizHawk.WinForms.Controls.LabelEx lblAspectRatioNonSquare;
		private BizHawk.WinForms.Controls.RadioButtonEx rbARBySystem;
		private BizHawk.WinForms.Controls.SingleRowFLP flpCustomSize;
		private BizHawk.WinForms.Controls.RadioButtonEx rbARCustomSize;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtARCustomWidth;
		private BizHawk.WinForms.Controls.LabelEx lblARCustomSizeSeparator;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtARCustomHeight;
		private BizHawk.WinForms.Controls.SingleRowFLP flpCustomAR;
		private BizHawk.WinForms.Controls.RadioButtonEx rbARCustomRatio;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtARCustomRatioH;
		private BizHawk.WinForms.Controls.LabelEx lblARCustomRatioSeparator;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtARCustomRatioV;
		private BizHawk.WinForms.Controls.CheckBoxEx cbScaleByInteger;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpCrop;
		private BizHawk.WinForms.Controls.LocSingleRowFLP flpGrpCrop;
		private BizHawk.WinForms.Controls.LabelEx lblCropLeft;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtCropLeft;
		private BizHawk.WinForms.Controls.LabelEx lblCropTop;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtCropTop;
		private BizHawk.WinForms.Controls.LabelEx lblCropRight;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtCropRight;
		private BizHawk.WinForms.Controls.LabelEx lblCropBottom;
		private BizHawk.WinForms.Controls.SzTextBoxEx txtCropBottom;
		private BizHawk.WinForms.Controls.LocSzButtonEx btnDefaults;
		private BizHawk.WinForms.Controls.SzRowsToBottomFLP flpFSCheckBoxes;
		private BizHawk.WinForms.Controls.CheckBoxEx cbFSStatusBar;
		private BizHawk.WinForms.Controls.CheckBoxEx cbFSAutohideMouse;
		private BizHawk.WinForms.Controls.CheckBoxEx cbFSMenu;
		private BizHawk.WinForms.Controls.CheckBoxEx cbFSWinHacks;
		private BizHawk.WinForms.Controls.SzLabelEx lblFSWinHacks;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpD3DSection;
		private BizHawk.WinForms.Controls.RadioButtonEx rbDispMethodD3D;
		private BizHawk.WinForms.Controls.LocLabelEx lblDispMethodD3D;
		private BizHawk.WinForms.Controls.SingleRowFLP flpD3DAltVSync;
		private BizHawk.WinForms.Controls.CheckBoxEx cbD3DAltVSync;
		private BizHawk.WinForms.Controls.SzLabelEx lblD3DAltVSync;
	}
}
