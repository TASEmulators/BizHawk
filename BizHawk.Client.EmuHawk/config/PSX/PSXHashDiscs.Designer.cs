namespace BizHawk.Client.EmuHawk
{
	partial class PSXHashDiscs
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
			this.btnClose = new System.Windows.Forms.Button();
			this.btnHash = new System.Windows.Forms.Button();
			this.txtHashes = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnClose
			// 
			this.btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnClose.Location = new System.Drawing.Point(347, 239);
			this.btnClose.Name = "btnClose";
			this.btnClose.Size = new System.Drawing.Size(75, 23);
			this.btnClose.TabIndex = 0;
			this.btnClose.Text = "Close";
			this.btnClose.UseVisualStyleBackColor = true;
			// 
			// btnHash
			// 
			this.btnHash.Location = new System.Drawing.Point(27, 239);
			this.btnHash.Name = "btnHash";
			this.btnHash.Size = new System.Drawing.Size(75, 23);
			this.btnHash.TabIndex = 1;
			this.btnHash.Text = "Hash";
			this.btnHash.UseVisualStyleBackColor = true;
			this.btnHash.Click += new System.EventHandler(this.btnHash_Click);
			// 
			// txtHashes
			// 
			this.txtHashes.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtHashes.Location = new System.Drawing.Point(27, 51);
			this.txtHashes.Multiline = true;
			this.txtHashes.Name = "txtHashes";
			this.txtHashes.ReadOnly = true;
			this.txtHashes.Size = new System.Drawing.Size(395, 146);
			this.txtHashes.TabIndex = 2;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(24, 211);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(200, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Sorry, this is gonna freeze while it hashes";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(96, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(223, 26);
			this.label2.TabIndex = 4;
			this.label2.Text = "This tool hashes your discs in a standard way.\r\nCheck against redump.org \"Total\" " +
    "CRC-32";
			// 
			// PSXHashDiscs
			// 
			this.AcceptButton = this.btnClose;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnClose;
			this.ClientSize = new System.Drawing.Size(446, 276);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.txtHashes);
			this.Controls.Add(this.btnHash);
			this.Controls.Add(this.btnClose);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PSXHashDiscs";
			this.Text = "PSX Disc Hasher";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnClose;
		private System.Windows.Forms.Button btnHash;
		private System.Windows.Forms.TextBox txtHashes;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
	}
}