namespace BizHawk.Client.EmuHawk
{
	partial class NESSyncSettingsForm
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
			this.OkBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.dataGridView1 = new System.Windows.Forms.DataGridView();
			this.RegionComboBox = new System.Windows.Forms.ComboBox();
			this.HelpBtn = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.BoardPropertiesGroupBox = new System.Windows.Forms.GroupBox();
			this.label1 = new System.Windows.Forms.Label();
			this.InfoLabel = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.RamPatternOverrideBox = new BizHawk.Client.EmuHawk.HexTextBox();
			this.label4 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
			this.BoardPropertiesGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(221, 423);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(67, 23);
			this.OkBtn.TabIndex = 0;
			this.OkBtn.Text = "OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(294, 423);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(67, 23);
			this.CancelBtn.TabIndex = 1;
			this.CancelBtn.Text = "Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// dataGridView1
			// 
			this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView1.Location = new System.Drawing.Point(10, 19);
			this.dataGridView1.MultiSelect = false;
			this.dataGridView1.Name = "dataGridView1";
			this.dataGridView1.Size = new System.Drawing.Size(333, 181);
			this.dataGridView1.TabIndex = 9;
			// 
			// RegionComboBox
			// 
			this.RegionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.RegionComboBox.FormattingEnabled = true;
			this.RegionComboBox.Location = new System.Drawing.Point(12, 26);
			this.RegionComboBox.Name = "RegionComboBox";
			this.RegionComboBox.Size = new System.Drawing.Size(124, 21);
			this.RegionComboBox.TabIndex = 11;
			// 
			// HelpBtn
			// 
			this.HelpBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.HelpBtn.Image = global::BizHawk.Client.EmuHawk.Properties.Resources.Help;
			this.HelpBtn.Location = new System.Drawing.Point(12, 177);
			this.HelpBtn.Name = "HelpBtn";
			this.HelpBtn.Size = new System.Drawing.Size(23, 23);
			this.HelpBtn.TabIndex = 10;
			this.HelpBtn.UseVisualStyleBackColor = true;
			this.HelpBtn.Click += new System.EventHandler(this.HelpBtn_Click);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 10);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(87, 13);
			this.label2.TabIndex = 12;
			this.label2.Text = "Region Override:";
			// 
			// BoardPropertiesGroupBox
			// 
			this.BoardPropertiesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.BoardPropertiesGroupBox.Controls.Add(this.dataGridView1);
			this.BoardPropertiesGroupBox.Location = new System.Drawing.Point(12, 204);
			this.BoardPropertiesGroupBox.Name = "BoardPropertiesGroupBox";
			this.BoardPropertiesGroupBox.Size = new System.Drawing.Size(349, 206);
			this.BoardPropertiesGroupBox.TabIndex = 13;
			this.BoardPropertiesGroupBox.TabStop = false;
			this.BoardPropertiesGroupBox.Text = "Custom Board Properties";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 53);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(362, 13);
			this.label1.TabIndex = 14;
			this.label1.Text = "Region Override will be ignored when playing Famicom Disk System games.";
			// 
			// InfoLabel
			// 
			this.InfoLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.InfoLabel.AutoSize = true;
			this.InfoLabel.Location = new System.Drawing.Point(41, 182);
			this.InfoLabel.Name = "InfoLabel";
			this.InfoLabel.Size = new System.Drawing.Size(213, 13);
			this.InfoLabel.TabIndex = 15;
			this.InfoLabel.Text = "The current board has no custom properties";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 92);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(133, 13);
			this.label3.TabIndex = 16;
			this.label3.Text = "Initial Ram pattern override";
			// 
			// RamPatternOverrideBox
			// 
			this.RamPatternOverrideBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.RamPatternOverrideBox.Location = new System.Drawing.Point(12, 108);
			this.RamPatternOverrideBox.Name = "RamPatternOverrideBox";
			this.RamPatternOverrideBox.Nullable = true;
			this.RamPatternOverrideBox.Size = new System.Drawing.Size(165, 20);
			this.RamPatternOverrideBox.TabIndex = 17;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 131);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(269, 13);
			this.label4.TabIndex = 18;
			this.label4.Text = "Provides an override to the initial WRAM startup pattern";
			// 
			// NESSyncSettingsForm
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(373, 458);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.RamPatternOverrideBox);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.InfoLabel);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.BoardPropertiesGroupBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.RegionComboBox);
			this.Controls.Add(this.HelpBtn);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.MinimumSize = new System.Drawing.Size(210, 150);
			this.Name = "NESSyncSettingsForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "NES Advanced Settings";
			((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
			this.BoardPropertiesGroupBox.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.DataGridView dataGridView1;
		private System.Windows.Forms.ComboBox RegionComboBox;
		private System.Windows.Forms.Button HelpBtn;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		internal System.Windows.Forms.GroupBox BoardPropertiesGroupBox;
		private System.Windows.Forms.Label InfoLabel;
		private System.Windows.Forms.Label label3;
		private HexTextBox RamPatternOverrideBox;
		private System.Windows.Forms.Label label4;
	}
}