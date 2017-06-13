namespace BizHawk.Client.EmuHawk
{
	partial class SNESOptions
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
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbDoubleSize = new System.Windows.Forms.CheckBox();
            this.lblDoubleSize = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.Location = new System.Drawing.Point(136, 117);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.BtnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(217, 117);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // cbDoubleSize
            // 
            this.cbDoubleSize.AutoSize = true;
            this.cbDoubleSize.Location = new System.Drawing.Point(18, 20);
            this.cbDoubleSize.Name = "cbDoubleSize";
            this.cbDoubleSize.Size = new System.Drawing.Size(178, 17);
            this.cbDoubleSize.TabIndex = 6;
            this.cbDoubleSize.Text = "Always Double-Size Framebuffer";
            this.cbDoubleSize.UseVisualStyleBackColor = true;
            this.cbDoubleSize.CheckedChanged += new System.EventHandler(this.CbDoubleSize_CheckedChanged);
            // 
            // lblDoubleSize
            // 
            this.lblDoubleSize.Location = new System.Drawing.Point(36, 41);
            this.lblDoubleSize.Name = "lblDoubleSize";
            this.lblDoubleSize.Size = new System.Drawing.Size(254, 57);
            this.lblDoubleSize.TabIndex = 7;
            this.lblDoubleSize.Text = "Some games are changing the resolution constantly (e.g. SD3) so this option can f" +
    "orce the SNES output to stay double-size always. NOTE: The Accuracy core runs as" +
    " if this is selected.\r\n";
            // 
            // SNESOptions
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(304, 152);
            this.Controls.Add(this.lblDoubleSize);
            this.Controls.Add(this.cbDoubleSize);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SNESOptions";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "SNES Options";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.CheckBox cbDoubleSize;
		private System.Windows.Forms.Label lblDoubleSize;
	}
}