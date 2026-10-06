namespace BizHawk.Client.EmuHawk
{
	partial class BizBoxInfoControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BizBoxInfoControl));
            this.CoreNameLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.CoreAuthorLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.CorePortedLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.CoreUrlLink = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            // 
            // CoreNameLabel
            // 
            resources.ApplyResources(this.CoreNameLabel, "CoreNameLabel");
            this.CoreNameLabel.Name = "CoreNameLabel";
            // 
            // CoreAuthorLabel
            // 
            resources.ApplyResources(this.CoreAuthorLabel, "CoreAuthorLabel");
            this.CoreAuthorLabel.Name = "CoreAuthorLabel";
            // 
            // CorePortedLabel
            // 
            resources.ApplyResources(this.CorePortedLabel, "CorePortedLabel");
            this.CorePortedLabel.Name = "CorePortedLabel";
            // 
            // CoreUrlLink
            // 
            resources.ApplyResources(this.CoreUrlLink, "CoreUrlLink");
            this.CoreUrlLink.Name = "CoreUrlLink";
            this.CoreUrlLink.TabStop = true;
            this.CoreUrlLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CoreUrlLink_LinkClicked);
            // 
            // BizBoxInfoControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.CoreUrlLink);
            this.Controls.Add(this.CorePortedLabel);
            this.Controls.Add(this.CoreAuthorLabel);
            this.Controls.Add(this.CoreNameLabel);
            this.Name = "BizBoxInfoControl";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.LocLabelEx CoreNameLabel;
		private BizHawk.WinForms.Controls.LocLabelEx CoreAuthorLabel;
		private BizHawk.WinForms.Controls.LocLabelEx CorePortedLabel;
		private System.Windows.Forms.LinkLabel CoreUrlLink;
	}
}
