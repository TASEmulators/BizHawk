namespace BizHawk.Client.EmuHawk
{
	partial class MultiDiskBundler
	{
		/// <summary>SystemLabel
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultiDiskBundler));
            this.MultiDiskMenuStrip = new System.Windows.Forms.MenuStrip();
            this.SaveRunButton = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.grpName = new System.Windows.Forms.GroupBox();
            this.BrowseBtn = new System.Windows.Forms.Button();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.FileSelectorPanel = new System.Windows.Forms.Panel();
            this.AddButton = new System.Windows.Forms.Button();
            this.SystemDropDown = new System.Windows.Forms.ComboBox();
            this.SystemLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnRemove = new System.Windows.Forms.Button();
            this.SaveButton = new System.Windows.Forms.Button();
            this.grpName.SuspendLayout();
            this.SuspendLayout();
            // 
            // MultiDiskMenuStrip
            // 
            resources.ApplyResources(this.MultiDiskMenuStrip, "MultiDiskMenuStrip");
            this.MultiDiskMenuStrip.Name = "MultiDiskMenuStrip";
            // 
            // SaveRunButton
            // 
            resources.ApplyResources(this.SaveRunButton, "SaveRunButton");
            this.SaveRunButton.Name = "SaveRunButton";
            this.SaveRunButton.UseVisualStyleBackColor = true;
            this.SaveRunButton.Click += new System.EventHandler(this.SaveRunButton_Click);
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // grpName
            // 
            resources.ApplyResources(this.grpName, "grpName");
            this.grpName.Controls.Add(this.BrowseBtn);
            this.grpName.Controls.Add(this.NameBox);
            this.grpName.Name = "grpName";
            this.grpName.TabStop = false;
            // 
            // BrowseBtn
            // 
            resources.ApplyResources(this.BrowseBtn, "BrowseBtn");
            this.BrowseBtn.Name = "BrowseBtn";
            this.BrowseBtn.UseVisualStyleBackColor = true;
            this.BrowseBtn.Click += new System.EventHandler(this.BrowseBtn_Click);
            // 
            // NameBox
            // 
            resources.ApplyResources(this.NameBox, "NameBox");
            this.NameBox.Name = "NameBox";
            this.NameBox.TextChanged += new System.EventHandler(this.NameBox_TextChanged);
            // 
            // FileSelectorPanel
            // 
            resources.ApplyResources(this.FileSelectorPanel, "FileSelectorPanel");
            this.FileSelectorPanel.AllowDrop = true;
            this.FileSelectorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.FileSelectorPanel.Name = "FileSelectorPanel";
            this.FileSelectorPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDragDrop);
            this.FileSelectorPanel.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnter);
            // 
            // AddButton
            // 
            resources.ApplyResources(this.AddButton, "AddButton");
            this.AddButton.Name = "AddButton";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // SystemDropDown
            // 
            resources.ApplyResources(this.SystemDropDown, "SystemDropDown");
            this.SystemDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SystemDropDown.FormattingEnabled = true;
            this.SystemDropDown.Name = "SystemDropDown";
            this.SystemDropDown.SelectedIndexChanged += new System.EventHandler(this.SystemDropDown_SelectedIndexChanged);
            // 
            // SystemLabel
            // 
            resources.ApplyResources(this.SystemLabel, "SystemLabel");
            this.SystemLabel.Name = "SystemLabel";
            // 
            // btnRemove
            // 
            resources.ApplyResources(this.btnRemove, "btnRemove");
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // SaveButton
            // 
            resources.ApplyResources(this.SaveButton, "SaveButton");
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // MultiDiskBundler
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.SystemLabel);
            this.Controls.Add(this.SystemDropDown);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.FileSelectorPanel);
            this.Controls.Add(this.grpName);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.SaveRunButton);
            this.Controls.Add(this.MultiDiskMenuStrip);
            this.MainMenuStrip = this.MultiDiskMenuStrip;
            this.Name = "MultiDiskBundler";
            this.grpName.ResumeLayout(false);
            this.grpName.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip MultiDiskMenuStrip;
		private System.Windows.Forms.Button SaveRunButton;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.GroupBox grpName;
		private System.Windows.Forms.TextBox NameBox;
		private System.Windows.Forms.Panel FileSelectorPanel;
		private System.Windows.Forms.Button AddButton;
		private System.Windows.Forms.Button BrowseBtn;
		private System.Windows.Forms.ComboBox SystemDropDown;
		private BizHawk.WinForms.Controls.LocLabelEx SystemLabel;
		private System.Windows.Forms.Button btnRemove;
		private System.Windows.Forms.Button SaveButton;
	}
}
