namespace BizHawk.Client.EmuHawk
{
	partial class MultiDiskBundler
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultiDiskBundler));
			this.MultiDiskMenuStrip = new System.Windows.Forms.MenuStrip();
			this.SaveRunButton = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.BrowseBtn = new System.Windows.Forms.Button();
			this.NameBox = new System.Windows.Forms.TextBox();
			this.FileSelectorPanel = new System.Windows.Forms.Panel();
			this.AddButton = new System.Windows.Forms.Button();
			this.groupBox3.SuspendLayout();
			this.SuspendLayout();
			// 
			// MultiDiskMenuStrip
			// 
			this.MultiDiskMenuStrip.Location = new System.Drawing.Point(0, 0);
			this.MultiDiskMenuStrip.Name = "MultiDiskMenuStrip";
			this.MultiDiskMenuStrip.Size = new System.Drawing.Size(506, 24);
			this.MultiDiskMenuStrip.TabIndex = 0;
			this.MultiDiskMenuStrip.Text = "menuStrip1";
			// 
			// SaveRunButton
			// 
			this.SaveRunButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.SaveRunButton.Enabled = false;
			this.SaveRunButton.Location = new System.Drawing.Point(343, 329);
			this.SaveRunButton.Name = "SaveRunButton";
			this.SaveRunButton.Size = new System.Drawing.Size(85, 23);
			this.SaveRunButton.TabIndex = 9;
			this.SaveRunButton.Text = "&Save and Run";
			this.SaveRunButton.UseVisualStyleBackColor = true;
			this.SaveRunButton.Click += new System.EventHandler(this.SaveRunButton_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(434, 329);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 10;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// groupBox3
			// 
			this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox3.Controls.Add(this.BrowseBtn);
			this.groupBox3.Controls.Add(this.NameBox);
			this.groupBox3.Location = new System.Drawing.Point(8, 28);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(486, 45);
			this.groupBox3.TabIndex = 11;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Name";
			// 
			// BrowseBtn
			// 
			this.BrowseBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.BrowseBtn.Location = new System.Drawing.Point(417, 18);
			this.BrowseBtn.Name = "BrowseBtn";
			this.BrowseBtn.Size = new System.Drawing.Size(63, 23);
			this.BrowseBtn.TabIndex = 14;
			this.BrowseBtn.Text = "Browse...";
			this.BrowseBtn.UseVisualStyleBackColor = true;
			this.BrowseBtn.Click += new System.EventHandler(this.BrowseBtn_Click);
			// 
			// NameBox
			// 
			this.NameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.NameBox.Location = new System.Drawing.Point(6, 19);
			this.NameBox.Name = "NameBox";
			this.NameBox.Size = new System.Drawing.Size(405, 20);
			this.NameBox.TabIndex = 0;
			this.NameBox.TextChanged += new System.EventHandler(this.NameBox_TextChanged);
			// 
			// FileSelectorPanel
			// 
			this.FileSelectorPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.FileSelectorPanel.AutoScroll = true;
			this.FileSelectorPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.FileSelectorPanel.Location = new System.Drawing.Point(8, 79);
			this.FileSelectorPanel.Name = "FileSelectorPanel";
			this.FileSelectorPanel.Size = new System.Drawing.Size(486, 244);
			this.FileSelectorPanel.TabIndex = 12;
			// 
			// AddButton
			// 
			this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddButton.Location = new System.Drawing.Point(8, 329);
			this.AddButton.Name = "AddButton";
			this.AddButton.Size = new System.Drawing.Size(60, 23);
			this.AddButton.TabIndex = 13;
			this.AddButton.Text = "Add";
			this.AddButton.UseVisualStyleBackColor = true;
			this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
			// 
			// MultiDiskBundler
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(506, 364);
			this.Controls.Add(this.AddButton);
			this.Controls.Add(this.FileSelectorPanel);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.SaveRunButton);
			this.Controls.Add(this.MultiDiskMenuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.MultiDiskMenuStrip;
			this.Name = "MultiDiskBundler";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Multi-disk Bundler";
			this.Load += new System.EventHandler(this.MultiGameCreator_Load);
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip MultiDiskMenuStrip;
		private System.Windows.Forms.Button SaveRunButton;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.TextBox NameBox;
		private System.Windows.Forms.Panel FileSelectorPanel;
		private System.Windows.Forms.Button AddButton;
		private System.Windows.Forms.Button BrowseBtn;
	}
}