namespace BizHawk.MultiClient
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
			this.button2 = new System.Windows.Forms.Button();
			this.button1 = new System.Windows.Forms.Button();
			this.MovieCount = new System.Windows.Forms.Label();
			this.ReadOnlyCheckBox = new System.Windows.Forms.CheckBox();
			this.IncludeSubDirectories = new System.Windows.Forms.CheckBox();
			this.ShowStateFiles = new System.Windows.Forms.CheckBox();
			this.Scan = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.MatchGameNameCheckBox = new System.Windows.Forms.CheckBox();
			this.MovieView = new BizHawk.VirtualListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(678, 352);
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
			this.OK.Location = new System.Drawing.Point(597, 352);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 50;
			this.OK.Text = "&Ok";
			this.toolTip1.SetToolTip(this.OK, "Load selected movie");
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// BrowseMovies
			// 
			this.BrowseMovies.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BrowseMovies.Image = global::BizHawk.MultiClient.Properties.Resources.OpenFile;
			this.BrowseMovies.Location = new System.Drawing.Point(12, 337);
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
			this.DetailsView.Size = new System.Drawing.Size(228, 242);
			this.DetailsView.TabIndex = 10;
			this.toolTip1.SetToolTip(this.DetailsView, "Contains the header information for the selected movie");
			this.DetailsView.UseCompatibleStateImageBehavior = false;
			this.DetailsView.View = System.Windows.Forms.View.Details;
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
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.button2);
			this.groupBox1.Controls.Add(this.button1);
			this.groupBox1.Controls.Add(this.DetailsView);
			this.groupBox1.Location = new System.Drawing.Point(494, 28);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(259, 303);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Details";
			// 
			// button2
			// 
			this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button2.Enabled = false;
			this.button2.Location = new System.Drawing.Point(125, 267);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 20;
			this.button2.Text = "Subtitles";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler(this.button2_Click);
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.button1.Enabled = false;
			this.button1.Location = new System.Drawing.Point(15, 267);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(75, 23);
			this.button1.TabIndex = 15;
			this.button1.Text = "Comments";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
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
			this.ReadOnlyCheckBox.Location = new System.Drawing.Point(494, 356);
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
			this.IncludeSubDirectories.Location = new System.Drawing.Point(123, 337);
			this.IncludeSubDirectories.Name = "IncludeSubDirectories";
			this.IncludeSubDirectories.Size = new System.Drawing.Size(131, 17);
			this.IncludeSubDirectories.TabIndex = 35;
			this.IncludeSubDirectories.Text = "Include Subdirectories";
			this.IncludeSubDirectories.UseVisualStyleBackColor = true;
			this.IncludeSubDirectories.CheckedChanged += new System.EventHandler(this.IncludeSubDirectories_CheckedChanged);
			// 
			// ShowStateFiles
			// 
			this.ShowStateFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.ShowStateFiles.AutoSize = true;
			this.ShowStateFiles.Location = new System.Drawing.Point(123, 358);
			this.ShowStateFiles.Name = "ShowStateFiles";
			this.ShowStateFiles.Size = new System.Drawing.Size(128, 17);
			this.ShowStateFiles.TabIndex = 40;
			this.ShowStateFiles.Text = "Show valid .state files";
			this.ShowStateFiles.UseVisualStyleBackColor = true;
			this.ShowStateFiles.CheckedChanged += new System.EventHandler(this.ShowStateFiles_CheckedChanged);
			// 
			// Scan
			// 
			this.Scan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.Scan.Image = global::BizHawk.MultiClient.Properties.Resources.Scan;
			this.Scan.Location = new System.Drawing.Point(49, 337);
			this.Scan.Name = "Scan";
			this.Scan.Size = new System.Drawing.Size(27, 23);
			this.Scan.TabIndex = 30;
			this.toolTip1.SetToolTip(this.Scan, "Rescan Movie folder for movie files");
			this.Scan.UseVisualStyleBackColor = true;
			this.Scan.Click += new System.EventHandler(this.Scan_Click);
			// 
			// MatchGameNameCheckBox
			// 
			this.MatchGameNameCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.MatchGameNameCheckBox.AutoSize = true;
			this.MatchGameNameCheckBox.Location = new System.Drawing.Point(260, 337);
			this.MatchGameNameCheckBox.Name = "MatchGameNameCheckBox";
			this.MatchGameNameCheckBox.Size = new System.Drawing.Size(150, 17);
			this.MatchGameNameCheckBox.TabIndex = 56;
			this.MatchGameNameCheckBox.Text = "Match current game name";
			this.MatchGameNameCheckBox.UseVisualStyleBackColor = true;
			this.MatchGameNameCheckBox.CheckedChanged += new System.EventHandler(this.MatchGameNameCheckBox_CheckedChanged);
			// 
			// MovieView
			// 
			this.MovieView.AllowDrop = true;
			this.MovieView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.MovieView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4});
			this.MovieView.FullRowSelect = true;
			this.MovieView.GridLines = true;
			this.MovieView.HideSelection = false;
			this.MovieView.ItemCount = 0;
			this.MovieView.Location = new System.Drawing.Point(12, 28);
			this.MovieView.MultiSelect = false;
			this.MovieView.Name = "MovieView";
			this.MovieView.selectedItem = -1;
			this.MovieView.Size = new System.Drawing.Size(463, 303);
			this.MovieView.TabIndex = 5;
			this.MovieView.UseCompatibleStateImageBehavior = false;
			this.MovieView.View = System.Windows.Forms.View.Details;
			this.MovieView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.MovieView_ColumnClick);
			this.MovieView.SelectedIndexChanged += new System.EventHandler(this.MovieView_SelectedIndexChanged);
			this.MovieView.DragDrop += new System.Windows.Forms.DragEventHandler(this.MovieView_DragDrop);
			this.MovieView.DragEnter += new System.Windows.Forms.DragEventHandler(this.MovieView_DragEnter);
			this.MovieView.DoubleClick += new System.EventHandler(this.MovieView_DoubleClick);
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
			this.columnHeader4.Width = 64;
			// 
			// PlayMovie
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(765, 387);
			this.Controls.Add(this.MatchGameNameCheckBox);
			this.Controls.Add(this.Scan);
			this.Controls.Add(this.ShowStateFiles);
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
			this.Text = "Play Movie";
			this.Load += new System.EventHandler(this.PlayMovie_Load);
			this.groupBox1.ResumeLayout(false);
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
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label MovieCount;
        private System.Windows.Forms.CheckBox ReadOnlyCheckBox;
		private System.Windows.Forms.CheckBox IncludeSubDirectories;
		private System.Windows.Forms.CheckBox ShowStateFiles;
		private System.Windows.Forms.Button Scan;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.CheckBox MatchGameNameCheckBox;
    }
}