namespace BizHawk.Client.EmuHawk
{
	partial class VDPViewer
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.bmpViewBG = new BizHawk.Client.EmuHawk.BmpView();
			this.bmpViewPalette = new BizHawk.Client.EmuHawk.BmpView();
			this.bmpViewTiles = new BizHawk.Client.EmuHawk.BmpView();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.bmpViewTiles);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(268, 153);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Tiles";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.bmpViewPalette);
			this.groupBox2.Location = new System.Drawing.Point(12, 171);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(268, 57);
			this.groupBox2.TabIndex = 2;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Palettes";
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.bmpViewBG);
			this.groupBox3.Location = new System.Drawing.Point(286, 12);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(268, 281);
			this.groupBox3.TabIndex = 3;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "BG";
			// 
			// bmpViewBG
			// 
			this.bmpViewBG.Location = new System.Drawing.Point(6, 19);
			this.bmpViewBG.Name = "bmpViewBG";
			this.bmpViewBG.Size = new System.Drawing.Size(256, 256);
			this.bmpViewBG.TabIndex = 0;
			this.bmpViewBG.Text = "bmpViewBG";
			// 
			// bmpViewPalette
			// 
			this.bmpViewPalette.Location = new System.Drawing.Point(6, 19);
			this.bmpViewPalette.Name = "bmpViewPalette";
			this.bmpViewPalette.Size = new System.Drawing.Size(256, 32);
			this.bmpViewPalette.TabIndex = 3;
			this.bmpViewPalette.Text = "bmpViewPalette";
			this.bmpViewPalette.MouseClick += new System.Windows.Forms.MouseEventHandler(this.bmpViewPalette_MouseClick);
			// 
			// bmpViewTiles
			// 
			this.bmpViewTiles.Location = new System.Drawing.Point(6, 19);
			this.bmpViewTiles.Name = "bmpViewTiles";
			this.bmpViewTiles.Size = new System.Drawing.Size(256, 128);
			this.bmpViewTiles.TabIndex = 0;
			this.bmpViewTiles.Text = "bmpViewTiles";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 304);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(294, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "CTRL + C copies the pane under the mouse to the clipboard.";
			// 
			// VDPViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(572, 326);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.KeyPreview = true;
			this.Name = "VDPViewer";
			this.Text = "VDP Viewer";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.VDPViewer_KeyDown);
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private BmpView bmpViewTiles;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private BmpView bmpViewPalette;
		private System.Windows.Forms.GroupBox groupBox3;
		private BmpView bmpViewBG;
		private System.Windows.Forms.Label label1;
	}
}