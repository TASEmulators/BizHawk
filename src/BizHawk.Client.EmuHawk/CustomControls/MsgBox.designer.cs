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
            this.messageLbl = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btn3 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chkBx
            // 
            resources.ApplyResources(this.chkBx, "chkBx");
            this.chkBx.Name = "chkBx";
            this.chkBx.UseVisualStyleBackColor = true;
            // 
            // btn1
            // 
            resources.ApplyResources(this.btn1, "btn1");
            this.btn1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn1.Name = "btn1";
            this.btn1.UseVisualStyleBackColor = true;
            this.btn1.Click += new System.EventHandler(this.ButtonClick);
            // 
            // btn2
            // 
            resources.ApplyResources(this.btn2, "btn2");
            this.btn2.Name = "btn2";
            this.btn2.UseVisualStyleBackColor = true;
            this.btn2.Click += new System.EventHandler(this.ButtonClick);
            // 
            // messageLbl
            // 
            resources.ApplyResources(this.messageLbl, "messageLbl");
            this.messageLbl.Name = "messageLbl";
            // 
            // btn3
            // 
            resources.ApplyResources(this.btn3, "btn3");
            this.btn3.Name = "btn3";
            this.btn3.UseVisualStyleBackColor = true;
            this.btn3.Click += new System.EventHandler(this.ButtonClick);
            // 
            // MsgBox
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btn1;
            this.Controls.Add(this.btn3);
            this.Controls.Add(this.chkBx);
            this.Controls.Add(this.btn1);
            this.Controls.Add(this.btn2);
            this.Controls.Add(this.messageLbl);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MsgBox";
            this.Load += new System.EventHandler(this.DialogBox_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox chkBx;
		private System.Windows.Forms.Button btn1;
		private System.Windows.Forms.Button btn2;
		private BizHawk.WinForms.Controls.LocLabelEx messageLbl;
		private System.Windows.Forms.Button btn3;
	}
}