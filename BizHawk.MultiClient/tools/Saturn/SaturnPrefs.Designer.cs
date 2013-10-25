namespace BizHawk.MultiClient.SATTools
{
	partial class SaturnPrefs
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
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.radioButtonGL = new System.Windows.Forms.RadioButton();
			this.radioButtonSoft = new System.Windows.Forms.RadioButton();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.numericUpDownFactor = new System.Windows.Forms.NumericUpDown();
			this.radioButtonFree = new System.Windows.Forms.RadioButton();
			this.radioButtonFactor = new System.Windows.Forms.RadioButton();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownFactor)).BeginInit();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.radioButtonGL);
			this.groupBox1.Controls.Add(this.radioButtonSoft);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(111, 100);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Render Type";
			// 
			// radioButtonGL
			// 
			this.radioButtonGL.AutoSize = true;
			this.radioButtonGL.Location = new System.Drawing.Point(6, 42);
			this.radioButtonGL.Name = "radioButtonGL";
			this.radioButtonGL.Size = new System.Drawing.Size(68, 17);
			this.radioButtonGL.TabIndex = 1;
			this.radioButtonGL.TabStop = true;
			this.radioButtonGL.Text = "Open GL";
			this.radioButtonGL.UseVisualStyleBackColor = true;
			// 
			// radioButtonSoft
			// 
			this.radioButtonSoft.AutoSize = true;
			this.radioButtonSoft.Location = new System.Drawing.Point(6, 19);
			this.radioButtonSoft.Name = "radioButtonSoft";
			this.radioButtonSoft.Size = new System.Drawing.Size(67, 17);
			this.radioButtonSoft.TabIndex = 0;
			this.radioButtonSoft.TabStop = true;
			this.radioButtonSoft.Text = "Software";
			this.radioButtonSoft.UseVisualStyleBackColor = true;
			this.radioButtonSoft.CheckedChanged += new System.EventHandler(this.radioButtonSoft_CheckedChanged);
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.numericUpDown2);
			this.groupBox2.Controls.Add(this.numericUpDown1);
			this.groupBox2.Controls.Add(this.numericUpDownFactor);
			this.groupBox2.Controls.Add(this.radioButtonFree);
			this.groupBox2.Controls.Add(this.radioButtonFactor);
			this.groupBox2.Location = new System.Drawing.Point(129, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(215, 100);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Render Resolution";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(134, 48);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(14, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "X";
			// 
			// numericUpDown2
			// 
			this.numericUpDown2.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.numericUpDown2.Location = new System.Drawing.Point(154, 44);
			this.numericUpDown2.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.numericUpDown2.Minimum = new decimal(new int[] {
            224,
            0,
            0,
            0});
			this.numericUpDown2.Name = "numericUpDown2";
			this.numericUpDown2.Size = new System.Drawing.Size(53, 20);
			this.numericUpDown2.TabIndex = 4;
			this.numericUpDown2.Value = new decimal(new int[] {
            224,
            0,
            0,
            0});
			// 
			// numericUpDown1
			// 
			this.numericUpDown1.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.numericUpDown1.Location = new System.Drawing.Point(75, 44);
			this.numericUpDown1.Maximum = new decimal(new int[] {
            2048,
            0,
            0,
            0});
			this.numericUpDown1.Minimum = new decimal(new int[] {
            320,
            0,
            0,
            0});
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(53, 20);
			this.numericUpDown1.TabIndex = 3;
			this.numericUpDown1.Value = new decimal(new int[] {
            320,
            0,
            0,
            0});
			// 
			// numericUpDownFactor
			// 
			this.numericUpDownFactor.Location = new System.Drawing.Point(119, 19);
			this.numericUpDownFactor.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.numericUpDownFactor.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numericUpDownFactor.Name = "numericUpDownFactor";
			this.numericUpDownFactor.Size = new System.Drawing.Size(64, 20);
			this.numericUpDownFactor.TabIndex = 2;
			this.numericUpDownFactor.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// radioButtonFree
			// 
			this.radioButtonFree.AutoSize = true;
			this.radioButtonFree.Location = new System.Drawing.Point(6, 46);
			this.radioButtonFree.Name = "radioButtonFree";
			this.radioButtonFree.Size = new System.Drawing.Size(63, 17);
			this.radioButtonFree.TabIndex = 1;
			this.radioButtonFree.TabStop = true;
			this.radioButtonFree.Text = "Specific";
			this.radioButtonFree.UseVisualStyleBackColor = true;
			// 
			// radioButtonFactor
			// 
			this.radioButtonFactor.AutoSize = true;
			this.radioButtonFactor.Location = new System.Drawing.Point(6, 19);
			this.radioButtonFactor.Name = "radioButtonFactor";
			this.radioButtonFactor.Size = new System.Drawing.Size(107, 17);
			this.radioButtonFactor.TabIndex = 0;
			this.radioButtonFactor.TabStop = true;
			this.radioButtonFactor.Text = "Multiple of Native";
			this.radioButtonFactor.UseVisualStyleBackColor = true;
			this.radioButtonFactor.CheckedChanged += new System.EventHandler(this.radioButtonFactor_CheckedChanged);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.Location = new System.Drawing.Point(187, 120);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 2;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(268, 120);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 3;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// SaturnPrefs
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(355, 155);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Name = "SaturnPrefs";
			this.ShowIcon = false;
			this.Text = "Preferences";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDownFactor)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radioButtonGL;
		private System.Windows.Forms.RadioButton radioButtonSoft;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown numericUpDown2;
		private System.Windows.Forms.NumericUpDown numericUpDown1;
		private System.Windows.Forms.NumericUpDown numericUpDownFactor;
		private System.Windows.Forms.RadioButton radioButtonFree;
		private System.Windows.Forms.RadioButton radioButtonFactor;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
	}
}