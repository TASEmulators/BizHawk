namespace BizHawk.MultiClient
{
	partial class PCEBGViewer
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
			this.vdcComboBox = new System.Windows.Forms.ComboBox();
			this.canvas = new BizHawk.MultiClient.PCEBGCanvas();
			this.SuspendLayout();
			// 
			// vdcComboBox
			// 
			this.vdcComboBox.FormattingEnabled = true;
			this.vdcComboBox.Location = new System.Drawing.Point(13, 13);
			this.vdcComboBox.Name = "vdcComboBox";
			this.vdcComboBox.Size = new System.Drawing.Size(121, 20);
			this.vdcComboBox.TabIndex = 1;
			this.vdcComboBox.SelectedIndexChanged += new System.EventHandler(this.vdcComboBox_SelectedIndexChanged);
			// 
			// canvas
			// 
			this.canvas.Location = new System.Drawing.Point(12, 49);
			this.canvas.Name = "canvas";
			this.canvas.Size = new System.Drawing.Size(1024, 512);
			this.canvas.TabIndex = 0;
			// 
			// PCEBGViewer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1056, 573);
			this.Controls.Add(this.vdcComboBox);
			this.Controls.Add(this.canvas);
			this.Name = "PCEBGViewer";
			this.ShowIcon = false;
			this.Text = "PCE BG Viewer (interim)";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.PCEBGViewer_FormClosed);
			this.Load += new System.EventHandler(this.PCEBGViewer_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private PCEBGCanvas canvas;
		private System.Windows.Forms.ComboBox vdcComboBox;
	}
}