using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class BasicBot
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BasicBot));
            this.BotMenu = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.NewMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.OpenMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.SaveAsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RecentSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.OptionsSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.MemoryDomainsMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator3 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.DataSizeMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this._1ByteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this._2ByteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this._4ByteMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.BigEndianMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator4 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.TurboWhileBottingMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.helpToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.RunBtn = new System.Windows.Forms.Button();
            this.BotStatusStrip = new System.Windows.Forms.StatusStrip();
            this.BotStatusButton = new System.Windows.Forms.ToolStripStatusLabel();
            this.MessageLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.ControlsBox = new System.Windows.Forms.GroupBox();
            this.ControlProbabilityPanel = new System.Windows.Forms.Panel();
            this.BestGroupBox = new System.Windows.Forms.GroupBox();
            this.btnCopyBestInput = new System.Windows.Forms.Button();
            this.PlayBestButton = new System.Windows.Forms.Button();
            this.ClearBestButton = new System.Windows.Forms.Button();
            this.BestAttemptNumberLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label17 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.panel1 = new System.Windows.Forms.Panel();
            this.BestAttemptLogLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.BestTieBreak3Box = new System.Windows.Forms.TextBox();
            this.BestTieBreak2Box = new System.Windows.Forms.TextBox();
            this.BestTieBreak1Box = new System.Windows.Forms.TextBox();
            this.BestMaximizeBox = new System.Windows.Forms.TextBox();
            this.label16 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label15 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label14 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label13 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AttemptsLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.FramesLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.GoalGroupBox = new System.Windows.Forms.GroupBox();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.FrameLengthNumeric = new System.Windows.Forms.NumericUpDown();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.panel3 = new System.Windows.Forms.Panel();
            this.MainValueNumeric = new System.Windows.Forms.NumericUpDown();
            this.MainValueRadio = new System.Windows.Forms.RadioButton();
            this.MainBestRadio = new System.Windows.Forms.RadioButton();
            this.MainOperator = new System.Windows.Forms.ComboBox();
            this.label9 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MaximizeAddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
            this.maximizeLabeltext = new BizHawk.WinForms.Controls.LocLabelEx();
            this.panel4 = new System.Windows.Forms.Panel();
            this.TieBreak1Numeric = new System.Windows.Forms.NumericUpDown();
            this.TieBreak1ValueRadio = new System.Windows.Forms.RadioButton();
            this.Tiebreak1Operator = new System.Windows.Forms.ComboBox();
            this.TieBreak1BestRadio = new System.Windows.Forms.RadioButton();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.TieBreaker1Box = new BizHawk.Client.EmuHawk.HexTextBox();
            this.label10 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.panel5 = new System.Windows.Forms.Panel();
            this.TieBreak2Numeric = new System.Windows.Forms.NumericUpDown();
            this.Tiebreak2Operator = new System.Windows.Forms.ComboBox();
            this.TieBreak2ValueRadio = new System.Windows.Forms.RadioButton();
            this.TieBreak2BestRadio = new System.Windows.Forms.RadioButton();
            this.label11 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.TieBreaker2Box = new BizHawk.Client.EmuHawk.HexTextBox();
            this.panel6 = new System.Windows.Forms.Panel();
            this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.TieBreaker3Box = new BizHawk.Client.EmuHawk.HexTextBox();
            this.TieBreak3Numeric = new System.Windows.Forms.NumericUpDown();
            this.TieBreak3ValueRadio = new System.Windows.Forms.RadioButton();
            this.TieBreak3BestRadio = new System.Windows.Forms.RadioButton();
            this.Tiebreak3Operator = new System.Windows.Forms.ComboBox();
            this.StopBtn = new System.Windows.Forms.Button();
            this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.StartFromSlotBox = new System.Windows.Forms.ComboBox();
            this.ControlGroupBox = new System.Windows.Forms.GroupBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.StatsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ClearStatsContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.BotMenu.SuspendLayout();
            this.BotStatusStrip.SuspendLayout();
            this.ControlsBox.SuspendLayout();
            this.BestGroupBox.SuspendLayout();
            this.panel1.SuspendLayout();
            this.GoalGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FrameLengthNumeric)).BeginInit();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainValueNumeric)).BeginInit();
            this.panel4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TieBreak1Numeric)).BeginInit();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TieBreak2Numeric)).BeginInit();
            this.panel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TieBreak3Numeric)).BeginInit();
            this.ControlGroupBox.SuspendLayout();
            this.panel2.SuspendLayout();
            this.StatsContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // BotMenu
            // 
            resources.ApplyResources(this.BotMenu, "BotMenu");
            this.BotMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu,
            this.OptionsSubMenu,
            this.helpToolStripMenuItem});
            this.toolTip1.SetToolTip(this.BotMenu, resources.GetString("BotMenu.ToolTip"));
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.NewMenuItem,
            this.OpenMenuItem,
            this.SaveMenuItem,
            this.SaveAsMenuItem,
            this.RecentSubMenu});
            this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
            // 
            // NewMenuItem
            // 
            resources.ApplyResources(this.NewMenuItem, "NewMenuItem");
            this.NewMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
            // 
            // OpenMenuItem
            // 
            resources.ApplyResources(this.OpenMenuItem, "OpenMenuItem");
            this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
            // 
            // SaveMenuItem
            // 
            resources.ApplyResources(this.SaveMenuItem, "SaveMenuItem");
            this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
            // 
            // SaveAsMenuItem
            // 
            resources.ApplyResources(this.SaveAsMenuItem, "SaveAsMenuItem");
            this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
            // 
            // RecentSubMenu
            // 
            resources.ApplyResources(this.RecentSubMenu, "RecentSubMenu");
            this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator2});
            this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // OptionsSubMenu
            // 
            resources.ApplyResources(this.OptionsSubMenu, "OptionsSubMenu");
            this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MemoryDomainsMenuItem,
            this.DataSizeMenuItem,
            this.BigEndianMenuItem,
            this.toolStripSeparator4,
            this.TurboWhileBottingMenuItem});
            this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
            // 
            // MemoryDomainsMenuItem
            // 
            resources.ApplyResources(this.MemoryDomainsMenuItem, "MemoryDomainsMenuItem");
            this.MemoryDomainsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSeparator3});
            this.MemoryDomainsMenuItem.DropDownOpened += new System.EventHandler(this.MemoryDomainsMenuItem_DropDownOpened);
            // 
            // toolStripSeparator3
            // 
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // DataSizeMenuItem
            // 
            resources.ApplyResources(this.DataSizeMenuItem, "DataSizeMenuItem");
            this.DataSizeMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._1ByteMenuItem,
            this._2ByteMenuItem,
            this._4ByteMenuItem});
            this.DataSizeMenuItem.DropDownOpened += new System.EventHandler(this.DataSizeMenuItem_DropDownOpened);
            // 
            // _1ByteMenuItem
            // 
            resources.ApplyResources(this._1ByteMenuItem, "_1ByteMenuItem");
            this._1ByteMenuItem.Click += new System.EventHandler(this.OneByteMenuItem_Click);
            // 
            // _2ByteMenuItem
            // 
            resources.ApplyResources(this._2ByteMenuItem, "_2ByteMenuItem");
            this._2ByteMenuItem.Click += new System.EventHandler(this.TwoByteMenuItem_Click);
            // 
            // _4ByteMenuItem
            // 
            resources.ApplyResources(this._4ByteMenuItem, "_4ByteMenuItem");
            this._4ByteMenuItem.Click += new System.EventHandler(this.FourByteMenuItem_Click);
            // 
            // BigEndianMenuItem
            // 
            resources.ApplyResources(this.BigEndianMenuItem, "BigEndianMenuItem");
            this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
            // 
            // toolStripSeparator4
            // 
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // TurboWhileBottingMenuItem
            // 
            resources.ApplyResources(this.TurboWhileBottingMenuItem, "TurboWhileBottingMenuItem");
            this.TurboWhileBottingMenuItem.Click += new System.EventHandler(this.TurboWhileBottingMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
            this.helpToolStripMenuItem.Click += new System.EventHandler(this.HelpToolStripMenuItem_Click);
            // 
            // RunBtn
            // 
            resources.ApplyResources(this.RunBtn, "RunBtn");
            this.RunBtn.Name = "RunBtn";
            this.toolTip1.SetToolTip(this.RunBtn, resources.GetString("RunBtn.ToolTip"));
            this.RunBtn.UseVisualStyleBackColor = true;
            this.RunBtn.Click += new System.EventHandler(this.RunBtn_Click);
            // 
            // BotStatusStrip
            // 
            resources.ApplyResources(this.BotStatusStrip, "BotStatusStrip");
            this.BotStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.BotStatusButton,
            this.MessageLabel});
            this.BotStatusStrip.Name = "BotStatusStrip";
            this.toolTip1.SetToolTip(this.BotStatusStrip, resources.GetString("BotStatusStrip.ToolTip"));
            // 
            // BotStatusButton
            // 
            resources.ApplyResources(this.BotStatusButton, "BotStatusButton");
            this.BotStatusButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.BotStatusButton.Name = "BotStatusButton";
            // 
            // MessageLabel
            // 
            resources.ApplyResources(this.MessageLabel, "MessageLabel");
            this.MessageLabel.Name = "MessageLabel";
            // 
            // ControlsBox
            // 
            resources.ApplyResources(this.ControlsBox, "ControlsBox");
            this.ControlsBox.Controls.Add(this.ControlProbabilityPanel);
            this.ControlsBox.Name = "ControlsBox";
            this.ControlsBox.TabStop = false;
            this.toolTip1.SetToolTip(this.ControlsBox, resources.GetString("ControlsBox.ToolTip"));
            // 
            // ControlProbabilityPanel
            // 
            resources.ApplyResources(this.ControlProbabilityPanel, "ControlProbabilityPanel");
            this.ControlProbabilityPanel.Name = "ControlProbabilityPanel";
            this.toolTip1.SetToolTip(this.ControlProbabilityPanel, resources.GetString("ControlProbabilityPanel.ToolTip"));
            // 
            // BestGroupBox
            // 
            resources.ApplyResources(this.BestGroupBox, "BestGroupBox");
            this.BestGroupBox.Controls.Add(this.btnCopyBestInput);
            this.BestGroupBox.Controls.Add(this.PlayBestButton);
            this.BestGroupBox.Controls.Add(this.ClearBestButton);
            this.BestGroupBox.Controls.Add(this.BestAttemptNumberLabel);
            this.BestGroupBox.Controls.Add(this.label17);
            this.BestGroupBox.Controls.Add(this.panel1);
            this.BestGroupBox.Controls.Add(this.BestTieBreak3Box);
            this.BestGroupBox.Controls.Add(this.BestTieBreak2Box);
            this.BestGroupBox.Controls.Add(this.BestTieBreak1Box);
            this.BestGroupBox.Controls.Add(this.BestMaximizeBox);
            this.BestGroupBox.Controls.Add(this.label16);
            this.BestGroupBox.Controls.Add(this.label15);
            this.BestGroupBox.Controls.Add(this.label14);
            this.BestGroupBox.Controls.Add(this.label13);
            this.BestGroupBox.Name = "BestGroupBox";
            this.BestGroupBox.TabStop = false;
            this.toolTip1.SetToolTip(this.BestGroupBox, resources.GetString("BestGroupBox.ToolTip"));
            // 
            // btnCopyBestInput
            // 
            resources.ApplyResources(this.btnCopyBestInput, "btnCopyBestInput");
            this.btnCopyBestInput.Name = "btnCopyBestInput";
            this.toolTip1.SetToolTip(this.btnCopyBestInput, resources.GetString("btnCopyBestInput.ToolTip"));
            this.btnCopyBestInput.UseVisualStyleBackColor = true;
            this.btnCopyBestInput.Click += new System.EventHandler(this.BtnCopyBestInput_Click);
            // 
            // PlayBestButton
            // 
            resources.ApplyResources(this.PlayBestButton, "PlayBestButton");
            this.PlayBestButton.Name = "PlayBestButton";
            this.toolTip1.SetToolTip(this.PlayBestButton, resources.GetString("PlayBestButton.ToolTip"));
            this.PlayBestButton.UseVisualStyleBackColor = true;
            this.PlayBestButton.Click += new System.EventHandler(this.PlayBestButton_Click);
            // 
            // ClearBestButton
            // 
            resources.ApplyResources(this.ClearBestButton, "ClearBestButton");
            this.ClearBestButton.Name = "ClearBestButton";
            this.toolTip1.SetToolTip(this.ClearBestButton, resources.GetString("ClearBestButton.ToolTip"));
            this.ClearBestButton.UseVisualStyleBackColor = true;
            this.ClearBestButton.Click += new System.EventHandler(this.ClearBestButton_Click);
            // 
            // BestAttemptNumberLabel
            // 
            resources.ApplyResources(this.BestAttemptNumberLabel, "BestAttemptNumberLabel");
            this.BestAttemptNumberLabel.Name = "BestAttemptNumberLabel";
            this.toolTip1.SetToolTip(this.BestAttemptNumberLabel, resources.GetString("BestAttemptNumberLabel.ToolTip"));
            // 
            // label17
            // 
            resources.ApplyResources(this.label17, "label17");
            this.label17.Name = "label17";
            this.toolTip1.SetToolTip(this.label17, resources.GetString("label17.ToolTip"));
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.BestAttemptLogLabel);
            this.panel1.Name = "panel1";
            this.toolTip1.SetToolTip(this.panel1, resources.GetString("panel1.ToolTip"));
            // 
            // BestAttemptLogLabel
            // 
            resources.ApplyResources(this.BestAttemptLogLabel, "BestAttemptLogLabel");
            this.BestAttemptLogLabel.Name = "BestAttemptLogLabel";
            this.toolTip1.SetToolTip(this.BestAttemptLogLabel, resources.GetString("BestAttemptLogLabel.ToolTip"));
            // 
            // BestTieBreak3Box
            // 
            resources.ApplyResources(this.BestTieBreak3Box, "BestTieBreak3Box");
            this.BestTieBreak3Box.Name = "BestTieBreak3Box";
            this.BestTieBreak3Box.ReadOnly = true;
            this.BestTieBreak3Box.TabStop = false;
            this.toolTip1.SetToolTip(this.BestTieBreak3Box, resources.GetString("BestTieBreak3Box.ToolTip"));
            // 
            // BestTieBreak2Box
            // 
            resources.ApplyResources(this.BestTieBreak2Box, "BestTieBreak2Box");
            this.BestTieBreak2Box.Name = "BestTieBreak2Box";
            this.BestTieBreak2Box.ReadOnly = true;
            this.BestTieBreak2Box.TabStop = false;
            this.toolTip1.SetToolTip(this.BestTieBreak2Box, resources.GetString("BestTieBreak2Box.ToolTip"));
            // 
            // BestTieBreak1Box
            // 
            resources.ApplyResources(this.BestTieBreak1Box, "BestTieBreak1Box");
            this.BestTieBreak1Box.Name = "BestTieBreak1Box";
            this.BestTieBreak1Box.ReadOnly = true;
            this.BestTieBreak1Box.TabStop = false;
            this.toolTip1.SetToolTip(this.BestTieBreak1Box, resources.GetString("BestTieBreak1Box.ToolTip"));
            // 
            // BestMaximizeBox
            // 
            resources.ApplyResources(this.BestMaximizeBox, "BestMaximizeBox");
            this.BestMaximizeBox.Name = "BestMaximizeBox";
            this.BestMaximizeBox.ReadOnly = true;
            this.BestMaximizeBox.TabStop = false;
            this.toolTip1.SetToolTip(this.BestMaximizeBox, resources.GetString("BestMaximizeBox.ToolTip"));
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
            // label14
            // 
            resources.ApplyResources(this.label14, "label14");
            this.label14.Name = "label14";
            this.toolTip1.SetToolTip(this.label14, resources.GetString("label14.ToolTip"));
            // 
            // label13
            // 
            resources.ApplyResources(this.label13, "label13");
            this.label13.Name = "label13";
            this.toolTip1.SetToolTip(this.label13, resources.GetString("label13.ToolTip"));
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // AttemptsLabel
            // 
            resources.ApplyResources(this.AttemptsLabel, "AttemptsLabel");
            this.AttemptsLabel.Name = "AttemptsLabel";
            this.toolTip1.SetToolTip(this.AttemptsLabel, resources.GetString("AttemptsLabel.ToolTip"));
            // 
            // FramesLabel
            // 
            resources.ApplyResources(this.FramesLabel, "FramesLabel");
            this.FramesLabel.Name = "FramesLabel";
            this.toolTip1.SetToolTip(this.FramesLabel, resources.GetString("FramesLabel.ToolTip"));
            // 
            // GoalGroupBox
            // 
            resources.ApplyResources(this.GoalGroupBox, "GoalGroupBox");
            this.GoalGroupBox.Controls.Add(this.label4);
            this.GoalGroupBox.Controls.Add(this.FrameLengthNumeric);
            this.GoalGroupBox.Controls.Add(this.label3);
            this.GoalGroupBox.Controls.Add(this.panel3);
            this.GoalGroupBox.Controls.Add(this.panel4);
            this.GoalGroupBox.Controls.Add(this.panel5);
            this.GoalGroupBox.Controls.Add(this.panel6);
            this.GoalGroupBox.Name = "GoalGroupBox";
            this.GoalGroupBox.TabStop = false;
            this.toolTip1.SetToolTip(this.GoalGroupBox, resources.GetString("GoalGroupBox.ToolTip"));
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            this.toolTip1.SetToolTip(this.label4, resources.GetString("label4.ToolTip"));
            // 
            // FrameLengthNumeric
            // 
            resources.ApplyResources(this.FrameLengthNumeric, "FrameLengthNumeric");
            this.FrameLengthNumeric.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.FrameLengthNumeric.Name = "FrameLengthNumeric";
            this.toolTip1.SetToolTip(this.FrameLengthNumeric, resources.GetString("FrameLengthNumeric.ToolTip"));
            this.FrameLengthNumeric.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.FrameLengthNumeric.ValueChanged += new System.EventHandler(this.FrameLengthNumeric_ValueChanged);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            this.toolTip1.SetToolTip(this.label3, resources.GetString("label3.ToolTip"));
            // 
            // panel3
            // 
            resources.ApplyResources(this.panel3, "panel3");
            this.panel3.Controls.Add(this.MainValueNumeric);
            this.panel3.Controls.Add(this.MainValueRadio);
            this.panel3.Controls.Add(this.MainBestRadio);
            this.panel3.Controls.Add(this.MainOperator);
            this.panel3.Controls.Add(this.label9);
            this.panel3.Controls.Add(this.MaximizeAddressBox);
            this.panel3.Controls.Add(this.maximizeLabeltext);
            this.panel3.Name = "panel3";
            this.toolTip1.SetToolTip(this.panel3, resources.GetString("panel3.ToolTip"));
            // 
            // MainValueNumeric
            // 
            resources.ApplyResources(this.MainValueNumeric, "MainValueNumeric");
            this.MainValueNumeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.MainValueNumeric.Minimum = new decimal(new int[] {
            100000,
            0,
            0,
            -2147483648});
            this.MainValueNumeric.Name = "MainValueNumeric";
            this.toolTip1.SetToolTip(this.MainValueNumeric, resources.GetString("MainValueNumeric.ToolTip"));
            this.MainValueNumeric.ValueChanged += new System.EventHandler(this.MainValueNumeric_ValueChanged);
            // 
            // MainValueRadio
            // 
            resources.ApplyResources(this.MainValueRadio, "MainValueRadio");
            this.MainValueRadio.Name = "MainValueRadio";
            this.toolTip1.SetToolTip(this.MainValueRadio, resources.GetString("MainValueRadio.ToolTip"));
            this.MainValueRadio.UseVisualStyleBackColor = true;
            this.MainValueRadio.CheckedChanged += new System.EventHandler(this.MainValueRadio_CheckedChanged);
            // 
            // MainBestRadio
            // 
            resources.ApplyResources(this.MainBestRadio, "MainBestRadio");
            this.MainBestRadio.Checked = true;
            this.MainBestRadio.Name = "MainBestRadio";
            this.MainBestRadio.TabStop = true;
            this.toolTip1.SetToolTip(this.MainBestRadio, resources.GetString("MainBestRadio.ToolTip"));
            this.MainBestRadio.UseVisualStyleBackColor = true;
            this.MainBestRadio.CheckedChanged += new System.EventHandler(this.MainBestRadio_CheckedChanged);
            // 
            // MainOperator
            // 
            resources.ApplyResources(this.MainOperator, "MainOperator");
            this.MainOperator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MainOperator.FormattingEnabled = true;
            this.MainOperator.Items.AddRange(new object[] {
            resources.GetString("MainOperator.Items"),
            resources.GetString("MainOperator.Items1"),
            resources.GetString("MainOperator.Items2"),
            resources.GetString("MainOperator.Items3"),
            resources.GetString("MainOperator.Items4"),
            resources.GetString("MainOperator.Items5")});
            this.MainOperator.Name = "MainOperator";
            this.toolTip1.SetToolTip(this.MainOperator, resources.GetString("MainOperator.ToolTip"));
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            this.toolTip1.SetToolTip(this.label9, resources.GetString("label9.ToolTip"));
            // 
            // MaximizeAddressBox
            // 
            resources.ApplyResources(this.MaximizeAddressBox, "MaximizeAddressBox");
            this.MaximizeAddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.MaximizeAddressBox.Name = "MaximizeAddressBox";
            this.MaximizeAddressBox.Nullable = true;
            this.toolTip1.SetToolTip(this.MaximizeAddressBox, resources.GetString("MaximizeAddressBox.ToolTip"));
            this.MaximizeAddressBox.TextChanged += new System.EventHandler(this.MaximizeAddressBox_TextChanged);
            // 
            // maximizeLabeltext
            // 
            resources.ApplyResources(this.maximizeLabeltext, "maximizeLabeltext");
            this.maximizeLabeltext.Name = "maximizeLabeltext";
            this.toolTip1.SetToolTip(this.maximizeLabeltext, resources.GetString("maximizeLabeltext.ToolTip"));
            // 
            // panel4
            // 
            resources.ApplyResources(this.panel4, "panel4");
            this.panel4.Controls.Add(this.TieBreak1Numeric);
            this.panel4.Controls.Add(this.TieBreak1ValueRadio);
            this.panel4.Controls.Add(this.Tiebreak1Operator);
            this.panel4.Controls.Add(this.TieBreak1BestRadio);
            this.panel4.Controls.Add(this.label5);
            this.panel4.Controls.Add(this.TieBreaker1Box);
            this.panel4.Controls.Add(this.label10);
            this.panel4.Name = "panel4";
            this.toolTip1.SetToolTip(this.panel4, resources.GetString("panel4.ToolTip"));
            // 
            // TieBreak1Numeric
            // 
            resources.ApplyResources(this.TieBreak1Numeric, "TieBreak1Numeric");
            this.TieBreak1Numeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.TieBreak1Numeric.Minimum = new decimal(new int[] {
            100000,
            0,
            0,
            -2147483648});
            this.TieBreak1Numeric.Name = "TieBreak1Numeric";
            this.toolTip1.SetToolTip(this.TieBreak1Numeric, resources.GetString("TieBreak1Numeric.ToolTip"));
            this.TieBreak1Numeric.ValueChanged += new System.EventHandler(this.TieBreak1Numeric_ValueChanged);
            // 
            // TieBreak1ValueRadio
            // 
            resources.ApplyResources(this.TieBreak1ValueRadio, "TieBreak1ValueRadio");
            this.TieBreak1ValueRadio.Name = "TieBreak1ValueRadio";
            this.toolTip1.SetToolTip(this.TieBreak1ValueRadio, resources.GetString("TieBreak1ValueRadio.ToolTip"));
            this.TieBreak1ValueRadio.UseVisualStyleBackColor = true;
            this.TieBreak1ValueRadio.CheckedChanged += new System.EventHandler(this.TieBreak1ValueRadio_CheckedChanged);
            // 
            // Tiebreak1Operator
            // 
            resources.ApplyResources(this.Tiebreak1Operator, "Tiebreak1Operator");
            this.Tiebreak1Operator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Tiebreak1Operator.FormattingEnabled = true;
            this.Tiebreak1Operator.Items.AddRange(new object[] {
            resources.GetString("Tiebreak1Operator.Items"),
            resources.GetString("Tiebreak1Operator.Items1"),
            resources.GetString("Tiebreak1Operator.Items2"),
            resources.GetString("Tiebreak1Operator.Items3"),
            resources.GetString("Tiebreak1Operator.Items4"),
            resources.GetString("Tiebreak1Operator.Items5")});
            this.Tiebreak1Operator.Name = "Tiebreak1Operator";
            this.toolTip1.SetToolTip(this.Tiebreak1Operator, resources.GetString("Tiebreak1Operator.ToolTip"));
            // 
            // TieBreak1BestRadio
            // 
            resources.ApplyResources(this.TieBreak1BestRadio, "TieBreak1BestRadio");
            this.TieBreak1BestRadio.Checked = true;
            this.TieBreak1BestRadio.Name = "TieBreak1BestRadio";
            this.TieBreak1BestRadio.TabStop = true;
            this.toolTip1.SetToolTip(this.TieBreak1BestRadio, resources.GetString("TieBreak1BestRadio.ToolTip"));
            this.TieBreak1BestRadio.UseVisualStyleBackColor = true;
            this.TieBreak1BestRadio.CheckedChanged += new System.EventHandler(this.Tiebreak1BestRadio_CheckedChanged);
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            this.toolTip1.SetToolTip(this.label5, resources.GetString("label5.ToolTip"));
            // 
            // TieBreaker1Box
            // 
            resources.ApplyResources(this.TieBreaker1Box, "TieBreaker1Box");
            this.TieBreaker1Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TieBreaker1Box.Name = "TieBreaker1Box";
            this.TieBreaker1Box.Nullable = true;
            this.toolTip1.SetToolTip(this.TieBreaker1Box, resources.GetString("TieBreaker1Box.ToolTip"));
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            this.toolTip1.SetToolTip(this.label10, resources.GetString("label10.ToolTip"));
            // 
            // panel5
            // 
            resources.ApplyResources(this.panel5, "panel5");
            this.panel5.Controls.Add(this.TieBreak2Numeric);
            this.panel5.Controls.Add(this.Tiebreak2Operator);
            this.panel5.Controls.Add(this.TieBreak2ValueRadio);
            this.panel5.Controls.Add(this.TieBreak2BestRadio);
            this.panel5.Controls.Add(this.label11);
            this.panel5.Controls.Add(this.label6);
            this.panel5.Controls.Add(this.TieBreaker2Box);
            this.panel5.Name = "panel5";
            this.toolTip1.SetToolTip(this.panel5, resources.GetString("panel5.ToolTip"));
            // 
            // TieBreak2Numeric
            // 
            resources.ApplyResources(this.TieBreak2Numeric, "TieBreak2Numeric");
            this.TieBreak2Numeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.TieBreak2Numeric.Minimum = new decimal(new int[] {
            100000,
            0,
            0,
            -2147483648});
            this.TieBreak2Numeric.Name = "TieBreak2Numeric";
            this.toolTip1.SetToolTip(this.TieBreak2Numeric, resources.GetString("TieBreak2Numeric.ToolTip"));
            this.TieBreak2Numeric.ValueChanged += new System.EventHandler(this.TieBreak2Numeric_ValueChanged);
            // 
            // Tiebreak2Operator
            // 
            resources.ApplyResources(this.Tiebreak2Operator, "Tiebreak2Operator");
            this.Tiebreak2Operator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Tiebreak2Operator.FormattingEnabled = true;
            this.Tiebreak2Operator.Items.AddRange(new object[] {
            resources.GetString("Tiebreak2Operator.Items"),
            resources.GetString("Tiebreak2Operator.Items1"),
            resources.GetString("Tiebreak2Operator.Items2"),
            resources.GetString("Tiebreak2Operator.Items3"),
            resources.GetString("Tiebreak2Operator.Items4"),
            resources.GetString("Tiebreak2Operator.Items5")});
            this.Tiebreak2Operator.Name = "Tiebreak2Operator";
            this.toolTip1.SetToolTip(this.Tiebreak2Operator, resources.GetString("Tiebreak2Operator.ToolTip"));
            // 
            // TieBreak2ValueRadio
            // 
            resources.ApplyResources(this.TieBreak2ValueRadio, "TieBreak2ValueRadio");
            this.TieBreak2ValueRadio.Name = "TieBreak2ValueRadio";
            this.toolTip1.SetToolTip(this.TieBreak2ValueRadio, resources.GetString("TieBreak2ValueRadio.ToolTip"));
            this.TieBreak2ValueRadio.UseVisualStyleBackColor = true;
            this.TieBreak2ValueRadio.CheckedChanged += new System.EventHandler(this.TieBreak2ValueRadio_CheckedChanged);
            // 
            // TieBreak2BestRadio
            // 
            resources.ApplyResources(this.TieBreak2BestRadio, "TieBreak2BestRadio");
            this.TieBreak2BestRadio.Checked = true;
            this.TieBreak2BestRadio.Name = "TieBreak2BestRadio";
            this.TieBreak2BestRadio.TabStop = true;
            this.toolTip1.SetToolTip(this.TieBreak2BestRadio, resources.GetString("TieBreak2BestRadio.ToolTip"));
            this.TieBreak2BestRadio.UseVisualStyleBackColor = true;
            this.TieBreak2BestRadio.CheckedChanged += new System.EventHandler(this.Tiebreak2BestRadio_CheckedChanged);
            // 
            // label11
            // 
            resources.ApplyResources(this.label11, "label11");
            this.label11.Name = "label11";
            this.toolTip1.SetToolTip(this.label11, resources.GetString("label11.ToolTip"));
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            this.toolTip1.SetToolTip(this.label6, resources.GetString("label6.ToolTip"));
            // 
            // TieBreaker2Box
            // 
            resources.ApplyResources(this.TieBreaker2Box, "TieBreaker2Box");
            this.TieBreaker2Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TieBreaker2Box.Name = "TieBreaker2Box";
            this.TieBreaker2Box.Nullable = true;
            this.toolTip1.SetToolTip(this.TieBreaker2Box, resources.GetString("TieBreaker2Box.ToolTip"));
            // 
            // panel6
            // 
            resources.ApplyResources(this.panel6, "panel6");
            this.panel6.Controls.Add(this.label12);
            this.panel6.Controls.Add(this.label7);
            this.panel6.Controls.Add(this.TieBreaker3Box);
            this.panel6.Controls.Add(this.TieBreak3Numeric);
            this.panel6.Controls.Add(this.TieBreak3ValueRadio);
            this.panel6.Controls.Add(this.TieBreak3BestRadio);
            this.panel6.Controls.Add(this.Tiebreak3Operator);
            this.panel6.Name = "panel6";
            this.toolTip1.SetToolTip(this.panel6, resources.GetString("panel6.ToolTip"));
            // 
            // label12
            // 
            resources.ApplyResources(this.label12, "label12");
            this.label12.Name = "label12";
            this.toolTip1.SetToolTip(this.label12, resources.GetString("label12.ToolTip"));
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            this.toolTip1.SetToolTip(this.label7, resources.GetString("label7.ToolTip"));
            // 
            // TieBreaker3Box
            // 
            resources.ApplyResources(this.TieBreaker3Box, "TieBreaker3Box");
            this.TieBreaker3Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.TieBreaker3Box.Name = "TieBreaker3Box";
            this.TieBreaker3Box.Nullable = true;
            this.toolTip1.SetToolTip(this.TieBreaker3Box, resources.GetString("TieBreaker3Box.ToolTip"));
            // 
            // TieBreak3Numeric
            // 
            resources.ApplyResources(this.TieBreak3Numeric, "TieBreak3Numeric");
            this.TieBreak3Numeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.TieBreak3Numeric.Minimum = new decimal(new int[] {
            100000,
            0,
            0,
            -2147483648});
            this.TieBreak3Numeric.Name = "TieBreak3Numeric";
            this.toolTip1.SetToolTip(this.TieBreak3Numeric, resources.GetString("TieBreak3Numeric.ToolTip"));
            this.TieBreak3Numeric.ValueChanged += new System.EventHandler(this.TieBreak3Numeric_ValueChanged);
            // 
            // TieBreak3ValueRadio
            // 
            resources.ApplyResources(this.TieBreak3ValueRadio, "TieBreak3ValueRadio");
            this.TieBreak3ValueRadio.Name = "TieBreak3ValueRadio";
            this.toolTip1.SetToolTip(this.TieBreak3ValueRadio, resources.GetString("TieBreak3ValueRadio.ToolTip"));
            this.TieBreak3ValueRadio.UseVisualStyleBackColor = true;
            this.TieBreak3ValueRadio.CheckedChanged += new System.EventHandler(this.TieBreak3ValueRadio_CheckedChanged);
            // 
            // TieBreak3BestRadio
            // 
            resources.ApplyResources(this.TieBreak3BestRadio, "TieBreak3BestRadio");
            this.TieBreak3BestRadio.Checked = true;
            this.TieBreak3BestRadio.Name = "TieBreak3BestRadio";
            this.TieBreak3BestRadio.TabStop = true;
            this.toolTip1.SetToolTip(this.TieBreak3BestRadio, resources.GetString("TieBreak3BestRadio.ToolTip"));
            this.TieBreak3BestRadio.UseVisualStyleBackColor = true;
            this.TieBreak3BestRadio.CheckedChanged += new System.EventHandler(this.Tiebreak3BestRadio_CheckedChanged);
            // 
            // Tiebreak3Operator
            // 
            resources.ApplyResources(this.Tiebreak3Operator, "Tiebreak3Operator");
            this.Tiebreak3Operator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Tiebreak3Operator.FormattingEnabled = true;
            this.Tiebreak3Operator.Items.AddRange(new object[] {
            resources.GetString("Tiebreak3Operator.Items"),
            resources.GetString("Tiebreak3Operator.Items1"),
            resources.GetString("Tiebreak3Operator.Items2"),
            resources.GetString("Tiebreak3Operator.Items3"),
            resources.GetString("Tiebreak3Operator.Items4"),
            resources.GetString("Tiebreak3Operator.Items5")});
            this.Tiebreak3Operator.Name = "Tiebreak3Operator";
            this.toolTip1.SetToolTip(this.Tiebreak3Operator, resources.GetString("Tiebreak3Operator.ToolTip"));
            // 
            // StopBtn
            // 
            resources.ApplyResources(this.StopBtn, "StopBtn");
            this.StopBtn.Name = "StopBtn";
            this.toolTip1.SetToolTip(this.StopBtn, resources.GetString("StopBtn.ToolTip"));
            this.StopBtn.UseVisualStyleBackColor = true;
            this.StopBtn.Click += new System.EventHandler(this.StopBtn_Click);
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            this.toolTip1.SetToolTip(this.label8, resources.GetString("label8.ToolTip"));
            // 
            // StartFromSlotBox
            // 
            resources.ApplyResources(this.StartFromSlotBox, "StartFromSlotBox");
            this.StartFromSlotBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.StartFromSlotBox.FormattingEnabled = true;
            this.StartFromSlotBox.Items.AddRange(new object[] {
            resources.GetString("StartFromSlotBox.Items"),
            resources.GetString("StartFromSlotBox.Items1"),
            resources.GetString("StartFromSlotBox.Items2"),
            resources.GetString("StartFromSlotBox.Items3"),
            resources.GetString("StartFromSlotBox.Items4"),
            resources.GetString("StartFromSlotBox.Items5"),
            resources.GetString("StartFromSlotBox.Items6"),
            resources.GetString("StartFromSlotBox.Items7"),
            resources.GetString("StartFromSlotBox.Items8"),
            resources.GetString("StartFromSlotBox.Items9")});
            this.StartFromSlotBox.Name = "StartFromSlotBox";
            this.toolTip1.SetToolTip(this.StartFromSlotBox, resources.GetString("StartFromSlotBox.ToolTip"));
            // 
            // ControlGroupBox
            // 
            resources.ApplyResources(this.ControlGroupBox, "ControlGroupBox");
            this.ControlGroupBox.Controls.Add(this.panel2);
            this.ControlGroupBox.Controls.Add(this.StopBtn);
            this.ControlGroupBox.Controls.Add(this.RunBtn);
            this.ControlGroupBox.Controls.Add(this.StartFromSlotBox);
            this.ControlGroupBox.Controls.Add(this.label8);
            this.ControlGroupBox.Name = "ControlGroupBox";
            this.ControlGroupBox.TabStop = false;
            this.toolTip1.SetToolTip(this.ControlGroupBox, resources.GetString("ControlGroupBox.ToolTip"));
            // 
            // panel2
            // 
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.ContextMenuStrip = this.StatsContextMenu;
            this.panel2.Controls.Add(this.label1);
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.FramesLabel);
            this.panel2.Controls.Add(this.AttemptsLabel);
            this.panel2.Name = "panel2";
            this.toolTip1.SetToolTip(this.panel2, resources.GetString("panel2.ToolTip"));
            // 
            // StatsContextMenu
            // 
            resources.ApplyResources(this.StatsContextMenu, "StatsContextMenu");
            this.StatsContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ClearStatsContextMenuItem});
            this.StatsContextMenu.Name = "StatsContextMenu";
            this.toolTip1.SetToolTip(this.StatsContextMenu, resources.GetString("StatsContextMenu.ToolTip"));
            // 
            // ClearStatsContextMenuItem
            // 
            resources.ApplyResources(this.ClearStatsContextMenuItem, "ClearStatsContextMenuItem");
            this.ClearStatsContextMenuItem.Click += new System.EventHandler(this.ClearStatsContextMenuItem_Click);
            // 
            // BasicBot
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ControlGroupBox);
            this.Controls.Add(this.GoalGroupBox);
            this.Controls.Add(this.BestGroupBox);
            this.Controls.Add(this.ControlsBox);
            this.Controls.Add(this.BotStatusStrip);
            this.Controls.Add(this.BotMenu);
            this.MainMenuStrip = this.BotMenu;
            this.Name = "BasicBot";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.Load += new System.EventHandler(this.BasicBot_Load);
            this.BotMenu.ResumeLayout(false);
            this.BotMenu.PerformLayout();
            this.BotStatusStrip.ResumeLayout(false);
            this.BotStatusStrip.PerformLayout();
            this.ControlsBox.ResumeLayout(false);
            this.BestGroupBox.ResumeLayout(false);
            this.BestGroupBox.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.GoalGroupBox.ResumeLayout(false);
            this.GoalGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FrameLengthNumeric)).EndInit();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MainValueNumeric)).EndInit();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TieBreak1Numeric)).EndInit();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TieBreak2Numeric)).EndInit();
            this.panel6.ResumeLayout(false);
            this.panel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TieBreak3Numeric)).EndInit();
            this.ControlGroupBox.ResumeLayout(false);
            this.ControlGroupBox.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.StatsContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private MenuStripEx BotMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private System.Windows.Forms.Button RunBtn;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OpenMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RecentSubMenu;
		private System.Windows.Forms.StatusStrip BotStatusStrip;
		private System.Windows.Forms.GroupBox ControlsBox;
		private System.Windows.Forms.Panel ControlProbabilityPanel;
		private System.Windows.Forms.GroupBox BestGroupBox;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx AttemptsLabel;
		private BizHawk.WinForms.Controls.LocLabelEx FramesLabel;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx OptionsSubMenu;
		private System.Windows.Forms.GroupBox GoalGroupBox;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private HexTextBox TieBreaker1Box;
		private HexTextBox TieBreaker2Box;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.NumericUpDown FrameLengthNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.Button StopBtn;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private System.Windows.Forms.ComboBox StartFromSlotBox;
		private BizHawk.WinForms.Controls.LocLabelEx label11;
		private BizHawk.WinForms.Controls.LocLabelEx label10;
		private System.Windows.Forms.TextBox BestTieBreak3Box;
		private System.Windows.Forms.TextBox BestTieBreak2Box;
		private System.Windows.Forms.TextBox BestTieBreak1Box;
		private System.Windows.Forms.TextBox BestMaximizeBox;
		private BizHawk.WinForms.Controls.LocLabelEx label16;
		private BizHawk.WinForms.Controls.LocLabelEx label15;
		private BizHawk.WinForms.Controls.LocLabelEx label14;
		private BizHawk.WinForms.Controls.LocLabelEx label13;
		private System.Windows.Forms.Panel panel1;
		private BizHawk.WinForms.Controls.LocLabelEx BestAttemptNumberLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label17;
		private BizHawk.WinForms.Controls.LocLabelEx BestAttemptLogLabel;
		private System.Windows.Forms.Button ClearBestButton;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx SaveAsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx NewMenuItem;
		private System.Windows.Forms.Button PlayBestButton;
		private System.Windows.Forms.ToolStripStatusLabel MessageLabel;
		private System.Windows.Forms.GroupBox ControlGroupBox;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx TurboWhileBottingMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx MemoryDomainsMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator3;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.ContextMenuStrip StatsContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ClearStatsContextMenuItem;
		private System.Windows.Forms.ToolStripStatusLabel BotStatusButton;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx BigEndianMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator4;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx DataSizeMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx _1ByteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx _2ByteMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx _4ByteMenuItem;
        private System.Windows.Forms.ComboBox Tiebreak2Operator;
        private System.Windows.Forms.ComboBox Tiebreak1Operator;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.ComboBox Tiebreak3Operator;
		private BizHawk.WinForms.Controls.LocLabelEx label12;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private HexTextBox TieBreaker3Box;
		private System.Windows.Forms.NumericUpDown TieBreak3Numeric;
		private System.Windows.Forms.RadioButton TieBreak3ValueRadio;
		private System.Windows.Forms.RadioButton TieBreak3BestRadio;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.NumericUpDown TieBreak2Numeric;
		private System.Windows.Forms.RadioButton TieBreak2ValueRadio;
		private System.Windows.Forms.RadioButton TieBreak2BestRadio;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.NumericUpDown TieBreak1Numeric;
		private System.Windows.Forms.RadioButton TieBreak1ValueRadio;
		private System.Windows.Forms.RadioButton TieBreak1BestRadio;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.NumericUpDown MainValueNumeric;
		private System.Windows.Forms.RadioButton MainValueRadio;
		private System.Windows.Forms.RadioButton MainBestRadio;
		private System.Windows.Forms.ComboBox MainOperator;
		private BizHawk.WinForms.Controls.LocLabelEx label9;
		private HexTextBox MaximizeAddressBox;
		private BizHawk.WinForms.Controls.LocLabelEx maximizeLabeltext;
		private System.Windows.Forms.Button btnCopyBestInput;
		private System.Windows.Forms.ToolTip toolTip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx helpToolStripMenuItem;
	}
}
