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
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(17, 21);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(22, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "BG";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(275, 24);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(26, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Win";
			// 
			// bmpViewWin
			// 
			this.bmpViewWin.BackColor = System.Drawing.Color.Transparent;
			this.bmpViewWin.Location = new System.Drawing.Point(278, 37);
			this.bmpViewWin.Name = "bmpViewWin";
			this.bmpViewWin.Size = new System.Drawing.Size(256, 256);
			this.bmpViewWin.TabIndex = 5;
			this.bmpViewWin.Text = "bmpView2";
			// 
			// bmpViewBG
			// 
			this.bmpViewBG.BackColor = System.Drawing.Color.Transparent;
			this.bmpViewBG.Location = new System.Drawing.Point(12, 37);
			this.bmpViewBG.Name = "bmpViewBG";
			this.bmpViewBG.Size = new System.Drawing.Size(256, 256);
			this.bmpViewBG.TabIndex = 4;
			this.bmpViewBG.Text = "bmpView1";
			// 
			// GBGPUView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(564, 404);
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
	}
}