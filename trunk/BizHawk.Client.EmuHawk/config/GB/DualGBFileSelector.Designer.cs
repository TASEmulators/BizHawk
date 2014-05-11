namespace BizHawk.Client.EmuHawk
{
	partial class DualGBFileSelector
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
			this.button1 = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.UseCurrentRomButton = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Location = new System.Drawing.Point(362, 3);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(60, 23);
			this.button1.TabIndex = 2;
			this.button1.Text = "Browse...";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.button1_Click);
			// 
			// textBox1
			// 
			this.textBox1.AllowDrop = true;
			this.textBox1.Location = new System.Drawing.Point(3, 5);
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(270, 20);
			this.textBox1.TabIndex = 1;
			this.textBox1.DragDrop += new System.Windows.Forms.DragEventHandler(this.textBox1_DragDrop);
			this.textBox1.DragEnter += new System.Windows.Forms.DragEventHandler(this.textBox1_DragEnter);
			// 
			// UseCurrentRomButton
			// 
			this.UseCurrentRomButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.UseCurrentRomButton.Location = new System.Drawing.Point(279, 3);
			this.UseCurrentRomButton.Name = "UseCurrentRomButton";
			this.UseCurrentRomButton.Size = new System.Drawing.Size(83, 23);
			this.UseCurrentRomButton.TabIndex = 3;
			this.UseCurrentRomButton.Text = "Current Rom";
			this.UseCurrentRomButton.UseVisualStyleBackColor = true;
			this.UseCurrentRomButton.Click += new System.EventHandler(this.UseCurrentRomButton_Click);
			// 
			// DualGBFileSelector
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.UseCurrentRomButton);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.textBox1);
			this.Name = "DualGBFileSelector";
			this.Size = new System.Drawing.Size(425, 29);
			this.Load += new System.EventHandler(this.DualGBFileSelector_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button UseCurrentRomButton;

	}
}
