namespace BizHawk.Client.EmuHawk
{
	partial class MessageRow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MessageRow));
            this.RowRadio = new BizHawk.WinForms.Controls.RadioButtonEx();
            this.LocationLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SuspendLayout();
            // 
            // RowRadio
            // 
            resources.ApplyResources(this.RowRadio, "RowRadio");
            this.RowRadio.Name = "RowRadio";
            this.RowRadio.CheckedChanged += new System.EventHandler(this.RowRadio_CheckedChanged);
            // 
            // LocationLabel
            // 
            resources.ApplyResources(this.LocationLabel, "LocationLabel");
            this.LocationLabel.AllowDrop = true;
            this.LocationLabel.Name = "LocationLabel";
            // 
            // MessageRow
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.LocationLabel);
            this.Controls.Add(this.RowRadio);
            this.Name = "MessageRow";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.RadioButtonEx RowRadio;
		private WinForms.Controls.LocLabelEx LocationLabel;
	}
}
