namespace BizHawk.Client.EmuHawk
{
	partial class GbGpuView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GbGpuView));
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.bmpViewBG = new BizHawk.Client.EmuHawk.BmpView();
            this.bmpViewWin = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.bmpViewTiles1 = new BizHawk.Client.EmuHawk.BmpView();
            this.bmpViewTiles2 = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label7 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.bmpViewBGPal = new BizHawk.Client.EmuHawk.BmpView();
            this.bmpViewSPPal = new BizHawk.Client.EmuHawk.BmpView();
            this.label8 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label9 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.bmpViewOAM = new BizHawk.Client.EmuHawk.BmpView();
            this.bmpViewOBJ = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.hScrollBarScanline = new System.Windows.Forms.HScrollBar();
            this.labelScanline = new BizHawk.WinForms.Controls.LocLabelEx();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.radioButtonRefreshManual = new System.Windows.Forms.RadioButton();
            this.radioButtonRefreshScanline = new System.Windows.Forms.RadioButton();
            this.radioButtonRefreshFrame = new System.Windows.Forms.RadioButton();
            this.groupBoxDetails = new System.Windows.Forms.GroupBox();
            this.labelDetails = new BizHawk.WinForms.Controls.LocLabelEx();
            this.bmpViewDetails = new BizHawk.Client.EmuHawk.BmpView();
            this.groupBoxMemory = new System.Windows.Forms.GroupBox();
            this.bmpViewMemory = new BizHawk.Client.EmuHawk.BmpView();
            this.labelMemory = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.labelClipboard = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox8 = new System.Windows.Forms.GroupBox();
            this.labelSpriteBackColor = new BizHawk.WinForms.Controls.LocLabelEx();
            this.buttonChangeColor = new System.Windows.Forms.Button();
            this.panelSpriteBackColor = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBoxDetails.SuspendLayout();
            this.groupBoxMemory.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox8.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.bmpViewBG);
            this.groupBox1.Controls.Add(this.bmpViewWin);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // bmpViewBG
            // 
            resources.ApplyResources(this.bmpViewBG, "bmpViewBG");
            this.bmpViewBG.BackColor = System.Drawing.Color.Black;
            this.bmpViewBG.Name = "bmpViewBG";
            this.bmpViewBG.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewBG.MouseEnter += new System.EventHandler(this.bmpViewBG_MouseEnter);
            this.bmpViewBG.MouseLeave += new System.EventHandler(this.bmpViewBG_MouseLeave);
            this.bmpViewBG.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewBG_MouseMove);
            // 
            // bmpViewWin
            // 
            resources.ApplyResources(this.bmpViewWin, "bmpViewWin");
            this.bmpViewWin.BackColor = System.Drawing.Color.Black;
            this.bmpViewWin.Name = "bmpViewWin";
            this.bmpViewWin.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewWin.MouseEnter += new System.EventHandler(this.bmpViewWin_MouseEnter);
            this.bmpViewWin.MouseLeave += new System.EventHandler(this.bmpViewWin_MouseLeave);
            this.bmpViewWin.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewWin_MouseMove);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.bmpViewTiles1);
            this.groupBox2.Controls.Add(this.bmpViewTiles2);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // bmpViewTiles1
            // 
            resources.ApplyResources(this.bmpViewTiles1, "bmpViewTiles1");
            this.bmpViewTiles1.BackColor = System.Drawing.Color.Black;
            this.bmpViewTiles1.Name = "bmpViewTiles1";
            this.bmpViewTiles1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewTiles1.MouseEnter += new System.EventHandler(this.bmpViewTiles1_MouseEnter);
            this.bmpViewTiles1.MouseLeave += new System.EventHandler(this.bmpViewTiles1_MouseLeave);
            this.bmpViewTiles1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewTiles1_MouseMove);
            // 
            // bmpViewTiles2
            // 
            resources.ApplyResources(this.bmpViewTiles2, "bmpViewTiles2");
            this.bmpViewTiles2.BackColor = System.Drawing.Color.Black;
            this.bmpViewTiles2.Name = "bmpViewTiles2";
            this.bmpViewTiles2.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewTiles2.MouseEnter += new System.EventHandler(this.bmpViewTiles2_MouseEnter);
            this.bmpViewTiles2.MouseLeave += new System.EventHandler(this.bmpViewTiles2_MouseLeave);
            this.bmpViewTiles2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewTiles2_MouseMove);
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.bmpViewBGPal);
            this.groupBox3.Controls.Add(this.bmpViewSPPal);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // label7
            // 
            resources.ApplyResources(this.label7, "label7");
            this.label7.Name = "label7";
            // 
            // bmpViewBGPal
            // 
            resources.ApplyResources(this.bmpViewBGPal, "bmpViewBGPal");
            this.bmpViewBGPal.BackColor = System.Drawing.Color.Black;
            this.bmpViewBGPal.Name = "bmpViewBGPal";
            this.bmpViewBGPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewBGPal.MouseEnter += new System.EventHandler(this.bmpViewBGPal_MouseEnter);
            this.bmpViewBGPal.MouseLeave += new System.EventHandler(this.bmpViewBGPal_MouseLeave);
            this.bmpViewBGPal.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewBGPal_MouseMove);
            // 
            // bmpViewSPPal
            // 
            resources.ApplyResources(this.bmpViewSPPal, "bmpViewSPPal");
            this.bmpViewSPPal.BackColor = System.Drawing.Color.Black;
            this.bmpViewSPPal.Name = "bmpViewSPPal";
            this.bmpViewSPPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewSPPal.MouseEnter += new System.EventHandler(this.bmpViewSPPal_MouseEnter);
            this.bmpViewSPPal.MouseLeave += new System.EventHandler(this.bmpViewSPPal_MouseLeave);
            this.bmpViewSPPal.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewSPPal_MouseMove);
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.Name = "label8";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.Name = "label9";
            // 
            // groupBox4
            // 
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Controls.Add(this.bmpViewOAM);
            this.groupBox4.Controls.Add(this.bmpViewOBJ);
            this.groupBox4.Controls.Add(this.label8);
            this.groupBox4.Controls.Add(this.label9);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // bmpViewOAM
            // 
            resources.ApplyResources(this.bmpViewOAM, "bmpViewOAM");
            this.bmpViewOAM.BackColor = System.Drawing.Color.Black;
            this.bmpViewOAM.Name = "bmpViewOAM";
            this.bmpViewOAM.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewOAM.MouseEnter += new System.EventHandler(this.bmpViewOAM_MouseEnter);
            this.bmpViewOAM.MouseLeave += new System.EventHandler(this.bmpViewOAM_MouseLeave);
            this.bmpViewOAM.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewOAM_MouseMove);
            // 
            // bmpViewOBJ
            // 
            resources.ApplyResources(this.bmpViewOBJ, "bmpViewOBJ");
            this.bmpViewOBJ.BackColor = System.Drawing.Color.Black;
            this.bmpViewOBJ.Name = "bmpViewOBJ";
            this.bmpViewOBJ.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            this.bmpViewOBJ.MouseEnter += new System.EventHandler(this.bmpViewOBJ_MouseEnter);
            this.bmpViewOBJ.MouseLeave += new System.EventHandler(this.bmpViewOBJ_MouseLeave);
            this.bmpViewOBJ.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewOBJ_MouseMove);
            // 
            // groupBox5
            // 
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Controls.Add(this.hScrollBarScanline);
            this.groupBox5.Controls.Add(this.labelScanline);
            this.groupBox5.Controls.Add(this.buttonRefresh);
            this.groupBox5.Controls.Add(this.radioButtonRefreshManual);
            this.groupBox5.Controls.Add(this.radioButtonRefreshScanline);
            this.groupBox5.Controls.Add(this.radioButtonRefreshFrame);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // hScrollBarScanline
            // 
            resources.ApplyResources(this.hScrollBarScanline, "hScrollBarScanline");
            this.hScrollBarScanline.Maximum = 162;
            this.hScrollBarScanline.Name = "hScrollBarScanline";
            this.hScrollBarScanline.ValueChanged += new System.EventHandler(this.hScrollBarScanline_ValueChanged);
            // 
            // labelScanline
            // 
            resources.ApplyResources(this.labelScanline, "labelScanline");
            this.labelScanline.Name = "labelScanline";
            // 
            // buttonRefresh
            // 
            resources.ApplyResources(this.buttonRefresh, "buttonRefresh");
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.buttonRefresh_Click);
            // 
            // radioButtonRefreshManual
            // 
            resources.ApplyResources(this.radioButtonRefreshManual, "radioButtonRefreshManual");
            this.radioButtonRefreshManual.Name = "radioButtonRefreshManual";
            this.radioButtonRefreshManual.TabStop = true;
            this.radioButtonRefreshManual.UseVisualStyleBackColor = true;
            this.radioButtonRefreshManual.CheckedChanged += new System.EventHandler(this.radioButtonRefreshManual_CheckedChanged);
            // 
            // radioButtonRefreshScanline
            // 
            resources.ApplyResources(this.radioButtonRefreshScanline, "radioButtonRefreshScanline");
            this.radioButtonRefreshScanline.Name = "radioButtonRefreshScanline";
            this.radioButtonRefreshScanline.TabStop = true;
            this.radioButtonRefreshScanline.UseVisualStyleBackColor = true;
            this.radioButtonRefreshScanline.CheckedChanged += new System.EventHandler(this.radioButtonRefreshScanline_CheckedChanged);
            // 
            // radioButtonRefreshFrame
            // 
            resources.ApplyResources(this.radioButtonRefreshFrame, "radioButtonRefreshFrame");
            this.radioButtonRefreshFrame.Name = "radioButtonRefreshFrame";
            this.radioButtonRefreshFrame.TabStop = true;
            this.radioButtonRefreshFrame.UseVisualStyleBackColor = true;
            this.radioButtonRefreshFrame.CheckedChanged += new System.EventHandler(this.radioButtonRefreshFrame_CheckedChanged);
            // 
            // groupBoxDetails
            // 
            resources.ApplyResources(this.groupBoxDetails, "groupBoxDetails");
            this.groupBoxDetails.Controls.Add(this.labelDetails);
            this.groupBoxDetails.Controls.Add(this.bmpViewDetails);
            this.groupBoxDetails.Name = "groupBoxDetails";
            this.groupBoxDetails.TabStop = false;
            // 
            // labelDetails
            // 
            resources.ApplyResources(this.labelDetails, "labelDetails");
            this.labelDetails.Name = "labelDetails";
            // 
            // bmpViewDetails
            // 
            resources.ApplyResources(this.bmpViewDetails, "bmpViewDetails");
            this.bmpViewDetails.BackColor = System.Drawing.Color.Black;
            this.bmpViewDetails.Name = "bmpViewDetails";
            this.bmpViewDetails.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
            // 
            // groupBoxMemory
            // 
            resources.ApplyResources(this.groupBoxMemory, "groupBoxMemory");
            this.groupBoxMemory.Controls.Add(this.bmpViewMemory);
            this.groupBoxMemory.Controls.Add(this.labelMemory);
            this.groupBoxMemory.Name = "groupBoxMemory";
            this.groupBoxMemory.TabStop = false;
            // 
            // bmpViewMemory
            // 
            resources.ApplyResources(this.bmpViewMemory, "bmpViewMemory");
            this.bmpViewMemory.BackColor = System.Drawing.Color.Black;
            this.bmpViewMemory.Name = "bmpViewMemory";
            // 
            // labelMemory
            // 
            resources.ApplyResources(this.labelMemory, "labelMemory");
            this.labelMemory.Name = "labelMemory";
            // 
            // groupBox6
            // 
            resources.ApplyResources(this.groupBox6, "groupBox6");
            this.groupBox6.Controls.Add(this.labelClipboard);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.TabStop = false;
            // 
            // labelClipboard
            // 
            resources.ApplyResources(this.labelClipboard, "labelClipboard");
            this.labelClipboard.Name = "labelClipboard";
            // 
            // groupBox8
            // 
            resources.ApplyResources(this.groupBox8, "groupBox8");
            this.groupBox8.Controls.Add(this.labelSpriteBackColor);
            this.groupBox8.Controls.Add(this.buttonChangeColor);
            this.groupBox8.Controls.Add(this.panelSpriteBackColor);
            this.groupBox8.Name = "groupBox8";
            this.groupBox8.TabStop = false;
            // 
            // labelSpriteBackColor
            // 
            resources.ApplyResources(this.labelSpriteBackColor, "labelSpriteBackColor");
            this.labelSpriteBackColor.Name = "labelSpriteBackColor";
            // 
            // buttonChangeColor
            // 
            resources.ApplyResources(this.buttonChangeColor, "buttonChangeColor");
            this.buttonChangeColor.Name = "buttonChangeColor";
            this.buttonChangeColor.UseVisualStyleBackColor = true;
            this.buttonChangeColor.Click += new System.EventHandler(this.ButtonChangeColor_Click);
            // 
            // panelSpriteBackColor
            // 
            resources.ApplyResources(this.panelSpriteBackColor, "panelSpriteBackColor");
            this.panelSpriteBackColor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelSpriteBackColor.Name = "panelSpriteBackColor";
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            // 
            // GbGpuView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox8);
            this.Controls.Add(this.groupBox6);
            this.Controls.Add(this.groupBoxMemory);
            this.Controls.Add(this.groupBoxDetails);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "GbGpuView";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GbGpuView_FormClosed);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GbGpuView_KeyDown);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBoxDetails.ResumeLayout(false);
            this.groupBoxDetails.PerformLayout();
            this.groupBoxMemory.ResumeLayout(false);
            this.groupBoxMemory.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox8.ResumeLayout(false);
            this.groupBox8.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BmpView bmpViewBG;
		private BmpView bmpViewWin;
		private BmpView bmpViewTiles1;
		private BmpView bmpViewTiles2;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BmpView bmpViewBGPal;
		private BmpView bmpViewSPPal;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private BmpView bmpViewOAM;
		private BmpView bmpViewOBJ;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.GroupBox groupBox5;
		private BizHawk.WinForms.Controls.LocLabelEx labelScanline;
		private System.Windows.Forms.Button buttonRefresh;
		private System.Windows.Forms.RadioButton radioButtonRefreshManual;
		private System.Windows.Forms.RadioButton radioButtonRefreshScanline;
		private System.Windows.Forms.RadioButton radioButtonRefreshFrame;
		private System.Windows.Forms.HScrollBar hScrollBarScanline;
		private System.Windows.Forms.GroupBox groupBoxDetails;
		private BmpView bmpViewDetails;
		private BizHawk.WinForms.Controls.LocLabelEx labelDetails;
		private System.Windows.Forms.GroupBox groupBoxMemory;
		private BizHawk.WinForms.Controls.LocLabelEx labelMemory;
		private BmpView bmpViewMemory;
		private BizHawk.WinForms.Controls.LocLabelEx label7;
		private BizHawk.WinForms.Controls.LocLabelEx label8;
		private BizHawk.WinForms.Controls.LocLabelEx label9;
		private System.Windows.Forms.GroupBox groupBox6;
		private BizHawk.WinForms.Controls.LocLabelEx labelClipboard;
		private System.Windows.Forms.GroupBox groupBox8;
		private System.Windows.Forms.Panel panelSpriteBackColor;
		private System.Windows.Forms.Button buttonChangeColor;
		private BizHawk.WinForms.Controls.LocLabelEx labelSpriteBackColor;
		private System.Windows.Forms.MenuStrip menuStrip1;
	}
}