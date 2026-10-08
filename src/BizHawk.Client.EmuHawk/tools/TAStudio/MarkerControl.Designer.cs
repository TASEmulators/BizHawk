namespace BizHawk.Client.EmuHawk
{
	partial class MarkerControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MarkerControl));
            this.MarkerContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.JumpToMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.ScrollToMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.EditMarkerFrameToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.AddMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.RemoveMarkerToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.JumpToMarkerButton = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.EditMarkerButton = new System.Windows.Forms.Button();
            this.EditMarkerFrameButton = new System.Windows.Forms.Button();
            this.AddMarkerButton = new System.Windows.Forms.Button();
            this.ScrollToMarkerButton = new System.Windows.Forms.Button();
            this.AddMarkerWithTextButton = new System.Windows.Forms.Button();
            this.RemoveMarkerButton = new System.Windows.Forms.Button();
            this.MarkerView = new BizHawk.Client.EmuHawk.InputRoll();
            this.MarkersGroupBox = new System.Windows.Forms.GroupBox();
            this.MarkerContextMenu.SuspendLayout();
            this.MarkersGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // MarkerContextMenu
            // 
            resources.ApplyResources(this.MarkerContextMenu, "MarkerContextMenu");
            this.MarkerContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.JumpToMarkerToolStripMenuItem,
            this.ScrollToMarkerToolStripMenuItem,
            this.EditMarkerToolStripMenuItem,
            this.EditMarkerFrameToolStripMenuItem,
            this.AddMarkerToolStripMenuItem,
            this.toolStripSeparator1,
            this.RemoveMarkerToolStripMenuItem});
            this.MarkerContextMenu.Name = "MarkerContextMenu";
            this.toolTip1.SetToolTip(this.MarkerContextMenu, resources.GetString("MarkerContextMenu.ToolTip"));
            this.MarkerContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.MarkerContextMenu_Opening);
            // 
            // JumpToMarkerToolStripMenuItem
            // 
            resources.ApplyResources(this.JumpToMarkerToolStripMenuItem, "JumpToMarkerToolStripMenuItem");
            this.JumpToMarkerToolStripMenuItem.Click += new System.EventHandler(this.JumpToMarkerToolStripMenuItem_Click);
            // 
            // ScrollToMarkerToolStripMenuItem
            // 
            resources.ApplyResources(this.ScrollToMarkerToolStripMenuItem, "ScrollToMarkerToolStripMenuItem");
            this.ScrollToMarkerToolStripMenuItem.Click += new System.EventHandler(this.ScrollToMarkerToolStripMenuItem_Click);
            // 
            // EditMarkerToolStripMenuItem
            // 
            resources.ApplyResources(this.EditMarkerToolStripMenuItem, "EditMarkerToolStripMenuItem");
            this.EditMarkerToolStripMenuItem.Click += new System.EventHandler(this.EditMarkerToolStripMenuItem_Click);
            // 
            // EditMarkerFrameToolStripMenuItem
            // 
            resources.ApplyResources(this.EditMarkerFrameToolStripMenuItem, "EditMarkerFrameToolStripMenuItem");
            this.EditMarkerFrameToolStripMenuItem.Click += new System.EventHandler(this.EditMarkerFrameToolStripMenuItem_Click);
            // 
            // AddMarkerToolStripMenuItem
            // 
            resources.ApplyResources(this.AddMarkerToolStripMenuItem, "AddMarkerToolStripMenuItem");
            this.AddMarkerToolStripMenuItem.Click += new System.EventHandler(this.AddMarkerToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // RemoveMarkerToolStripMenuItem
            // 
            resources.ApplyResources(this.RemoveMarkerToolStripMenuItem, "RemoveMarkerToolStripMenuItem");
            this.RemoveMarkerToolStripMenuItem.Click += new System.EventHandler(this.RemoveMarkerToolStripMenuItem_Click);
            // 
            // JumpToMarkerButton
            // 
            resources.ApplyResources(this.JumpToMarkerButton, "JumpToMarkerButton");
            this.JumpToMarkerButton.Name = "JumpToMarkerButton";
            this.toolTip1.SetToolTip(this.JumpToMarkerButton, resources.GetString("JumpToMarkerButton.ToolTip"));
            this.JumpToMarkerButton.UseVisualStyleBackColor = true;
            this.JumpToMarkerButton.Click += new System.EventHandler(this.JumpToMarkerToolStripMenuItem_Click);
            // 
            // EditMarkerButton
            // 
            resources.ApplyResources(this.EditMarkerButton, "EditMarkerButton");
            this.EditMarkerButton.Name = "EditMarkerButton";
            this.toolTip1.SetToolTip(this.EditMarkerButton, resources.GetString("EditMarkerButton.ToolTip"));
            this.EditMarkerButton.UseVisualStyleBackColor = true;
            this.EditMarkerButton.Click += new System.EventHandler(this.EditMarkerToolStripMenuItem_Click);
            // 
            // EditMarkerFrameButton
            // 
            resources.ApplyResources(this.EditMarkerFrameButton, "EditMarkerFrameButton");
            this.EditMarkerFrameButton.Name = "EditMarkerFrameButton";
            this.toolTip1.SetToolTip(this.EditMarkerFrameButton, resources.GetString("EditMarkerFrameButton.ToolTip"));
            this.EditMarkerFrameButton.UseVisualStyleBackColor = true;
            this.EditMarkerFrameButton.Click += new System.EventHandler(this.EditMarkerFrameToolStripMenuItem_Click);
            // 
            // AddMarkerButton
            // 
            resources.ApplyResources(this.AddMarkerButton, "AddMarkerButton");
            this.AddMarkerButton.Name = "AddMarkerButton";
            this.toolTip1.SetToolTip(this.AddMarkerButton, resources.GetString("AddMarkerButton.ToolTip"));
            this.AddMarkerButton.UseVisualStyleBackColor = true;
            this.AddMarkerButton.Click += new System.EventHandler(this.AddMarkerToolStripMenuItem_Click);
            // 
            // ScrollToMarkerButton
            // 
            resources.ApplyResources(this.ScrollToMarkerButton, "ScrollToMarkerButton");
            this.ScrollToMarkerButton.Name = "ScrollToMarkerButton";
            this.toolTip1.SetToolTip(this.ScrollToMarkerButton, resources.GetString("ScrollToMarkerButton.ToolTip"));
            this.ScrollToMarkerButton.UseVisualStyleBackColor = true;
            this.ScrollToMarkerButton.Click += new System.EventHandler(this.ScrollToMarkerToolStripMenuItem_Click);
            // 
            // AddMarkerWithTextButton
            // 
            resources.ApplyResources(this.AddMarkerWithTextButton, "AddMarkerWithTextButton");
            this.AddMarkerWithTextButton.Name = "AddMarkerWithTextButton";
            this.toolTip1.SetToolTip(this.AddMarkerWithTextButton, resources.GetString("AddMarkerWithTextButton.ToolTip"));
            this.AddMarkerWithTextButton.UseVisualStyleBackColor = true;
            this.AddMarkerWithTextButton.Click += new System.EventHandler(this.AddMarkerWithTextToolStripMenuItem_Click);
            // 
            // RemoveMarkerButton
            // 
            resources.ApplyResources(this.RemoveMarkerButton, "RemoveMarkerButton");
            this.RemoveMarkerButton.Name = "RemoveMarkerButton";
            this.toolTip1.SetToolTip(this.RemoveMarkerButton, resources.GetString("RemoveMarkerButton.ToolTip"));
            this.RemoveMarkerButton.UseVisualStyleBackColor = true;
            this.RemoveMarkerButton.Click += new System.EventHandler(this.RemoveMarkerToolStripMenuItem_Click);
            // 
            // MarkerView
            // 
            resources.ApplyResources(this.MarkerView, "MarkerView");
            this.MarkerView.AllowColumnReorder = false;
            this.MarkerView.AllowColumnResize = false;
            this.MarkerView.AlwaysScroll = false;
            this.MarkerView.CellHeightPadding = 0;
            this.MarkerView.ContextMenuStrip = this.MarkerContextMenu;
            this.MarkerView.FullRowSelect = true;
            this.MarkerView.HorizontalOrientation = false;
            this.MarkerView.LetKeysModifySelection = false;
            this.MarkerView.Name = "MarkerView";
            this.MarkerView.RowCount = 0;
            this.MarkerView.ScrollSpeed = 1;
            this.MarkerView.TabStop = false;
            this.toolTip1.SetToolTip(this.MarkerView, resources.GetString("MarkerView.ToolTip"));
            this.MarkerView.SelectedIndexChanged += new System.EventHandler(this.MarkerView_SelectedIndexChanged);
            this.MarkerView.DoubleClick += new System.EventHandler(this.MarkerView_MouseDoubleClick);
            this.MarkerView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MarkerView_MouseDown);
            // 
            // MarkersGroupBox
            // 
            resources.ApplyResources(this.MarkersGroupBox, "MarkersGroupBox");
            this.MarkersGroupBox.Controls.Add(this.MarkerView);
            this.MarkersGroupBox.Controls.Add(this.AddMarkerButton);
            this.MarkersGroupBox.Controls.Add(this.AddMarkerWithTextButton);
            this.MarkersGroupBox.Controls.Add(this.RemoveMarkerButton);
            this.MarkersGroupBox.Controls.Add(this.ScrollToMarkerButton);
            this.MarkersGroupBox.Controls.Add(this.EditMarkerButton);
            this.MarkersGroupBox.Controls.Add(this.EditMarkerFrameButton);
            this.MarkersGroupBox.Controls.Add(this.JumpToMarkerButton);
            this.MarkersGroupBox.Name = "MarkersGroupBox";
            this.MarkersGroupBox.TabStop = false;
            this.toolTip1.SetToolTip(this.MarkersGroupBox, resources.GetString("MarkersGroupBox.ToolTip"));
            // 
            // MarkerControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.MarkersGroupBox);
            this.Name = "MarkerControl";
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
            this.MarkerContextMenu.ResumeLayout(false);
            this.MarkersGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private InputRoll MarkerView;
		private System.Windows.Forms.Button AddMarkerButton;
		private System.Windows.Forms.Button RemoveMarkerButton;
		private System.Windows.Forms.Button JumpToMarkerButton;
		private System.Windows.Forms.Button EditMarkerButton;
		private System.Windows.Forms.Button EditMarkerFrameButton;
		private System.Windows.Forms.Button ScrollToMarkerButton;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.ContextMenuStrip MarkerContextMenu;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx ScrollToMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx EditMarkerFrameToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx AddMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx RemoveMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx JumpToMarkerToolStripMenuItem;
		private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private System.Windows.Forms.Button AddMarkerWithTextButton;
		private System.Windows.Forms.GroupBox MarkersGroupBox;
	}
}
