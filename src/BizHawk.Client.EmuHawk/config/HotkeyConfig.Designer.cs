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
            this.label38 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AutoTabCheckBox = new System.Windows.Forms.CheckBox();
            this.HotkeyTabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.IDB_CANCEL = new System.Windows.Forms.Button();
            this.IDB_SAVE = new System.Windows.Forms.Button();
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.MiscButton = new BizHawk.Client.EmuHawk.MenuButton();
            this.clearBtnContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.restoreDefaultsToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.restoreDefaultsForCurrentTabToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new BizHawk.WinForms.Controls.ToolStripSeparatorEx();
            this.clearAllToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.clearCurrentTabToolStripMenuItem = new BizHawk.WinForms.Controls.ToolStripMenuItemEx();
            this.HotkeyTabControl.SuspendLayout();
            this.clearBtnContextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // label38
            // 
            resources.ApplyResources(this.label38, "label38");
            this.label38.Name = "label38";
            // 
            // AutoTabCheckBox
            // 
            resources.ApplyResources(this.AutoTabCheckBox, "AutoTabCheckBox");
            this.AutoTabCheckBox.Name = "AutoTabCheckBox";
            this.AutoTabCheckBox.UseVisualStyleBackColor = true;
            this.AutoTabCheckBox.CheckedChanged += new System.EventHandler(this.AutoTabCheckBox_CheckedChanged);
            // 
            // HotkeyTabControl
            // 
            resources.ApplyResources(this.HotkeyTabControl, "HotkeyTabControl");
            this.HotkeyTabControl.Controls.Add(this.tabPage1);
            this.HotkeyTabControl.Name = "HotkeyTabControl";
            this.HotkeyTabControl.SelectedIndex = 0;
            this.HotkeyTabControl.SelectedIndexChanged += new System.EventHandler(this.HotkeyTabControl_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // IDB_CANCEL
            // 
            resources.ApplyResources(this.IDB_CANCEL, "IDB_CANCEL");
            this.IDB_CANCEL.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.IDB_CANCEL.Name = "IDB_CANCEL";
            this.IDB_CANCEL.TabStop = false;
            this.IDB_CANCEL.UseVisualStyleBackColor = true;
            this.IDB_CANCEL.Click += new System.EventHandler(this.IDB_CANCEL_Click);
            // 
            // IDB_SAVE
            // 
            resources.ApplyResources(this.IDB_SAVE, "IDB_SAVE");
            this.IDB_SAVE.Name = "IDB_SAVE";
            this.IDB_SAVE.TabStop = false;
            this.IDB_SAVE.UseVisualStyleBackColor = true;
            this.IDB_SAVE.Click += new System.EventHandler(this.IDB_SAVE_Click);
            // 
            // SearchBox
            // 
            resources.ApplyResources(this.SearchBox, "SearchBox");
            this.SearchBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SearchBox_KeyDown);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // MiscButton
            // 
            resources.ApplyResources(this.MiscButton, "MiscButton");
            this.MiscButton.Menu = this.clearBtnContextMenu;
            this.MiscButton.Name = "MiscButton";
            this.toolTip1.SetToolTip(this.MiscButton, resources.GetString("MiscButton.ToolTip"));
            this.MiscButton.UseVisualStyleBackColor = true;
            // 
            // clearBtnContextMenu
            // 
            resources.ApplyResources(this.clearBtnContextMenu, "clearBtnContextMenu");
            this.clearBtnContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.restoreDefaultsToolStripMenuItem,
            this.restoreDefaultsForCurrentTabToolStripMenuItem,
            this.toolStripSeparator1,
            this.clearAllToolStripMenuItem,
            this.clearCurrentTabToolStripMenuItem});
            this.clearBtnContextMenu.Name = "clearBtnContextMenu";
            this.toolTip1.SetToolTip(this.clearBtnContextMenu, resources.GetString("clearBtnContextMenu.ToolTip"));
            // 
            // restoreDefaultsToolStripMenuItem
            // 
            resources.ApplyResources(this.restoreDefaultsToolStripMenuItem, "restoreDefaultsToolStripMenuItem");
            this.restoreDefaultsToolStripMenuItem.Click += new System.EventHandler(this.RestoreDefaultsToolStripMenuItem_Click);
            // 
            // restoreDefaultsForCurrentTabToolStripMenuItem
            // 
            resources.ApplyResources(this.restoreDefaultsForCurrentTabToolStripMenuItem, "restoreDefaultsForCurrentTabToolStripMenuItem");
            this.restoreDefaultsForCurrentTabToolStripMenuItem.Name = "restoreDefaultsForCurrentTabToolStripMenuItem";
            this.restoreDefaultsForCurrentTabToolStripMenuItem.Click += new System.EventHandler(this.RestoreDefaultsCurrentTabToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // clearAllToolStripMenuItem
            // 
            resources.ApplyResources(this.clearAllToolStripMenuItem, "clearAllToolStripMenuItem");
            this.clearAllToolStripMenuItem.Click += new System.EventHandler(this.ClearAllToolStripMenuItem_Click);
            // 
            // clearCurrentTabToolStripMenuItem
            // 
            resources.ApplyResources(this.clearCurrentTabToolStripMenuItem, "clearCurrentTabToolStripMenuItem");
            this.clearCurrentTabToolStripMenuItem.Click += new System.EventHandler(this.ClearCurrentTabToolStripMenuItem_Click);
            // 
            // HotkeyConfig
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.IDB_CANCEL;
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
            this.Name = "HotkeyConfig";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.HotkeyConfig_FormClosed);
            this.Load += new System.EventHandler(this.HotkeyConfig_Load);
            this.HotkeyTabControl.ResumeLayout(false);
            this.clearBtnContextMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private BizHawk.WinForms.Controls.LocLabelEx label38;
		private System.Windows.Forms.CheckBox AutoTabCheckBox;
		private System.Windows.Forms.TabControl HotkeyTabControl;
		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Button IDB_CANCEL;
		private System.Windows.Forms.Button IDB_SAVE;
        private System.Windows.Forms.TextBox SearchBox;
        private BizHawk.WinForms.Controls.LocLabelEx label1;
				private BizHawk.WinForms.Controls.LocLabelEx label2;
				private BizHawk.WinForms.Controls.LocLabelEx label3;
				private System.Windows.Forms.ToolTip toolTip1;
				private BizHawk.Client.EmuHawk.MenuButton MiscButton;
				private System.Windows.Forms.ContextMenuStrip clearBtnContextMenu;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx clearAllToolStripMenuItem;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx clearCurrentTabToolStripMenuItem;
				private BizHawk.WinForms.Controls.ToolStripMenuItemEx restoreDefaultsToolStripMenuItem;
				private BizHawk.WinForms.Controls.ToolStripSeparatorEx toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem restoreDefaultsForCurrentTabToolStripMenuItem;
	}
}
