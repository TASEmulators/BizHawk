namespace BizHawk.MultiClient.GBAtools
{
	partial class GBAGPUView
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
			this.listBoxWidgets = new System.Windows.Forms.ListBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label1 = new System.Windows.Forms.Label();
			this.buttonShowWidget = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.buttonRefresh = new System.Windows.Forms.Button();
			this.hScrollBar1 = new System.Windows.Forms.HScrollBar();
			this.radioButtonManual = new System.Windows.Forms.RadioButton();
			this.radioButtonScanline = new System.Windows.Forms.RadioButton();
			this.radioButtonFrame = new System.Windows.Forms.RadioButton();
			this.labelClipboard = new System.Windows.Forms.Label();
			this.timerMessage = new System.Windows.Forms.Timer(this.components);
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// listBoxWidgets
			// 
			this.listBoxWidgets.Location = new System.Drawing.Point(12, 25);
			this.listBoxWidgets.Name = "listBoxWidgets";
			this.listBoxWidgets.Size = new System.Drawing.Size(137, 160);
			this.listBoxWidgets.TabIndex = 0;
			this.listBoxWidgets.DoubleClick += new System.EventHandler(this.listBoxWidgets_DoubleClick);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.AutoScroll = true;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panel1.Location = new System.Drawing.Point(155, 0);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(481, 405);
			this.panel1.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(92, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Available widgets:";
			// 
			// buttonShowWidget
			// 
			this.buttonShowWidget.Location = new System.Drawing.Point(29, 191);
			this.buttonShowWidget.Name = "buttonShowWidget";
			this.buttonShowWidget.Size = new System.Drawing.Size(75, 23);
			this.buttonShowWidget.TabIndex = 3;
			this.buttonShowWidget.Text = "Show >>";
			this.buttonShowWidget.UseVisualStyleBackColor = true;
			this.buttonShowWidget.Click += new System.EventHandler(this.buttonShowWidget_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.buttonRefresh);
			this.groupBox1.Controls.Add(this.hScrollBar1);
			this.groupBox1.Controls.Add(this.radioButtonManual);
			this.groupBox1.Controls.Add(this.radioButtonScanline);
			this.groupBox1.Controls.Add(this.radioButtonFrame);
			this.groupBox1.Location = new System.Drawing.Point(15, 220);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(134, 133);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Refresh";
			// 
			// buttonRefresh
			// 
			this.buttonRefresh.Location = new System.Drawing.Point(6, 104);
			this.buttonRefresh.Name = "buttonRefresh";
			this.buttonRefresh.Size = new System.Drawing.Size(75, 23);
			this.buttonRefresh.TabIndex = 4;
			this.buttonRefresh.Text = "Refresh";
			this.buttonRefresh.UseVisualStyleBackColor = true;
			this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
			// 
			// hScrollBar1
			// 
			this.hScrollBar1.LargeChange = 20;
			this.hScrollBar1.Location = new System.Drawing.Point(3, 62);
			this.hScrollBar1.Maximum = 246;
			this.hScrollBar1.Name = "hScrollBar1";
			this.hScrollBar1.Size = new System.Drawing.Size(128, 16);
			this.hScrollBar1.TabIndex = 3;
			this.hScrollBar1.ValueChanged += new System.EventHandler(this.hScrollBar1_ValueChanged);
			// 
			// radioButtonManual
			// 
			this.radioButtonManual.AutoSize = true;
			this.radioButtonManual.Location = new System.Drawing.Point(6, 81);
			this.radioButtonManual.Name = "radioButtonManual";
			this.radioButtonManual.Size = new System.Drawing.Size(60, 17);
			this.radioButtonManual.TabIndex = 2;
			this.radioButtonManual.TabStop = true;
			this.radioButtonManual.Text = "Manual";
			this.radioButtonManual.UseVisualStyleBackColor = true;
			this.radioButtonManual.CheckedChanged += new System.EventHandler(this.radioButtonManual_CheckedChanged);
			// 
			// radioButtonScanline
			// 
			this.radioButtonScanline.AutoSize = true;
			this.radioButtonScanline.Location = new System.Drawing.Point(6, 42);
			this.radioButtonScanline.Name = "radioButtonScanline";
			this.radioButtonScanline.Size = new System.Drawing.Size(66, 17);
			this.radioButtonScanline.TabIndex = 1;
			this.radioButtonScanline.Text = "Scanline";
			this.radioButtonScanline.UseVisualStyleBackColor = true;
			this.radioButtonScanline.CheckedChanged += new System.EventHandler(this.radioButtonScanline_CheckedChanged);
			// 
			// radioButtonFrame
			// 
			this.radioButtonFrame.AutoSize = true;
			this.radioButtonFrame.Location = new System.Drawing.Point(6, 19);
			this.radioButtonFrame.Name = "radioButtonFrame";
			this.radioButtonFrame.Size = new System.Drawing.Size(54, 17);
			this.radioButtonFrame.TabIndex = 0;
			this.radioButtonFrame.Text = "Frame";
			this.radioButtonFrame.UseVisualStyleBackColor = true;
			this.radioButtonFrame.CheckedChanged += new System.EventHandler(this.radioButtonFrame_CheckedChanged);
			// 
			// labelClipboard
			// 
			this.labelClipboard.AutoSize = true;
			this.labelClipboard.Location = new System.Drawing.Point(9, 356);
			this.labelClipboard.MaximumSize = new System.Drawing.Size(145, 0);
			this.labelClipboard.Name = "labelClipboard";
			this.labelClipboard.Size = new System.Drawing.Size(117, 26);
			this.labelClipboard.TabIndex = 5;
			this.labelClipboard.Text = "CTRL + C: Copy under mouse to clipboard.";
			// 
			// timerMessage
			// 
			this.timerMessage.Interval = 5000;
			this.timerMessage.Tick += new System.EventHandler(this.timerMessage_Tick);
			// 
			// GBAGPUView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(636, 405);
			this.Controls.Add(this.labelClipboard);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.buttonShowWidget);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.listBoxWidgets);
			this.KeyPreview = true;
			this.Name = "GBAGPUView";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "GBA GPU Viewer";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GBAGPUView_FormClosed);
			this.Load += new System.EventHandler(this.GBAGPUView_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GBAGPUView_KeyDown);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxWidgets;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonShowWidget;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Button buttonRefresh;
		private System.Windows.Forms.HScrollBar hScrollBar1;
		private System.Windows.Forms.RadioButton radioButtonManual;
		private System.Windows.Forms.RadioButton radioButtonScanline;
		private System.Windows.Forms.RadioButton radioButtonFrame;
		private System.Windows.Forms.Label labelClipboard;
		private System.Windows.Forms.Timer timerMessage;

	}
}