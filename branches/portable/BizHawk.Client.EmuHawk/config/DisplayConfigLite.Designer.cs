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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DisplayConfigLite));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
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
			this.grpFinalFilter = new System.Windows.Forms.GroupBox();
			this.rbFinalFilterBicubic = new System.Windows.Forms.RadioButton();
			this.rbFinalFilterNone = new System.Windows.Forms.RadioButton();
			this.rbFinalFilterBilinear = new System.Windows.Forms.RadioButton();
			this.rbUseRaw = new System.Windows.Forms.RadioButton();
			this.rbUseSystem = new System.Windows.Forms.RadioButton();
			this.grpARSelection = new System.Windows.Forms.GroupBox();
			this.checkFullscreenHacks = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.checkSnowyNullEmulator = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).BeginInit();
			this.grpFinalFilter.SuspendLayout();
			this.grpARSelection.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(510, 250);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// btnOk
			// 
			this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnOk.Location = new System.Drawing.Point(429, 250);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 4;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
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
			this.groupBox1.Location = new System.Drawing.Point(12, 12);
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
			this.tbScanlineIntensity.Maximum = 256;
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
			this.checkLetterbox.Location = new System.Drawing.Point(12, 154);
			this.checkLetterbox.Name = "checkLetterbox";
			this.checkLetterbox.Size = new System.Drawing.Size(173, 17);
			this.checkLetterbox.TabIndex = 8;
			this.checkLetterbox.Text = "Maintain aspect ratio (letterbox)";
			this.checkLetterbox.UseVisualStyleBackColor = true;
			this.checkLetterbox.CheckedChanged += new System.EventHandler(this.checkLetterbox_CheckedChanged);
			// 
			// checkPadInteger
			// 
			this.checkPadInteger.AutoSize = true;
			this.checkPadInteger.Location = new System.Drawing.Point(21, 254);
			this.checkPadInteger.Name = "checkPadInteger";
			this.checkPadInteger.Size = new System.Drawing.Size(248, 17);
			this.checkPadInteger.TabIndex = 9;
			this.checkPadInteger.Text = "Stretch pixels by integers only (e.g. no 1.3333x)";
			this.checkPadInteger.UseVisualStyleBackColor = true;
			this.checkPadInteger.CheckedChanged += new System.EventHandler(this.checkPadInteger_CheckedChanged);
			// 
			// grpFinalFilter
			// 
			this.grpFinalFilter.Controls.Add(this.rbFinalFilterBicubic);
			this.grpFinalFilter.Controls.Add(this.rbFinalFilterNone);
			this.grpFinalFilter.Controls.Add(this.rbFinalFilterBilinear);
			this.grpFinalFilter.Location = new System.Drawing.Point(191, 12);
			this.grpFinalFilter.Name = "grpFinalFilter";
			this.grpFinalFilter.Size = new System.Drawing.Size(173, 132);
			this.grpFinalFilter.TabIndex = 8;
			this.grpFinalFilter.TabStop = false;
			this.grpFinalFilter.Text = "Final Filter";
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
			// rbUseRaw
			// 
			this.rbUseRaw.AutoSize = true;
			this.rbUseRaw.Location = new System.Drawing.Point(6, 19);
			this.rbUseRaw.Name = "rbUseRaw";
			this.rbUseRaw.Size = new System.Drawing.Size(240, 17);
			this.rbUseRaw.TabIndex = 11;
			this.rbUseRaw.TabStop = true;
			this.rbUseRaw.Text = "Use 1:1 pixel size (for crispness or debugging)";
			this.rbUseRaw.UseVisualStyleBackColor = true;
			this.rbUseRaw.CheckedChanged += new System.EventHandler(this.rbUseRaw_CheckedChanged);
			// 
			// rbUseSystem
			// 
			this.rbUseSystem.AutoSize = true;
			this.rbUseSystem.Location = new System.Drawing.Point(6, 42);
			this.rbUseSystem.Name = "rbUseSystem";
			this.rbUseSystem.Size = new System.Drawing.Size(320, 17);
			this.rbUseSystem.TabIndex = 12;
			this.rbUseSystem.TabStop = true;
			this.rbUseSystem.Text = "Use system\'s recommendation (e.g. 2x1 pixels, for better AR fit)";
			this.rbUseSystem.UseVisualStyleBackColor = true;
			this.rbUseSystem.CheckedChanged += new System.EventHandler(this.rbUseSystem_CheckedChanged);
			// 
			// grpARSelection
			// 
			this.grpARSelection.Controls.Add(this.rbUseRaw);
			this.grpARSelection.Controls.Add(this.rbUseSystem);
			this.grpARSelection.Location = new System.Drawing.Point(21, 177);
			this.grpARSelection.Name = "grpARSelection";
			this.grpARSelection.Size = new System.Drawing.Size(342, 71);
			this.grpARSelection.TabIndex = 13;
			this.grpARSelection.TabStop = false;
			this.grpARSelection.Text = "Aspect Ratio Selection";
			// 
			// checkFullscreenHacks
			// 
			this.checkFullscreenHacks.AutoSize = true;
			this.checkFullscreenHacks.Location = new System.Drawing.Point(6, 19);
			this.checkFullscreenHacks.Name = "checkFullscreenHacks";
			this.checkFullscreenHacks.Size = new System.Drawing.Size(191, 17);
			this.checkFullscreenHacks.TabIndex = 14;
			this.checkFullscreenHacks.Text = "Enable Windows Fullscreen Hacks";
			this.checkFullscreenHacks.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.checkSnowyNullEmulator);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Controls.Add(this.checkFullscreenHacks);
			this.groupBox2.Location = new System.Drawing.Point(370, 12);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(217, 224);
			this.groupBox2.TabIndex = 15;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Misc.";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(7, 167);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(204, 45);
			this.label2.TabIndex = 17;
			this.label2.Text = "Some people think the whitenoise is a great idea, and some people don\'t. Enabling" +
    " this displays an Oxoo instead.";
			// 
			// checkSnowyNullEmulator
			// 
			this.checkSnowyNullEmulator.AutoSize = true;
			this.checkSnowyNullEmulator.Location = new System.Drawing.Point(6, 142);
			this.checkSnowyNullEmulator.Name = "checkSnowyNullEmulator";
			this.checkSnowyNullEmulator.Size = new System.Drawing.Size(159, 17);
			this.checkSnowyNullEmulator.TabIndex = 16;
			this.checkSnowyNullEmulator.Text = "Enable Snowy Null Emulator";
			this.checkSnowyNullEmulator.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(7, 42);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(204, 102);
			this.label1.TabIndex = 15;
			this.label1.Text = resources.GetString("label1.Text");
			// 
			// DisplayConfigLite
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(597, 285);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.grpARSelection);
			this.Controls.Add(this.grpFinalFilter);
			this.Controls.Add(this.checkPadInteger);
			this.Controls.Add(this.checkLetterbox);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "DisplayConfigLite";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Display Configuration";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.tbScanlineIntensity)).EndInit();
			this.grpFinalFilter.ResumeLayout(false);
			this.grpFinalFilter.PerformLayout();
			this.grpARSelection.ResumeLayout(false);
			this.grpARSelection.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.RadioButton rbNone;
		private System.Windows.Forms.RadioButton rbScanlines;
		private System.Windows.Forms.RadioButton rbHq2x;
		private System.Windows.Forms.TrackBar tbScanlineIntensity;
		private System.Windows.Forms.CheckBox checkLetterbox;
		private System.Windows.Forms.CheckBox checkPadInteger;
		private System.Windows.Forms.GroupBox grpFinalFilter;
		private System.Windows.Forms.RadioButton rbFinalFilterBicubic;
		private System.Windows.Forms.RadioButton rbFinalFilterNone;
		private System.Windows.Forms.RadioButton rbFinalFilterBilinear;
		private System.Windows.Forms.Button btnSelectUserFilter;
		private System.Windows.Forms.RadioButton rbUser;
		private System.Windows.Forms.Label lblUserFilterName;
		private System.Windows.Forms.RadioButton rbUseRaw;
		private System.Windows.Forms.RadioButton rbUseSystem;
		private System.Windows.Forms.GroupBox grpARSelection;
		private System.Windows.Forms.CheckBox checkFullscreenHacks;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox checkSnowyNullEmulator;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
	}
}