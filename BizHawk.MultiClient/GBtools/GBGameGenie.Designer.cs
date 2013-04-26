namespace BizHawk.MultiClient
{
	partial class GBGameGenie
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
			this.GameGenieCodeBox = new System.Windows.Forms.GroupBox();
			this.GGCodeMaskBox = new System.Windows.Forms.MaskedTextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.ValueBox = new BizHawk.HexTextBox();
			this.AddressBox = new BizHawk.HexTextBox();
			this.addcheatbt = new System.Windows.Forms.Button();
			this.ButtonPanel = new System.Windows.Forms.Panel();
			this.BF = new System.Windows.Forms.Button();
			this.BD = new System.Windows.Forms.Button();
			this.BB = new System.Windows.Forms.Button();
			this.B9 = new System.Windows.Forms.Button();
			this.B7 = new System.Windows.Forms.Button();
			this.B5 = new System.Windows.Forms.Button();
			this.B3 = new System.Windows.Forms.Button();
			this.BE = new System.Windows.Forms.Button();
			this.B6 = new System.Windows.Forms.Button();
			this.B1 = new System.Windows.Forms.Button();
			this.BC = new System.Windows.Forms.Button();
			this.BA = new System.Windows.Forms.Button();
			this.B8 = new System.Windows.Forms.Button();
			this.B4 = new System.Windows.Forms.Button();
			this.B2 = new System.Windows.Forms.Button();
			this.B0 = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.ClearButton = new System.Windows.Forms.Button();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.cheatname = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.CompareBox = new BizHawk.HexTextBox();
			this.GameGenieCodeBox.SuspendLayout();
			this.ButtonPanel.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// GameGenieCodeBox
			// 
			this.GameGenieCodeBox.Controls.Add(this.GGCodeMaskBox);
			this.GameGenieCodeBox.Location = new System.Drawing.Point(41, 127);
			this.GameGenieCodeBox.Margin = new System.Windows.Forms.Padding(4);
			this.GameGenieCodeBox.Name = "GameGenieCodeBox";
			this.GameGenieCodeBox.Padding = new System.Windows.Forms.Padding(4);
			this.GameGenieCodeBox.Size = new System.Drawing.Size(158, 66);
			this.GameGenieCodeBox.TabIndex = 1;
			this.GameGenieCodeBox.TabStop = false;
			this.GameGenieCodeBox.Text = "Game Genie Code";
			// 
			// GGCodeMaskBox
			// 
			this.GGCodeMaskBox.Location = new System.Drawing.Point(33, 32);
			this.GGCodeMaskBox.Mask = ">AAA-AAA-AAA";
			this.GGCodeMaskBox.Name = "GGCodeMaskBox";
			this.GGCodeMaskBox.Size = new System.Drawing.Size(99, 22);
			this.GGCodeMaskBox.TabIndex = 10;
			this.GGCodeMaskBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.GGCodeMaskBox.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
			this.GGCodeMaskBox.TextChanged += new System.EventHandler(this.GGCodeMaskBox_TextChanged);
			this.GGCodeMaskBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.GGCodeMaskBox_KeyPress);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(11, 80);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(44, 17);
			this.label3.TabIndex = 5;
			this.label3.Text = "Value";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(11, 24);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(60, 17);
			this.label1.TabIndex = 3;
			this.label1.Text = "Address";
			// 
			// ValueBox
			// 
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(140, 75);
			this.ValueBox.Margin = new System.Windows.Forms.Padding(4);
			this.ValueBox.MaxLength = 2;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.ValueBox.Size = new System.Drawing.Size(35, 22);
			this.ValueBox.TabIndex = 23;
			this.ValueBox.TextChanged += new System.EventHandler(this.ValueBox_TextChanged);
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(117, 20);
			this.AddressBox.Margin = new System.Windows.Forms.Padding(4);
			this.AddressBox.MaxLength = 4;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.AddressBox.Size = new System.Drawing.Size(58, 22);
			this.AddressBox.TabIndex = 21;
			this.AddressBox.TextChanged += new System.EventHandler(this.AddressBox_TextChanged);
			// 
			// addcheatbt
			// 
			this.addcheatbt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.addcheatbt.Enabled = false;
			this.addcheatbt.Location = new System.Drawing.Point(117, 201);
			this.addcheatbt.Margin = new System.Windows.Forms.Padding(4);
			this.addcheatbt.Name = "addcheatbt";
			this.addcheatbt.Size = new System.Drawing.Size(82, 32);
			this.addcheatbt.TabIndex = 33;
			this.addcheatbt.Text = "&Add Cheat";
			this.addcheatbt.UseVisualStyleBackColor = true;
			this.addcheatbt.Click += new System.EventHandler(this.AddCheatClick);
			// 
			// ButtonPanel
			// 
			this.ButtonPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.ButtonPanel.Controls.Add(this.BF);
			this.ButtonPanel.Controls.Add(this.BD);
			this.ButtonPanel.Controls.Add(this.BB);
			this.ButtonPanel.Controls.Add(this.B9);
			this.ButtonPanel.Controls.Add(this.B7);
			this.ButtonPanel.Controls.Add(this.B5);
			this.ButtonPanel.Controls.Add(this.B3);
			this.ButtonPanel.Controls.Add(this.BE);
			this.ButtonPanel.Controls.Add(this.B6);
			this.ButtonPanel.Controls.Add(this.B1);
			this.ButtonPanel.Controls.Add(this.BC);
			this.ButtonPanel.Controls.Add(this.BA);
			this.ButtonPanel.Controls.Add(this.B8);
			this.ButtonPanel.Controls.Add(this.B4);
			this.ButtonPanel.Controls.Add(this.B2);
			this.ButtonPanel.Controls.Add(this.B0);
			this.ButtonPanel.Location = new System.Drawing.Point(47, 37);
			this.ButtonPanel.Margin = new System.Windows.Forms.Padding(4);
			this.ButtonPanel.Name = "ButtonPanel";
			this.ButtonPanel.Size = new System.Drawing.Size(319, 82);
			this.ButtonPanel.TabIndex = 4;
			// 
			// BF
			// 
			this.BF.Location = new System.Drawing.Point(275, 43);
			this.BF.Margin = new System.Windows.Forms.Padding(4);
			this.BF.Name = "BF";
			this.BF.Size = new System.Drawing.Size(35, 28);
			this.BF.TabIndex = 16;
			this.BF.Text = "F";
			this.BF.UseVisualStyleBackColor = true;
			this.BF.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BD
			// 
			this.BD.Location = new System.Drawing.Point(199, 43);
			this.BD.Margin = new System.Windows.Forms.Padding(4);
			this.BD.Name = "BD";
			this.BD.Size = new System.Drawing.Size(35, 28);
			this.BD.TabIndex = 14;
			this.BD.Text = "D";
			this.BD.UseVisualStyleBackColor = true;
			this.BD.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BB
			// 
			this.BB.Location = new System.Drawing.Point(121, 43);
			this.BB.Margin = new System.Windows.Forms.Padding(4);
			this.BB.Name = "BB";
			this.BB.Size = new System.Drawing.Size(35, 28);
			this.BB.TabIndex = 12;
			this.BB.Text = "B";
			this.BB.UseVisualStyleBackColor = true;
			this.BB.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B9
			// 
			this.B9.Location = new System.Drawing.Point(45, 43);
			this.B9.Margin = new System.Windows.Forms.Padding(4);
			this.B9.Name = "B9";
			this.B9.Size = new System.Drawing.Size(35, 28);
			this.B9.TabIndex = 10;
			this.B9.Text = "9";
			this.B9.UseVisualStyleBackColor = true;
			this.B9.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B7
			// 
			this.B7.Location = new System.Drawing.Point(275, 6);
			this.B7.Margin = new System.Windows.Forms.Padding(4);
			this.B7.Name = "B7";
			this.B7.Size = new System.Drawing.Size(35, 28);
			this.B7.TabIndex = 8;
			this.B7.Text = "7";
			this.B7.UseVisualStyleBackColor = true;
			this.B7.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B5
			// 
			this.B5.Location = new System.Drawing.Point(199, 7);
			this.B5.Margin = new System.Windows.Forms.Padding(4);
			this.B5.Name = "B5";
			this.B5.Size = new System.Drawing.Size(35, 28);
			this.B5.TabIndex = 6;
			this.B5.Text = "5";
			this.B5.UseVisualStyleBackColor = true;
			this.B5.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B3
			// 
			this.B3.Location = new System.Drawing.Point(121, 7);
			this.B3.Margin = new System.Windows.Forms.Padding(4);
			this.B3.Name = "B3";
			this.B3.Size = new System.Drawing.Size(35, 28);
			this.B3.TabIndex = 4;
			this.B3.Text = "3";
			this.B3.UseVisualStyleBackColor = true;
			this.B3.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BE
			// 
			this.BE.Location = new System.Drawing.Point(237, 43);
			this.BE.Margin = new System.Windows.Forms.Padding(4);
			this.BE.Name = "BE";
			this.BE.Size = new System.Drawing.Size(35, 28);
			this.BE.TabIndex = 15;
			this.BE.Text = "E";
			this.BE.UseVisualStyleBackColor = true;
			this.BE.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B6
			// 
			this.B6.Location = new System.Drawing.Point(237, 6);
			this.B6.Margin = new System.Windows.Forms.Padding(4);
			this.B6.Name = "B6";
			this.B6.Size = new System.Drawing.Size(35, 28);
			this.B6.TabIndex = 7;
			this.B6.Text = "6";
			this.B6.UseVisualStyleBackColor = true;
			this.B6.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B1
			// 
			this.B1.Location = new System.Drawing.Point(45, 7);
			this.B1.Margin = new System.Windows.Forms.Padding(4);
			this.B1.Name = "B1";
			this.B1.Size = new System.Drawing.Size(35, 28);
			this.B1.TabIndex = 2;
			this.B1.Text = "1";
			this.B1.UseVisualStyleBackColor = true;
			this.B1.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BC
			// 
			this.BC.Location = new System.Drawing.Point(161, 43);
			this.BC.Margin = new System.Windows.Forms.Padding(4);
			this.BC.Name = "BC";
			this.BC.Size = new System.Drawing.Size(35, 28);
			this.BC.TabIndex = 13;
			this.BC.Text = "C";
			this.BC.UseVisualStyleBackColor = true;
			this.BC.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BA
			// 
			this.BA.Location = new System.Drawing.Point(84, 43);
			this.BA.Margin = new System.Windows.Forms.Padding(4);
			this.BA.Name = "BA";
			this.BA.Size = new System.Drawing.Size(35, 28);
			this.BA.TabIndex = 11;
			this.BA.Text = "A";
			this.BA.UseVisualStyleBackColor = true;
			this.BA.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B8
			// 
			this.B8.Location = new System.Drawing.Point(8, 43);
			this.B8.Margin = new System.Windows.Forms.Padding(4);
			this.B8.Name = "B8";
			this.B8.Size = new System.Drawing.Size(35, 28);
			this.B8.TabIndex = 9;
			this.B8.Text = "8";
			this.B8.UseVisualStyleBackColor = true;
			this.B8.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B4
			// 
			this.B4.Location = new System.Drawing.Point(161, 7);
			this.B4.Margin = new System.Windows.Forms.Padding(4);
			this.B4.Name = "B4";
			this.B4.Size = new System.Drawing.Size(35, 28);
			this.B4.TabIndex = 5;
			this.B4.Text = "4";
			this.B4.UseVisualStyleBackColor = true;
			this.B4.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B2
			// 
			this.B2.Location = new System.Drawing.Point(84, 7);
			this.B2.Margin = new System.Windows.Forms.Padding(4);
			this.B2.Name = "B2";
			this.B2.Size = new System.Drawing.Size(35, 28);
			this.B2.TabIndex = 3;
			this.B2.Text = "2";
			this.B2.UseVisualStyleBackColor = true;
			this.B2.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B0
			// 
			this.B0.Location = new System.Drawing.Point(8, 6);
			this.B0.Margin = new System.Windows.Forms.Padding(4);
			this.B0.Name = "B0";
			this.B0.Size = new System.Drawing.Size(35, 28);
			this.B0.TabIndex = 1;
			this.B0.Text = "0";
			this.B0.UseVisualStyleBackColor = true;
			this.B0.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.label5);
			this.groupBox1.Controls.Add(this.CompareBox);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.AddressBox);
			this.groupBox1.Controls.Add(this.ValueBox);
			this.groupBox1.Location = new System.Drawing.Point(207, 127);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
			this.groupBox1.Size = new System.Drawing.Size(183, 101);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Decoded Value";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(116, 80);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(22, 17);
			this.label6.TabIndex = 9;
			this.label6.Text = "0x";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(76, 24);
			this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(22, 17);
			this.label4.TabIndex = 7;
			this.label4.Text = "0x";
			// 
			// ClearButton
			// 
			this.ClearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ClearButton.Location = new System.Drawing.Point(41, 201);
			this.ClearButton.Name = "ClearButton";
			this.ClearButton.Size = new System.Drawing.Size(72, 32);
			this.ClearButton.TabIndex = 32;
			this.ClearButton.Text = "&Clear";
			this.ClearButton.UseVisualStyleBackColor = true;
			this.ClearButton.Click += new System.EventHandler(this.ClearButton_Click);
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
			this.menuStrip1.Size = new System.Drawing.Size(403, 28);
			this.menuStrip1.TabIndex = 8;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autoloadToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem,
            this.toolStripSeparator1,
            this.exitToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(73, 24);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(225, 24);
			this.autoloadToolStripMenuItem.Text = "Auto-load";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(225, 24);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(222, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(225, 24);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cheatname);
			this.groupBox2.Location = new System.Drawing.Point(41, 242);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(349, 61);
			this.groupBox2.TabIndex = 24;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Cheat Title (Uses GG code if left empty)";
			// 
			// cheatname
			// 
			this.cheatname.Location = new System.Drawing.Point(24, 28);
			this.cheatname.Name = "cheatname";
			this.cheatname.Size = new System.Drawing.Size(301, 22);
			this.cheatname.TabIndex = 0;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(116, 52);
			this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(22, 17);
			this.label2.TabIndex = 25;
			this.label2.Text = "0x";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(11, 52);
			this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(65, 17);
			this.label5.TabIndex = 24;
			this.label5.Text = "Compare";
			// 
			// CompareBox
			// 
			this.CompareBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.CompareBox.Location = new System.Drawing.Point(140, 47);
			this.CompareBox.Margin = new System.Windows.Forms.Padding(4);
			this.CompareBox.MaxLength = 2;
			this.CompareBox.Name = "CompareBox";
			this.CompareBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.CompareBox.Size = new System.Drawing.Size(35, 22);
			this.CompareBox.TabIndex = 26;
			this.CompareBox.TextChanged += new System.EventHandler(this.CompareBox_TextChanged);
			// 
			// GBGameGenie
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(403, 315);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.ClearButton);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.ButtonPanel);
			this.Controls.Add(this.addcheatbt);
			this.Controls.Add(this.GameGenieCodeBox);
			this.Controls.Add(this.menuStrip1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MainMenuStrip = this.menuStrip1;
			this.Margin = new System.Windows.Forms.Padding(4);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(413, 354);
			this.MinimumSize = new System.Drawing.Size(413, 354);
			this.Name = "GBGameGenie";
			this.ShowIcon = false;
			this.Text = "Game Genie Encoder / Decoder";
			this.GameGenieCodeBox.ResumeLayout(false);
			this.GameGenieCodeBox.PerformLayout();
			this.ButtonPanel.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.GroupBox GameGenieCodeBox;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label1;
        private HexTextBox ValueBox;
        private HexTextBox AddressBox;
        private System.Windows.Forms.Button addcheatbt;
        private System.Windows.Forms.Panel ButtonPanel;
        private System.Windows.Forms.Button B6;
        private System.Windows.Forms.Button B4;
        private System.Windows.Forms.Button B2;
        private System.Windows.Forms.Button B0;
        private System.Windows.Forms.Button BE;
        private System.Windows.Forms.Button BC;
        private System.Windows.Forms.Button BA;
        private System.Windows.Forms.Button B8;
        private System.Windows.Forms.Button BF;
        private System.Windows.Forms.Button BD;
        private System.Windows.Forms.Button BB;
        private System.Windows.Forms.Button B9;
        private System.Windows.Forms.Button B7;
        private System.Windows.Forms.Button B5;
        private System.Windows.Forms.Button B3;
        private System.Windows.Forms.Button B1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button ClearButton;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.MaskedTextBox GGCodeMaskBox;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TextBox cheatname;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label5;
		private HexTextBox CompareBox;
    }
}