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
			this.ProfileOptionsLabel = new System.Windows.Forms.Label();
			this.CustomProfileOptionsLabel = new System.Windows.Forms.Label();
			this.N64CoreSettingsLabel = new System.Windows.Forms.Label();
			this.N64CoreTypeComboBox = new System.Windows.Forms.ComboBox();
			this.N64CoreTypeLabel = new System.Windows.Forms.Label();
			this.OtherOptions = new System.Windows.Forms.Label();
			this.AutoCheckForUpdates = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(290, 337);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 0;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(356, 337);
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
            "N64 Tool-assisted Speedruns",
            "Longplays",
            "Custom Profile"});
			this.ProfileSelectComboBox.Location = new System.Drawing.Point(12, 27);
			this.ProfileSelectComboBox.Name = "ProfileSelectComboBox";
			this.ProfileSelectComboBox.Size = new System.Drawing.Size(156, 21);
			this.ProfileSelectComboBox.TabIndex = 4;
			// 
			// SaveScreenshotStatesCheckBox
			// 
			this.SaveScreenshotStatesCheckBox.AutoSize = true;
			this.SaveScreenshotStatesCheckBox.Location = new System.Drawing.Point(12, 103);
			this.SaveScreenshotStatesCheckBox.Name = "SaveScreenshotStatesCheckBox";
			this.SaveScreenshotStatesCheckBox.Size = new System.Drawing.Size(166, 17);
			this.SaveScreenshotStatesCheckBox.TabIndex = 6;
			this.SaveScreenshotStatesCheckBox.Text = "Save Screenshot With States";
			this.SaveScreenshotStatesCheckBox.UseVisualStyleBackColor = true;
			this.SaveScreenshotStatesCheckBox.Visible = false;
			this.SaveScreenshotStatesCheckBox.MouseHover += new System.EventHandler(this.SaveScreenshotStatesCheckBox_MouseHover);
			// 
			// SaveLargeScreenshotStatesCheckBox
			// 
			this.SaveLargeScreenshotStatesCheckBox.AutoSize = true;
			this.SaveLargeScreenshotStatesCheckBox.Location = new System.Drawing.Point(12, 126);
			this.SaveLargeScreenshotStatesCheckBox.Name = "SaveLargeScreenshotStatesCheckBox";
			this.SaveLargeScreenshotStatesCheckBox.Size = new System.Drawing.Size(196, 17);
			this.SaveLargeScreenshotStatesCheckBox.TabIndex = 7;
			this.SaveLargeScreenshotStatesCheckBox.Text = "Save Large Screenshot With States";
			this.SaveLargeScreenshotStatesCheckBox.UseVisualStyleBackColor = true;
			this.SaveLargeScreenshotStatesCheckBox.Visible = false;
			this.SaveLargeScreenshotStatesCheckBox.MouseHover += new System.EventHandler(this.SaveLargeScreenshotStatesCheckBox_MouseHover);
			// 
			// AllowUDLRCheckBox
			// 
			this.AllowUDLRCheckBox.AutoSize = true;
			this.AllowUDLRCheckBox.Location = new System.Drawing.Point(12, 149);
			this.AllowUDLRCheckBox.Name = "AllowUDLRCheckBox";
			this.AllowUDLRCheckBox.Size = new System.Drawing.Size(111, 17);
			this.AllowUDLRCheckBox.TabIndex = 8;
			this.AllowUDLRCheckBox.Text = "Allow U+D or L+R";
			this.AllowUDLRCheckBox.UseVisualStyleBackColor = true;
			this.AllowUDLRCheckBox.Visible = false;
			this.AllowUDLRCheckBox.MouseHover += new System.EventHandler(this.AllowUDLRCheckBox_MouseHover);
			// 
			// ProfileDialogHelpTexBox
			// 
			this.ProfileDialogHelpTexBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ProfileDialogHelpTexBox.Location = new System.Drawing.Point(218, 12);
			this.ProfileDialogHelpTexBox.Name = "ProfileDialogHelpTexBox";
			this.ProfileDialogHelpTexBox.ReadOnly = true;
			this.ProfileDialogHelpTexBox.Size = new System.Drawing.Size(198, 154);
			this.ProfileDialogHelpTexBox.TabIndex = 2;
			this.ProfileDialogHelpTexBox.Text = resources.GetString("ProfileDialogHelpTexBox.Text");
			// 
			// ProfileOptionsLabel
			// 
			this.ProfileOptionsLabel.AutoSize = true;
			this.ProfileOptionsLabel.Location = new System.Drawing.Point(9, 9);
			this.ProfileOptionsLabel.Name = "ProfileOptionsLabel";
			this.ProfileOptionsLabel.Size = new System.Drawing.Size(75, 13);
			this.ProfileOptionsLabel.TabIndex = 3;
			this.ProfileOptionsLabel.Text = "Profile Options";
			// 
			// CustomProfileOptionsLabel
			// 
			this.CustomProfileOptionsLabel.AutoSize = true;
			this.CustomProfileOptionsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CustomProfileOptionsLabel.Location = new System.Drawing.Point(9, 85);
			this.CustomProfileOptionsLabel.Name = "CustomProfileOptionsLabel";
			this.CustomProfileOptionsLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.CustomProfileOptionsLabel.Size = new System.Drawing.Size(113, 13);
			this.CustomProfileOptionsLabel.TabIndex = 5;
			this.CustomProfileOptionsLabel.Text = "Custom Profile Options";
			this.CustomProfileOptionsLabel.Visible = false;
			// 
			// N64CoreSettingsLabel
			// 
			this.N64CoreSettingsLabel.AutoSize = true;
			this.N64CoreSettingsLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.N64CoreSettingsLabel.Location = new System.Drawing.Point(9, 188);
			this.N64CoreSettingsLabel.Name = "N64CoreSettingsLabel";
			this.N64CoreSettingsLabel.Size = new System.Drawing.Size(93, 13);
			this.N64CoreSettingsLabel.TabIndex = 9;
			this.N64CoreSettingsLabel.Text = "N64 Core Settings";
			this.N64CoreSettingsLabel.Visible = false;
			// 
			// N64CoreTypeComboBox
			// 
			this.N64CoreTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.N64CoreTypeComboBox.FormattingEnabled = true;
			this.N64CoreTypeComboBox.Items.AddRange(new object[] {
            "Pure Interpreter",
            "Interpreter",
            "Dynarec"});
			this.N64CoreTypeComboBox.Location = new System.Drawing.Point(79, 206);
			this.N64CoreTypeComboBox.Name = "N64CoreTypeComboBox";
			this.N64CoreTypeComboBox.Size = new System.Drawing.Size(99, 21);
			this.N64CoreTypeComboBox.TabIndex = 11;
			this.N64CoreTypeComboBox.Visible = false;
			// 
			// N64CoreTypeLabel
			// 
			this.N64CoreTypeLabel.AutoSize = true;
			this.N64CoreTypeLabel.Location = new System.Drawing.Point(9, 209);
			this.N64CoreTypeLabel.Name = "N64CoreTypeLabel";
			this.N64CoreTypeLabel.Size = new System.Drawing.Size(56, 13);
			this.N64CoreTypeLabel.TabIndex = 10;
			this.N64CoreTypeLabel.Text = "Core Type";
			this.N64CoreTypeLabel.Visible = false;
			// 
			// OtherOptions
			// 
			this.OtherOptions.AutoSize = true;
			this.OtherOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.OtherOptions.Location = new System.Drawing.Point(9, 250);
			this.OtherOptions.Name = "OtherOptions";
			this.OtherOptions.Size = new System.Drawing.Size(72, 13);
			this.OtherOptions.TabIndex = 12;
			this.OtherOptions.Text = "Other Options";
			// 
			// AutoCheckForUpdates
			// 
			this.AutoCheckForUpdates.AutoSize = true;
			this.AutoCheckForUpdates.Location = new System.Drawing.Point(12, 268);
			this.AutoCheckForUpdates.Name = "AutoCheckForUpdates";
			this.AutoCheckForUpdates.Size = new System.Drawing.Size(345, 17);
			this.AutoCheckForUpdates.TabIndex = 13;
			this.AutoCheckForUpdates.Text = "Automatically check for and notify me of newer versions of BizHawk";
			this.AutoCheckForUpdates.UseVisualStyleBackColor = true;
			// 
			// ProfileConfig
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(428, 372);
			this.Controls.Add(this.AutoCheckForUpdates);
			this.Controls.Add(this.OtherOptions);
			this.Controls.Add(this.N64CoreTypeLabel);
			this.Controls.Add(this.N64CoreTypeComboBox);
			this.Controls.Add(this.N64CoreSettingsLabel);
			this.Controls.Add(this.CustomProfileOptionsLabel);
			this.Controls.Add(this.ProfileOptionsLabel);
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
		private System.Windows.Forms.Label ProfileOptionsLabel;
		private System.Windows.Forms.Label CustomProfileOptionsLabel;
		private System.Windows.Forms.Label N64CoreSettingsLabel;
		private System.Windows.Forms.ComboBox N64CoreTypeComboBox;
		private System.Windows.Forms.Label N64CoreTypeLabel;
		private System.Windows.Forms.Label OtherOptions;
		private System.Windows.Forms.CheckBox AutoCheckForUpdates;
	}
}