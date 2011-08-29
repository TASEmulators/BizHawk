namespace BizHawk.MultiClient
{
	partial class AutofireConfig
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutofireConfig));
			this.Ok = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.OnNumeric = new System.Windows.Forms.NumericUpDown();
			this.OffNumeric = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.LagFrameCheck = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.OnNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.OffNumeric)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// Ok
			// 
			this.Ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Ok.Location = new System.Drawing.Point(120, 148);
			this.Ok.Name = "Ok";
			this.Ok.Size = new System.Drawing.Size(75, 23);
			this.Ok.TabIndex = 5;
			this.Ok.Text = "&Ok";
			this.Ok.UseVisualStyleBackColor = true;
			this.Ok.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(201, 148);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 7;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OnNumeric
			// 
			this.OnNumeric.Location = new System.Drawing.Point(10, 32);
			this.OnNumeric.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.OnNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.OnNumeric.Name = "OnNumeric";
			this.OnNumeric.Size = new System.Drawing.Size(74, 20);
			this.OnNumeric.TabIndex = 2;
			this.OnNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// OffNumeric
			// 
			this.OffNumeric.Location = new System.Drawing.Point(101, 32);
			this.OffNumeric.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
			this.OffNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.OffNumeric.Name = "OffNumeric";
			this.OffNumeric.Size = new System.Drawing.Size(74, 20);
			this.OffNumeric.TabIndex = 3;
			this.OffNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(10, 16);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(21, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "On";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(101, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(21, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "Off";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.OnNumeric);
			this.groupBox1.Controls.Add(this.OffNumeric);
			this.groupBox1.Location = new System.Drawing.Point(13, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(184, 70);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Autofire Pattern";
			// 
			// LagFrameCheck
			// 
			this.LagFrameCheck.AutoSize = true;
			this.LagFrameCheck.Location = new System.Drawing.Point(13, 100);
			this.LagFrameCheck.Name = "LagFrameCheck";
			this.LagFrameCheck.Size = new System.Drawing.Size(164, 17);
			this.LagFrameCheck.TabIndex = 8;
			this.LagFrameCheck.Text = "Take lag frames into account";
			this.LagFrameCheck.UseVisualStyleBackColor = true;
			// 
			// AutofireConfig
			// 
			this.AcceptButton = this.Ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(288, 183);
			this.Controls.Add(this.LagFrameCheck);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.Ok);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(512, 512);
			this.MinimumSize = new System.Drawing.Size(218, 179);
			this.Name = "AutofireConfig";
			this.Text = "Autofire Configuration";
			this.Load += new System.EventHandler(this.AutofireConfig_Load);
			((System.ComponentModel.ISupportInitialize)(this.OnNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.OffNumeric)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.NumericUpDown OffNumeric;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox1;
		public System.Windows.Forms.NumericUpDown OnNumeric;
		private System.Windows.Forms.CheckBox LagFrameCheck;
	}
}