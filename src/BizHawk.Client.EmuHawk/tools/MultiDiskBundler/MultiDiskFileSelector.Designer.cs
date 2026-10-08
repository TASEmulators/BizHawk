namespace BizHawk.Client.EmuHawk
{
	partial class MultiDiskFileSelector
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultiDiskFileSelector));
            this.BrowseButton = new System.Windows.Forms.Button();
            this.PathBox = new System.Windows.Forms.TextBox();
            this.UseCurrentRomButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // BrowseButton
            // 
            resources.ApplyResources(this.BrowseButton, "BrowseButton");
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // PathBox
            // 
            resources.ApplyResources(this.PathBox, "PathBox");
            this.PathBox.AllowDrop = true;
            this.PathBox.Name = "PathBox";
            this.PathBox.TextChanged += new System.EventHandler(this.PathBox_TextChanged);
            this.PathBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.PathBox_DragDrop);
            this.PathBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.PathBox_DragEnter);
            // 
            // UseCurrentRomButton
            // 
            resources.ApplyResources(this.UseCurrentRomButton, "UseCurrentRomButton");
            this.UseCurrentRomButton.Name = "UseCurrentRomButton";
            this.UseCurrentRomButton.UseVisualStyleBackColor = true;
            this.UseCurrentRomButton.Click += new System.EventHandler(this.UseCurrentRomButton_Click);
            // 
            // MultiDiskFileSelector
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.UseCurrentRomButton);
            this.Controls.Add(this.BrowseButton);
            this.Controls.Add(this.PathBox);
            this.Name = "MultiDiskFileSelector";
            this.Load += new System.EventHandler(this.DualGBFileSelector_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button BrowseButton;
		private System.Windows.Forms.TextBox PathBox;
		private System.Windows.Forms.Button UseCurrentRomButton;

	}
}
