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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ColorRow));
            this.DisplayNameLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ColorPanel = new System.Windows.Forms.Panel();
            this.HexLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ColorText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // DisplayNameLabel
            // 
            resources.ApplyResources(this.DisplayNameLabel, "DisplayNameLabel");
            this.DisplayNameLabel.Name = "DisplayNameLabel";
            // 
            // ColorPanel
            // 
            resources.ApplyResources(this.ColorPanel, "ColorPanel");
            this.ColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ColorPanel.Name = "ColorPanel";
            this.ColorPanel.Click += new System.EventHandler(this.ColorPanel_Click);
            // 
            // HexLabel
            // 
            resources.ApplyResources(this.HexLabel, "HexLabel");
            this.HexLabel.Name = "HexLabel";
            // 
            // ColorText
            // 
            resources.ApplyResources(this.ColorText, "ColorText");
            this.ColorText.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ColorText.Name = "ColorText";
            this.ColorText.ReadOnly = true;
            // 
            // ColorRow
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ColorText);
            this.Controls.Add(this.HexLabel);
            this.Controls.Add(this.ColorPanel);
            this.Controls.Add(this.DisplayNameLabel);
            this.Name = "ColorRow";
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
