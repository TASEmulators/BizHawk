namespace BizHawk.Client.EmuHawk
{
	partial class FirmwareConfigInfo
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FirmwareConfigInfo));
            this.lvOptions = new System.Windows.Forms.ListView();
            this.colSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colHash = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStandardFilename = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDescription = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colInfo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnClose = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.lblFirmware = new BizHawk.WinForms.Controls.LocLabelEx();
            this.lvmiOptionsContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiOptionsCopy = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.lvmiOptionsContextMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // lvOptions
            // 
            resources.ApplyResources(this.lvOptions, "lvOptions");
            this.lvOptions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSize,
            this.colHash,
            this.colStandardFilename,
            this.colDescription,
            this.colInfo});
            this.lvOptions.FullRowSelect = true;
            this.lvOptions.GridLines = true;
            this.lvOptions.HideSelection = false;
            this.lvOptions.Name = "lvOptions";
            this.lvOptions.ShowItemToolTips = true;
            this.lvOptions.SmallImageList = this.imageList1;
            this.lvOptions.UseCompatibleStateImageBehavior = false;
            this.lvOptions.View = System.Windows.Forms.View.Details;
            this.lvOptions.KeyDown += new System.Windows.Forms.KeyEventHandler(this.LvOptions_KeyDown);
            this.lvOptions.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LvOptions_MouseClick);
            // 
            // colSize
            // 
            resources.ApplyResources(this.colSize, "colSize");
            // 
            // colHash
            // 
            resources.ApplyResources(this.colHash, "colHash");
            // 
            // colStandardFilename
            // 
            resources.ApplyResources(this.colStandardFilename, "colStandardFilename");
            // 
            // colDescription
            // 
            resources.ApplyResources(this.colDescription, "colDescription");
            // 
            // colInfo
            // 
            resources.ApplyResources(this.colInfo, "colInfo");
            // 
            // imageList1
            // 
            this.imageList1.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            resources.ApplyResources(this.imageList1, "imageList1");
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lvOptions, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.btnClose, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // btnClose
            // 
            resources.ApplyResources(this.btnClose, "btnClose");
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Name = "btnClose";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
            this.flowLayoutPanel1.Controls.Add(this.label2);
            this.flowLayoutPanel1.Controls.Add(this.lblFirmware);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // lblFirmware
            // 
            resources.ApplyResources(this.lblFirmware, "lblFirmware");
            this.lblFirmware.Name = "lblFirmware";
            // 
            // lvmiOptionsContextMenuStrip
            // 
            resources.ApplyResources(this.lvmiOptionsContextMenuStrip, "lvmiOptionsContextMenuStrip");
            this.lvmiOptionsContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOptionsCopy});
            this.lvmiOptionsContextMenuStrip.Name = "lvmiOptionsContextMenuStrip";
            // 
            // tsmiOptionsCopy
            // 
            resources.ApplyResources(this.tsmiOptionsCopy, "tsmiOptionsCopy");
            this.tsmiOptionsCopy.Click += new System.EventHandler(this.TsmiOptionsCopy_Click);
            // 
            // FirmwareConfigInfo
            // 
            this.AcceptButton = this.btnClose;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FirmwareConfigInfo";
            this.ShowIcon = false;
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.lvmiOptionsContextMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ColumnHeader colHash;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.ColumnHeader colStandardFilename;
		private System.Windows.Forms.ColumnHeader colDescription;
		public System.Windows.Forms.ListView lvOptions;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		public BizHawk.WinForms.Controls.LocLabelEx lblFirmware;
		private System.Windows.Forms.ContextMenuStrip lvmiOptionsContextMenuStrip;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx tsmiOptionsCopy;
		private System.Windows.Forms.ColumnHeader colInfo;
		private System.Windows.Forms.ImageList imageList1;
		private System.Windows.Forms.ColumnHeader colSize;
		private System.Windows.Forms.ToolTip toolTip1;
	}
}