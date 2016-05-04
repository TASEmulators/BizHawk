namespace BizHawk.Client.DBMan
{
	partial class DBMan_MainForm
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
			this.filterPanel = new System.Windows.Forms.Panel();
			this.whereLabel = new System.Windows.Forms.Label();
			this.systemBox = new System.Windows.Forms.ComboBox();
			this.whereBox = new System.Windows.Forms.TextBox();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.romListView = new System.Windows.Forms.ListView();
			this.romListColumnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.romListColumnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.romListColumnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.romListColumnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.romListColumnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.detailPanel = new System.Windows.Forms.Panel();
			this.sizeLabel = new System.Windows.Forms.Label();
			this.sizeBox = new System.Windows.Forms.TextBox();
			this.notesLabel = new System.Windows.Forms.Label();
			this.notesBox = new System.Windows.Forms.TextBox();
			this.altNamesLabel = new System.Windows.Forms.Label();
			this.altNamesBox = new System.Windows.Forms.TextBox();
			this.cancelButton = new System.Windows.Forms.Button();
			this.saveButton = new System.Windows.Forms.Button();
			this.playersBox = new System.Windows.Forms.TextBox();
			this.catalogLabel = new System.Windows.Forms.Label();
			this.catalogBox = new System.Windows.Forms.TextBox();
			this.playersLabel = new System.Windows.Forms.Label();
			this.releaseDateLabel = new System.Windows.Forms.Label();
			this.releaseDateBox = new System.Windows.Forms.TextBox();
			this.classificationLabel = new System.Windows.Forms.Label();
			this.classificationBox = new System.Windows.Forms.ComboBox();
			this.publisherLabel = new System.Windows.Forms.Label();
			this.publisherBox = new System.Windows.Forms.TextBox();
			this.developerLabel = new System.Windows.Forms.Label();
			this.developerBox = new System.Windows.Forms.TextBox();
			this.romStatusLabel = new System.Windows.Forms.Label();
			this.romStatusBox = new System.Windows.Forms.ComboBox();
			this.tagsLabel = new System.Windows.Forms.Label();
			this.tagsBox = new System.Windows.Forms.TextBox();
			this.romMetaLabel = new System.Windows.Forms.Label();
			this.romMetaBox = new System.Windows.Forms.TextBox();
			this.gameMetaLabel = new System.Windows.Forms.Label();
			this.gameMetaBox = new System.Windows.Forms.TextBox();
			this.versionLabel = new System.Windows.Forms.Label();
			this.versionBox = new System.Windows.Forms.TextBox();
			this.regionLabel = new System.Windows.Forms.Label();
			this.regionBox = new System.Windows.Forms.TextBox();
			this.sha1Box = new System.Windows.Forms.TextBox();
			this.md5Box = new System.Windows.Forms.TextBox();
			this.crcBox = new System.Windows.Forms.TextBox();
			this.sha1Label = new System.Windows.Forms.Label();
			this.md5Label = new System.Windows.Forms.Label();
			this.crcLabel = new System.Windows.Forms.Label();
			this.gameSystemBox = new System.Windows.Forms.ComboBox();
			this.systemLabel = new System.Windows.Forms.Label();
			this.nameBox = new System.Windows.Forms.TextBox();
			this.nameLabel = new System.Windows.Forms.Label();
			this.mainMenuStrip = new System.Windows.Forms.MenuStrip();
			this.databaseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.directoryScanToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cleanupDBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exportGameDBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteButton = new System.Windows.Forms.Button();
			this.filterPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.detailPanel.SuspendLayout();
			this.mainMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// filterPanel
			// 
			this.filterPanel.Controls.Add(this.whereLabel);
			this.filterPanel.Controls.Add(this.systemBox);
			this.filterPanel.Controls.Add(this.whereBox);
			this.filterPanel.Controls.Add(this.menuStrip1);
			this.filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.filterPanel.Location = new System.Drawing.Point(0, 24);
			this.filterPanel.Name = "filterPanel";
			this.filterPanel.Size = new System.Drawing.Size(963, 30);
			this.filterPanel.TabIndex = 0;
			// 
			// whereLabel
			// 
			this.whereLabel.AutoSize = true;
			this.whereLabel.Location = new System.Drawing.Point(131, 9);
			this.whereLabel.Name = "whereLabel";
			this.whereLabel.Size = new System.Drawing.Size(42, 13);
			this.whereLabel.TabIndex = 2;
			this.whereLabel.Text = "Where:";
			// 
			// systemBox
			// 
			this.systemBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.systemBox.FormattingEnabled = true;
			this.systemBox.Location = new System.Drawing.Point(4, 3);
			this.systemBox.Name = "systemBox";
			this.systemBox.Size = new System.Drawing.Size(121, 21);
			this.systemBox.TabIndex = 1;
			this.systemBox.SelectedIndexChanged += new System.EventHandler(this.systemBox_SelectedIndexChanged);
			// 
			// whereBox
			// 
			this.whereBox.Location = new System.Drawing.Point(179, 3);
			this.whereBox.Name = "whereBox";
			this.whereBox.Size = new System.Drawing.Size(334, 20);
			this.whereBox.TabIndex = 2;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(963, 24);
			this.menuStrip1.TabIndex = 3;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 54);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.romListView);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.detailPanel);
			this.splitContainer1.Size = new System.Drawing.Size(963, 624);
			this.splitContainer1.SplitterDistance = 561;
			this.splitContainer1.TabIndex = 0;
			this.splitContainer1.TabStop = false;
			// 
			// romListView
			// 
			this.romListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.romListColumnHeader1,
            this.romListColumnHeader2,
            this.romListColumnHeader3,
            this.romListColumnHeader4,
            this.romListColumnHeader5});
			this.romListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.romListView.FullRowSelect = true;
			this.romListView.GridLines = true;
			this.romListView.Location = new System.Drawing.Point(0, 0);
			this.romListView.MultiSelect = false;
			this.romListView.Name = "romListView";
			this.romListView.Size = new System.Drawing.Size(561, 624);
			this.romListView.TabIndex = 0;
			this.romListView.TabStop = false;
			this.romListView.UseCompatibleStateImageBehavior = false;
			this.romListView.View = System.Windows.Forms.View.Details;
			this.romListView.SelectedIndexChanged += new System.EventHandler(this.selectedRomChanged);
			// 
			// romListColumnHeader1
			// 
			this.romListColumnHeader1.Text = "Game";
			this.romListColumnHeader1.Width = 86;
			// 
			// romListColumnHeader2
			// 
			this.romListColumnHeader2.Text = "Region";
			this.romListColumnHeader2.Width = 80;
			// 
			// romListColumnHeader3
			// 
			this.romListColumnHeader3.Text = "Version";
			this.romListColumnHeader3.Width = 80;
			// 
			// romListColumnHeader4
			// 
			this.romListColumnHeader4.Text = "Meta";
			this.romListColumnHeader4.Width = 80;
			// 
			// romListColumnHeader5
			// 
			this.romListColumnHeader5.Text = "Tags";
			// 
			// detailPanel
			// 
			this.detailPanel.Controls.Add(this.deleteButton);
			this.detailPanel.Controls.Add(this.sizeLabel);
			this.detailPanel.Controls.Add(this.sizeBox);
			this.detailPanel.Controls.Add(this.notesLabel);
			this.detailPanel.Controls.Add(this.notesBox);
			this.detailPanel.Controls.Add(this.altNamesLabel);
			this.detailPanel.Controls.Add(this.altNamesBox);
			this.detailPanel.Controls.Add(this.cancelButton);
			this.detailPanel.Controls.Add(this.saveButton);
			this.detailPanel.Controls.Add(this.playersBox);
			this.detailPanel.Controls.Add(this.catalogLabel);
			this.detailPanel.Controls.Add(this.catalogBox);
			this.detailPanel.Controls.Add(this.playersLabel);
			this.detailPanel.Controls.Add(this.releaseDateLabel);
			this.detailPanel.Controls.Add(this.releaseDateBox);
			this.detailPanel.Controls.Add(this.classificationLabel);
			this.detailPanel.Controls.Add(this.classificationBox);
			this.detailPanel.Controls.Add(this.publisherLabel);
			this.detailPanel.Controls.Add(this.publisherBox);
			this.detailPanel.Controls.Add(this.developerLabel);
			this.detailPanel.Controls.Add(this.developerBox);
			this.detailPanel.Controls.Add(this.romStatusLabel);
			this.detailPanel.Controls.Add(this.romStatusBox);
			this.detailPanel.Controls.Add(this.tagsLabel);
			this.detailPanel.Controls.Add(this.tagsBox);
			this.detailPanel.Controls.Add(this.romMetaLabel);
			this.detailPanel.Controls.Add(this.romMetaBox);
			this.detailPanel.Controls.Add(this.gameMetaLabel);
			this.detailPanel.Controls.Add(this.gameMetaBox);
			this.detailPanel.Controls.Add(this.versionLabel);
			this.detailPanel.Controls.Add(this.versionBox);
			this.detailPanel.Controls.Add(this.regionLabel);
			this.detailPanel.Controls.Add(this.regionBox);
			this.detailPanel.Controls.Add(this.sha1Box);
			this.detailPanel.Controls.Add(this.md5Box);
			this.detailPanel.Controls.Add(this.crcBox);
			this.detailPanel.Controls.Add(this.sha1Label);
			this.detailPanel.Controls.Add(this.md5Label);
			this.detailPanel.Controls.Add(this.crcLabel);
			this.detailPanel.Controls.Add(this.gameSystemBox);
			this.detailPanel.Controls.Add(this.systemLabel);
			this.detailPanel.Controls.Add(this.nameBox);
			this.detailPanel.Controls.Add(this.nameLabel);
			this.detailPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.detailPanel.Location = new System.Drawing.Point(0, 0);
			this.detailPanel.Name = "detailPanel";
			this.detailPanel.Size = new System.Drawing.Size(398, 624);
			this.detailPanel.TabIndex = 0;
			// 
			// sizeLabel
			// 
			this.sizeLabel.AutoSize = true;
			this.sizeLabel.Location = new System.Drawing.Point(6, 557);
			this.sizeLabel.Name = "sizeLabel";
			this.sizeLabel.Size = new System.Drawing.Size(27, 13);
			this.sizeLabel.TabIndex = 50;
			this.sizeLabel.Text = "Size";
			// 
			// sizeBox
			// 
			this.sizeBox.Location = new System.Drawing.Point(80, 557);
			this.sizeBox.Name = "sizeBox";
			this.sizeBox.ReadOnly = true;
			this.sizeBox.Size = new System.Drawing.Size(255, 20);
			this.sizeBox.TabIndex = 49;
			// 
			// notesLabel
			// 
			this.notesLabel.AutoSize = true;
			this.notesLabel.Location = new System.Drawing.Point(3, 408);
			this.notesLabel.Name = "notesLabel";
			this.notesLabel.Size = new System.Drawing.Size(35, 13);
			this.notesLabel.TabIndex = 48;
			this.notesLabel.Text = "Notes";
			// 
			// notesBox
			// 
			this.notesBox.Location = new System.Drawing.Point(80, 408);
			this.notesBox.Multiline = true;
			this.notesBox.Name = "notesBox";
			this.notesBox.Size = new System.Drawing.Size(296, 61);
			this.notesBox.TabIndex = 44;
			// 
			// altNamesLabel
			// 
			this.altNamesLabel.AutoSize = true;
			this.altNamesLabel.Location = new System.Drawing.Point(3, 381);
			this.altNamesLabel.Name = "altNamesLabel";
			this.altNamesLabel.Size = new System.Drawing.Size(55, 13);
			this.altNamesLabel.TabIndex = 46;
			this.altNamesLabel.Text = "Alt Names";
			// 
			// altNamesBox
			// 
			this.altNamesBox.Location = new System.Drawing.Point(80, 381);
			this.altNamesBox.Name = "altNamesBox";
			this.altNamesBox.Size = new System.Drawing.Size(296, 20);
			this.altNamesBox.TabIndex = 42;
			// 
			// cancelButton
			// 
			this.cancelButton.Location = new System.Drawing.Point(125, 589);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 48;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// saveButton
			// 
			this.saveButton.Location = new System.Drawing.Point(6, 589);
			this.saveButton.Name = "saveButton";
			this.saveButton.Size = new System.Drawing.Size(75, 23);
			this.saveButton.TabIndex = 46;
			this.saveButton.Text = "&Save";
			this.saveButton.UseVisualStyleBackColor = true;
			this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
			// 
			// playersBox
			// 
			this.playersBox.AutoCompleteCustomSource.AddRange(new string[] {
            "1 Player",
            "2 Players Alternating",
            "2 Players Cooperative",
            "2 Players Versus"});
			this.playersBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this.playersBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
			this.playersBox.Location = new System.Drawing.Point(80, 326);
			this.playersBox.Name = "playersBox";
			this.playersBox.Size = new System.Drawing.Size(194, 20);
			this.playersBox.TabIndex = 38;
			// 
			// catalogLabel
			// 
			this.catalogLabel.AutoSize = true;
			this.catalogLabel.Location = new System.Drawing.Point(3, 354);
			this.catalogLabel.Name = "catalogLabel";
			this.catalogLabel.Size = new System.Drawing.Size(43, 13);
			this.catalogLabel.TabIndex = 41;
			this.catalogLabel.Text = "Catalog";
			// 
			// catalogBox
			// 
			this.catalogBox.Location = new System.Drawing.Point(80, 354);
			this.catalogBox.Name = "catalogBox";
			this.catalogBox.Size = new System.Drawing.Size(194, 20);
			this.catalogBox.TabIndex = 40;
			// 
			// playersLabel
			// 
			this.playersLabel.AutoSize = true;
			this.playersLabel.Location = new System.Drawing.Point(3, 326);
			this.playersLabel.Name = "playersLabel";
			this.playersLabel.Size = new System.Drawing.Size(41, 13);
			this.playersLabel.TabIndex = 39;
			this.playersLabel.Text = "Players";
			// 
			// releaseDateLabel
			// 
			this.releaseDateLabel.AutoSize = true;
			this.releaseDateLabel.Location = new System.Drawing.Point(3, 299);
			this.releaseDateLabel.Name = "releaseDateLabel";
			this.releaseDateLabel.Size = new System.Drawing.Size(48, 13);
			this.releaseDateLabel.TabIndex = 37;
			this.releaseDateLabel.Text = "Rls Date";
			// 
			// releaseDateBox
			// 
			this.releaseDateBox.Location = new System.Drawing.Point(80, 299);
			this.releaseDateBox.Name = "releaseDateBox";
			this.releaseDateBox.Size = new System.Drawing.Size(100, 20);
			this.releaseDateBox.TabIndex = 36;
			// 
			// classificationLabel
			// 
			this.classificationLabel.AutoSize = true;
			this.classificationLabel.Location = new System.Drawing.Point(3, 271);
			this.classificationLabel.Name = "classificationLabel";
			this.classificationLabel.Size = new System.Drawing.Size(32, 13);
			this.classificationLabel.TabIndex = 35;
			this.classificationLabel.Text = "Class";
			// 
			// classificationBox
			// 
			this.classificationBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.classificationBox.FormattingEnabled = true;
			this.classificationBox.Items.AddRange(new object[] {
            "Licensed",
            "Unlicensed",
            "Unreleased",
            "Homebrew",
            "Test Rom",
            "Firmware"});
			this.classificationBox.Location = new System.Drawing.Point(80, 271);
			this.classificationBox.Name = "classificationBox";
			this.classificationBox.Size = new System.Drawing.Size(121, 21);
			this.classificationBox.TabIndex = 34;
			// 
			// publisherLabel
			// 
			this.publisherLabel.AutoSize = true;
			this.publisherLabel.Location = new System.Drawing.Point(3, 244);
			this.publisherLabel.Name = "publisherLabel";
			this.publisherLabel.Size = new System.Drawing.Size(50, 13);
			this.publisherLabel.TabIndex = 33;
			this.publisherLabel.Text = "Publisher";
			// 
			// publisherBox
			// 
			this.publisherBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this.publisherBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
			this.publisherBox.Location = new System.Drawing.Point(80, 244);
			this.publisherBox.Name = "publisherBox";
			this.publisherBox.Size = new System.Drawing.Size(194, 20);
			this.publisherBox.TabIndex = 32;
			// 
			// developerLabel
			// 
			this.developerLabel.AutoSize = true;
			this.developerLabel.Location = new System.Drawing.Point(3, 217);
			this.developerLabel.Name = "developerLabel";
			this.developerLabel.Size = new System.Drawing.Size(56, 13);
			this.developerLabel.TabIndex = 31;
			this.developerLabel.Text = "Developer";
			// 
			// developerBox
			// 
			this.developerBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this.developerBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
			this.developerBox.Location = new System.Drawing.Point(80, 217);
			this.developerBox.Name = "developerBox";
			this.developerBox.Size = new System.Drawing.Size(194, 20);
			this.developerBox.TabIndex = 30;
			// 
			// romStatusLabel
			// 
			this.romStatusLabel.AutoSize = true;
			this.romStatusLabel.Location = new System.Drawing.Point(3, 189);
			this.romStatusLabel.Name = "romStatusLabel";
			this.romStatusLabel.Size = new System.Drawing.Size(62, 13);
			this.romStatusLabel.TabIndex = 29;
			this.romStatusLabel.Text = "Rom Status";
			// 
			// romStatusBox
			// 
			this.romStatusBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.romStatusBox.FormattingEnabled = true;
			this.romStatusBox.Items.AddRange(new object[] {
            "OK",
            "Bad Dump",
            "Hack",
            "Translation",
            "Overdump",
            "Trained"});
			this.romStatusBox.Location = new System.Drawing.Point(80, 189);
			this.romStatusBox.Name = "romStatusBox";
			this.romStatusBox.Size = new System.Drawing.Size(121, 21);
			this.romStatusBox.TabIndex = 28;
			// 
			// tagsLabel
			// 
			this.tagsLabel.AutoSize = true;
			this.tagsLabel.Location = new System.Drawing.Point(3, 162);
			this.tagsLabel.Name = "tagsLabel";
			this.tagsLabel.Size = new System.Drawing.Size(31, 13);
			this.tagsLabel.TabIndex = 27;
			this.tagsLabel.Text = "Tags";
			// 
			// tagsBox
			// 
			this.tagsBox.Location = new System.Drawing.Point(80, 162);
			this.tagsBox.Name = "tagsBox";
			this.tagsBox.Size = new System.Drawing.Size(296, 20);
			this.tagsBox.TabIndex = 26;
			// 
			// romMetaLabel
			// 
			this.romMetaLabel.AutoSize = true;
			this.romMetaLabel.Location = new System.Drawing.Point(3, 135);
			this.romMetaLabel.Name = "romMetaLabel";
			this.romMetaLabel.Size = new System.Drawing.Size(62, 13);
			this.romMetaLabel.TabIndex = 25;
			this.romMetaLabel.Text = "Meta (Rom)";
			// 
			// romMetaBox
			// 
			this.romMetaBox.Location = new System.Drawing.Point(80, 135);
			this.romMetaBox.Name = "romMetaBox";
			this.romMetaBox.Size = new System.Drawing.Size(296, 20);
			this.romMetaBox.TabIndex = 24;
			// 
			// gameMetaLabel
			// 
			this.gameMetaLabel.AutoSize = true;
			this.gameMetaLabel.Location = new System.Drawing.Point(3, 108);
			this.gameMetaLabel.Name = "gameMetaLabel";
			this.gameMetaLabel.Size = new System.Drawing.Size(68, 13);
			this.gameMetaLabel.TabIndex = 23;
			this.gameMetaLabel.Text = "Meta (Game)";
			// 
			// gameMetaBox
			// 
			this.gameMetaBox.Location = new System.Drawing.Point(80, 108);
			this.gameMetaBox.Name = "gameMetaBox";
			this.gameMetaBox.Size = new System.Drawing.Size(296, 20);
			this.gameMetaBox.TabIndex = 22;
			// 
			// versionLabel
			// 
			this.versionLabel.AutoSize = true;
			this.versionLabel.Location = new System.Drawing.Point(3, 81);
			this.versionLabel.Name = "versionLabel";
			this.versionLabel.Size = new System.Drawing.Size(42, 13);
			this.versionLabel.TabIndex = 21;
			this.versionLabel.Text = "Version";
			// 
			// versionBox
			// 
			this.versionBox.Location = new System.Drawing.Point(80, 81);
			this.versionBox.Name = "versionBox";
			this.versionBox.Size = new System.Drawing.Size(296, 20);
			this.versionBox.TabIndex = 20;
			// 
			// regionLabel
			// 
			this.regionLabel.AutoSize = true;
			this.regionLabel.Location = new System.Drawing.Point(3, 54);
			this.regionLabel.Name = "regionLabel";
			this.regionLabel.Size = new System.Drawing.Size(41, 13);
			this.regionLabel.TabIndex = 19;
			this.regionLabel.Text = "Region";
			// 
			// regionBox
			// 
			this.regionBox.AutoCompleteCustomSource.AddRange(new string[] {
            "USA",
            "Japan",
            "Europe",
            "Taiwan",
            "Brazil",
            "Korea"});
			this.regionBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this.regionBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
			this.regionBox.Location = new System.Drawing.Point(80, 54);
			this.regionBox.Name = "regionBox";
			this.regionBox.Size = new System.Drawing.Size(194, 20);
			this.regionBox.TabIndex = 18;
			// 
			// sha1Box
			// 
			this.sha1Box.Location = new System.Drawing.Point(80, 530);
			this.sha1Box.Name = "sha1Box";
			this.sha1Box.ReadOnly = true;
			this.sha1Box.Size = new System.Drawing.Size(296, 20);
			this.sha1Box.TabIndex = 17;
			this.sha1Box.TabStop = false;
			// 
			// md5Box
			// 
			this.md5Box.Location = new System.Drawing.Point(80, 503);
			this.md5Box.Name = "md5Box";
			this.md5Box.ReadOnly = true;
			this.md5Box.Size = new System.Drawing.Size(255, 20);
			this.md5Box.TabIndex = 16;
			this.md5Box.TabStop = false;
			// 
			// crcBox
			// 
			this.crcBox.Location = new System.Drawing.Point(80, 477);
			this.crcBox.Name = "crcBox";
			this.crcBox.ReadOnly = true;
			this.crcBox.Size = new System.Drawing.Size(100, 20);
			this.crcBox.TabIndex = 15;
			this.crcBox.TabStop = false;
			// 
			// sha1Label
			// 
			this.sha1Label.AutoSize = true;
			this.sha1Label.Location = new System.Drawing.Point(3, 530);
			this.sha1Label.Name = "sha1Label";
			this.sha1Label.Size = new System.Drawing.Size(35, 13);
			this.sha1Label.TabIndex = 14;
			this.sha1Label.Text = "SHA1";
			// 
			// md5Label
			// 
			this.md5Label.AutoSize = true;
			this.md5Label.Location = new System.Drawing.Point(3, 503);
			this.md5Label.Name = "md5Label";
			this.md5Label.Size = new System.Drawing.Size(30, 13);
			this.md5Label.TabIndex = 13;
			this.md5Label.Text = "MD5";
			// 
			// crcLabel
			// 
			this.crcLabel.AutoSize = true;
			this.crcLabel.Location = new System.Drawing.Point(3, 477);
			this.crcLabel.Name = "crcLabel";
			this.crcLabel.Size = new System.Drawing.Size(41, 13);
			this.crcLabel.TabIndex = 12;
			this.crcLabel.Text = "CRC32";
			// 
			// gameSystemBox
			// 
			this.gameSystemBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.gameSystemBox.FormattingEnabled = true;
			this.gameSystemBox.Location = new System.Drawing.Point(80, 4);
			this.gameSystemBox.Name = "gameSystemBox";
			this.gameSystemBox.Size = new System.Drawing.Size(121, 21);
			this.gameSystemBox.TabIndex = 10;
			// 
			// systemLabel
			// 
			this.systemLabel.AutoSize = true;
			this.systemLabel.Location = new System.Drawing.Point(3, 3);
			this.systemLabel.Name = "systemLabel";
			this.systemLabel.Size = new System.Drawing.Size(41, 13);
			this.systemLabel.TabIndex = 11;
			this.systemLabel.Text = "System";
			// 
			// nameBox
			// 
			this.nameBox.Location = new System.Drawing.Point(80, 27);
			this.nameBox.Name = "nameBox";
			this.nameBox.Size = new System.Drawing.Size(233, 20);
			this.nameBox.TabIndex = 11;
			// 
			// nameLabel
			// 
			this.nameLabel.AutoSize = true;
			this.nameLabel.Location = new System.Drawing.Point(3, 27);
			this.nameLabel.Name = "nameLabel";
			this.nameLabel.Size = new System.Drawing.Size(35, 13);
			this.nameLabel.TabIndex = 0;
			this.nameLabel.Text = "Name";
			// 
			// mainMenuStrip
			// 
			this.mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.databaseToolStripMenuItem});
			this.mainMenuStrip.Location = new System.Drawing.Point(0, 0);
			this.mainMenuStrip.Name = "mainMenuStrip";
			this.mainMenuStrip.Size = new System.Drawing.Size(963, 24);
			this.mainMenuStrip.TabIndex = 2;
			this.mainMenuStrip.Text = "menuStrip2";
			// 
			// databaseToolStripMenuItem
			// 
			this.databaseToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.directoryScanToolStripMenuItem,
            this.cleanupDBToolStripMenuItem,
            this.exportGameDBToolStripMenuItem});
			this.databaseToolStripMenuItem.Name = "databaseToolStripMenuItem";
			this.databaseToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
			this.databaseToolStripMenuItem.Text = "Database";
			// 
			// directoryScanToolStripMenuItem
			// 
			this.directoryScanToolStripMenuItem.Name = "directoryScanToolStripMenuItem";
			this.directoryScanToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
			this.directoryScanToolStripMenuItem.Text = "Directory Scan";
			this.directoryScanToolStripMenuItem.Click += new System.EventHandler(this.directoryScanToolStripMenuItem_Click);
			// 
			// cleanupDBToolStripMenuItem
			// 
			this.cleanupDBToolStripMenuItem.Name = "cleanupDBToolStripMenuItem";
			this.cleanupDBToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
			this.cleanupDBToolStripMenuItem.Text = "Cleanup DB";
			this.cleanupDBToolStripMenuItem.Click += new System.EventHandler(this.cleanupDBToolStripMenuItem_Click);
			// 
			// exportGameDBToolStripMenuItem
			// 
			this.exportGameDBToolStripMenuItem.Name = "exportGameDBToolStripMenuItem";
			this.exportGameDBToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
			this.exportGameDBToolStripMenuItem.Text = "Export GameDB";
			this.exportGameDBToolStripMenuItem.Click += new System.EventHandler(this.exportGameDBToolStripMenuItem_Click);
			// 
			// deleteButton
			// 
			this.deleteButton.Location = new System.Drawing.Point(301, 589);
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(75, 23);
			this.deleteButton.TabIndex = 51;
			this.deleteButton.Text = "Delete";
			this.deleteButton.UseVisualStyleBackColor = true;
			this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
			// 
			// DBMan_MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(963, 678);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.filterPanel);
			this.Controls.Add(this.mainMenuStrip);
			this.KeyPreview = true;
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "DBMan_MainForm";
			this.Text = "Bizhawk DBMan";
			this.filterPanel.ResumeLayout(false);
			this.filterPanel.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.detailPanel.ResumeLayout(false);
			this.detailPanel.PerformLayout();
			this.mainMenuStrip.ResumeLayout(false);
			this.mainMenuStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel filterPanel;
		private System.Windows.Forms.Label whereLabel;
		private System.Windows.Forms.ComboBox systemBox;
		private System.Windows.Forms.TextBox whereBox;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ListView romListView;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.MenuStrip mainMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem databaseToolStripMenuItem;
		private System.Windows.Forms.Panel detailPanel;
		private System.Windows.Forms.ToolStripMenuItem directoryScanToolStripMenuItem;
		private System.Windows.Forms.ComboBox gameSystemBox;
		private System.Windows.Forms.Label systemLabel;
		private System.Windows.Forms.TextBox nameBox;
		private System.Windows.Forms.Label nameLabel;
		private System.Windows.Forms.ColumnHeader romListColumnHeader1;
		private System.Windows.Forms.ColumnHeader romListColumnHeader2;
		private System.Windows.Forms.ColumnHeader romListColumnHeader3;
		private System.Windows.Forms.ColumnHeader romListColumnHeader4;
		private System.Windows.Forms.ColumnHeader romListColumnHeader5;
		private System.Windows.Forms.TextBox crcBox;
		private System.Windows.Forms.Label sha1Label;
		private System.Windows.Forms.Label md5Label;
		private System.Windows.Forms.Label crcLabel;
		private System.Windows.Forms.TextBox sha1Box;
		private System.Windows.Forms.TextBox md5Box;
		private System.Windows.Forms.Label regionLabel;
		private System.Windows.Forms.TextBox regionBox;
		private System.Windows.Forms.Label versionLabel;
		private System.Windows.Forms.TextBox versionBox;
		private System.Windows.Forms.Label romMetaLabel;
		private System.Windows.Forms.TextBox romMetaBox;
		private System.Windows.Forms.Label gameMetaLabel;
		private System.Windows.Forms.TextBox gameMetaBox;
		private System.Windows.Forms.TextBox tagsBox;
		private System.Windows.Forms.Label romStatusLabel;
		private System.Windows.Forms.ComboBox romStatusBox;
		private System.Windows.Forms.Label tagsLabel;
		private System.Windows.Forms.Label developerLabel;
		private System.Windows.Forms.TextBox developerBox;
		private System.Windows.Forms.Label classificationLabel;
		private System.Windows.Forms.ComboBox classificationBox;
		private System.Windows.Forms.Label publisherLabel;
		private System.Windows.Forms.TextBox publisherBox;
		private System.Windows.Forms.Label releaseDateLabel;
		private System.Windows.Forms.TextBox releaseDateBox;
		private System.Windows.Forms.Label playersLabel;
		private System.Windows.Forms.Label catalogLabel;
		private System.Windows.Forms.TextBox catalogBox;
		private System.Windows.Forms.TextBox playersBox;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button saveButton;
		private System.Windows.Forms.ToolStripMenuItem cleanupDBToolStripMenuItem;
		private System.Windows.Forms.Label altNamesLabel;
		private System.Windows.Forms.TextBox altNamesBox;
		private System.Windows.Forms.Label notesLabel;
		private System.Windows.Forms.TextBox notesBox;
		private System.Windows.Forms.Label sizeLabel;
		private System.Windows.Forms.TextBox sizeBox;
		private System.Windows.Forms.ToolStripMenuItem exportGameDBToolStripMenuItem;
		private System.Windows.Forms.Button deleteButton;

	}
}

