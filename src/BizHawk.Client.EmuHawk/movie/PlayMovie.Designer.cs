namespace BizHawk.Client.EmuHawk
{
	partial class PlayMovie
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PlayMovie));
            this.Cancel = new System.Windows.Forms.Button();
            this.OK = new System.Windows.Forms.Button();
            this.BrowseMovies = new System.Windows.Forms.Button();
            this.DetailsView = new System.Windows.Forms.ListView();
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.SubtitlesBtn = new System.Windows.Forms.Button();
            this.CommentsBtn = new System.Windows.Forms.Button();
            this.ReadOnlyCheckBox = new System.Windows.Forms.CheckBox();
            this.IncludeSubDirectories = new System.Windows.Forms.CheckBox();
            this.Scan = new System.Windows.Forms.Button();
            this.MatchHashCheckBox = new System.Windows.Forms.CheckBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.editToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.StopOnFrameCheckbox = new System.Windows.Forms.CheckBox();
            this.MovieView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LastFrameCheckbox = new System.Windows.Forms.CheckBox();
            this.TurboCheckbox = new System.Windows.Forms.CheckBox();
            this.StopOnFrameTextBox = new BizHawk.Client.EmuHawk.WatchValueBox();
            this.MovieCount = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // BrowseMovies
            // 
            resources.ApplyResources(this.BrowseMovies, "BrowseMovies");
            this.BrowseMovies.Name = "BrowseMovies";
            this.BrowseMovies.UseVisualStyleBackColor = true;
            this.BrowseMovies.Click += new System.EventHandler(this.BrowseMovies_Click);
            // 
            // DetailsView
            // 
            resources.ApplyResources(this.DetailsView, "DetailsView");
            this.DetailsView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6});
            this.DetailsView.FullRowSelect = true;
            this.DetailsView.GridLines = true;
            this.DetailsView.HideSelection = false;
            this.DetailsView.Name = "DetailsView";
            this.DetailsView.ShowItemToolTips = true;
            this.DetailsView.UseCompatibleStateImageBehavior = false;
            this.DetailsView.View = System.Windows.Forms.View.Details;
            this.DetailsView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.DetailsView_ColumnClick);
            // 
            // columnHeader5
            // 
            resources.ApplyResources(this.columnHeader5, "columnHeader5");
            // 
            // columnHeader6
            // 
            resources.ApplyResources(this.columnHeader6, "columnHeader6");
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.SubtitlesBtn);
            this.groupBox1.Controls.Add(this.CommentsBtn);
            this.groupBox1.Controls.Add(this.DetailsView);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // SubtitlesBtn
            // 
            resources.ApplyResources(this.SubtitlesBtn, "SubtitlesBtn");
            this.SubtitlesBtn.Name = "SubtitlesBtn";
            this.SubtitlesBtn.UseVisualStyleBackColor = true;
            this.SubtitlesBtn.Click += new System.EventHandler(this.SubtitlesBtn_Click);
            // 
            // CommentsBtn
            // 
            resources.ApplyResources(this.CommentsBtn, "CommentsBtn");
            this.CommentsBtn.Name = "CommentsBtn";
            this.CommentsBtn.UseVisualStyleBackColor = true;
            this.CommentsBtn.Click += new System.EventHandler(this.CommentsBtn_Click);
            // 
            // ReadOnlyCheckBox
            // 
            resources.ApplyResources(this.ReadOnlyCheckBox, "ReadOnlyCheckBox");
            this.ReadOnlyCheckBox.Checked = true;
            this.ReadOnlyCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ReadOnlyCheckBox.Name = "ReadOnlyCheckBox";
            this.ReadOnlyCheckBox.UseVisualStyleBackColor = true;
            // 
            // IncludeSubDirectories
            // 
            resources.ApplyResources(this.IncludeSubDirectories, "IncludeSubDirectories");
            this.IncludeSubDirectories.Name = "IncludeSubDirectories";
            this.IncludeSubDirectories.UseVisualStyleBackColor = true;
            this.IncludeSubDirectories.CheckedChanged += new System.EventHandler(this.IncludeSubDirectories_CheckedChanged);
            // 
            // Scan
            // 
            resources.ApplyResources(this.Scan, "Scan");
            this.Scan.Name = "Scan";
            this.Scan.UseVisualStyleBackColor = true;
            this.Scan.Click += new System.EventHandler(this.Scan_Click);
            // 
            // MatchHashCheckBox
            // 
            resources.ApplyResources(this.MatchHashCheckBox, "MatchHashCheckBox");
            this.MatchHashCheckBox.Name = "MatchHashCheckBox";
            this.MatchHashCheckBox.UseVisualStyleBackColor = true;
            this.MatchHashCheckBox.CheckedChanged += new System.EventHandler(this.MatchHashCheckBox_CheckedChanged);
            // 
            // contextMenuStrip1
            // 
            resources.ApplyResources(this.contextMenuStrip1, "contextMenuStrip1");
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            // 
            // editToolStripMenuItem
            // 
            resources.ApplyResources(this.editToolStripMenuItem, "editToolStripMenuItem");
            this.editToolStripMenuItem.Click += new System.EventHandler(this.EditMenuItem_Click);
            // 
            // StopOnFrameCheckbox
            // 
            resources.ApplyResources(this.StopOnFrameCheckbox, "StopOnFrameCheckbox");
            this.StopOnFrameCheckbox.Name = "StopOnFrameCheckbox";
            this.StopOnFrameCheckbox.UseVisualStyleBackColor = true;
            this.StopOnFrameCheckbox.CheckedChanged += new System.EventHandler(this.StopOnFrameCheckbox_CheckedChanged);
            // 
            // MovieView
            // 
            resources.ApplyResources(this.MovieView, "MovieView");
            this.MovieView.AllowDrop = true;
            this.MovieView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
            this.MovieView.ContextMenuStrip = this.contextMenuStrip1;
            this.MovieView.FullRowSelect = true;
            this.MovieView.GridLines = true;
            this.MovieView.HideSelection = false;
            this.MovieView.MultiSelect = false;
            this.MovieView.Name = "MovieView";
            this.MovieView.UseCompatibleStateImageBehavior = false;
            this.MovieView.View = System.Windows.Forms.View.Details;
            this.MovieView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.MovieView_ColumnClick);
            this.MovieView.SelectedIndexChanged += new System.EventHandler(this.MovieView_SelectedIndexChanged);
            this.MovieView.DragDrop += new System.Windows.Forms.DragEventHandler(this.MovieView_DragDrop);
            this.MovieView.DragEnter += new System.Windows.Forms.DragEventHandler(this.MovieView_DragEnter);
            this.MovieView.DoubleClick += new System.EventHandler(this.MovieView_DoubleClick);
            this.MovieView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MovieView_KeyDown);
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // columnHeader3
            // 
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // columnHeader4
            // 
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // LastFrameCheckbox
            // 
            resources.ApplyResources(this.LastFrameCheckbox, "LastFrameCheckbox");
            this.LastFrameCheckbox.Name = "LastFrameCheckbox";
            this.LastFrameCheckbox.UseVisualStyleBackColor = true;
            this.LastFrameCheckbox.CheckedChanged += new System.EventHandler(this.LastFrameCheckbox_CheckedChanged);
            // 
            // TurboCheckbox
            // 
            resources.ApplyResources(this.TurboCheckbox, "TurboCheckbox");
            this.TurboCheckbox.Name = "TurboCheckbox";
            this.TurboCheckbox.UseVisualStyleBackColor = true;
            // 
            // StopOnFrameTextBox
            // 
            resources.ApplyResources(this.StopOnFrameTextBox, "StopOnFrameTextBox");
            this.StopOnFrameTextBox.ByteSize = BizHawk.Client.Common.WatchSize.DWord;
            this.StopOnFrameTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.StopOnFrameTextBox.Name = "StopOnFrameTextBox";
            this.StopOnFrameTextBox.Nullable = true;
            this.StopOnFrameTextBox.Type = BizHawk.Client.Common.WatchDisplayType.Unsigned;
            this.StopOnFrameTextBox.TextChanged += new System.EventHandler(this.StopOnFrameTextBox_TextChanged_1);
            // 
            // MovieCount
            // 
            resources.ApplyResources(this.MovieCount, "MovieCount");
            this.MovieCount.Name = "MovieCount";
            // 
            // PlayMovie
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.TurboCheckbox);
            this.Controls.Add(this.LastFrameCheckbox);
            this.Controls.Add(this.StopOnFrameTextBox);
            this.Controls.Add(this.StopOnFrameCheckbox);
            this.Controls.Add(this.MatchHashCheckBox);
            this.Controls.Add(this.Scan);
            this.Controls.Add(this.IncludeSubDirectories);
            this.Controls.Add(this.ReadOnlyCheckBox);
            this.Controls.Add(this.MovieCount);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.MovieView);
            this.Controls.Add(this.BrowseMovies);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.Cancel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlayMovie";
            this.Load += new System.EventHandler(this.PlayMovie_Load);
            this.groupBox1.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button BrowseMovies;
		private  System.Windows.Forms.ListView MovieView;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.ColumnHeader columnHeader4;
		private System.Windows.Forms.ListView DetailsView;
		private System.Windows.Forms.ColumnHeader columnHeader5;
		private System.Windows.Forms.ColumnHeader columnHeader6;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button SubtitlesBtn;
		private System.Windows.Forms.Button CommentsBtn;
		private BizHawk.WinForms.Controls.LocLabelEx MovieCount;
		private System.Windows.Forms.CheckBox ReadOnlyCheckBox;
		private System.Windows.Forms.CheckBox IncludeSubDirectories;
		private System.Windows.Forms.Button Scan;
		private System.Windows.Forms.CheckBox MatchHashCheckBox;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx editToolStripMenuItem;
		private System.Windows.Forms.CheckBox StopOnFrameCheckbox;
		private WatchValueBox StopOnFrameTextBox;
		private System.Windows.Forms.CheckBox LastFrameCheckbox;
		private System.Windows.Forms.CheckBox TurboCheckbox;
	}
}