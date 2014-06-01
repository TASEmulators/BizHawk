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
			this.CoreNameLabel = new System.Windows.Forms.Label();
			this.CoreAuthorLabel = new System.Windows.Forms.Label();
			this.CorePortedLabel = new System.Windows.Forms.Label();
			this.CoreUrlLink = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// CoreNameLabel
			// 
			this.CoreNameLabel.AutoSize = true;
			this.CoreNameLabel.Dock = System.Windows.Forms.DockStyle.Left;
			this.CoreNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.CoreNameLabel.Location = new System.Drawing.Point(0, 0);
			this.CoreNameLabel.MinimumSize = new System.Drawing.Size(100, 23);
			this.CoreNameLabel.Name = "CoreNameLabel";
			this.CoreNameLabel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
			this.CoreNameLabel.Size = new System.Drawing.Size(100, 23);
			this.CoreNameLabel.TabIndex = 0;
			this.CoreNameLabel.Text = "label1";
			// 
			// CoreAuthorLabel
			// 
			this.CoreAuthorLabel.AutoSize = true;
			this.CoreAuthorLabel.Dock = System.Windows.Forms.DockStyle.Left;
			this.CoreAuthorLabel.Location = new System.Drawing.Point(100, 0);
			this.CoreAuthorLabel.Name = "CoreAuthorLabel";
			this.CoreAuthorLabel.Padding = new System.Windows.Forms.Padding(5, 5, 0, 0);
			this.CoreAuthorLabel.Size = new System.Drawing.Size(40, 18);
			this.CoreAuthorLabel.TabIndex = 1;
			this.CoreAuthorLabel.Text = "label2";
			// 
			// CorePortedLabel
			// 
			this.CorePortedLabel.AutoSize = true;
			this.CorePortedLabel.Dock = System.Windows.Forms.DockStyle.Left;
			this.CorePortedLabel.Location = new System.Drawing.Point(140, 0);
			this.CorePortedLabel.Name = "CorePortedLabel";
			this.CorePortedLabel.Padding = new System.Windows.Forms.Padding(5, 5, 0, 0);
			this.CorePortedLabel.Size = new System.Drawing.Size(40, 18);
			this.CorePortedLabel.TabIndex = 2;
			this.CorePortedLabel.Text = "label3";
			// 
			// CoreUrlLink
			// 
			this.CoreUrlLink.AutoSize = true;
			this.CoreUrlLink.Dock = System.Windows.Forms.DockStyle.Left;
			this.CoreUrlLink.Location = new System.Drawing.Point(180, 0);
			this.CoreUrlLink.Name = "CoreUrlLink";
			this.CoreUrlLink.Padding = new System.Windows.Forms.Padding(5, 5, 0, 0);
			this.CoreUrlLink.Size = new System.Drawing.Size(60, 18);
			this.CoreUrlLink.TabIndex = 3;
			this.CoreUrlLink.TabStop = true;
			this.CoreUrlLink.Text = "linkLabel1";
			this.CoreUrlLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.CoreUrlLink_LinkClicked);
			// 
			// BizBoxInfoControl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.CoreUrlLink);
			this.Controls.Add(this.CorePortedLabel);
			this.Controls.Add(this.CoreAuthorLabel);
			this.Controls.Add(this.CoreNameLabel);
			this.Name = "BizBoxInfoControl";
			this.Size = new System.Drawing.Size(359, 25);
			this.Load += new System.EventHandler(this.BizBoxInfoControl_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label CoreNameLabel;
		private System.Windows.Forms.Label CoreAuthorLabel;
		private System.Windows.Forms.Label CorePortedLabel;
		private System.Windows.Forms.LinkLabel CoreUrlLink;
	}
}
