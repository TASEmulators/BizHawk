namespace BizHawk.Client.EmuHawk
{
	partial class VirtualPad
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtualPad));
            this.PadBox = new System.Windows.Forms.GroupBox();
            this.SuspendLayout();
            // 
            // PadBox
            // 
            resources.ApplyResources(this.PadBox, "PadBox");
            this.PadBox.Name = "PadBox";
            this.PadBox.TabStop = false;
            // 
            // VirtualPad
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PadBox);
            this.Name = "VirtualPad";
            this.Load += new System.EventHandler(this.VirtualPadControl_Load);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox PadBox;

	}
}
