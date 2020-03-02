namespace BizHawk.Client.EmuHawk
{
	partial class AutofireConfig
	{
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
			this.label1 = new BizHawk.WinForms.Controls.LabelEx();
			this.label2 = new BizHawk.WinForms.Controls.LabelEx();
			this.flpButtons = new BizHawk.WinForms.Controls.LocSzSingleRowFLP();
			this.flpMain = new BizHawk.WinForms.Controls.LocSzSingleColumnFLP();
			this.flpPattern = new BizHawk.WinForms.Controls.SingleRowFLP();
			this.lblPattern = new BizHawk.WinForms.Controls.LabelEx();
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
			this.Ok.Name = "Ok";
			this.Ok.Text = "&OK";
			this.Ok.UseVisualStyleBackColor = true;
			this.Ok.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Name = "Cancel";
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OnNumeric
			// 
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
			this.OnNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// OffNumeric
			// 
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
			this.OffNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// label1
			// 
			this.label1.Name = "label1";
			this.label1.Text = "on,";
			// 
			// label2
			// 
			this.label2.Name = "label2";
			this.label2.Text = "off";
			// 
			// flpButtons
			// 
			this.flpButtons.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.flpButtons.Controls.Add(this.Ok);
			this.flpButtons.Controls.Add(this.Cancel);
			this.flpButtons.Location = new System.Drawing.Point(161, 61);
			this.flpButtons.Name = "flpButtons";
			this.flpButtons.Size = new System.Drawing.Size(162, 29);
			// 
			// flpMain
			// 
			this.flpMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
			this.flpMain.Controls.Add(this.flpPattern);
			this.flpMain.Controls.Add(this.LagFrameCheck);
			this.flpMain.Location = new System.Drawing.Point(0, 0);
			this.flpMain.Name = "flpMain";
			this.flpMain.Size = new System.Drawing.Size(323, 55);
			// 
			// flpPattern
			// 
			this.flpPattern.Controls.Add(this.lblPattern);
			this.flpPattern.Controls.Add(this.OnNumeric);
			this.flpPattern.Controls.Add(this.label1);
			this.flpPattern.Controls.Add(this.OffNumeric);
			this.flpPattern.Controls.Add(this.label2);
			this.flpPattern.Name = "flpPattern";
			// 
			// lblPattern
			// 
			this.lblPattern.Name = "lblPattern";
			this.lblPattern.Text = "Pattern:";
			// 
			// LagFrameCheck
			// 
			this.LagFrameCheck.AutoSize = true;
			this.LagFrameCheck.Name = "LagFrameCheck";
			this.LagFrameCheck.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
			this.LagFrameCheck.Text = "Take lag frames into account";
			this.LagFrameCheck.UseVisualStyleBackColor = true;
			// 
			// AutofireConfig
			// 
			this.AcceptButton = this.Ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(323, 90);
			this.Controls.Add(this.flpMain);
			this.Controls.Add(this.flpButtons);
			this.Icon = global::BizHawk.Client.EmuHawk.Properties.Resources.Lightning_MultiSize;
			this.MaximizeBox = false;
			this.MinimumSize = new System.Drawing.Size(339, 129);
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
		private BizHawk.WinForms.Controls.LabelEx label1;
		private BizHawk.WinForms.Controls.LabelEx label2;
		private BizHawk.WinForms.Controls.SingleRowFLP flpPattern;
		private BizHawk.WinForms.Controls.LocSzSingleColumnFLP flpMain;
		private BizHawk.WinForms.Controls.LocSzSingleRowFLP flpButtons;
		private BizHawk.WinForms.Controls.LabelEx lblPattern;
		public System.Windows.Forms.NumericUpDown OnNumeric;
		private System.Windows.Forms.CheckBox LagFrameCheck;
	}
}
