namespace BizHawk.Client.EmuHawk
{
	partial class Atari2600Debugger
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Atari2600Debugger));
			this.DebuggerMenu = new System.Windows.Forms.MenuStrip();
			this.FileSubMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.ExitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StepBtn = new System.Windows.Forms.Button();
			this.ScanlineAdvanceBtn = new System.Windows.Forms.Button();
			this.FrameAdvButton = new System.Windows.Forms.Button();
			this.RegistersBox = new System.Windows.Forms.GroupBox();
			this.CFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.ZFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.IFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.DFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.BFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.TFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.VFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.NFlagCheckbox = new System.Windows.Forms.CheckBox();
			this.YRegisterHexBox = new System.Windows.Forms.TextBox();
			this.XRegisterHexBox = new System.Windows.Forms.TextBox();
			this.ARegisterHexBox = new System.Windows.Forms.TextBox();
			this.SPRegisterHexBox = new System.Windows.Forms.TextBox();
			this.YRegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.XRegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.ARegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.SPRegisterBinaryBox = new System.Windows.Forms.TextBox();
			this.YRegisterBox = new System.Windows.Forms.TextBox();
			this.XRegisterBox = new System.Windows.Forms.TextBox();
			this.ARegisterBox = new System.Windows.Forms.TextBox();
			this.SPRegisterBox = new System.Windows.Forms.TextBox();
			this.PCRegisterBox = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.CoreInfoBox = new System.Windows.Forms.GroupBox();
			this.ScanlineBox = new System.Windows.Forms.TextBox();
			this.label8 = new System.Windows.Forms.Label();
			this.FrameCountBox = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.DebuggerMenu.SuspendLayout();
			this.RegistersBox.SuspendLayout();
			this.CoreInfoBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// DebuggerMenu
			// 
			this.DebuggerMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
			this.DebuggerMenu.Location = new System.Drawing.Point(0, 0);
			this.DebuggerMenu.Name = "DebuggerMenu";
			this.DebuggerMenu.Size = new System.Drawing.Size(534, 24);
			this.DebuggerMenu.TabIndex = 0;
			this.DebuggerMenu.Text = "menuStrip1";
			// 
			// FileSubMenu
			// 
			this.FileSubMenu.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ExitMenuItem});
			this.FileSubMenu.Name = "FileSubMenu";
			this.FileSubMenu.Size = new System.Drawing.Size(37, 20);
			this.FileSubMenu.Text = "&File";
			// 
			// ExitMenuItem
			// 
			this.ExitMenuItem.Name = "ExitMenuItem";
			this.ExitMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.ExitMenuItem.Size = new System.Drawing.Size(145, 22);
			this.ExitMenuItem.Text = "&Close";
			this.ExitMenuItem.Click += new System.EventHandler(this.ExitMenuItem_Click);
			// 
			// StepBtn
			// 
			this.StepBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.StepBtn.Location = new System.Drawing.Point(447, 27);
			this.StepBtn.Name = "StepBtn";
			this.StepBtn.Size = new System.Drawing.Size(75, 23);
			this.StepBtn.TabIndex = 1;
			this.StepBtn.Text = "&Step";
			this.StepBtn.UseVisualStyleBackColor = true;
			this.StepBtn.Click += new System.EventHandler(this.StepBtn_Click);
			// 
			// ScanlineAdvanceBtn
			// 
			this.ScanlineAdvanceBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ScanlineAdvanceBtn.Location = new System.Drawing.Point(447, 56);
			this.ScanlineAdvanceBtn.Name = "ScanlineAdvanceBtn";
			this.ScanlineAdvanceBtn.Size = new System.Drawing.Size(75, 23);
			this.ScanlineAdvanceBtn.TabIndex = 2;
			this.ScanlineAdvanceBtn.Text = "&Scan +1";
			this.ScanlineAdvanceBtn.UseVisualStyleBackColor = true;
			this.ScanlineAdvanceBtn.Click += new System.EventHandler(this.ScanlineAdvanceBtn_Click);
			// 
			// FrameAdvButton
			// 
			this.FrameAdvButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.FrameAdvButton.Location = new System.Drawing.Point(447, 85);
			this.FrameAdvButton.Name = "FrameAdvButton";
			this.FrameAdvButton.Size = new System.Drawing.Size(75, 23);
			this.FrameAdvButton.TabIndex = 3;
			this.FrameAdvButton.Text = "&Frame";
			this.FrameAdvButton.UseVisualStyleBackColor = true;
			this.FrameAdvButton.Click += new System.EventHandler(this.FrameAdvButton_Click);
			// 
			// RegistersBox
			// 
			this.RegistersBox.Controls.Add(this.CFlagCheckbox);
			this.RegistersBox.Controls.Add(this.ZFlagCheckbox);
			this.RegistersBox.Controls.Add(this.IFlagCheckbox);
			this.RegistersBox.Controls.Add(this.DFlagCheckbox);
			this.RegistersBox.Controls.Add(this.BFlagCheckbox);
			this.RegistersBox.Controls.Add(this.TFlagCheckbox);
			this.RegistersBox.Controls.Add(this.VFlagCheckbox);
			this.RegistersBox.Controls.Add(this.label6);
			this.RegistersBox.Controls.Add(this.NFlagCheckbox);
			this.RegistersBox.Controls.Add(this.YRegisterHexBox);
			this.RegistersBox.Controls.Add(this.XRegisterHexBox);
			this.RegistersBox.Controls.Add(this.ARegisterHexBox);
			this.RegistersBox.Controls.Add(this.SPRegisterHexBox);
			this.RegistersBox.Controls.Add(this.YRegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.XRegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.ARegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.SPRegisterBinaryBox);
			this.RegistersBox.Controls.Add(this.YRegisterBox);
			this.RegistersBox.Controls.Add(this.XRegisterBox);
			this.RegistersBox.Controls.Add(this.ARegisterBox);
			this.RegistersBox.Controls.Add(this.SPRegisterBox);
			this.RegistersBox.Controls.Add(this.PCRegisterBox);
			this.RegistersBox.Controls.Add(this.label5);
			this.RegistersBox.Controls.Add(this.label4);
			this.RegistersBox.Controls.Add(this.label3);
			this.RegistersBox.Controls.Add(this.label2);
			this.RegistersBox.Controls.Add(this.label1);
			this.RegistersBox.Location = new System.Drawing.Point(12, 27);
			this.RegistersBox.Name = "RegistersBox";
			this.RegistersBox.Size = new System.Drawing.Size(242, 155);
			this.RegistersBox.TabIndex = 4;
			this.RegistersBox.TabStop = false;
			this.RegistersBox.Text = "Registers";
			// 
			// CFlagCheckbox
			// 
			this.CFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.CFlagCheckbox.AutoSize = true;
			this.CFlagCheckbox.Location = new System.Drawing.Point(209, 121);
			this.CFlagCheckbox.Name = "CFlagCheckbox";
			this.CFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.CFlagCheckbox.TabIndex = 26;
			this.CFlagCheckbox.Text = "C";
			this.CFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// ZFlagCheckbox
			// 
			this.ZFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.ZFlagCheckbox.AutoSize = true;
			this.ZFlagCheckbox.Location = new System.Drawing.Point(186, 121);
			this.ZFlagCheckbox.Name = "ZFlagCheckbox";
			this.ZFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.ZFlagCheckbox.TabIndex = 25;
			this.ZFlagCheckbox.Text = "Z";
			this.ZFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// IFlagCheckbox
			// 
			this.IFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.IFlagCheckbox.AutoSize = true;
			this.IFlagCheckbox.Location = new System.Drawing.Point(167, 121);
			this.IFlagCheckbox.Name = "IFlagCheckbox";
			this.IFlagCheckbox.Size = new System.Drawing.Size(20, 23);
			this.IFlagCheckbox.TabIndex = 24;
			this.IFlagCheckbox.Text = "I";
			this.IFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// DFlagCheckbox
			// 
			this.DFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.DFlagCheckbox.AutoSize = true;
			this.DFlagCheckbox.Location = new System.Drawing.Point(143, 121);
			this.DFlagCheckbox.Name = "DFlagCheckbox";
			this.DFlagCheckbox.Size = new System.Drawing.Size(25, 23);
			this.DFlagCheckbox.TabIndex = 23;
			this.DFlagCheckbox.Text = "D";
			this.DFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// BFlagCheckbox
			// 
			this.BFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.BFlagCheckbox.AutoSize = true;
			this.BFlagCheckbox.Location = new System.Drawing.Point(118, 121);
			this.BFlagCheckbox.Name = "BFlagCheckbox";
			this.BFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.BFlagCheckbox.TabIndex = 22;
			this.BFlagCheckbox.Text = "B";
			this.BFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// TFlagCheckbox
			// 
			this.TFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.TFlagCheckbox.AutoSize = true;
			this.TFlagCheckbox.Location = new System.Drawing.Point(95, 121);
			this.TFlagCheckbox.Name = "TFlagCheckbox";
			this.TFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.TFlagCheckbox.TabIndex = 21;
			this.TFlagCheckbox.Text = "T";
			this.TFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// VFlagCheckbox
			// 
			this.VFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.VFlagCheckbox.AutoSize = true;
			this.VFlagCheckbox.Location = new System.Drawing.Point(72, 121);
			this.VFlagCheckbox.Name = "VFlagCheckbox";
			this.VFlagCheckbox.Size = new System.Drawing.Size(24, 23);
			this.VFlagCheckbox.TabIndex = 20;
			this.VFlagCheckbox.Text = "V";
			this.VFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(7, 126);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(35, 13);
			this.label6.TabIndex = 19;
			this.label6.Text = "Flags:";
			// 
			// NFlagCheckbox
			// 
			this.NFlagCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.NFlagCheckbox.AutoSize = true;
			this.NFlagCheckbox.Location = new System.Drawing.Point(48, 121);
			this.NFlagCheckbox.Name = "NFlagCheckbox";
			this.NFlagCheckbox.Size = new System.Drawing.Size(25, 23);
			this.NFlagCheckbox.TabIndex = 18;
			this.NFlagCheckbox.Text = "N";
			this.NFlagCheckbox.UseVisualStyleBackColor = true;
			// 
			// YRegisterHexBox
			// 
			this.YRegisterHexBox.Location = new System.Drawing.Point(86, 97);
			this.YRegisterHexBox.Name = "YRegisterHexBox";
			this.YRegisterHexBox.ReadOnly = true;
			this.YRegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.YRegisterHexBox.TabIndex = 17;
			// 
			// XRegisterHexBox
			// 
			this.XRegisterHexBox.Location = new System.Drawing.Point(86, 77);
			this.XRegisterHexBox.Name = "XRegisterHexBox";
			this.XRegisterHexBox.ReadOnly = true;
			this.XRegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.XRegisterHexBox.TabIndex = 16;
			// 
			// ARegisterHexBox
			// 
			this.ARegisterHexBox.Location = new System.Drawing.Point(86, 57);
			this.ARegisterHexBox.Name = "ARegisterHexBox";
			this.ARegisterHexBox.ReadOnly = true;
			this.ARegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.ARegisterHexBox.TabIndex = 15;
			// 
			// SPRegisterHexBox
			// 
			this.SPRegisterHexBox.Location = new System.Drawing.Point(86, 37);
			this.SPRegisterHexBox.Name = "SPRegisterHexBox";
			this.SPRegisterHexBox.ReadOnly = true;
			this.SPRegisterHexBox.Size = new System.Drawing.Size(32, 20);
			this.SPRegisterHexBox.TabIndex = 14;
			// 
			// YRegisterBinaryBox
			// 
			this.YRegisterBinaryBox.Location = new System.Drawing.Point(121, 97);
			this.YRegisterBinaryBox.Name = "YRegisterBinaryBox";
			this.YRegisterBinaryBox.ReadOnly = true;
			this.YRegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.YRegisterBinaryBox.TabIndex = 13;
			this.YRegisterBinaryBox.Text = "0000 0000";
			// 
			// XRegisterBinaryBox
			// 
			this.XRegisterBinaryBox.Location = new System.Drawing.Point(121, 77);
			this.XRegisterBinaryBox.Name = "XRegisterBinaryBox";
			this.XRegisterBinaryBox.ReadOnly = true;
			this.XRegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.XRegisterBinaryBox.TabIndex = 12;
			this.XRegisterBinaryBox.Text = "0000 0000";
			// 
			// ARegisterBinaryBox
			// 
			this.ARegisterBinaryBox.Location = new System.Drawing.Point(121, 57);
			this.ARegisterBinaryBox.Name = "ARegisterBinaryBox";
			this.ARegisterBinaryBox.ReadOnly = true;
			this.ARegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.ARegisterBinaryBox.TabIndex = 11;
			this.ARegisterBinaryBox.Text = "0000 0000";
			// 
			// SPRegisterBinaryBox
			// 
			this.SPRegisterBinaryBox.Location = new System.Drawing.Point(121, 37);
			this.SPRegisterBinaryBox.Name = "SPRegisterBinaryBox";
			this.SPRegisterBinaryBox.ReadOnly = true;
			this.SPRegisterBinaryBox.Size = new System.Drawing.Size(62, 20);
			this.SPRegisterBinaryBox.TabIndex = 10;
			this.SPRegisterBinaryBox.Text = "0000 0000";
			// 
			// YRegisterBox
			// 
			this.YRegisterBox.Location = new System.Drawing.Point(36, 97);
			this.YRegisterBox.Name = "YRegisterBox";
			this.YRegisterBox.ReadOnly = true;
			this.YRegisterBox.Size = new System.Drawing.Size(32, 20);
			this.YRegisterBox.TabIndex = 9;
			// 
			// XRegisterBox
			// 
			this.XRegisterBox.Location = new System.Drawing.Point(36, 77);
			this.XRegisterBox.Name = "XRegisterBox";
			this.XRegisterBox.ReadOnly = true;
			this.XRegisterBox.Size = new System.Drawing.Size(32, 20);
			this.XRegisterBox.TabIndex = 8;
			// 
			// ARegisterBox
			// 
			this.ARegisterBox.Location = new System.Drawing.Point(36, 57);
			this.ARegisterBox.Name = "ARegisterBox";
			this.ARegisterBox.ReadOnly = true;
			this.ARegisterBox.Size = new System.Drawing.Size(32, 20);
			this.ARegisterBox.TabIndex = 7;
			// 
			// SPRegisterBox
			// 
			this.SPRegisterBox.Location = new System.Drawing.Point(36, 37);
			this.SPRegisterBox.Name = "SPRegisterBox";
			this.SPRegisterBox.ReadOnly = true;
			this.SPRegisterBox.Size = new System.Drawing.Size(32, 20);
			this.SPRegisterBox.TabIndex = 6;
			// 
			// PCRegisterBox
			// 
			this.PCRegisterBox.Location = new System.Drawing.Point(36, 16);
			this.PCRegisterBox.Name = "PCRegisterBox";
			this.PCRegisterBox.ReadOnly = true;
			this.PCRegisterBox.Size = new System.Drawing.Size(72, 20);
			this.PCRegisterBox.TabIndex = 5;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 100);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(17, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "Y:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(6, 80);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(17, 13);
			this.label4.TabIndex = 3;
			this.label4.Text = "X:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 60);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(17, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "A:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(24, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "SP:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(24, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "PC:";
			// 
			// CoreInfoBox
			// 
			this.CoreInfoBox.Controls.Add(this.ScanlineBox);
			this.CoreInfoBox.Controls.Add(this.label8);
			this.CoreInfoBox.Controls.Add(this.FrameCountBox);
			this.CoreInfoBox.Controls.Add(this.label7);
			this.CoreInfoBox.Location = new System.Drawing.Point(260, 27);
			this.CoreInfoBox.Name = "CoreInfoBox";
			this.CoreInfoBox.Size = new System.Drawing.Size(160, 155);
			this.CoreInfoBox.TabIndex = 5;
			this.CoreInfoBox.TabStop = false;
			// 
			// ScanlineBox
			// 
			this.ScanlineBox.Location = new System.Drawing.Point(58, 37);
			this.ScanlineBox.Name = "ScanlineBox";
			this.ScanlineBox.ReadOnly = true;
			this.ScanlineBox.Size = new System.Drawing.Size(74, 20);
			this.ScanlineBox.TabIndex = 3;
			this.ScanlineBox.Text = "Todo";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(6, 40);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(51, 13);
			this.label8.TabIndex = 2;
			this.label8.Text = "Scanline:";
			// 
			// FrameCountBox
			// 
			this.FrameCountBox.Location = new System.Drawing.Point(58, 13);
			this.FrameCountBox.Name = "FrameCountBox";
			this.FrameCountBox.ReadOnly = true;
			this.FrameCountBox.Size = new System.Drawing.Size(74, 20);
			this.FrameCountBox.TabIndex = 1;
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 16);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(39, 13);
			this.label7.TabIndex = 0;
			this.label7.Text = "Frame:";
			// 
			// Atari2600Debugger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(534, 498);
			this.Controls.Add(this.CoreInfoBox);
			this.Controls.Add(this.RegistersBox);
			this.Controls.Add(this.FrameAdvButton);
			this.Controls.Add(this.ScanlineAdvanceBtn);
			this.Controls.Add(this.StepBtn);
			this.Controls.Add(this.DebuggerMenu);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.DebuggerMenu;
			this.Name = "Atari2600Debugger";
			this.Text = " Debugger";
			this.Load += new System.EventHandler(this.Atari2600Debugger_Load);
			this.DebuggerMenu.ResumeLayout(false);
			this.DebuggerMenu.PerformLayout();
			this.RegistersBox.ResumeLayout(false);
			this.RegistersBox.PerformLayout();
			this.CoreInfoBox.ResumeLayout(false);
			this.CoreInfoBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip DebuggerMenu;
		private System.Windows.Forms.ToolStripMenuItem FileSubMenu;
		private System.Windows.Forms.ToolStripMenuItem ExitMenuItem;
		private System.Windows.Forms.Button StepBtn;
		private System.Windows.Forms.Button ScanlineAdvanceBtn;
		private System.Windows.Forms.Button FrameAdvButton;
		private System.Windows.Forms.GroupBox RegistersBox;
		private System.Windows.Forms.TextBox YRegisterBox;
		private System.Windows.Forms.TextBox XRegisterBox;
		private System.Windows.Forms.TextBox ARegisterBox;
		private System.Windows.Forms.TextBox SPRegisterBox;
		private System.Windows.Forms.TextBox PCRegisterBox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox SPRegisterBinaryBox;
		private System.Windows.Forms.TextBox YRegisterBinaryBox;
		private System.Windows.Forms.TextBox XRegisterBinaryBox;
		private System.Windows.Forms.TextBox ARegisterBinaryBox;
		private System.Windows.Forms.TextBox YRegisterHexBox;
		private System.Windows.Forms.TextBox XRegisterHexBox;
		private System.Windows.Forms.TextBox ARegisterHexBox;
		private System.Windows.Forms.TextBox SPRegisterHexBox;
		private System.Windows.Forms.CheckBox CFlagCheckbox;
		private System.Windows.Forms.CheckBox ZFlagCheckbox;
		private System.Windows.Forms.CheckBox IFlagCheckbox;
		private System.Windows.Forms.CheckBox DFlagCheckbox;
		private System.Windows.Forms.CheckBox BFlagCheckbox;
		private System.Windows.Forms.CheckBox TFlagCheckbox;
		private System.Windows.Forms.CheckBox VFlagCheckbox;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.CheckBox NFlagCheckbox;
		private System.Windows.Forms.GroupBox CoreInfoBox;
		private System.Windows.Forms.TextBox FrameCountBox;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox ScanlineBox;
		private System.Windows.Forms.Label label8;
	}
}