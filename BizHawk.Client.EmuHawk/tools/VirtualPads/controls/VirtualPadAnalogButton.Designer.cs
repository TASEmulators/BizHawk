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
			this.AnalogTrackBar = new System.Windows.Forms.TrackBar();
			this.DisplayNameLabel = new System.Windows.Forms.Label();
			this.ValueLabel = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.AnalogTrackBar)).BeginInit();
			this.SuspendLayout();
			// 
			// AnalogTrackBar
			// 
			this.AnalogTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.AnalogTrackBar.Location = new System.Drawing.Point(3, 3);
			this.AnalogTrackBar.Name = "AnalogTrackBar";
			this.AnalogTrackBar.Size = new System.Drawing.Size(299, 45);
			this.AnalogTrackBar.TabIndex = 0;
			this.AnalogTrackBar.ValueChanged += new System.EventHandler(this.AnalogTrackBar_ValueChanged);
			// 
			// DisplayNameLabel
			// 
			this.DisplayNameLabel.AutoSize = true;
			this.DisplayNameLabel.Location = new System.Drawing.Point(13, 51);
			this.DisplayNameLabel.Name = "DisplayNameLabel";
			this.DisplayNameLabel.Size = new System.Drawing.Size(33, 13);
			this.DisplayNameLabel.TabIndex = 1;
			this.DisplayNameLabel.Text = "Slider";
			// 
			// ValueLabel
			// 
			this.ValueLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.ValueLabel.AutoSize = true;
			this.ValueLabel.Location = new System.Drawing.Point(265, 51);
			this.ValueLabel.Name = "ValueLabel";
			this.ValueLabel.Size = new System.Drawing.Size(37, 13);
			this.ValueLabel.TabIndex = 2;
			this.ValueLabel.Text = "99999";
			// 
			// VirtualPadAnalogButton
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.ValueLabel);
			this.Controls.Add(this.DisplayNameLabel);
			this.Controls.Add(this.AnalogTrackBar);
			this.Name = "VirtualPadAnalogButton";
			this.Size = new System.Drawing.Size(338, 74);
			this.Load += new System.EventHandler(this.VirtualPadAnalogButton_Load);
			((System.ComponentModel.ISupportInitialize)(this.AnalogTrackBar)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TrackBar AnalogTrackBar;
		private System.Windows.Forms.Label DisplayNameLabel;
		private System.Windows.Forms.Label ValueLabel;
	}
}
