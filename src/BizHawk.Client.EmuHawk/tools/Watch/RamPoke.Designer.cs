namespace BizHawk.Client.EmuHawk
{
    partial class RamPoke
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
			this.OK = new System.Windows.Forms.Button();
			this.Cancel = new System.Windows.Forms.Button();
			this.OutputLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.ValeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.ValueBox = new BizHawk.Client.EmuHawk.WatchValueBox();
			this.ValueHexLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.DisplayTypeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.SizeLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.BigEndianLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.AddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
			this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label3 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
			this.DomainLabel = new BizHawk.WinForms.Controls.LocLabelEx();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(20, 33);
			this.label1.Name = "label1";
			this.label1.Text = "Address: 0x";
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.OK.Location = new System.Drawing.Point(12, 169);
			this.OK.Name = "OK";
			this.OK.Size = new System.Drawing.Size(65, 23);
			this.OK.TabIndex = 35;
			this.OK.Text = "&Poke";
			this.OK.UseVisualStyleBackColor = true;
			this.OK.Click += new System.EventHandler(this.Ok_Click);
			// 
			// Cancel
			// 
			this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel.Location = new System.Drawing.Point(136, 169);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(65, 23);
			this.Cancel.TabIndex = 40;
			this.Cancel.Text = "&Close";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// OutputLabel
			// 
			this.OutputLabel.Location = new System.Drawing.Point(12, 7);
			this.OutputLabel.Name = "OutputLabel";
			this.OutputLabel.Text = "Enter an address to poke:";
			// 
			// ValeLabel
			// 
			this.ValeLabel.Location = new System.Drawing.Point(31, 59);
			this.ValeLabel.Name = "ValeLabel";
			this.ValeLabel.Text = "Value:";
			// 
			// ValueBox
			// 
			this.ValueBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
			this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.ValueBox.Location = new System.Drawing.Point(82, 57);
			this.ValueBox.MaxLength = 2;
			this.ValueBox.Name = "ValueBox";
			this.ValueBox.Nullable = false;
			this.ValueBox.Size = new System.Drawing.Size(116, 20);
			this.ValueBox.TabIndex = 10;
			this.ValueBox.Text = "00";
			this.ValueBox.Type = BizHawk.Client.Common.DisplayType.Hex;
			// 
			// ValueHexLabel
			// 
			this.ValueHexLabel.Location = new System.Drawing.Point(64, 60);
			this.ValueHexLabel.Name = "ValueHexLabel";
			this.ValueHexLabel.Text = "0x";
			// 
			// DisplayTypeLabel
			// 
			this.DisplayTypeLabel.Location = new System.Drawing.Point(81, 118);
			this.DisplayTypeLabel.Name = "DisplayTypeLabel";
			this.DisplayTypeLabel.Text = "Unsigned";
			// 
			// SizeLabel
			// 
			this.SizeLabel.Location = new System.Drawing.Point(82, 101);
			this.SizeLabel.Name = "SizeLabel";
			this.SizeLabel.Text = "Byte";
			// 
			// BigEndianLabel
			// 
			this.BigEndianLabel.Location = new System.Drawing.Point(82, 135);
			this.BigEndianLabel.Name = "BigEndianLabel";
			this.BigEndianLabel.Text = "Big Endian";
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Enabled = false;
			this.AddressBox.Location = new System.Drawing.Point(82, 30);
			this.AddressBox.MaxLength = 8;
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Nullable = false;
			this.AddressBox.Size = new System.Drawing.Size(116, 20);
			this.AddressBox.TabIndex = 5;
			this.AddressBox.Text = "0000";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(39, 135);
			this.label2.Name = "label2";
			this.label2.Text = "Endian:";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(11, 118);
			this.label3.Name = "label3";
			this.label3.Text = "Display Type:";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(52, 101);
			this.label4.Name = "label4";
			this.label4.Text = "Size:";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(36, 84);
			this.label5.Name = "label5";
			this.label5.Text = "Domain:";
			// 
			// DomainLabel
			// 
			this.DomainLabel.Location = new System.Drawing.Point(82, 84);
			this.DomainLabel.Name = "DomainLabel";
			this.DomainLabel.Text = "Main Memory";
			// 
			// RamPoke
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(213, 208);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.DomainLabel);
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
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RamPoke";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "RAM Poke";
			this.Load += new System.EventHandler(this.RamPoke_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        private BizHawk.WinForms.Controls.LocLabelEx label1;
        private HexTextBox AddressBox;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
        private BizHawk.WinForms.Controls.LocLabelEx OutputLabel;
        private BizHawk.WinForms.Controls.LocLabelEx ValeLabel;
        private WatchValueBox ValueBox;
		private BizHawk.WinForms.Controls.LocLabelEx ValueHexLabel;
		private BizHawk.WinForms.Controls.LocLabelEx DisplayTypeLabel;
		private BizHawk.WinForms.Controls.LocLabelEx SizeLabel;
		private BizHawk.WinForms.Controls.LocLabelEx BigEndianLabel;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private BizHawk.WinForms.Controls.LocLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx DomainLabel;
    }
}