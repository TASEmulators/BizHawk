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
			this.BranchView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.BranchNumberColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.FrameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.TimeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.BookmarksBranchesGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// BookmarksBranchesGroupBox
			// 
			this.BookmarksBranchesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BookmarksBranchesGroupBox.Controls.Add(this.BranchView);
			this.BookmarksBranchesGroupBox.Location = new System.Drawing.Point(3, 0);
			this.BookmarksBranchesGroupBox.Name = "BookmarksBranchesGroupBox";
			this.BookmarksBranchesGroupBox.Size = new System.Drawing.Size(198, 249);
			this.BookmarksBranchesGroupBox.TabIndex = 0;
			this.BookmarksBranchesGroupBox.TabStop = false;
			this.BookmarksBranchesGroupBox.Text = "Bookmarks / Branches";
			// 
			// BranchView
			// 
			this.BranchView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BranchView.BlazingFast = false;
			this.BranchView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.BranchNumberColumn,
            this.FrameColumn,
            this.TimeColumn});
			this.BranchView.GridLines = true;
			this.BranchView.ItemCount = 0;
			this.BranchView.Location = new System.Drawing.Point(6, 19);
			this.BranchView.Name = "BranchView";
			this.BranchView.SelectAllInProgress = false;
			this.BranchView.selectedItem = -1;
			this.BranchView.Size = new System.Drawing.Size(186, 224);
			this.BranchView.TabIndex = 0;
			this.BranchView.UseCompatibleStateImageBehavior = false;
			this.BranchView.UseCustomBackground = true;
			this.BranchView.View = System.Windows.Forms.View.Details;
			// 
			// BranchNumberColumn
			// 
			this.BranchNumberColumn.Text = "#";
			this.BranchNumberColumn.Width = 30;
			// 
			// FrameColumn
			// 
			this.FrameColumn.Text = "Frame";
			this.FrameColumn.Width = 68;
			// 
			// TimeColumn
			// 
			this.TimeColumn.Text = "Length";
			this.TimeColumn.Width = 83;
			// 
			// BookmarksBranchesBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.BookmarksBranchesGroupBox);
			this.Name = "BookmarksBranchesBox";
			this.Size = new System.Drawing.Size(204, 253);
			this.BookmarksBranchesGroupBox.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox BookmarksBranchesGroupBox;
		private VirtualListView BranchView;
		private System.Windows.Forms.ColumnHeader BranchNumberColumn;
		private System.Windows.Forms.ColumnHeader FrameColumn;
		private System.Windows.Forms.ColumnHeader TimeColumn;
	}
}
