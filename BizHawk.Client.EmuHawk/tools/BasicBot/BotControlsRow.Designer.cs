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
			((System.ComponentModel.ISupportInitialize)(this.ProbabilityUpDown)).BeginInit();
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
			this.ProbabilityUpDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ProbabilityUpDown.DecimalPlaces = 1;
			this.ProbabilityUpDown.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.ProbabilityUpDown.Location = new System.Drawing.Point(81, -2);
			this.ProbabilityUpDown.Name = "ProbabilityUpDown";
			this.ProbabilityUpDown.Size = new System.Drawing.Size(79, 20);
			this.ProbabilityUpDown.TabIndex = 1;
			// 
			// BotControlsRow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.ProbabilityUpDown);
			this.Controls.Add(this.ButtonNameLabel);
			this.Name = "BotControlsRow";
			this.Size = new System.Drawing.Size(163, 20);
			this.Load += new System.EventHandler(this.BotControlsRow_Load);
			((System.ComponentModel.ISupportInitialize)(this.ProbabilityUpDown)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label ButtonNameLabel;
		private System.Windows.Forms.NumericUpDown ProbabilityUpDown;
	}
}
