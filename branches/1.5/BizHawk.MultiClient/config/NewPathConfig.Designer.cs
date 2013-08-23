namespace BizHawk.MultiClient
{
	partial class NewPathConfig
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
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.PathTabControl = new System.Windows.Forms.TabControl();
            this.SaveBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.RecentForROMs = new System.Windows.Forms.CheckBox();
            this.BasePathBox = new System.Windows.Forms.TextBox();
            this.BrowseBase = new System.Windows.Forms.Button();
            this.BaseDescription = new System.Windows.Forms.Label();
            this.DefaultsBtn = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(471, 411);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 0;
            this.OK.Text = "&Ok";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(552, 411);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 1;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // PathTabControl
            // 
            this.PathTabControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PathTabControl.Location = new System.Drawing.Point(12, 84);
            this.PathTabControl.Multiline = true;
            this.PathTabControl.Name = "PathTabControl";
            this.PathTabControl.SelectedIndex = 0;
            this.PathTabControl.Size = new System.Drawing.Size(615, 321);
            this.PathTabControl.TabIndex = 2;
            // 
            // SaveBtn
            // 
            this.SaveBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SaveBtn.Location = new System.Drawing.Point(12, 411);
            this.SaveBtn.Name = "SaveBtn";
            this.SaveBtn.Size = new System.Drawing.Size(75, 23);
            this.SaveBtn.TabIndex = 3;
            this.SaveBtn.Text = "&Save";
            this.SaveBtn.UseVisualStyleBackColor = true;
            this.SaveBtn.Click += new System.EventHandler(this.SaveBtn_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(527, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(97, 13);
            this.label1.TabIndex = 210;
            this.label1.Text = "Special Commands";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Image = global::BizHawk.MultiClient.Properties.Resources.Help;
            this.button1.Location = new System.Drawing.Point(496, 47);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(26, 23);
            this.button1.TabIndex = 209;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // RecentForROMs
            // 
            this.RecentForROMs.AutoSize = true;
            this.RecentForROMs.Location = new System.Drawing.Point(12, 51);
            this.RecentForROMs.Name = "RecentForROMs";
            this.RecentForROMs.Size = new System.Drawing.Size(184, 17);
            this.RecentForROMs.TabIndex = 207;
            this.RecentForROMs.Text = "Always use recent path for ROMs";
            this.RecentForROMs.UseVisualStyleBackColor = true;
            this.RecentForROMs.CheckedChanged += new System.EventHandler(this.RecentForROMs_CheckedChanged);
            // 
            // BasePathBox
            // 
            this.BasePathBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BasePathBox.Location = new System.Drawing.Point(12, 15);
            this.BasePathBox.Name = "BasePathBox";
            this.BasePathBox.Size = new System.Drawing.Size(510, 20);
            this.BasePathBox.TabIndex = 205;
            // 
            // BrowseBase
            // 
            this.BrowseBase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BrowseBase.Image = global::BizHawk.MultiClient.Properties.Resources.OpenFile;
            this.BrowseBase.Location = new System.Drawing.Point(530, 14);
            this.BrowseBase.Name = "BrowseBase";
            this.BrowseBase.Size = new System.Drawing.Size(26, 23);
            this.BrowseBase.TabIndex = 206;
            this.BrowseBase.UseVisualStyleBackColor = true;
            this.BrowseBase.Click += new System.EventHandler(this.BrowseBase_Click);
            // 
            // BaseDescription
            // 
            this.BaseDescription.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BaseDescription.AutoSize = true;
            this.BaseDescription.Location = new System.Drawing.Point(563, 19);
            this.BaseDescription.Name = "BaseDescription";
            this.BaseDescription.Size = new System.Drawing.Size(64, 13);
            this.BaseDescription.TabIndex = 208;
            this.BaseDescription.Text = "Global Base";
            // 
            // DefaultsBtn
            // 
            this.DefaultsBtn.Location = new System.Drawing.Point(93, 411);
            this.DefaultsBtn.Name = "DefaultsBtn";
            this.DefaultsBtn.Size = new System.Drawing.Size(75, 23);
            this.DefaultsBtn.TabIndex = 211;
            this.DefaultsBtn.Text = "&Defaults";
            this.DefaultsBtn.UseVisualStyleBackColor = true;
            this.DefaultsBtn.Click += new System.EventHandler(this.DefaultsBtn_Click);
            // 
            // NewPathConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(639, 446);
            this.Controls.Add(this.DefaultsBtn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.RecentForROMs);
            this.Controls.Add(this.BasePathBox);
            this.Controls.Add(this.BrowseBase);
            this.Controls.Add(this.BaseDescription);
            this.Controls.Add(this.SaveBtn);
            this.Controls.Add(this.PathTabControl);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.MinimumSize = new System.Drawing.Size(360, 250);
            this.Name = "NewPathConfig";
            this.ShowIcon = false;
            this.Text = "Path Configuration";
            this.Load += new System.EventHandler(this.NewPathConfig_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.TabControl PathTabControl;
		private System.Windows.Forms.Button SaveBtn;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.CheckBox RecentForROMs;
		private System.Windows.Forms.TextBox BasePathBox;
		private System.Windows.Forms.Button BrowseBase;
		private System.Windows.Forms.Label BaseDescription;
		private System.Windows.Forms.Button DefaultsBtn;
	}
}