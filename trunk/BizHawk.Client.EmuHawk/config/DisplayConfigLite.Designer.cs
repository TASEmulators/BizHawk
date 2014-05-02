namespace BizHawk.Client.EmuHawk.config
{
	partial class DisplayConfigLite
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
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lblUserFilterName = new System.Windows.Forms.Label();
			this.btnSelectUserFilter = new System.Windows.Forms.Button();
			this.rbUser = new System.Windows.Forms.RadioButton();
			this.tbScanlineIntensity = new System.Windows.Forms.TrackBar();
			this.rbNone = new System.Windows.Forms.RadioButton();
			this.rbScanlines = new System.Windows.Forms.RadioButton();
			this.rbHq2x = new System.Windows.Forms.RadioButton();
			this.checkLetterbox = new System.Windows.Forms.CheckBox();
			this.checkPadInteger = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.rbFinalFilterBicubic = new System.Windows.Forms.RadioButton();
			this.rbFinalFilterNone = new System.Windows.Forms.RadioButton();
			this.rbFinalFilterBilinear = new System.Windows.Forms.RadioButton();
			this.checkObeyAR = new System.Windows.Forms.CheckBox();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).BeginInit();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(289, 221);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(208, 221);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 4;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(269, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "This is a staging ground for more complex configuration.";
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.lblUserFilterName);
			this.groupBox1.Controls.Add(this.btnSelectUserFilter);
			this.groupBox1.Controls.Add(this.rbUser);
			this.groupBox1.Controls.Add(this.tbScanlineIntensity);
			this.groupBox1.Controls.Add(this.rbNone);
			this.groupBox1.Controls.Add(this.rbScanlines);
			this.groupBox1.Controls.Add(this.rbHq2x);
			this.groupBox1.Location = new System.Drawing.Point(12, 34);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(173, 132);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Scaling Filter";
			// 
			// lblUserFilterName
			// 
			this.lblUserFilterName.Location = new System.Drawing.Point(6, 114);
			this.lblUserFilterName.Name = "lblUserFilterName";
			this.lblUserFilterName.Size = new System.Drawing.Size(161, 15);
			this.lblUserFilterName.TabIndex = 10;
			this.lblUserFilterName.Text = "Will contain user filter name";
			// 
			// btnSelectUserFilter
			// 
			this.btnSelectUserFilter.Location = new System.Drawing.Point(83, 88);
			this.btnSelectUserFilter.Name = "btnSelectUserFilter";
			this.btnSelectUserFilter.Size = new System.Drawing.Size(75, 23);
			this.btnSelectUserFilter.TabIndex = 5;
			this.btnSelectUserFilter.Text = "Select";
			this.btnSelectUserFilter.UseVisualStyleBackColor = true;
			this.btnSelectUserFilter.Click += new System.EventHandler(this.btnSelectUserFilter_Click);
			// 
			// rbUser
			// 
			this.rbUser.AutoSize = true;
			this.rbUser.Location = new System.Drawing.Point(6, 88);
			this.rbUser.Name = "rbUser";
			this.rbUser.Size = new System.Drawing.Size(47, 17);
			this.rbUser.TabIndex = 4;
			this.rbUser.TabStop = true;
			this.rbUser.Text = "User";
			this.rbUser.UseVisualStyleBackColor = true;
			// 
			// tbScanlineIntensity
			// 
			this.tbScanlineIntensity.LargeChange = 32;
			this.tbScanlineIntensity.Location = new System.Drawing.Point(83, 55);
			this.tbScanlineIntensity.Maximum = 255;
			this.tbScanlineIntensity.Name = "tbScanlineIntensity";
			this.tbScanlineIntensity.Size = new System.Drawing.Size(70, 42);
			this.tbScanlineIntensity.TabIndex = 3;
			this.tbScanlineIntensity.TickFrequency = 32;
			this.tbScanlineIntensity.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
			// 
			// rbNone
			// 
			this.rbNone.AutoSize = true;
			this.rbNone.Location = new System.Drawing.Point(6, 19);
			this.rbNone.Name = "rbNone";
			this.rbNone.Size = new System.Drawing.Size(51, 17);
			this.rbNone.TabIndex = 2;
			this.rbNone.TabStop = true;
			this.rbNone.Text = "None";
			this.rbNone.UseVisualStyleBackColor = true;
			// 
			// rbScanlines
			// 
			this.rbScanlines.AutoSize = true;
			this.rbScanlines.Location = new System.Drawing.Point(6, 65);
			this.rbScanlines.Name = "rbScanlines";
			this.rbScanlines.Size = new System.Drawing.Size(71, 17);
			this.rbScanlines.TabIndex = 1;
			this.rbScanlines.TabStop = true;
			this.rbScanlines.Text = "Scanlines";
			this.rbScanlines.UseVisualStyleBackColor = true;
			// 
			// rbHq2x
			// 
			this.rbHq2x.AutoSize = true;
			this.rbHq2x.Location = new System.Drawing.Point(6, 42);
			this.rbHq2x.Name = "rbHq2x";
			this.rbHq2x.Size = new System.Drawing.Size(50, 17);
			this.rbHq2x.TabIndex = 0;
			this.rbHq2x.TabStop = true;
			this.rbHq2x.Text = "Hq2x";
			this.rbHq2x.UseVisualStyleBackColor = true;
			// 
			// checkLetterbox
			// 
			this.checkLetterbox.AutoSize = true;
			this.checkLetterbox.Location = new System.Drawing.Point(12, 172);
			this.checkLetterbox.Name = "checkLetterbox";
			this.checkLetterbox.Size = new System.Drawing.Size(173, 17);
			this.checkLetterbox.TabIndex = 8;
			this.checkLetterbox.Text = "Maintain aspect ratio (letterbox)";
			this.checkLetterbox.UseVisualStyleBackColor = true;
			// 
			// checkPadInteger
			// 
			this.checkPadInteger.AutoSize = true;
			this.checkPadInteger.Location = new System.Drawing.Point(12, 218);
			this.checkPadInteger.Name = "checkPadInteger";
			this.checkPadInteger.Size = new System.Drawing.Size(120, 17);
			this.checkPadInteger.TabIndex = 9;
			this.checkPadInteger.Text = "Pad to integer scale";
			this.checkPadInteger.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.rbFinalFilterBicubic);
			this.groupBox2.Controls.Add(this.rbFinalFilterNone);
			this.groupBox2.Controls.Add(this.rbFinalFilterBilinear);
			this.groupBox2.Location = new System.Drawing.Point(191, 34);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(173, 132);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Final Filter";
			// 
			// rbFinalFilterBicubic
			// 
			this.rbFinalFilterBicubic.AutoSize = true;
			this.rbFinalFilterBicubic.Location = new System.Drawing.Point(7, 65);
			this.rbFinalFilterBicubic.Name = "rbFinalFilterBicubic";
			this.rbFinalFilterBicubic.Size = new System.Drawing.Size(142, 17);
			this.rbFinalFilterBicubic.TabIndex = 3;
			this.rbFinalFilterBicubic.TabStop = true;
			this.rbFinalFilterBicubic.Text = "Bicubic (shader. buggy?)";
			this.rbFinalFilterBicubic.UseVisualStyleBackColor = true;
			// 
			// rbFinalFilterNone
			// 
			this.rbFinalFilterNone.AutoSize = true;
			this.rbFinalFilterNone.Location = new System.Drawing.Point(6, 19);
			this.rbFinalFilterNone.Name = "rbFinalFilterNone";
			this.rbFinalFilterNone.Size = new System.Drawing.Size(51, 17);
			this.rbFinalFilterNone.TabIndex = 2;
			this.rbFinalFilterNone.TabStop = true;
			this.rbFinalFilterNone.Text = "None";
			this.rbFinalFilterNone.UseVisualStyleBackColor = true;
			// 
			// rbFinalFilterBilinear
			// 
			this.rbFinalFilterBilinear.AutoSize = true;
			this.rbFinalFilterBilinear.Location = new System.Drawing.Point(6, 42);
			this.rbFinalFilterBilinear.Name = "rbFinalFilterBilinear";
			this.rbFinalFilterBilinear.Size = new System.Drawing.Size(59, 17);
			this.rbFinalFilterBilinear.TabIndex = 0;
			this.rbFinalFilterBilinear.TabStop = true;
			this.rbFinalFilterBilinear.Text = "Bilinear";
			this.rbFinalFilterBilinear.UseVisualStyleBackColor = true;
			// 
			// checkObeyAR
			// 
			this.checkObeyAR.AutoSize = true;
			this.checkObeyAR.Location = new System.Drawing.Point(12, 195);
			this.checkObeyAR.Name = "checkObeyAR";
			this.checkObeyAR.Size = new System.Drawing.Size(211, 17);
			this.checkObeyAR.TabIndex = 10;
			this.checkObeyAR.Text = "Obey system\'s Aspect Ratio suggestion";
			this.checkObeyAR.UseVisualStyleBackColor = true;
			// 
			// DisplayConfigLite
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(376, 256);
			this.Controls.Add(this.checkObeyAR);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.checkPadInteger);
			this.Controls.Add(this.checkLetterbox);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "DisplayConfigLite";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Display Configuration";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).EndInit();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbNone;
		private System.Windows.Forms.RadioButton rbScanlines;
		private System.Windows.Forms.RadioButton rbHq2x;
		private System.Windows.Forms.TrackBar tbScanlineIntensity;
		private System.Windows.Forms.CheckBox checkLetterbox;
		private System.Windows.Forms.CheckBox checkPadInteger;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.RadioButton rbFinalFilterBicubic;
		private System.Windows.Forms.RadioButton rbFinalFilterNone;
		private System.Windows.Forms.RadioButton rbFinalFilterBilinear;
		private System.Windows.Forms.Button btnSelectUserFilter;
		private System.Windows.Forms.RadioButton rbUser;
		private System.Windows.Forms.Label lblUserFilterName;
		private System.Windows.Forms.CheckBox checkObeyAR;
	}
}