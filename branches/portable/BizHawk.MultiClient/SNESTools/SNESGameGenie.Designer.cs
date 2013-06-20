namespace BizHawk.MultiClient
{
    partial class SNESGameGenie
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
			this.GameGenieCodeBox.Location = new System.Drawing.Point(31, 103);
			this.GameGenieCodeBox.Name = "GameGenieCodeBox";
			this.GameGenieCodeBox.Size = new System.Drawing.Size(118, 54);
			this.GameGenieCodeBox.TabIndex = 1;
			this.GameGenieCodeBox.TabStop = false;
			this.GameGenieCodeBox.Text = "Game Genie Code";
			// 
			// GGCodeMaskBox
			// 
			this.GGCodeMaskBox.Location = new System.Drawing.Point(25, 26);
			this.GGCodeMaskBox.Margin = new System.Windows.Forms.Padding(2);
			this.GGCodeMaskBox.Mask = ">AAAA-AAAA";
			this.GGCodeMaskBox.Name = "GGCodeMaskBox";
			this.GGCodeMaskBox.Size = new System.Drawing.Size(75, 20);
			this.GGCodeMaskBox.TabIndex = 10;
			this.GGCodeMaskBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.GGCodeMaskBox.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
			this.GGCodeMaskBox.TextChanged += new System.EventHandler(this.GGCodeMaskBox_TextChanged);
			this.GGCodeMaskBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.GGCodeMaskBox_KeyPress);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 54);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(34, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "Value";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 22);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(45, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Address";
			// 
			// ValueBox
			// 
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(105, 50);
			this.ValueBox.MaxLength = 2;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.ValueBox.Size = new System.Drawing.Size(27, 20);
			this.ValueBox.TabIndex = 23;
			this.ValueBox.TextChanged += new System.EventHandler(this.ValueBox_TextChanged);
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(75, 19);
			this.AddressBox.MaxLength = 6;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.AddressBox.Size = new System.Drawing.Size(57, 20);
			this.AddressBox.TabIndex = 21;
			this.AddressBox.TextChanged += new System.EventHandler(this.AddressBox_TextChanged);
			// 
			// addcheatbt
			// 
			this.addcheatbt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.addcheatbt.Enabled = false;
			this.addcheatbt.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.addcheatbt.Location = new System.Drawing.Point(85, 163);
			this.addcheatbt.Margin = new System.Windows.Forms.Padding(0);
			this.addcheatbt.Name = "addcheatbt";
			this.addcheatbt.Size = new System.Drawing.Size(65, 26);
			this.addcheatbt.TabIndex = 33;
			this.addcheatbt.Text = "&Add Cheat";
			this.addcheatbt.UseVisualStyleBackColor = true;
			this.addcheatbt.Click += new System.EventHandler(this.AddCheat_Click);
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
			this.ButtonPanel.Location = new System.Drawing.Point(35, 30);
			this.ButtonPanel.Name = "ButtonPanel";
			this.ButtonPanel.Size = new System.Drawing.Size(240, 67);
			this.ButtonPanel.TabIndex = 4;
			// 
			// BF
			// 
			this.BF.Location = new System.Drawing.Point(206, 35);
			this.BF.Name = "BF";
			this.BF.Size = new System.Drawing.Size(26, 23);
			this.BF.TabIndex = 16;
			this.BF.Text = "F";
			this.BF.UseVisualStyleBackColor = true;
			this.BF.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BD
			// 
			this.BD.Location = new System.Drawing.Point(149, 35);
			this.BD.Name = "BD";
			this.BD.Size = new System.Drawing.Size(26, 23);
			this.BD.TabIndex = 14;
			this.BD.Text = "D";
			this.BD.UseVisualStyleBackColor = true;
			this.BD.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BB
			// 
			this.BB.Location = new System.Drawing.Point(91, 35);
			this.BB.Name = "BB";
			this.BB.Size = new System.Drawing.Size(26, 23);
			this.BB.TabIndex = 12;
			this.BB.Text = "B";
			this.BB.UseVisualStyleBackColor = true;
			this.BB.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B9
			// 
			this.B9.Location = new System.Drawing.Point(34, 35);
			this.B9.Name = "B9";
			this.B9.Size = new System.Drawing.Size(26, 23);
			this.B9.TabIndex = 10;
			this.B9.Text = "9";
			this.B9.UseVisualStyleBackColor = true;
			this.B9.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B7
			// 
			this.B7.Location = new System.Drawing.Point(206, 5);
			this.B7.Name = "B7";
			this.B7.Size = new System.Drawing.Size(26, 23);
			this.B7.TabIndex = 8;
			this.B7.Text = "7";
			this.B7.UseVisualStyleBackColor = true;
			this.B7.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B5
			// 
			this.B5.Location = new System.Drawing.Point(149, 6);
			this.B5.Name = "B5";
			this.B5.Size = new System.Drawing.Size(26, 23);
			this.B5.TabIndex = 6;
			this.B5.Text = "5";
			this.B5.UseVisualStyleBackColor = true;
			this.B5.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B3
			// 
			this.B3.Location = new System.Drawing.Point(91, 6);
			this.B3.Name = "B3";
			this.B3.Size = new System.Drawing.Size(26, 23);
			this.B3.TabIndex = 4;
			this.B3.Text = "3";
			this.B3.UseVisualStyleBackColor = true;
			this.B3.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BE
			// 
			this.BE.Location = new System.Drawing.Point(178, 35);
			this.BE.Name = "BE";
			this.BE.Size = new System.Drawing.Size(26, 23);
			this.BE.TabIndex = 15;
			this.BE.Text = "E";
			this.BE.UseVisualStyleBackColor = true;
			this.BE.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B6
			// 
			this.B6.Location = new System.Drawing.Point(178, 5);
			this.B6.Name = "B6";
			this.B6.Size = new System.Drawing.Size(26, 23);
			this.B6.TabIndex = 7;
			this.B6.Text = "6";
			this.B6.UseVisualStyleBackColor = true;
			this.B6.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B1
			// 
			this.B1.Location = new System.Drawing.Point(34, 6);
			this.B1.Name = "B1";
			this.B1.Size = new System.Drawing.Size(26, 23);
			this.B1.TabIndex = 2;
			this.B1.Text = "1";
			this.B1.UseVisualStyleBackColor = true;
			this.B1.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BC
			// 
			this.BC.Location = new System.Drawing.Point(121, 35);
			this.BC.Name = "BC";
			this.BC.Size = new System.Drawing.Size(26, 23);
			this.BC.TabIndex = 13;
			this.BC.Text = "C";
			this.BC.UseVisualStyleBackColor = true;
			this.BC.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// BA
			// 
			this.BA.Location = new System.Drawing.Point(63, 35);
			this.BA.Name = "BA";
			this.BA.Size = new System.Drawing.Size(26, 23);
			this.BA.TabIndex = 11;
			this.BA.Text = "A";
			this.BA.UseVisualStyleBackColor = true;
			this.BA.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B8
			// 
			this.B8.Location = new System.Drawing.Point(6, 35);
			this.B8.Name = "B8";
			this.B8.Size = new System.Drawing.Size(26, 23);
			this.B8.TabIndex = 9;
			this.B8.Text = "8";
			this.B8.UseVisualStyleBackColor = true;
			this.B8.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B4
			// 
			this.B4.Location = new System.Drawing.Point(121, 6);
			this.B4.Name = "B4";
			this.B4.Size = new System.Drawing.Size(26, 23);
			this.B4.TabIndex = 5;
			this.B4.Text = "4";
			this.B4.UseVisualStyleBackColor = true;
			this.B4.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B2
			// 
			this.B2.Location = new System.Drawing.Point(63, 6);
			this.B2.Name = "B2";
			this.B2.Size = new System.Drawing.Size(26, 23);
			this.B2.TabIndex = 3;
			this.B2.Text = "2";
			this.B2.UseVisualStyleBackColor = true;
			this.B2.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// B0
			// 
			this.B0.Location = new System.Drawing.Point(6, 5);
			this.B0.Name = "B0";
			this.B0.Size = new System.Drawing.Size(26, 23);
			this.B0.TabIndex = 1;
			this.B0.Text = "0";
			this.B0.UseVisualStyleBackColor = true;
			this.B0.Click += new System.EventHandler(this.Keypad_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.AddressBox);
			this.groupBox1.Controls.Add(this.ValueBox);
			this.groupBox1.Location = new System.Drawing.Point(155, 103);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(137, 82);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Decoded Value";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(87, 54);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(18, 13);
			this.label6.TabIndex = 9;
			this.label6.Text = "0x";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(57, 22);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(18, 13);
			this.label4.TabIndex = 7;
			this.label4.Text = "0x";
			// 
			// ClearButton
			// 
			this.ClearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ClearButton.Location = new System.Drawing.Point(31, 163);
			this.ClearButton.Margin = new System.Windows.Forms.Padding(2);
			this.ClearButton.Name = "ClearButton";
			this.ClearButton.Size = new System.Drawing.Size(52, 26);
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
			this.menuStrip1.Size = new System.Drawing.Size(302, 24);
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
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
			// 
			// autoloadToolStripMenuItem
			// 
			this.autoloadToolStripMenuItem.Name = "autoloadToolStripMenuItem";
			this.autoloadToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.autoloadToolStripMenuItem.Text = "Auto-load";
			this.autoloadToolStripMenuItem.Click += new System.EventHandler(this.autoloadToolStripMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "Save Window Position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(188, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cheatname);
			this.groupBox2.Location = new System.Drawing.Point(31, 197);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(2);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(2);
			this.groupBox2.Size = new System.Drawing.Size(262, 50);
			this.groupBox2.TabIndex = 24;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Cheat Title (Uses GG code if left empty)";
			// 
			// cheatname
			// 
			this.cheatname.Location = new System.Drawing.Point(18, 23);
			this.cheatname.Margin = new System.Windows.Forms.Padding(2);
			this.cheatname.Name = "cheatname";
			this.cheatname.Size = new System.Drawing.Size(227, 20);
			this.cheatname.TabIndex = 0;
			// 
			// SNESGameGenie
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(302, 261);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.ClearButton);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.ButtonPanel);
			this.Controls.Add(this.addcheatbt);
			this.Controls.Add(this.GameGenieCodeBox);
			this.Controls.Add(this.menuStrip1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MainMenuStrip = this.menuStrip1;
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(312, 294);
			this.MinimumSize = new System.Drawing.Size(312, 294);
			this.Name = "SNESGameGenie";
			this.ShowIcon = false;
			this.Text = "SNES Game Genie Encoder / Decoder";
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
    }
}