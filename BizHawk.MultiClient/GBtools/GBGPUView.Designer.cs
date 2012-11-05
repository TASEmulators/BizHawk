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
			this.bmpViewWin = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewBG = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewTiles1 = new BizHawk.MultiClient.GBtools.BmpView();
			this.bmpViewTiles2 = new BizHawk.MultiClient.GBtools.BmpView();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(22, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "BG";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(271, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(26, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Win";
			// 
			// bmpViewWin
			// 
			this.bmpViewWin.BackColor = System.Drawing.Color.Transparent;
			this.bmpViewWin.Location = new System.Drawing.Point(274, 25);
			this.bmpViewWin.Name = "bmpViewWin";
			this.bmpViewWin.Size = new System.Drawing.Size(256, 256);
			this.bmpViewWin.TabIndex = 5;
			this.bmpViewWin.Text = "bmpView2";
			// 
			// bmpViewBG
			// 
			this.bmpViewBG.BackColor = System.Drawing.Color.Transparent;
			this.bmpViewBG.Location = new System.Drawing.Point(12, 25);
			this.bmpViewBG.Name = "bmpViewBG";
			this.bmpViewBG.Size = new System.Drawing.Size(256, 256);
			this.bmpViewBG.TabIndex = 4;
			this.bmpViewBG.Text = "bmpView1";
			// 
			// bmpViewTiles1
			// 
			this.bmpViewTiles1.BackColor = System.Drawing.Color.Transparent;
			this.bmpViewTiles1.Location = new System.Drawing.Point(536, 25);
			this.bmpViewTiles1.Name = "bmpViewTiles1";
			this.bmpViewTiles1.Size = new System.Drawing.Size(128, 192);
			this.bmpViewTiles1.TabIndex = 6;
			this.bmpViewTiles1.Text = "bmpView1";
			// 
			// bmpViewTiles2
			// 
			this.bmpViewTiles2.BackColor = System.Drawing.Color.Transparent;
			this.bmpViewTiles2.Location = new System.Drawing.Point(670, 25);
			this.bmpViewTiles2.Name = "bmpViewTiles2";
			this.bmpViewTiles2.Size = new System.Drawing.Size(128, 192);
			this.bmpViewTiles2.TabIndex = 7;
			this.bmpViewTiles2.Text = "bmpView2";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(533, 9);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(38, 13);
			this.label3.TabIndex = 8;
			this.label3.Text = "Tiles 1";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(667, 9);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(69, 13);
			this.label4.TabIndex = 9;
			this.label4.Text = "Tiles 2 (CGB)";
			// 
			// GBGPUView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(951, 414);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.bmpViewTiles2);
			this.Controls.Add(this.bmpViewTiles1);
			this.Controls.Add(this.bmpViewWin);
			this.Controls.Add(this.bmpViewBG);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Name = "GBGPUView";
			this.Text = "GB GPU Viewer";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.GBGPUView_FormClosed);
			this.Load += new System.EventHandler(this.GBGPUView_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

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
	}
}