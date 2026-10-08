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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MobileBmpView));
            this.bmpView1 = new BizHawk.Client.EmuHawk.BmpView();
            this.SuspendLayout();
            // 
            // bmpView1
            // 
            resources.ApplyResources(this.bmpView1, "bmpView1");
            this.bmpView1.BackColor = System.Drawing.Color.Transparent;
            this.bmpView1.Name = "bmpView1";
            // 
            // MobileBmpView
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.bmpView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MobileBmpView";
            this.ResumeLayout(false);

		}

		#endregion

		private BmpView bmpView1;
	}
}