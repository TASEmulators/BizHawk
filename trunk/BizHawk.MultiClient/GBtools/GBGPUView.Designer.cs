namespace BizHawk.MultiClient.GBtools
{
	partial class GBGPUView
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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.radioButtonRefreshFrame = new System.Windows.Forms.RadioButton();
			this.radioButtonRefreshScanline = new System.Windows.Forms.RadioButton();
			this.radioButtonRefreshManual = new System.Windows.Forms.RadioButton();
			this.buttonRefresh = new System.Windows.Forms.Button();
			this.labelScanline = new System.Windows.Forms.Label();
			this.hScrollBarScanline = new System.Windows.Forms.HScrollBar();
			this.bmpViewOAM = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewBGPal = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewSPPal = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewTiles1 = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewTiles2 = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewBG = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewWin = new BizHawk.MultiClient.GBtools.BmpView();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(65, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Background";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(265, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(46, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Window";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 16);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(41, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Bank 1";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(137, 16);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(72, 13);
			this.label4.TabIndex = 9;
			this.label4.Text = "Bank 2 (CGB)";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(3, 16);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(116, 13);
			this.label5.TabIndex = 12;
			this.label5.Text = "Background && Window";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(137, 16);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(34, 13);
			this.label6.TabIndex = 13;
			this.label6.Text = "Sprite";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.bmpViewBG);
			this.groupBox1.Controls.Add(this.bmpViewWin);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(530, 294);
			this.groupBox1.TabIndex = 16;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Tilemaps";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label3);
			this.groupBox2.Controls.Add(this.bmpViewTiles1);
			this.groupBox2.Controls.Add(this.bmpViewTiles2);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Location = new System.Drawing.Point(548, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(274, 230);
			this.groupBox2.TabIndex = 17;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Tiles";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.label5);
			this.groupBox3.Controls.Add(this.bmpViewBGPal);
			this.groupBox3.Controls.Add(this.bmpViewSPPal);
			this.groupBox3.Controls.Add(this.label6);
			this.groupBox3.Location = new System.Drawing.Point(548, 248);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(274, 102);
			this.groupBox3.TabIndex = 18;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Palettes";
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.bmpViewOAM);
			this.groupBox4.Location = new System.Drawing.Point(12, 312);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(332, 41);
			this.groupBox4.TabIndex = 19;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Sprites";
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.hScrollBarScanline);
			this.groupBox5.Controls.Add(this.labelScanline);
			this.groupBox5.Controls.Add(this.buttonRefresh);
			this.groupBox5.Controls.Add(this.radioButtonRefreshManual);
			this.groupBox5.Controls.Add(this.radioButtonRefreshScanline);
			this.groupBox5.Controls.Add(this.radioButtonRefreshFrame);
			this.groupBox5.Location = new System.Drawing.Point(548, 356);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(274, 94);
			this.groupBox5.TabIndex = 20;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Refresh Control";
			// 
			// radioButtonRefreshFrame
			// 
			this.radioButtonRefreshFrame.AutoSize = true;
			this.radioButtonRefreshFrame.Location = new System.Drawing.Point(7, 20);
			this.radioButtonRefreshFrame.Name = "radioButtonRefreshFrame";
			this.radioButtonRefreshFrame.Size = new System.Drawing.Size(54, 17);
			this.radioButtonRefreshFrame.TabIndex = 0;
			this.radioButtonRefreshFrame.TabStop = true;
			this.radioButtonRefreshFrame.Text = "Frame";
			this.radioButtonRefreshFrame.UseVisualStyleBackColor = true;
			this.radioButtonRefreshFrame.CheckedChanged += new System.EventHandler(this.radioButtonRefreshFrame_CheckedChanged);
			// 
			// radioButtonRefreshScanline
			// 
			this.radioButtonRefreshScanline.AutoSize = true;
			this.radioButtonRefreshScanline.Location = new System.Drawing.Point(7, 44);
			this.radioButtonRefreshScanline.Name = "radioButtonRefreshScanline";
			this.radioButtonRefreshScanline.Size = new System.Drawing.Size(66, 17);
			this.radioButtonRefreshScanline.TabIndex = 1;
			this.radioButtonRefreshScanline.TabStop = true;
			this.radioButtonRefreshScanline.Text = "Scanline";
			this.radioButtonRefreshScanline.UseVisualStyleBackColor = true;
			this.radioButtonRefreshScanline.CheckedChanged += new System.EventHandler(this.radioButtonRefreshScanline_CheckedChanged);
			// 
			// radioButtonRefreshManual
			// 
			this.radioButtonRefreshManual.AutoSize = true;
			this.radioButtonRefreshManual.Location = new System.Drawing.Point(7, 68);
			this.radioButtonRefreshManual.Name = "radioButtonRefreshManual";
			this.radioButtonRefreshManual.Size = new System.Drawing.Size(60, 17);
			this.radioButtonRefreshManual.TabIndex = 2;
			this.radioButtonRefreshManual.TabStop = true;
			this.radioButtonRefreshManual.Text = "Manual";
			this.radioButtonRefreshManual.UseVisualStyleBackColor = true;
			this.radioButtonRefreshManual.CheckedChanged += new System.EventHandler(this.radioButtonRefreshManual_CheckedChanged);
			// 
			// buttonRefresh
			// 
			this.buttonRefresh.Location = new System.Drawing.Point(76, 65);
			this.buttonRefresh.Name = "buttonRefresh";
			this.buttonRefresh.Size = new System.Drawing.Size(80, 23);
			this.buttonRefresh.TabIndex = 4;
			this.buttonRefresh.Text = "Refresh Now";
			this.buttonRefresh.UseVisualStyleBackColor = true;
			this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
			// 
			// labelScanline
			// 
			this.labelScanline.AutoSize = true;
			this.labelScanline.Location = new System.Drawing.Point(159, 24);
			this.labelScanline.Name = "labelScanline";
			this.labelScanline.Size = new System.Drawing.Size(21, 13);
			this.labelScanline.TabIndex = 5;
			this.labelScanline.Text = "SS";
			this.labelScanline.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// hScrollBarScanline
			// 
			this.hScrollBarScanline.Location = new System.Drawing.Point(76, 45);
			this.hScrollBarScanline.Maximum = 153;
			this.hScrollBarScanline.Name = "hScrollBarScanline";
			this.hScrollBarScanline.Size = new System.Drawing.Size(192, 16);
			this.hScrollBarScanline.TabIndex = 21;
			this.hScrollBarScanline.ValueChanged += new System.EventHandler(this.hScrollBarScanline_ValueChanged);
			// 
			// bmpViewOAM
			// 
			this.bmpViewOAM.BackColor = System.Drawing.Color.Black;
			this.bmpViewOAM.Location = new System.Drawing.Point(6, 19);
			this.bmpViewOAM.Name = "bmpViewOAM";
			this.bmpViewOAM.Size = new System.Drawing.Size(320, 16);
			this.bmpViewOAM.TabIndex = 14;
			// 
			// bmpViewBGPal
			// 
			this.bmpViewBGPal.BackColor = System.Drawing.Color.Black;
			this.bmpViewBGPal.Location = new System.Drawing.Point(6, 32);
			this.bmpViewBGPal.Name = "bmpViewBGPal";
			this.bmpViewBGPal.Size = new System.Drawing.Size(128, 64);
			this.bmpViewBGPal.TabIndex = 10;
			this.bmpViewBGPal.Text = "bmpView1";
			// 
			// bmpViewSPPal
			// 
			this.bmpViewSPPal.BackColor = System.Drawing.Color.Black;
			this.bmpViewSPPal.Location = new System.Drawing.Point(140, 32);
			this.bmpViewSPPal.Name = "bmpViewSPPal";
			this.bmpViewSPPal.Size = new System.Drawing.Size(128, 64);
			this.bmpViewSPPal.TabIndex = 11;
			this.bmpViewSPPal.Text = "bmpView2";
			// 
			// bmpViewTiles1
			// 
			this.bmpViewTiles1.BackColor = System.Drawing.Color.Black;
			this.bmpViewTiles1.Location = new System.Drawing.Point(6, 32);
			this.bmpViewTiles1.Name = "bmpViewTiles1";
			this.bmpViewTiles1.Size = new System.Drawing.Size(128, 192);
			this.bmpViewTiles1.TabIndex = 6;
			this.bmpViewTiles1.Text = "bmpView1";
			// 
			// bmpViewTiles2
			// 
			this.bmpViewTiles2.BackColor = System.Drawing.Color.Black;
			this.bmpViewTiles2.Location = new System.Drawing.Point(140, 32);
			this.bmpViewTiles2.Name = "bmpViewTiles2";
			this.bmpViewTiles2.Size = new System.Drawing.Size(128, 192);
			this.bmpViewTiles2.TabIndex = 7;
			this.bmpViewTiles2.Text = "bmpView2";
			// 
			// bmpViewBG
			// 
			this.bmpViewBG.BackColor = System.Drawing.Color.Black;
			this.bmpViewBG.Location = new System.Drawing.Point(6, 32);
			this.bmpViewBG.Name = "bmpViewBG";
			this.bmpViewBG.Size = new System.Drawing.Size(256, 256);
			this.bmpViewBG.TabIndex = 4;
			this.bmpViewBG.Text = "bmpView1";
			// 
			// bmpViewWin
			// 
			this.bmpViewWin.BackColor = System.Drawing.Color.Black;
			this.bmpViewWin.Location = new System.Drawing.Point(268, 32);
			this.bmpViewWin.Name = "bmpViewWin";
			this.bmpViewWin.Size = new System.Drawing.Size(256, 256);
			this.bmpViewWin.TabIndex = 5;
			this.bmpViewWin.Text = "bmpView2";
			// 
			// GBGPUView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(903, 500);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Name = "GBGPUView";
			this.Text = "GB GPU Viewer";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GBGPUView_FormClosed);
			this.Load += new System.EventHandler(this.GBGPUView_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private BmpView bmpViewBG;
		private BmpView bmpViewWin;
		private BmpView bmpViewTiles1;
		private BmpView bmpViewTiles2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private BmpView bmpViewBGPal;
		private BmpView bmpViewSPPal;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private BmpView bmpViewOAM;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.Label labelScanline;
		private System.Windows.Forms.Button buttonRefresh;
		private System.Windows.Forms.RadioButton radioButtonRefreshManual;
		private System.Windows.Forms.RadioButton radioButtonRefreshScanline;
		private System.Windows.Forms.RadioButton radioButtonRefreshFrame;
		private System.Windows.Forms.HScrollBar hScrollBarScanline;
	}
}