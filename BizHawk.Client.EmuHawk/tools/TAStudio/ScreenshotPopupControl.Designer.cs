namespace BizHawk.Client.EmuHawk
{
	partial class ScreenshotPopupControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// ScreenshotPopupControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "ScreenshotPopupControl";
			this.Size = new System.Drawing.Size(237, 255);
			this.Load += new System.EventHandler(this.ScreenshotPopupControl_Load);
			this.ResumeLayout(false);
			this.MouseLeave += new System.EventHandler(ScreenshotPopupControl_MouseLeave);
			this.MouseHover += new System.EventHandler(ScreenshotPopupControl_MouseHover);
		}

		#endregion
	}
}
