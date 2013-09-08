namespace BizHawk.MultiClient
{
    partial class WatchEditor
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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.AddressBox = new BizHawk.HexTextBox();
			this.NotesBox = new System.Windows.Forms.TextBox();
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.DomainDropDown = new System.Windows.Forms.ComboBox();
			this.label3 = new System.Windows.Forms.Label();
			this.SizeDropDown = new System.Windows.Forms.ComboBox();
			this.DisplayTypeLael = new System.Windows.Forms.Label();
			this.DisplayTypeDropDown = new System.Windows.Forms.ComboBox();
			this.BigEndianCheckBox = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(62, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Address: 0x";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(9, 35);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(38, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "Notes:";
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(69, 6);
			this.AddressBox.MaxLength = 8;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Size = new System.Drawing.Size(100, 20);
			this.AddressBox.TabIndex = 2;
			this.AddressBox.Text = "00000000";
			// 
			// NotesBox
			// 
			this.NotesBox.Location = new System.Drawing.Point(69, 32);
			this.NotesBox.MaxLength = 256;
			this.NotesBox.Name = "NotesBox";
			this.NotesBox.Size = new System.Drawing.Size(100, 20);
			this.NotesBox.TabIndex = 3;
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OK.Location = new System.Drawing.Point(12, 260);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 7;
			this.OK.Text = "Ok";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(123, 260);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 8;
			this.Cancel.Text = "Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(11, 214);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(83, 13);
			this.label6.TabIndex = 15;
			this.label6.Text = "Memory Domain";
			// 
			// DomainComboBox
			// 
			this.DomainDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DomainDropDown.FormattingEnabled = true;
			this.DomainDropDown.Location = new System.Drawing.Point(12, 230);
			this.DomainDropDown.Name = "DomainComboBox";
			this.DomainDropDown.Size = new System.Drawing.Size(141, 21);
			this.DomainDropDown.TabIndex = 14;
			this.DomainDropDown.SelectedIndexChanged += new System.EventHandler(this.DomainComboBox_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 64);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(27, 13);
			this.label3.TabIndex = 17;
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
			this.SizeDropDown.Location = new System.Drawing.Point(10, 80);
			this.SizeDropDown.Name = "SizeDropDown";
			this.SizeDropDown.Size = new System.Drawing.Size(141, 21);
			this.SizeDropDown.TabIndex = 16;
			this.SizeDropDown.SelectedIndexChanged += new System.EventHandler(this.SizeDropDown_SelectedIndexChanged);
			// 
			// DisplayTypeLael
			// 
			this.DisplayTypeLael.AutoSize = true;
			this.DisplayTypeLael.Location = new System.Drawing.Point(11, 106);
			this.DisplayTypeLael.Name = "DisplayTypeLael";
			this.DisplayTypeLael.Size = new System.Drawing.Size(68, 13);
			this.DisplayTypeLael.TabIndex = 19;
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
			this.DisplayTypeDropDown.Location = new System.Drawing.Point(12, 122);
			this.DisplayTypeDropDown.Name = "DisplayTypeDropDown";
			this.DisplayTypeDropDown.Size = new System.Drawing.Size(141, 21);
			this.DisplayTypeDropDown.TabIndex = 18;
			// 
			// BigEndianCheckBox
			// 
			this.BigEndianCheckBox.AutoSize = true;
			this.BigEndianCheckBox.Location = new System.Drawing.Point(14, 159);
			this.BigEndianCheckBox.Name = "BigEndianCheckBox";
			this.BigEndianCheckBox.Size = new System.Drawing.Size(77, 17);
			this.BigEndianCheckBox.TabIndex = 20;
			this.BigEndianCheckBox.Text = "Big Endian";
			this.BigEndianCheckBox.UseVisualStyleBackColor = true;
			// 
			// WatchEditor
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(213, 296);
			this.Controls.Add(this.BigEndianCheckBox);
			this.Controls.Add(this.DisplayTypeLael);
			this.Controls.Add(this.DisplayTypeDropDown);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.SizeDropDown);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.DomainDropDown);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.NotesBox);
			this.Controls.Add(this.AddressBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WatchEditor";
			this.Text = "New Watch";
			this.Load += new System.EventHandler(this.RamWatchNewWatch_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private HexTextBox AddressBox;
		private System.Windows.Forms.TextBox NotesBox;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ComboBox DomainDropDown;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox SizeDropDown;
		private System.Windows.Forms.Label DisplayTypeLael;
		private System.Windows.Forms.ComboBox DisplayTypeDropDown;
		private System.Windows.Forms.CheckBox BigEndianCheckBox;
    }
}