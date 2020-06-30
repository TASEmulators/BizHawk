namespace BizHawk.Client.EmuHawk
{
	partial class ColorRow
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
            this.DisplayNameLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ColorPanel = new System.Windows.Forms.Panel();
            this.HexLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ColorText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // DisplayNameLabel
            // 
            this.DisplayNameLabel.Location = new System.Drawing.Point(3, 0);
            this.DisplayNameLabel.Name = "DisplayNameLabel";
            this.DisplayNameLabel.Text = "Messages";
            // 
            // ColorPanel
            // 
            this.ColorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ColorPanel.Location = new System.Drawing.Point(6, 16);
            this.ColorPanel.Name = "ColorPanel";
            this.ColorPanel.Size = new System.Drawing.Size(46, 20);
            this.ColorPanel.TabIndex = 8;
            this.ColorPanel.Click += new System.EventHandler(this.ColorPanel_Click);
            // 
            // HexLabel
            // 
            this.HexLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.HexLabel.Location = new System.Drawing.Point(55, 20);
            this.HexLabel.Margin = new System.Windows.Forms.Padding(0);
            this.HexLabel.Name = "HexLabel";
            this.HexLabel.Text = "0x";
            // 
            // ColorText
            // 
            this.ColorText.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ColorText.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ColorText.Location = new System.Drawing.Point(75, 16);
            this.ColorText.MaxLength = 8;
            this.ColorText.Name = "ColorText";
            this.ColorText.ReadOnly = true;
            this.ColorText.Size = new System.Drawing.Size(59, 20);
            this.ColorText.TabIndex = 11;
            // 
            // ColorRow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ColorText);
            this.Controls.Add(this.HexLabel);
            this.Controls.Add(this.ColorPanel);
            this.Controls.Add(this.DisplayNameLabel);
            this.Name = "ColorRow";
            this.Size = new System.Drawing.Size(143, 41);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private WinForms.Controls.LocLabelEx DisplayNameLabel;
		private System.Windows.Forms.Panel ColorPanel;
		private WinForms.Controls.LocLabelEx HexLabel;
		private System.Windows.Forms.TextBox ColorText;
	}
}
