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
			this.DataTypeGroupBox = new System.Windows.Forms.GroupBox();
			this.HexRadio = new System.Windows.Forms.RadioButton();
			this.UnsignedRadio = new System.Windows.Forms.RadioButton();
			this.SignedRadio = new System.Windows.Forms.RadioButton();
			this.DataSizeBox = new System.Windows.Forms.GroupBox();
			this.Byte4Radio = new System.Windows.Forms.RadioButton();
			this.Byte2Radio = new System.Windows.Forms.RadioButton();
			this.Byte1Radio = new System.Windows.Forms.RadioButton();
			this.EndianBox = new System.Windows.Forms.GroupBox();
			this.LittleEndianRadio = new System.Windows.Forms.RadioButton();
			this.BigEndianRadio = new System.Windows.Forms.RadioButton();
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.label6 = new System.Windows.Forms.Label();
			this.DomainComboBox = new System.Windows.Forms.ComboBox();
			this.DataTypeGroupBox.SuspendLayout();
			this.DataSizeBox.SuspendLayout();
			this.EndianBox.SuspendLayout();
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
			this.AddressBox.Leave += new System.EventHandler(this.AddressBox_Leave);
			// 
			// NotesBox
			// 
			this.NotesBox.Location = new System.Drawing.Point(69, 32);
			this.NotesBox.MaxLength = 256;
			this.NotesBox.Name = "NotesBox";
			this.NotesBox.Size = new System.Drawing.Size(100, 20);
			this.NotesBox.TabIndex = 3;
			// 
			// DataTypeGroupBox
			// 
			this.DataTypeGroupBox.Controls.Add(this.HexRadio);
			this.DataTypeGroupBox.Controls.Add(this.UnsignedRadio);
			this.DataTypeGroupBox.Controls.Add(this.SignedRadio);
			this.DataTypeGroupBox.Location = new System.Drawing.Point(12, 67);
			this.DataTypeGroupBox.Name = "DataTypeGroupBox";
			this.DataTypeGroupBox.Size = new System.Drawing.Size(95, 79);
			this.DataTypeGroupBox.TabIndex = 4;
			this.DataTypeGroupBox.TabStop = false;
			this.DataTypeGroupBox.Text = "Data Type";
			// 
			// HexRadio
			// 
			this.HexRadio.AutoSize = true;
			this.HexRadio.Location = new System.Drawing.Point(4, 51);
			this.HexRadio.Name = "HexRadio";
			this.HexRadio.Size = new System.Drawing.Size(86, 17);
			this.HexRadio.TabIndex = 2;
			this.HexRadio.Text = "Hexadecimal";
			this.HexRadio.UseVisualStyleBackColor = true;
			// 
			// UnsignedRadio
			// 
			this.UnsignedRadio.AutoSize = true;
			this.UnsignedRadio.Checked = true;
			this.UnsignedRadio.Location = new System.Drawing.Point(4, 34);
			this.UnsignedRadio.Name = "UnsignedRadio";
			this.UnsignedRadio.Size = new System.Drawing.Size(70, 17);
			this.UnsignedRadio.TabIndex = 1;
			this.UnsignedRadio.TabStop = true;
			this.UnsignedRadio.Text = "Unsigned";
			this.UnsignedRadio.UseVisualStyleBackColor = true;
			// 
			// SignedRadio
			// 
			this.SignedRadio.AutoSize = true;
			this.SignedRadio.Location = new System.Drawing.Point(4, 17);
			this.SignedRadio.Name = "SignedRadio";
			this.SignedRadio.Size = new System.Drawing.Size(58, 17);
			this.SignedRadio.TabIndex = 0;
			this.SignedRadio.Text = "Signed";
			this.SignedRadio.UseVisualStyleBackColor = true;
			// 
			// DataSizeBox
			// 
			this.DataSizeBox.Controls.Add(this.Byte4Radio);
			this.DataSizeBox.Controls.Add(this.Byte2Radio);
			this.DataSizeBox.Controls.Add(this.Byte1Radio);
			this.DataSizeBox.Location = new System.Drawing.Point(115, 67);
			this.DataSizeBox.Name = "DataSizeBox";
			this.DataSizeBox.Size = new System.Drawing.Size(83, 79);
			this.DataSizeBox.TabIndex = 5;
			this.DataSizeBox.TabStop = false;
			this.DataSizeBox.Text = "Data Size:";
			// 
			// Byte4Radio
			// 
			this.Byte4Radio.AutoSize = true;
			this.Byte4Radio.Location = new System.Drawing.Point(5, 51);
			this.Byte4Radio.Name = "Byte4Radio";
			this.Byte4Radio.Size = new System.Drawing.Size(60, 17);
			this.Byte4Radio.TabIndex = 2;
			this.Byte4Radio.Text = "4 Bytes";
			this.Byte4Radio.UseVisualStyleBackColor = true;
			// 
			// Byte2Radio
			// 
			this.Byte2Radio.AutoSize = true;
			this.Byte2Radio.Location = new System.Drawing.Point(5, 34);
			this.Byte2Radio.Name = "Byte2Radio";
			this.Byte2Radio.Size = new System.Drawing.Size(60, 17);
			this.Byte2Radio.TabIndex = 1;
			this.Byte2Radio.Text = "2 Bytes";
			this.Byte2Radio.UseVisualStyleBackColor = true;
			// 
			// Byte1Radio
			// 
			this.Byte1Radio.AutoSize = true;
			this.Byte1Radio.Checked = true;
			this.Byte1Radio.Location = new System.Drawing.Point(5, 17);
			this.Byte1Radio.Name = "Byte1Radio";
			this.Byte1Radio.Size = new System.Drawing.Size(55, 17);
			this.Byte1Radio.TabIndex = 0;
			this.Byte1Radio.TabStop = true;
			this.Byte1Radio.Text = "1 Byte";
			this.Byte1Radio.UseVisualStyleBackColor = true;
			// 
			// EndianBox
			// 
			this.EndianBox.Controls.Add(this.LittleEndianRadio);
			this.EndianBox.Controls.Add(this.BigEndianRadio);
			this.EndianBox.Location = new System.Drawing.Point(12, 152);
			this.EndianBox.Name = "EndianBox";
			this.EndianBox.Size = new System.Drawing.Size(117, 55);
			this.EndianBox.TabIndex = 6;
			this.EndianBox.TabStop = false;
			this.EndianBox.Text = "Endian";
			// 
			// LittleEndianRadio
			// 
			this.LittleEndianRadio.AutoSize = true;
			this.LittleEndianRadio.Location = new System.Drawing.Point(4, 35);
			this.LittleEndianRadio.Name = "LittleEndianRadio";
			this.LittleEndianRadio.Size = new System.Drawing.Size(83, 17);
			this.LittleEndianRadio.TabIndex = 1;
			this.LittleEndianRadio.Text = "Little Endian";
			this.LittleEndianRadio.UseVisualStyleBackColor = true;
			// 
			// BigEndianRadio
			// 
			this.BigEndianRadio.AutoSize = true;
			this.BigEndianRadio.Checked = true;
			this.BigEndianRadio.Location = new System.Drawing.Point(4, 18);
			this.BigEndianRadio.Name = "BigEndianRadio";
			this.BigEndianRadio.Size = new System.Drawing.Size(76, 17);
			this.BigEndianRadio.TabIndex = 0;
			this.BigEndianRadio.TabStop = true;
			this.BigEndianRadio.Text = "Big Endian";
			this.BigEndianRadio.UseVisualStyleBackColor = true;
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
			this.DomainComboBox.FormattingEnabled = true;
			this.DomainComboBox.Location = new System.Drawing.Point(12, 230);
			this.DomainComboBox.Name = "DomainComboBox";
			this.DomainComboBox.Size = new System.Drawing.Size(141, 21);
			this.DomainComboBox.TabIndex = 14;
			this.DomainComboBox.SelectedIndexChanged += new System.EventHandler(this.DomainComboBox_SelectedIndexChanged);
			// 
			// WatchEditor
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(213, 296);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.DomainComboBox);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.Controls.Add(this.EndianBox);
			this.Controls.Add(this.DataSizeBox);
			this.Controls.Add(this.DataTypeGroupBox);
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
			this.DataTypeGroupBox.ResumeLayout(false);
			this.DataTypeGroupBox.PerformLayout();
			this.DataSizeBox.ResumeLayout(false);
			this.DataSizeBox.PerformLayout();
			this.EndianBox.ResumeLayout(false);
			this.EndianBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private HexTextBox AddressBox;
        private System.Windows.Forms.TextBox NotesBox;
        private System.Windows.Forms.GroupBox DataTypeGroupBox;
        private System.Windows.Forms.RadioButton SignedRadio;
        private System.Windows.Forms.RadioButton UnsignedRadio;
        private System.Windows.Forms.RadioButton HexRadio;
        private System.Windows.Forms.GroupBox DataSizeBox;
        private System.Windows.Forms.RadioButton Byte1Radio;
        private System.Windows.Forms.RadioButton Byte2Radio;
        private System.Windows.Forms.RadioButton Byte4Radio;
        private System.Windows.Forms.GroupBox EndianBox;
        private System.Windows.Forms.RadioButton BigEndianRadio;
        private System.Windows.Forms.RadioButton LittleEndianRadio;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ComboBox DomainComboBox;
    }
}