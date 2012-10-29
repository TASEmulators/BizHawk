namespace BizHawk.MultiClient
{
	partial class VirtualPadForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VirtualPadForm));
			this.ControllerBox = new System.Windows.Forms.GroupBox();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autolaodToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveWindowPositionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.StickyBox = new System.Windows.Forms.CheckBox();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// ControllerBox
			// 
			this.ControllerBox.Location = new System.Drawing.Point(9, 26);
			this.ControllerBox.Name = "ControllerBox";
			this.ControllerBox.Size = new System.Drawing.Size(367, 207);
			this.ControllerBox.TabIndex = 5;
			this.ControllerBox.TabStop = false;
			this.ControllerBox.Text = "Controllers";
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(388, 24);
			this.menuStrip1.TabIndex = 6;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// optionsToolStripMenuItem
			// 
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.autolaodToolStripMenuItem,
            this.saveWindowPositionToolStripMenuItem,
            this.toolStripSeparator2,
            this.exitToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
			this.optionsToolStripMenuItem.Text = "&Options";
			this.optionsToolStripMenuItem.DropDownOpened += new System.EventHandler(this.optionsToolStripMenuItem_DropDownOpened);
			// 
			// autolaodToolStripMenuItem
			// 
			this.autolaodToolStripMenuItem.Name = "autolaodToolStripMenuItem";
			this.autolaodToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.autolaodToolStripMenuItem.Text = "&Autoload";
			this.autolaodToolStripMenuItem.Click += new System.EventHandler(this.autolaodToolStripMenuItem_Click);
			// 
			// saveWindowPositionToolStripMenuItem
			// 
			this.saveWindowPositionToolStripMenuItem.Name = "saveWindowPositionToolStripMenuItem";
			this.saveWindowPositionToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.saveWindowPositionToolStripMenuItem.Text = "&Save Window Position";
			this.saveWindowPositionToolStripMenuItem.Click += new System.EventHandler(this.saveWindowPositionToolStripMenuItem_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(188, 6);
			// 
			// exitToolStripMenuItem
			// 
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeyDisplayString = "Alt+F4";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			// 
			// StickyBox
			// 
			this.StickyBox.AutoSize = true;
			this.StickyBox.Enabled = false;
			this.StickyBox.Location = new System.Drawing.Point(12, 247);
			this.StickyBox.Name = "StickyBox";
			this.StickyBox.Size = new System.Drawing.Size(55, 17);
			this.StickyBox.TabIndex = 7;
			this.StickyBox.Text = "Sticky";
			this.StickyBox.UseVisualStyleBackColor = true;
			this.StickyBox.CheckedChanged += new System.EventHandler(this.StickyBox_CheckedChanged);
			// 
			// VirtualPadForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(388, 274);
			this.Controls.Add(this.StickyBox);
			this.Controls.Add(this.ControllerBox);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.MaximumSize = new System.Drawing.Size(404, 312);
			this.MinimumSize = new System.Drawing.Size(404, 312);
			this.Name = "VirtualPadForm";
			this.Text = "Virtual Pads";
			this.Load += new System.EventHandler(this.VirtualPadForm_Load);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox ControllerBox;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem autolaodToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveWindowPositionToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.CheckBox StickyBox;
	}
}