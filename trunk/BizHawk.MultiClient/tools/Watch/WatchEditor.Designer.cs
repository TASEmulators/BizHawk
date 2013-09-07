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
			this.DomainComboBox = new System.Windows.Forms.ComboBox();
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
			this.DomainComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
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
		private System.Windows.Forms.ComboBox DomainComboBox;
    }
}