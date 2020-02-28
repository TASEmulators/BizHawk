namespace BizHawk.Client.EmuHawk
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
			this.Ok = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.OnNumeric = new System.Windows.Forms.NumericUpDown();
			this.OffNumeric = new System.Windows.Forms.NumericUpDown();
			this.label1 = new BizHawk.Client.EmuHawk.CustomControls.AutosizedLabel();
			this.label2 = new BizHawk.Client.EmuHawk.CustomControls.AutosizedLabel();
			this.flpButtons = new BizHawk.Client.EmuHawk.CustomControls.SingleRowFLP();
			this.flpMain = new BizHawk.Client.EmuHawk.CustomControls.SingleColumnFLP();
			this.flpPattern = new BizHawk.Client.EmuHawk.CustomControls.SingleRowFLP();
			this.lblPattern = new BizHawk.Client.EmuHawk.CustomControls.AutosizedLabel();
			this.LagFrameCheck = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.OnNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.OffNumeric)).BeginInit();
			this.flpButtons.SuspendLayout();
			this.flpMain.SuspendLayout();
			this.flpPattern.SuspendLayout();
			this.SuspendLayout();
			// 
			// Ok
			// 
			this.Ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Ok.Location = new System.Drawing.Point(108, 140);
			this.Ok.Name = "Ok";
			this.Ok.Size = new System.Drawing.Size(75, 23);
			this.Ok.TabIndex = 5;
			this.Ok.Text = "&OK";
			this.Ok.UseVisualStyleBackColor = true;
			this.Ok.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(189, 140);
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
			this.OnNumeric.Size = new System.Drawing.Size(48, 19);
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
			this.OffNumeric.Size = new System.Drawing.Size(48, 19);
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
			this.label1.Text = "on,";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(101, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(21, 13);
			this.label2.TabIndex = 5;
			this.label2.Text = "off";
			// 
			// flpButtons
			// 
			this.flpButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.flpButtons.AutoSize = true;
			this.flpButtons.Controls.Add(this.Ok);
			this.flpButtons.Controls.Add(this.Cancel);
			this.flpButtons.Location = new System.Drawing.Point(55, 61);
			this.flpButtons.Name = "flpButtons";
			this.flpButtons.Size = new System.Drawing.Size(162, 29);
			this.flpButtons.TabIndex = 11;
			this.flpButtons.WrapContents = false;
			// 
			// flpMain
			// 
			this.flpMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flpMain.AutoSize = false;
			this.flpMain.Controls.Add(this.flpPattern);
			this.flpMain.Controls.Add(this.LagFrameCheck);
			this.flpMain.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flpMain.Location = new System.Drawing.Point(0, 0);
			this.flpMain.Name = "flpMain";
			this.flpMain.Size = new System.Drawing.Size(217, 55);
			this.flpMain.TabIndex = 10;
			this.flpMain.WrapContents = false;
			// 
			// flpPattern
			// 
			this.flpPattern.AutoSize = true;
			this.flpPattern.Controls.Add(this.lblPattern);
			this.flpPattern.Controls.Add(this.OnNumeric);
			this.flpPattern.Controls.Add(this.label1);
			this.flpPattern.Controls.Add(this.OffNumeric);
			this.flpPattern.Controls.Add(this.label2);
			this.flpPattern.Location = new System.Drawing.Point(3, 3);
			this.flpPattern.Name = "flpPattern";
			this.flpPattern.Size = new System.Drawing.Size(211, 26);
			this.flpPattern.TabIndex = 9;
			this.flpPattern.WrapContents = false;
			// 
			// lblPattern
			// 
			this.lblPattern.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.lblPattern.AutoSize = true;
			this.lblPattern.Location = new System.Drawing.Point(3, 6);
			this.lblPattern.Name = "lblPattern";
			this.lblPattern.Size = new System.Drawing.Size(44, 13);
			this.lblPattern.TabIndex = 12;
			this.lblPattern.Text = "Pattern:";
			// 
			// LagFrameCheck
			// 
			this.LagFrameCheck.AutoSize = true;
			this.LagFrameCheck.Location = new System.Drawing.Point(13, 100);
			this.LagFrameCheck.Name = "LagFrameCheck";
			this.LagFrameCheck.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
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
			this.ClientSize = new System.Drawing.Size(217, 90);
			this.Controls.Add(this.flpMain);
			this.Controls.Add(this.flpButtons);
			this.Icon = global::BizHawk.Client.EmuHawk.Properties.Resources.Lightning_MultiSize;
			this.MaximizeBox = false;
			this.MinimumSize = new System.Drawing.Size(233, 129);
			this.Name = "AutofireConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Autofire Configuration";
			this.Load += new System.EventHandler(this.AutofireConfig_Load);
			((System.ComponentModel.ISupportInitialize)(this.OnNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.OffNumeric)).EndInit();
			this.flpButtons.ResumeLayout(false);
			this.flpMain.ResumeLayout(false);
			this.flpMain.PerformLayout();
			this.flpPattern.ResumeLayout(false);
			this.flpPattern.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.NumericUpDown OffNumeric;
		private CustomControls.AutosizedLabel label1;
		private CustomControls.AutosizedLabel label2;
		private CustomControls.SingleRowFLP flpPattern;
		private CustomControls.SingleColumnFLP flpMain;
		private CustomControls.SingleRowFLP flpButtons;
		private CustomControls.AutosizedLabel lblPattern;
		public System.Windows.Forms.NumericUpDown OnNumeric;
		private System.Windows.Forms.CheckBox LagFrameCheck;
	}
}
