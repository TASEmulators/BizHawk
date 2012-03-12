namespace BizHawk.MultiClient
{
	partial class PCEBGViewer
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
			this.canvas = new BizHawk.MultiClient.PCEBGCanvas();
			this.SuspendLayout();
			// 
			// canvas
			// 
			this.canvas.Location = new System.Drawing.Point(12, 12);
			this.canvas.Name = "canvas";
			this.canvas.Size = new System.Drawing.Size(1024, 512);
			this.canvas.TabIndex = 0;
			// 
			// PCEBGViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1056, 549);
			this.Controls.Add(this.canvas);
			this.Name = "PCEBGViewer";
			this.ShowIcon = false;
			this.Text = "PCE BG Viewer (interim)";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.PCEBGViewer_FormClosed);
			this.Load += new System.EventHandler(this.PCEBGViewer_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private PCEBGCanvas canvas;
	}
}