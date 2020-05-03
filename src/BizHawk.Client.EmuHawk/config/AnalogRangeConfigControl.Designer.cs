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
			this.XNumeric = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.YNumeric = new System.Windows.Forms.NumericUpDown();
			this.RadialCheckbox = new System.Windows.Forms.CheckBox();
			this.AnalogRange = new BizHawk.Client.EmuHawk.AnalogRangeConfig();
			((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// XNumeric
			// 
			this.XNumeric.Location = new System.Drawing.Point(86, 5);
			this.XNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
			this.XNumeric.Name = "XNumeric";
			this.XNumeric.Size = new System.Drawing.Size(45, 20);
			this.XNumeric.TabIndex = 1;
			this.XNumeric.ValueChanged += new System.EventHandler(this.XNumeric_ValueChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(71, 30);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(14, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Y";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(71, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(14, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "X";
			// 
			// YNumeric
			// 
			this.YNumeric.Location = new System.Drawing.Point(86, 26);
			this.YNumeric.Maximum = new decimal(new int[] {
            127,
            0,
            0,
            0});
			this.YNumeric.Name = "YNumeric";
			this.YNumeric.Size = new System.Drawing.Size(45, 20);
			this.YNumeric.TabIndex = 2;
			this.YNumeric.ValueChanged += new System.EventHandler(this.YNumeric_ValueChanged);
			// 
			// RadialCheckbox
			// 
			this.RadialCheckbox.Appearance = System.Windows.Forms.Appearance.Button;
			this.RadialCheckbox.AutoSize = true;
			this.RadialCheckbox.Location = new System.Drawing.Point(84, 47);
			this.RadialCheckbox.Name = "RadialCheckbox";
			this.RadialCheckbox.Size = new System.Drawing.Size(47, 23);
			this.RadialCheckbox.TabIndex = 5;
			this.RadialCheckbox.Text = "Radial";
			this.RadialCheckbox.UseVisualStyleBackColor = true;
			this.RadialCheckbox.CheckedChanged += new System.EventHandler(this.RadialCheckbox_CheckedChanged);
			// 
			// AnalogRange
			// 
			this.AnalogRange.BackColor = System.Drawing.Color.Gray;
			this.AnalogRange.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.AnalogRange.ChangeCallback = null;
			this.AnalogRange.Location = new System.Drawing.Point(5, 5);
			this.AnalogRange.MaxX = 0;
			this.AnalogRange.MaxY = 0;
			this.AnalogRange.Name = "AnalogRange";
			this.AnalogRange.Radial = false;
			this.AnalogRange.Size = new System.Drawing.Size(65, 65);
			this.AnalogRange.TabIndex = 0;
			// 
			// AnalogRangeConfigControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.RadialCheckbox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.YNumeric);
			this.Controls.Add(this.XNumeric);
			this.Controls.Add(this.AnalogRange);
			this.Name = "AnalogRangeConfigControl";
			this.Size = new System.Drawing.Size(135, 76);
			this.Load += new System.EventHandler(this.AnalogRangeConfigControl_Load);
			((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private AnalogRangeConfig AnalogRange;
		private System.Windows.Forms.NumericUpDown XNumeric;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown YNumeric;
		private System.Windows.Forms.CheckBox RadialCheckbox;

	}
}
