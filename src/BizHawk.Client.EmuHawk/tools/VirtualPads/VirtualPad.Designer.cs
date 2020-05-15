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
			this.PadBox = new System.Windows.Forms.GroupBox();
			this.SuspendLayout();
			// 
			// PadBox
			// 
			this.PadBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PadBox.Location = new System.Drawing.Point(0, 0);
			this.PadBox.Name = "PadBox";
			this.PadBox.Size = new System.Drawing.Size(150, 150);
			this.PadBox.TabIndex = 0;
			this.PadBox.TabStop = false;
			// 
			// VirtualPad
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
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
