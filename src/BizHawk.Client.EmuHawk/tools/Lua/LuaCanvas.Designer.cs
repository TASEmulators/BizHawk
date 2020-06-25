namespace BizHawk.Client.EmuHawk
{
	partial class LuaCanvas
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
            this.luaPictureBox = new BizHawk.Client.EmuHawk.LuaPictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.luaPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // luaPictureBox
            // 
            this.luaPictureBox.Location = new System.Drawing.Point(0, 0);
            this.luaPictureBox.Margin = new System.Windows.Forms.Padding(0);
            this.luaPictureBox.Name = "luaPictureBox";
            this.luaPictureBox.Size = new System.Drawing.Size(100, 50);
            this.luaPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.luaPictureBox.TabIndex = 0;
            this.luaPictureBox.TabStop = false;
            // 
            // LuaCanvas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.luaPictureBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "LuaCanvas";
            this.Text = "LuaCanvas";
            ((System.ComponentModel.ISupportInitialize)(this.luaPictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private LuaPictureBox luaPictureBox;
	}
}