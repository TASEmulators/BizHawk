namespace BizHawk.Client.EmuHawk.tools.TAStudio
{
	partial class MarkerControl
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
			this.MarkerLabel = new System.Windows.Forms.Label();
			this.MarkerBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// MarkerLabel
			// 
			this.MarkerLabel.AutoSize = true;
			this.MarkerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.MarkerLabel.ForeColor = System.Drawing.Color.DeepSkyBlue;
			this.MarkerLabel.Location = new System.Drawing.Point(0, 4);
			this.MarkerLabel.Name = "MarkerLabel";
			this.MarkerLabel.Size = new System.Drawing.Size(100, 16);
			this.MarkerLabel.TabIndex = 5;
			this.MarkerLabel.Text = "Marker 99999";
			// 
			// MarkerBox
			// 
			this.MarkerBox.Location = new System.Drawing.Point(103, 1);
			this.MarkerBox.Name = "MarkerBox";
			this.MarkerBox.Size = new System.Drawing.Size(188, 20);
			this.MarkerBox.TabIndex = 5;
			// 
			// MarkerControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MarkerBox);
			this.Controls.Add(this.MarkerLabel);
			this.Name = "MarkerControl";
			this.Size = new System.Drawing.Size(292, 24);
			this.Load += new System.EventHandler(this.MarkerControl_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label MarkerLabel;
		private System.Windows.Forms.TextBox MarkerBox;
	}
}
