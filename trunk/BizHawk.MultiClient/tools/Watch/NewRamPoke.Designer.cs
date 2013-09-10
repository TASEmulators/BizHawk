namespace BizHawk.MultiClient
{
    partial class NewRamPoke
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewRamPoke));
			this.label1 = new System.Windows.Forms.Label();
			this.AddressBox = new BizHawk.HexTextBox();
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.OutputLabel = new System.Windows.Forms.Label();
			this.ValeLabel = new System.Windows.Forms.Label();
			this.ValueBox = new System.Windows.Forms.TextBox();
			this.ValueHexLabel = new System.Windows.Forms.Label();
			this.DomainDropDown = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.BigEndianCheckBox = new System.Windows.Forms.CheckBox();
			this.DisplayTypeLael = new System.Windows.Forms.Label();
			this.DisplayTypeDropDown = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.SizeDropDown = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(11, 33);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(62, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Address: 0x";
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(73, 30);
			this.AddressBox.MaxLength = 8;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Size = new System.Drawing.Size(80, 20);
			this.AddressBox.TabIndex = 5;
			this.AddressBox.Text = "0000";
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OK.Location = new System.Drawing.Point(12, 293);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 35;
			this.OK.Text = "&Poke";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(123, 293);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 40;
			this.Cancel.Text = "&Close";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OutputLabel
			// 
			this.OutputLabel.AutoSize = true;
			this.OutputLabel.Location = new System.Drawing.Point(12, 7);
			this.OutputLabel.Name = "OutputLabel";
			this.OutputLabel.Size = new System.Drawing.Size(129, 13);
			this.OutputLabel.TabIndex = 9;
			this.OutputLabel.Text = "Enter an address to poke:";
			// 
			// ValeLabel
			// 
			this.ValeLabel.AutoSize = true;
			this.ValeLabel.Location = new System.Drawing.Point(11, 59);
			this.ValeLabel.Name = "ValeLabel";
			this.ValeLabel.Size = new System.Drawing.Size(37, 13);
			this.ValeLabel.TabIndex = 10;
			this.ValeLabel.Text = "Value:";
			// 
			// ValueBox
			// 
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(73, 57);
			this.ValueBox.MaxLength = 9;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.Size = new System.Drawing.Size(80, 20);
			this.ValueBox.TabIndex = 10;
			this.ValueBox.Text = "0000";
			// 
			// ValueHexLabel
			// 
			this.ValueHexLabel.AutoSize = true;
			this.ValueHexLabel.Location = new System.Drawing.Point(55, 60);
			this.ValueHexLabel.Name = "ValueHexLabel";
			this.ValueHexLabel.Size = new System.Drawing.Size(18, 13);
			this.ValueHexLabel.TabIndex = 11;
			this.ValueHexLabel.Text = "0x";
			// 
			// DomainDropDown
			// 
			this.DomainDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DomainDropDown.FormattingEnabled = true;
			this.DomainDropDown.Location = new System.Drawing.Point(12, 261);
			this.DomainDropDown.Name = "DomainDropDown";
			this.DomainDropDown.Size = new System.Drawing.Size(141, 21);
			this.DomainDropDown.TabIndex = 30;
			this.DomainDropDown.SelectedIndexChanged += new System.EventHandler(this.DomainComboBox_SelectedIndexChanged);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(11, 245);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(83, 13);
			this.label6.TabIndex = 13;
			this.label6.Text = "Memory Domain";
			// 
			// BigEndianCheckBox
			// 
			this.BigEndianCheckBox.AutoSize = true;
			this.BigEndianCheckBox.Location = new System.Drawing.Point(17, 184);
			this.BigEndianCheckBox.Name = "BigEndianCheckBox";
			this.BigEndianCheckBox.Size = new System.Drawing.Size(77, 17);
			this.BigEndianCheckBox.TabIndex = 25;
			this.BigEndianCheckBox.Text = "Big Endian";
			this.BigEndianCheckBox.UseVisualStyleBackColor = true;
			// 
			// DisplayTypeLael
			// 
			this.DisplayTypeLael.AutoSize = true;
			this.DisplayTypeLael.Location = new System.Drawing.Point(14, 131);
			this.DisplayTypeLael.Name = "DisplayTypeLael";
			this.DisplayTypeLael.Size = new System.Drawing.Size(68, 13);
			this.DisplayTypeLael.TabIndex = 24;
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
			this.DisplayTypeDropDown.Location = new System.Drawing.Point(15, 147);
			this.DisplayTypeDropDown.Name = "DisplayTypeDropDown";
			this.DisplayTypeDropDown.Size = new System.Drawing.Size(141, 21);
			this.DisplayTypeDropDown.TabIndex = 20;
			this.DisplayTypeDropDown.SelectedIndexChanged += new System.EventHandler(this.DisplayTypeDropDown_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 89);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(27, 13);
			this.label3.TabIndex = 23;
			this.label3.Text = "Size";
			// 
			// SizeDropDown
			// 
			this.SizeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.SizeDropDown.FormattingEnabled = true;
			this.SizeDropDown.Items.AddRange(new object[] {
            "1 Byte",
            "2 Byte",
            "4 Byte"});
			this.SizeDropDown.Location = new System.Drawing.Point(13, 105);
			this.SizeDropDown.Name = "SizeDropDown";
			this.SizeDropDown.Size = new System.Drawing.Size(141, 21);
			this.SizeDropDown.TabIndex = 15;
			this.SizeDropDown.SelectedIndexChanged += new System.EventHandler(this.SizeDropDown_SelectedIndexChanged);
			// 
			// NewRamPoke
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(213, 332);
			this.Controls.Add(this.BigEndianCheckBox);
			this.Controls.Add(this.DisplayTypeLael);
			this.Controls.Add(this.DisplayTypeDropDown);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.SizeDropDown);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.DomainDropDown);
			this.Controls.Add(this.ValueHexLabel);
			this.Controls.Add(this.ValueBox);
			this.Controls.Add(this.ValeLabel);
			this.Controls.Add(this.OutputLabel);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.AddressBox);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NewRamPoke";
			this.Text = "Ram Poke";
			this.Load += new System.EventHandler(this.RamPoke_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private HexTextBox AddressBox;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Label OutputLabel;
        private System.Windows.Forms.Label ValeLabel;
        private System.Windows.Forms.TextBox ValueBox;
		private System.Windows.Forms.Label ValueHexLabel;
		private System.Windows.Forms.ComboBox DomainDropDown;
		private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox BigEndianCheckBox;
        private System.Windows.Forms.Label DisplayTypeLael;
        private System.Windows.Forms.ComboBox DisplayTypeDropDown;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox SizeDropDown;
    }
}