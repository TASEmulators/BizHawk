namespace BizHawk.MultiClient
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
			this.AddressBox = new BizHawk.HexTextBox();
			this.ValueBox = new BizHawk.MultiClient.WatchValueBox();
			this.ValueHexIndLabel = new System.Windows.Forms.Label();
			this.ValueLabel = new System.Windows.Forms.Label();
			this.CompareBox = new BizHawk.MultiClient.WatchValueBox();
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
			this.SuspendLayout();
			// 
			// NameBox
			// 
			this.NameBox.Location = new System.Drawing.Point(70, 15);
			this.NameBox.Name = "NameBox";
			this.NameBox.Size = new System.Drawing.Size(108, 20);
			this.NameBox.TabIndex = 5;
			// 
			// NameLabel
			// 
			this.NameLabel.AutoSize = true;
			this.NameLabel.Location = new System.Drawing.Point(12, 18);
			this.NameLabel.Name = "NameLabel";
			this.NameLabel.Size = new System.Drawing.Size(35, 13);
			this.NameLabel.TabIndex = 4;
			this.NameLabel.Text = "Name";
			// 
			// AddressLabel
			// 
			this.AddressLabel.AutoSize = true;
			this.AddressLabel.Location = new System.Drawing.Point(38, 45);
			this.AddressLabel.Name = "AddressLabel";
			this.AddressLabel.Size = new System.Drawing.Size(45, 13);
			this.AddressLabel.TabIndex = 6;
			this.AddressLabel.Text = "Address";
			// 
			// AddressHexIndLabel
			// 
			this.AddressHexIndLabel.AutoSize = true;
			this.AddressHexIndLabel.Location = new System.Drawing.Point(89, 45);
			this.AddressHexIndLabel.Name = "AddressHexIndLabel";
			this.AddressHexIndLabel.Size = new System.Drawing.Size(18, 13);
			this.AddressHexIndLabel.TabIndex = 8;
			this.AddressHexIndLabel.Text = "0x";
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(113, 42);
			this.AddressBox.MaxLength = 8;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Size = new System.Drawing.Size(65, 20);
			this.AddressBox.TabIndex = 9;
			// 
			// ValueBox
			// 
			this.ValueBox.ByteSize = BizHawk.MultiClient.Watch.WatchSize.Byte;
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(113, 68);
			this.ValueBox.MaxLength = 2;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.Size = new System.Drawing.Size(65, 20);
			this.ValueBox.TabIndex = 12;
			this.ValueBox.Type = BizHawk.MultiClient.Watch.DisplayType.Hex;
			// 
			// ValueHexIndLabel
			// 
			this.ValueHexIndLabel.AutoSize = true;
			this.ValueHexIndLabel.Location = new System.Drawing.Point(89, 71);
			this.ValueHexIndLabel.Name = "ValueHexIndLabel";
			this.ValueHexIndLabel.Size = new System.Drawing.Size(18, 13);
			this.ValueHexIndLabel.TabIndex = 11;
			this.ValueHexIndLabel.Text = "0x";
			// 
			// ValueLabel
			// 
			this.ValueLabel.AutoSize = true;
			this.ValueLabel.Location = new System.Drawing.Point(38, 71);
			this.ValueLabel.Name = "ValueLabel";
			this.ValueLabel.Size = new System.Drawing.Size(34, 13);
			this.ValueLabel.TabIndex = 10;
			this.ValueLabel.Text = "Value";
			// 
			// CompareBox
			// 
			this.CompareBox.ByteSize = BizHawk.MultiClient.Watch.WatchSize.Byte;
			this.CompareBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.CompareBox.Location = new System.Drawing.Point(113, 94);
			this.CompareBox.MaxLength = 2;
			this.CompareBox.Name = "CompareBox";
			this.CompareBox.Size = new System.Drawing.Size(65, 20);
			this.CompareBox.TabIndex = 15;
			this.CompareBox.Type = BizHawk.MultiClient.Watch.DisplayType.Hex;
			// 
			// CompareHexIndLabel
			// 
			this.CompareHexIndLabel.AutoSize = true;
			this.CompareHexIndLabel.Location = new System.Drawing.Point(89, 97);
			this.CompareHexIndLabel.Name = "CompareHexIndLabel";
			this.CompareHexIndLabel.Size = new System.Drawing.Size(18, 13);
			this.CompareHexIndLabel.TabIndex = 14;
			this.CompareHexIndLabel.Text = "0x";
			// 
			// CompareLabel
			// 
			this.CompareLabel.AutoSize = true;
			this.CompareLabel.Location = new System.Drawing.Point(38, 97);
			this.CompareLabel.Name = "CompareLabel";
			this.CompareLabel.Size = new System.Drawing.Size(49, 13);
			this.CompareLabel.TabIndex = 13;
			this.CompareLabel.Text = "Compare";
			// 
			// DomainLabel
			// 
			this.DomainLabel.AutoSize = true;
			this.DomainLabel.Location = new System.Drawing.Point(12, 124);
			this.DomainLabel.Name = "DomainLabel";
			this.DomainLabel.Size = new System.Drawing.Size(43, 13);
			this.DomainLabel.TabIndex = 16;
			this.DomainLabel.Text = "Domain";
			// 
			// DomainDropDown
			// 
			this.DomainDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DomainDropDown.FormattingEnabled = true;
			this.DomainDropDown.Location = new System.Drawing.Point(78, 121);
			this.DomainDropDown.Name = "DomainDropDown";
			this.DomainDropDown.Size = new System.Drawing.Size(100, 21);
			this.DomainDropDown.TabIndex = 17;
			this.DomainDropDown.SelectedIndexChanged += new System.EventHandler(this.DomainDropDown_SelectedIndexChanged);
			// 
			// SizeLabel
			// 
			this.SizeLabel.AutoSize = true;
			this.SizeLabel.Location = new System.Drawing.Point(20, 150);
			this.SizeLabel.Name = "SizeLabel";
			this.SizeLabel.Size = new System.Drawing.Size(27, 13);
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
			this.SizeDropDown.Location = new System.Drawing.Point(78, 147);
			this.SizeDropDown.Name = "SizeDropDown";
			this.SizeDropDown.Size = new System.Drawing.Size(100, 21);
			this.SizeDropDown.TabIndex = 19;
			this.SizeDropDown.SelectedIndexChanged += new System.EventHandler(this.SizeDropDown_SelectedIndexChanged);
			// 
			// DisplayTypeLael
			// 
			this.DisplayTypeLael.AutoSize = true;
			this.DisplayTypeLael.Location = new System.Drawing.Point(4, 174);
			this.DisplayTypeLael.Name = "DisplayTypeLael";
			this.DisplayTypeLael.Size = new System.Drawing.Size(68, 13);
			this.DisplayTypeLael.TabIndex = 20;
			this.DisplayTypeLael.Text = "Display Type";
			// 
			// DisplayTypeDropDown
			// 
			this.DisplayTypeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DisplayTypeDropDown.FormattingEnabled = true;
			this.DisplayTypeDropDown.Items.AddRange(new object[] {
            "1 Byte",
            "2 Byte",
            "4 Byte"});
			this.DisplayTypeDropDown.Location = new System.Drawing.Point(78, 171);
			this.DisplayTypeDropDown.Name = "DisplayTypeDropDown";
			this.DisplayTypeDropDown.Size = new System.Drawing.Size(100, 21);
			this.DisplayTypeDropDown.TabIndex = 21;
			this.DisplayTypeDropDown.SelectedIndexChanged += new System.EventHandler(this.DisplayTypeDropDown_SelectedIndexChanged);
			// 
			// BigEndianCheckBox
			// 
			this.BigEndianCheckBox.AutoSize = true;
			this.BigEndianCheckBox.Location = new System.Drawing.Point(101, 198);
			this.BigEndianCheckBox.Name = "BigEndianCheckBox";
			this.BigEndianCheckBox.Size = new System.Drawing.Size(77, 17);
			this.BigEndianCheckBox.TabIndex = 22;
			this.BigEndianCheckBox.Text = "Big Endian";
			this.BigEndianCheckBox.UseVisualStyleBackColor = true;
			// 
			// AddButton
			// 
			this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.AddButton.Enabled = false;
			this.AddButton.Location = new System.Drawing.Point(7, 226);
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
			this.EditButton.Location = new System.Drawing.Point(113, 226);
			this.EditButton.Name = "EditButton";
			this.EditButton.Size = new System.Drawing.Size(65, 23);
			this.EditButton.TabIndex = 24;
			this.EditButton.Text = "&Edit";
			this.EditButton.UseVisualStyleBackColor = true;
			this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
			// 
			// CheatEdit
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
			this.Size = new System.Drawing.Size(200, 266);
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
	}
}
