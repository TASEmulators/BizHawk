namespace BizHawk.Client.EmuHawk
{
	partial class MessageConfig
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
            this.OK = new System.Windows.Forms.Button();
            this.MessageTypeBox = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label12 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MovieInputText = new System.Windows.Forms.TextBox();
            this.label11 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MovieInputColor = new System.Windows.Forms.Panel();
            this.LInputColorPanel = new System.Windows.Forms.Panel();
            this.AlertColorPanel = new System.Windows.Forms.Panel();
            this.ColorPanel = new System.Windows.Forms.Panel();
            this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.LInputText = new System.Windows.Forms.TextBox();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AlertColorText = new System.Windows.Forms.TextBox();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ColorText = new System.Windows.Forms.TextBox();
            this.MessageColorDialog = new System.Windows.Forms.ColorDialog();
            this.Cancel = new System.Windows.Forms.Button();
            this.ResetDefaultsButton = new System.Windows.Forms.Button();
            this.AlertColorDialog = new System.Windows.Forms.ColorDialog();
            this.LInputColorDialog = new System.Windows.Forms.ColorDialog();
            this.MovieInputColorDialog = new System.Windows.Forms.ColorDialog();
            this.StackMessagesCheckbox = new System.Windows.Forms.CheckBox();
            this.MessageEditor = new BizHawk.Client.EmuHawk.MessageEdit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(348, 418);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 1;
            this.OK.Text = "&OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // MessageTypeBox
            // 
            this.MessageTypeBox.Location = new System.Drawing.Point(12, 12);
            this.MessageTypeBox.Name = "MessageTypeBox";
            this.MessageTypeBox.Size = new System.Drawing.Size(177, 211);
            this.MessageTypeBox.TabIndex = 2;
            this.MessageTypeBox.TabStop = false;
            this.MessageTypeBox.Text = "Message Type";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Controls.Add(this.label12);
            this.groupBox2.Controls.Add(this.MovieInputText);
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.MovieInputColor);
            this.groupBox2.Controls.Add(this.LInputColorPanel);
            this.groupBox2.Controls.Add(this.AlertColorPanel);
            this.groupBox2.Controls.Add(this.ColorPanel);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Controls.Add(this.LInputText);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.AlertColorText);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.ColorText);
            this.groupBox2.Location = new System.Drawing.Point(12, 231);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(177, 210);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Message Colors";
            // 
            // label12
            // 
            this.label12.Location = new System.Drawing.Point(1, 161);
            this.label12.Name = "label12";
            this.label12.Text = "Movie Input";
            // 
            // MovieInputText
            // 
            this.MovieInputText.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.MovieInputText.Location = new System.Drawing.Point(45, 178);
            this.MovieInputText.MaxLength = 8;
            this.MovieInputText.Name = "MovieInputText";
            this.MovieInputText.ReadOnly = true;
            this.MovieInputText.Size = new System.Drawing.Size(59, 20);
            this.MovieInputText.TabIndex = 23;
            // 
            // label11
            // 
            this.label11.Location = new System.Drawing.Point(28, 181);
            this.label11.Name = "label11";
            this.label11.Text = "0x";
            // 
            // MovieInputColor
            // 
            this.MovieInputColor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.MovieInputColor.Location = new System.Drawing.Point(4, 178);
            this.MovieInputColor.Name = "MovieInputColor";
            this.MovieInputColor.Size = new System.Drawing.Size(20, 20);
            this.MovieInputColor.TabIndex = 9;
            this.MovieInputColor.Click += new System.EventHandler(this.MovieInputColor_Click);
            // 
            // LInputColorPanel
            // 
            this.LInputColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.LInputColorPanel.Location = new System.Drawing.Point(6, 130);
            this.LInputColorPanel.Name = "LInputColorPanel";
            this.LInputColorPanel.Size = new System.Drawing.Size(20, 20);
            this.LInputColorPanel.TabIndex = 7;
            this.LInputColorPanel.Click += new System.EventHandler(this.LInputColorPanel_Click);
            // 
            // AlertColorPanel
            // 
            this.AlertColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.AlertColorPanel.Location = new System.Drawing.Point(6, 81);
            this.AlertColorPanel.Name = "AlertColorPanel";
            this.AlertColorPanel.Size = new System.Drawing.Size(20, 20);
            this.AlertColorPanel.TabIndex = 7;
            this.AlertColorPanel.Click += new System.EventHandler(this.AlertColorPanel_Click);
            // 
            // ColorPanel
            // 
            this.ColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ColorPanel.Location = new System.Drawing.Point(6, 34);
            this.ColorPanel.Name = "ColorPanel";
            this.ColorPanel.Size = new System.Drawing.Size(20, 20);
            this.ColorPanel.TabIndex = 7;
            this.ColorPanel.Click += new System.EventHandler(this.ColorPanel_Click);
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(1, 111);
            this.label7.Name = "label7";
            this.label7.Text = "Previous Frame Input";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(28, 133);
            this.label8.Name = "label8";
            this.label8.Text = "0x";
            // 
            // LInputText
            // 
            this.LInputText.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.LInputText.Location = new System.Drawing.Point(46, 130);
            this.LInputText.MaxLength = 8;
            this.LInputText.Name = "LInputText";
            this.LInputText.ReadOnly = true;
            this.LInputText.Size = new System.Drawing.Size(59, 20);
            this.LInputText.TabIndex = 16;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(1, 62);
            this.label6.Name = "label6";
            this.label6.Text = "Alert messages";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(28, 84);
            this.label5.Name = "label5";
            this.label5.Text = "0x";
            // 
            // AlertColorText
            // 
            this.AlertColorText.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.AlertColorText.Location = new System.Drawing.Point(46, 81);
            this.AlertColorText.MaxLength = 8;
            this.AlertColorText.Name = "AlertColorText";
            this.AlertColorText.ReadOnly = true;
            this.AlertColorText.Size = new System.Drawing.Size(59, 20);
            this.AlertColorText.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(6, 18);
            this.label4.Name = "label4";
            this.label4.Text = "Main messages";
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(28, 37);
            this.label3.Name = "label3";
            this.label3.Text = "0x";
            // 
            // ColorText
            // 
            this.ColorText.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ColorText.Location = new System.Drawing.Point(46, 34);
            this.ColorText.MaxLength = 8;
            this.ColorText.Name = "ColorText";
            this.ColorText.ReadOnly = true;
            this.ColorText.Size = new System.Drawing.Size(59, 20);
            this.ColorText.TabIndex = 2;
            // 
            // MessageColorDialog
            // 
            this.MessageColorDialog.FullOpen = true;
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(429, 418);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 5;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ResetDefaultsButton
            // 
            this.ResetDefaultsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ResetDefaultsButton.Location = new System.Drawing.Point(195, 418);
            this.ResetDefaultsButton.Name = "ResetDefaultsButton";
            this.ResetDefaultsButton.Size = new System.Drawing.Size(96, 23);
            this.ResetDefaultsButton.TabIndex = 6;
            this.ResetDefaultsButton.Text = "Restore Defaults";
            this.ResetDefaultsButton.UseVisualStyleBackColor = true;
            this.ResetDefaultsButton.Click += new System.EventHandler(this.ResetDefaultsButton_Click);
            // 
            // AlertColorDialog
            // 
            this.AlertColorDialog.FullOpen = true;
            // 
            // LInputColorDialog
            // 
            this.LInputColorDialog.FullOpen = true;
            // 
            // MovieInputColorDialog
            // 
            this.MovieInputColorDialog.FullOpen = true;
            // 
            // StackMessagesCheckbox
            // 
            this.StackMessagesCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StackMessagesCheckbox.AutoSize = true;
            this.StackMessagesCheckbox.Location = new System.Drawing.Point(195, 388);
            this.StackMessagesCheckbox.Name = "StackMessagesCheckbox";
            this.StackMessagesCheckbox.Size = new System.Drawing.Size(105, 17);
            this.StackMessagesCheckbox.TabIndex = 7;
            this.StackMessagesCheckbox.Text = "Stack Messages";
            this.StackMessagesCheckbox.UseVisualStyleBackColor = true;
            // 
            // MessageEditor
            // 
            this.MessageEditor.Location = new System.Drawing.Point(195, 12);
            this.MessageEditor.Name = "MessageEditor";
            this.MessageEditor.Size = new System.Drawing.Size(310, 256);
            this.MessageEditor.TabIndex = 8;
            // 
            // MessageConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(512, 446);
            this.Controls.Add(this.MessageEditor);
            this.Controls.Add(this.StackMessagesCheckbox);
            this.Controls.Add(this.ResetDefaultsButton);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.MessageTypeBox);
            this.Controls.Add(this.OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(404, 375);
            this.Name = "MessageConfig";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configure On Screen Messages";
            this.Load += new System.EventHandler(this.MessageConfig_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.GroupBox MessageTypeBox;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.ColorDialog MessageColorDialog;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button ResetDefaultsButton;
		private System.Windows.Forms.TextBox ColorText;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private System.Windows.Forms.TextBox AlertColorText;
		private System.Windows.Forms.Panel AlertColorPanel;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private System.Windows.Forms.TextBox LInputText;
		private System.Windows.Forms.Panel LInputColorPanel;
		private System.Windows.Forms.ColorDialog AlertColorDialog;
		private System.Windows.Forms.ColorDialog LInputColorDialog;
		private System.Windows.Forms.Panel ColorPanel;
		private System.Windows.Forms.TextBox MovieInputText;
		private BizHawk.WinForms.Controls.LocLabelEx label11;
		private System.Windows.Forms.Panel MovieInputColor;
		private BizHawk.WinForms.Controls.LocLabelEx label12;
		private System.Windows.Forms.ColorDialog MovieInputColorDialog;
		private System.Windows.Forms.CheckBox StackMessagesCheckbox;
		private MessageEdit MessageEditor;
	}
}