namespace BizHawk.Client.EmuHawk
{
	partial class NDSSettings
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
			this.components = new System.ComponentModel.Container();
			this.chkBootToFirmware = new System.Windows.Forms.CheckBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnSave = new System.Windows.Forms.Button();
			this.ttipFirmware = new System.Windows.Forms.ToolTip(this.components);
			this.txtName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.numBirthDay = new System.Windows.Forms.NumericUpDown();
			this.numBirthMonth = new System.Windows.Forms.NumericUpDown();
			this.label3 = new System.Windows.Forms.Label();
			this.cbxFavColor = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.btnDefault = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numBirthDay)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numBirthMonth)).BeginInit();
			this.SuspendLayout();
			// 
			// chkBootToFirmware
			// 
			this.chkBootToFirmware.AutoSize = true;
			this.chkBootToFirmware.Location = new System.Drawing.Point(12, 12);
			this.chkBootToFirmware.Name = "chkBootToFirmware";
			this.chkBootToFirmware.Size = new System.Drawing.Size(102, 17);
			this.chkBootToFirmware.TabIndex = 0;
			this.chkBootToFirmware.Text = "Boot to firmware";
			this.ttipFirmware.SetToolTip(this.chkBootToFirmware, "This option requires that a firmware file be in use, as well as both bios files. " +
        "See Config -> Firmwares.");
			this.chkBootToFirmware.UseVisualStyleBackColor = true;
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.Location = new System.Drawing.Point(144, 147);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(60, 23);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// btnSave
			// 
			this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSave.Location = new System.Drawing.Point(78, 147);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(60, 23);
			this.btnSave.TabIndex = 1;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// txtName
			// 
			this.txtName.Location = new System.Drawing.Point(50, 16);
			this.txtName.MaxLength = 10;
			this.txtName.Name = "txtName";
			this.txtName.Size = new System.Drawing.Size(94, 20);
			this.txtName.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(6, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(38, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Name:";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.numBirthDay);
			this.groupBox1.Controls.Add(this.numBirthMonth);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.cbxFavColor);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.txtName);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Location = new System.Drawing.Point(12, 35);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(192, 106);
			this.groupBox1.TabIndex = 4;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "groupBox1";
			// 
			// numBirthDay
			// 
			this.numBirthDay.Location = new System.Drawing.Point(133, 69);
			this.numBirthDay.Maximum = new decimal(new int[] {
            31,
            0,
            0,
            0});
			this.numBirthDay.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numBirthDay.Name = "numBirthDay";
			this.numBirthDay.Size = new System.Drawing.Size(36, 20);
			this.numBirthDay.TabIndex = 7;
			this.numBirthDay.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// numBirthMonth
			// 
			this.numBirthMonth.Location = new System.Drawing.Point(91, 69);
			this.numBirthMonth.Maximum = new decimal(new int[] {
            12,
            0,
            0,
            0});
			this.numBirthMonth.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numBirthMonth.Name = "numBirthMonth";
			this.numBirthMonth.Size = new System.Drawing.Size(36, 20);
			this.numBirthMonth.TabIndex = 6;
			this.numBirthMonth.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numBirthMonth.ValueChanged += new System.EventHandler(this.numBirthMonth_ValueChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(6, 71);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(79, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Birthday (M/D):";
			// 
			// cbxFavColor
			// 
			this.cbxFavColor.FormattingEnabled = true;
			this.cbxFavColor.Items.AddRange(new object[] {
            "Gray",
            "Brown",
            "Red",
            "Pink",
            "Orange",
            "Yellow",
            "Lime Green",
            "Green",
            "Dark Green",
            "Sea Green",
            "Turquoise",
            "Blue",
            "Dark Blue",
            "Dark Purple",
            "Violet",
            "Magenta"});
			this.cbxFavColor.Location = new System.Drawing.Point(50, 42);
			this.cbxFavColor.Name = "cbxFavColor";
			this.cbxFavColor.Size = new System.Drawing.Size(94, 21);
			this.cbxFavColor.TabIndex = 5;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 45);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(34, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Color:";
			// 
			// btnDefault
			// 
			this.btnDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnDefault.Location = new System.Drawing.Point(12, 147);
			this.btnDefault.Name = "btnDefault";
			this.btnDefault.Size = new System.Drawing.Size(60, 23);
			this.btnDefault.TabIndex = 1;
			this.btnDefault.Text = "Default";
			this.btnDefault.UseVisualStyleBackColor = true;
			this.btnDefault.Click += new System.EventHandler(this.btnDefault_Click);
			// 
			// NDSSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(216, 182);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnDefault);
			this.Controls.Add(this.btnSave);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.chkBootToFirmware);
			this.Name = "NDSSettings";
			this.Text = "NDSSettings";
			this.Load += new System.EventHandler(this.NDSSettings_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numBirthDay)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numBirthMonth)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox chkBootToFirmware;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnSave;
		private System.Windows.Forms.ToolTip ttipFirmware;
		private System.Windows.Forms.TextBox txtName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox cbxFavColor;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.NumericUpDown numBirthDay;
		private System.Windows.Forms.NumericUpDown numBirthMonth;
		private System.Windows.Forms.Button btnDefault;
	}
}