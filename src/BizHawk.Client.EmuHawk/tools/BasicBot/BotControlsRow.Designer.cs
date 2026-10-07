namespace BizHawk.Client.EmuHawk
{
	partial class BotControlsRow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BotControlsRow));
            this.ButtonNameLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ProbabilityUpDown = new System.Windows.Forms.NumericUpDown();
            this.ProbabilitySlider = new System.Windows.Forms.TrackBar();
            ((System.ComponentModel.ISupportInitialize)(this.ProbabilityUpDown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ProbabilitySlider)).BeginInit();
            this.SuspendLayout();
            // 
            // ButtonNameLabel
            // 
            resources.ApplyResources(this.ButtonNameLabel, "ButtonNameLabel");
            this.ButtonNameLabel.Name = "ButtonNameLabel";
            // 
            // ProbabilityUpDown
            // 
            resources.ApplyResources(this.ProbabilityUpDown, "ProbabilityUpDown");
            this.ProbabilityUpDown.DecimalPlaces = 1;
            this.ProbabilityUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.ProbabilityUpDown.Name = "ProbabilityUpDown";
            this.ProbabilityUpDown.ValueChanged += new System.EventHandler(this.ProbabilityUpDown_ValueChanged);
            // 
            // ProbabilitySlider
            // 
            resources.ApplyResources(this.ProbabilitySlider, "ProbabilitySlider");
            this.ProbabilitySlider.Maximum = 100;
            this.ProbabilitySlider.Name = "ProbabilitySlider";
            this.ProbabilitySlider.TickFrequency = 25;
            this.ProbabilitySlider.ValueChanged += new System.EventHandler(this.ProbabilitySlider_ValueChanged);
            // 
            // BotControlsRow
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.ProbabilitySlider);
            this.Controls.Add(this.ProbabilityUpDown);
            this.Controls.Add(this.ButtonNameLabel);
            this.Name = "BotControlsRow";
            ((System.ComponentModel.ISupportInitialize)(this.ProbabilityUpDown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ProbabilitySlider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.LocLabelEx ButtonNameLabel;
		private System.Windows.Forms.NumericUpDown ProbabilityUpDown;
		private System.Windows.Forms.TrackBar ProbabilitySlider;
	}
}
