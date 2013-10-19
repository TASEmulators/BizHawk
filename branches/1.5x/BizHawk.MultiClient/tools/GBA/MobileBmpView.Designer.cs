namespace BizHawk.MultiClient.GBAtools
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
			this.bmpView1 = new BizHawk.MultiClient.GBtools.BmpView();
			this.SuspendLayout();
			// 
			// bmpView1
			// 
			this.bmpView1.Location = new System.Drawing.Point(0, 0);
			this.bmpView1.Name = "bmpView1";
			this.bmpView1.Size = new System.Drawing.Size(64, 64);
			this.bmpView1.TabIndex = 0;
			this.bmpView1.Text = "bmpView1";
			// 
			// MobileBmpView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Controls.Add(this.bmpView1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MobileBmpView";
			this.Text = "MobileBmpView";
			this.ResumeLayout(false);

		}

		#endregion

		private GBtools.BmpView bmpView1;
	}
}