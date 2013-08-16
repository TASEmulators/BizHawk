namespace BizHawk.MultiClient
{
    partial class FirmwaresConfig
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FirmwaresConfig));
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.lvFirmwares = new System.Windows.Forms.ListView();
			this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.panel1 = new System.Windows.Forms.Panel();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.tbbGroup = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.tbbScan = new System.Windows.Forms.ToolStripButton();
			this.tbbOrganize = new System.Windows.Forms.ToolStripButton();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.lvFirmwaresContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.tsmiClearCustomization = new System.Windows.Forms.ToolStripMenuItem();
			this.tsmiSetCustomization = new System.Windows.Forms.ToolStripMenuItem();
			this.panel1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.lvFirmwaresContextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// imageList1
			// 
			this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// lvFirmwares
			// 
			this.lvFirmwares.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader1,
            this.columnHeader6,
            this.columnHeader4,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader7});
			this.lvFirmwares.ContextMenuStrip = this.lvFirmwaresContextMenuStrip;
			this.lvFirmwares.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvFirmwares.FullRowSelect = true;
			this.lvFirmwares.GridLines = true;
			this.lvFirmwares.Location = new System.Drawing.Point(0, 25);
			this.lvFirmwares.Name = "lvFirmwares";
			this.lvFirmwares.Size = new System.Drawing.Size(773, 447);
			this.lvFirmwares.SmallImageList = this.imageList1;
			this.lvFirmwares.TabIndex = 24;
			this.lvFirmwares.UseCompatibleStateImageBehavior = false;
			this.lvFirmwares.View = System.Windows.Forms.View.Details;
			this.lvFirmwares.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvFirmwares_ColumnClick);
			this.lvFirmwares.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvFirmwares_KeyDown);
			this.lvFirmwares.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lvFirmwares_MouseClick);
			// 
			// columnHeader5
			// 
			this.columnHeader5.Text = "";
			this.columnHeader5.Width = 31;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "System";
			// 
			// columnHeader4
			// 
			this.columnHeader4.Text = "Description";
			this.columnHeader4.Width = 165;
			// 
			// panel1
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.panel1, 2);
			this.panel1.Controls.Add(this.lvFirmwares);
			this.panel1.Controls.Add(this.toolStrip1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 3);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(773, 472);
			this.panel1.TabIndex = 24;
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbGroup,
            this.toolStripSeparator2,
            this.tbbScan,
            this.tbbOrganize});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(773, 25);
			this.toolStrip1.TabIndex = 23;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// tbbGroup
			// 
			this.tbbGroup.Checked = true;
			this.tbbGroup.CheckOnClick = true;
			this.tbbGroup.CheckState = System.Windows.Forms.CheckState.Checked;
			this.tbbGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbGroup.Image = ((System.Drawing.Image)(resources.GetObject("tbbGroup.Image")));
			this.tbbGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbGroup.Name = "tbbGroup";
			this.tbbGroup.Size = new System.Drawing.Size(40, 22);
			this.tbbGroup.Text = "Group";
			this.tbbGroup.Click += new System.EventHandler(this.tbbGroup_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// tbbScan
			// 
			this.tbbScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbScan.Image = ((System.Drawing.Image)(resources.GetObject("tbbScan.Image")));
			this.tbbScan.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbScan.Name = "tbbScan";
			this.tbbScan.Size = new System.Drawing.Size(34, 22);
			this.tbbScan.Text = "Scan";
			this.tbbScan.Click += new System.EventHandler(this.tbbScan_Click);
			// 
			// tbbOrganize
			// 
			this.tbbOrganize.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbOrganize.Image = ((System.Drawing.Image)(resources.GetObject("tbbOrganize.Image")));
			this.tbbOrganize.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbOrganize.Name = "tbbOrganize";
			this.tbbOrganize.Size = new System.Drawing.Size(54, 22);
			this.tbbOrganize.Text = "Organize";
			this.tbbOrganize.Click += new System.EventHandler(this.tbbOrganize_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 2;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(779, 478);
			this.tableLayoutPanel1.TabIndex = 25;
			// 
			// columnHeader6
			// 
			this.columnHeader6.Text = "Id";
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Resolved With";
			this.columnHeader2.Width = 116;
			// 
			// columnHeader7
			// 
			this.columnHeader7.Text = "Hash";
			this.columnHeader7.Width = 340;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Location";
			this.columnHeader3.Width = 252;
			// 
			// lvFirmwaresContextMenuStrip
			// 
			this.lvFirmwaresContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiSetCustomization,
            this.tsmiClearCustomization});
			this.lvFirmwaresContextMenuStrip.Name = "lvFirmwaresContextMenuStrip";
			this.lvFirmwaresContextMenuStrip.Size = new System.Drawing.Size(170, 48);
			// 
			// tsmiClearCustomization
			// 
			this.tsmiClearCustomization.Name = "tsmiClearCustomization";
			this.tsmiClearCustomization.Size = new System.Drawing.Size(169, 22);
			this.tsmiClearCustomization.Text = "&Clear Customization";
			this.tsmiClearCustomization.Click += new System.EventHandler(this.tsmiClearCustomization_Click);
			// 
			// tsmiSetCustomization
			// 
			this.tsmiSetCustomization.Name = "tsmiSetCustomization";
			this.tsmiSetCustomization.Size = new System.Drawing.Size(169, 22);
			this.tsmiSetCustomization.Text = "&Set Customization";
			this.tsmiSetCustomization.Click += new System.EventHandler(this.tsmiSetCustomization_Click);
			// 
			// FirmwaresConfig
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(779, 478);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Name = "FirmwaresConfig";
			this.ShowIcon = false;
			this.Text = "Firmwares";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FirmwaresConfig_FormClosed);
			this.Load += new System.EventHandler(this.FirmwaresConfig_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.lvFirmwaresContextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

				private System.Windows.Forms.ImageList imageList1;
				private System.Windows.Forms.ListView lvFirmwares;
				private System.Windows.Forms.ColumnHeader columnHeader5;
				private System.Windows.Forms.ColumnHeader columnHeader1;
				private System.Windows.Forms.ColumnHeader columnHeader4;
				private System.Windows.Forms.Panel panel1;
				private System.Windows.Forms.ToolStrip toolStrip1;
				private System.Windows.Forms.ToolStripButton tbbGroup;
				private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
				private System.Windows.Forms.ToolStripButton tbbScan;
				private System.Windows.Forms.ToolStripButton tbbOrganize;
				private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
				private System.Windows.Forms.ColumnHeader columnHeader6;
				private System.Windows.Forms.ColumnHeader columnHeader2;
				private System.Windows.Forms.ToolTip toolTip1;
				private System.Windows.Forms.ColumnHeader columnHeader3;
				private System.Windows.Forms.ColumnHeader columnHeader7;
				private System.Windows.Forms.ContextMenuStrip lvFirmwaresContextMenuStrip;
				private System.Windows.Forms.ToolStripMenuItem tsmiSetCustomization;
				private System.Windows.Forms.ToolStripMenuItem tsmiClearCustomization;
    }
}