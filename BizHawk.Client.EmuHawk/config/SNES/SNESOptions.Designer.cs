namespace BizHawk.Client.EmuHawk
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
			this.label2 = new System.Windows.Forms.Label();
			this.rbAccuracy = new System.Windows.Forms.RadioButton();
			this.rbPerformance = new System.Windows.Forms.RadioButton();
			this.cbRingbuf = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.cbDoubleSize = new System.Windows.Forms.CheckBox();
			this.lblDoubleSize = new System.Windows.Forms.Label();
			this.cbForceDeterminism = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(136, 344);
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
			this.btnCancel.Location = new System.Drawing.Point(217, 344);
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
			this.rbCompatibility.Size = new System.Drawing.Size(202, 17);
			this.rbCompatibility.TabIndex = 2;
			this.rbCompatibility.TabStop = true;
			this.rbCompatibility.Text = "Compatibility (more debug tools work!)";
			this.rbCompatibility.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.rbAccuracy);
			this.groupBox1.Controls.Add(this.rbPerformance);
			this.groupBox1.Controls.Add(this.rbCompatibility);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(277, 108);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Core Selection";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(72, 85);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(136, 21);
			this.label2.TabIndex = 8;
			this.label2.Text = "NOT SUPPORTED YET!";
			// 
			// rbAccuracy
			// 
			this.rbAccuracy.AutoSize = true;
			this.rbAccuracy.Enabled = false;
			this.rbAccuracy.Location = new System.Drawing.Point(6, 65);
			this.rbAccuracy.Name = "rbAccuracy";
			this.rbAccuracy.Size = new System.Drawing.Size(224, 17);
			this.rbAccuracy.TabIndex = 4;
			this.rbAccuracy.TabStop = true;
			this.rbAccuracy.Text = "Accuracy (only to fix bugs in Compatibility!)";
			this.rbAccuracy.UseVisualStyleBackColor = true;
			this.rbAccuracy.CheckedChanged += new System.EventHandler(this.rbAccuracy_CheckedChanged);
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
			this.cbRingbuf.Location = new System.Drawing.Point(19, 134);
			this.cbRingbuf.Name = "cbRingbuf";
			this.cbRingbuf.Size = new System.Drawing.Size(115, 17);
			this.cbRingbuf.TabIndex = 4;
			this.cbRingbuf.Text = "Use Ring Buffer IO";
			this.cbRingbuf.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(35, 154);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(254, 45);
			this.label1.TabIndex = 5;
			this.label1.Text = "Performance-affecting option; results differ for different users\' systems.";
			// 
			// cbDoubleSize
			// 
			this.cbDoubleSize.AutoSize = true;
			this.cbDoubleSize.Location = new System.Drawing.Point(18, 189);
			this.cbDoubleSize.Name = "cbDoubleSize";
			this.cbDoubleSize.Size = new System.Drawing.Size(178, 17);
			this.cbDoubleSize.TabIndex = 6;
			this.cbDoubleSize.Text = "Always Double-Size Framebuffer";
			this.cbDoubleSize.UseVisualStyleBackColor = true;
			this.cbDoubleSize.CheckedChanged += new System.EventHandler(this.cbDoubleSize_CheckedChanged);
			// 
			// lblDoubleSize
			// 
			this.lblDoubleSize.Location = new System.Drawing.Point(36, 210);
			this.lblDoubleSize.Name = "lblDoubleSize";
			this.lblDoubleSize.Size = new System.Drawing.Size(254, 57);
			this.lblDoubleSize.TabIndex = 7;
			this.lblDoubleSize.Text = "Some games are changing the resolution constantly (e.g. SD3) so this option can f" +
    "orce the SNES output to stay double-size always. NOTE: The Accuracy core runs as" +
    " if this is selected.\r\n";
			// 
			// cbForceDeterminism
			// 
			this.cbForceDeterminism.AutoSize = true;
			this.cbForceDeterminism.Location = new System.Drawing.Point(19, 271);
			this.cbForceDeterminism.Name = "cbForceDeterminism";
			this.cbForceDeterminism.Size = new System.Drawing.Size(113, 17);
			this.cbForceDeterminism.TabIndex = 8;
			this.cbForceDeterminism.Text = "Force Determinism";
			this.cbForceDeterminism.UseVisualStyleBackColor = true;
			this.cbForceDeterminism.CheckedChanged += new System.EventHandler(this.cbForceDeterminism_CheckedChanged);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(38, 295);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(251, 41);
			this.label3.TabIndex = 9;
			this.label3.Text = "Guarantee deterministic emulation by savestating every frame. Don\'t TAS without i" +
    "t! Only ~75% of runs sync without it, but speed boost is ~30%.";
			// 
			// SNESOptions
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(304, 379);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.cbForceDeterminism);
			this.Controls.Add(this.lblDoubleSize);
			this.Controls.Add(this.cbDoubleSize);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.cbRingbuf);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SNESOptions";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "SNES Options";
			this.Load += new System.EventHandler(this.SNESOptions_Load);
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
		private System.Windows.Forms.CheckBox cbDoubleSize;
		private System.Windows.Forms.Label lblDoubleSize;
		private System.Windows.Forms.RadioButton rbAccuracy;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox cbForceDeterminism;
		private System.Windows.Forms.Label label3;
	}
}