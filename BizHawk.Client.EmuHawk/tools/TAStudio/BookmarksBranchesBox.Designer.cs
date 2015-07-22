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
			this.components = new System.ComponentModel.Container();
			this.BookmarksBranchesGroupBox = new System.Windows.Forms.GroupBox();
			this.BranchesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.LoadBranchContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.AddContextMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveBranchContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.BranchView = new InputRoll();
			this.BookmarksBranchesGroupBox.SuspendLayout();
			this.BranchesContextMenu.SuspendLayout();
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
			// BranchesContextMenu
			// 
			this.BranchesContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadBranchContextMenuItem,
            this.toolStripSeparator1,
            this.AddContextMenu,
            this.RemoveBranchContextMenuItem});
			this.BranchesContextMenu.Name = "BranchesContextMenu";
			this.BranchesContextMenu.Size = new System.Drawing.Size(118, 76);
			this.BranchesContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.BranchesContextMenu_Opening);
			// 
			// LoadBranchContextMenuItem
			// 
			this.LoadBranchContextMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Debugger;
			this.LoadBranchContextMenuItem.Name = "LoadBranchContextMenuItem";
			this.LoadBranchContextMenuItem.Size = new System.Drawing.Size(117, 22);
			this.LoadBranchContextMenuItem.Text = "Load";
			this.LoadBranchContextMenuItem.Click += new System.EventHandler(this.LoadBranchContextMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(114, 6);
			// 
			// AddContextMenu
			// 
			this.AddContextMenu.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.add;
			this.AddContextMenu.Name = "AddContextMenu";
			this.AddContextMenu.Size = new System.Drawing.Size(117, 22);
			this.AddContextMenu.Text = "Add";
			this.AddContextMenu.Click += new System.EventHandler(this.AddContextMenu_Click);
			// 
			// RemoveBranchContextMenuItem
			// 
			this.RemoveBranchContextMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Delete;
			this.RemoveBranchContextMenuItem.Name = "RemoveBranchContextMenuItem";
			this.RemoveBranchContextMenuItem.Size = new System.Drawing.Size(117, 22);
			this.RemoveBranchContextMenuItem.Text = "Remove";
			this.RemoveBranchContextMenuItem.Click += new System.EventHandler(this.RemoveBranchContextMenuItem_Click);
			// 
			// BranchView
			// 
			this.BranchView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BranchView.ContextMenuStrip = this.BranchesContextMenu;
			this.BranchView.FullRowSelect = true;
			this.BranchView.GridLines = true;
			this.BranchView.Location = new System.Drawing.Point(6, 19);
			this.BranchView.MultiSelect = false;
			this.BranchView.Name = "BranchView";
			this.BranchView.Size = new System.Drawing.Size(186, 224);
			this.BranchView.TabIndex = 0;
			this.BranchView.UseCustomBackground = true;
			this.BranchView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseDoubleClick);
			// 
			// BookmarksBranchesBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.BookmarksBranchesGroupBox);
			this.Name = "BookmarksBranchesBox";
			this.Size = new System.Drawing.Size(204, 253);
			this.BookmarksBranchesGroupBox.ResumeLayout(false);
			this.BranchesContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox BookmarksBranchesGroupBox;
		private InputRoll BranchView;
		private System.Windows.Forms.ContextMenuStrip BranchesContextMenu;
		private System.Windows.Forms.ToolStripMenuItem AddContextMenu;
		private System.Windows.Forms.ToolStripMenuItem RemoveBranchContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem LoadBranchContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
	}
}
