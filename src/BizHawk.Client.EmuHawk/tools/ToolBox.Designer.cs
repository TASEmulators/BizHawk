﻿using BizHawk.WinForms.Controls;

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
            this.ToolBoxStrip = new BizHawk.WinForms.Controls.ToolStripEx();
            this.SuspendLayout();
            // 
            // ToolBoxStrip
            // 
            this.ToolBoxStrip.AutoSize = false;
            this.ToolBoxStrip.BackColor = System.Drawing.SystemColors.Control;
            this.ToolBoxStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ToolBoxStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.ToolBoxStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.ToolBoxStrip.Location = new System.Drawing.Point(0, 0);
            this.ToolBoxStrip.Name = "ToolBoxStrip";
            this.ToolBoxStrip.Padding = new System.Windows.Forms.Padding(0);
            this.ToolBoxStrip.Stretch = true;
            this.ToolBoxStrip.TabIndex = 0;
            this.ToolBoxStrip.TabStop = true;
            // 
            // ToolBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(184, 183);
            this.Controls.Add(this.ToolBoxStrip);
            this.MaximumSize = new System.Drawing.Size(270, 600);
            this.MinimumSize = new System.Drawing.Size(135, 39);
            this.Name = "ToolBox";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.ToolBox_Load);
            this.ResumeLayout(false);

		}

		#endregion

		private ToolStripEx ToolBoxStrip;

	}
}