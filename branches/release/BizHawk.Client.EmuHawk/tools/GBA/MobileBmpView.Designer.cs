namespace BizHawk.Client.EmuHawk
{
	partial class MobileBmpView
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
            this.BmpView = new BizHawk.Client.EmuHawk.BmpView();
            this.SuspendLayout();
            // 
            // bmpView1
            // 
            this.BmpView.Location = new System.Drawing.Point(0, 0);
            this.BmpView.Name = "bmpView";
            this.BmpView.Size = new System.Drawing.Size(64, 64);
            this.BmpView.TabIndex = 0;
            this.BmpView.Text = "bmpView1";
            // 
            // MobileBmpView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 273);
            this.Controls.Add(this.BmpView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MobileBmpView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MobileBmpView";
            this.ResumeLayout(false);

		}

		#endregion
	}
}