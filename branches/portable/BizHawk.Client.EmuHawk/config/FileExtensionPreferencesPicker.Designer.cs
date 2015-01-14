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
			this.FileExtensionLabel = new System.Windows.Forms.Label();
			this.PlatformDropdown = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// FileExtensionLabel
			// 
			this.FileExtensionLabel.AutoSize = true;
			this.FileExtensionLabel.Location = new System.Drawing.Point(3, 5);
			this.FileExtensionLabel.Name = "FileExtensionLabel";
			this.FileExtensionLabel.Size = new System.Drawing.Size(24, 13);
			this.FileExtensionLabel.TabIndex = 0;
			this.FileExtensionLabel.Text = ".bin";
			// 
			// PlatformDropdown
			// 
			this.PlatformDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.PlatformDropdown.FormattingEnabled = true;
			this.PlatformDropdown.Location = new System.Drawing.Point(37, 2);
			this.PlatformDropdown.Name = "PlatformDropdown";
			this.PlatformDropdown.Size = new System.Drawing.Size(142, 21);
			this.PlatformDropdown.TabIndex = 1;
			// 
			// FileExtensionPreferencesPicker
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.PlatformDropdown);
			this.Controls.Add(this.FileExtensionLabel);
			this.Name = "FileExtensionPreferencesPicker";
			this.Size = new System.Drawing.Size(182, 29);
			this.Load += new System.EventHandler(this.FileExtensionPreferencesPicker_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label FileExtensionLabel;
		private System.Windows.Forms.ComboBox PlatformDropdown;
	}
}
