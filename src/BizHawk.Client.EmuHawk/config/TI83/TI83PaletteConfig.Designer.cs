namespace BizHawk.Client.EmuHawk
{
	partial class TI83PaletteConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TI83PaletteConfig));
            this.CancelBtn = new System.Windows.Forms.Button();
            this.OkBtn = new System.Windows.Forms.Button();
            this.BackgroundPanel = new System.Windows.Forms.Panel();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ForeGroundPanel = new System.Windows.Forms.Panel();
            this.DefaultsBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // OkBtn
            // 
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // BackgroundPanel
            // 
            resources.ApplyResources(this.BackgroundPanel, "BackgroundPanel");
            this.BackgroundPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.BackgroundPanel.Name = "BackgroundPanel";
            this.BackgroundPanel.Click += new System.EventHandler(this.BackgroundPanel_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // ForeGroundPanel
            // 
            resources.ApplyResources(this.ForeGroundPanel, "ForeGroundPanel");
            this.ForeGroundPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ForeGroundPanel.Name = "ForeGroundPanel";
            this.ForeGroundPanel.Click += new System.EventHandler(this.ForeGroundPanel_Click);
            // 
            // DefaultsBtn
            // 
            resources.ApplyResources(this.DefaultsBtn, "DefaultsBtn");
            this.DefaultsBtn.Name = "DefaultsBtn";
            this.DefaultsBtn.UseVisualStyleBackColor = true;
            this.DefaultsBtn.Click += new System.EventHandler(this.DefaultsBtn_Click);
            // 
            // TI83PaletteConfig
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.DefaultsBtn);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ForeGroundPanel);
            this.Controls.Add(this.BackgroundPanel);
            this.Controls.Add(this.OkBtn);
            this.Controls.Add(this.CancelBtn);
            this.Name = "TI83PaletteConfig";
            this.Load += new System.EventHandler(this.TI83PaletteConfig_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Panel BackgroundPanel;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.Panel ForeGroundPanel;
		private System.Windows.Forms.Button DefaultsBtn;
	}
}