namespace BizHawk.MultiClient
{
	partial class N64VideoConfig
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
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.VideoResolutionComboBox = new System.Windows.Forms.ComboBox();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(287, 360);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(368, 360);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// VideoResolutionComboBox
			// 
			this.VideoResolutionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.VideoResolutionComboBox.FormattingEnabled = true;
			this.VideoResolutionComboBox.Items.AddRange(new object[] {
            "320 x 240",
            "400 x 300",
            "480 x 360",
            "512 x 384",
            "640 x 480",
            "800 x 600",
            "1024 x 768",
            "1152 x 864",
            "1280 x 960",
            "1400 x 1050",
            "1600 x 1200",
            "1920 x 1440",
            "2048 x 1536"});
			this.VideoResolutionComboBox.Location = new System.Drawing.Point(16, 29);
			this.VideoResolutionComboBox.Name = "VideoResolutionComboBox";
			this.VideoResolutionComboBox.Size = new System.Drawing.Size(136, 21);
			this.VideoResolutionComboBox.TabIndex = 10;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(87, 13);
			this.label1.TabIndex = 11;
			this.label1.Text = "Video Resolution";
			// 
			// N64VideoConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(455, 395);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.VideoResolutionComboBox);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Name = "N64VideoConfig";
			this.ShowIcon = false;
			this.Text = "Video Configuration";
			this.Load += new System.EventHandler(this.N64VideoConfig_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.ComboBox VideoResolutionComboBox;
		private System.Windows.Forms.Label label1;
	}
}