namespace BizHawk.Client.EmuHawk
{
	partial class PSXOptions
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PSXOptions));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.button1 = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.radioButton2 = new System.Windows.Forms.RadioButton();
			this.label8 = new System.Windows.Forms.Label();
			this.radioButton1 = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.checkBox1 = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(437, 347);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(356, 347);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 2;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.button1);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.radioButton2);
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Controls.Add(this.radioButton1);
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(276, 265);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Resolution Management";
			// 
			// button1
			// 
			this.button1.AutoSize = true;
			this.button1.Location = new System.Drawing.Point(46, 225);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(173, 23);
			this.button1.TabIndex = 24;
			this.button1.Text = "Change My Display Configuration";
			this.button1.UseVisualStyleBackColor = true;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(6, 140);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(213, 82);
			this.label2.TabIndex = 23;
			this.label2.Text = resources.GetString("label2.Text");
			// 
			// radioButton2
			// 
			this.radioButton2.AutoSize = true;
			this.radioButton2.Location = new System.Drawing.Point(6, 120);
			this.radioButton2.Name = "radioButton2";
			this.radioButton2.Size = new System.Drawing.Size(145, 17);
			this.radioButton2.TabIndex = 22;
			this.radioButton2.TabStop = true;
			this.radioButton2.Text = "Mednafen Mode (4:3 AR)";
			this.radioButton2.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(6, 39);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(252, 78);
			this.label8.TabIndex = 21;
			this.label8.Text = "Converts content with nearest neighbor to \r\nfit gracefully in a 800x480 window.\r\n" +
    " • Content is pixel perfect\r\n • Aspect ratio is usually wrong\r\n • Game may seen " +
    "to have scale varying by mode\r\n\r\n\r\n";
			// 
			// radioButton1
			// 
			this.radioButton1.AutoSize = true;
			this.radioButton1.Location = new System.Drawing.Point(6, 19);
			this.radioButton1.Name = "radioButton1";
			this.radioButton1.Size = new System.Drawing.Size(96, 17);
			this.radioButton1.TabIndex = 0;
			this.radioButton1.TabStop = true;
			this.radioButton1.Text = "Pixel Pro Mode";
			this.radioButton1.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(9, 291);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(283, 91);
			this.label3.TabIndex = 24;
			this.label3.Text = resources.GetString("label3.Text");
			// 
			// checkBox1
			// 
			this.checkBox1.AutoSize = true;
			this.checkBox1.Location = new System.Drawing.Point(6, 100);
			this.checkBox1.Name = "checkBox1";
			this.checkBox1.Size = new System.Drawing.Size(109, 17);
			this.checkBox1.TabIndex = 25;
			this.checkBox1.Text = "Discard overscan";
			this.checkBox1.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label6);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Controls.Add(this.label4);
			this.groupBox2.Controls.Add(this.checkBox1);
			this.groupBox2.Location = new System.Drawing.Point(294, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(211, 265);
			this.groupBox2.TabIndex = 26;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Non-Functional Options";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(7, 144);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(197, 21);
			this.label6.TabIndex = 29;
			this.label6.Text = "Maybe fixes 1x window size problem";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 21);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(197, 21);
			this.label1.TabIndex = 28;
			this.label1.Text = "To think about and discuss";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(6, 201);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(197, 21);
			this.label5.TabIndex = 27;
			this.label5.Text = "(Scanline range selection)";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(6, 120);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(197, 21);
			this.label4.TabIndex = 26;
			this.label4.Text = "Decreases resolutions (e.g. 350 to 320)";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(315, 303);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(197, 29);
			this.label7.TabIndex = 30;
			this.label7.Text = "Restart the core to take effect.\r\nSorry, its still in development";
			// 
			// PSXOptions
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(524, 382);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PSXOptions";
			this.Text = "PSX Options";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton radioButton1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RadioButton radioButton2;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.CheckBox checkBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label7;
	}
}