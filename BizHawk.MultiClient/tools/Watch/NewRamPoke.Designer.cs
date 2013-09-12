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
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.OutputLabel = new System.Windows.Forms.Label();
			this.ValeLabel = new System.Windows.Forms.Label();
			this.ValueBox = new System.Windows.Forms.TextBox();
			this.ValueHexLabel = new System.Windows.Forms.Label();
			this.DisplayTypeLabel = new System.Windows.Forms.Label();
			this.SizeLabel = new System.Windows.Forms.Label();
			this.BigEndianLabel = new System.Windows.Forms.Label();
			this.AddressBox = new BizHawk.HexTextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(20, 33);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(62, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Address: 0x";
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OK.Location = new System.Drawing.Point(12, 163);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(65, 23);
			this.OK.TabIndex = 35;
			this.OK.Text = "&Poke";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.OK_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(136, 163);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(65, 23);
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
			this.ValeLabel.Location = new System.Drawing.Point(31, 59);
			this.ValeLabel.Name = "ValeLabel";
			this.ValeLabel.Size = new System.Drawing.Size(37, 13);
			this.ValeLabel.TabIndex = 10;
			this.ValeLabel.Text = "Value:";
			// 
			// ValueBox
			// 
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(82, 57);
			this.ValueBox.MaxLength = 9;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.Size = new System.Drawing.Size(116, 20);
			this.ValueBox.TabIndex = 10;
			this.ValueBox.Text = "0000";
			this.ValueBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ValueBox_KeyPress);
			// 
			// ValueHexLabel
			// 
			this.ValueHexLabel.AutoSize = true;
			this.ValueHexLabel.Location = new System.Drawing.Point(64, 60);
			this.ValueHexLabel.Name = "ValueHexLabel";
			this.ValueHexLabel.Size = new System.Drawing.Size(18, 13);
			this.ValueHexLabel.TabIndex = 11;
			this.ValueHexLabel.Text = "0x";
			// 
			// DisplayTypeLabel
			// 
			this.DisplayTypeLabel.AutoSize = true;
			this.DisplayTypeLabel.Location = new System.Drawing.Point(81, 101);
			this.DisplayTypeLabel.Name = "DisplayTypeLabel";
			this.DisplayTypeLabel.Size = new System.Drawing.Size(52, 13);
			this.DisplayTypeLabel.TabIndex = 24;
			this.DisplayTypeLabel.Text = "Unsigned";
			// 
			// SizeLabel
			// 
			this.SizeLabel.AutoSize = true;
			this.SizeLabel.Location = new System.Drawing.Point(82, 83);
			this.SizeLabel.Name = "SizeLabel";
			this.SizeLabel.Size = new System.Drawing.Size(28, 13);
			this.SizeLabel.TabIndex = 23;
			this.SizeLabel.Text = "Byte";
			// 
			// BigEndianLabel
			// 
			this.BigEndianLabel.AutoSize = true;
			this.BigEndianLabel.Location = new System.Drawing.Point(82, 119);
			this.BigEndianLabel.Name = "BigEndianLabel";
			this.BigEndianLabel.Size = new System.Drawing.Size(58, 13);
			this.BigEndianLabel.TabIndex = 41;
			this.BigEndianLabel.Text = "Big Endian";
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Enabled = false;
			this.AddressBox.Location = new System.Drawing.Point(82, 30);
			this.AddressBox.MaxLength = 8;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Size = new System.Drawing.Size(116, 20);
			this.AddressBox.TabIndex = 5;
			this.AddressBox.Text = "0000";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(39, 119);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(43, 13);
			this.label2.TabIndex = 44;
			this.label2.Text = "Endian:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(11, 101);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(71, 13);
			this.label3.TabIndex = 43;
			this.label3.Text = "Display Type:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(52, 83);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(30, 13);
			this.label4.TabIndex = 42;
			this.label4.Text = "Size:";
			// 
			// NewRamPoke
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(213, 202);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.BigEndianLabel);
			this.Controls.Add(this.DisplayTypeLabel);
			this.Controls.Add(this.SizeLabel);
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
		private System.Windows.Forms.Label DisplayTypeLabel;
		private System.Windows.Forms.Label SizeLabel;
		private System.Windows.Forms.Label BigEndianLabel;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
    }
}