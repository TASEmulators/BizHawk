namespace BizHawk.Client.EmuHawk
{
	partial class HotkeyConfig
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HotkeyConfig));
			this.label38 = new System.Windows.Forms.Label();
			this.AutoTabCheckBox = new System.Windows.Forms.CheckBox();
			this.HotkeyTabControl = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.IDB_CANCEL = new System.Windows.Forms.Button();
			this.IDB_SAVE = new System.Windows.Forms.Button();
			this.SearchBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.MiscButton = new BizHawk.Client.EmuHawk.MenuButton();
			this.clearBtnContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.clearAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.clearCurrentTabToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.restoreDefaultsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.HotkeyTabControl.SuspendLayout();
			this.clearBtnContextMenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// label38
			// 
			this.label38.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label38.AutoSize = true;
			this.label38.Location = new System.Drawing.Point(39, 441);
			this.label38.Name = "label38";
			this.label38.Size = new System.Drawing.Size(153, 13);
			this.label38.TabIndex = 4;
			this.label38.Text = "* Escape clears a key mapping";
			// 
			// AutoTabCheckBox
			// 
			this.AutoTabCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AutoTabCheckBox.AutoSize = true;
			this.AutoTabCheckBox.Location = new System.Drawing.Point(432, 440);
			this.AutoTabCheckBox.Name = "AutoTabCheckBox";
			this.AutoTabCheckBox.Size = new System.Drawing.Size(70, 17);
			this.AutoTabCheckBox.TabIndex = 101;
			this.AutoTabCheckBox.Text = "Auto Tab";
			this.AutoTabCheckBox.UseVisualStyleBackColor = true;
			this.AutoTabCheckBox.CheckedChanged += new System.EventHandler(this.AutoTabCheckBox_CheckedChanged);
			// 
			// HotkeyTabControl
			// 
			this.HotkeyTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.HotkeyTabControl.Controls.Add(this.tabPage1);
			this.HotkeyTabControl.Location = new System.Drawing.Point(12, 28);
			this.HotkeyTabControl.Name = "HotkeyTabControl";
			this.HotkeyTabControl.SelectedIndex = 0;
			this.HotkeyTabControl.Size = new System.Drawing.Size(729, 396);
			this.HotkeyTabControl.TabIndex = 102;
			this.HotkeyTabControl.SelectedIndexChanged += new System.EventHandler(this.HotkeyTabControl_SelectedIndexChanged);
			// 
			// tabPage1
			// 
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(721, 370);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "For designer";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// IDB_CANCEL
			// 
			this.IDB_CANCEL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.IDB_CANCEL.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.IDB_CANCEL.Location = new System.Drawing.Point(681, 436);
			this.IDB_CANCEL.Name = "IDB_CANCEL";
			this.IDB_CANCEL.Size = new System.Drawing.Size(60, 22);
			this.IDB_CANCEL.TabIndex = 103;
			this.IDB_CANCEL.TabStop = false;
			this.IDB_CANCEL.Text = "&Cancel";
			this.IDB_CANCEL.UseVisualStyleBackColor = true;
			this.IDB_CANCEL.Click += new System.EventHandler(this.IDB_CANCEL_Click);
			// 
			// IDB_SAVE
			// 
			this.IDB_SAVE.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.IDB_SAVE.Location = new System.Drawing.Point(615, 436);
			this.IDB_SAVE.Name = "IDB_SAVE";
			this.IDB_SAVE.Size = new System.Drawing.Size(60, 22);
			this.IDB_SAVE.TabIndex = 104;
			this.IDB_SAVE.TabStop = false;
			this.IDB_SAVE.Text = "&Save";
			this.IDB_SAVE.UseVisualStyleBackColor = true;
			this.IDB_SAVE.Click += new System.EventHandler(this.IDB_SAVE_Click);
			// 
			// SearchBox
			// 
			this.SearchBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.SearchBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.SearchBox.Location = new System.Drawing.Point(592, 9);
			this.SearchBox.Name = "SearchBox";
			this.SearchBox.Size = new System.Drawing.Size(149, 20);
			this.SearchBox.TabIndex = 106;
			this.SearchBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchBox_KeyDown);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(556, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(30, 13);
			this.label1.TabIndex = 107;
			this.label1.Text = "Find:";
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(194, 441);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(168, 13);
			this.label2.TabIndex = 108;
			this.label2.Text = "* Disable Auto Tab to multiply bind";
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 441);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(30, 13);
			this.label3.TabIndex = 109;
			this.label3.Text = "Tips:";
			// 
			// MiscButton
			// 
			this.MiscButton.Location = new System.Drawing.Point(526, 436);
			this.MiscButton.Menu = this.clearBtnContextMenu;
			this.MiscButton.Name = "MiscButton";
			this.MiscButton.Size = new System.Drawing.Size(60, 22);
			this.MiscButton.TabIndex = 110;
			this.MiscButton.Text = "Misc...";
			this.MiscButton.UseVisualStyleBackColor = true;
			// 
			// clearBtnContextMenu
			// 
			this.clearBtnContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.restoreDefaultsToolStripMenuItem,
            this.toolStripSeparator1,
            this.clearAllToolStripMenuItem,
            this.clearCurrentTabToolStripMenuItem});
			this.clearBtnContextMenu.Name = "clearBtnContextMenu";
			this.clearBtnContextMenu.Size = new System.Drawing.Size(168, 76);
			// 
			// clearAllToolStripMenuItem
			// 
			this.clearAllToolStripMenuItem.Name = "clearAllToolStripMenuItem";
			this.clearAllToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.clearAllToolStripMenuItem.Text = "Clear All";
			this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.clearAllToolStripMenuItem_Click);
			// 
			// clearCurrentTabToolStripMenuItem
			// 
			this.clearCurrentTabToolStripMenuItem.Name = "clearCurrentTabToolStripMenuItem";
			this.clearCurrentTabToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.clearCurrentTabToolStripMenuItem.Text = "Clear Current Tab";
			this.clearCurrentTabToolStripMenuItem.Click += new System.EventHandler(this.clearCurrentTabToolStripMenuItem_Click);
			// 
			// restoreDefaultsToolStripMenuItem
			// 
			this.restoreDefaultsToolStripMenuItem.Name = "restoreDefaultsToolStripMenuItem";
			this.restoreDefaultsToolStripMenuItem.Size = new System.Drawing.Size(167, 22);
			this.restoreDefaultsToolStripMenuItem.Text = "Restore Defaults";
			this.restoreDefaultsToolStripMenuItem.Click += new System.EventHandler(this.restoreDefaultsToolStripMenuItem_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(164, 6);
			// 
			// HotkeyConfig
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.IDB_CANCEL;
			this.ClientSize = new System.Drawing.Size(753, 463);
			this.Controls.Add(this.MiscButton);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.SearchBox);
			this.Controls.Add(this.IDB_SAVE);
			this.Controls.Add(this.IDB_CANCEL);
			this.Controls.Add(this.HotkeyTabControl);
			this.Controls.Add(this.AutoTabCheckBox);
			this.Controls.Add(this.label38);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "HotkeyConfig";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Configure Hotkeys";
			this.Load += new System.EventHandler(this.NewHotkeyWindow_Load);
			this.HotkeyTabControl.ResumeLayout(false);
			this.clearBtnContextMenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label38;
		private System.Windows.Forms.CheckBox AutoTabCheckBox;
		private System.Windows.Forms.TabControl HotkeyTabControl;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Button IDB_CANCEL;
		private System.Windows.Forms.Button IDB_SAVE;
        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.Label label1;
				private System.Windows.Forms.Label label2;
				private System.Windows.Forms.Label label3;
				private System.Windows.Forms.ToolTip toolTip1;
				private MenuButton MiscButton;
				private System.Windows.Forms.ContextMenuStrip clearBtnContextMenu;
				private System.Windows.Forms.ToolStripMenuItem clearAllToolStripMenuItem;
				private System.Windows.Forms.ToolStripMenuItem clearCurrentTabToolStripMenuItem;
				private System.Windows.Forms.ToolStripMenuItem restoreDefaultsToolStripMenuItem;
				private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
	}
}