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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutofireConfig));
            this.btnDialogOK = new BizHawk.WinForms.Controls.SzButtonEx();
            this.btnDialogCancel = new BizHawk.WinForms.Controls.SzButtonEx();
            this.nudPatternOn = new BizHawk.WinForms.Controls.SzNUDEx();
            this.nudPatternOff = new BizHawk.WinForms.Controls.SzNUDEx();
            this.lblPatternOn = new BizHawk.WinForms.Controls.LabelEx();
            this.lblPatternOff = new BizHawk.WinForms.Controls.LabelEx();
            this.flpDialogButtons = new BizHawk.WinForms.Controls.LocSzSingleRowFLP();
            this.flpDialog = new BizHawk.WinForms.Controls.LocSzSingleColumnFLP();
            this.flpPattern = new BizHawk.WinForms.Controls.SingleRowFLP();
            this.lblPatternDesc = new BizHawk.WinForms.Controls.LabelEx();
            this.cbConsiderLag = new BizHawk.WinForms.Controls.CheckBoxEx();
            ((System.ComponentModel.ISupportInitialize)(this.nudPatternOn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPatternOff)).BeginInit();
            this.flpDialogButtons.SuspendLayout();
            this.flpDialog.SuspendLayout();
            this.flpPattern.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnDialogOK
            // 
            resources.ApplyResources(this.btnDialogOK, "btnDialogOK");
            this.btnDialogOK.Name = "btnDialogOK";
            this.btnDialogOK.Click += new System.EventHandler(this.btnDialogOK_Click);
            // 
            // btnDialogCancel
            // 
            resources.ApplyResources(this.btnDialogCancel, "btnDialogCancel");
            this.btnDialogCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnDialogCancel.Name = "btnDialogCancel";
            this.btnDialogCancel.Click += new System.EventHandler(this.btnDialogCancel_Click);
            // 
            // nudPatternOn
            // 
            resources.ApplyResources(this.nudPatternOn, "nudPatternOn");
            this.nudPatternOn.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
            this.nudPatternOn.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPatternOn.Name = "nudPatternOn";
            this.nudPatternOn.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // nudPatternOff
            // 
            resources.ApplyResources(this.nudPatternOff, "nudPatternOff");
            this.nudPatternOff.Maximum = new decimal(new int[] {
            512,
            0,
            0,
            0});
            this.nudPatternOff.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPatternOff.Name = "nudPatternOff";
            this.nudPatternOff.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblPatternOn
            // 
            resources.ApplyResources(this.lblPatternOn, "lblPatternOn");
            this.lblPatternOn.Name = "lblPatternOn";
            // 
            // lblPatternOff
            // 
            resources.ApplyResources(this.lblPatternOff, "lblPatternOff");
            this.lblPatternOff.Name = "lblPatternOff";
            // 
            // flpDialogButtons
            // 
            resources.ApplyResources(this.flpDialogButtons, "flpDialogButtons");
            this.flpDialogButtons.Controls.Add(this.btnDialogOK);
            this.flpDialogButtons.Controls.Add(this.btnDialogCancel);
            this.flpDialogButtons.Name = "flpDialogButtons";
            // 
            // flpDialog
            // 
            resources.ApplyResources(this.flpDialog, "flpDialog");
            this.flpDialog.Controls.Add(this.flpPattern);
            this.flpDialog.Controls.Add(this.cbConsiderLag);
            this.flpDialog.Name = "flpDialog";
            // 
            // flpPattern
            // 
            resources.ApplyResources(this.flpPattern, "flpPattern");
            this.flpPattern.Controls.Add(this.lblPatternDesc);
            this.flpPattern.Controls.Add(this.nudPatternOn);
            this.flpPattern.Controls.Add(this.lblPatternOn);
            this.flpPattern.Controls.Add(this.nudPatternOff);
            this.flpPattern.Controls.Add(this.lblPatternOff);
            this.flpPattern.Name = "flpPattern";
            // 
            // lblPatternDesc
            // 
            resources.ApplyResources(this.lblPatternDesc, "lblPatternDesc");
            this.lblPatternDesc.Name = "lblPatternDesc";
            // 
            // cbConsiderLag
            // 
            resources.ApplyResources(this.cbConsiderLag, "cbConsiderLag");
            this.cbConsiderLag.Name = "cbConsiderLag";
            // 
            // AutofireConfig
            // 
            this.AcceptButton = this.btnDialogOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnDialogCancel;
            this.Controls.Add(this.flpDialog);
            this.Controls.Add(this.flpDialogButtons);
            this.MaximizeBox = false;
            this.Name = "AutofireConfig";
            this.Load += new System.EventHandler(this.AutofireConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nudPatternOn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPatternOff)).EndInit();
            this.flpDialogButtons.ResumeLayout(false);
            this.flpDialog.ResumeLayout(false);
            this.flpDialog.PerformLayout();
            this.flpPattern.ResumeLayout(false);
            this.flpPattern.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private BizHawk.WinForms.Controls.SzButtonEx btnDialogOK;
		private BizHawk.WinForms.Controls.SzButtonEx btnDialogCancel;
		private BizHawk.WinForms.Controls.SzNUDEx nudPatternOff;
		private BizHawk.WinForms.Controls.LabelEx lblPatternOn;
		private BizHawk.WinForms.Controls.LabelEx lblPatternOff;
		private BizHawk.WinForms.Controls.SingleRowFLP flpPattern;
		private BizHawk.WinForms.Controls.LocSzSingleColumnFLP flpDialog;
		private BizHawk.WinForms.Controls.LocSzSingleRowFLP flpDialogButtons;
		private BizHawk.WinForms.Controls.LabelEx lblPatternDesc;
		public BizHawk.WinForms.Controls.SzNUDEx nudPatternOn;
		private BizHawk.WinForms.Controls.CheckBoxEx cbConsiderLag;
	}
}
