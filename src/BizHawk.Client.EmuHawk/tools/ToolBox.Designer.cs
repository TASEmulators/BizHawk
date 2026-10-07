using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class ToolBox
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToolBox));
            this.ToolBoxStrip = new BizHawk.WinForms.Controls.ToolStripEx();
            this.SuspendLayout();
            // 
            // ToolBoxStrip
            // 
            resources.ApplyResources(this.ToolBoxStrip, "ToolBoxStrip");
            this.ToolBoxStrip.BackColor = System.Drawing.SystemColors.Control;
            this.ToolBoxStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.ToolBoxStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.ToolBoxStrip.Name = "ToolBoxStrip";
            this.ToolBoxStrip.Stretch = true;
            this.ToolBoxStrip.TabStop = true;
            // 
            // ToolBox
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ToolBoxStrip);
            this.Name = "ToolBox";
            this.Load += new System.EventHandler(this.ToolBox_Load);
            this.ResumeLayout(false);

		}

		#endregion

		private ToolStripEx ToolBoxStrip;

	}
}