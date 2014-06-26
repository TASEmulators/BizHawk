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
			this.XLabel = new System.Windows.Forms.Label();
			this.ManualX = new System.Windows.Forms.NumericUpDown();
			this.YLabel = new System.Windows.Forms.Label();
			this.ManualY = new System.Windows.Forms.NumericUpDown();
			this.MaxLabel = new System.Windows.Forms.Label();
			this.MaxXNumeric = new System.Windows.Forms.NumericUpDown();
			this.MaxYNumeric = new System.Windows.Forms.NumericUpDown();
			this.AnalogStick = new BizHawk.Client.EmuHawk.AnalogStickPanel();
			((System.ComponentModel.ISupportInitialize)(this.ManualX)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.ManualY)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MaxXNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MaxYNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// XLabel
			// 
			this.XLabel.AutoSize = true;
			this.XLabel.Location = new System.Drawing.Point(138, 7);
			this.XLabel.Name = "XLabel";
			this.XLabel.Size = new System.Drawing.Size(14, 13);
			this.XLabel.TabIndex = 23;
			this.XLabel.Text = "X";
			// 
			// ManualX
			// 
			this.ManualX.Location = new System.Drawing.Point(156, 3);
			this.ManualX.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
			this.ManualX.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
			this.ManualX.Name = "ManualX";
			this.ManualX.Size = new System.Drawing.Size(44, 20);
			this.ManualX.TabIndex = 24;
			this.ManualX.ValueChanged += new System.EventHandler(this.ManualX_ValueChanged);
			this.ManualX.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ManualX_KeyUp);
			// 
			// YLabel
			// 
			this.YLabel.AutoSize = true;
			this.YLabel.Location = new System.Drawing.Point(138, 33);
			this.YLabel.Name = "YLabel";
			this.YLabel.Size = new System.Drawing.Size(14, 13);
			this.YLabel.TabIndex = 26;
			this.YLabel.Text = "Y";
			// 
			// ManualY
			// 
			this.ManualY.Location = new System.Drawing.Point(156, 29);
			this.ManualY.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
			this.ManualY.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
			this.ManualY.Name = "ManualY";
			this.ManualY.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.ManualY.Size = new System.Drawing.Size(44, 20);
			this.ManualY.TabIndex = 25;
			this.ManualY.ValueChanged += new System.EventHandler(this.ManualY_ValueChanged);
			this.ManualY.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ManualY_KeyUp);
			// 
			// MaxLabel
			// 
			this.MaxLabel.AutoSize = true;
			this.MaxLabel.Location = new System.Drawing.Point(138, 72);
			this.MaxLabel.Name = "MaxLabel";
			this.MaxLabel.Size = new System.Drawing.Size(27, 13);
			this.MaxLabel.TabIndex = 27;
			this.MaxLabel.Text = "Max";
			// 
			// MaxXNumeric
			// 
			this.MaxXNumeric.Location = new System.Drawing.Point(138, 89);
			this.MaxXNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
			this.MaxXNumeric.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
			this.MaxXNumeric.Name = "MaxXNumeric";
			this.MaxXNumeric.Size = new System.Drawing.Size(44, 20);
			this.MaxXNumeric.TabIndex = 28;
			this.MaxXNumeric.ValueChanged += new System.EventHandler(this.MaxXNumeric_ValueChanged);
			this.MaxXNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MaxXNumeric_KeyUp);
			// 
			// MaxYNumeric
			// 
			this.MaxYNumeric.Location = new System.Drawing.Point(138, 112);
			this.MaxYNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
			this.MaxYNumeric.Minimum = new decimal(new int[] {
            127,
            0,
            0,
            -2147483648});
			this.MaxYNumeric.Name = "MaxYNumeric";
			this.MaxYNumeric.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.MaxYNumeric.Size = new System.Drawing.Size(44, 20);
			this.MaxYNumeric.TabIndex = 29;
			this.MaxYNumeric.ValueChanged += new System.EventHandler(this.MaxYNumeric_ValueChanged);
			this.MaxYNumeric.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MaxYNumeric_KeyUp);
			// 
			// AnalogStick
			// 
			this.AnalogStick.BackColor = System.Drawing.Color.Gray;
			this.AnalogStick.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.AnalogStick.Location = new System.Drawing.Point(3, 3);
			this.AnalogStick.MaxX = 127;
			this.AnalogStick.MaxY = 127;
			this.AnalogStick.Name = "AnalogStick";
			this.AnalogStick.Size = new System.Drawing.Size(129, 129);
			this.AnalogStick.TabIndex = 0;
			this.AnalogStick.MouseDown += new System.Windows.Forms.MouseEventHandler(this.AnalogStick_MouseDown);
			this.AnalogStick.MouseMove += new System.Windows.Forms.MouseEventHandler(this.AnalogStick_MouseMove);
			// 
			// VirtualPadAnalogStick
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.MaxYNumeric);
			this.Controls.Add(this.MaxXNumeric);
			this.Controls.Add(this.MaxLabel);
			this.Controls.Add(this.YLabel);
			this.Controls.Add(this.ManualY);
			this.Controls.Add(this.ManualX);
			this.Controls.Add(this.XLabel);
			this.Controls.Add(this.AnalogStick);
			this.Name = "VirtualPadAnalogStick";
			this.Size = new System.Drawing.Size(204, 136);
			this.Load += new System.EventHandler(this.VirtualPadAnalogStick_Load);
			((System.ComponentModel.ISupportInitialize)(this.ManualX)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.ManualY)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MaxXNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MaxYNumeric)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private AnalogStickPanel AnalogStick;
		private System.Windows.Forms.Label XLabel;
		private System.Windows.Forms.NumericUpDown ManualX;
		private System.Windows.Forms.Label YLabel;
		private System.Windows.Forms.NumericUpDown ManualY;
		private System.Windows.Forms.Label MaxLabel;
		private System.Windows.Forms.NumericUpDown MaxXNumeric;
		private System.Windows.Forms.NumericUpDown MaxYNumeric;
	}
}
