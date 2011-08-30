namespace BizHawk
{
	partial class DiscoHawkDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DiscoHawkDialog));
			this.btnAddDisc = new System.Windows.Forms.Button();
			this.lvDiscs = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.lblSize = new System.Windows.Forms.Label();
			this.lblTracks = new System.Windows.Forms.Label();
			this.lblSessions = new System.Windows.Forms.Label();
			this.lblSectors = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.txtCuePreview = new System.Windows.Forms.TextBox();
			this.label5 = new System.Windows.Forms.Label();
			this.checkCueProp_OneBlobPerTrack = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.label7 = new System.Windows.Forms.Label();
			this.checkCueProp_Annotations = new System.Windows.Forms.CheckBox();
			this.panel3 = new System.Windows.Forms.Panel();
			this.btnPresetCanonical = new System.Windows.Forms.Button();
			this.btnPresetDaemonTools = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label9 = new System.Windows.Forms.Label();
			this.treeView1 = new System.Windows.Forms.TreeView();
			this.label10 = new System.Windows.Forms.Label();
			this.btnExportCue = new System.Windows.Forms.Button();
			this.label11 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.lblMagicDragArea = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnAddDisc
			// 
			this.btnAddDisc.Location = new System.Drawing.Point(12, 46);
			this.btnAddDisc.Name = "btnAddDisc";
			this.btnAddDisc.Size = new System.Drawing.Size(75, 23);
			this.btnAddDisc.TabIndex = 0;
			this.btnAddDisc.Text = "Add Disc";
			this.btnAddDisc.UseVisualStyleBackColor = true;
			this.btnAddDisc.Click += new System.EventHandler(this.btnAddDisc_Click);
			// 
			// lvDiscs
			// 
			this.lvDiscs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.lvDiscs.GridLines = true;
			this.lvDiscs.HideSelection = false;
			this.lvDiscs.Location = new System.Drawing.Point(12, 80);
			this.lvDiscs.Name = "lvDiscs";
			this.lvDiscs.Size = new System.Drawing.Size(248, 165);
			this.lvDiscs.TabIndex = 1;
			this.lvDiscs.UseCompatibleStateImageBehavior = false;
			this.lvDiscs.View = System.Windows.Forms.View.Details;
			this.lvDiscs.SelectedIndexChanged += new System.EventHandler(this.lvDiscs_SelectedIndexChanged);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			this.columnHeader1.Width = 240;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.tableLayoutPanel1);
			this.groupBox1.Location = new System.Drawing.Point(21, 251);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.groupBox1.Size = new System.Drawing.Size(203, 107);
			this.groupBox1.TabIndex = 2;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Quick Info";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.lblSize, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.lblTracks, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.lblSessions, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.lblSectors, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label4, 0, 3);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 19);
			this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 5;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(184, 79);
			this.tableLayoutPanel1.TabIndex = 3;
			// 
			// lblSize
			// 
			this.lblSize.AutoSize = true;
			this.lblSize.Location = new System.Drawing.Point(61, 39);
			this.lblSize.Name = "lblSize";
			this.lblSize.Size = new System.Drawing.Size(30, 13);
			this.lblSize.TabIndex = 7;
			this.lblSize.Text = "Size:";
			// 
			// lblTracks
			// 
			this.lblTracks.AutoSize = true;
			this.lblTracks.Location = new System.Drawing.Point(61, 13);
			this.lblTracks.Name = "lblTracks";
			this.lblTracks.Size = new System.Drawing.Size(30, 13);
			this.lblTracks.TabIndex = 6;
			this.lblTracks.Text = "Size:";
			// 
			// lblSessions
			// 
			this.lblSessions.AutoSize = true;
			this.lblSessions.Location = new System.Drawing.Point(61, 0);
			this.lblSessions.Name = "lblSessions";
			this.lblSessions.Size = new System.Drawing.Size(30, 13);
			this.lblSessions.TabIndex = 5;
			this.lblSessions.Text = "Size:";
			// 
			// lblSectors
			// 
			this.lblSectors.AutoSize = true;
			this.lblSectors.Location = new System.Drawing.Point(61, 26);
			this.lblSectors.Name = "lblSectors";
			this.lblSectors.Size = new System.Drawing.Size(30, 13);
			this.lblSectors.TabIndex = 4;
			this.lblSectors.Text = "Size:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 0);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(52, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "Sessions:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 26);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(46, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Sectors:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(43, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Tracks:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(3, 39);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(30, 13);
			this.label4.TabIndex = 3;
			this.label4.Text = "Size:";
			// 
			// txtCuePreview
			// 
			this.txtCuePreview.Font = new System.Drawing.Font("Courier New", 8F);
			this.txtCuePreview.Location = new System.Drawing.Point(284, 25);
			this.txtCuePreview.Multiline = true;
			this.txtCuePreview.Name = "txtCuePreview";
			this.txtCuePreview.ReadOnly = true;
			this.txtCuePreview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtCuePreview.Size = new System.Drawing.Size(375, 571);
			this.txtCuePreview.TabIndex = 3;
			this.txtCuePreview.WordWrap = false;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(281, 6);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(102, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "Output Cue Preview";
			// 
			// checkCueProp_OneBlobPerTrack
			// 
			this.checkCueProp_OneBlobPerTrack.AutoSize = true;
			this.checkCueProp_OneBlobPerTrack.Location = new System.Drawing.Point(7, 6);
			this.checkCueProp_OneBlobPerTrack.Name = "checkCueProp_OneBlobPerTrack";
			this.checkCueProp_OneBlobPerTrack.Size = new System.Drawing.Size(111, 17);
			this.checkCueProp_OneBlobPerTrack.TabIndex = 5;
			this.checkCueProp_OneBlobPerTrack.Text = "OneBlobPerTrack";
			this.checkCueProp_OneBlobPerTrack.UseVisualStyleBackColor = true;
			this.checkCueProp_OneBlobPerTrack.CheckedChanged += new System.EventHandler(this.checkCueProp_CheckedChanged);
			// 
			// label6
			// 
			this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label6.Location = new System.Drawing.Point(4, 26);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(196, 42);
			this.label6.TabIndex = 6;
			this.label6.Text = "Should the output be split into several blobs, or just use one?";
			// 
			// panel1
			// 
			this.panel1.AutoSize = true;
			this.panel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel1.Controls.Add(this.label6);
			this.panel1.Controls.Add(this.checkCueProp_OneBlobPerTrack);
			this.panel1.Location = new System.Drawing.Point(15, 19);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(203, 68);
			this.panel1.TabIndex = 9;
			// 
			// panel2
			// 
			this.panel2.AutoSize = true;
			this.panel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel2.Controls.Add(this.label7);
			this.panel2.Controls.Add(this.checkCueProp_Annotations);
			this.panel2.Location = new System.Drawing.Point(224, 19);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(154, 68);
			this.panel2.TabIndex = 10;
			// 
			// label7
			// 
			this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label7.Location = new System.Drawing.Point(4, 26);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(147, 42);
			this.label7.TabIndex = 6;
			this.label7.Text = "Annotate cue with non-standard comments";
			// 
			// checkCueProp_Annotations
			// 
			this.checkCueProp_Annotations.AutoSize = true;
			this.checkCueProp_Annotations.Location = new System.Drawing.Point(7, 6);
			this.checkCueProp_Annotations.Name = "checkCueProp_Annotations";
			this.checkCueProp_Annotations.Size = new System.Drawing.Size(82, 17);
			this.checkCueProp_Annotations.TabIndex = 5;
			this.checkCueProp_Annotations.Text = "Annotations";
			this.checkCueProp_Annotations.UseVisualStyleBackColor = true;
			this.checkCueProp_Annotations.CheckedChanged += new System.EventHandler(this.checkCueProp_CheckedChanged);
			// 
			// panel3
			// 
			this.panel3.AutoSize = true;
			this.panel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel3.Location = new System.Drawing.Point(15, 94);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(0, 0);
			this.panel3.TabIndex = 10;
			// 
			// btnPresetCanonical
			// 
			this.btnPresetCanonical.Location = new System.Drawing.Point(720, 210);
			this.btnPresetCanonical.Name = "btnPresetCanonical";
			this.btnPresetCanonical.Size = new System.Drawing.Size(114, 23);
			this.btnPresetCanonical.TabIndex = 11;
			this.btnPresetCanonical.Text = "BizHawk Canonical";
			this.btnPresetCanonical.UseVisualStyleBackColor = true;
			this.btnPresetCanonical.Click += new System.EventHandler(this.btnPresetCanonical_Click);
			// 
			// btnPresetDaemonTools
			// 
			this.btnPresetDaemonTools.Location = new System.Drawing.Point(840, 210);
			this.btnPresetDaemonTools.Name = "btnPresetDaemonTools";
			this.btnPresetDaemonTools.Size = new System.Drawing.Size(115, 23);
			this.btnPresetDaemonTools.TabIndex = 12;
			this.btnPresetDaemonTools.Text = "Daemon Tools";
			this.btnPresetDaemonTools.UseVisualStyleBackColor = true;
			this.btnPresetDaemonTools.Click += new System.EventHandler(this.btnPresetDaemonTools_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.panel1);
			this.groupBox2.Controls.Add(this.panel2);
			this.groupBox2.Controls.Add(this.panel3);
			this.groupBox2.Location = new System.Drawing.Point(665, 25);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(394, 179);
			this.groupBox2.TabIndex = 13;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Cue Export Properties";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(672, 214);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(42, 13);
			this.label9.TabIndex = 14;
			this.label9.Text = "Presets";
			// 
			// treeView1
			// 
			this.treeView1.Location = new System.Drawing.Point(12, 384);
			this.treeView1.Name = "treeView1";
			this.treeView1.Size = new System.Drawing.Size(239, 225);
			this.treeView1.TabIndex = 15;
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(18, 364);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(137, 13);
			this.label10.TabIndex = 16;
			this.label10.Text = "tree/list like isobuster (TBD)";
			// 
			// btnExportCue
			// 
			this.btnExportCue.Location = new System.Drawing.Point(665, 335);
			this.btnExportCue.Name = "btnExportCue";
			this.btnExportCue.Size = new System.Drawing.Size(101, 23);
			this.btnExportCue.TabIndex = 17;
			this.btnExportCue.Text = "Export Cue+XXX";
			this.btnExportCue.UseVisualStyleBackColor = true;
			this.btnExportCue.Click += new System.EventHandler(this.btnExportCue_Click);
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(677, 251);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(297, 81);
			this.label11.TabIndex = 18;
			this.label11.Text = resources.GetString("label11.Text");
			// 
			// label12
			// 
			this.label12.Location = new System.Drawing.Point(93, 5);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(167, 67);
			this.label12.TabIndex = 19;
			this.label12.Text = "Why is this a list? Not sure.  I thought we might want to make multi-disc archive" +
				" format of our own one day. Also disc hopper demo";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(21, 620);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(522, 13);
			this.label13.TabIndex = 20;
			this.label13.Text = "Wouldnt it be cool if you could edit the disc by deleting and moving and adding t" +
				"racks and such .. yeahhhhhh";
			// 
			// lblMagicDragArea
			// 
			this.lblMagicDragArea.AllowDrop = true;
			this.lblMagicDragArea.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblMagicDragArea.Location = new System.Drawing.Point(665, 384);
			this.lblMagicDragArea.Name = "lblMagicDragArea";
			this.lblMagicDragArea.Size = new System.Drawing.Size(147, 70);
			this.lblMagicDragArea.TabIndex = 21;
			this.lblMagicDragArea.Text = "Drag a cue into the DRAG HERE FOR MAGIC area for magic";
			this.lblMagicDragArea.Click += new System.EventHandler(this.lblMagicDragArea_Click);
			this.lblMagicDragArea.DragDrop += new System.Windows.Forms.DragEventHandler(this.handleDragDrop);
			this.lblMagicDragArea.DragEnter += new System.Windows.Forms.DragEventHandler(this.handleDragEnter);
			// 
			// DiscoHawkDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1151, 645);
			this.Controls.Add(this.lblMagicDragArea);
			this.Controls.Add(this.label13);
			this.Controls.Add(this.label12);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.btnExportCue);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.treeView1);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.btnPresetDaemonTools);
			this.Controls.Add(this.btnPresetCanonical);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.txtCuePreview);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.lvDiscs);
			this.Controls.Add(this.btnAddDisc);
			this.Name = "DiscoHawkDialog";
			this.Text = "DiscoHawkDialog";
			this.groupBox1.ResumeLayout(false);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnAddDisc;
		private System.Windows.Forms.ListView lvDiscs;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label lblSectors;
		private System.Windows.Forms.Label lblSize;
		private System.Windows.Forms.Label lblTracks;
		private System.Windows.Forms.Label lblSessions;
		private System.Windows.Forms.TextBox txtCuePreview;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox checkCueProp_OneBlobPerTrack;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.CheckBox checkCueProp_Annotations;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Button btnPresetCanonical;
		private System.Windows.Forms.Button btnPresetDaemonTools;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.TreeView treeView1;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Button btnExportCue;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label lblMagicDragArea;
	}
}