namespace BizHawk.Client.EmuHawk.CustomControls
{
	partial class MsgBox
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MsgBox));
			this.chkBx = new System.Windows.Forms.CheckBox();
			this.btn1 = new System.Windows.Forms.Button();
			this.btn2 = new System.Windows.Forms.Button();
			this.messageLbl = new System.Windows.Forms.Label();
			this.btn3 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// chkBx
			// 
			this.chkBx.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.chkBx.AutoSize = true;
			this.chkBx.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.chkBx.Location = new System.Drawing.Point(12, 114);
			this.chkBx.Name = "chkBx";
			this.chkBx.Size = new System.Drawing.Size(152, 20);
			this.chkBx.TabIndex = 22;
			this.chkBx.Text = "Don\'t show this again";
			this.chkBx.UseVisualStyleBackColor = true;
			this.chkBx.Visible = false;
			// 
			// btn1
			// 
			this.btn1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btn1.AutoSize = true;
			this.btn1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btn1.Location = new System.Drawing.Point(236, 114);
			this.btn1.Name = "btn1";
			this.btn1.Size = new System.Drawing.Size(75, 23);
			this.btn1.TabIndex = 5;
			this.btn1.Text = "Button1";
			this.btn1.UseVisualStyleBackColor = true;
			this.btn1.Visible = false;
			this.btn1.Click += new System.EventHandler(this.btn_Click);
			// 
			// btn2
			// 
			this.btn2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btn2.AutoSize = true;
			this.btn2.Location = new System.Drawing.Point(317, 114);
			this.btn2.Name = "btn2";
			this.btn2.Size = new System.Drawing.Size(75, 23);
			this.btn2.TabIndex = 6;
			this.btn2.Text = "Button2";
			this.btn2.UseVisualStyleBackColor = true;
			this.btn2.Visible = false;
			this.btn2.Click += new System.EventHandler(this.btn_Click);
			// 
			// messageLbl
			// 
			this.messageLbl.AutoSize = true;
			this.messageLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.messageLbl.Location = new System.Drawing.Point(58, 10);
			this.messageLbl.Name = "messageLbl";
			this.messageLbl.Size = new System.Drawing.Size(73, 16);
			this.messageLbl.TabIndex = 19;
			this.messageLbl.Text = "[Message]";
			// 
			// btn3
			// 
			this.btn3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btn3.AutoSize = true;
			this.btn3.Location = new System.Drawing.Point(398, 114);
			this.btn3.Name = "btn3";
			this.btn3.Size = new System.Drawing.Size(75, 23);
			this.btn3.TabIndex = 7;
			this.btn3.Text = "Button3";
			this.btn3.UseVisualStyleBackColor = true;
			this.btn3.Visible = false;
			this.btn3.Click += new System.EventHandler(this.btn_Click);
			// 
			// DialogBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btn1;
			this.ClientSize = new System.Drawing.Size(485, 149);
			this.ControlBox = false;
			this.Controls.Add(this.btn3);
			this.Controls.Add(this.chkBx);
			this.Controls.Add(this.btn1);
			this.Controls.Add(this.btn2);
			this.Controls.Add(this.messageLbl);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DialogBox";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "[Title]";
			this.Load += new System.EventHandler(this.DialogBox_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox chkBx;
		private System.Windows.Forms.Button btn1;
		private System.Windows.Forms.Button btn2;
		private System.Windows.Forms.Label messageLbl;
		private System.Windows.Forms.Button btn3;
	}
}