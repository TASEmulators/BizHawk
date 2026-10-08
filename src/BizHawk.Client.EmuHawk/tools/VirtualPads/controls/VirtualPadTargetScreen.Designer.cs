namespace BizHawk.Client.EmuHawk
{
	partial class VirtualPadTargetScreen
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtualPadTargetScreen));
            this.TargetPanel = new System.Windows.Forms.Panel();
            this.XNumeric = new System.Windows.Forms.NumericUpDown();
            this.XLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.YLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.YNumeric = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // TargetPanel
            // 
            resources.ApplyResources(this.TargetPanel, "TargetPanel");
            this.TargetPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.TargetPanel.Name = "TargetPanel";
            this.TargetPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.TargetPanel_Paint);
            this.TargetPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TargetPanel_MouseDown);
            this.TargetPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.TargetPanel_MouseMove);
            this.TargetPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TargetPanel_MouseUp);
            // 
            // XNumeric
            // 
            resources.ApplyResources(this.XNumeric, "XNumeric");
            this.XNumeric.Name = "XNumeric";
            this.XNumeric.ValueChanged += new System.EventHandler(this.XNumeric_ValueChanged);
            this.XNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.XNumeric_KeyUp);
            // 
            // XLabel
            // 
            resources.ApplyResources(this.XLabel, "XLabel");
            this.XLabel.Name = "XLabel";
            // 
            // YLabel
            // 
            resources.ApplyResources(this.YLabel, "YLabel");
            this.YLabel.Name = "YLabel";
            // 
            // YNumeric
            // 
            resources.ApplyResources(this.YNumeric, "YNumeric");
            this.YNumeric.Name = "YNumeric";
            this.YNumeric.ValueChanged += new System.EventHandler(this.YNumeric_ValueChanged);
            this.YNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.YNumeric_KeyUp);
            // 
            // VirtualPadTargetScreen
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.YLabel);
            this.Controls.Add(this.YNumeric);
            this.Controls.Add(this.XLabel);
            this.Controls.Add(this.XNumeric);
            this.Controls.Add(this.TargetPanel);
            this.Name = "VirtualPadTargetScreen";
            this.Load += new System.EventHandler(this.VirtualPadTargetScreen_Load);
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel TargetPanel;
		private System.Windows.Forms.NumericUpDown XNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx XLabel;
		private BizHawk.WinForms.Controls.LocLabelEx YLabel;
		private System.Windows.Forms.NumericUpDown YNumeric;
	}
}
