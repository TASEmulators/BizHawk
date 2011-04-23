namespace BizHawk.MultiClient
{
    partial class MessageConfig
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
            this.OK = new System.Windows.Forms.Button();
            this.MessageTypeBox = new System.Windows.Forms.GroupBox();
            this.MessLabel = new System.Windows.Forms.Label();
            this.InpLabel = new System.Windows.Forms.Label();
            this.LagLabel = new System.Windows.Forms.Label();
            this.FCLabel = new System.Windows.Forms.Label();
            this.FpsPosLabel = new System.Windows.Forms.Label();
            this.MessagesRadio = new System.Windows.Forms.RadioButton();
            this.InputDisplayRadio = new System.Windows.Forms.RadioButton();
            this.LagCounterRadio = new System.Windows.Forms.RadioButton();
            this.FrameCounterRadio = new System.Windows.Forms.RadioButton();
            this.FPSRadio = new System.Windows.Forms.RadioButton();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.ColorText = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.MessageColorBox = new System.Windows.Forms.GroupBox();
            this.ColorPanel = new System.Windows.Forms.Panel();
            this.MessageColorDialog = new System.Windows.Forms.ColorDialog();
            this.Cancel = new System.Windows.Forms.Button();
            this.ResetDefaultsButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.PositionPanel = new System.Windows.Forms.Panel();
            this.XNumeric = new System.Windows.Forms.NumericUpDown();
            this.YNumeric = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.PositionGroupBox = new System.Windows.Forms.GroupBox();
            this.MessageTypeBox.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.MessageColorBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
            this.PositionGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(327, 319);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 1;
            this.OK.Text = "&OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // MessageTypeBox
            // 
            this.MessageTypeBox.Controls.Add(this.MessLabel);
            this.MessageTypeBox.Controls.Add(this.InpLabel);
            this.MessageTypeBox.Controls.Add(this.LagLabel);
            this.MessageTypeBox.Controls.Add(this.FCLabel);
            this.MessageTypeBox.Controls.Add(this.FpsPosLabel);
            this.MessageTypeBox.Controls.Add(this.MessagesRadio);
            this.MessageTypeBox.Controls.Add(this.InputDisplayRadio);
            this.MessageTypeBox.Controls.Add(this.LagCounterRadio);
            this.MessageTypeBox.Controls.Add(this.FrameCounterRadio);
            this.MessageTypeBox.Controls.Add(this.FPSRadio);
            this.MessageTypeBox.Location = new System.Drawing.Point(12, 12);
            this.MessageTypeBox.Name = "MessageTypeBox";
            this.MessageTypeBox.Size = new System.Drawing.Size(149, 139);
            this.MessageTypeBox.TabIndex = 2;
            this.MessageTypeBox.TabStop = false;
            this.MessageTypeBox.Text = "Message Type";
            // 
            // MessLabel
            // 
            this.MessLabel.AutoSize = true;
            this.MessLabel.Location = new System.Drawing.Point(98, 116);
            this.MessLabel.Name = "MessLabel";
            this.MessLabel.Size = new System.Drawing.Size(49, 13);
            this.MessLabel.TabIndex = 9;
            this.MessLabel.Text = "255, 255";
            // 
            // InpLabel
            // 
            this.InpLabel.AutoSize = true;
            this.InpLabel.Location = new System.Drawing.Point(98, 92);
            this.InpLabel.Name = "InpLabel";
            this.InpLabel.Size = new System.Drawing.Size(49, 13);
            this.InpLabel.TabIndex = 8;
            this.InpLabel.Text = "255, 255";
            // 
            // LagLabel
            // 
            this.LagLabel.AutoSize = true;
            this.LagLabel.Location = new System.Drawing.Point(98, 68);
            this.LagLabel.Name = "LagLabel";
            this.LagLabel.Size = new System.Drawing.Size(49, 13);
            this.LagLabel.TabIndex = 7;
            this.LagLabel.Text = "255, 255";
            // 
            // FCLabel
            // 
            this.FCLabel.AutoSize = true;
            this.FCLabel.Location = new System.Drawing.Point(98, 44);
            this.FCLabel.Name = "FCLabel";
            this.FCLabel.Size = new System.Drawing.Size(49, 13);
            this.FCLabel.TabIndex = 6;
            this.FCLabel.Text = "255, 255";
            // 
            // FpsPosLabel
            // 
            this.FpsPosLabel.AutoSize = true;
            this.FpsPosLabel.Location = new System.Drawing.Point(98, 20);
            this.FpsPosLabel.Name = "FpsPosLabel";
            this.FpsPosLabel.Size = new System.Drawing.Size(49, 13);
            this.FpsPosLabel.TabIndex = 5;
            this.FpsPosLabel.Text = "255, 255";
            // 
            // MessagesRadio
            // 
            this.MessagesRadio.AutoSize = true;
            this.MessagesRadio.Enabled = false;
            this.MessagesRadio.Location = new System.Drawing.Point(6, 114);
            this.MessagesRadio.Name = "MessagesRadio";
            this.MessagesRadio.Size = new System.Drawing.Size(73, 17);
            this.MessagesRadio.TabIndex = 4;
            this.MessagesRadio.Text = "Messages";
            this.MessagesRadio.UseVisualStyleBackColor = true;
            this.MessagesRadio.CheckedChanged += new System.EventHandler(this.MessagesRadio_CheckedChanged);
            // 
            // InputDisplayRadio
            // 
            this.InputDisplayRadio.AutoSize = true;
            this.InputDisplayRadio.Location = new System.Drawing.Point(6, 90);
            this.InputDisplayRadio.Name = "InputDisplayRadio";
            this.InputDisplayRadio.Size = new System.Drawing.Size(86, 17);
            this.InputDisplayRadio.TabIndex = 3;
            this.InputDisplayRadio.Text = "Input Display";
            this.InputDisplayRadio.UseVisualStyleBackColor = true;
            this.InputDisplayRadio.CheckedChanged += new System.EventHandler(this.InputDisplayRadio_CheckedChanged);
            // 
            // LagCounterRadio
            // 
            this.LagCounterRadio.AutoSize = true;
            this.LagCounterRadio.Location = new System.Drawing.Point(6, 66);
            this.LagCounterRadio.Name = "LagCounterRadio";
            this.LagCounterRadio.Size = new System.Drawing.Size(83, 17);
            this.LagCounterRadio.TabIndex = 2;
            this.LagCounterRadio.Text = "Lag Counter";
            this.LagCounterRadio.UseVisualStyleBackColor = true;
            this.LagCounterRadio.CheckedChanged += new System.EventHandler(this.LagCounterRadio_CheckedChanged);
            // 
            // FrameCounterRadio
            // 
            this.FrameCounterRadio.AutoSize = true;
            this.FrameCounterRadio.Location = new System.Drawing.Point(6, 42);
            this.FrameCounterRadio.Name = "FrameCounterRadio";
            this.FrameCounterRadio.Size = new System.Drawing.Size(93, 17);
            this.FrameCounterRadio.TabIndex = 1;
            this.FrameCounterRadio.Text = "Frame counter";
            this.FrameCounterRadio.UseVisualStyleBackColor = true;
            this.FrameCounterRadio.CheckedChanged += new System.EventHandler(this.FrameCounterRadio_CheckedChanged);
            // 
            // FPSRadio
            // 
            this.FPSRadio.AutoSize = true;
            this.FPSRadio.Checked = true;
            this.FPSRadio.Location = new System.Drawing.Point(6, 18);
            this.FPSRadio.Name = "FPSRadio";
            this.FPSRadio.Size = new System.Drawing.Size(42, 17);
            this.FPSRadio.TabIndex = 0;
            this.FPSRadio.TabStop = true;
            this.FPSRadio.Text = "Fps";
            this.FPSRadio.UseVisualStyleBackColor = true;
            this.FPSRadio.CheckedChanged += new System.EventHandler(this.FPSRadio_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.ColorText);
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.MessageColorBox);
            this.groupBox2.Location = new System.Drawing.Point(12, 173);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(128, 88);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Message Color";
            // 
            // ColorText
            // 
            this.ColorText.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ColorText.Location = new System.Drawing.Point(61, 27);
            this.ColorText.MaxLength = 8;
            this.ColorText.Name = "ColorText";
            this.ColorText.ReadOnly = true;
            this.ColorText.Size = new System.Drawing.Size(59, 20);
            this.ColorText.TabIndex = 2;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 53);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(65, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Change...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // MessageColorBox
            // 
            this.MessageColorBox.Controls.Add(this.ColorPanel);
            this.MessageColorBox.Location = new System.Drawing.Point(12, 19);
            this.MessageColorBox.Name = "MessageColorBox";
            this.MessageColorBox.Size = new System.Drawing.Size(29, 29);
            this.MessageColorBox.TabIndex = 0;
            this.MessageColorBox.TabStop = false;
            // 
            // ColorPanel
            // 
            this.ColorPanel.Location = new System.Drawing.Point(4, 8);
            this.ColorPanel.Name = "ColorPanel";
            this.ColorPanel.Size = new System.Drawing.Size(20, 16);
            this.ColorPanel.TabIndex = 7;
            this.ColorPanel.DoubleClick += new System.EventHandler(this.ColorPanel_DoubleClick);
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(408, 319);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 5;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ResetDefaultsButton
            // 
            this.ResetDefaultsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ResetDefaultsButton.Location = new System.Drawing.Point(18, 280);
            this.ResetDefaultsButton.Name = "ResetDefaultsButton";
            this.ResetDefaultsButton.Size = new System.Drawing.Size(96, 23);
            this.ResetDefaultsButton.TabIndex = 6;
            this.ResetDefaultsButton.Text = "Restore Defaults";
            this.ResetDefaultsButton.UseVisualStyleBackColor = true;
            this.ResetDefaultsButton.Click += new System.EventHandler(this.ResetDefaultsButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(44, 30);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(18, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "0x";
            // 
            // PositionPanel
            // 
            this.PositionPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PositionPanel.Location = new System.Drawing.Point(16, 18);
            this.PositionPanel.Name = "PositionPanel";
            this.PositionPanel.Size = new System.Drawing.Size(264, 248);
            this.PositionPanel.TabIndex = 0;
            this.PositionPanel.MouseLeave += new System.EventHandler(this.PositionPanel_MouseLeave);
            this.PositionPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.PositionPanel_Paint);
            this.PositionPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseMove);
            this.PositionPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseDown);
            this.PositionPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.PositionPanel_MouseUp);
            this.PositionPanel.MouseEnter += new System.EventHandler(this.PositionPanel_MouseEnter);
            // 
            // XNumeric
            // 
            this.XNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.XNumeric.Location = new System.Drawing.Point(28, 271);
            this.XNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.XNumeric.Name = "XNumeric";
            this.XNumeric.Size = new System.Drawing.Size(44, 20);
            this.XNumeric.TabIndex = 1;
            this.XNumeric.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.XNumeric.ValueChanged += new System.EventHandler(this.XNumeric_ValueChanged);
            // 
            // YNumeric
            // 
            this.YNumeric.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.YNumeric.Location = new System.Drawing.Point(91, 271);
            this.YNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.YNumeric.Name = "YNumeric";
            this.YNumeric.Size = new System.Drawing.Size(44, 20);
            this.YNumeric.TabIndex = 2;
            this.YNumeric.Value = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.YNumeric.ValueChanged += new System.EventHandler(this.YNumeric_ValueChanged);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 274);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(12, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "x";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(77, 273);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(12, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "y";
            // 
            // PositionGroupBox
            // 
            this.PositionGroupBox.Controls.Add(this.label2);
            this.PositionGroupBox.Controls.Add(this.label1);
            this.PositionGroupBox.Controls.Add(this.YNumeric);
            this.PositionGroupBox.Controls.Add(this.XNumeric);
            this.PositionGroupBox.Controls.Add(this.PositionPanel);
            this.PositionGroupBox.Location = new System.Drawing.Point(182, 12);
            this.PositionGroupBox.Name = "PositionGroupBox";
            this.PositionGroupBox.Size = new System.Drawing.Size(301, 299);
            this.PositionGroupBox.TabIndex = 3;
            this.PositionGroupBox.TabStop = false;
            this.PositionGroupBox.Text = "Position";
            // 
            // MessageConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(521, 354);
            this.Controls.Add(this.ResetDefaultsButton);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.PositionGroupBox);
            this.Controls.Add(this.MessageTypeBox);
            this.Controls.Add(this.OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "MessageConfig";
            this.ShowIcon = false;
            this.Text = "Configure On Screen Messages";
            this.Load += new System.EventHandler(this.MessageConfig_Load);
            this.MessageTypeBox.ResumeLayout(false);
            this.MessageTypeBox.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.MessageColorBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
            this.PositionGroupBox.ResumeLayout(false);
            this.PositionGroupBox.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.GroupBox MessageTypeBox;
        private System.Windows.Forms.RadioButton MessagesRadio;
        private System.Windows.Forms.RadioButton InputDisplayRadio;
        private System.Windows.Forms.RadioButton LagCounterRadio;
        private System.Windows.Forms.RadioButton FrameCounterRadio;
        private System.Windows.Forms.RadioButton FPSRadio;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ColorDialog MessageColorDialog;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Label MessLabel;
        private System.Windows.Forms.Label InpLabel;
        private System.Windows.Forms.Label LagLabel;
        private System.Windows.Forms.Label FCLabel;
        private System.Windows.Forms.Label FpsPosLabel;
        private System.Windows.Forms.Button ResetDefaultsButton;
        private System.Windows.Forms.TextBox ColorText;
        private System.Windows.Forms.GroupBox MessageColorBox;
        private System.Windows.Forms.Panel ColorPanel;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Panel PositionPanel;
        private System.Windows.Forms.NumericUpDown XNumeric;
        private System.Windows.Forms.NumericUpDown YNumeric;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox PositionGroupBox;
    }
}