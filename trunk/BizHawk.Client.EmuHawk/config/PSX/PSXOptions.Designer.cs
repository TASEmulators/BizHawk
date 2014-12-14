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
			this.btnNiceDisplayConfig = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.rbMednafenMode = new System.Windows.Forms.RadioButton();
			this.label8 = new System.Windows.Forms.Label();
			this.rbPixelPro = new System.Windows.Forms.RadioButton();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.rbDebugMode = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.rbTweakedMednafenMode = new System.Windows.Forms.RadioButton();
			this.label9 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(622, 240);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 3;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(541, 240);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 2;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label9);
			this.groupBox1.Controls.Add(this.rbTweakedMednafenMode);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.rbDebugMode);
			this.groupBox1.Controls.Add(this.btnNiceDisplayConfig);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.rbMednafenMode);
			this.groupBox1.Controls.Add(this.label8);
			this.groupBox1.Controls.Add(this.rbPixelPro);
			this.groupBox1.Location = new System.Drawing.Point(12, 7);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(474, 256);
			this.groupBox1.TabIndex = 6;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Resolution Management";
			// 
			// btnNiceDisplayConfig
			// 
			this.btnNiceDisplayConfig.AutoSize = true;
			this.btnNiceDisplayConfig.Location = new System.Drawing.Point(140, 221);
			this.btnNiceDisplayConfig.Name = "btnNiceDisplayConfig";
			this.btnNiceDisplayConfig.Size = new System.Drawing.Size(173, 23);
			this.btnNiceDisplayConfig.TabIndex = 24;
			this.btnNiceDisplayConfig.Text = "Change My Display Options";
			this.btnNiceDisplayConfig.UseVisualStyleBackColor = true;
			this.btnNiceDisplayConfig.Click += new System.EventHandler(this.btnNiceDisplayConfig_Click);
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(6, 132);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(213, 82);
			this.label2.TabIndex = 23;
			this.label2.Text = resources.GetString("label2.Text");
			// 
			// rbMednafenMode
			// 
			this.rbMednafenMode.AutoSize = true;
			this.rbMednafenMode.Location = new System.Drawing.Point(6, 116);
			this.rbMednafenMode.Name = "rbMednafenMode";
			this.rbMednafenMode.Size = new System.Drawing.Size(145, 17);
			this.rbMednafenMode.TabIndex = 22;
			this.rbMednafenMode.TabStop = true;
			this.rbMednafenMode.Text = "Mednafen Mode (4:3 AR)";
			this.rbMednafenMode.UseVisualStyleBackColor = true;
			// 
			// label8
			// 
			this.label8.Location = new System.Drawing.Point(6, 35);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(252, 78);
			this.label8.TabIndex = 21;
			this.label8.Text = "Converts content with nearest neighbor to \r\nfit gracefully in a 800x480 window.\r\n" +
    " • Content is pixel perfect\r\n • Aspect ratio is usually wrong\r\n • Game may seen " +
    "to have scale varying by mode\r\n\r\n\r\n";
			// 
			// rbPixelPro
			// 
			this.rbPixelPro.AutoSize = true;
			this.rbPixelPro.Location = new System.Drawing.Point(6, 19);
			this.rbPixelPro.Name = "rbPixelPro";
			this.rbPixelPro.Size = new System.Drawing.Size(96, 17);
			this.rbPixelPro.TabIndex = 0;
			this.rbPixelPro.TabStop = true;
			this.rbPixelPro.Text = "Pixel Pro Mode";
			this.rbPixelPro.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.label5);
			this.groupBox2.Location = new System.Drawing.Point(492, 7);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(211, 143);
			this.groupBox2.TabIndex = 26;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Non-Functional Options";
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
			this.label5.Location = new System.Drawing.Point(8, 45);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(197, 21);
			this.label5.TabIndex = 27;
			this.label5.Text = "(Scanline range selection)";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(500, 169);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(197, 29);
			this.label7.TabIndex = 30;
			this.label7.Text = "Restart the core to take effect.\r\nSorry, its still in development";
			// 
			// rbDebugMode
			// 
			this.rbDebugMode.AutoSize = true;
			this.rbDebugMode.Location = new System.Drawing.Point(246, 19);
			this.rbDebugMode.Name = "rbDebugMode";
			this.rbDebugMode.Size = new System.Drawing.Size(134, 17);
			this.rbDebugMode.TabIndex = 25;
			this.rbDebugMode.TabStop = true;
			this.rbDebugMode.Text = "Hardcore Debug Mode";
			this.rbDebugMode.UseVisualStyleBackColor = true;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(246, 39);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(213, 50);
			this.label3.TabIndex = 26;
			this.label3.Text = "Displays all content unmodified\r\n • Window size will constantly change\r\n • Aspect" +
    " ratio is usually wrong";
			// 
			// rbTweakedMednafenMode
			// 
			this.rbTweakedMednafenMode.AutoSize = true;
			this.rbTweakedMednafenMode.Location = new System.Drawing.Point(246, 116);
			this.rbTweakedMednafenMode.Name = "rbTweakedMednafenMode";
			this.rbTweakedMednafenMode.Size = new System.Drawing.Size(193, 17);
			this.rbTweakedMednafenMode.TabIndex = 27;
			this.rbTweakedMednafenMode.TabStop = true;
			this.rbTweakedMednafenMode.Text = "Tweaked Mednafen Mode (4:3 AR)";
			this.rbTweakedMednafenMode.UseVisualStyleBackColor = true;
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(255, 132);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(213, 79);
			this.label9.TabIndex = 28;
			this.label9.Text = "Displays all content at as multiple of 400x300.\r\n • Correct aspect ratio\r\n • Gene" +
    "rally enjoyable game presentation\r\n • Detail loss at 1x in fewer cases\r\n • Requi" +
    "res certain display configuration:\r\n";
			// 
			// PSXOptions
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(713, 275);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.groupBox2);
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
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbPixelPro;
		private System.Windows.Forms.Button btnNiceDisplayConfig;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RadioButton rbMednafenMode;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.RadioButton rbDebugMode;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.RadioButton rbTweakedMednafenMode;
	}
}