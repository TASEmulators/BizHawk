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
			this.BranchView = new BizHawk.Client.EmuHawk.InputRoll();
			this.BranchesContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.LoadBranchContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.UpdateBranchContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AddBranchContextMenu = new System.Windows.Forms.ToolStripMenuItem();
			this.RemoveBranchContextMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.EditBranchTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AddBranchWithTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
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
			this.BookmarksBranchesGroupBox.Text = "Branches";
			// 
			// BranchView
			// 
			this.BranchView.AllowColumnReorder = false;
			this.BranchView.AllowColumnResize = false;
			this.BranchView.AlwaysScroll = false;
			this.BranchView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BranchView.CellHeightPadding = 0;
			this.BranchView.ContextMenuStrip = this.BranchesContextMenu;
			this.BranchView.denoteMarkersWithBGColor = false;
			this.BranchView.denoteMarkersWithIcons = false;
			this.BranchView.denoteStatesWithBGColor = false;
			this.BranchView.denoteStatesWithIcons = false;
			this.BranchView.FullRowSelect = true;
			this.BranchView.HideWasLagFrames = false;
			this.BranchView.HorizontalOrientation = false;
			this.BranchView.HoverInterval = 500;
			this.BranchView.LagFramesToHide = 0;
			this.BranchView.Location = new System.Drawing.Point(6, 19);
			this.BranchView.MaxCharactersInHorizontal = 1;
			this.BranchView.MultiSelect = false;
			this.BranchView.Name = "BranchView";
			this.BranchView.RowCount = 0;
			this.BranchView.ScrollSpeed = 13;
			this.BranchView.Size = new System.Drawing.Size(186, 224);
			this.BranchView.TabIndex = 0;
			this.BranchView.CellHovered += new BizHawk.Client.EmuHawk.InputRoll.HoverEventHandler(this.BranchView_CellHovered);
			this.BranchView.CellDropped += new BizHawk.Client.EmuHawk.InputRoll.CellDroppedEvent(this.BranchView_CellDropped);
			this.BranchView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseDoubleClick);
			this.BranchView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseDown);
			this.BranchView.MouseLeave += new System.EventHandler(this.BranchView_MouseLeave);
			this.BranchView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseMove);
			this.BranchView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.BranchView_MouseUp);
			// 
			// BranchesContextMenu
			// 
			this.BranchesContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.LoadBranchContextMenuItem,
            this.toolStripSeparator1,
            this.AddBranchContextMenu,
            this.AddBranchWithTextToolStripMenuItem,
            this.UpdateBranchContextMenuItem,
            this.EditBranchTextToolStripMenuItem,
            this.RemoveBranchContextMenuItem});
			this.BranchesContextMenu.Name = "BranchesContextMenu";
			this.BranchesContextMenu.Size = new System.Drawing.Size(153, 164);
			this.BranchesContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.BranchesContextMenu_Opening);
			// 
			// LoadBranchContextMenuItem
			// 
			this.LoadBranchContextMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Debugger;
			this.LoadBranchContextMenuItem.Name = "LoadBranchContextMenuItem";
			this.LoadBranchContextMenuItem.Size = new System.Drawing.Size(152, 22);
			this.LoadBranchContextMenuItem.Text = "Load";
			this.LoadBranchContextMenuItem.Click += new System.EventHandler(this.LoadBranchContextMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
			// 
			// UpdateBranchContextMenuItem
			// 
			this.UpdateBranchContextMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.reboot;
			this.UpdateBranchContextMenuItem.Name = "UpdateBranchContextMenuItem";
			this.UpdateBranchContextMenuItem.Size = new System.Drawing.Size(152, 22);
			this.UpdateBranchContextMenuItem.Text = "&Update";
			this.UpdateBranchContextMenuItem.Click += new System.EventHandler(this.UpdateBranchContextMenuItem_Click);
			// 
			// AddBranchContextMenu
			// 
			this.AddBranchContextMenu.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.add;
			this.AddBranchContextMenu.Name = "AddBranchContextMenu";
			this.AddBranchContextMenu.Size = new System.Drawing.Size(152, 22);
			this.AddBranchContextMenu.Text = "Add";
			this.AddBranchContextMenu.Click += new System.EventHandler(this.AddContextMenu_Click);
			// 
			// RemoveBranchContextMenuItem
			// 
			this.RemoveBranchContextMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Delete;
			this.RemoveBranchContextMenuItem.Name = "RemoveBranchContextMenuItem";
			this.RemoveBranchContextMenuItem.Size = new System.Drawing.Size(152, 22);
			this.RemoveBranchContextMenuItem.Text = "Remove";
			this.RemoveBranchContextMenuItem.Click += new System.EventHandler(this.RemoveBranchContextMenuItem_Click);
			// 
			// EditBranchTextToolStripMenuItem
			// 
			this.EditBranchTextToolStripMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.pencil;
			this.EditBranchTextToolStripMenuItem.Name = "EditBranchTextToolStripMenuItem";
			this.EditBranchTextToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.EditBranchTextToolStripMenuItem.Text = "Edit Text";
			this.EditBranchTextToolStripMenuItem.Click += new System.EventHandler(this.EditBranchTextToolStripMenuItem_Click);
			// 
			// AddBranchWithTextToolStripMenuItem
			// 
			this.AddBranchWithTextToolStripMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.add;
			this.AddBranchWithTextToolStripMenuItem.Name = "AddBranchWithTextToolStripMenuItem";
			this.AddBranchWithTextToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.AddBranchWithTextToolStripMenuItem.Text = "Add with Text";
			this.AddBranchWithTextToolStripMenuItem.Click += new System.EventHandler(this.AddBranchWithTextToolStripMenuItem_Click);
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
		private System.Windows.Forms.ToolStripMenuItem AddBranchContextMenu;
		private System.Windows.Forms.ToolStripMenuItem RemoveBranchContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem LoadBranchContextMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem UpdateBranchContextMenuItem;
		private System.Windows.Forms.ToolStripMenuItem EditBranchTextToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem AddBranchWithTextToolStripMenuItem;
	}
}
