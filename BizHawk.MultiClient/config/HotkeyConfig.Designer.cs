namespace BizHawk.MultiClient
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HotkeyConfig));
            this.label38 = new System.Windows.Forms.Label();
            this.AutoTabCheckBox = new System.Windows.Forms.CheckBox();
            this.HotkeyTabControl = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.IDB_CANCEL = new System.Windows.Forms.Button();
            this.IDB_SAVE = new System.Windows.Forms.Button();
            this.RestoreDefaults = new System.Windows.Forms.Button();
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.HotkeyTabControl.SuspendLayout();
            this.SuspendLayout();
            // 
            // label38
            // 
            this.label38.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label38.AutoSize = true;
            this.label38.Location = new System.Drawing.Point(12, 441);
            this.label38.Name = "label38";
            this.label38.Size = new System.Drawing.Size(153, 13);
            this.label38.TabIndex = 4;
            this.label38.Text = "* Escape clears a key mapping";
            // 
            // AutoTabCheckBox
            // 
            this.AutoTabCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AutoTabCheckBox.AutoSize = true;
            this.AutoTabCheckBox.Location = new System.Drawing.Point(453, 440);
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
            // RestoreDefaults
            // 
            this.RestoreDefaults.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.RestoreDefaults.Location = new System.Drawing.Point(529, 436);
            this.RestoreDefaults.Name = "RestoreDefaults";
            this.RestoreDefaults.Size = new System.Drawing.Size(60, 22);
            this.RestoreDefaults.TabIndex = 105;
            this.RestoreDefaults.TabStop = false;
            this.RestoreDefaults.Text = "&Defaults";
            this.RestoreDefaults.UseVisualStyleBackColor = true;
            this.RestoreDefaults.Click += new System.EventHandler(this.RestoreDefaults_Click);
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
            // NewHotkeyWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.IDB_CANCEL;
            this.ClientSize = new System.Drawing.Size(753, 463);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SearchBox);
            this.Controls.Add(this.RestoreDefaults);
            this.Controls.Add(this.IDB_SAVE);
            this.Controls.Add(this.IDB_CANCEL);
            this.Controls.Add(this.HotkeyTabControl);
            this.Controls.Add(this.AutoTabCheckBox);
            this.Controls.Add(this.label38);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "NewHotkeyWindow";
            this.Text = "Configure Hotkeys";
            this.Load += new System.EventHandler(this.NewHotkeyWindow_Load);
            this.HotkeyTabControl.ResumeLayout(false);
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
		private System.Windows.Forms.Button RestoreDefaults;
        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.Label label1;
	}
}