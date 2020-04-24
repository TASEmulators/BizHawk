namespace BizHawk.Client.EmuHawk
{
	partial class EmuHawkOptions
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
			this.btnDialogOK = new BizHawk.WinForms.Controls.LocSzButtonEx();
			this.btnDialogCancel = new BizHawk.WinForms.Controls.LocSzButtonEx();
			this.tcDialog = new System.Windows.Forms.TabControl();
			this.tpGeneral = new BizHawk.WinForms.Controls.TabPageEx();
			this.flpTpGeneral = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbMenusPauseEmulation = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbEnableContextMenu = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbSaveWindowPosition = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbNeverAskForSave = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.flpNoFocusEmulate = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbNoFocusEmulate = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblNoFocusEmulate = new BizHawk.WinForms.Controls.LocLabelEx();
			this.flpNoFocusInput = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.flpNoFocusInputCheckBoxes = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.cbNoFocusInput = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbNoFocusInputGamepadOnly = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblNoFocusInput = new BizHawk.WinForms.Controls.LocLabelEx();
			this.cbNonQWERTY = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpStartup = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpStartup = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.cbStartPaused = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.cbStartInFS = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.flpSingleInstance = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbSingleInstance = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblSingleInstanceDesc = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblSingleInstanceRestartWarning = new BizHawk.WinForms.Controls.LocLabelEx();
			this.tpAdvanced = new BizHawk.WinForms.Controls.TabPageEx();
			this.flpTpAdvanced = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbBackupSaveRAM = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.flpAutoSaveRAM = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.cbAutoSaveRAM = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.grpAutoSaveRAM = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpAutoSaveRAM = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.lblAutoSaveRAM = new BizHawk.WinForms.Controls.LabelEx();
			this.flpAutoSaveRAMFreq = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.lblAutoSaveRAMFreqDesc = new BizHawk.WinForms.Controls.LabelEx();
			this.cbAutoSaveRAMFreq5s = new BizHawk.WinForms.Controls.RadioButtonEx(grpAutoSaveRAM.Tracker);
			this.AutoSaveRAMFreq5min = new BizHawk.WinForms.Controls.RadioButtonEx(grpAutoSaveRAM.Tracker);
			this.flpAutoSaveRAMFreqCustom = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.rbAutoSaveRAMFreqCustom = new BizHawk.WinForms.Controls.RadioButtonEx(grpAutoSaveRAM.Tracker);
			this.nudAutoSaveRAMFreqCustom = new BizHawk.WinForms.Controls.SzNUDEx();
			this.lblAutoSaveRAMFreqCustomUnits = new BizHawk.WinForms.Controls.LabelEx();
			this.flpFrameAdvPastLag = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbFrameAdvPastLag = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblFrameAdvPastLag1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.lblFrameAdvPastLag2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.cbRunLuaDuringTurbo = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.flpMoviesOnDisk = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbMoviesOnDisk = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblMoviesOnDisk = new BizHawk.WinForms.Controls.LocLabelEx();
			this.flpMoviesInAWE = new BizHawk.WinForms.Controls.SingleColumnFLP();
			this.cbMoviesInAWE = new BizHawk.WinForms.Controls.CheckBoxEx();
			this.lblMoviesInAWE = new BizHawk.WinForms.Controls.LocLabelEx();
			this.grpLuaEngine = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpLuaEngine = new BizHawk.WinForms.Controls.LocSingleColumnFLP();
			this.rbKopiLua = new BizHawk.WinForms.Controls.RadioButtonEx(grpLuaEngine.Tracker);
			this.rbLuaInterface = new BizHawk.WinForms.Controls.RadioButtonEx(grpLuaEngine.Tracker);
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.flpDialogButtons = new BizHawk.WinForms.Controls.LocSzSingleRowFLP();
			this.grpInputMethod = new BizHawk.WinForms.Controls.SzGroupBoxEx();
			this.flpGrpInputMethod = new BizHawk.WinForms.Controls.LocSingleRowFLP();
			this.rbInputMethodOpenTK = new BizHawk.WinForms.Controls.RadioButtonEx(grpInputMethod.Tracker);
			this.rbInputMethodDirectInput = new BizHawk.WinForms.Controls.RadioButtonEx(grpInputMethod.Tracker);
			this.tcDialog.SuspendLayout();
			this.tpGeneral.SuspendLayout();
			this.flpTpGeneral.SuspendLayout();
			this.flpNoFocusEmulate.SuspendLayout();
			this.flpNoFocusInput.SuspendLayout();
			this.flpNoFocusInputCheckBoxes.SuspendLayout();
			this.grpStartup.SuspendLayout();
			this.flpGrpStartup.SuspendLayout();
			this.flpSingleInstance.SuspendLayout();
			this.tpAdvanced.SuspendLayout();
			this.flpTpAdvanced.SuspendLayout();
			this.flpAutoSaveRAM.SuspendLayout();
			this.grpAutoSaveRAM.SuspendLayout();
			this.flpGrpAutoSaveRAM.SuspendLayout();
			this.flpAutoSaveRAMFreq.SuspendLayout();
			this.flpAutoSaveRAMFreqCustom.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudAutoSaveRAMFreqCustom)).BeginInit();
			this.flpFrameAdvPastLag.SuspendLayout();
			this.flpMoviesOnDisk.SuspendLayout();
			this.flpMoviesInAWE.SuspendLayout();
			this.grpLuaEngine.SuspendLayout();
			this.flpGrpLuaEngine.SuspendLayout();
			this.flpDialogButtons.SuspendLayout();
			this.grpInputMethod.SuspendLayout();
			this.flpGrpInputMethod.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnDialogOK
			// 
			this.btnDialogOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDialogOK.Location = new System.Drawing.Point(3, 3);
			this.btnDialogOK.Name = "btnDialogOK";
			this.btnDialogOK.Size = new System.Drawing.Size(60, 23);
			this.btnDialogOK.Text = "&OK";
			this.btnDialogOK.Click += new System.EventHandler(this.btnDialogOK_Click);
			// 
			// btnDialogCancel
			// 
			this.btnDialogCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDialogCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnDialogCancel.Location = new System.Drawing.Point(69, 3);
			this.btnDialogCancel.Name = "btnDialogCancel";
			this.btnDialogCancel.Size = new System.Drawing.Size(60, 23);
			this.btnDialogCancel.Text = "&Cancel";
			this.btnDialogCancel.Click += new System.EventHandler(this.btnDialogCancel_Click);
			// 
			// tcDialog
			// 
			this.tcDialog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tcDialog.Controls.Add(this.tpGeneral);
			this.tcDialog.Controls.Add(this.tpAdvanced);
			this.tcDialog.Location = new System.Drawing.Point(4, 4);
			this.tcDialog.Name = "tcDialog";
			this.tcDialog.SelectedIndex = 0;
			this.tcDialog.Size = new System.Drawing.Size(379, 389);
			this.tcDialog.TabIndex = 0;
			// 
			// tpGeneral
			// 
			this.tpGeneral.Controls.Add(this.flpTpGeneral);
			this.tpGeneral.Name = "tpGeneral";
			this.tpGeneral.Padding = new System.Windows.Forms.Padding(3);
			this.tpGeneral.Text = "General";
			// 
			// flpTpGeneral
			// 
			this.flpTpGeneral.Controls.Add(this.cbMenusPauseEmulation);
			this.flpTpGeneral.Controls.Add(this.cbEnableContextMenu);
			this.flpTpGeneral.Controls.Add(this.cbSaveWindowPosition);
			this.flpTpGeneral.Controls.Add(this.cbNeverAskForSave);
			this.flpTpGeneral.Controls.Add(this.flpNoFocusEmulate);
			this.flpTpGeneral.Controls.Add(this.flpNoFocusInput);
			this.flpTpGeneral.Controls.Add(this.grpInputMethod);
			this.flpTpGeneral.Controls.Add(this.cbNonQWERTY);
			this.flpTpGeneral.Controls.Add(this.grpStartup);
			this.flpTpGeneral.Name = "flpTpGeneral";
			// 
			// cbMenusPauseEmulation
			// 
			this.cbMenusPauseEmulation.Name = "cbMenusPauseEmulation";
			this.cbMenusPauseEmulation.Text = "Pause when menu activated";
			// 
			// cbEnableContextMenu
			// 
			this.cbEnableContextMenu.Name = "cbEnableContextMenu";
			this.cbEnableContextMenu.Text = "Enable Context Menu";
			// 
			// cbSaveWindowPosition
			// 
			this.cbSaveWindowPosition.Name = "cbSaveWindowPosition";
			this.cbSaveWindowPosition.Text = "Save Window Position";
			// 
			// cbNeverAskForSave
			// 
			this.cbNeverAskForSave.Name = "cbNeverAskForSave";
			this.cbNeverAskForSave.Text = "Never be asked to save changes";
			// 
			// flpNoFocusEmulate
			// 
			this.flpNoFocusEmulate.Controls.Add(this.cbNoFocusEmulate);
			this.flpNoFocusEmulate.Controls.Add(this.lblNoFocusEmulate);
			this.flpNoFocusEmulate.Name = "flpNoFocusEmulate";
			// 
			// cbNoFocusEmulate
			// 
			this.cbNoFocusEmulate.Name = "cbNoFocusEmulate";
			this.cbNoFocusEmulate.Text = "Run in background";
			// 
			// lblNoFocusEmulate
			// 
			this.lblNoFocusEmulate.Location = new System.Drawing.Point(3, 23);
			this.lblNoFocusEmulate.Name = "lblNoFocusEmulate";
			this.lblNoFocusEmulate.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblNoFocusEmulate.Text = "When this is set, the client will continue to run when it loses focus";
			// 
			// flpNoFocusInput
			// 
			this.flpNoFocusInput.Controls.Add(this.flpNoFocusInputCheckBoxes);
			this.flpNoFocusInput.Controls.Add(this.lblNoFocusInput);
			this.flpNoFocusInput.Name = "flpNoFocusInput";
			// 
			// flpNoFocusInputCheckBoxes
			// 
			this.flpNoFocusInputCheckBoxes.Controls.Add(this.cbNoFocusInput);
			this.flpNoFocusInputCheckBoxes.Controls.Add(this.cbNoFocusInputGamepadOnly);
			this.flpNoFocusInputCheckBoxes.Name = "flpNoFocusInputCheckBoxes";
			// 
			// cbNoFocusInput
			// 
			this.cbNoFocusInput.Name = "cbNoFocusInput";
			this.cbNoFocusInput.Text = "Accept background input";
			this.cbNoFocusInput.CheckedChanged += new System.EventHandler(this.cbNoFocusInput_CheckedChanged);
			// 
			// cbNoFocusInputGamepadOnly
			// 
			this.cbNoFocusInputGamepadOnly.Enabled = false;
			this.cbNoFocusInputGamepadOnly.Name = "cbNoFocusInputGamepadOnly";
			this.cbNoFocusInputGamepadOnly.Text = "From controller only";
			// 
			// lblNoFocusInput
			// 
			this.lblNoFocusInput.Location = new System.Drawing.Point(3, 24);
			this.lblNoFocusInput.Name = "lblNoFocusInput";
			this.lblNoFocusInput.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblNoFocusInput.Text = "When this is set, the client will receive user input even when focus is lost";
			// 
			// cbNonQWERTY
			// 
			this.cbNonQWERTY.Name = "cbNonQWERTY";
			this.cbNonQWERTY.Text = "Handle alternate keyboard layouts (e.g. Dvorak) [experimental]";
			// 
			// grpStartup
			// 
			this.grpStartup.Controls.Add(this.flpGrpStartup);
			this.grpStartup.Name = "grpStartup";
			this.grpStartup.Size = new System.Drawing.Size(334, 118);
			this.grpStartup.Text = "Startup Options";
			// 
			// flpGrpStartup
			// 
			this.flpGrpStartup.Controls.Add(this.cbStartPaused);
			this.flpGrpStartup.Controls.Add(this.cbStartInFS);
			this.flpGrpStartup.Controls.Add(this.flpSingleInstance);
			this.flpGrpStartup.Location = new System.Drawing.Point(3, 16);
			this.flpGrpStartup.Name = "flpGrpStartup";
			// 
			// cbStartPaused
			// 
			this.cbStartPaused.Name = "cbStartPaused";
			this.cbStartPaused.Text = "Start paused";
			// 
			// cbStartInFS
			// 
			this.cbStartInFS.Name = "cbStartInFS";
			this.cbStartInFS.Text = "Start in Fullscreen";
			// 
			// flpSingleInstance
			// 
			this.flpSingleInstance.Controls.Add(this.cbSingleInstance);
			this.flpSingleInstance.Controls.Add(this.lblSingleInstanceDesc);
			this.flpSingleInstance.Controls.Add(this.lblSingleInstanceRestartWarning);
			this.flpSingleInstance.Name = "flpSingleInstance";
			// 
			// cbSingleInstance
			// 
			this.cbSingleInstance.Name = "cbSingleInstance";
			this.cbSingleInstance.Text = "Single instance mode";
			// 
			// lblSingleInstanceDesc
			// 
			this.lblSingleInstanceDesc.Location = new System.Drawing.Point(3, 23);
			this.lblSingleInstanceDesc.Name = "lblSingleInstanceDesc";
			this.lblSingleInstanceDesc.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblSingleInstanceDesc.Text = "Enable to force only one instance of EmuHawk at a time.";
			// 
			// lblSingleInstanceRestartWarning
			// 
			this.lblSingleInstanceRestartWarning.Location = new System.Drawing.Point(3, 36);
			this.lblSingleInstanceRestartWarning.Name = "lblSingleInstanceRestartWarning";
			this.lblSingleInstanceRestartWarning.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblSingleInstanceRestartWarning.Text = "Note: Requires closing and reopening EmuHawk to take effect.";
			// 
			// tpAdvanced
			// 
			this.tpAdvanced.Controls.Add(this.flpTpAdvanced);
			this.tpAdvanced.Name = "tpAdvanced";
			this.tpAdvanced.Text = "Advanced";
			// 
			// flpTpAdvanced
			// 
			this.flpTpAdvanced.Controls.Add(this.cbBackupSaveRAM);
			this.flpTpAdvanced.Controls.Add(this.flpAutoSaveRAM);
			this.flpTpAdvanced.Controls.Add(this.flpFrameAdvPastLag);
			this.flpTpAdvanced.Controls.Add(this.cbRunLuaDuringTurbo);
			this.flpTpAdvanced.Controls.Add(this.flpMoviesOnDisk);
			this.flpTpAdvanced.Controls.Add(this.flpMoviesInAWE);
			this.flpTpAdvanced.Controls.Add(this.grpLuaEngine);
			this.flpTpAdvanced.Name = "flpTpAdvanced";
			// 
			// cbBackupSaveRAM
			// 
			this.cbBackupSaveRAM.Name = "cbBackupSaveRAM";
			this.cbBackupSaveRAM.Text = "Backup SaveRAM to .SaveRAM.bak";
			// 
			// flpAutoSaveRAM
			// 
			this.flpAutoSaveRAM.Controls.Add(this.cbAutoSaveRAM);
			this.flpAutoSaveRAM.Controls.Add(this.grpAutoSaveRAM);
			this.flpAutoSaveRAM.Name = "flpAutoSaveRAM";
			// 
			// cbAutoSaveRAM
			// 
			this.cbAutoSaveRAM.Name = "cbAutoSaveRAM";
			this.cbAutoSaveRAM.CheckedChanged += new System.EventHandler(this.cbAutoSaveRAM_CheckedChanged);
			// 
			// grpAutoSaveRAM
			// 
			this.grpAutoSaveRAM.Controls.Add(this.flpGrpAutoSaveRAM);
			this.grpAutoSaveRAM.Name = "grpAutoSaveRAM";
			this.grpAutoSaveRAM.Size = new System.Drawing.Size(238, 54);
			this.grpAutoSaveRAM.Text = "AutoSaveRAM";
			// 
			// flpGrpAutoSaveRAM
			// 
			this.flpGrpAutoSaveRAM.Controls.Add(this.lblAutoSaveRAM);
			this.flpGrpAutoSaveRAM.Controls.Add(this.flpAutoSaveRAMFreq);
			this.flpGrpAutoSaveRAM.Location = new System.Drawing.Point(3, 12);
			this.flpGrpAutoSaveRAM.Name = "flpGrpAutoSaveRAM";
			// 
			// lblAutoSaveRAM
			// 
			this.lblAutoSaveRAM.Name = "lblAutoSaveRAM";
			this.lblAutoSaveRAM.Text = "Save SaveRAM to .AutoSaveRAM.SaveRAM";
			// 
			// flpAutoSaveRAMFreq
			// 
			this.flpAutoSaveRAMFreq.Controls.Add(this.lblAutoSaveRAMFreqDesc);
			this.flpAutoSaveRAMFreq.Controls.Add(this.cbAutoSaveRAMFreq5s);
			this.flpAutoSaveRAMFreq.Controls.Add(this.AutoSaveRAMFreq5min);
			this.flpAutoSaveRAMFreq.Controls.Add(this.flpAutoSaveRAMFreqCustom);
			this.flpAutoSaveRAMFreq.Name = "flpAutoSaveRAMFreq";
			// 
			// lblAutoSaveRAMFreqDesc
			// 
			this.lblAutoSaveRAMFreqDesc.Name = "lblAutoSaveRAMFreqDesc";
			this.lblAutoSaveRAMFreqDesc.Text = "every";
			// 
			// cbAutoSaveRAMFreq5s
			// 
			this.cbAutoSaveRAMFreq5s.Name = "cbAutoSaveRAMFreq5s";
			this.cbAutoSaveRAMFreq5s.Text = "5s";
			// 
			// AutoSaveRAMFreq5min
			// 
			this.AutoSaveRAMFreq5min.Name = "AutoSaveRAMFreq5min";
			this.AutoSaveRAMFreq5min.Text = "5m";
			// 
			// flpAutoSaveRAMFreqCustom
			// 
			this.flpAutoSaveRAMFreqCustom.Controls.Add(this.rbAutoSaveRAMFreqCustom);
			this.flpAutoSaveRAMFreqCustom.Controls.Add(this.nudAutoSaveRAMFreqCustom);
			this.flpAutoSaveRAMFreqCustom.Controls.Add(this.lblAutoSaveRAMFreqCustomUnits);
			this.flpAutoSaveRAMFreqCustom.Name = "flpAutoSaveRAMFreqCustom";
			// 
			// rbAutoSaveRAMFreqCustom
			// 
			this.rbAutoSaveRAMFreqCustom.Name = "rbAutoSaveRAMFreqCustom";
			this.rbAutoSaveRAMFreqCustom.CheckedChanged += new System.EventHandler(this.rbAutoSaveRAMFreqCustom_CheckedChanged);
			// 
			// nudAutoSaveRAMFreqCustom
			// 
			this.nudAutoSaveRAMFreqCustom.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
			this.nudAutoSaveRAMFreqCustom.Name = "nudAutoSaveRAMFreqCustom";
			this.nudAutoSaveRAMFreqCustom.Size = new System.Drawing.Size(50, 20);
			// 
			// lblAutoSaveRAMFreqCustomUnits
			// 
			this.lblAutoSaveRAMFreqCustomUnits.Name = "lblAutoSaveRAMFreqCustomUnits";
			this.lblAutoSaveRAMFreqCustomUnits.Text = "s";
			// 
			// flpFrameAdvPastLag
			// 
			this.flpFrameAdvPastLag.Controls.Add(this.cbFrameAdvPastLag);
			this.flpFrameAdvPastLag.Controls.Add(this.lblFrameAdvPastLag1);
			this.flpFrameAdvPastLag.Controls.Add(this.lblFrameAdvPastLag2);
			this.flpFrameAdvPastLag.Name = "flpFrameAdvPastLag";
			// 
			// cbFrameAdvPastLag
			// 
			this.cbFrameAdvPastLag.Name = "cbFrameAdvPastLag";
			this.cbFrameAdvPastLag.Text = "Frame advance button skips non-input frames";
			// 
			// lblFrameAdvPastLag1
			// 
			this.lblFrameAdvPastLag1.Location = new System.Drawing.Point(3, 23);
			this.lblFrameAdvPastLag1.Name = "lblFrameAdvPastLag1";
			this.lblFrameAdvPastLag1.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblFrameAdvPastLag1.Text = "When enabled, the frame advance button will skip over";
			// 
			// lblFrameAdvPastLag2
			// 
			this.lblFrameAdvPastLag2.Location = new System.Drawing.Point(3, 36);
			this.lblFrameAdvPastLag2.Name = "lblFrameAdvPastLag2";
			this.lblFrameAdvPastLag2.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblFrameAdvPastLag2.Text = "frames in which no input was polled (lag frames)";
			// 
			// cbRunLuaDuringTurbo
			// 
			this.cbRunLuaDuringTurbo.Name = "cbRunLuaDuringTurbo";
			this.cbRunLuaDuringTurbo.Text = "Run lua scripts when turboing";
			// 
			// flpMoviesOnDisk
			// 
			this.flpMoviesOnDisk.Controls.Add(this.cbMoviesOnDisk);
			this.flpMoviesOnDisk.Controls.Add(this.lblMoviesOnDisk);
			this.flpMoviesOnDisk.Name = "flpMoviesOnDisk";
			// 
			// cbMoviesOnDisk
			// 
			this.cbMoviesOnDisk.Name = "cbMoviesOnDisk";
			this.cbMoviesOnDisk.Text = "Store movie working data on disk instead of RAM";
			// 
			// lblMoviesOnDisk
			// 
			this.lblMoviesOnDisk.Location = new System.Drawing.Point(3, 23);
			this.lblMoviesOnDisk.Name = "lblMoviesOnDisk";
			this.lblMoviesOnDisk.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblMoviesOnDisk.Text = "Will prevent many Out Of Memory crashes during long movies.\r\nYou must restart the" +
    " program after changing this.";
			// 
			// flpMoviesInAWE
			// 
			this.flpMoviesInAWE.Controls.Add(this.cbMoviesInAWE);
			this.flpMoviesInAWE.Controls.Add(this.lblMoviesInAWE);
			this.flpMoviesInAWE.Name = "flpMoviesInAWE";
			// 
			// cbMoviesInAWE
			// 
			this.cbMoviesInAWE.Name = "cbMoviesInAWE";
			this.cbMoviesInAWE.Text = "Store movie working data in extended > 1GB Ram";
			// 
			// lblMoviesInAWE
			// 
			this.lblMoviesInAWE.Location = new System.Drawing.Point(3, 23);
			this.lblMoviesInAWE.Name = "lblMoviesInAWE";
			this.lblMoviesInAWE.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
			this.lblMoviesInAWE.Text = "Will reduce many Out Of Memory crashes during long movies.\r\nThis is experimental;" +
    " it may require admin permissions.\r\nYou must restart the program after changing " +
    "this.";
			// 
			// grpLuaEngine
			// 
			this.grpLuaEngine.Controls.Add(this.flpGrpLuaEngine);
			this.grpLuaEngine.Name = "grpLuaEngine";
			this.grpLuaEngine.Size = new System.Drawing.Size(355, 67);
			this.grpLuaEngine.Text = "Lua Engine";
			// 
			// flpGrpLuaEngine
			// 
			this.flpGrpLuaEngine.Controls.Add(this.rbLuaInterface);
			this.flpGrpLuaEngine.Controls.Add(this.rbKopiLua);
			this.flpGrpLuaEngine.Location = new System.Drawing.Point(7, 16);
			this.flpGrpLuaEngine.Name = "flpGrpLuaEngine";
			// 
			// rbKopiLua
			// 
			this.rbKopiLua.Name = "rbKopiLua";
			this.rbKopiLua.Tag = BizHawk.Client.Common.ELuaEngine.NLuaPlusKopiLua;
			this.rbKopiLua.Text = "NLua+KopiLua - Slower but reliable";
			// 
			// rbLuaInterface
			// 
			this.rbLuaInterface.Name = "rbLuaInterface";
			this.rbLuaInterface.Tag = BizHawk.Client.Common.ELuaEngine.LuaPlusLuaInterface;
			this.rbLuaInterface.Text = "Lua+LuaInterface";
			// 
			// flpDialogButtons
			// 
			this.flpDialogButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.flpDialogButtons.Controls.Add(this.btnDialogOK);
			this.flpDialogButtons.Controls.Add(this.btnDialogCancel);
			this.flpDialogButtons.Location = new System.Drawing.Point(247, 396);
			this.flpDialogButtons.MinimumSize = new System.Drawing.Size(24, 24);
			this.flpDialogButtons.Name = "flpDialogButtons";
			this.flpDialogButtons.Size = new System.Drawing.Size(132, 29);
			// 
			// grpInputMethod
			// 
			this.grpInputMethod.Controls.Add(this.flpGrpInputMethod);
			this.grpInputMethod.Name = "grpInputMethod";
			this.grpInputMethod.Size = new System.Drawing.Size(334, 45);
			this.grpInputMethod.Text = "Input Method (requires restart)";
			// 
			// flpGrpInputMethod
			// 
			this.flpGrpInputMethod.Controls.Add(this.rbInputMethodDirectInput);
			this.flpGrpInputMethod.Controls.Add(this.rbInputMethodOpenTK);
			this.flpGrpInputMethod.Location = new System.Drawing.Point(4, 12);
			this.flpGrpInputMethod.Name = "flpGrpInputMethod";
			// 
			// rbInputMethodOpenTK
			// 
			this.rbInputMethodOpenTK.Name = "rbInputMethodOpenTK";
			this.rbInputMethodOpenTK.Tag = BizHawk.Client.Common.EHostInputMethod.OpenTK;
			this.rbInputMethodOpenTK.Text = "OpenTK";
			// 
			// rbInputMethodDirectInput
			// 
			this.rbInputMethodDirectInput.Name = "rbInputMethodDirectInput";
			this.rbInputMethodDirectInput.Tag = BizHawk.Client.Common.EHostInputMethod.DirectInput;
			this.rbInputMethodDirectInput.Text = "DirectInput";
			// 
			// EmuHawkOptions
			// 
			this.AcceptButton = this.btnDialogOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnDialogCancel;
			this.ClientSize = new System.Drawing.Size(385, 431);
			this.Controls.Add(this.flpDialogButtons);
			this.Controls.Add(this.tcDialog);
			this.MinimumSize = new System.Drawing.Size(401, 444);
			this.Name = "EmuHawkOptions";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Customization Options";
			this.Load += new System.EventHandler(this.GuiOptions_Load);
			this.tcDialog.ResumeLayout(false);
			this.tpGeneral.ResumeLayout(false);
			this.tpGeneral.PerformLayout();
			this.flpTpGeneral.ResumeLayout(false);
			this.flpTpGeneral.PerformLayout();
			this.flpNoFocusEmulate.ResumeLayout(false);
			this.flpNoFocusEmulate.PerformLayout();
			this.flpNoFocusInput.ResumeLayout(false);
			this.flpNoFocusInput.PerformLayout();
			this.flpNoFocusInputCheckBoxes.ResumeLayout(false);
			this.flpNoFocusInputCheckBoxes.PerformLayout();
			this.grpStartup.ResumeLayout(false);
			this.grpStartup.PerformLayout();
			this.flpGrpStartup.ResumeLayout(false);
			this.flpGrpStartup.PerformLayout();
			this.flpSingleInstance.ResumeLayout(false);
			this.flpSingleInstance.PerformLayout();
			this.tpAdvanced.ResumeLayout(false);
			this.tpAdvanced.PerformLayout();
			this.flpTpAdvanced.ResumeLayout(false);
			this.flpTpAdvanced.PerformLayout();
			this.flpAutoSaveRAM.ResumeLayout(false);
			this.flpAutoSaveRAM.PerformLayout();
			this.grpAutoSaveRAM.ResumeLayout(false);
			this.grpAutoSaveRAM.PerformLayout();
			this.flpGrpAutoSaveRAM.ResumeLayout(false);
			this.flpGrpAutoSaveRAM.PerformLayout();
			this.flpAutoSaveRAMFreq.ResumeLayout(false);
			this.flpAutoSaveRAMFreq.PerformLayout();
			this.flpAutoSaveRAMFreqCustom.ResumeLayout(false);
			this.flpAutoSaveRAMFreqCustom.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.nudAutoSaveRAMFreqCustom)).EndInit();
			this.flpFrameAdvPastLag.ResumeLayout(false);
			this.flpFrameAdvPastLag.PerformLayout();
			this.flpMoviesOnDisk.ResumeLayout(false);
			this.flpMoviesOnDisk.PerformLayout();
			this.flpMoviesInAWE.ResumeLayout(false);
			this.flpMoviesInAWE.PerformLayout();
			this.grpLuaEngine.ResumeLayout(false);
			this.grpLuaEngine.PerformLayout();
			this.flpGrpLuaEngine.ResumeLayout(false);
			this.flpGrpLuaEngine.PerformLayout();
			this.flpDialogButtons.ResumeLayout(false);
			this.grpInputMethod.ResumeLayout(false);
			this.grpInputMethod.PerformLayout();
			this.flpGrpInputMethod.ResumeLayout(false);
			this.flpGrpInputMethod.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private BizHawk.WinForms.Controls.LocSzButtonEx btnDialogOK;
		private BizHawk.WinForms.Controls.LocSzButtonEx btnDialogCancel;
		private System.Windows.Forms.TabControl tcDialog;
		private BizHawk.WinForms.Controls.TabPageEx tpGeneral;
		private BizHawk.WinForms.Controls.CheckBoxEx cbStartPaused;
		private BizHawk.WinForms.Controls.CheckBoxEx cbMenusPauseEmulation;
		private BizHawk.WinForms.Controls.CheckBoxEx cbEnableContextMenu;
		private BizHawk.WinForms.Controls.CheckBoxEx cbSaveWindowPosition;
		private BizHawk.WinForms.Controls.CheckBoxEx cbNoFocusEmulate;
		private BizHawk.WinForms.Controls.LocLabelEx lblNoFocusEmulate;
		private BizHawk.WinForms.Controls.LocLabelEx lblNoFocusInput;
		private BizHawk.WinForms.Controls.CheckBoxEx cbNoFocusInput;
		private BizHawk.WinForms.Controls.CheckBoxEx cbNoFocusInputGamepadOnly;
		private BizHawk.WinForms.Controls.CheckBoxEx cbNeverAskForSave;
		private BizHawk.WinForms.Controls.LocLabelEx lblSingleInstanceDesc;
		private BizHawk.WinForms.Controls.CheckBoxEx cbSingleInstance;
		private BizHawk.WinForms.Controls.TabPageEx tpAdvanced;
		private System.Windows.Forms.ToolTip toolTip1;
		private BizHawk.WinForms.Controls.CheckBoxEx cbBackupSaveRAM;
		private BizHawk.WinForms.Controls.CheckBoxEx cbFrameAdvPastLag;
		private BizHawk.WinForms.Controls.LocLabelEx lblFrameAdvPastLag2;
		private BizHawk.WinForms.Controls.LocLabelEx lblFrameAdvPastLag1;
		private BizHawk.WinForms.Controls.LocLabelEx lblSingleInstanceRestartWarning;
		private BizHawk.WinForms.Controls.CheckBoxEx cbStartInFS;
		private BizHawk.WinForms.Controls.CheckBoxEx cbRunLuaDuringTurbo;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpStartup;
		private BizHawk.WinForms.Controls.LocLabelEx lblMoviesOnDisk;
		private BizHawk.WinForms.Controls.CheckBoxEx cbMoviesOnDisk;
		private BizHawk.WinForms.Controls.LocLabelEx lblMoviesInAWE;
		private BizHawk.WinForms.Controls.CheckBoxEx cbMoviesInAWE;
		private BizHawk.WinForms.Controls.RadioButtonEx rbLuaInterface;
		private BizHawk.WinForms.Controls.RadioButtonEx rbKopiLua;
		private BizHawk.WinForms.Controls.CheckBoxEx cbAutoSaveRAM;
		private BizHawk.WinForms.Controls.RadioButtonEx AutoSaveRAMFreq5min;
		private BizHawk.WinForms.Controls.RadioButtonEx cbAutoSaveRAMFreq5s;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpAutoSaveRAM;
		private BizHawk.WinForms.Controls.LabelEx lblAutoSaveRAMFreqDesc;
		private BizHawk.WinForms.Controls.LabelEx lblAutoSaveRAM;
		private BizHawk.WinForms.Controls.CheckBoxEx cbNonQWERTY;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpTpGeneral;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpNoFocusEmulate;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpNoFocusInput;
		private BizHawk.WinForms.Controls.SingleRowFLP flpNoFocusInputCheckBoxes;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpStartup;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpSingleInstance;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpTpAdvanced;
		private BizHawk.WinForms.Controls.SingleRowFLP flpAutoSaveRAM;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpAutoSaveRAM;
		private BizHawk.WinForms.Controls.SingleRowFLP flpAutoSaveRAMFreq;
		private BizHawk.WinForms.Controls.SingleRowFLP flpAutoSaveRAMFreqCustom;
		private BizHawk.WinForms.Controls.RadioButtonEx rbAutoSaveRAMFreqCustom;
		private BizHawk.WinForms.Controls.SzNUDEx nudAutoSaveRAMFreqCustom;
		private BizHawk.WinForms.Controls.LabelEx lblAutoSaveRAMFreqCustomUnits;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpFrameAdvPastLag;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpMoviesOnDisk;
		private BizHawk.WinForms.Controls.SingleColumnFLP flpMoviesInAWE;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpLuaEngine;
		private BizHawk.WinForms.Controls.LocSingleColumnFLP flpGrpLuaEngine;
		private BizHawk.WinForms.Controls.LocSzSingleRowFLP flpDialogButtons;
		private BizHawk.WinForms.Controls.SzGroupBoxEx grpInputMethod;
		private BizHawk.WinForms.Controls.LocSingleRowFLP flpGrpInputMethod;
		private BizHawk.WinForms.Controls.RadioButtonEx rbInputMethodDirectInput;
		private BizHawk.WinForms.Controls.RadioButtonEx rbInputMethodOpenTK;
	}
}