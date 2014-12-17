namespace BizHawk.Client.EmuHawk
{
	partial class ToolBox
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolBox));
			this.ToolBoxStrip = new ToolStripEx();
			this.SuspendLayout();
			// 
			// ToolBoxStrip
			// 
			this.ToolBoxStrip.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ToolBoxStrip.AutoSize = false;
			this.ToolBoxStrip.BackColor = System.Drawing.SystemColors.Control;
			this.ToolBoxStrip.ClickThrough = true;
			this.ToolBoxStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.ToolBoxStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.ToolBoxStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
			this.ToolBoxStrip.Location = new System.Drawing.Point(2, 2);
			this.ToolBoxStrip.Name = "ToolBoxStrip";
			this.ToolBoxStrip.Padding = new System.Windows.Forms.Padding(0);
			this.ToolBoxStrip.Size = new System.Drawing.Size(137, 179);
			this.ToolBoxStrip.Stretch = true;
			this.ToolBoxStrip.TabIndex = 0;
			this.ToolBoxStrip.TabStop = true;
			// 
			// ToolBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(140, 183);
			this.Controls.Add(this.ToolBoxStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximumSize = new System.Drawing.Size(270, 600);
			this.MinimumSize = new System.Drawing.Size(135, 38);
			this.Name = "ToolBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Load += new System.EventHandler(this.ToolBox_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private ToolStripEx ToolBoxStrip;

	}
}