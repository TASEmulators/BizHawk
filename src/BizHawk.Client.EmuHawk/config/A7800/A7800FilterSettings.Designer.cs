namespace BizHawk.Client.EmuHawk
{
	partial class A7800FilterSettings
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



		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.OkBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.Port1ComboBox = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(170, 112);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 3;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(236, 112);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 4;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 44);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(35, 13);
			this.label4.TabIndex = 15;
			this.label4.Text = "Filter: ";
			// 
			// Port1ComboBox
			// 
			this.Port1ComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.Port1ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.Port1ComboBox.FormattingEnabled = true;
			this.Port1ComboBox.Location = new System.Drawing.Point(12, 60);
			this.Port1ComboBox.Name = "Port1ComboBox";
			this.Port1ComboBox.Size = new System.Drawing.Size(284, 21);
			this.Port1ComboBox.TabIndex = 13;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 14);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(286, 13);
			this.label1.TabIndex = 17;
			this.label1.Text = "A7800 Filter Settings: Needed For Tower Toppler and Jinks";
			// 
			// A7800FilterSettings
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(308, 147);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.Port1ComboBox);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.Icon = global::BizHawk.Client.EmuHawk.Properties.Resources.GameController_MultiSize;
			this.Name = "A7800FilterSettings";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Filter Settings";
			this.Load += new System.EventHandler(this.A7800FilterSettings_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}



		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox Port1ComboBox;
		private System.Windows.Forms.Label label1;
	}
}