namespace BizHawk.Client.EmuHawk
{
	partial class VirtualPadDiscManager
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtualPadDiscManager));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lvDiscs = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnClose = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.btnOpen = new BizHawk.Client.EmuHawk.VirtualPadButton();
            this.lblTimeZero = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.lvDiscs);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // lvDiscs
            // 
            resources.ApplyResources(this.lvDiscs, "lvDiscs");
            this.lvDiscs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.lvDiscs.FullRowSelect = true;
            this.lvDiscs.GridLines = true;
            this.lvDiscs.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.lvDiscs.HideSelection = false;
            this.lvDiscs.MultiSelect = false;
            this.lvDiscs.Name = "lvDiscs";
            this.lvDiscs.UseCompatibleStateImageBehavior = false;
            this.lvDiscs.View = System.Windows.Forms.View.Details;
            this.lvDiscs.SelectedIndexChanged += new System.EventHandler(this.lvDiscs_SelectedIndexChanged);
            // 
            // columnHeader1
            // 
            resources.ApplyResources(this.columnHeader1, "columnHeader1");
            // 
            // columnHeader2
            // 
            resources.ApplyResources(this.columnHeader2, "columnHeader2");
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // btnClose
            // 
            resources.ApplyResources(this.btnClose, "btnClose");
            this.btnClose.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnClose.Name = "btnClose";
            this.btnClose.ReadOnly = false;
            this.btnClose.RightClicked = false;
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnOpen
            // 
            resources.ApplyResources(this.btnOpen, "btnOpen");
            this.btnOpen.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.ReadOnly = false;
            this.btnOpen.RightClicked = false;
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // lblTimeZero
            // 
            resources.ApplyResources(this.lblTimeZero, "lblTimeZero");
            this.lblTimeZero.Name = "lblTimeZero";
            // 
            // VirtualPadDiscManager
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Controls.Add(this.lblTimeZero);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnOpen);
            this.Name = "VirtualPadDiscManager";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private VirtualPadButton btnOpen;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ListView lvDiscs;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private VirtualPadButton btnClose;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx lblTimeZero;
	}
}
