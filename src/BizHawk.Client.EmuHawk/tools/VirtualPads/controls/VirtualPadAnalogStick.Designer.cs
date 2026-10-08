namespace BizHawk.Client.EmuHawk
{
	partial class VirtualPadAnalogStick
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtualPadAnalogStick));
            this.XLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ManualX = new System.Windows.Forms.NumericUpDown();
            this.YLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ManualY = new System.Windows.Forms.NumericUpDown();
            this.MaxLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MaxXNumeric = new System.Windows.Forms.NumericUpDown();
            this.MaxYNumeric = new System.Windows.Forms.NumericUpDown();
            this.rLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.manualR = new System.Windows.Forms.NumericUpDown();
            this.manualTheta = new System.Windows.Forms.NumericUpDown();
            this.thetaLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AnalogStick = new BizHawk.Client.EmuHawk.AnalogStickPanel();
            ((System.ComponentModel.ISupportInitialize)(this.ManualX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ManualY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MaxXNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MaxYNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.manualR)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.manualTheta)).BeginInit();
            this.SuspendLayout();
            // 
            // XLabel
            // 
            resources.ApplyResources(this.XLabel, "XLabel");
            this.XLabel.Name = "XLabel";
            // 
            // ManualX
            // 
            resources.ApplyResources(this.ManualX, "ManualX");
            this.ManualX.Name = "ManualX";
            this.ManualX.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ManualXY_ValueChanged);
            // 
            // YLabel
            // 
            resources.ApplyResources(this.YLabel, "YLabel");
            this.YLabel.Name = "YLabel";
            // 
            // ManualY
            // 
            resources.ApplyResources(this.ManualY, "ManualY");
            this.ManualY.Name = "ManualY";
            this.ManualY.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ManualXY_ValueChanged);
            // 
            // MaxLabel
            // 
            resources.ApplyResources(this.MaxLabel, "MaxLabel");
            this.MaxLabel.Name = "MaxLabel";
            // 
            // MaxXNumeric
            // 
            resources.ApplyResources(this.MaxXNumeric, "MaxXNumeric");
            this.MaxXNumeric.Name = "MaxXNumeric";
            this.MaxXNumeric.ValueChanged += new System.EventHandler(this.MaxManualXY_ValueChanged);
            this.MaxXNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MaxManualXY_ValueChanged);
            // 
            // MaxYNumeric
            // 
            resources.ApplyResources(this.MaxYNumeric, "MaxYNumeric");
            this.MaxYNumeric.Name = "MaxYNumeric";
            this.MaxYNumeric.ValueChanged += new System.EventHandler(this.MaxManualXY_ValueChanged);
            this.MaxYNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MaxManualXY_ValueChanged);
            // 
            // rLabel
            // 
            resources.ApplyResources(this.rLabel, "rLabel");
            this.rLabel.Name = "rLabel";
            // 
            // manualR
            // 
            resources.ApplyResources(this.manualR, "manualR");
            this.manualR.Maximum = new decimal(new int[] {
            1810,
            0,
            0,
            65536});
            this.manualR.Name = "manualR";
            // 
            // manualTheta
            // 
            resources.ApplyResources(this.manualTheta, "manualTheta");
            this.manualTheta.Maximum = new decimal(new int[] {
            3590,
            0,
            0,
            65536});
            this.manualTheta.Name = "manualTheta";
            // 
            // thetaLabel
            // 
            resources.ApplyResources(this.thetaLabel, "thetaLabel");
            this.thetaLabel.Name = "thetaLabel";
            // 
            // AnalogStick
            // 
            resources.ApplyResources(this.AnalogStick, "AnalogStick");
            this.AnalogStick.BackColor = System.Drawing.Color.Gray;
            this.AnalogStick.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.AnalogStick.Name = "AnalogStick";
            this.AnalogStick.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AnalogStick_MouseDown);
            this.AnalogStick.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AnalogStick_MouseMove);
            // 
            // VirtualPadAnalogStick
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.manualTheta);
            this.Controls.Add(this.thetaLabel);
            this.Controls.Add(this.manualR);
            this.Controls.Add(this.rLabel);
            this.Controls.Add(this.MaxYNumeric);
            this.Controls.Add(this.MaxXNumeric);
            this.Controls.Add(this.MaxLabel);
            this.Controls.Add(this.YLabel);
            this.Controls.Add(this.ManualY);
            this.Controls.Add(this.ManualX);
            this.Controls.Add(this.XLabel);
            this.Controls.Add(this.AnalogStick);
            this.Name = "VirtualPadAnalogStick";
            ((System.ComponentModel.ISupportInitialize)(this.ManualX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ManualY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MaxXNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MaxYNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.manualR)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.manualTheta)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private AnalogStickPanel AnalogStick;
		private BizHawk.WinForms.Controls.LocLabelEx XLabel;
		private System.Windows.Forms.NumericUpDown ManualX;
		private BizHawk.WinForms.Controls.LocLabelEx YLabel;
		private System.Windows.Forms.NumericUpDown ManualY;
		private BizHawk.WinForms.Controls.LocLabelEx MaxLabel;
		private System.Windows.Forms.NumericUpDown MaxXNumeric;
		private System.Windows.Forms.NumericUpDown MaxYNumeric;
		private BizHawk.WinForms.Controls.LocLabelEx rLabel;
		private System.Windows.Forms.NumericUpDown manualR;
		private System.Windows.Forms.NumericUpDown manualTheta;
		private BizHawk.WinForms.Controls.LocLabelEx thetaLabel;
	}
}
