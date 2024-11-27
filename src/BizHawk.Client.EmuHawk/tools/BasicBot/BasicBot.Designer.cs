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
			this.BotMenu = new MenuStripEx();
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
			this.InvisibleEmulationCheckBox = new System.Windows.Forms.CheckBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.StatsContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ClearStatsContextMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.helpToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
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
			this.BotMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.FileSubMenu,
			this.OptionsSubMenu,
			this.helpToolStripMenuItem});
			this.BotMenu.TabIndex = 0;
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.NewMenuItem,
			this.OpenMenuItem,
			this.SaveMenuItem,
			this.SaveAsMenuItem,
			this.RecentSubMenu});
			this.FileSubMenu.Text = "&File";
			this.FileSubMenu.DropDownOpened += new System.EventHandler(this.FileSubMenu_DropDownOpened);
			// 
			// NewMenuItem
			// 
			this.NewMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
			this.NewMenuItem.Text = "&New";
			this.NewMenuItem.Click += new System.EventHandler(this.NewMenuItem_Click);
			// 
			// OpenMenuItem
			// 
			this.OpenMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.OpenMenuItem.Text = "&Open...";
			this.OpenMenuItem.Click += new System.EventHandler(this.OpenMenuItem_Click);
			// 
			// SaveMenuItem
			// 
			this.SaveMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.SaveMenuItem.Text = "&Save";
			this.SaveMenuItem.Click += new System.EventHandler(this.SaveMenuItem_Click);
			// 
			// SaveAsMenuItem
			// 
			this.SaveAsMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
			| System.Windows.Forms.Keys.S)));
			this.SaveAsMenuItem.Text = "Save &As...";
			this.SaveAsMenuItem.Click += new System.EventHandler(this.SaveAsMenuItem_Click);
			// 
			// RecentSubMenu
			// 
			this.RecentSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripSeparator2});
			this.RecentSubMenu.Text = "Recent";
			this.RecentSubMenu.DropDownOpened += new System.EventHandler(this.RecentSubMenu_DropDownOpened);
			// 
			// OptionsSubMenu
			// 
			this.OptionsSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.MemoryDomainsMenuItem,
			this.DataSizeMenuItem,
			this.BigEndianMenuItem,
			this.toolStripSeparator4,
			this.TurboWhileBottingMenuItem});
			this.OptionsSubMenu.Text = "&Options";
			this.OptionsSubMenu.DropDownOpened += new System.EventHandler(this.OptionsSubMenu_DropDownOpened);
			// 
			// MemoryDomainsMenuItem
			// 
			this.MemoryDomainsMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripSeparator3});
			this.MemoryDomainsMenuItem.Text = "Memory Domains";
			this.MemoryDomainsMenuItem.DropDownOpened += new System.EventHandler(this.MemoryDomainsMenuItem_DropDownOpened);
			// 
			// DataSizeMenuItem
			// 
			this.DataSizeMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this._1ByteMenuItem,
			this._2ByteMenuItem,
			this._4ByteMenuItem});
			this.DataSizeMenuItem.Text = "Data Size";
			this.DataSizeMenuItem.DropDownOpened += new System.EventHandler(this.DataSizeMenuItem_DropDownOpened);
			// 
			// _1ByteMenuItem
			// 
			this._1ByteMenuItem.Text = "1 Byte";
			this._1ByteMenuItem.Click += new System.EventHandler(this.OneByteMenuItem_Click);
			// 
			// _2ByteMenuItem
			// 
			this._2ByteMenuItem.Text = "2 Bytes";
			this._2ByteMenuItem.Click += new System.EventHandler(this.TwoByteMenuItem_Click);
			// 
			// _4ByteMenuItem
			// 
			this._4ByteMenuItem.Text = "4 Bytes";
			this._4ByteMenuItem.Click += new System.EventHandler(this.FourByteMenuItem_Click);
			// 
			// BigEndianMenuItem
			// 
			this.BigEndianMenuItem.Text = "Big Endian";
			this.BigEndianMenuItem.Click += new System.EventHandler(this.BigEndianMenuItem_Click);
			// 
			// TurboWhileBottingMenuItem
			// 
			this.TurboWhileBottingMenuItem.Text = "Turbo While Botting";
			this.TurboWhileBottingMenuItem.Click += new System.EventHandler(this.TurboWhileBottingMenuItem_Click);
			// 
			// RunBtn
			// 
			this.RunBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.RunBtn.Location = new System.Drawing.Point(6, 56);
			this.RunBtn.Name = "RunBtn";
			this.RunBtn.Size = new System.Drawing.Size(75, 23);
			this.RunBtn.TabIndex = 2001;
			this.RunBtn.Text = "&Run";
			this.RunBtn.UseVisualStyleBackColor = true;
			this.RunBtn.Click += new System.EventHandler(this.RunBtn_Click);
			// 
			// BotStatusStrip
			// 
			this.BotStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.BotStatusButton,
			this.MessageLabel});
			this.BotStatusStrip.Location = new System.Drawing.Point(0, 565);
			this.BotStatusStrip.Name = "BotStatusStrip";
			this.BotStatusStrip.Size = new System.Drawing.Size(707, 22);
			this.BotStatusStrip.TabIndex = 2;
			this.BotStatusStrip.Text = "statusStrip1";
			// 
			// BotStatusButton
			// 
			this.BotStatusButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.BotStatusButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.BotStatusButton.Name = "BotStatusButton";
			this.BotStatusButton.RightToLeftAutoMirrorImage = true;
			this.BotStatusButton.Size = new System.Drawing.Size(16, 17);
			this.BotStatusButton.Text = " ";
			this.BotStatusButton.ToolTipText = " ";
			// 
			// MessageLabel
			// 
			this.MessageLabel.Name = "MessageLabel";
			this.MessageLabel.Size = new System.Drawing.Size(109, 17);
			this.MessageLabel.Text = "          ";
			// 
			// ControlsBox
			// 
			this.ControlsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.ControlsBox.Controls.Add(this.ControlProbabilityPanel);
			this.ControlsBox.Location = new System.Drawing.Point(12, 183);
			this.ControlsBox.Name = "ControlsBox";
			this.ControlsBox.Size = new System.Drawing.Size(442, 369);
			this.ControlsBox.TabIndex = 3;
			this.ControlsBox.TabStop = false;
			this.ControlsBox.Text = "Controls";
			// 
			// ControlProbabilityPanel
			// 
			this.ControlProbabilityPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.ControlProbabilityPanel.AutoScroll = true;
			this.ControlProbabilityPanel.Location = new System.Drawing.Point(6, 19);
			this.ControlProbabilityPanel.Name = "ControlProbabilityPanel";
			this.ControlProbabilityPanel.Size = new System.Drawing.Size(430, 350);
			this.ControlProbabilityPanel.TabIndex = 0;
			// 
			// BestGroupBox
			// 
			this.BestGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
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
			this.BestGroupBox.Location = new System.Drawing.Point(461, 183);
			this.BestGroupBox.Name = "BestGroupBox";
			this.BestGroupBox.Size = new System.Drawing.Size(230, 369);
			this.BestGroupBox.TabIndex = 4;
			this.BestGroupBox.TabStop = false;
			this.BestGroupBox.Text = "Best";
			// 
			// btnCopyBestInput
			// 
			this.btnCopyBestInput.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnCopyBestInput.Location = new System.Drawing.Point(12, 94);
			this.btnCopyBestInput.Name = "btnCopyBestInput";
			this.btnCopyBestInput.Size = new System.Drawing.Size(75, 23);
			this.btnCopyBestInput.TabIndex = 2005;
			this.btnCopyBestInput.Text = "&Copy";
			this.toolTip1.SetToolTip(this.btnCopyBestInput, "\"Copy to Clipboard.  Then possible to paste to text file or directly into TasStud" +
					"io.");
			this.btnCopyBestInput.UseVisualStyleBackColor = true;
			this.btnCopyBestInput.Click += new System.EventHandler(this.BtnCopyBestInput_Click);
			// 
			// PlayBestButton
			// 
			this.PlayBestButton.Enabled = false;
			this.PlayBestButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.PlayBestButton.Location = new System.Drawing.Point(12, 46);
			this.PlayBestButton.Name = "PlayBestButton";
			this.PlayBestButton.Size = new System.Drawing.Size(75, 23);
			this.PlayBestButton.TabIndex = 2004;
			this.PlayBestButton.Text = "&Play";
			this.PlayBestButton.UseVisualStyleBackColor = true;
			this.PlayBestButton.Click += new System.EventHandler(this.PlayBestButton_Click);
			// 
			// ClearBestButton
			// 
			this.ClearBestButton.Enabled = false;
			this.ClearBestButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.ClearBestButton.Location = new System.Drawing.Point(12, 70);
			this.ClearBestButton.Name = "ClearBestButton";
			this.ClearBestButton.Size = new System.Drawing.Size(75, 23);
			this.ClearBestButton.TabIndex = 2003;
			this.ClearBestButton.Text = "&Clear";
			this.ClearBestButton.UseVisualStyleBackColor = true;
			this.ClearBestButton.Click += new System.EventHandler(this.ClearBestButton_Click);
			// 
			// BestAttemptNumberLabel
			// 
			this.BestAttemptNumberLabel.Location = new System.Drawing.Point(59, 20);
			this.BestAttemptNumberLabel.Name = "BestAttemptNumberLabel";
			this.BestAttemptNumberLabel.Text = "0";
			// 
			// label17
			// 
			this.label17.Location = new System.Drawing.Point(17, 20);
			this.label17.Name = "label17";
			this.label17.Text = "Attempt:";
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.AutoScroll = true;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Controls.Add(this.BestAttemptLogLabel);
			this.panel1.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.panel1.Location = new System.Drawing.Point(12, 125);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(212, 238);
			this.panel1.TabIndex = 21;
			// 
			// BestAttemptLogLabel
			// 
			this.BestAttemptLogLabel.Location = new System.Drawing.Point(8, 8);
			this.BestAttemptLogLabel.Name = "BestAttemptLogLabel";
			this.BestAttemptLogLabel.Text = "     ";
			// 
			// BestTieBreak3Box
			// 
			this.BestTieBreak3Box.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BestTieBreak3Box.Location = new System.Drawing.Point(163, 73);
			this.BestTieBreak3Box.Name = "BestTieBreak3Box";
			this.BestTieBreak3Box.ReadOnly = true;
			this.BestTieBreak3Box.Size = new System.Drawing.Size(58, 20);
			this.BestTieBreak3Box.TabIndex = 20;
			this.BestTieBreak3Box.TabStop = false;
			// 
			// BestTieBreak2Box
			// 
			this.BestTieBreak2Box.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BestTieBreak2Box.Location = new System.Drawing.Point(163, 53);
			this.BestTieBreak2Box.Name = "BestTieBreak2Box";
			this.BestTieBreak2Box.ReadOnly = true;
			this.BestTieBreak2Box.Size = new System.Drawing.Size(58, 20);
			this.BestTieBreak2Box.TabIndex = 19;
			this.BestTieBreak2Box.TabStop = false;
			// 
			// BestTieBreak1Box
			// 
			this.BestTieBreak1Box.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BestTieBreak1Box.Location = new System.Drawing.Point(163, 33);
			this.BestTieBreak1Box.Name = "BestTieBreak1Box";
			this.BestTieBreak1Box.ReadOnly = true;
			this.BestTieBreak1Box.Size = new System.Drawing.Size(58, 20);
			this.BestTieBreak1Box.TabIndex = 18;
			this.BestTieBreak1Box.TabStop = false;
			// 
			// BestMaximizeBox
			// 
			this.BestMaximizeBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BestMaximizeBox.Location = new System.Drawing.Point(163, 13);
			this.BestMaximizeBox.Name = "BestMaximizeBox";
			this.BestMaximizeBox.ReadOnly = true;
			this.BestMaximizeBox.Size = new System.Drawing.Size(58, 20);
			this.BestMaximizeBox.TabIndex = 17;
			this.BestMaximizeBox.TabStop = false;
			// 
			// label16
			// 
			this.label16.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label16.Location = new System.Drawing.Point(96, 76);
			this.label16.Name = "label16";
			this.label16.Text = "Tiebreak 3:";
			// 
			// label15
			// 
			this.label15.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label15.Location = new System.Drawing.Point(96, 56);
			this.label15.Name = "label15";
			this.label15.Text = "Tiebreak 2:";
			// 
			// label14
			// 
			this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label14.Location = new System.Drawing.Point(96, 36);
			this.label14.Name = "label14";
			this.label14.Text = "Tiebreak 1:";
			// 
			// label13
			// 
			this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label13.Location = new System.Drawing.Point(96, 16);
			this.label13.Name = "label13";
			this.label13.Text = "Main Value:";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(3, 2);
			this.label1.Name = "label1";
			this.label1.Text = "Attempts:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(10, 17);
			this.label2.Name = "label2";
			this.label2.Text = "Frames:";
			// 
			// AttemptsLabel
			// 
			this.AttemptsLabel.Location = new System.Drawing.Point(61, 2);
			this.AttemptsLabel.Name = "AttemptsLabel";
			this.AttemptsLabel.Text = "0";
			// 
			// FramesLabel
			// 
			this.FramesLabel.Location = new System.Drawing.Point(61, 17);
			this.FramesLabel.Name = "FramesLabel";
			this.FramesLabel.Text = "0";
			// 
			// GoalGroupBox
			// 
			this.GoalGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.GoalGroupBox.Controls.Add(this.label4);
			this.GoalGroupBox.Controls.Add(this.FrameLengthNumeric);
			this.GoalGroupBox.Controls.Add(this.label3);
			this.GoalGroupBox.Controls.Add(this.panel3);
			this.GoalGroupBox.Controls.Add(this.panel4);
			this.GoalGroupBox.Controls.Add(this.panel5);
			this.GoalGroupBox.Controls.Add(this.panel6);
			this.GoalGroupBox.Location = new System.Drawing.Point(12, 27);
			this.GoalGroupBox.Name = "GoalGroupBox";
			this.GoalGroupBox.Size = new System.Drawing.Size(442, 150);
			this.GoalGroupBox.TabIndex = 9;
			this.GoalGroupBox.TabStop = false;
			this.GoalGroupBox.Text = " ";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(113, 29);
			this.label4.Name = "label4";
			this.label4.Text = "frames";
			// 
			// FrameLengthNumeric
			// 
			this.FrameLengthNumeric.Location = new System.Drawing.Point(60, 25);
			this.FrameLengthNumeric.Maximum = new decimal(new int[] {
			9999,
			0,
			0,
			0});
			this.FrameLengthNumeric.Name = "FrameLengthNumeric";
			this.FrameLengthNumeric.Size = new System.Drawing.Size(46, 20);
			this.FrameLengthNumeric.TabIndex = 1000;
			this.FrameLengthNumeric.Value = new decimal(new int[] {
			100,
			0,
			0,
			0});
			this.FrameLengthNumeric.ValueChanged += new System.EventHandler(this.FrameLengthNumeric_ValueChanged);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(7, 29);
			this.label3.Name = "label3";
			this.label3.Text = "End after";
			// 
			// panel3
			// 
			this.panel3.Controls.Add(this.MainValueNumeric);
			this.panel3.Controls.Add(this.MainValueRadio);
			this.panel3.Controls.Add(this.MainBestRadio);
			this.panel3.Controls.Add(this.MainOperator);
			this.panel3.Controls.Add(this.label9);
			this.panel3.Controls.Add(this.MaximizeAddressBox);
			this.panel3.Controls.Add(this.maximizeLabeltext);
			this.panel3.Location = new System.Drawing.Point(9, 51);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(427, 26);
			this.panel3.TabIndex = 0;
			// 
			// MainValueNumeric
			// 
			this.MainValueNumeric.Enabled = false;
			this.MainValueNumeric.Location = new System.Drawing.Point(357, 4);
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
			this.MainValueNumeric.Size = new System.Drawing.Size(61, 20);
			this.MainValueNumeric.TabIndex = 1013;
			this.MainValueNumeric.ValueChanged += new System.EventHandler(this.MainValueNumeric_ValueChanged);
			// 
			// MainValueRadio
			// 
			this.MainValueRadio.AutoSize = true;
			this.MainValueRadio.Location = new System.Drawing.Point(302, 6);
			this.MainValueRadio.Name = "MainValueRadio";
			this.MainValueRadio.Size = new System.Drawing.Size(52, 17);
			this.MainValueRadio.TabIndex = 1012;
			this.MainValueRadio.Text = "Value";
			this.MainValueRadio.UseVisualStyleBackColor = true;
			this.MainValueRadio.CheckedChanged += new System.EventHandler(this.MainValueRadio_CheckedChanged);
			// 
			// MainBestRadio
			// 
			this.MainBestRadio.AutoSize = true;
			this.MainBestRadio.Checked = true;
			this.MainBestRadio.Location = new System.Drawing.Point(256, 6);
			this.MainBestRadio.Name = "MainBestRadio";
			this.MainBestRadio.Size = new System.Drawing.Size(46, 17);
			this.MainBestRadio.TabIndex = 1011;
			this.MainBestRadio.TabStop = true;
			this.MainBestRadio.Text = "Best";
			this.MainBestRadio.UseVisualStyleBackColor = true;
			this.MainBestRadio.CheckedChanged += new System.EventHandler(this.MainBestRadio_CheckedChanged);
			// 
			// MainOperator
			// 
			this.MainOperator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.MainOperator.FormattingEnabled = true;
			this.MainOperator.Items.AddRange(new object[] {
			">",
			">=",
			"=",
			"<=",
			"<",
			"!="});
			this.MainOperator.Location = new System.Drawing.Point(208, 3);
			this.MainOperator.Name = "MainOperator";
			this.MainOperator.Size = new System.Drawing.Size(40, 21);
			this.MainOperator.TabIndex = 1010;
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(67, 7);
			this.label9.Name = "label9";
			this.label9.Text = "Address 0x";
			// 
			// MaximizeAddressBox
			// 
			this.MaximizeAddressBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.MaximizeAddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.MaximizeAddressBox.Location = new System.Drawing.Point(133, 4);
			this.MaximizeAddressBox.Name = "MaximizeAddressBox";
			this.MaximizeAddressBox.Nullable = true;
			this.MaximizeAddressBox.Size = new System.Drawing.Size(67, 20);
			this.MaximizeAddressBox.TabIndex = 1009;
			this.MaximizeAddressBox.TextChanged += new System.EventHandler(this.MaximizeAddressBox_TextChanged);
			// 
			// maximizeLabeltext
			// 
			this.maximizeLabeltext.Location = new System.Drawing.Point(1, 7);
			this.maximizeLabeltext.Name = "maximizeLabeltext";
			this.maximizeLabeltext.Text = "Main Value:";
			// 
			// panel4
			// 
			this.panel4.Controls.Add(this.TieBreak1Numeric);
			this.panel4.Controls.Add(this.TieBreak1ValueRadio);
			this.panel4.Controls.Add(this.Tiebreak1Operator);
			this.panel4.Controls.Add(this.TieBreak1BestRadio);
			this.panel4.Controls.Add(this.label5);
			this.panel4.Controls.Add(this.TieBreaker1Box);
			this.panel4.Controls.Add(this.label10);
			this.panel4.Location = new System.Drawing.Point(9, 74);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(427, 26);
			this.panel4.TabIndex = 1;
			// 
			// TieBreak1Numeric
			// 
			this.TieBreak1Numeric.Enabled = false;
			this.TieBreak1Numeric.Location = new System.Drawing.Point(357, 4);
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
			this.TieBreak1Numeric.Size = new System.Drawing.Size(61, 20);
			this.TieBreak1Numeric.TabIndex = 1013;
			this.TieBreak1Numeric.ValueChanged += new System.EventHandler(this.TieBreak1Numeric_ValueChanged);
			// 
			// TieBreak1ValueRadio
			// 
			this.TieBreak1ValueRadio.AutoSize = true;
			this.TieBreak1ValueRadio.Location = new System.Drawing.Point(302, 6);
			this.TieBreak1ValueRadio.Name = "TieBreak1ValueRadio";
			this.TieBreak1ValueRadio.Size = new System.Drawing.Size(52, 17);
			this.TieBreak1ValueRadio.TabIndex = 1012;
			this.TieBreak1ValueRadio.Text = "Value";
			this.TieBreak1ValueRadio.UseVisualStyleBackColor = true;
			this.TieBreak1ValueRadio.CheckedChanged += new System.EventHandler(this.TieBreak1ValueRadio_CheckedChanged);
			// 
			// Tiebreak1Operator
			// 
			this.Tiebreak1Operator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.Tiebreak1Operator.FormattingEnabled = true;
			this.Tiebreak1Operator.Items.AddRange(new object[] {
			">",
			">=",
			"=",
			"<=",
			"<",
			"!="});
			this.Tiebreak1Operator.Location = new System.Drawing.Point(208, 3);
			this.Tiebreak1Operator.Name = "Tiebreak1Operator";
			this.Tiebreak1Operator.Size = new System.Drawing.Size(40, 21);
			this.Tiebreak1Operator.TabIndex = 1007;
			// 
			// TieBreak1BestRadio
			// 
			this.TieBreak1BestRadio.AutoSize = true;
			this.TieBreak1BestRadio.Checked = true;
			this.TieBreak1BestRadio.Location = new System.Drawing.Point(256, 6);
			this.TieBreak1BestRadio.Name = "TieBreak1BestRadio";
			this.TieBreak1BestRadio.Size = new System.Drawing.Size(46, 17);
			this.TieBreak1BestRadio.TabIndex = 1011;
			this.TieBreak1BestRadio.TabStop = true;
			this.TieBreak1BestRadio.Text = "Best";
			this.TieBreak1BestRadio.UseVisualStyleBackColor = true;
			this.TieBreak1BestRadio.CheckedChanged += new System.EventHandler(this.Tiebreak1BestRadio_CheckedChanged);
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(1, 7);
			this.label5.Name = "label5";
			this.label5.Text = "Tiebreak 1:";
			// 
			// TieBreaker1Box
			// 
			this.TieBreaker1Box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.TieBreaker1Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.TieBreaker1Box.Location = new System.Drawing.Point(133, 4);
			this.TieBreaker1Box.Name = "TieBreaker1Box";
			this.TieBreaker1Box.Nullable = true;
			this.TieBreaker1Box.Size = new System.Drawing.Size(67, 20);
			this.TieBreaker1Box.TabIndex = 1002;
			// 
			// label10
			// 
			this.label10.Location = new System.Drawing.Point(67, 7);
			this.label10.Name = "label10";
			this.label10.Text = "Address 0x";
			// 
			// panel5
			// 
			this.panel5.Controls.Add(this.TieBreak2Numeric);
			this.panel5.Controls.Add(this.Tiebreak2Operator);
			this.panel5.Controls.Add(this.TieBreak2ValueRadio);
			this.panel5.Controls.Add(this.TieBreak2BestRadio);
			this.panel5.Controls.Add(this.label11);
			this.panel5.Controls.Add(this.label6);
			this.panel5.Controls.Add(this.TieBreaker2Box);
			this.panel5.Location = new System.Drawing.Point(9, 97);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(427, 26);
			this.panel5.TabIndex = 2;
			// 
			// TieBreak2Numeric
			// 
			this.TieBreak2Numeric.Enabled = false;
			this.TieBreak2Numeric.Location = new System.Drawing.Point(357, 4);
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
			this.TieBreak2Numeric.Size = new System.Drawing.Size(61, 20);
			this.TieBreak2Numeric.TabIndex = 1013;
			this.TieBreak2Numeric.ValueChanged += new System.EventHandler(this.TieBreak2Numeric_ValueChanged);
			// 
			// Tiebreak2Operator
			// 
			this.Tiebreak2Operator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.Tiebreak2Operator.FormattingEnabled = true;
			this.Tiebreak2Operator.Items.AddRange(new object[] {
			">",
			">=",
			"=",
			"<=",
			"<",
			"!="});
			this.Tiebreak2Operator.Location = new System.Drawing.Point(208, 3);
			this.Tiebreak2Operator.Name = "Tiebreak2Operator";
			this.Tiebreak2Operator.Size = new System.Drawing.Size(40, 21);
			this.Tiebreak2Operator.TabIndex = 1008;
			// 
			// TieBreak2ValueRadio
			// 
			this.TieBreak2ValueRadio.AutoSize = true;
			this.TieBreak2ValueRadio.Location = new System.Drawing.Point(302, 6);
			this.TieBreak2ValueRadio.Name = "TieBreak2ValueRadio";
			this.TieBreak2ValueRadio.Size = new System.Drawing.Size(52, 17);
			this.TieBreak2ValueRadio.TabIndex = 1012;
			this.TieBreak2ValueRadio.Text = "Value";
			this.TieBreak2ValueRadio.UseVisualStyleBackColor = true;
			this.TieBreak2ValueRadio.CheckedChanged += new System.EventHandler(this.TieBreak2ValueRadio_CheckedChanged);
			// 
			// TieBreak2BestRadio
			// 
			this.TieBreak2BestRadio.AutoSize = true;
			this.TieBreak2BestRadio.Checked = true;
			this.TieBreak2BestRadio.Location = new System.Drawing.Point(256, 6);
			this.TieBreak2BestRadio.Name = "TieBreak2BestRadio";
			this.TieBreak2BestRadio.Size = new System.Drawing.Size(46, 17);
			this.TieBreak2BestRadio.TabIndex = 1011;
			this.TieBreak2BestRadio.TabStop = true;
			this.TieBreak2BestRadio.Text = "Best";
			this.TieBreak2BestRadio.UseVisualStyleBackColor = true;
			this.TieBreak2BestRadio.CheckedChanged += new System.EventHandler(this.Tiebreak2BestRadio_CheckedChanged);
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(67, 7);
			this.label11.Name = "label11";
			this.label11.Text = "Address 0x";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(1, 7);
			this.label6.Name = "label6";
			this.label6.Text = "Tiebreak 2:";
			// 
			// TieBreaker2Box
			// 
			this.TieBreaker2Box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.TieBreaker2Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.TieBreaker2Box.Location = new System.Drawing.Point(133, 4);
			this.TieBreaker2Box.Name = "TieBreaker2Box";
			this.TieBreaker2Box.Nullable = true;
			this.TieBreaker2Box.Size = new System.Drawing.Size(67, 20);
			this.TieBreaker2Box.TabIndex = 1003;
			// 
			// panel6
			// 
			this.panel6.Controls.Add(this.label12);
			this.panel6.Controls.Add(this.label7);
			this.panel6.Controls.Add(this.TieBreaker3Box);
			this.panel6.Controls.Add(this.TieBreak3Numeric);
			this.panel6.Controls.Add(this.TieBreak3ValueRadio);
			this.panel6.Controls.Add(this.TieBreak3BestRadio);
			this.panel6.Controls.Add(this.Tiebreak3Operator);
			this.panel6.Location = new System.Drawing.Point(9, 120);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(427, 26);
			this.panel6.TabIndex = 3;
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(67, 7);
			this.label12.Name = "label12";
			this.label12.Text = "Address 0x";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(1, 7);
			this.label7.Name = "label7";
			this.label7.Text = "Tiebreak 3:";
			// 
			// TieBreaker3Box
			// 
			this.TieBreaker3Box.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.TieBreaker3Box.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.TieBreaker3Box.Location = new System.Drawing.Point(133, 4);
			this.TieBreaker3Box.Name = "TieBreaker3Box";
			this.TieBreaker3Box.Nullable = true;
			this.TieBreaker3Box.Size = new System.Drawing.Size(67, 20);
			this.TieBreaker3Box.TabIndex = 1016;
			// 
			// TieBreak3Numeric
			// 
			this.TieBreak3Numeric.Enabled = false;
			this.TieBreak3Numeric.Location = new System.Drawing.Point(357, 4);
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
			this.TieBreak3Numeric.Size = new System.Drawing.Size(61, 20);
			this.TieBreak3Numeric.TabIndex = 1013;
			this.TieBreak3Numeric.ValueChanged += new System.EventHandler(this.TieBreak3Numeric_ValueChanged);
			// 
			// TieBreak3ValueRadio
			// 
			this.TieBreak3ValueRadio.AutoSize = true;
			this.TieBreak3ValueRadio.Location = new System.Drawing.Point(302, 6);
			this.TieBreak3ValueRadio.Name = "TieBreak3ValueRadio";
			this.TieBreak3ValueRadio.Size = new System.Drawing.Size(52, 17);
			this.TieBreak3ValueRadio.TabIndex = 1012;
			this.TieBreak3ValueRadio.Text = "Value";
			this.TieBreak3ValueRadio.UseVisualStyleBackColor = true;
			this.TieBreak3ValueRadio.CheckedChanged += new System.EventHandler(this.TieBreak3ValueRadio_CheckedChanged);
			// 
			// TieBreak3BestRadio
			// 
			this.TieBreak3BestRadio.AutoSize = true;
			this.TieBreak3BestRadio.Checked = true;
			this.TieBreak3BestRadio.Location = new System.Drawing.Point(256, 6);
			this.TieBreak3BestRadio.Name = "TieBreak3BestRadio";
			this.TieBreak3BestRadio.Size = new System.Drawing.Size(46, 17);
			this.TieBreak3BestRadio.TabIndex = 1011;
			this.TieBreak3BestRadio.TabStop = true;
			this.TieBreak3BestRadio.Text = "Best";
			this.TieBreak3BestRadio.UseVisualStyleBackColor = true;
			this.TieBreak3BestRadio.CheckedChanged += new System.EventHandler(this.Tiebreak3BestRadio_CheckedChanged);
			// 
			// Tiebreak3Operator
			// 
			this.Tiebreak3Operator.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.Tiebreak3Operator.FormattingEnabled = true;
			this.Tiebreak3Operator.Items.AddRange(new object[] {
			">",
			">=",
			"=",
			"<=",
			"<",
			"!="});
			this.Tiebreak3Operator.Location = new System.Drawing.Point(208, 3);
			this.Tiebreak3Operator.Name = "Tiebreak3Operator";
			this.Tiebreak3Operator.Size = new System.Drawing.Size(40, 21);
			this.Tiebreak3Operator.TabIndex = 1017;
			// 
			// StopBtn
			// 
			this.StopBtn.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.StopBtn.Location = new System.Drawing.Point(6, 56);
			this.StopBtn.Name = "StopBtn";
			this.StopBtn.Size = new System.Drawing.Size(75, 23);
			this.StopBtn.TabIndex = 2002;
			this.StopBtn.Text = "&Stop";
			this.StopBtn.UseVisualStyleBackColor = true;
			this.StopBtn.Visible = false;
			this.StopBtn.Click += new System.EventHandler(this.StopBtn_Click);
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(7, 29);
			this.label8.Name = "label8";
			this.label8.Text = "Start From:";
			// 
			// StartFromSlotBox
			// 
			this.StartFromSlotBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.StartFromSlotBox.FormattingEnabled = true;
			this.StartFromSlotBox.Items.AddRange(new object[] {
			"Slot 1",
			"Slot 2",
			"Slot 3",
			"Slot 4",
			"Slot 5",
			"Slot 6",
			"Slot 7",
			"Slot 8",
			"Slot 9",
			"Slot 10"});
			this.StartFromSlotBox.Location = new System.Drawing.Point(71, 25);
			this.StartFromSlotBox.Name = "StartFromSlotBox";
			this.StartFromSlotBox.Size = new System.Drawing.Size(75, 21);
			this.StartFromSlotBox.TabIndex = 2000;
			// 
			// ControlGroupBox
			// 
			this.ControlGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ControlGroupBox.Controls.Add(this.InvisibleEmulationCheckBox);
			this.ControlGroupBox.Controls.Add(this.panel2);
			this.ControlGroupBox.Controls.Add(this.StopBtn);
			this.ControlGroupBox.Controls.Add(this.RunBtn);
			this.ControlGroupBox.Controls.Add(this.StartFromSlotBox);
			this.ControlGroupBox.Controls.Add(this.label8);
			this.ControlGroupBox.Location = new System.Drawing.Point(460, 27);
			this.ControlGroupBox.Name = "ControlGroupBox";
			this.ControlGroupBox.Size = new System.Drawing.Size(230, 150);
			this.ControlGroupBox.TabIndex = 2004;
			this.ControlGroupBox.TabStop = false;
			this.ControlGroupBox.Text = "Control";
			// 
			// InvisibleEmulationCheckBox
			// 
			this.InvisibleEmulationCheckBox.AutoSize = true;
			this.InvisibleEmulationCheckBox.Location = new System.Drawing.Point(88, 60);
			this.InvisibleEmulationCheckBox.Name = "InvisibleEmulationCheckBox";
			this.InvisibleEmulationCheckBox.Size = new System.Drawing.Size(127, 17);
			this.InvisibleEmulationCheckBox.TabIndex = 2004;
			this.InvisibleEmulationCheckBox.Text = "Turn Off Audio/Video";
			this.InvisibleEmulationCheckBox.UseVisualStyleBackColor = true;
			this.InvisibleEmulationCheckBox.CheckedChanged += new System.EventHandler(this.InvisibleEmulationCheckBox_CheckedChanged);
			// 
			// panel2
			// 
			this.panel2.ContextMenuStrip = this.StatsContextMenu;
			this.panel2.Controls.Add(this.label1);
			this.panel2.Controls.Add(this.label2);
			this.panel2.Controls.Add(this.FramesLabel);
			this.panel2.Controls.Add(this.AttemptsLabel);
			this.panel2.Location = new System.Drawing.Point(6, 85);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(140, 33);
			this.panel2.TabIndex = 2003;
			// 
			// StatsContextMenu
			// 
			this.StatsContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.ClearStatsContextMenuItem});
			this.StatsContextMenu.Name = "StatsContextMenu";
			this.StatsContextMenu.Size = new System.Drawing.Size(102, 26);
			// 
			// ClearStatsContextMenuItem
			// 
			this.ClearStatsContextMenuItem.Text = "&Clear";
			this.ClearStatsContextMenuItem.Click += new System.EventHandler(this.ClearStatsContextMenuItem_Click);
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.Text = "Help";
			this.helpToolStripMenuItem.Click += new System.EventHandler(this.HelpToolStripMenuItem_Click);
			// 
			// BasicBot
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(707, 587);
			this.Controls.Add(this.ControlGroupBox);
			this.Controls.Add(this.GoalGroupBox);
			this.Controls.Add(this.BestGroupBox);
			this.Controls.Add(this.ControlsBox);
			this.Controls.Add(this.BotStatusStrip);
			this.Controls.Add(this.BotMenu);
			this.MainMenuStrip = this.BotMenu;
			this.Name = "BasicBot";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
		private System.Windows.Forms.CheckBox InvisibleEmulationCheckBox;
	}
}
