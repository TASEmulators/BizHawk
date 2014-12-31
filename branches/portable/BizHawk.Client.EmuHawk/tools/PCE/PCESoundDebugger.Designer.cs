namespace BizHawk.Client.EmuHawk
{
	partial class PCESoundDebugger
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
			System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("PSG 0");
			System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("PSG 1");
			System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("PSG 2");
			System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("PSG 3");
			System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem("PSG 4");
			System.Windows.Forms.ListViewItem listViewItem6 = new System.Windows.Forms.ListViewItem("PSG 5");
			System.Windows.Forms.ListViewItem listViewItem7 = new System.Windows.Forms.ListViewItem(new string[] {
            "0",
            "-",
            "-",
            "-"}, -1);
			System.Windows.Forms.ListViewItem listViewItem8 = new System.Windows.Forms.ListViewItem(new string[] {
            "1",
            "-",
            "-",
            "-"}, -1);
			System.Windows.Forms.ListViewItem listViewItem9 = new System.Windows.Forms.ListViewItem(new string[] {
            "2",
            "-",
            "-",
            "-"}, -1);
			System.Windows.Forms.ListViewItem listViewItem10 = new System.Windows.Forms.ListViewItem(new string[] {
            "3",
            "-",
            "-",
            "-"}, -1);
			System.Windows.Forms.ListViewItem listViewItem11 = new System.Windows.Forms.ListViewItem(new string[] {
            "4",
            "-",
            "-",
            "-"}, -1);
			System.Windows.Forms.ListViewItem listViewItem12 = new System.Windows.Forms.ListViewItem(new string[] {
            "5",
            "-",
            "-",
            "-"}, -1);
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PCESoundDebugger));
			this.btnExport = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lvPsgWaveforms = new System.Windows.Forms.ListView();
			this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colHitCount = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.btnReset = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lvChEn = new System.Windows.Forms.ListView();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.lvChannels = new System.Windows.Forms.ListView();
			this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.SoundMenuStrip = new MenuStripEx();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnExport
			// 
			this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnExport.Location = new System.Drawing.Point(441, 311);
			this.btnExport.Name = "btnExport";
			this.btnExport.Size = new System.Drawing.Size(75, 23);
			this.btnExport.TabIndex = 0;
			this.btnExport.Text = "Export";
			this.btnExport.UseVisualStyleBackColor = true;
			this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.lvPsgWaveforms);
			this.groupBox1.Controls.Add(this.btnReset);
			this.groupBox1.Controls.Add(this.btnExport);
			this.groupBox1.Location = new System.Drawing.Point(12, 232);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(603, 340);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "PSG Waveform Capture";
			// 
			// lvPsgWaveforms
			// 
			this.lvPsgWaveforms.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lvPsgWaveforms.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colHitCount});
			this.lvPsgWaveforms.FullRowSelect = true;
			this.lvPsgWaveforms.LabelEdit = true;
			this.lvPsgWaveforms.Location = new System.Drawing.Point(7, 20);
			this.lvPsgWaveforms.MultiSelect = false;
			this.lvPsgWaveforms.Name = "lvPsgWaveforms";
			this.lvPsgWaveforms.Size = new System.Drawing.Size(590, 285);
			this.lvPsgWaveforms.TabIndex = 2;
			this.lvPsgWaveforms.UseCompatibleStateImageBehavior = false;
			this.lvPsgWaveforms.View = System.Windows.Forms.View.Details;
			this.lvPsgWaveforms.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvPsgWaveforms_AfterLabelEdit);
			this.lvPsgWaveforms.ItemActivate += new System.EventHandler(this.lvPsgWaveforms_ItemActivate);
			this.lvPsgWaveforms.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvPsgWaveforms_KeyDown);
			// 
			// colName
			// 
			this.colName.Text = "Name";
			this.colName.Width = 191;
			// 
			// colHitCount
			// 
			this.colHitCount.Text = "Hit Count";
			this.colHitCount.Width = 79;
			// 
			// btnReset
			// 
			this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnReset.Location = new System.Drawing.Point(522, 311);
			this.btnReset.Name = "btnReset";
			this.btnReset.Size = new System.Drawing.Size(75, 23);
			this.btnReset.TabIndex = 1;
			this.btnReset.Text = "Reset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.lvChEn);
			this.groupBox2.Location = new System.Drawing.Point(621, 51);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(165, 175);
			this.groupBox2.TabIndex = 2;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Channel Enable";
			// 
			// lvChEn
			// 
			this.lvChEn.CheckBoxes = true;
			this.lvChEn.FullRowSelect = true;
			listViewItem1.StateImageIndex = 0;
			listViewItem2.StateImageIndex = 0;
			listViewItem3.StateImageIndex = 0;
			listViewItem4.StateImageIndex = 0;
			listViewItem5.StateImageIndex = 0;
			listViewItem6.StateImageIndex = 0;
			this.lvChEn.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5,
            listViewItem6});
			this.lvChEn.Location = new System.Drawing.Point(7, 20);
			this.lvChEn.Name = "lvChEn";
			this.lvChEn.Size = new System.Drawing.Size(121, 127);
			this.lvChEn.TabIndex = 0;
			this.lvChEn.UseCompatibleStateImageBehavior = false;
			this.lvChEn.View = System.Windows.Forms.View.List;
			this.lvChEn.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvChEn_ItemChecked);
			this.lvChEn.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lvChEn_ItemSelectionChanged);
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this.lvChannels);
			this.groupBox3.Location = new System.Drawing.Point(12, 49);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(603, 177);
			this.groupBox3.TabIndex = 3;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "PSG Channels";
			// 
			// lvChannels
			// 
			this.lvChannels.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lvChannels.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader1});
			this.lvChannels.Enabled = false;
			this.lvChannels.FullRowSelect = true;
			this.lvChannels.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem7,
            listViewItem8,
            listViewItem9,
            listViewItem10,
            listViewItem11,
            listViewItem12});
			this.lvChannels.LabelEdit = true;
			this.lvChannels.Location = new System.Drawing.Point(6, 19);
			this.lvChannels.MultiSelect = false;
			this.lvChannels.Name = "lvChannels";
			this.lvChannels.Size = new System.Drawing.Size(591, 152);
			this.lvChannels.TabIndex = 3;
			this.lvChannels.UseCompatibleStateImageBehavior = false;
			this.lvChannels.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader5
			// 
			this.columnHeader5.Text = "Channel";
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Vol";
			// 
			// columnHeader4
			// 
			this.columnHeader4.Text = "Pitch";
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Status";
			this.columnHeader1.Width = 259;
			// 
			// SoundMenuStrip
			// 
			this.SoundMenuStrip.ClickThrough = true;
			this.SoundMenuStrip.Location = new System.Drawing.Point(0, 0);
			this.SoundMenuStrip.Name = "SoundMenuStrip";
			this.SoundMenuStrip.Size = new System.Drawing.Size(787, 24);
			this.SoundMenuStrip.TabIndex = 4;
			this.SoundMenuStrip.Text = "menuStrip1";
			// 
			// PCESoundDebugger
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(787, 580);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.SoundMenuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.SoundMenuStrip;
			this.Name = "PCESoundDebugger";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Sound Debugger";
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnExport;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.ListView lvPsgWaveforms;
		private System.Windows.Forms.ColumnHeader colHitCount;
		private System.Windows.Forms.ColumnHeader colName;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.ListView lvChEn;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.ListView lvChannels;
		private System.Windows.Forms.ColumnHeader columnHeader5;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		private System.Windows.Forms.ColumnHeader columnHeader4;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private MenuStripEx SoundMenuStrip;
	}
}