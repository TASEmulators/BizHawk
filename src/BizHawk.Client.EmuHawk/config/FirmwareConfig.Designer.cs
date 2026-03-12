using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
    partial class FirmwareConfig
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FirmwareConfig));
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.lvFirmware = new System.Windows.Forms.ListView();
			this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader8 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader7 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.lvFirmwareContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.tsmiSetCustomization = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.tsmiClearCustomization = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.tsmiInfo = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.tsmiCopy = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
			this.panel1 = new System.Windows.Forms.Panel();
			this.toolStrip1 = new BizHawk.WinForms.Controls.ToolStripEx();
			this.tbbGroup = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.tbbScan = new System.Windows.Forms.ToolStripButton();
			this.tbbOrganize = new System.Windows.Forms.ToolStripButton();
			this.tbbImport = new System.Windows.Forms.ToolStripButton();
			this.tbbClose = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
			this.tbbCloseReload = new System.Windows.Forms.ToolStripButton();
			this.tbbOpenFolder = new System.Windows.Forms.ToolStripButton();
			this._cbAllowImport = new System.Windows.Forms.ToolStripButton();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.linkBasePath = new System.Windows.Forms.Label();
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.lvFirmwareContextMenuStrip.SuspendLayout();
			this.panel1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// imageList1
			// 
			this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
			this.imageList1.ImageSize = new System.Drawing.Size(16, 16);
			this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
			// 
			// lvFirmware
			// 
			this.lvFirmware.AllowDrop = true;
			this.lvFirmware.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader5,
            this.columnHeader1,
            this.columnHeader6,
            this.columnHeader4,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader8,
            this.columnHeader7});
			this.lvFirmware.ContextMenuStrip = this.lvFirmwareContextMenuStrip;
			this.lvFirmware.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvFirmware.FullRowSelect = true;
			this.lvFirmware.GridLines = true;
			this.lvFirmware.HideSelection = false;
			this.lvFirmware.Location = new System.Drawing.Point(0, 25);
			this.lvFirmware.Name = "lvFirmware";
			this.lvFirmware.ShowItemToolTips = true;
			this.lvFirmware.Size = new System.Drawing.Size(824, 404);
			this.lvFirmware.SmallImageList = this.imageList1;
			this.lvFirmware.TabIndex = 24;
			this.lvFirmware.UseCompatibleStateImageBehavior = false;
			this.lvFirmware.View = System.Windows.Forms.View.Details;
			this.lvFirmware.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.LvFirmware_ColumnClick);
			this.lvFirmware.DragDrop += new System.Windows.Forms.DragEventHandler(this.LvFirmware_DragDrop);
			this.lvFirmware.DragEnter += new System.Windows.Forms.DragEventHandler(this.LvFirmware_DragEnter);
			this.lvFirmware.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LvFirmware_KeyDown);
			this.lvFirmware.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LvFirmware_MouseClick);
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
			// columnHeader6
			// 
			this.columnHeader6.Text = "Id";
			// 
			// columnHeader4
			// 
			this.columnHeader4.Text = "Description";
			this.columnHeader4.Width = 165;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Resolved With";
			this.columnHeader2.Width = 116;
			// 
			// columnHeader3
			// 
			this.columnHeader3.Text = "Location";
			this.columnHeader3.Width = 252;
			// 
			// columnHeader8
			// 
			this.columnHeader8.Text = "Size";
			// 
			// columnHeader7
			// 
			this.columnHeader7.Text = "Hash";
			this.columnHeader7.Width = 250;
			// 
			// lvFirmwareContextMenuStrip
			// 
			this.lvFirmwareContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiSetCustomization,
            this.tsmiClearCustomization,
            this.tsmiInfo,
            this.tsmiCopy});
			this.lvFirmwareContextMenuStrip.Name = "lvFirmwareContextMenuStrip";
			this.lvFirmwareContextMenuStrip.Size = new System.Drawing.Size(170, 92);
			this.lvFirmwareContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.LvFirmwareContextMenuStrip_Opening);
			// 
			// tsmiSetCustomization
			// 
			this.tsmiSetCustomization.Text = "&Set Customization";
			this.tsmiSetCustomization.Click += new System.EventHandler(this.TsmiSetCustomization_Click);
			// 
			// tsmiClearCustomization
			// 
			this.tsmiClearCustomization.Text = "C&lear Customization";
			this.tsmiClearCustomization.Click += new System.EventHandler(this.TsmiClearCustomization_Click);
			// 
			// tsmiInfo
			// 
			this.tsmiInfo.Text = "&Info";
			this.tsmiInfo.Click += new System.EventHandler(this.TsmiInfo_Click);
			// 
			// tsmiCopy
			// 
			this.tsmiCopy.Text = "&Copy";
			this.tsmiCopy.Click += new System.EventHandler(this.TsmiCopy_Click);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.lvFirmware);
			this.panel1.Controls.Add(this.toolStrip1);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 23);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(824, 429);
			this.panel1.TabIndex = 24;
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbGroup,
            this.toolStripSeparator2,
            this.tbbScan,
            this.tbbOrganize,
            this.tbbImport,
            this.tbbClose,
            this.toolStripSeparator1,
            this.tbbCloseReload,
            this.tbbOpenFolder,
            this._cbAllowImport});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.TabIndex = 23;
			// 
			// tbbGroup
			// 
			this.tbbGroup.Checked = true;
			this.tbbGroup.CheckOnClick = true;
			this.tbbGroup.CheckState = System.Windows.Forms.CheckState.Checked;
			this.tbbGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbGroup.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbGroup.Name = "tbbGroup";
			this.tbbGroup.Size = new System.Drawing.Size(40, 22);
			this.tbbGroup.Text = "Group";
			this.tbbGroup.Click += new System.EventHandler(this.TbbGroup_Click);
			// 
			// tbbScan
			// 
			this.tbbScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbScan.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbScan.Name = "tbbScan";
			this.tbbScan.Size = new System.Drawing.Size(34, 22);
			this.tbbScan.Text = "Scan";
			this.tbbScan.Click += new System.EventHandler(this.TbbScan_Click);
			// 
			// tbbOrganize
			// 
			this.tbbOrganize.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbOrganize.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbOrganize.Name = "tbbOrganize";
			this.tbbOrganize.Size = new System.Drawing.Size(54, 22);
			this.tbbOrganize.Text = "Organize";
			this.tbbOrganize.Click += new System.EventHandler(this.TbbOrganize_Click);
			// 
			// tbbImport
			// 
			this.tbbImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbImport.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbImport.Name = "tbbImport";
			this.tbbImport.Size = new System.Drawing.Size(43, 22);
			this.tbbImport.Text = "Import";
			this.tbbImport.Click += new System.EventHandler(this.TbbImport_Click);
			// 
			// tbbClose
			// 
			this.tbbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.tbbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbClose.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbClose.Margin = new System.Windows.Forms.Padding(0, 1, 2, 2);
			this.tbbClose.Name = "tbbClose";
			this.tbbClose.Size = new System.Drawing.Size(37, 22);
			this.tbbClose.Text = "Close";
			this.tbbClose.Click += new System.EventHandler(this.TbbClose_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripSeparator1.Visible = false;
			// 
			// tbbCloseReload
			// 
			this.tbbCloseReload.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.tbbCloseReload.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbCloseReload.Enabled = false;
			this.tbbCloseReload.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbCloseReload.Name = "tbbCloseReload";
			this.tbbCloseReload.Size = new System.Drawing.Size(117, 22);
			this.tbbCloseReload.Text = "Close and reload ROM";
			this.tbbCloseReload.ToolTipText = "Close and reload ROM";
			this.tbbCloseReload.Visible = false;
			this.tbbCloseReload.Click += new System.EventHandler(this.TbbCloseReload_Click);
			// 
			// tbbOpenFolder
			// 
			this.tbbOpenFolder.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbOpenFolder.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbOpenFolder.Name = "tbbOpenFolder";
			this.tbbOpenFolder.Size = new System.Drawing.Size(117, 22);
			this.tbbOpenFolder.Text = "Open Firmware Folder";
			this.tbbOpenFolder.Click += new System.EventHandler(this.TbbOpenFolder_Click);
			// 
			// _cbAllowImport
			// 
			this._cbAllowImport.CheckOnClick = true;
			this._cbAllowImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this._cbAllowImport.Image = ((System.Drawing.Image)(resources.GetObject("_cbAllowImport.Image")));
			this._cbAllowImport.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._cbAllowImport.Name = "_cbAllowImport";
			this._cbAllowImport.Size = new System.Drawing.Size(169, 22);
			this._cbAllowImport.Text = "Allow Importing of Unknown Files";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(830, 478);
			this.tableLayoutPanel1.TabIndex = 25;
			// 
			// panel2
			// 
			this.panel2.AutoSize = true;
			this.panel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel2.Controls.Add(this.linkBasePath);
			this.panel2.Controls.Add(this.label1);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(3, 458);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(824, 17);
			this.panel2.TabIndex = 26;
			// 
			// linkBasePath
			// 
			this.linkBasePath.AutoSize = true;
			this.linkBasePath.Location = new System.Drawing.Point(295, 0);
			this.linkBasePath.Name = "linkBasePath";
			this.linkBasePath.Size = new System.Drawing.Size(55, 13);
			this.linkBasePath.TabIndex = 27;
			this.linkBasePath.TabStop = true;
			this.linkBasePath.Text = "linkLabel1";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(3, 0);
			this.label1.Name = "label1";
			this.label1.Text = "Scan will look under (change in Config > Paths... > Global):";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(5, 5);
			this.label2.Margin = new System.Windows.Forms.Padding(5, 5, 3, 0);
			this.label2.Name = "label2";
			this.label2.Text = "Firmware such as BIOS files are copyrighted material and not provided by BizHawk";
			// 
			// FirmwareConfig
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(830, 478);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Name = "FirmwareConfig";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Firmware";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FirmwareConfig_FormClosed);
			this.Load += new System.EventHandler(this.FirmwareConfig_Load);
			this.lvFirmwareContextMenuStrip.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

				private System.Windows.Forms.ImageList imageList1;
				private System.Windows.Forms.ListView lvFirmware;
				private System.Windows.Forms.ColumnHeader columnHeader5;
				private System.Windows.Forms.ColumnHeader columnHeader1;
				private System.Windows.Forms.ColumnHeader columnHeader4;
				private System.Windows.Forms.Panel panel1;
				private ToolStripEx toolStrip1;
				private System.Windows.Forms.ToolStripButton tbbGroup;
				private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator2;
				private System.Windows.Forms.ToolStripButton tbbScan;
				private System.Windows.Forms.ToolStripButton tbbOrganize;
				private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
				private System.Windows.Forms.ColumnHeader columnHeader6;
				private System.Windows.Forms.ColumnHeader columnHeader2;
				private System.Windows.Forms.ToolTip toolTip1;
				private System.Windows.Forms.ColumnHeader columnHeader3;
				private System.Windows.Forms.ColumnHeader columnHeader7;
				private System.Windows.Forms.ContextMenuStrip lvFirmwareContextMenuStrip;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx tsmiSetCustomization;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx tsmiClearCustomization;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx tsmiInfo;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx tsmiCopy;
				private System.Windows.Forms.Panel panel2;
				private System.Windows.Forms.Label linkBasePath;
				private BizHawk.WinForms.Controls.LocLabelEx label1;
				private System.Windows.Forms.ToolStripButton tbbImport;
				private System.Windows.Forms.ColumnHeader columnHeader8;
				private System.Windows.Forms.ToolStripButton tbbClose;
				private System.Windows.Forms.ToolStripButton tbbCloseReload;
				private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
        private System.Windows.Forms.ToolStripButton tbbOpenFolder;
		private System.Windows.Forms.ToolStripButton _cbAllowImport;
	}
}