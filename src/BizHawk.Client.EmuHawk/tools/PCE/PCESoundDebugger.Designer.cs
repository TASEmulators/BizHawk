using BizHawk.WinForms.Controls;

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
            this.SoundMenuStrip = new BizHawk.WinForms.Controls.MenuStripEx();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnExport
            // 
            resources.ApplyResources(this.btnExport, "btnExport");
            this.btnExport.Name = "btnExport";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.BtnExport_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.lvPsgWaveforms);
            this.groupBox1.Controls.Add(this.btnReset);
            this.groupBox1.Controls.Add(this.btnExport);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // lvPsgWaveforms
            // 
            resources.ApplyResources(this.lvPsgWaveforms, "lvPsgWaveforms");
            this.lvPsgWaveforms.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colHitCount});
            this.lvPsgWaveforms.FullRowSelect = true;
            this.lvPsgWaveforms.HideSelection = false;
            this.lvPsgWaveforms.LabelEdit = true;
            this.lvPsgWaveforms.MultiSelect = false;
            this.lvPsgWaveforms.Name = "lvPsgWaveforms";
            this.lvPsgWaveforms.UseCompatibleStateImageBehavior = false;
            this.lvPsgWaveforms.View = System.Windows.Forms.View.Details;
            this.lvPsgWaveforms.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.lvPsgWaveforms_AfterLabelEdit);
            this.lvPsgWaveforms.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvPsgWaveforms_KeyDown);
            // 
            // colName
            // 
            resources.ApplyResources(this.colName, "colName");
            // 
            // colHitCount
            // 
            resources.ApplyResources(this.colHitCount, "colHitCount");
            // 
            // btnReset
            // 
            resources.ApplyResources(this.btnReset, "btnReset");
            this.btnReset.Name = "btnReset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.BtnReset_Click);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.lvChEn);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // lvChEn
            // 
            resources.ApplyResources(this.lvChEn, "lvChEn");
            this.lvChEn.CheckBoxes = true;
            this.lvChEn.FullRowSelect = true;
            this.lvChEn.HideSelection = false;
            this.lvChEn.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChEn.Items"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChEn.Items1"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChEn.Items2"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChEn.Items3"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChEn.Items4"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChEn.Items5")))});
            this.lvChEn.Name = "lvChEn";
            this.lvChEn.UseCompatibleStateImageBehavior = false;
            this.lvChEn.View = System.Windows.Forms.View.List;
            this.lvChEn.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.lvChEn_ItemChecked);
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.lvChannels);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // lvChannels
            // 
            resources.ApplyResources(this.lvChannels, "lvChannels");
            this.lvChannels.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader1});
            this.lvChannels.FullRowSelect = true;
            this.lvChannels.HideSelection = false;
            this.lvChannels.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChannels.Items"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChannels.Items1"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChannels.Items2"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChannels.Items3"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChannels.Items4"))),
            ((System.Windows.Forms.ListViewItem)(resources.GetObject("lvChannels.Items5")))});
            this.lvChannels.LabelEdit = true;
            this.lvChannels.MultiSelect = false;
            this.lvChannels.Name = "lvChannels";
            this.lvChannels.UseCompatibleStateImageBehavior = false;
            this.lvChannels.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader5
            // 
            resources.ApplyResources(this.columnHeader5, "columnHeader5");
            // 
            // columnHeader3
            // 
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // columnHeader4
            // 
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // SoundMenuStrip
            // 
            resources.ApplyResources(this.SoundMenuStrip, "SoundMenuStrip");
            // 
            // PCESoundDebugger
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.SoundMenuStrip);
            this.MainMenuStrip = this.SoundMenuStrip;
            this.Name = "PCESoundDebugger";
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