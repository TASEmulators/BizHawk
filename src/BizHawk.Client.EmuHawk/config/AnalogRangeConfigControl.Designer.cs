namespace BizHawk.Client.EmuHawk
{
	partial class AnalogRangeConfigControl
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnalogRangeConfigControl));
            this.XNumeric = new System.Windows.Forms.NumericUpDown();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.YNumeric = new System.Windows.Forms.NumericUpDown();
            this.RadialCheckbox = new System.Windows.Forms.CheckBox();
            this.AnalogRange = new BizHawk.Client.EmuHawk.AnalogRangeConfig();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // XNumeric
            // 
            resources.ApplyResources(this.XNumeric, "XNumeric");
            this.XNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.XNumeric.Name = "XNumeric";
            this.XNumeric.ValueChanged += new System.EventHandler(this.XNumeric_ValueChanged);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // YNumeric
            // 
            resources.ApplyResources(this.YNumeric, "YNumeric");
            this.YNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
            this.YNumeric.Name = "YNumeric";
            this.YNumeric.ValueChanged += new System.EventHandler(this.YNumeric_ValueChanged);
            // 
            // RadialCheckbox
            // 
            resources.ApplyResources(this.RadialCheckbox, "RadialCheckbox");
            this.RadialCheckbox.Name = "RadialCheckbox";
            this.RadialCheckbox.UseVisualStyleBackColor = true;
            this.RadialCheckbox.CheckedChanged += new System.EventHandler(this.RadialCheckbox_CheckedChanged);
            // 
            // AnalogRange
            // 
            resources.ApplyResources(this.AnalogRange, "AnalogRange");
            this.AnalogRange.BackColor = System.Drawing.Color.Gray;
            this.AnalogRange.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.AnalogRange.ChangeCallback = null;
            this.AnalogRange.MaxX = 0;
            this.AnalogRange.MaxY = 0;
            this.AnalogRange.Name = "AnalogRange";
            this.AnalogRange.Radial = false;
            // 
            // AnalogRangeConfigControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.RadialCheckbox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.YNumeric);
            this.Controls.Add(this.XNumeric);
            this.Controls.Add(this.AnalogRange);
            this.Name = "AnalogRangeConfigControl";
            this.Load += new System.EventHandler(this.AnalogRangeConfigControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private AnalogRangeConfig AnalogRange;
		private System.Windows.Forms.NumericUpDown XNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.NumericUpDown YNumeric;
		private System.Windows.Forms.CheckBox RadialCheckbox;

	}
}
