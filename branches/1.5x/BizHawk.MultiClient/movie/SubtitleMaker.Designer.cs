namespace BizHawk.MultiClient
{
	partial class SubtitleMaker
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SubtitleMaker));
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.Message = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.YNumeric = new System.Windows.Forms.NumericUpDown();
			this.XNumeric = new System.Windows.Forms.NumericUpDown();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.DurationNumeric = new System.Windows.Forms.NumericUpDown();
			this.label4 = new System.Windows.Forms.Label();
			this.ColorPanel = new System.Windows.Forms.Panel();
			this.label5 = new System.Windows.Forms.Label();
			this.colorDialog1 = new System.Windows.Forms.ColorDialog();
			this.FrameNumeric = new System.Windows.Forms.NumericUpDown();
			this.label6 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.DurationNumeric)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.FrameNumeric)).BeginInit();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(267, 164);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 0;
			this.OK.Text = "&Save";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(348, 164);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// Message
			// 
			this.Message.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.Message.Location = new System.Drawing.Point(12, 69);
			this.Message.MaxLength = 512;
			this.Message.Name = "Message";
			this.Message.Size = new System.Drawing.Size(416, 20);
			this.Message.TabIndex = 15;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 50);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(50, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Message";
			// 
			// YNumeric
			// 
			this.YNumeric.Location = new System.Drawing.Point(15, 130);
			this.YNumeric.Maximum = new decimal(new int[] {
            240,
            0,
            0,
            0});
			this.YNumeric.Name = "YNumeric";
			this.YNumeric.Size = new System.Drawing.Size(56, 20);
			this.YNumeric.TabIndex = 25;
			// 
			// XNumeric
			// 
			this.XNumeric.Location = new System.Drawing.Point(15, 106);
			this.XNumeric.Maximum = new decimal(new int[] {
            320,
            0,
            0,
            0});
			this.XNumeric.Name = "XNumeric";
			this.XNumeric.Size = new System.Drawing.Size(56, 20);
			this.XNumeric.TabIndex = 20;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(77, 108);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "X position";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(75, 133);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 13);
			this.label3.TabIndex = 7;
			this.label3.Text = "Y position";
			// 
			// DurationNumeric
			// 
			this.DurationNumeric.Location = new System.Drawing.Point(153, 108);
			this.DurationNumeric.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
			this.DurationNumeric.Name = "DurationNumeric";
			this.DurationNumeric.Size = new System.Drawing.Size(56, 20);
			this.DurationNumeric.TabIndex = 30;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(215, 108);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(47, 13);
			this.label4.TabIndex = 9;
			this.label4.Text = "Duration";
			// 
			// ColorPanel
			// 
			this.ColorPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.ColorPanel.Location = new System.Drawing.Point(153, 131);
			this.ColorPanel.Name = "ColorPanel";
			this.ColorPanel.Size = new System.Drawing.Size(56, 19);
			this.ColorPanel.TabIndex = 35;
			this.ColorPanel.TabStop = true;
			this.ColorPanel.DoubleClick += new System.EventHandler(this.ColorPanel_DoubleClick);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(215, 133);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(31, 13);
			this.label5.TabIndex = 11;
			this.label5.Text = "Color";
			// 
			// FrameNumeric
			// 
			this.FrameNumeric.Location = new System.Drawing.Point(78, 19);
			this.FrameNumeric.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
			this.FrameNumeric.Name = "FrameNumeric";
			this.FrameNumeric.Size = new System.Drawing.Size(70, 20);
			this.FrameNumeric.TabIndex = 10;
			this.FrameNumeric.ThousandsSeparator = true;
			this.FrameNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(12, 21);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(36, 13);
			this.label6.TabIndex = 13;
			this.label6.Text = "Frame";
			// 
			// SubtitleMaker
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(435, 199);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.FrameNumeric);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.ColorPanel);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.DurationNumeric);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.XNumeric);
			this.Controls.Add(this.YNumeric);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.Message);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(272, 214);
			this.Name = "SubtitleMaker";
			this.Text = "Subtitle Maker";
			this.Load += new System.EventHandler(this.SubtitleMaker_Load);
			((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.DurationNumeric)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.FrameNumeric)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.TextBox Message;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown YNumeric;
		private System.Windows.Forms.NumericUpDown XNumeric;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NumericUpDown DurationNumeric;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Panel ColorPanel;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.ColorDialog colorDialog1;
		private System.Windows.Forms.NumericUpDown FrameNumeric;
		private System.Windows.Forms.Label label6;
	}
}