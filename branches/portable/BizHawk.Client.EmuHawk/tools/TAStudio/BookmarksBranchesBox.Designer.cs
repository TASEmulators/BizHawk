namespace BizHawk.Client.EmuHawk
{
	partial class BookmarksBranchesBox
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
			this.BookmarksBranchesGroupBox = new System.Windows.Forms.GroupBox();
			this.SuspendLayout();
			// 
			// BookmarksBranchesGroupBox
			// 
			this.BookmarksBranchesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BookmarksBranchesGroupBox.Location = new System.Drawing.Point(3, 0);
			this.BookmarksBranchesGroupBox.Name = "BookmarksBranchesGroupBox";
			this.BookmarksBranchesGroupBox.Size = new System.Drawing.Size(198, 249);
			this.BookmarksBranchesGroupBox.TabIndex = 0;
			this.BookmarksBranchesGroupBox.TabStop = false;
			this.BookmarksBranchesGroupBox.Text = "Bookmarks / Branches";
			this.BookmarksBranchesGroupBox.Enter += new System.EventHandler(this.BookmarksBranchesGroupBox_Enter);
			// 
			// BookmarksBranchesBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.BookmarksBranchesGroupBox);
			this.Name = "BookmarksBranchesBox";
			this.Size = new System.Drawing.Size(204, 253);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox BookmarksBranchesGroupBox;
	}
}
