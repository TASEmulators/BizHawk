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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.ForeGroundPanel = new System.Windows.Forms.Panel();
			this.DefaultsBtn = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(165, 112);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 0;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(99, 112);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 1;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// BackgroundPanel
			// 
			this.BackgroundPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.BackgroundPanel.Location = new System.Drawing.Point(12, 12);
			this.BackgroundPanel.Name = "BackgroundPanel";
			this.BackgroundPanel.Size = new System.Drawing.Size(40, 32);
			this.BackgroundPanel.TabIndex = 2;
			this.BackgroundPanel.Click += new System.EventHandler(this.BackgroundPanel_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(58, 22);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(65, 13);
			this.label1.TabIndex = 13;
			this.label1.Text = "Background";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(58, 60);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(61, 13);
			this.label2.TabIndex = 15;
			this.label2.Text = "Foreground";
			// 
			// ForeGroundPanel
			// 
			this.ForeGroundPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.ForeGroundPanel.Location = new System.Drawing.Point(12, 50);
			this.ForeGroundPanel.Name = "ForeGroundPanel";
			this.ForeGroundPanel.Size = new System.Drawing.Size(40, 32);
			this.ForeGroundPanel.TabIndex = 14;
			this.ForeGroundPanel.Click += new System.EventHandler(this.ForeGroundPanel_Click);
			// 
			// DefaultsBtn
			// 
			this.DefaultsBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.DefaultsBtn.Location = new System.Drawing.Point(165, 12);
			this.DefaultsBtn.Name = "DefaultsBtn";
			this.DefaultsBtn.Size = new System.Drawing.Size(60, 23);
			this.DefaultsBtn.TabIndex = 16;
			this.DefaultsBtn.Text = "&Defaults";
			this.DefaultsBtn.UseVisualStyleBackColor = true;
			this.DefaultsBtn.Click += new System.EventHandler(this.DefaultsBtn_Click);
			// 
			// TI83PaletteConfig
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(237, 147);
			this.Controls.Add(this.DefaultsBtn);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.ForeGroundPanel);
			this.Controls.Add(this.BackgroundPanel);
			this.Controls.Add(this.OkBtn);
			this.Controls.Add(this.CancelBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "TI83PaletteConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Palette Config";
			this.Load += new System.EventHandler(this.TI83PaletteConfig_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Panel BackgroundPanel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Panel ForeGroundPanel;
		private System.Windows.Forms.Button DefaultsBtn;
	}
}