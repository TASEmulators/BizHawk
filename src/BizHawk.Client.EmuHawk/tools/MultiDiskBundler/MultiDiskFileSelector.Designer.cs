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
			this.BrowseButton = new System.Windows.Forms.Button();
			this.PathBox = new System.Windows.Forms.TextBox();
			this.UseCurrentRomButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// BrowseButton
			// 
			this.BrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BrowseButton.Location = new System.Drawing.Point(294, 0);
			this.BrowseButton.Name = "BrowseButton";
			this.BrowseButton.Size = new System.Drawing.Size(66, 23);
			this.BrowseButton.TabIndex = 2;
			this.BrowseButton.Text = "Browse...";
			this.BrowseButton.UseVisualStyleBackColor = true;
			this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
			// 
			// PathBox
			// 
			this.PathBox.AllowDrop = true;
			this.PathBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PathBox.Location = new System.Drawing.Point(0, 1);
			this.PathBox.Name = "PathBox";
			this.PathBox.Size = new System.Drawing.Size(288, 20);
			this.PathBox.TabIndex = 1;
			this.PathBox.TextChanged += new System.EventHandler(this.PathBox_TextChanged);
			this.PathBox.DragDrop += new System.Windows.Forms.DragEventHandler(this.PathBox_DragDrop);
			this.PathBox.DragEnter += new System.Windows.Forms.DragEventHandler(this.PathBox_DragEnter);
			// 
			// UseCurrentRomButton
			// 
			this.UseCurrentRomButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.UseCurrentRomButton.Location = new System.Drawing.Point(364, 0);
			this.UseCurrentRomButton.Name = "UseCurrentRomButton";
			this.UseCurrentRomButton.Size = new System.Drawing.Size(58, 23);
			this.UseCurrentRomButton.TabIndex = 3;
			this.UseCurrentRomButton.Text = "Current";
			this.UseCurrentRomButton.UseVisualStyleBackColor = true;
			this.UseCurrentRomButton.Click += new System.EventHandler(this.UseCurrentRomButton_Click);
			// 
			// MultiDiskFileSelector
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.UseCurrentRomButton);
			this.Controls.Add(this.BrowseButton);
			this.Controls.Add(this.PathBox);
			this.Name = "MultiDiskFileSelector";
			this.Size = new System.Drawing.Size(422, 23);
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
