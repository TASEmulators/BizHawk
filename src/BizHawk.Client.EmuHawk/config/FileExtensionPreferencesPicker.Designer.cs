namespace BizHawk.Client.EmuHawk
{
	partial class FileExtensionPreferencesPicker
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileExtensionPreferencesPicker));
            this.FileExtensionLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.PlatformDropdown = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // FileExtensionLabel
            // 
            resources.ApplyResources(this.FileExtensionLabel, "FileExtensionLabel");
            this.FileExtensionLabel.Name = "FileExtensionLabel";
            // 
            // PlatformDropdown
            // 
            resources.ApplyResources(this.PlatformDropdown, "PlatformDropdown");
            this.PlatformDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PlatformDropdown.FormattingEnabled = true;
            this.PlatformDropdown.Name = "PlatformDropdown";
            // 
            // FileExtensionPreferencesPicker
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PlatformDropdown);
            this.Controls.Add(this.FileExtensionLabel);
            this.Name = "FileExtensionPreferencesPicker";
            this.Load += new System.EventHandler(this.FileExtensionPreferencesPicker_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.LocLabelEx FileExtensionLabel;
		private System.Windows.Forms.ComboBox PlatformDropdown;
	}
}
