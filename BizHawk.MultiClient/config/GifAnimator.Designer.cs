namespace BizHawk.MultiClient
{
	partial class GifAnimator
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
			this.Exit = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// Exit
			// 
			this.Exit.Location = new System.Drawing.Point(197, 227);
			this.Exit.Name = "Exit";
			this.Exit.Size = new System.Drawing.Size(75, 23);
			this.Exit.TabIndex = 0;
			this.Exit.Text = "E&xit";
			this.Exit.UseVisualStyleBackColor = true;
			this.Exit.Click += new System.EventHandler(this.Exit_Click);
			// 
			// GifAnimator
			// 
			this.AcceptButton = this.Exit;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.Exit);
			this.Name = "GifAnimator";
			this.Text = "GifAnimator";
			this.Load += new System.EventHandler(this.GifAnimator_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button Exit;
	}
}