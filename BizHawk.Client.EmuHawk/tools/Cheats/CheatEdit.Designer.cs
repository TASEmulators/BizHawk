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
			this.NameBox = new System.Windows.Forms.TextBox();
			this.NameLabel = new System.Windows.Forms.Label();
			this.AddressLabel = new System.Windows.Forms.Label();
			this.AddressHexIndLabel = new System.Windows.Forms.Label();
			this.AddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
			this.ValueHexIndLabel = new System.Windows.Forms.Label();
			this.ValueLabel = new System.Windows.Forms.Label();
			this.CompareHexIndLabel = new System.Windows.Forms.Label();
			this.CompareLabel = new System.Windows.Forms.Label();
			this.DomainLabel = new System.Windows.Forms.Label();
			this.DomainDropDown = new System.Windows.Forms.ComboBox();
			this.SizeLabel = new System.Windows.Forms.Label();
			this.SizeDropDown = new System.Windows.Forms.ComboBox();
			this.DisplayTypeLael = new System.Windows.Forms.Label();
			this.DisplayTypeDropDown = new System.Windows.Forms.ComboBox();
			this.BigEndianCheckBox = new System.Windows.Forms.CheckBox();
			this.AddButton = new System.Windows.Forms.Button();
			this.EditButton = new System.Windows.Forms.Button();
			this.CompareBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.ValueBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.CompareTypeDropDown = new System.Windows.Forms.ComboBox();
			this.CompareTypeLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// NameBox
			// 
			this.NameBox.Location = new System.Drawing.Point(70, 12);
			this.NameBox.Name = "NameBox";
			this.NameBox.Size = new System.Drawing.Size(108, 31);
			this.NameBox.TabIndex = 5;
			// 
			// NameLabel
			// 
			this.NameLabel.AutoSize = true;
			this.NameLabel.Location = new System.Drawing.Point(32, 16);
			this.NameLabel.Name = "NameLabel";
			this.NameLabel.Size = new System.Drawing.Size(68, 25);
			this.NameLabel.TabIndex = 4;
			this.NameLabel.Text = "Name";
			// 
			// AddressLabel
			// 
			this.AddressLabel.AutoSize = true;
			this.AddressLabel.Location = new System.Drawing.Point(22, 43);
			this.AddressLabel.Name = "AddressLabel";
			this.AddressLabel.Size = new System.Drawing.Size(91, 25);
			this.AddressLabel.TabIndex = 6;
			this.AddressLabel.Text = "Address";
			// 
			// AddressHexIndLabel
			// 
			this.AddressHexIndLabel.AutoSize = true;
			this.AddressHexIndLabel.Location = new System.Drawing.Point(92, 43);
			this.AddressHexIndLabel.Name = "AddressHexIndLabel";
			this.AddressHexIndLabel.Size = new System.Drawing.Size(35, 25);
			this.AddressHexIndLabel.TabIndex = 8;
			this.AddressHexIndLabel.Text = "0x";
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(113, 39);
			this.AddressBox.MaxLength = 8;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Nullable = true;
			this.AddressBox.Size = new System.Drawing.Size(65, 31);
			this.AddressBox.TabIndex = 9;
			// 
			// ValueHexIndLabel
			// 
			this.ValueHexIndLabel.AutoSize = true;
			this.ValueHexIndLabel.Location = new System.Drawing.Point(92, 69);
			this.ValueHexIndLabel.Name = "ValueHexIndLabel";
			this.ValueHexIndLabel.Size = new System.Drawing.Size(35, 25);
			this.ValueHexIndLabel.TabIndex = 11;
			this.ValueHexIndLabel.Text = "0x";
			// 
			// ValueLabel
			// 
			this.ValueLabel.AutoSize = true;
			this.ValueLabel.Location = new System.Drawing.Point(33, 69);
			this.ValueLabel.Name = "ValueLabel";
			this.ValueLabel.Size = new System.Drawing.Size(67, 25);
			this.ValueLabel.TabIndex = 10;
			this.ValueLabel.Text = "Value";
			// 
			// CompareHexIndLabel
			// 
			this.CompareHexIndLabel.AutoSize = true;
			this.CompareHexIndLabel.Location = new System.Drawing.Point(92, 95);
			this.CompareHexIndLabel.Name = "CompareHexIndLabel";
			this.CompareHexIndLabel.Size = new System.Drawing.Size(35, 25);
			this.CompareHexIndLabel.TabIndex = 14;
			this.CompareHexIndLabel.Text = "0x";
			// 
			// CompareLabel
			// 
			this.CompareLabel.AutoSize = true;
			this.CompareLabel.Location = new System.Drawing.Point(24, 95);
			this.CompareLabel.Name = "CompareLabel";
			this.CompareLabel.Size = new System.Drawing.Size(99, 25);
			this.CompareLabel.TabIndex = 13;
			this.CompareLabel.Text = "Compare";
			// 
			// DomainLabel
			// 
			this.DomainLabel.AutoSize = true;
			this.DomainLabel.Location = new System.Drawing.Point(24, 158);
			this.DomainLabel.Name = "DomainLabel";
			this.DomainLabel.Size = new System.Drawing.Size(85, 25);
			this.DomainLabel.TabIndex = 16;
			this.DomainLabel.Text = "Domain";
			// 
			// DomainDropDown
			// 
			this.DomainDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DomainDropDown.FormattingEnabled = true;
			this.DomainDropDown.Location = new System.Drawing.Point(78, 154);
			this.DomainDropDown.Name = "DomainDropDown";
			this.DomainDropDown.Size = new System.Drawing.Size(100, 33);
			this.DomainDropDown.TabIndex = 17;
			this.DomainDropDown.SelectedIndexChanged += new System.EventHandler(this.DomainDropDown_SelectedIndexChanged);
			// 
			// SizeLabel
			// 
			this.SizeLabel.AutoSize = true;
			this.SizeLabel.Location = new System.Drawing.Point(40, 190);
			this.SizeLabel.Name = "SizeLabel";
			this.SizeLabel.Size = new System.Drawing.Size(54, 25);
			this.SizeLabel.TabIndex = 18;
			this.SizeLabel.Text = "Size";
			// 
			// SizeDropDown
			// 
			this.SizeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.SizeDropDown.FormattingEnabled = true;
			this.SizeDropDown.Items.AddRange(new object[] {
            "1 Byte",
            "2 Byte",
            "4 Byte"});
			this.SizeDropDown.Location = new System.Drawing.Point(78, 184);
			this.SizeDropDown.Name = "SizeDropDown";
			this.SizeDropDown.Size = new System.Drawing.Size(100, 33);
			this.SizeDropDown.TabIndex = 19;
			this.SizeDropDown.SelectedIndexChanged += new System.EventHandler(this.SizeDropDown_SelectedIndexChanged);
			// 
			// DisplayTypeLael
			// 
			this.DisplayTypeLael.AutoSize = true;
			this.DisplayTypeLael.Location = new System.Drawing.Point(11, 219);
			this.DisplayTypeLael.Name = "DisplayTypeLael";
			this.DisplayTypeLael.Size = new System.Drawing.Size(114, 25);
			this.DisplayTypeLael.TabIndex = 20;
			this.DisplayTypeLael.Text = "Display As";
			// 
			// DisplayTypeDropDown
			// 
			this.DisplayTypeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DisplayTypeDropDown.FormattingEnabled = true;
			this.DisplayTypeDropDown.Items.AddRange(new object[] {
            "1 Byte",
            "2 Byte",
            "4 Byte"});
			this.DisplayTypeDropDown.Location = new System.Drawing.Point(78, 213);
			this.DisplayTypeDropDown.Name = "DisplayTypeDropDown";
			this.DisplayTypeDropDown.Size = new System.Drawing.Size(100, 33);
			this.DisplayTypeDropDown.TabIndex = 21;
			this.DisplayTypeDropDown.SelectedIndexChanged += new System.EventHandler(this.DisplayTypeDropDown_SelectedIndexChanged);
			// 
			// BigEndianCheckBox
			// 
			this.BigEndianCheckBox.AutoSize = true;
			this.BigEndianCheckBox.Location = new System.Drawing.Point(101, 242);
			this.BigEndianCheckBox.Name = "BigEndianCheckBox";
			this.BigEndianCheckBox.Size = new System.Drawing.Size(148, 29);
			this.BigEndianCheckBox.TabIndex = 22;
			this.BigEndianCheckBox.Text = "Big Endian";
			this.BigEndianCheckBox.UseVisualStyleBackColor = true;
			// 
			// AddButton
			// 
			this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddButton.Enabled = false;
			this.AddButton.Location = new System.Drawing.Point(7, 265);
			this.AddButton.Name = "AddButton";
			this.AddButton.Size = new System.Drawing.Size(65, 23);
			this.AddButton.TabIndex = 23;
			this.AddButton.Text = "&Add";
			this.AddButton.UseVisualStyleBackColor = true;
			this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
			// 
			// EditButton
			// 
			this.EditButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.EditButton.Enabled = false;
			this.EditButton.Location = new System.Drawing.Point(113, 265);
			this.EditButton.Name = "EditButton";
			this.EditButton.Size = new System.Drawing.Size(65, 23);
			this.EditButton.TabIndex = 24;
			this.EditButton.Text = "&Edit";
			this.EditButton.UseVisualStyleBackColor = true;
			this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
			// 
			// CompareBox
			// 
			this.CompareBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
			this.CompareBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.CompareBox.Location = new System.Drawing.Point(113, 91);
			this.CompareBox.MaxLength = 2;
			this.CompareBox.Name = "CompareBox";
			this.CompareBox.Nullable = true;
			this.CompareBox.Size = new System.Drawing.Size(65, 31);
			this.CompareBox.TabIndex = 15;
			this.CompareBox.Type = BizHawk.Client.Common.DisplayType.Hex;
			this.CompareBox.TextChanged += new System.EventHandler(this.CompareBox_TextChanged);
			// 
			// ValueBox
			// 
			this.ValueBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(113, 65);
			this.ValueBox.MaxLength = 2;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.Nullable = true;
			this.ValueBox.Size = new System.Drawing.Size(65, 31);
			this.ValueBox.TabIndex = 12;
			this.ValueBox.Text = "00";
			this.ValueBox.Type = BizHawk.Client.Common.DisplayType.Hex;
			// 
			// CompareTypeDropDown
			// 
			this.CompareTypeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.CompareTypeDropDown.FormattingEnabled = true;
			this.CompareTypeDropDown.Location = new System.Drawing.Point(113, 122);
			this.CompareTypeDropDown.Name = "CompareTypeDropDown";
			this.CompareTypeDropDown.Size = new System.Drawing.Size(65, 33);
			this.CompareTypeDropDown.TabIndex = 26;
			this.CompareTypeDropDown.Items.AddRange(new object[] {
				""
			});
			this.CompareTypeDropDown.SelectedIndex = 0;
			// 
			// CompareTypeLabel
			// 
			this.CompareTypeLabel.AutoSize = true;
			this.CompareTypeLabel.Location = new System.Drawing.Point(24, 125);
			this.CompareTypeLabel.Name = "CompareTypeLabel";
			this.CompareTypeLabel.Size = new System.Drawing.Size(153, 25);
			this.CompareTypeLabel.TabIndex = 25;
			this.CompareTypeLabel.Text = "Compare Type";
			this.CompareTypeLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// CheatEdit
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.CompareTypeDropDown);
			this.Controls.Add(this.CompareTypeLabel);
			this.Controls.Add(this.EditButton);
			this.Controls.Add(this.AddButton);
			this.Controls.Add(this.BigEndianCheckBox);
			this.Controls.Add(this.DisplayTypeDropDown);
			this.Controls.Add(this.DisplayTypeLael);
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
			this.Size = new System.Drawing.Size(191, 298);
			this.Load += new System.EventHandler(this.CheatEdit_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox NameBox;
		private System.Windows.Forms.Label NameLabel;
		private System.Windows.Forms.Label AddressLabel;
		private System.Windows.Forms.Label AddressHexIndLabel;
		private HexTextBox AddressBox;
		private WatchValueBox ValueBox;
		private System.Windows.Forms.Label ValueHexIndLabel;
		private System.Windows.Forms.Label ValueLabel;
		private WatchValueBox CompareBox;
		private System.Windows.Forms.Label CompareHexIndLabel;
		private System.Windows.Forms.Label CompareLabel;
		private System.Windows.Forms.Label DomainLabel;
		private System.Windows.Forms.ComboBox DomainDropDown;
		private System.Windows.Forms.Label SizeLabel;
		private System.Windows.Forms.ComboBox SizeDropDown;
		private System.Windows.Forms.Label DisplayTypeLael;
		private System.Windows.Forms.ComboBox DisplayTypeDropDown;
		private System.Windows.Forms.CheckBox BigEndianCheckBox;
		private System.Windows.Forms.Button AddButton;
		private System.Windows.Forms.Button EditButton;
		private System.Windows.Forms.ComboBox CompareTypeDropDown;
		private System.Windows.Forms.Label CompareTypeLabel;
	}
}
