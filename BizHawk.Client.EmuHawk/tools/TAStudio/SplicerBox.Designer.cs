namespace BizHawk.Client.EmuHawk
{
	partial class SplicerBox
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
			this.SplicerGroupBox = new System.Windows.Forms.GroupBox();
			this.ClipboardStatsLabel = new System.Windows.Forms.Label();
			this.SelectionStatsLabel = new System.Windows.Forms.Label();
			this.ClipboardNameLabel = new System.Windows.Forms.Label();
			this.SelectionNameLabel = new System.Windows.Forms.Label();
			this.SplicerGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// SplicerGroupBox
			// 
			this.SplicerGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SplicerGroupBox.Controls.Add(this.ClipboardStatsLabel);
			this.SplicerGroupBox.Controls.Add(this.SelectionStatsLabel);
			this.SplicerGroupBox.Controls.Add(this.ClipboardNameLabel);
			this.SplicerGroupBox.Controls.Add(this.SelectionNameLabel);
			this.SplicerGroupBox.Location = new System.Drawing.Point(3, 3);
			this.SplicerGroupBox.Name = "SplicerGroupBox";
			this.SplicerGroupBox.Size = new System.Drawing.Size(198, 59);
			this.SplicerGroupBox.TabIndex = 0;
			this.SplicerGroupBox.TabStop = false;
			this.SplicerGroupBox.Text = "Splicer";
			// 
			// ClipboardStatsLabel
			// 
			this.ClipboardStatsLabel.AutoSize = true;
			this.ClipboardStatsLabel.Enabled = false;
			this.ClipboardStatsLabel.Location = new System.Drawing.Point(66, 33);
			this.ClipboardStatsLabel.Name = "ClipboardStatsLabel";
			this.ClipboardStatsLabel.Size = new System.Drawing.Size(35, 13);
			this.ClipboardStatsLabel.TabIndex = 3;
			this.ClipboardStatsLabel.Text = "empty";
			// 
			// SelectionStatsLabel
			// 
			this.SelectionStatsLabel.AutoSize = true;
			this.SelectionStatsLabel.Enabled = false;
			this.SelectionStatsLabel.Location = new System.Drawing.Point(66, 16);
			this.SelectionStatsLabel.Name = "SelectionStatsLabel";
			this.SelectionStatsLabel.Size = new System.Drawing.Size(87, 13);
			this.SelectionStatsLabel.TabIndex = 2;
			this.SelectionStatsLabel.Text = "1 row, 8 columns";
			// 
			// ClipboardNameLabel
			// 
			this.ClipboardNameLabel.AutoSize = true;
			this.ClipboardNameLabel.Location = new System.Drawing.Point(6, 33);
			this.ClipboardNameLabel.Name = "ClipboardNameLabel";
			this.ClipboardNameLabel.Size = new System.Drawing.Size(54, 13);
			this.ClipboardNameLabel.TabIndex = 1;
			this.ClipboardNameLabel.Text = "Clipboard:";
			// 
			// SelectionNameLabel
			// 
			this.SelectionNameLabel.AutoSize = true;
			this.SelectionNameLabel.Location = new System.Drawing.Point(6, 16);
			this.SelectionNameLabel.Name = "SelectionNameLabel";
			this.SelectionNameLabel.Size = new System.Drawing.Size(54, 13);
			this.SelectionNameLabel.TabIndex = 0;
			this.SelectionNameLabel.Text = "Selection:";
			// 
			// SplicerBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.SplicerGroupBox);
			this.Name = "SplicerBox";
			this.Size = new System.Drawing.Size(204, 65);
			this.SplicerGroupBox.ResumeLayout(false);
			this.SplicerGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox SplicerGroupBox;
		private System.Windows.Forms.Label ClipboardStatsLabel;
		private System.Windows.Forms.Label SelectionStatsLabel;
		private System.Windows.Forms.Label ClipboardNameLabel;
		private System.Windows.Forms.Label SelectionNameLabel;
	}
}
