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
			this.label7 = new System.Windows.Forms.Label();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.hScrollBarScanline = new System.Windows.Forms.HScrollBar();
			this.labelScanline = new System.Windows.Forms.Label();
			this.buttonRefresh = new System.Windows.Forms.Button();
			this.radioButtonRefreshManual = new System.Windows.Forms.RadioButton();
			this.radioButtonRefreshScanline = new System.Windows.Forms.RadioButton();
			this.radioButtonRefreshFrame = new System.Windows.Forms.RadioButton();
			this.groupBoxDetails = new System.Windows.Forms.GroupBox();
			this.labelDetails = new System.Windows.Forms.Label();
			this.groupBoxMemory = new System.Windows.Forms.GroupBox();
			this.labelMemory = new System.Windows.Forms.Label();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.labelClipboard = new System.Windows.Forms.Label();
			this.groupBox7 = new System.Windows.Forms.GroupBox();
			this.checkBoxSavePos = new System.Windows.Forms.CheckBox();
			this.checkBoxAutoLoad = new System.Windows.Forms.CheckBox();
			this.bmpViewMemory = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewDetails = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewOAM = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewBGPal = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewSPPal = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewTiles1 = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewTiles2 = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewBG = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewWin = new BizHawk.MultiClient.GBtools.BmpView();
			this.groupBox8 = new System.Windows.Forms.GroupBox();
			this.panelSpriteBackColor = new System.Windows.Forms.Panel();
			this.buttonChangeColor = new System.Windows.Forms.Button();
			this.labelSpriteBackColor = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			this.groupBox5.SuspendLayout();
			this.groupBoxDetails.SuspendLayout();
			this.groupBoxMemory.SuspendLayout();
			this.groupBox6.SuspendLayout();
			this.groupBox7.SuspendLayout();
			this.groupBox8.SuspendLayout();
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
			this.groupBox3.Controls.Add(this.label7);
			this.groupBox3.Controls.Add(this.label5);
			this.groupBox3.Controls.Add(this.bmpViewBGPal);
			this.groupBox3.Controls.Add(this.bmpViewSPPal);
			this.groupBox3.Controls.Add(this.label6);
			this.groupBox3.Location = new System.Drawing.Point(548, 248);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(274, 115);
			this.groupBox3.TabIndex = 18;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Palettes";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(6, 99);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(266, 13);
			this.label7.TabIndex = 14;
			this.label7.Text = "Left-click a palette to use it for drawing the tiles display.";
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
			this.groupBox5.Location = new System.Drawing.Point(548, 369);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(274, 94);
			this.groupBox5.TabIndex = 20;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Refresh Control";
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
			// groupBoxDetails
			// 
			this.groupBoxDetails.Controls.Add(this.labelDetails);
			this.groupBoxDetails.Controls.Add(this.bmpViewDetails);
			this.groupBoxDetails.Location = new System.Drawing.Point(12, 359);
			this.groupBoxDetails.Name = "groupBoxDetails";
			this.groupBoxDetails.Size = new System.Drawing.Size(262, 153);
			this.groupBoxDetails.TabIndex = 21;
			this.groupBoxDetails.TabStop = false;
			this.groupBoxDetails.Text = "Details";
			// 
			// labelDetails
			// 
			this.labelDetails.AutoSize = true;
			this.labelDetails.Location = new System.Drawing.Point(76, 16);
			this.labelDetails.MaximumSize = new System.Drawing.Size(150, 0);
			this.labelDetails.Name = "labelDetails";
			this.labelDetails.Size = new System.Drawing.Size(135, 26);
			this.labelDetails.TabIndex = 1;
			this.labelDetails.Text = "Mouse over an item to see details about it.";
			// 
			// groupBoxMemory
			// 
			this.groupBoxMemory.Controls.Add(this.bmpViewMemory);
			this.groupBoxMemory.Controls.Add(this.labelMemory);
			this.groupBoxMemory.Location = new System.Drawing.Point(280, 359);
			this.groupBoxMemory.Name = "groupBoxMemory";
			this.groupBoxMemory.Size = new System.Drawing.Size(262, 153);
			this.groupBoxMemory.TabIndex = 22;
			this.groupBoxMemory.TabStop = false;
			this.groupBoxMemory.Text = "Details - Memory";
			// 
			// labelMemory
			// 
			this.labelMemory.AutoSize = true;
			this.labelMemory.Location = new System.Drawing.Point(76, 16);
			this.labelMemory.MaximumSize = new System.Drawing.Size(150, 0);
			this.labelMemory.Name = "labelMemory";
			this.labelMemory.Size = new System.Drawing.Size(149, 26);
			this.labelMemory.TabIndex = 0;
			this.labelMemory.Text = "Right-click an item to display it here.";
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.labelClipboard);
			this.groupBox6.Location = new System.Drawing.Point(548, 469);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(274, 43);
			this.groupBox6.TabIndex = 23;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Copy to Clipboard";
			// 
			// labelClipboard
			// 
			this.labelClipboard.AutoSize = true;
			this.labelClipboard.Location = new System.Drawing.Point(7, 23);
			this.labelClipboard.Name = "labelClipboard";
			this.labelClipboard.Size = new System.Drawing.Size(212, 13);
			this.labelClipboard.TabIndex = 0;
			this.labelClipboard.Text = "CTRL+C copies the pane under the mouse.";
			// 
			// groupBox7
			// 
			this.groupBox7.Controls.Add(this.checkBoxSavePos);
			this.groupBox7.Controls.Add(this.checkBoxAutoLoad);
			this.groupBox7.Location = new System.Drawing.Point(350, 312);
			this.groupBox7.Name = "groupBox7";
			this.groupBox7.Size = new System.Drawing.Size(192, 41);
			this.groupBox7.TabIndex = 24;
			this.groupBox7.TabStop = false;
			this.groupBox7.Text = "Config";
			// 
			// checkBoxSavePos
			// 
			this.checkBoxSavePos.AutoSize = true;
			this.checkBoxSavePos.Location = new System.Drawing.Point(87, 19);
			this.checkBoxSavePos.Name = "checkBoxSavePos";
			this.checkBoxSavePos.Size = new System.Drawing.Size(90, 17);
			this.checkBoxSavePos.TabIndex = 1;
			this.checkBoxSavePos.Text = "Save position";
			this.checkBoxSavePos.UseVisualStyleBackColor = true;
			this.checkBoxSavePos.CheckedChanged += new System.EventHandler(this.checkBoxSavePos_CheckedChanged);
			// 
			// checkBoxAutoLoad
			// 
			this.checkBoxAutoLoad.AutoSize = true;
			this.checkBoxAutoLoad.Location = new System.Drawing.Point(6, 19);
			this.checkBoxAutoLoad.Name = "checkBoxAutoLoad";
			this.checkBoxAutoLoad.Size = new System.Drawing.Size(75, 17);
			this.checkBoxAutoLoad.TabIndex = 0;
			this.checkBoxAutoLoad.Text = "Auto-Load";
			this.checkBoxAutoLoad.UseVisualStyleBackColor = true;
			this.checkBoxAutoLoad.CheckedChanged += new System.EventHandler(this.checkBoxAutoLoad_CheckedChanged);
			// 
			// bmpViewMemory
			// 
			this.bmpViewMemory.BackColor = System.Drawing.Color.Black;
			this.bmpViewMemory.DrawBackdrop = true;
			this.bmpViewMemory.Location = new System.Drawing.Point(6, 19);
			this.bmpViewMemory.Name = "bmpViewMemory";
			this.bmpViewMemory.Size = new System.Drawing.Size(64, 128);
			this.bmpViewMemory.TabIndex = 1;
			this.bmpViewMemory.Text = "Details (memory)";
			// 
			// bmpViewDetails
			// 
			this.bmpViewDetails.BackColor = System.Drawing.Color.Black;
			this.bmpViewDetails.DrawBackdrop = true;
			this.bmpViewDetails.Location = new System.Drawing.Point(6, 19);
			this.bmpViewDetails.Name = "bmpViewDetails";
			this.bmpViewDetails.Size = new System.Drawing.Size(64, 128);
			this.bmpViewDetails.TabIndex = 0;
			this.bmpViewDetails.Text = "Details (mouseover)";
			this.bmpViewDetails.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			// 
			// bmpViewOAM
			// 
			this.bmpViewOAM.BackColor = System.Drawing.Color.Black;
			this.bmpViewOAM.DrawBackdrop = true;
			this.bmpViewOAM.Location = new System.Drawing.Point(6, 19);
			this.bmpViewOAM.Name = "bmpViewOAM";
			this.bmpViewOAM.Size = new System.Drawing.Size(320, 16);
			this.bmpViewOAM.TabIndex = 14;
			this.bmpViewOAM.Text = "Sprites";
			this.bmpViewOAM.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			this.bmpViewOAM.MouseEnter += new System.EventHandler(this.bmpViewOAM_MouseEnter);
			this.bmpViewOAM.MouseLeave += new System.EventHandler(this.bmpViewOAM_MouseLeave);
			this.bmpViewOAM.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewOAM_MouseMove);
			// 
			// bmpViewBGPal
			// 
			this.bmpViewBGPal.BackColor = System.Drawing.Color.Black;
			this.bmpViewBGPal.DrawBackdrop = true;
			this.bmpViewBGPal.Location = new System.Drawing.Point(6, 32);
			this.bmpViewBGPal.Name = "bmpViewBGPal";
			this.bmpViewBGPal.Size = new System.Drawing.Size(128, 64);
			this.bmpViewBGPal.TabIndex = 10;
			this.bmpViewBGPal.Text = "Background palettes";
			this.bmpViewBGPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			this.bmpViewBGPal.MouseEnter += new System.EventHandler(this.bmpViewBGPal_MouseEnter);
			this.bmpViewBGPal.MouseLeave += new System.EventHandler(this.bmpViewBGPal_MouseLeave);
			this.bmpViewBGPal.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewBGPal_MouseMove);
			// 
			// bmpViewSPPal
			// 
			this.bmpViewSPPal.BackColor = System.Drawing.Color.Black;
			this.bmpViewSPPal.DrawBackdrop = true;
			this.bmpViewSPPal.Location = new System.Drawing.Point(140, 32);
			this.bmpViewSPPal.Name = "bmpViewSPPal";
			this.bmpViewSPPal.Size = new System.Drawing.Size(128, 64);
			this.bmpViewSPPal.TabIndex = 11;
			this.bmpViewSPPal.Text = "Sprite palettes";
			this.bmpViewSPPal.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			this.bmpViewSPPal.MouseEnter += new System.EventHandler(this.bmpViewSPPal_MouseEnter);
			this.bmpViewSPPal.MouseLeave += new System.EventHandler(this.bmpViewSPPal_MouseLeave);
			this.bmpViewSPPal.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewSPPal_MouseMove);
			// 
			// bmpViewTiles1
			// 
			this.bmpViewTiles1.BackColor = System.Drawing.Color.Black;
			this.bmpViewTiles1.DrawBackdrop = false;
			this.bmpViewTiles1.Location = new System.Drawing.Point(6, 32);
			this.bmpViewTiles1.Name = "bmpViewTiles1";
			this.bmpViewTiles1.Size = new System.Drawing.Size(128, 192);
			this.bmpViewTiles1.TabIndex = 6;
			this.bmpViewTiles1.Text = "Tiles 1";
			this.bmpViewTiles1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			this.bmpViewTiles1.MouseEnter += new System.EventHandler(this.bmpViewTiles1_MouseEnter);
			this.bmpViewTiles1.MouseLeave += new System.EventHandler(this.bmpViewTiles1_MouseLeave);
			this.bmpViewTiles1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewTiles1_MouseMove);
			// 
			// bmpViewTiles2
			// 
			this.bmpViewTiles2.BackColor = System.Drawing.Color.Black;
			this.bmpViewTiles2.DrawBackdrop = false;
			this.bmpViewTiles2.Location = new System.Drawing.Point(140, 32);
			this.bmpViewTiles2.Name = "bmpViewTiles2";
			this.bmpViewTiles2.Size = new System.Drawing.Size(128, 192);
			this.bmpViewTiles2.TabIndex = 7;
			this.bmpViewTiles2.Text = "Tiles 2";
			this.bmpViewTiles2.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			this.bmpViewTiles2.MouseEnter += new System.EventHandler(this.bmpViewTiles2_MouseEnter);
			this.bmpViewTiles2.MouseLeave += new System.EventHandler(this.bmpViewTiles2_MouseLeave);
			this.bmpViewTiles2.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewTiles2_MouseMove);
			// 
			// bmpViewBG
			// 
			this.bmpViewBG.BackColor = System.Drawing.Color.Black;
			this.bmpViewBG.DrawBackdrop = false;
			this.bmpViewBG.Location = new System.Drawing.Point(6, 32);
			this.bmpViewBG.Name = "bmpViewBG";
			this.bmpViewBG.Size = new System.Drawing.Size(256, 256);
			this.bmpViewBG.TabIndex = 4;
			this.bmpViewBG.Text = "Background";
			this.bmpViewBG.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			this.bmpViewBG.MouseEnter += new System.EventHandler(this.bmpViewBG_MouseEnter);
			this.bmpViewBG.MouseLeave += new System.EventHandler(this.bmpViewBG_MouseLeave);
			this.bmpViewBG.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewBG_MouseMove);
			// 
			// bmpViewWin
			// 
			this.bmpViewWin.BackColor = System.Drawing.Color.Black;
			this.bmpViewWin.DrawBackdrop = false;
			this.bmpViewWin.Location = new System.Drawing.Point(268, 32);
			this.bmpViewWin.Name = "bmpViewWin";
			this.bmpViewWin.Size = new System.Drawing.Size(256, 256);
			this.bmpViewWin.TabIndex = 5;
			this.bmpViewWin.Text = "Window";
			this.bmpViewWin.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpView_MouseClick);
			this.bmpViewWin.MouseEnter += new System.EventHandler(this.bmpViewWin_MouseEnter);
			this.bmpViewWin.MouseLeave += new System.EventHandler(this.bmpViewWin_MouseLeave);
			this.bmpViewWin.MouseMove += new System.Windows.Forms.MouseEventHandler(this.bmpViewWin_MouseMove);
			// 
			// groupBox8
			// 
			this.groupBox8.Controls.Add(this.labelSpriteBackColor);
			this.groupBox8.Controls.Add(this.buttonChangeColor);
			this.groupBox8.Controls.Add(this.panelSpriteBackColor);
			this.groupBox8.Location = new System.Drawing.Point(548, 518);
			this.groupBox8.Name = "groupBox8";
			this.groupBox8.Size = new System.Drawing.Size(274, 48);
			this.groupBox8.TabIndex = 25;
			this.groupBox8.TabStop = false;
			this.groupBox8.Text = "Sprite Backdrop";
			// 
			// panelSpriteBackColor
			// 
			this.panelSpriteBackColor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.panelSpriteBackColor.Location = new System.Drawing.Point(6, 19);
			this.panelSpriteBackColor.Name = "panelSpriteBackColor";
			this.panelSpriteBackColor.Size = new System.Drawing.Size(55, 23);
			this.panelSpriteBackColor.TabIndex = 0;
			// 
			// buttonChangeColor
			// 
			this.buttonChangeColor.Location = new System.Drawing.Point(164, 19);
			this.buttonChangeColor.Name = "buttonChangeColor";
			this.buttonChangeColor.Size = new System.Drawing.Size(104, 23);
			this.buttonChangeColor.TabIndex = 1;
			this.buttonChangeColor.Text = "Change Color...";
			this.buttonChangeColor.UseVisualStyleBackColor = true;
			this.buttonChangeColor.Click += new System.EventHandler(this.buttonChangeColor_Click);
			// 
			// labelSpriteBackColor
			// 
			this.labelSpriteBackColor.AutoSize = true;
			this.labelSpriteBackColor.Location = new System.Drawing.Point(67, 24);
			this.labelSpriteBackColor.Name = "labelSpriteBackColor";
			this.labelSpriteBackColor.Size = new System.Drawing.Size(35, 13);
			this.labelSpriteBackColor.TabIndex = 2;
			this.labelSpriteBackColor.Text = "label8";
			// 
			// GBGPUView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(834, 590);
			this.Controls.Add(this.groupBox8);
			this.Controls.Add(this.groupBox7);
			this.Controls.Add(this.groupBox6);
			this.Controls.Add(this.groupBoxMemory);
			this.Controls.Add(this.groupBoxDetails);
			this.Controls.Add(this.groupBox5);
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "GBGPUView";
			this.Text = "GameBoy GPU Viewer";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GBGPUView_FormClosing);
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GBGPUView_FormClosed);
			this.Load += new System.EventHandler(this.GBGPUView_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GBGPUView_KeyDown);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.groupBox4.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			this.groupBox5.PerformLayout();
			this.groupBoxDetails.ResumeLayout(false);
			this.groupBoxDetails.PerformLayout();
			this.groupBoxMemory.ResumeLayout(false);
			this.groupBoxMemory.PerformLayout();
			this.groupBox6.ResumeLayout(false);
			this.groupBox6.PerformLayout();
			this.groupBox7.ResumeLayout(false);
			this.groupBox7.PerformLayout();
			this.groupBox8.ResumeLayout(false);
			this.groupBox8.PerformLayout();
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
		private System.Windows.Forms.GroupBox groupBoxDetails;
		private BmpView bmpViewDetails;
		private System.Windows.Forms.Label labelDetails;
		private System.Windows.Forms.GroupBox groupBoxMemory;
		private System.Windows.Forms.Label labelMemory;
		private BmpView bmpViewMemory;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.Label labelClipboard;
		private System.Windows.Forms.GroupBox groupBox7;
		private System.Windows.Forms.CheckBox checkBoxSavePos;
		private System.Windows.Forms.CheckBox checkBoxAutoLoad;
		private System.Windows.Forms.GroupBox groupBox8;
		private System.Windows.Forms.Panel panelSpriteBackColor;
		private System.Windows.Forms.Button buttonChangeColor;
		private System.Windows.Forms.Label labelSpriteBackColor;
	}
}