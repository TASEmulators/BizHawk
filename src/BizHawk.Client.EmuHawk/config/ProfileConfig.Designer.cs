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
            this.ProfileDialogHelpTexBox = new System.Windows.Forms.RichTextBox();
            this.ProfileOptionsLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.OtherOptions = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AutoCheckForUpdates = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // OkBtn
            // 
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // ProfileSelectComboBox
            // 
            resources.ApplyResources(this.ProfileSelectComboBox, "ProfileSelectComboBox");
            this.ProfileSelectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ProfileSelectComboBox.FormattingEnabled = true;
            this.ProfileSelectComboBox.Items.AddRange(new object[] {
            resources.GetString("ProfileSelectComboBox.Items"),
            resources.GetString("ProfileSelectComboBox.Items1"),
            resources.GetString("ProfileSelectComboBox.Items2"),
            resources.GetString("ProfileSelectComboBox.Items3")});
            this.ProfileSelectComboBox.Name = "ProfileSelectComboBox";
            // 
            // ProfileDialogHelpTexBox
            // 
            resources.ApplyResources(this.ProfileDialogHelpTexBox, "ProfileDialogHelpTexBox");
            this.ProfileDialogHelpTexBox.Name = "ProfileDialogHelpTexBox";
            this.ProfileDialogHelpTexBox.ReadOnly = true;
            // 
            // ProfileOptionsLabel
            // 
            resources.ApplyResources(this.ProfileOptionsLabel, "ProfileOptionsLabel");
            this.ProfileOptionsLabel.Name = "ProfileOptionsLabel";
            // 
            // OtherOptions
            // 
            resources.ApplyResources(this.OtherOptions, "OtherOptions");
            this.OtherOptions.Name = "OtherOptions";
            // 
            // AutoCheckForUpdates
            // 
            resources.ApplyResources(this.AutoCheckForUpdates, "AutoCheckForUpdates");
            this.AutoCheckForUpdates.Name = "AutoCheckForUpdates";
            this.AutoCheckForUpdates.UseVisualStyleBackColor = true;
            // 
            // ProfileConfig
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.AutoCheckForUpdates);
            this.Controls.Add(this.OtherOptions);
            this.Controls.Add(this.ProfileOptionsLabel);
            this.Controls.Add(this.ProfileDialogHelpTexBox);
            this.Controls.Add(this.ProfileSelectComboBox);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Name = "ProfileConfig";
            this.Load += new System.EventHandler(this.ProfileConfig_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.ComboBox ProfileSelectComboBox;
		private System.Windows.Forms.RichTextBox ProfileDialogHelpTexBox;
		private BizHawk.WinForms.Controls.LocLabelEx ProfileOptionsLabel;
		private BizHawk.WinForms.Controls.LocLabelEx OtherOptions;
		private System.Windows.Forms.CheckBox AutoCheckForUpdates;
	}
}