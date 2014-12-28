namespace BizHawk.Client.EmuHawk.tools.TAStudio
{
	partial class HistoryBox
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
			this.HistoryGroupBox = new System.Windows.Forms.GroupBox();
			this.SuspendLayout();
			// 
			// HistoryGroupBox
			// 
			this.HistoryGroupBox.Location = new System.Drawing.Point(3, 3);
			this.HistoryGroupBox.Name = "HistoryGroupBox";
			this.HistoryGroupBox.Size = new System.Drawing.Size(198, 334);
			this.HistoryGroupBox.TabIndex = 0;
			this.HistoryGroupBox.TabStop = false;
			this.HistoryGroupBox.Text = "History";
			// 
			// HistoryBox
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.HistoryGroupBox);
			this.Name = "HistoryBox";
			this.Size = new System.Drawing.Size(204, 350);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox HistoryGroupBox;
	}
}
