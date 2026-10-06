namespace BizHawk.Client.EmuHawk
{
	partial class CheatEdit
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CheatEdit));
            this.NameBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AddressLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AddressHexIndLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
            this.ValueHexIndLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.ValueLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.CompareHexIndLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.CompareLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.DomainLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.DomainDropDown = new System.Windows.Forms.ComboBox();
            this.SizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SizeDropDown = new System.Windows.Forms.ComboBox();
            this.DisplayTypeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.DisplayTypeDropDown = new System.Windows.Forms.ComboBox();
            this.BigEndianCheckBox = new System.Windows.Forms.CheckBox();
            this.AddButton = new System.Windows.Forms.Button();
            this.EditButton = new System.Windows.Forms.Button();
            this.CompareBox = new BizHawk.Client.EmuHawk.WatchValueBox();
            this.ValueBox = new BizHawk.Client.EmuHawk.WatchValueBox();
            this.CompareTypeDropDown = new System.Windows.Forms.ComboBox();
            this.CompareTypeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SuspendLayout();
            // 
            // NameBox
            // 
            resources.ApplyResources(this.NameBox, "NameBox");
            this.NameBox.Name = "NameBox";
            // 
            // NameLabel
            // 
            resources.ApplyResources(this.NameLabel, "NameLabel");
            this.NameLabel.Name = "NameLabel";
            // 
            // AddressLabel
            // 
            resources.ApplyResources(this.AddressLabel, "AddressLabel");
            this.AddressLabel.Name = "AddressLabel";
            // 
            // AddressHexIndLabel
            // 
            resources.ApplyResources(this.AddressHexIndLabel, "AddressHexIndLabel");
            this.AddressHexIndLabel.Name = "AddressHexIndLabel";
            // 
            // AddressBox
            // 
            resources.ApplyResources(this.AddressBox, "AddressBox");
            this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.AddressBox.Name = "AddressBox";
            this.AddressBox.Nullable = true;
            // 
            // ValueHexIndLabel
            // 
            resources.ApplyResources(this.ValueHexIndLabel, "ValueHexIndLabel");
            this.ValueHexIndLabel.Name = "ValueHexIndLabel";
            // 
            // ValueLabel
            // 
            resources.ApplyResources(this.ValueLabel, "ValueLabel");
            this.ValueLabel.Name = "ValueLabel";
            // 
            // CompareHexIndLabel
            // 
            resources.ApplyResources(this.CompareHexIndLabel, "CompareHexIndLabel");
            this.CompareHexIndLabel.Name = "CompareHexIndLabel";
            // 
            // CompareLabel
            // 
            resources.ApplyResources(this.CompareLabel, "CompareLabel");
            this.CompareLabel.Name = "CompareLabel";
            // 
            // DomainLabel
            // 
            resources.ApplyResources(this.DomainLabel, "DomainLabel");
            this.DomainLabel.Name = "DomainLabel";
            // 
            // DomainDropDown
            // 
            resources.ApplyResources(this.DomainDropDown, "DomainDropDown");
            this.DomainDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DomainDropDown.FormattingEnabled = true;
            this.DomainDropDown.Name = "DomainDropDown";
            this.DomainDropDown.SelectedIndexChanged += new System.EventHandler(this.DomainDropDown_SelectedIndexChanged);
            // 
            // SizeLabel
            // 
            resources.ApplyResources(this.SizeLabel, "SizeLabel");
            this.SizeLabel.Name = "SizeLabel";
            // 
            // SizeDropDown
            // 
            resources.ApplyResources(this.SizeDropDown, "SizeDropDown");
            this.SizeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SizeDropDown.FormattingEnabled = true;
            this.SizeDropDown.Items.AddRange(new object[] {
            resources.GetString("SizeDropDown.Items"),
            resources.GetString("SizeDropDown.Items1"),
            resources.GetString("SizeDropDown.Items2")});
            this.SizeDropDown.Name = "SizeDropDown";
            this.SizeDropDown.SelectedIndexChanged += new System.EventHandler(this.SizeDropDown_SelectedIndexChanged);
            // 
            // DisplayTypeLabel
            // 
            resources.ApplyResources(this.DisplayTypeLabel, "DisplayTypeLabel");
            this.DisplayTypeLabel.Name = "DisplayTypeLabel";
            // 
            // DisplayTypeDropDown
            // 
            resources.ApplyResources(this.DisplayTypeDropDown, "DisplayTypeDropDown");
            this.DisplayTypeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.DisplayTypeDropDown.FormattingEnabled = true;
            this.DisplayTypeDropDown.Items.AddRange(new object[] {
            resources.GetString("DisplayTypeDropDown.Items"),
            resources.GetString("DisplayTypeDropDown.Items1"),
            resources.GetString("DisplayTypeDropDown.Items2")});
            this.DisplayTypeDropDown.Name = "DisplayTypeDropDown";
            this.DisplayTypeDropDown.SelectedIndexChanged += new System.EventHandler(this.DisplayTypeDropDown_SelectedIndexChanged);
            // 
            // BigEndianCheckBox
            // 
            resources.ApplyResources(this.BigEndianCheckBox, "BigEndianCheckBox");
            this.BigEndianCheckBox.Name = "BigEndianCheckBox";
            this.BigEndianCheckBox.UseVisualStyleBackColor = true;
            // 
            // AddButton
            // 
            resources.ApplyResources(this.AddButton, "AddButton");
            this.AddButton.Name = "AddButton";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // EditButton
            // 
            resources.ApplyResources(this.EditButton, "EditButton");
            this.EditButton.Name = "EditButton";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
            // 
            // CompareBox
            // 
            resources.ApplyResources(this.CompareBox, "CompareBox");
            this.CompareBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
            this.CompareBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.CompareBox.Name = "CompareBox";
            this.CompareBox.Nullable = true;
            this.CompareBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
            this.CompareBox.TextChanged += new System.EventHandler(this.CompareBox_TextChanged);
            // 
            // ValueBox
            // 
            resources.ApplyResources(this.ValueBox, "ValueBox");
            this.ValueBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
            this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ValueBox.Name = "ValueBox";
            this.ValueBox.Nullable = true;
            this.ValueBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
            // 
            // CompareTypeDropDown
            // 
            resources.ApplyResources(this.CompareTypeDropDown, "CompareTypeDropDown");
            this.CompareTypeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CompareTypeDropDown.FormattingEnabled = true;
            this.CompareTypeDropDown.Items.AddRange(new object[] {
            resources.GetString("CompareTypeDropDown.Items")});
            this.CompareTypeDropDown.Name = "CompareTypeDropDown";
            // 
            // CompareTypeLabel
            // 
            resources.ApplyResources(this.CompareTypeLabel, "CompareTypeLabel");
            this.CompareTypeLabel.Name = "CompareTypeLabel";
            // 
            // CheatEdit
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.CompareTypeDropDown);
            this.Controls.Add(this.CompareTypeLabel);
            this.Controls.Add(this.EditButton);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.BigEndianCheckBox);
            this.Controls.Add(this.DisplayTypeDropDown);
            this.Controls.Add(this.DisplayTypeLabel);
            this.Controls.Add(this.SizeDropDown);
            this.Controls.Add(this.SizeLabel);
            this.Controls.Add(this.DomainDropDown);
            this.Controls.Add(this.DomainLabel);
            this.Controls.Add(this.CompareBox);
            this.Controls.Add(this.CompareHexIndLabel);
            this.Controls.Add(this.CompareLabel);
            this.Controls.Add(this.ValueBox);
            this.Controls.Add(this.ValueHexIndLabel);
            this.Controls.Add(this.ValueLabel);
            this.Controls.Add(this.AddressBox);
            this.Controls.Add(this.AddressHexIndLabel);
            this.Controls.Add(this.AddressLabel);
            this.Controls.Add(this.NameBox);
            this.Controls.Add(this.NameLabel);
            this.Name = "CheatEdit";
            this.Load += new System.EventHandler(this.CheatEdit_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox NameBox;
		private BizHawk.WinForms.Controls.LocLabelEx NameLabel;
		private BizHawk.WinForms.Controls.LocLabelEx AddressLabel;
		private BizHawk.WinForms.Controls.LocLabelEx AddressHexIndLabel;
		private HexTextBox AddressBox;
		private WatchValueBox ValueBox;
		private BizHawk.WinForms.Controls.LocLabelEx ValueHexIndLabel;
		private BizHawk.WinForms.Controls.LocLabelEx ValueLabel;
		private WatchValueBox CompareBox;
		private BizHawk.WinForms.Controls.LocLabelEx CompareHexIndLabel;
		private BizHawk.WinForms.Controls.LocLabelEx CompareLabel;
		private BizHawk.WinForms.Controls.LocLabelEx DomainLabel;
		private System.Windows.Forms.ComboBox DomainDropDown;
		private BizHawk.WinForms.Controls.LocLabelEx SizeLabel;
		private System.Windows.Forms.ComboBox SizeDropDown;
		private BizHawk.WinForms.Controls.LocLabelEx DisplayTypeLabel;
		private System.Windows.Forms.ComboBox DisplayTypeDropDown;
		private System.Windows.Forms.CheckBox BigEndianCheckBox;
		private System.Windows.Forms.Button AddButton;
		private System.Windows.Forms.Button EditButton;
		private System.Windows.Forms.ComboBox CompareTypeDropDown;
		private BizHawk.WinForms.Controls.LocLabelEx CompareTypeLabel;
	}
}
