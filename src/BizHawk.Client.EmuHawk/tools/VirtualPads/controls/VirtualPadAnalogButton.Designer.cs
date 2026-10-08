namespace BizHawk.Client.EmuHawk
{
	partial class VirtualPadAnalogButton
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtualPadAnalogButton));
            this.AnalogTrackBar = new System.Windows.Forms.TrackBar();
            this.DisplayNameLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ValueLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            ((System.ComponentModel.ISupportInitialize)(this.AnalogTrackBar)).BeginInit();
            this.SuspendLayout();
            // 
            // AnalogTrackBar
            // 
            resources.ApplyResources(this.AnalogTrackBar, "AnalogTrackBar");
            this.AnalogTrackBar.Name = "AnalogTrackBar";
            this.AnalogTrackBar.ValueChanged += new System.EventHandler(this.AnalogTrackBar_ValueChanged);
            // 
            // DisplayNameLabel
            // 
            resources.ApplyResources(this.DisplayNameLabel, "DisplayNameLabel");
            this.DisplayNameLabel.Name = "DisplayNameLabel";
            // 
            // ValueLabel
            // 
            resources.ApplyResources(this.ValueLabel, "ValueLabel");
            this.ValueLabel.Name = "ValueLabel";
            // 
            // VirtualPadAnalogButton
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ValueLabel);
            this.Controls.Add(this.DisplayNameLabel);
            this.Controls.Add(this.AnalogTrackBar);
            this.Name = "VirtualPadAnalogButton";
            ((System.ComponentModel.ISupportInitialize)(this.AnalogTrackBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TrackBar AnalogTrackBar;
		private BizHawk.WinForms.Controls.LocLabelEx DisplayNameLabel;
		private BizHawk.WinForms.Controls.LocLabelEx ValueLabel;
	}
}
