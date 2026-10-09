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
            resources.ApplyResources(this.imageList1, "imageList1");
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // lvFirmware
            // 
            resources.ApplyResources(this.lvFirmware, "lvFirmware");
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
            this.lvFirmware.FullRowSelect = true;
            this.lvFirmware.GridLines = true;
            this.lvFirmware.HideSelection = false;
            this.lvFirmware.Name = "lvFirmware";
            this.lvFirmware.ShowItemToolTips = true;
            this.lvFirmware.SmallImageList = this.imageList1;
            this.toolTip1.SetToolTip(this.lvFirmware, resources.GetString("lvFirmware.ToolTip"));
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
            resources.ApplyResources(this.columnHeader5, "columnHeader5");
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader6
            // 
            resources.ApplyResources(this.columnHeader6, "columnHeader6");
            // 
            // columnHeader4
            // 
            resources.ApplyResources(this.columnHeader4, "columnHeader4");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // columnHeader3
            // 
            resources.ApplyResources(this.columnHeader3, "columnHeader3");
            // 
            // columnHeader8
            // 
            resources.ApplyResources(this.columnHeader8, "columnHeader8");
            // 
            // columnHeader7
            // 
            resources.ApplyResources(this.columnHeader7, "columnHeader7");
            // 
            // lvFirmwareContextMenuStrip
            // 
            resources.ApplyResources(this.lvFirmwareContextMenuStrip, "lvFirmwareContextMenuStrip");
            this.lvFirmwareContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiSetCustomization,
            this.tsmiClearCustomization,
            this.tsmiInfo,
            this.tsmiCopy});
            this.lvFirmwareContextMenuStrip.Name = "lvFirmwareContextMenuStrip";
            this.toolTip1.SetToolTip(this.lvFirmwareContextMenuStrip, resources.GetString("lvFirmwareContextMenuStrip.ToolTip"));
            this.lvFirmwareContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.LvFirmwareContextMenuStrip_Opening);
            // 
            // tsmiSetCustomization
            // 
            resources.ApplyResources(this.tsmiSetCustomization, "tsmiSetCustomization");
            this.tsmiSetCustomization.Click += new System.EventHandler(this.TsmiSetCustomization_Click);
            // 
            // tsmiClearCustomization
            // 
            resources.ApplyResources(this.tsmiClearCustomization, "tsmiClearCustomization");
            this.tsmiClearCustomization.Click += new System.EventHandler(this.TsmiClearCustomization_Click);
            // 
            // tsmiInfo
            // 
            resources.ApplyResources(this.tsmiInfo, "tsmiInfo");
            this.tsmiInfo.Click += new System.EventHandler(this.TsmiInfo_Click);
            // 
            // tsmiCopy
            // 
            resources.ApplyResources(this.tsmiCopy, "tsmiCopy");
            this.tsmiCopy.Click += new System.EventHandler(this.TsmiCopy_Click);
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Controls.Add(this.lvFirmware);
            this.panel1.Controls.Add(this.toolStrip1);
            this.panel1.Name = "panel1";
            this.toolTip1.SetToolTip(this.panel1, resources.GetString("panel1.ToolTip"));
            // 
            // toolStrip1
            // 
            resources.ApplyResources(this.toolStrip1, "toolStrip1");
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
            this.toolStrip1.Name = "toolStrip1";
            this.toolTip1.SetToolTip(this.toolStrip1, resources.GetString("toolStrip1.ToolTip"));
            // 
            // tbbGroup
            // 
            resources.ApplyResources(this.tbbGroup, "tbbGroup");
            this.tbbGroup.Checked = true;
            this.tbbGroup.CheckOnClick = true;
            this.tbbGroup.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tbbGroup.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbGroup.Name = "tbbGroup";
            this.tbbGroup.Click += new System.EventHandler(this.TbbGroup_Click);
            // 
            // toolStripSeparator2
            // 
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // tbbScan
            // 
            resources.ApplyResources(this.tbbScan, "tbbScan");
            this.tbbScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbScan.Name = "tbbScan";
            this.tbbScan.Click += new System.EventHandler(this.TbbScan_Click);
            // 
            // tbbOrganize
            // 
            resources.ApplyResources(this.tbbOrganize, "tbbOrganize");
            this.tbbOrganize.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbOrganize.Name = "tbbOrganize";
            this.tbbOrganize.Click += new System.EventHandler(this.TbbOrganize_Click);
            // 
            // tbbImport
            // 
            resources.ApplyResources(this.tbbImport, "tbbImport");
            this.tbbImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbImport.Name = "tbbImport";
            this.tbbImport.Click += new System.EventHandler(this.TbbImport_Click);
            // 
            // tbbClose
            // 
            resources.ApplyResources(this.tbbClose, "tbbClose");
            this.tbbClose.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tbbClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbClose.Margin = new System.Windows.Forms.Padding(0, 1, 2, 2);
            this.tbbClose.Name = "tbbClose";
            this.tbbClose.Click += new System.EventHandler(this.TbbClose_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            // 
            // tbbCloseReload
            // 
            resources.ApplyResources(this.tbbCloseReload, "tbbCloseReload");
            this.tbbCloseReload.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.tbbCloseReload.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbCloseReload.Name = "tbbCloseReload";
            this.tbbCloseReload.Click += new System.EventHandler(this.TbbCloseReload_Click);
            // 
            // tbbOpenFolder
            // 
            resources.ApplyResources(this.tbbOpenFolder, "tbbOpenFolder");
            this.tbbOpenFolder.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbOpenFolder.Name = "tbbOpenFolder";
            this.tbbOpenFolder.Click += new System.EventHandler(this.TbbOpenFolder_Click);
            // 
            // _cbAllowImport
            // 
            resources.ApplyResources(this._cbAllowImport, "_cbAllowImport");
            this._cbAllowImport.CheckOnClick = true;
            this._cbAllowImport.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this._cbAllowImport.Name = "_cbAllowImport";
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.toolTip1.SetToolTip(this.tableLayoutPanel1, resources.GetString("tableLayoutPanel1.ToolTip"));
            // 
            // panel2
            // 
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel2.Controls.Add(this.linkBasePath);
            this.panel2.Controls.Add(this.label1);
            this.panel2.Name = "panel2";
            this.toolTip1.SetToolTip(this.panel2, resources.GetString("panel2.ToolTip"));
            // 
            // linkBasePath
            // 
            resources.ApplyResources(this.linkBasePath, "linkBasePath");
            this.linkBasePath.Name = "linkBasePath";
            this.linkBasePath.TabStop = true;
            this.toolTip1.SetToolTip(this.linkBasePath, resources.GetString("linkBasePath.ToolTip"));
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.toolTip1.SetToolTip(this.label1, resources.GetString("label1.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // FirmwareConfig
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FirmwareConfig";
            this.ShowIcon = false;
            this.toolTip1.SetToolTip(this, resources.GetString("$this.ToolTip"));
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