namespace BizHawk.MultiClient
{
	partial class RewindConfig
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
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.SmallLabel1 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label10 = new System.Windows.Forms.Label();
			this.LargeStateEnabledBox = new System.Windows.Forms.CheckBox();
			this.MediumStateEnabledBox = new System.Windows.Forms.CheckBox();
			this.SmallStateEnabledBox = new System.Windows.Forms.CheckBox();
			this.LargeLabel2 = new System.Windows.Forms.Label();
			this.LargeLabel3 = new System.Windows.Forms.Label();
			this.LargeSavestateNumeric = new System.Windows.Forms.NumericUpDown();
			this.LargeLabel1 = new System.Windows.Forms.Label();
			this.MediumLabel2 = new System.Windows.Forms.Label();
			this.MediumLabel3 = new System.Windows.Forms.Label();
			this.MediumSavestateNumeric = new System.Windows.Forms.NumericUpDown();
			this.MediumLabel1 = new System.Windows.Forms.Label();
			this.SmallLabel2 = new System.Windows.Forms.Label();
			this.SmallLabel3 = new System.Windows.Forms.Label();
			this.SmallSavestateNumeric = new System.Windows.Forms.NumericUpDown();
			this.UseDeltaCompression = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.StateSizeLabel = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.LargeSavestateNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.MediumSavestateNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.SmallSavestateNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(215, 186);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(296, 186);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// SmallLabel1
			// 
			this.SmallLabel1.AutoSize = true;
			this.SmallLabel1.Location = new System.Drawing.Point(40, 40);
			this.SmallLabel1.Name = "SmallLabel1";
			this.SmallLabel1.Size = new System.Drawing.Size(164, 13);
			this.SmallLabel1.TabIndex = 2;
			this.SmallLabel1.Text = "Small savestates (less than 32kb)";
			this.SmallLabel1.Click += new System.EventHandler(this.SmallLabel1_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.label10);
			this.groupBox1.Controls.Add(this.LargeStateEnabledBox);
			this.groupBox1.Controls.Add(this.MediumStateEnabledBox);
			this.groupBox1.Controls.Add(this.SmallStateEnabledBox);
			this.groupBox1.Controls.Add(this.LargeLabel2);
			this.groupBox1.Controls.Add(this.LargeLabel3);
			this.groupBox1.Controls.Add(this.LargeSavestateNumeric);
			this.groupBox1.Controls.Add(this.LargeLabel1);
			this.groupBox1.Controls.Add(this.MediumLabel2);
			this.groupBox1.Controls.Add(this.MediumLabel3);
			this.groupBox1.Controls.Add(this.MediumSavestateNumeric);
			this.groupBox1.Controls.Add(this.MediumLabel1);
			this.groupBox1.Controls.Add(this.SmallLabel2);
			this.groupBox1.Controls.Add(this.SmallLabel3);
			this.groupBox1.Controls.Add(this.SmallSavestateNumeric);
			this.groupBox1.Controls.Add(this.SmallLabel1);
			this.groupBox1.Location = new System.Drawing.Point(12, 38);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(359, 118);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Rewind frequency";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(6, 22);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(40, 13);
			this.label10.TabIndex = 17;
			this.label10.Text = "Enable";
			// 
			// LargeStateEnabledBox
			// 
			this.LargeStateEnabledBox.AutoSize = true;
			this.LargeStateEnabledBox.Location = new System.Drawing.Point(9, 87);
			this.LargeStateEnabledBox.Name = "LargeStateEnabledBox";
			this.LargeStateEnabledBox.Size = new System.Drawing.Size(15, 14);
			this.LargeStateEnabledBox.TabIndex = 16;
			this.LargeStateEnabledBox.UseVisualStyleBackColor = true;
			this.LargeStateEnabledBox.CheckStateChanged += new System.EventHandler(this.LargeStateEnabledBox_CheckStateChanged);
			// 
			// MediumStateEnabledBox
			// 
			this.MediumStateEnabledBox.AutoSize = true;
			this.MediumStateEnabledBox.Location = new System.Drawing.Point(9, 63);
			this.MediumStateEnabledBox.Name = "MediumStateEnabledBox";
			this.MediumStateEnabledBox.Size = new System.Drawing.Size(15, 14);
			this.MediumStateEnabledBox.TabIndex = 15;
			this.MediumStateEnabledBox.UseVisualStyleBackColor = true;
			this.MediumStateEnabledBox.CheckStateChanged += new System.EventHandler(this.MediumStateEnabledBox_CheckStateChanged);
			// 
			// SmallStateEnabledBox
			// 
			this.SmallStateEnabledBox.AutoSize = true;
			this.SmallStateEnabledBox.Location = new System.Drawing.Point(9, 39);
			this.SmallStateEnabledBox.Name = "SmallStateEnabledBox";
			this.SmallStateEnabledBox.Size = new System.Drawing.Size(15, 14);
			this.SmallStateEnabledBox.TabIndex = 14;
			this.SmallStateEnabledBox.UseVisualStyleBackColor = true;
			this.SmallStateEnabledBox.CheckStateChanged += new System.EventHandler(this.SmallStateEnabledBox_CheckStateChanged);
			// 
			// LargeLabel2
			// 
			this.LargeLabel2.AutoSize = true;
			this.LargeLabel2.Location = new System.Drawing.Point(227, 88);
			this.LargeLabel2.Name = "LargeLabel2";
			this.LargeLabel2.Size = new System.Drawing.Size(33, 13);
			this.LargeLabel2.TabIndex = 13;
			this.LargeLabel2.Text = "every";
			// 
			// LargeLabel3
			// 
			this.LargeLabel3.AutoSize = true;
			this.LargeLabel3.Location = new System.Drawing.Point(307, 88);
			this.LargeLabel3.Name = "LargeLabel3";
			this.LargeLabel3.Size = new System.Drawing.Size(38, 13);
			this.LargeLabel3.TabIndex = 12;
			this.LargeLabel3.Text = "frames";
			// 
			// LargeSavestateNumeric
			// 
			this.LargeSavestateNumeric.Location = new System.Drawing.Point(263, 84);
			this.LargeSavestateNumeric.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.LargeSavestateNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.LargeSavestateNumeric.Name = "LargeSavestateNumeric";
			this.LargeSavestateNumeric.Size = new System.Drawing.Size(38, 20);
			this.LargeSavestateNumeric.TabIndex = 11;
			this.LargeSavestateNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// LargeLabel1
			// 
			this.LargeLabel1.AutoSize = true;
			this.LargeLabel1.Location = new System.Drawing.Point(40, 88);
			this.LargeLabel1.Name = "LargeLabel1";
			this.LargeLabel1.Size = new System.Drawing.Size(177, 13);
			this.LargeLabel1.TabIndex = 10;
			this.LargeLabel1.Text = "Large savestates (more than 100kb)";
			this.LargeLabel1.Click += new System.EventHandler(this.LargeLabel1_Click);
			// 
			// MediumLabel2
			// 
			this.MediumLabel2.AutoSize = true;
			this.MediumLabel2.Location = new System.Drawing.Point(227, 64);
			this.MediumLabel2.Name = "MediumLabel2";
			this.MediumLabel2.Size = new System.Drawing.Size(33, 13);
			this.MediumLabel2.TabIndex = 9;
			this.MediumLabel2.Text = "every";
			// 
			// MediumLabel3
			// 
			this.MediumLabel3.AutoSize = true;
			this.MediumLabel3.Location = new System.Drawing.Point(307, 64);
			this.MediumLabel3.Name = "MediumLabel3";
			this.MediumLabel3.Size = new System.Drawing.Size(38, 13);
			this.MediumLabel3.TabIndex = 8;
			this.MediumLabel3.Text = "frames";
			// 
			// MediumSavestateNumeric
			// 
			this.MediumSavestateNumeric.Location = new System.Drawing.Point(263, 60);
			this.MediumSavestateNumeric.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.MediumSavestateNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.MediumSavestateNumeric.Name = "MediumSavestateNumeric";
			this.MediumSavestateNumeric.Size = new System.Drawing.Size(38, 20);
			this.MediumSavestateNumeric.TabIndex = 7;
			this.MediumSavestateNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// MediumLabel1
			// 
			this.MediumLabel1.AutoSize = true;
			this.MediumLabel1.Location = new System.Drawing.Point(40, 64);
			this.MediumLabel1.Name = "MediumLabel1";
			this.MediumLabel1.Size = new System.Drawing.Size(158, 13);
			this.MediumLabel1.TabIndex = 6;
			this.MediumLabel1.Text = "Medium savestates (32 - 100kb)";
			this.MediumLabel1.Click += new System.EventHandler(this.MediumLabel1_Click);
			// 
			// SmallLabel2
			// 
			this.SmallLabel2.AutoSize = true;
			this.SmallLabel2.Location = new System.Drawing.Point(227, 40);
			this.SmallLabel2.Name = "SmallLabel2";
			this.SmallLabel2.Size = new System.Drawing.Size(33, 13);
			this.SmallLabel2.TabIndex = 5;
			this.SmallLabel2.Text = "every";
			// 
			// SmallLabel3
			// 
			this.SmallLabel3.AutoSize = true;
			this.SmallLabel3.Location = new System.Drawing.Point(307, 40);
			this.SmallLabel3.Name = "SmallLabel3";
			this.SmallLabel3.Size = new System.Drawing.Size(38, 13);
			this.SmallLabel3.TabIndex = 4;
			this.SmallLabel3.Text = "frames";
			// 
			// SmallSavestateNumeric
			// 
			this.SmallSavestateNumeric.Location = new System.Drawing.Point(263, 36);
			this.SmallSavestateNumeric.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			this.SmallSavestateNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.SmallSavestateNumeric.Name = "SmallSavestateNumeric";
			this.SmallSavestateNumeric.Size = new System.Drawing.Size(38, 20);
			this.SmallSavestateNumeric.TabIndex = 3;
			this.SmallSavestateNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// UseDeltaCompression
			// 
			this.UseDeltaCompression.AutoSize = true;
			this.UseDeltaCompression.Location = new System.Drawing.Point(21, 162);
			this.UseDeltaCompression.Name = "UseDeltaCompression";
			this.UseDeltaCompression.Size = new System.Drawing.Size(133, 17);
			this.UseDeltaCompression.TabIndex = 4;
			this.UseDeltaCompression.Text = "Use delta compression";
			this.UseDeltaCompression.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(18, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(118, 13);
			this.label1.TabIndex = 5;
			this.label1.Text = "Current Savestate Size:";
			// 
			// StateSizeLabel
			// 
			this.StateSizeLabel.AutoSize = true;
			this.StateSizeLabel.Location = new System.Drawing.Point(142, 9);
			this.StateSizeLabel.Name = "StateSizeLabel";
			this.StateSizeLabel.Size = new System.Drawing.Size(28, 13);
			this.StateSizeLabel.TabIndex = 6;
			this.StateSizeLabel.Text = "0 kb";
			// 
			// RewindConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(383, 221);
			this.Controls.Add(this.StateSizeLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.UseDeltaCompression);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Name = "RewindConfig";
			this.ShowIcon = false;
			this.Text = "Rewind Settings";
			this.Load += new System.EventHandler(this.RewindConfig_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.LargeSavestateNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.MediumSavestateNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.SmallSavestateNumeric)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Label SmallLabel1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label SmallLabel2;
		private System.Windows.Forms.Label SmallLabel3;
		private System.Windows.Forms.NumericUpDown SmallSavestateNumeric;
		private System.Windows.Forms.Label LargeLabel2;
		private System.Windows.Forms.Label LargeLabel3;
		private System.Windows.Forms.NumericUpDown LargeSavestateNumeric;
		private System.Windows.Forms.Label LargeLabel1;
		private System.Windows.Forms.Label MediumLabel2;
		private System.Windows.Forms.Label MediumLabel3;
		private System.Windows.Forms.NumericUpDown MediumSavestateNumeric;
		private System.Windows.Forms.Label MediumLabel1;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.CheckBox LargeStateEnabledBox;
		private System.Windows.Forms.CheckBox MediumStateEnabledBox;
		private System.Windows.Forms.CheckBox SmallStateEnabledBox;
		private System.Windows.Forms.CheckBox UseDeltaCompression;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label StateSizeLabel;
	}
}