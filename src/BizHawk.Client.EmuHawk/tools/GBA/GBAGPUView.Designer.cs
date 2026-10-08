namespace BizHawk.Client.EmuHawk
{
	partial class GbaGpuView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GbaGpuView));
            this.listBoxWidgets = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.buttonShowWidget = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.hScrollBar1 = new System.Windows.Forms.HScrollBar();
            this.radioButtonManual = new System.Windows.Forms.RadioButton();
            this.radioButtonScanline = new System.Windows.Forms.RadioButton();
            this.labelClipboard = new BizHawk.WinForms.Controls.LocLabelEx();
            this.timerMessage = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxWidgets
            // 
            resources.ApplyResources(this.listBoxWidgets, "listBoxWidgets");
            this.listBoxWidgets.Name = "listBoxWidgets";
            this.listBoxWidgets.DoubleClick += new System.EventHandler(this.listBoxWidgets_DoubleClick);
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Name = "panel1";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // buttonShowWidget
            // 
            resources.ApplyResources(this.buttonShowWidget, "buttonShowWidget");
            this.buttonShowWidget.Name = "buttonShowWidget";
            this.buttonShowWidget.UseVisualStyleBackColor = true;
            this.buttonShowWidget.Click += new System.EventHandler(this.buttonShowWidget_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.buttonRefresh);
            this.groupBox1.Controls.Add(this.hScrollBar1);
            this.groupBox1.Controls.Add(this.radioButtonManual);
            this.groupBox1.Controls.Add(this.radioButtonScanline);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // buttonRefresh
            // 
            resources.ApplyResources(this.buttonRefresh, "buttonRefresh");
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // hScrollBar1
            // 
            resources.ApplyResources(this.hScrollBar1, "hScrollBar1");
            this.hScrollBar1.LargeChange = 20;
            this.hScrollBar1.Maximum = 246;
            this.hScrollBar1.Name = "hScrollBar1";
            this.hScrollBar1.ValueChanged += new System.EventHandler(this.hScrollBar1_ValueChanged);
            // 
            // radioButtonManual
            // 
            resources.ApplyResources(this.radioButtonManual, "radioButtonManual");
            this.radioButtonManual.Name = "radioButtonManual";
            this.radioButtonManual.TabStop = true;
            this.radioButtonManual.UseVisualStyleBackColor = true;
            this.radioButtonManual.CheckedChanged += new System.EventHandler(this.radioButtonManual_CheckedChanged);
            // 
            // radioButtonScanline
            // 
            resources.ApplyResources(this.radioButtonScanline, "radioButtonScanline");
            this.radioButtonScanline.Name = "radioButtonScanline";
            this.radioButtonScanline.UseVisualStyleBackColor = true;
            this.radioButtonScanline.CheckedChanged += new System.EventHandler(this.radioButtonScanline_CheckedChanged);
            // 
            // labelClipboard
            // 
            resources.ApplyResources(this.labelClipboard, "labelClipboard");
            this.labelClipboard.Name = "labelClipboard";
            // 
            // timerMessage
            // 
            this.timerMessage.Interval = 5000;
            this.timerMessage.Tick += new System.EventHandler(this.timerMessage_Tick);
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            // 
            // GbaGpuView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.labelClipboard);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.buttonShowWidget);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.listBoxWidgets);
            this.Controls.Add(this.menuStrip1);
            this.KeyPreview = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "GbaGpuView";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GbaGpuView_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GbaGpuView_KeyDown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxWidgets;
		private System.Windows.Forms.Panel panel1;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button buttonShowWidget;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonRefresh;
		private System.Windows.Forms.HScrollBar hScrollBar1;
		private System.Windows.Forms.RadioButton radioButtonManual;
		private System.Windows.Forms.RadioButton radioButtonScanline;
		private BizHawk.WinForms.Controls.LocLabelEx labelClipboard;
		private System.Windows.Forms.Timer timerMessage;
		private System.Windows.Forms.MenuStrip menuStrip1;

	}
}