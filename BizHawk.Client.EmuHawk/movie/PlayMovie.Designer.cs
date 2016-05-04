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
			this.MovieCount = new System.Windows.Forms.Label();
			this.ReadOnlyCheckBox = new System.Windows.Forms.CheckBox();
			this.IncludeSubDirectories = new System.Windows.Forms.CheckBox();
			this.Scan = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.MatchHashCheckBox = new System.Windows.Forms.CheckBox();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StopOnFrameCheckbox = new System.Windows.Forms.CheckBox();
			this.StopOnFrameTextBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.MovieView = new BizHawk.Client.EmuHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.LastFrameCheckbox = new System.Windows.Forms.CheckBox();
			this.TurboCheckbox = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(687, 363);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 55;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(606, 363);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 50;
			this.OK.Text = "&OK";
			this.toolTip1.SetToolTip(this.OK, "Load selected movie");
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// BrowseMovies
			// 
			this.BrowseMovies.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BrowseMovies.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.OpenFile;
			this.BrowseMovies.Location = new System.Drawing.Point(12, 364);
			this.BrowseMovies.Name = "BrowseMovies";
			this.BrowseMovies.Size = new System.Drawing.Size(31, 23);
			this.BrowseMovies.TabIndex = 25;
			this.toolTip1.SetToolTip(this.BrowseMovies, "Browse for additional movie files");
			this.BrowseMovies.UseVisualStyleBackColor = true;
			this.BrowseMovies.Click += new System.EventHandler(this.BrowseMovies_Click);
			// 
			// DetailsView
			// 
			this.DetailsView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DetailsView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader6});
			this.DetailsView.FullRowSelect = true;
			this.DetailsView.GridLines = true;
			this.DetailsView.HideSelection = false;
			this.DetailsView.Location = new System.Drawing.Point(15, 19);
			this.DetailsView.Name = "DetailsView";
			this.DetailsView.Size = new System.Drawing.Size(228, 261);
			this.DetailsView.TabIndex = 10;
			this.toolTip1.SetToolTip(this.DetailsView, "Contains the header information for the selected movie");
			this.DetailsView.UseCompatibleStateImageBehavior = false;
			this.DetailsView.View = System.Windows.Forms.View.Details;
			this.DetailsView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.DetailsView_ColumnClick);
			// 
			// columnHeader5
			// 
			this.columnHeader5.Text = "Header";
			this.columnHeader5.Width = 102;
			// 
			// columnHeader6
			// 
			this.columnHeader6.Text = "Value";
			this.columnHeader6.Width = 121;
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.SubtitlesBtn);
			this.groupBox1.Controls.Add(this.CommentsBtn);
			this.groupBox1.Controls.Add(this.DetailsView);
			this.groupBox1.Location = new System.Drawing.Point(503, 28);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(259, 322);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Details";
			// 
			// SubtitlesBtn
			// 
			this.SubtitlesBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.SubtitlesBtn.Enabled = false;
			this.SubtitlesBtn.Location = new System.Drawing.Point(125, 286);
			this.SubtitlesBtn.Name = "SubtitlesBtn";
			this.SubtitlesBtn.Size = new System.Drawing.Size(75, 23);
			this.SubtitlesBtn.TabIndex = 20;
			this.SubtitlesBtn.Text = "Subtitles";
			this.SubtitlesBtn.UseVisualStyleBackColor = true;
			this.SubtitlesBtn.Click += new System.EventHandler(this.SubtitlesBtn_Click);
			// 
			// CommentsBtn
			// 
			this.CommentsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.CommentsBtn.Enabled = false;
			this.CommentsBtn.Location = new System.Drawing.Point(15, 286);
			this.CommentsBtn.Name = "CommentsBtn";
			this.CommentsBtn.Size = new System.Drawing.Size(75, 23);
			this.CommentsBtn.TabIndex = 15;
			this.CommentsBtn.Text = "Comments";
			this.CommentsBtn.UseVisualStyleBackColor = true;
			this.CommentsBtn.Click += new System.EventHandler(this.CommentsBtn_Click);
			// 
			// MovieCount
			// 
			this.MovieCount.AutoSize = true;
			this.MovieCount.Location = new System.Drawing.Point(12, 9);
			this.MovieCount.Name = "MovieCount";
			this.MovieCount.Size = new System.Drawing.Size(31, 13);
			this.MovieCount.TabIndex = 7;
			this.MovieCount.Text = "        ";
			// 
			// ReadOnlyCheckBox
			// 
			this.ReadOnlyCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ReadOnlyCheckBox.AutoSize = true;
			this.ReadOnlyCheckBox.Checked = true;
			this.ReadOnlyCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.ReadOnlyCheckBox.Location = new System.Drawing.Point(503, 367);
			this.ReadOnlyCheckBox.Name = "ReadOnlyCheckBox";
			this.ReadOnlyCheckBox.Size = new System.Drawing.Size(74, 17);
			this.ReadOnlyCheckBox.TabIndex = 45;
			this.ReadOnlyCheckBox.Text = "Read only";
			this.ReadOnlyCheckBox.UseVisualStyleBackColor = true;
			// 
			// IncludeSubDirectories
			// 
			this.IncludeSubDirectories.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.IncludeSubDirectories.AutoSize = true;
			this.IncludeSubDirectories.Location = new System.Drawing.Point(94, 375);
			this.IncludeSubDirectories.Name = "IncludeSubDirectories";
			this.IncludeSubDirectories.Size = new System.Drawing.Size(131, 17);
			this.IncludeSubDirectories.TabIndex = 35;
			this.IncludeSubDirectories.Text = "Include Subdirectories";
			this.IncludeSubDirectories.UseVisualStyleBackColor = true;
			this.IncludeSubDirectories.CheckedChanged += new System.EventHandler(this.IncludeSubDirectories_CheckedChanged);
			// 
			// Scan
			// 
			this.Scan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.Scan.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Scan;
			this.Scan.Location = new System.Drawing.Point(49, 364);
			this.Scan.Name = "Scan";
			this.Scan.Size = new System.Drawing.Size(27, 23);
			this.Scan.TabIndex = 30;
			this.toolTip1.SetToolTip(this.Scan, "Rescan Movie folder for movie files");
			this.Scan.UseVisualStyleBackColor = true;
			this.Scan.Click += new System.EventHandler(this.Scan_Click);
			// 
			// MatchHashCheckBox
			// 
			this.MatchHashCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MatchHashCheckBox.AutoSize = true;
			this.MatchHashCheckBox.Location = new System.Drawing.Point(94, 357);
			this.MatchHashCheckBox.Name = "MatchHashCheckBox";
			this.MatchHashCheckBox.Size = new System.Drawing.Size(147, 17);
			this.MatchHashCheckBox.TabIndex = 56;
			this.MatchHashCheckBox.Text = "Match current game hash";
			this.MatchHashCheckBox.UseVisualStyleBackColor = true;
			this.MatchHashCheckBox.CheckedChanged += new System.EventHandler(this.MatchHashCheckBox_CheckedChanged);
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.editToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(93, 26);
			// 
			// editToolStripMenuItem
			// 
			this.editToolStripMenuItem.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.CutHS;
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
			this.editToolStripMenuItem.Text = "&Edit";
			this.editToolStripMenuItem.Click += new System.EventHandler(this.EditMenuItem_Click);
			// 
			// StopOnFrameCheckbox
			// 
			this.StopOnFrameCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.StopOnFrameCheckbox.AutoSize = true;
			this.StopOnFrameCheckbox.Location = new System.Drawing.Point(342, 357);
			this.StopOnFrameCheckbox.Name = "StopOnFrameCheckbox";
			this.StopOnFrameCheckbox.Size = new System.Drawing.Size(95, 17);
			this.StopOnFrameCheckbox.TabIndex = 57;
			this.StopOnFrameCheckbox.Text = "Stop on frame:";
			this.StopOnFrameCheckbox.UseVisualStyleBackColor = true;
			this.StopOnFrameCheckbox.CheckedChanged += new System.EventHandler(this.StopOnFrameCheckbox_CheckedChanged);
			// 
			// StopOnFrameTextBox
			// 
			this.StopOnFrameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.StopOnFrameTextBox.ByteSize = BizHawk.Client.Common.WatchSize.DWord;
			this.StopOnFrameTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.StopOnFrameTextBox.Location = new System.Drawing.Point(438, 355);
			this.StopOnFrameTextBox.MaxLength = 10;
			this.StopOnFrameTextBox.Name = "StopOnFrameTextBox";
			this.StopOnFrameTextBox.Nullable = true;
			this.StopOnFrameTextBox.Size = new System.Drawing.Size(54, 20);
			this.StopOnFrameTextBox.TabIndex = 58;
			this.StopOnFrameTextBox.Type = BizHawk.Client.Common.DisplayType.Unsigned;
			this.StopOnFrameTextBox.TextChanged += new System.EventHandler(this.StopOnFrameTextBox_TextChanged_1);
			// 
			// MovieView
			// 
			this.MovieView.AllowDrop = true;
			this.MovieView.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MovieView.BlazingFast = false;
			this.MovieView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
			this.MovieView.ContextMenuStrip = this.contextMenuStrip1;
			this.MovieView.FullRowSelect = true;
			this.MovieView.GridLines = true;
			this.MovieView.HideSelection = false;
			this.MovieView.ItemCount = 0;
			this.MovieView.Location = new System.Drawing.Point(12, 28);
			this.MovieView.MultiSelect = false;
			this.MovieView.Name = "MovieView";
			this.MovieView.SelectAllInProgress = false;
			this.MovieView.selectedItem = -1;
			this.MovieView.Size = new System.Drawing.Size(480, 322);
			this.MovieView.TabIndex = 5;
			this.MovieView.UseCompatibleStateImageBehavior = false;
			this.MovieView.UseCustomBackground = true;
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
			this.columnHeader1.Text = "File";
			this.columnHeader1.Width = 221;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "SysID";
			this.columnHeader2.Width = 43;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Game";
			this.columnHeader3.Width = 129;
			// 
			// columnHeader4
			// 
			this.columnHeader4.Text = "Length (est.)";
			this.columnHeader4.Width = 82;
			// 
			// LastFrameCheckbox
			// 
			this.LastFrameCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LastFrameCheckbox.AutoSize = true;
			this.LastFrameCheckbox.Location = new System.Drawing.Point(342, 376);
			this.LastFrameCheckbox.Name = "LastFrameCheckbox";
			this.LastFrameCheckbox.Size = new System.Drawing.Size(75, 17);
			this.LastFrameCheckbox.TabIndex = 59;
			this.LastFrameCheckbox.Text = "Last frame";
			this.LastFrameCheckbox.UseVisualStyleBackColor = true;
			this.LastFrameCheckbox.CheckedChanged += new System.EventHandler(this.LastFrameCheckbox_CheckedChanged);
			// 
			// TurboCheckbox
			// 
			this.TurboCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.TurboCheckbox.AutoSize = true;
			this.TurboCheckbox.Location = new System.Drawing.Point(438, 376);
			this.TurboCheckbox.Name = "TurboCheckbox";
			this.TurboCheckbox.Size = new System.Drawing.Size(54, 17);
			this.TurboCheckbox.TabIndex = 60;
			this.TurboCheckbox.Text = "Turbo";
			this.TurboCheckbox.UseVisualStyleBackColor = true;
			// 
			// PlayMovie
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(774, 398);
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
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(547, 228);
			this.Name = "PlayMovie";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Play Movie";
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
		private VirtualListView MovieView;
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
		private System.Windows.Forms.Label MovieCount;
		private System.Windows.Forms.CheckBox ReadOnlyCheckBox;
		private System.Windows.Forms.CheckBox IncludeSubDirectories;
		private System.Windows.Forms.Button Scan;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.CheckBox MatchHashCheckBox;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.CheckBox StopOnFrameCheckbox;
		private WatchValueBox StopOnFrameTextBox;
		private System.Windows.Forms.CheckBox LastFrameCheckbox;
		private System.Windows.Forms.CheckBox TurboCheckbox;
	}
}