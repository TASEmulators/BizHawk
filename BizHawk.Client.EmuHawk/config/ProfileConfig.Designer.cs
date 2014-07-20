namespace BizHawk.Client.EmuHawk
{
	partial class ProfileConfig
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProfileConfig));
			this.OkBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.ProfileSelectComboBox = new System.Windows.Forms.ComboBox();
			this.SaveScreenshotStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.SaveLargeScreenshotStatesCheckBox = new System.Windows.Forms.CheckBox();
			this.AllowUDLRCheckBox = new System.Windows.Forms.CheckBox();
			this.ProfileDialogHelpTexBox = new System.Windows.Forms.RichTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.GeneralOptionsLabel = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBox2 = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(247, 337);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 0;
			this.OkBtn.Text = "&Ok";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(313, 337);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 1;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// ProfileSelectComboBox
			// 
			this.ProfileSelectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ProfileSelectComboBox.FormattingEnabled = true;
			this.ProfileSelectComboBox.Items.AddRange(new object[] {
            "Casual Gaming",
            "Tool-assisted Speedruns",
            "Longplays",
            "Custom Profile"});
			this.ProfileSelectComboBox.Location = new System.Drawing.Point(22, 20);
			this.ProfileSelectComboBox.Name = "ProfileSelectComboBox";
			this.ProfileSelectComboBox.Size = new System.Drawing.Size(156, 21);
			this.ProfileSelectComboBox.TabIndex = 2;
			// 
			// SaveScreenshotStatesCheckBox
			// 
			this.SaveScreenshotStatesCheckBox.AutoSize = true;
			this.SaveScreenshotStatesCheckBox.Location = new System.Drawing.Point(12, 67);
			this.SaveScreenshotStatesCheckBox.Name = "SaveScreenshotStatesCheckBox";
			this.SaveScreenshotStatesCheckBox.Size = new System.Drawing.Size(166, 17);
			this.SaveScreenshotStatesCheckBox.TabIndex = 4;
			this.SaveScreenshotStatesCheckBox.Text = "Save Screenshot With States";
			this.SaveScreenshotStatesCheckBox.UseVisualStyleBackColor = true;
			this.SaveScreenshotStatesCheckBox.Visible = false;
			this.SaveScreenshotStatesCheckBox.MouseHover += new System.EventHandler(this.SaveScreenshotStatesCheckBox_MouseHover);
			// 
			// SaveLargeScreenshotStatesCheckBox
			// 
			this.SaveLargeScreenshotStatesCheckBox.AutoSize = true;
			this.SaveLargeScreenshotStatesCheckBox.Location = new System.Drawing.Point(12, 90);
			this.SaveLargeScreenshotStatesCheckBox.Name = "SaveLargeScreenshotStatesCheckBox";
			this.SaveLargeScreenshotStatesCheckBox.Size = new System.Drawing.Size(196, 17);
			this.SaveLargeScreenshotStatesCheckBox.TabIndex = 5;
			this.SaveLargeScreenshotStatesCheckBox.Text = "Save Large Screenshot With States";
			this.SaveLargeScreenshotStatesCheckBox.UseVisualStyleBackColor = true;
			this.SaveLargeScreenshotStatesCheckBox.Visible = false;
			this.SaveLargeScreenshotStatesCheckBox.MouseHover += new System.EventHandler(this.SaveLargeScreenshotStatesCheckBox_MouseHover);
			// 
			// AllowUDLRCheckBox
			// 
			this.AllowUDLRCheckBox.AutoSize = true;
			this.AllowUDLRCheckBox.Location = new System.Drawing.Point(12, 113);
			this.AllowUDLRCheckBox.Name = "AllowUDLRCheckBox";
			this.AllowUDLRCheckBox.Size = new System.Drawing.Size(111, 17);
			this.AllowUDLRCheckBox.TabIndex = 6;
			this.AllowUDLRCheckBox.Text = "Allow U+D or L+R";
			this.AllowUDLRCheckBox.UseVisualStyleBackColor = true;
			this.AllowUDLRCheckBox.Visible = false;
			this.AllowUDLRCheckBox.MouseHover += new System.EventHandler(this.AllowUDLRCheckBox_MouseHover);
			// 
			// ProfileDialogHelpTexBox
			// 
			this.ProfileDialogHelpTexBox.Location = new System.Drawing.Point(184, 12);
			this.ProfileDialogHelpTexBox.Name = "ProfileDialogHelpTexBox";
			this.ProfileDialogHelpTexBox.ReadOnly = true;
			this.ProfileDialogHelpTexBox.Size = new System.Drawing.Size(198, 126);
			this.ProfileDialogHelpTexBox.TabIndex = 8;
			this.ProfileDialogHelpTexBox.Text = "Options:\nCasual Gaming - All about performance!\n\nTool-Assisted Speedruns - Maximu" +
    "m Accuracy!\n\nLongplays - Stability is the key!";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(21, 6);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(75, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "Profile Options";
			// 
			// GeneralOptionsLabel
			// 
			this.GeneralOptionsLabel.AutoSize = true;
			this.GeneralOptionsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.GeneralOptionsLabel.Location = new System.Drawing.Point(9, 51);
			this.GeneralOptionsLabel.Name = "GeneralOptionsLabel";
			this.GeneralOptionsLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.GeneralOptionsLabel.Size = new System.Drawing.Size(83, 13);
			this.GeneralOptionsLabel.TabIndex = 10;
			this.GeneralOptionsLabel.Text = "General Options";
			this.GeneralOptionsLabel.Visible = false;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(13, 195);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(93, 13);
			this.label3.TabIndex = 11;
			this.label3.Text = "N64 Core Settings";
			this.label3.Visible = false;
			// 
			// comboBox2
			// 
			this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox2.FormattingEnabled = true;
			this.comboBox2.Items.AddRange(new object[] {
            "Pure Interpreter",
            "Interpreter",
            "Dynarec"});
			this.comboBox2.Location = new System.Drawing.Point(109, 211);
			this.comboBox2.Name = "comboBox2";
			this.comboBox2.Size = new System.Drawing.Size(99, 21);
			this.comboBox2.TabIndex = 12;
			this.comboBox2.Visible = false;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(19, 214);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(56, 13);
			this.label4.TabIndex = 13;
			this.label4.Text = "Core Type";
			this.label4.Visible = false;
			// 
			// ProfileConfig
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(385, 372);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.comboBox2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.GeneralOptionsLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.ProfileDialogHelpTexBox);
			this.Controls.Add(this.AllowUDLRCheckBox);
			this.Controls.Add(this.SaveLargeScreenshotStatesCheckBox);
			this.Controls.Add(this.SaveScreenshotStatesCheckBox);
			this.Controls.Add(this.ProfileSelectComboBox);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ProfileConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Profile Config";
			this.Load += new System.EventHandler(this.ProfileConfig_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.ComboBox ProfileSelectComboBox;
		private System.Windows.Forms.CheckBox SaveScreenshotStatesCheckBox;
		private System.Windows.Forms.CheckBox SaveLargeScreenshotStatesCheckBox;
		private System.Windows.Forms.CheckBox AllowUDLRCheckBox;
		private System.Windows.Forms.RichTextBox ProfileDialogHelpTexBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label GeneralOptionsLabel;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBox2;
		private System.Windows.Forms.Label label4;
	}
}