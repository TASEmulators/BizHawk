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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RamPoke));
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
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OutputLabel
            // 
            resources.ApplyResources(this.OutputLabel, "OutputLabel");
            this.OutputLabel.Name = "OutputLabel";
            // 
            // ValeLabel
            // 
            resources.ApplyResources(this.ValeLabel, "ValeLabel");
            this.ValeLabel.Name = "ValeLabel";
            // 
            // ValueBox
            // 
            resources.ApplyResources(this.ValueBox, "ValueBox");
            this.ValueBox.ByteSize = BizHawk.Client.Common.WatchSize.Byte;
            this.ValueBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ValueBox.Name = "ValueBox";
            this.ValueBox.Nullable = false;
            this.ValueBox.Type = BizHawk.Client.Common.WatchDisplayType.Hex;
            // 
            // ValueHexLabel
            // 
            resources.ApplyResources(this.ValueHexLabel, "ValueHexLabel");
            this.ValueHexLabel.Name = "ValueHexLabel";
            // 
            // DisplayTypeLabel
            // 
            resources.ApplyResources(this.DisplayTypeLabel, "DisplayTypeLabel");
            this.DisplayTypeLabel.Name = "DisplayTypeLabel";
            // 
            // SizeLabel
            // 
            resources.ApplyResources(this.SizeLabel, "SizeLabel");
            this.SizeLabel.Name = "SizeLabel";
            // 
            // BigEndianLabel
            // 
            resources.ApplyResources(this.BigEndianLabel, "BigEndianLabel");
            this.BigEndianLabel.Name = "BigEndianLabel";
            // 
            // AddressBox
            // 
            resources.ApplyResources(this.AddressBox, "AddressBox");
            this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.AddressBox.Name = "AddressBox";
            this.AddressBox.Nullable = false;
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.Name = "label4";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // DomainLabel
            // 
            resources.ApplyResources(this.DomainLabel, "DomainLabel");
            this.DomainLabel.Name = "DomainLabel";
            // 
            // RamPoke
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
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