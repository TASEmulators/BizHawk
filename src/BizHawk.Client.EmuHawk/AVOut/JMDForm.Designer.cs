namespace BizHawk.Client.EmuHawk
{
	partial class JmdForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(JmdForm));
            this.okButton = new System.Windows.Forms.Button();
            this.threadsBar = new System.Windows.Forms.TrackBar();
            this.compressionBar = new System.Windows.Forms.TrackBar();
            this.threadLeft = new BizHawk.WinForms.Controls.LocLabelEx();
            this.threadRight = new BizHawk.WinForms.Controls.LocLabelEx();
            this.compressionLeft = new BizHawk.WinForms.Controls.LocLabelEx();
            this.compressionRight = new BizHawk.WinForms.Controls.LocLabelEx();
            this.threadTop = new BizHawk.WinForms.Controls.LocLabelEx();
            this.compressionTop = new BizHawk.WinForms.Controls.LocLabelEx();
            this.cancelButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.threadsBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.compressionBar)).BeginInit();
            this.SuspendLayout();
            // 
            // okButton
            // 
            resources.ApplyResources(this.okButton, "okButton");
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Name = "okButton";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // threadsBar
            // 
            resources.ApplyResources(this.threadsBar, "threadsBar");
            this.threadsBar.Name = "threadsBar";
            this.threadsBar.Scroll += new System.EventHandler(this.ThreadsBar_Scroll);
            // 
            // compressionBar
            // 
            resources.ApplyResources(this.compressionBar, "compressionBar");
            this.compressionBar.Name = "compressionBar";
            this.compressionBar.Scroll += new System.EventHandler(this.CompressionBar_Scroll);
            // 
            // threadLeft
            // 
            resources.ApplyResources(this.threadLeft, "threadLeft");
            this.threadLeft.Name = "threadLeft";
            // 
            // threadRight
            // 
            resources.ApplyResources(this.threadRight, "threadRight");
            this.threadRight.Name = "threadRight";
            // 
            // compressionLeft
            // 
            resources.ApplyResources(this.compressionLeft, "compressionLeft");
            this.compressionLeft.Name = "compressionLeft";
            // 
            // compressionRight
            // 
            resources.ApplyResources(this.compressionRight, "compressionRight");
            this.compressionRight.Name = "compressionRight";
            // 
            // threadTop
            // 
            resources.ApplyResources(this.threadTop, "threadTop");
            this.threadTop.Name = "threadTop";
            // 
            // compressionTop
            // 
            resources.ApplyResources(this.compressionTop, "compressionTop");
            this.compressionTop.Name = "compressionTop";
            // 
            // cancelButton
            // 
            resources.ApplyResources(this.cancelButton, "cancelButton");
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // JmdForm
            // 
            this.AcceptButton = this.okButton;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.compressionTop);
            this.Controls.Add(this.threadTop);
            this.Controls.Add(this.compressionRight);
            this.Controls.Add(this.compressionLeft);
            this.Controls.Add(this.threadRight);
            this.Controls.Add(this.threadLeft);
            this.Controls.Add(this.compressionBar);
            this.Controls.Add(this.threadsBar);
            this.Controls.Add(this.okButton);
            this.Name = "JmdForm";
            this.ShowIcon = false;
            ((System.ComponentModel.ISupportInitialize)(this.threadsBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.compressionBar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.TrackBar threadsBar;
		private System.Windows.Forms.TrackBar compressionBar;
		private BizHawk.WinForms.Controls.LocLabelEx threadLeft;
		private BizHawk.WinForms.Controls.LocLabelEx threadRight;
		private BizHawk.WinForms.Controls.LocLabelEx compressionLeft;
		private BizHawk.WinForms.Controls.LocLabelEx compressionRight;
		private BizHawk.WinForms.Controls.LocLabelEx threadTop;
		private BizHawk.WinForms.Controls.LocLabelEx compressionTop;
		private System.Windows.Forms.Button cancelButton;
	}
}