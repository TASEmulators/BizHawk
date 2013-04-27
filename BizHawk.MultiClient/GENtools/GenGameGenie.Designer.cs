namespace BizHawk.MultiClient
{
    partial class GenGameGenie
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
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.GGCodeMaskBox = new System.Windows.Forms.MaskedTextBox();
			this.addcheatbt = new System.Windows.Forms.Button();
			this.GameGenieCodeBox = new System.Windows.Forms.GroupBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.ValueBox = new BizHawk.HexTextBox();
			this.AddressBox = new BizHawk.HexTextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.ClearBT = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.cheatname = new System.Windows.Forms.TextBox();
			this.menuStrip1.SuspendLayout();
			this.GameGenieCodeBox.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
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
			// GGCodeMaskBox
			// 
			this.GGCodeMaskBox.Location = new System.Drawing.Point(18, 30);
			this.GGCodeMaskBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.GGCodeMaskBox.Mask = ">AAAA-AAAA";
			this.GGCodeMaskBox.Name = "GGCodeMaskBox";
			this.GGCodeMaskBox.Size = new System.Drawing.Size(76, 20);
			this.GGCodeMaskBox.TabIndex = 9;
			this.GGCodeMaskBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			this.GGCodeMaskBox.TextMaskFormat = System.Windows.Forms.MaskFormat.ExcludePromptAndLiterals;
			this.GGCodeMaskBox.TextChanged += new System.EventHandler(this.GGCodeMaskBox_TextChanged);
			this.GGCodeMaskBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.GGCodeMaskBox_KeyPress);
			// 
			// addcheatbt
			// 
			this.addcheatbt.Enabled = false;
			this.addcheatbt.Location = new System.Drawing.Point(156, 117);
			this.addcheatbt.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.addcheatbt.Name = "addcheatbt";
			this.addcheatbt.Size = new System.Drawing.Size(72, 26);
			this.addcheatbt.TabIndex = 10;
			this.addcheatbt.Text = "&Add Cheat";
			this.addcheatbt.UseVisualStyleBackColor = true;
			this.addcheatbt.Click += new System.EventHandler(this.addcheatbt_Click);
			// 
			// GameGenieCodeBox
			// 
			this.GameGenieCodeBox.Controls.Add(this.GGCodeMaskBox);
			this.GameGenieCodeBox.Location = new System.Drawing.Point(20, 35);
			this.GameGenieCodeBox.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.GameGenieCodeBox.Name = "GameGenieCodeBox";
			this.GameGenieCodeBox.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.GameGenieCodeBox.Size = new System.Drawing.Size(116, 69);
			this.GameGenieCodeBox.TabIndex = 11;
			this.GameGenieCodeBox.TabStop = false;
			this.GameGenieCodeBox.Text = "Game Genie Code";
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.groupBox1.Controls.Add(this.ValueBox);
			this.groupBox1.Controls.Add(this.AddressBox);
			this.groupBox1.Controls.Add(this.label6);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(156, 35);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.groupBox1.Size = new System.Drawing.Size(136, 69);
			this.groupBox1.TabIndex = 12;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Decoded Value";
			// 
			// ValueBox
			// 
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(92, 43);
			this.ValueBox.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.ValueBox.MaxLength = 4;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.ValueBox.Size = new System.Drawing.Size(40, 20);
			this.ValueBox.TabIndex = 23;
			this.ValueBox.TextChanged += new System.EventHandler(this.ValueBox_TextChanged);
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(72, 20);
			this.AddressBox.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.AddressBox.MaxLength = 6;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
			this.AddressBox.Size = new System.Drawing.Size(60, 20);
			this.AddressBox.TabIndex = 22;
			this.AddressBox.TextChanged += new System.EventHandler(this.AddressBox_TextChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(68, 46);
			this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(18, 13);
			this.label6.TabIndex = 9;
			this.label6.Text = "0x";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(52, 24);
			this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(18, 13);
			this.label4.TabIndex = 7;
			this.label4.Text = "0x";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 46);
			this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(34, 13);
			this.label3.TabIndex = 5;
			this.label3.Text = "Value";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 24);
			this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(45, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Address";
			// 
			// ClearBT
			// 
			this.ClearBT.Location = new System.Drawing.Point(66, 117);
			this.ClearBT.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.ClearBT.Name = "ClearBT";
			this.ClearBT.Size = new System.Drawing.Size(68, 26);
			this.ClearBT.TabIndex = 13;
			this.ClearBT.Text = "&Clear";
			this.ClearBT.UseVisualStyleBackColor = true;
			this.ClearBT.Click += new System.EventHandler(this.ClearBT_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.cheatname);
			this.groupBox2.Location = new System.Drawing.Point(20, 154);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.groupBox2.Size = new System.Drawing.Size(266, 50);
			this.groupBox2.TabIndex = 14;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Cheat Title (Uses GG code if left empty)";
			// 
			// cheatname
			// 
			this.cheatname.Location = new System.Drawing.Point(18, 24);
			this.cheatname.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.cheatname.Name = "cheatname";
			this.cheatname.Size = new System.Drawing.Size(228, 20);
			this.cheatname.TabIndex = 0;
			// 
			// GenGameGenie
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(302, 217);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.ClearBT);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.GameGenieCodeBox);
			this.Controls.Add(this.addcheatbt);
			this.Controls.Add(this.menuStrip1);
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
			this.MainMenuStrip = this.menuStrip1;
			this.Margin = new System.Windows.Forms.Padding(4, 2, 4, 2);
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(312, 293);
			this.MinimumSize = new System.Drawing.Size(312, 250);
			this.Name = "GenGameGenie";
			this.ShowIcon = false;
			this.Text = "Genesis Game Genie Encoder / Decoder";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.GameGenieCodeBox.ResumeLayout(false);
			this.GameGenieCodeBox.PerformLayout();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autoloadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.MaskedTextBox GGCodeMaskBox;
		private System.Windows.Forms.Button addcheatbt;
		private System.Windows.Forms.GroupBox GameGenieCodeBox;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button ClearBT;
		private HexTextBox ValueBox;
		private HexTextBox AddressBox;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.TextBox cheatname;
    }
}