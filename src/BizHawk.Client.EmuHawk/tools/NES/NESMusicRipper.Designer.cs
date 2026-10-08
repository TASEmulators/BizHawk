using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	partial class NESMusicRipper
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NESMusicRipper));
            this.btnControl = new System.Windows.Forms.Button();
            this.txtDivider = new System.Windows.Forms.TextBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnExport = new System.Windows.Forms.Button();
            this.lblContents = new BizHawk.WinForms.Controls.LocLabelEx();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.txtPatternLength = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.menuStrip1 = new BizHawk.WinForms.Controls.MenuStripEx();
            this.FileSubMenu = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnControl
            // 
            resources.ApplyResources(this.btnControl, "btnControl");
            this.btnControl.Name = "btnControl";
            this.btnControl.UseVisualStyleBackColor = true;
            this.btnControl.Click += new System.EventHandler(this.BtnControl_Click);
            // 
            // txtDivider
            // 
            resources.ApplyResources(this.txtDivider, "txtDivider");
            this.txtDivider.Name = "txtDivider";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // btnExport
            // 
            resources.ApplyResources(this.btnExport, "btnExport");
            this.btnExport.Name = "btnExport";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.Export_Click);
            // 
            // lblContents
            // 
            resources.ApplyResources(this.lblContents, "lblContents");
            this.lblContents.Name = "lblContents";
            // 
            // textBox1
            // 
            resources.ApplyResources(this.textBox1, "textBox1");
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            // 
            // txtPatternLength
            // 
            resources.ApplyResources(this.txtPatternLength, "txtPatternLength");
            this.txtPatternLength.Name = "txtPatternLength";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.txtPatternLength);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.btnControl);
            this.groupBox2.Controls.Add(this.txtDivider);
            this.groupBox2.Controls.Add(this.btnExport);
            this.groupBox2.Controls.Add(this.lblContents);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.FileSubMenu});
            // 
            // FileSubMenu
            // 
            resources.ApplyResources(this.FileSubMenu, "FileSubMenu");
            // 
            // NESMusicRipper
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.menuStrip1);
            this.Name = "NESMusicRipper";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.NESMusicRipper_FormClosed);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnControl;
		private System.Windows.Forms.TextBox txtDivider;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button btnExport;
		private BizHawk.WinForms.Controls.LocLabelEx lblContents;
		private MenuStripEx menuStrip1;
		private BizHawk.WinForms.Controls.ToolStripMenuItemEx FileSubMenu;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.TextBox txtPatternLength;
		private System.Windows.Forms.GroupBox groupBox1;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.GroupBox groupBox2;
	}
}