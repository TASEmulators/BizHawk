namespace BizHawk.Client.EmuHawk
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
			this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.NotesBox = new System.Windows.Forms.TextBox();
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.label6 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.DomainDropDown = new System.Windows.Forms.ComboBox();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.SizeDropDown = new System.Windows.Forms.ComboBox();
			this.DisplayTypeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.DisplayTypeDropDown = new System.Windows.Forms.ComboBox();
			this.BigEndianCheckBox = new System.Windows.Forms.CheckBox();
			this.BigEndianLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.AddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(6, 39);
			this.label1.Name = "label1";
			this.label1.Text = "Address:        0x";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(6, 66);
			this.label2.Name = "label2";
			this.label2.Text = "Notes:";
			// 
			// NotesBox
			// 
			this.NotesBox.Location = new System.Drawing.Point(90, 63);
			this.NotesBox.MaxLength = 256;
			this.NotesBox.Name = "NotesBox";
			this.NotesBox.Size = new System.Drawing.Size(123, 20);
			this.NotesBox.TabIndex = 2;
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OK.Location = new System.Drawing.Point(12, 170);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(75, 23);
			this.OK.TabIndex = 6;
			this.OK.Text = "OK";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(135, 170);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 7;
			this.Cancel.Text = "Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(6, 12);
			this.label6.Name = "label6";
			this.label6.Text = "Memory Domain";
			// 
			// DomainDropDown
			// 
			this.DomainDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DomainDropDown.FormattingEnabled = true;
			this.DomainDropDown.Location = new System.Drawing.Point(90, 9);
			this.DomainDropDown.Name = "DomainDropDown";
			this.DomainDropDown.Size = new System.Drawing.Size(123, 21);
			this.DomainDropDown.TabIndex = 0;
			this.DomainDropDown.SelectedIndexChanged += new System.EventHandler(this.DomainComboBox_SelectedIndexChanged);
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(6, 93);
			this.label3.Name = "label3";
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
			this.SizeDropDown.Location = new System.Drawing.Point(90, 90);
			this.SizeDropDown.Name = "SizeDropDown";
			this.SizeDropDown.Size = new System.Drawing.Size(123, 21);
			this.SizeDropDown.TabIndex = 3;
			this.SizeDropDown.SelectedIndexChanged += new System.EventHandler(this.SizeDropDown_SelectedIndexChanged);
			// 
			// DisplayTypeLabel
			// 
			this.DisplayTypeLabel.Location = new System.Drawing.Point(6, 120);
			this.DisplayTypeLabel.Name = "DisplayTypeLabel";
			this.DisplayTypeLabel.Text = "Display Type";
			// 
			// DisplayTypeDropDown
			// 
			this.DisplayTypeDropDown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.DisplayTypeDropDown.FormattingEnabled = true;
			this.DisplayTypeDropDown.Items.AddRange(new object[] {
            "1 Byte",
            "2 Byte",
            "4 Byte"});
			this.DisplayTypeDropDown.Location = new System.Drawing.Point(90, 117);
			this.DisplayTypeDropDown.Name = "DisplayTypeDropDown";
			this.DisplayTypeDropDown.Size = new System.Drawing.Size(123, 21);
			this.DisplayTypeDropDown.TabIndex = 4;
			this.DisplayTypeDropDown.SelectedIndexChanged += new System.EventHandler(this.DisplayTypeDropDown_SelectedIndexChanged);
			// 
			// BigEndianCheckBox
			// 
			this.BigEndianCheckBox.AutoSize = true;
			this.BigEndianCheckBox.Location = new System.Drawing.Point(91, 148);
			this.BigEndianCheckBox.Name = "BigEndianCheckBox";
			this.BigEndianCheckBox.Size = new System.Drawing.Size(15, 14);
			this.BigEndianCheckBox.TabIndex = 5;
			this.BigEndianCheckBox.UseVisualStyleBackColor = true;
			// 
			// BigEndianLabel
			// 
			this.BigEndianLabel.Location = new System.Drawing.Point(6, 147);
			this.BigEndianLabel.Name = "BigEndianLabel";
			this.BigEndianLabel.Text = "Big Endian";
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(90, 36);
			this.AddressBox.MaxLength = 8;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Nullable = false;
			this.AddressBox.Size = new System.Drawing.Size(123, 20);
			this.AddressBox.TabIndex = 1;
			this.AddressBox.Text = "00000000";
			// 
			// WatchEditor
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(225, 206);
			this.Controls.Add(this.BigEndianLabel);
			this.Controls.Add(this.BigEndianCheckBox);
			this.Controls.Add(this.DisplayTypeLabel);
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
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "New Watch";
			this.Load += new System.EventHandler(this.RamWatchNewWatch_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private BizHawk.WinForms.Controls.LocLabelEx label1;
        private BizHawk.WinForms.Controls.LocLabelEx label2;
        private HexTextBox AddressBox;
		private System.Windows.Forms.TextBox NotesBox;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
		private BizHawk.WinForms.Controls.LocLabelEx label6;
		private System.Windows.Forms.ComboBox DomainDropDown;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private System.Windows.Forms.ComboBox SizeDropDown;
		private BizHawk.WinForms.Controls.LocLabelEx DisplayTypeLabel;
		private System.Windows.Forms.ComboBox DisplayTypeDropDown;
		private System.Windows.Forms.CheckBox BigEndianCheckBox;
		private WinForms.Controls.LocLabelEx BigEndianLabel;
	}
}