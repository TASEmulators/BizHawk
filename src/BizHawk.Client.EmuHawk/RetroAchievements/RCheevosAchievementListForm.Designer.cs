namespace BizHawk.Client.EmuHawk
{
	partial class RCheevosAchievementListForm
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
			this.vScrollBar1 = new System.Windows.Forms.VScrollBar();
			this.flowLayoutPanel1 = new BizHawk.Client.EmuHawk.VirtualizedFlowLayoutPanel();
			this.SuspendLayout();
			// 
			// vScrollBar1
			// 
			this.vScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.vScrollBar1.Location = new System.Drawing.Point(537, 12);
			this.vScrollBar1.Name = "vScrollBar1";
			this.vScrollBar1.Size = new System.Drawing.Size(17, 567);
			this.vScrollBar1.TabIndex = 1;
			this.vScrollBar1.ValueChanged += new System.EventHandler(this.vScrollBar1_ValueChanged);
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flowLayoutPanel1.AutoScroll = true;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 12);
			this.flowLayoutPanel1.MinimumSize = new System.Drawing.Size(544, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(544, 567);
			this.flowLayoutPanel1.TabIndex = 0;
			// 
			// RCheevosAchievementListForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(568, 591);
			this.Controls.Add(this.vScrollBar1);
			this.Controls.Add(this.flowLayoutPanel1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(574, 244);
			this.Name = "RCheevosAchievementListForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Achievement List";
			this.SizeChanged += new System.EventHandler(this.RCheevosAchievementListForm_SizeChanged);
			this.ResumeLayout(false);

		}

		#endregion

		private VirtualizedFlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.VScrollBar vScrollBar1;
	}
}
