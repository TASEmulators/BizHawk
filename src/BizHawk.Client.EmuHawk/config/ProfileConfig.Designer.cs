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
			this.ProfileDialogHelpTexBox = new System.Windows.Forms.RichTextBox();
			this.ProfileOptionsLabel = new System.Windows.Forms.Label();
			this.OtherOptions = new System.Windows.Forms.Label();
			this.AutoCheckForUpdates = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(290, 231);
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
			this.CancelBtn.Location = new System.Drawing.Point(356, 231);
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
            "Longplays"});
			this.ProfileSelectComboBox.Location = new System.Drawing.Point(12, 27);
			this.ProfileSelectComboBox.Name = "ProfileSelectComboBox";
			this.ProfileSelectComboBox.Size = new System.Drawing.Size(156, 21);
			this.ProfileSelectComboBox.TabIndex = 4;
			// 
			// ProfileDialogHelpTexBox
			// 
			this.ProfileDialogHelpTexBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ProfileDialogHelpTexBox.Location = new System.Drawing.Point(218, 12);
			this.ProfileDialogHelpTexBox.Name = "ProfileDialogHelpTexBox";
			this.ProfileDialogHelpTexBox.ReadOnly = true;
			this.ProfileDialogHelpTexBox.Size = new System.Drawing.Size(198, 174);
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
			// OtherOptions
			// 
			this.OtherOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OtherOptions.AutoSize = true;
			this.OtherOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.OtherOptions.Location = new System.Drawing.Point(9, 190);
			this.OtherOptions.Name = "OtherOptions";
			this.OtherOptions.Size = new System.Drawing.Size(72, 13);
			this.OtherOptions.TabIndex = 12;
			this.OtherOptions.Text = "Other Options";
			// 
			// AutoCheckForUpdates
			// 
			this.AutoCheckForUpdates.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AutoCheckForUpdates.AutoSize = true;
			this.AutoCheckForUpdates.Location = new System.Drawing.Point(12, 208);
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
			this.ClientSize = new System.Drawing.Size(428, 266);
			this.Controls.Add(this.AutoCheckForUpdates);
			this.Controls.Add(this.OtherOptions);
			this.Controls.Add(this.ProfileOptionsLabel);
			this.Controls.Add(this.ProfileDialogHelpTexBox);
			this.Controls.Add(this.ProfileSelectComboBox);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(315, 280);
			this.Name = "ProfileConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Profile Config";
			this.Load += new System.EventHandler(this.ProfileConfig_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}



		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.ComboBox ProfileSelectComboBox;
		private System.Windows.Forms.RichTextBox ProfileDialogHelpTexBox;
		private System.Windows.Forms.Label ProfileOptionsLabel;
		private System.Windows.Forms.Label OtherOptions;
		private System.Windows.Forms.CheckBox AutoCheckForUpdates;
	}
}