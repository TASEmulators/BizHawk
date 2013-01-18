namespace BizHawk.MultiClient
{
	partial class SNESOptions
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
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.rbCompatibility = new System.Windows.Forms.RadioButton();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.rbPerformance = new System.Windows.Forms.RadioButton();
			this.cbRingbuf = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(141, 172);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(222, 172);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// rbCompatibility
			// 
			this.rbCompatibility.AutoSize = true;
			this.rbCompatibility.Location = new System.Drawing.Point(6, 19);
			this.rbCompatibility.Name = "rbCompatibility";
			this.rbCompatibility.Size = new System.Drawing.Size(83, 17);
			this.rbCompatibility.TabIndex = 2;
			this.rbCompatibility.TabStop = true;
			this.rbCompatibility.Text = "Compatibility";
			this.rbCompatibility.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.rbPerformance);
			this.groupBox1.Controls.Add(this.rbCompatibility);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(277, 70);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Core Selection";
			// 
			// rbPerformance
			// 
			this.rbPerformance.AutoSize = true;
			this.rbPerformance.Location = new System.Drawing.Point(6, 42);
			this.rbPerformance.Name = "rbPerformance";
			this.rbPerformance.Size = new System.Drawing.Size(202, 17);
			this.rbPerformance.TabIndex = 3;
			this.rbPerformance.TabStop = true;
			this.rbPerformance.Text = "Performance (only for casual gaming!)";
			this.rbPerformance.UseVisualStyleBackColor = true;
			// 
			// cbRingbuf
			// 
			this.cbRingbuf.AutoSize = true;
			this.cbRingbuf.Location = new System.Drawing.Point(18, 97);
			this.cbRingbuf.Name = "cbRingbuf";
			this.cbRingbuf.Size = new System.Drawing.Size(115, 17);
			this.cbRingbuf.TabIndex = 4;
			this.cbRingbuf.Text = "Use Ring Buffer IO";
			this.cbRingbuf.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(34, 117);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(254, 45);
			this.label1.TabIndex = 5;
			this.label1.Text = "This was designed as an optimization but it isn\'t clear whether it works. Feel fr" +
    "ee to try different settings and let us know the results.";
			// 
			// SNESOptions
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(300, 198);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cbRingbuf);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Name = "SNESOptions";
			this.Text = "SNESOptions";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.RadioButton rbCompatibility;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbPerformance;
		private System.Windows.Forms.CheckBox cbRingbuf;
		private System.Windows.Forms.Label label1;
	}
}