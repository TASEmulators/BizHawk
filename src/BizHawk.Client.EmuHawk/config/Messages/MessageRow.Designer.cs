namespace BizHawk.Client.EmuHawk
{
	partial class MessageRow
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
            this.RowRadio = new System.Windows.Forms.RadioButton();
            this.LocationLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SuspendLayout();
            // 
            // RowRadio
            // 
            this.RowRadio.AutoSize = true;
            this.RowRadio.Location = new System.Drawing.Point(3, 5);
            this.RowRadio.Name = "RowRadio";
            this.RowRadio.Size = new System.Drawing.Size(94, 17);
            this.RowRadio.TabIndex = 0;
            this.RowRadio.TabStop = true;
            this.RowRadio.Text = "Frame Counter";
            this.RowRadio.UseVisualStyleBackColor = true;
            this.RowRadio.CheckedChanged += new System.EventHandler(this.RowRadio_CheckedChanged);
            // 
            // LocationLabel
            // 
            this.LocationLabel.AllowDrop = true;
            this.LocationLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.LocationLabel.Location = new System.Drawing.Point(167, 7);
            this.LocationLabel.Name = "LocationLabel";
            this.LocationLabel.Text = "255, 255";
            // 
            // MessageRow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LocationLabel);
            this.Controls.Add(this.RowRadio);
            this.Name = "MessageRow";
            this.Size = new System.Drawing.Size(224, 28);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RadioButton RowRadio;
		private WinForms.Controls.LocLabelEx LocationLabel;
	}
}
