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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NESSyncSettingsForm));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.RegionComboBox = new System.Windows.Forms.ComboBox();
            this.HelpBtn = new System.Windows.Forms.Button();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.BoardPropertiesGroupBox = new System.Windows.Forms.GroupBox();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.InfoLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.RamPatternOverrideBox = new BizHawk.Client.EmuHawk.HexTextBox();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.BoardPropertiesGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // OkBtn
            // 
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // dataGridView1
            // 
            resources.ApplyResources(this.dataGridView1, "dataGridView1");
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            // 
            // RegionComboBox
            // 
            resources.ApplyResources(this.RegionComboBox, "RegionComboBox");
            this.RegionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RegionComboBox.FormattingEnabled = true;
            this.RegionComboBox.Name = "RegionComboBox";
            // 
            // HelpBtn
            // 
            resources.ApplyResources(this.HelpBtn, "HelpBtn");
            this.HelpBtn.Name = "HelpBtn";
            this.HelpBtn.UseVisualStyleBackColor = true;
            this.HelpBtn.Click += new System.EventHandler(this.HelpBtn_Click);
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // BoardPropertiesGroupBox
            // 
            resources.ApplyResources(this.BoardPropertiesGroupBox, "BoardPropertiesGroupBox");
            this.BoardPropertiesGroupBox.Controls.Add(this.dataGridView1);
            this.BoardPropertiesGroupBox.Name = "BoardPropertiesGroupBox";
            this.BoardPropertiesGroupBox.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // InfoLabel
            // 
            resources.ApplyResources(this.InfoLabel, "InfoLabel");
            this.InfoLabel.Name = "InfoLabel";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // RamPatternOverrideBox
            // 
            resources.ApplyResources(this.RamPatternOverrideBox, "RamPatternOverrideBox");
            this.RamPatternOverrideBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.RamPatternOverrideBox.Name = "RamPatternOverrideBox";
            this.RamPatternOverrideBox.Nullable = true;
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // NESSyncSettingsForm
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
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
            this.Name = "NESSyncSettingsForm";
            this.ShowIcon = false;
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
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		internal System.Windows.Forms.GroupBox BoardPropertiesGroupBox;
		private BizHawk.WinForms.Controls.LocLabelEx InfoLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private HexTextBox RamPatternOverrideBox;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
	}
}