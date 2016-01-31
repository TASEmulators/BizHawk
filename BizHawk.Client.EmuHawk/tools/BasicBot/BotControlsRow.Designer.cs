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
			this.ButtonNameLabel = new System.Windows.Forms.Label();
			this.ProbabilityUpDown = new System.Windows.Forms.NumericUpDown();
			this.ProbabilitySlider = new System.Windows.Forms.TrackBar();
			((System.ComponentModel.ISupportInitialize)(this.ProbabilityUpDown)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ProbabilitySlider)).BeginInit();
			this.SuspendLayout();
			// 
			// ButtonNameLabel
			// 
			this.ButtonNameLabel.AutoSize = true;
			this.ButtonNameLabel.Location = new System.Drawing.Point(3, 0);
			this.ButtonNameLabel.Name = "ButtonNameLabel";
			this.ButtonNameLabel.Size = new System.Drawing.Size(35, 13);
			this.ButtonNameLabel.TabIndex = 0;
			this.ButtonNameLabel.Text = "label1";
			// 
			// ProbabilityUpDown
			// 
			this.ProbabilityUpDown.DecimalPlaces = 1;
			this.ProbabilityUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.ProbabilityUpDown.Location = new System.Drawing.Point(92, 0);
			this.ProbabilityUpDown.Name = "ProbabilityUpDown";
			this.ProbabilityUpDown.Size = new System.Drawing.Size(49, 20);
			this.ProbabilityUpDown.TabIndex = 1;
			this.ProbabilityUpDown.ValueChanged += new System.EventHandler(this.ProbabilityUpDown_ValueChanged);
			// 
			// ProbabilitySlider
			// 
			this.ProbabilitySlider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ProbabilitySlider.Location = new System.Drawing.Point(147, -2);
			this.ProbabilitySlider.Maximum = 100;
			this.ProbabilitySlider.Name = "ProbabilitySlider";
			this.ProbabilitySlider.Size = new System.Drawing.Size(203, 45);
			this.ProbabilitySlider.TabIndex = 2;
			this.ProbabilitySlider.TickFrequency = 25;
			this.ProbabilitySlider.ValueChanged += new System.EventHandler(this.ProbabilitySlider_ValueChanged);
			// 
			// BotControlsRow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.ProbabilitySlider);
			this.Controls.Add(this.ProbabilityUpDown);
			this.Controls.Add(this.ButtonNameLabel);
			this.Name = "BotControlsRow";
			this.Size = new System.Drawing.Size(350, 29);
			((System.ComponentModel.ISupportInitialize)(this.ProbabilityUpDown)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ProbabilitySlider)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label ButtonNameLabel;
		private System.Windows.Forms.NumericUpDown ProbabilityUpDown;
		private System.Windows.Forms.TrackBar ProbabilitySlider;
	}
}
