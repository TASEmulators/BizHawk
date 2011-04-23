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
            this.MessagesRadio = new System.Windows.Forms.RadioButton();
            this.InputDisplayRadio = new System.Windows.Forms.RadioButton();
            this.LagCounterRadio = new System.Windows.Forms.RadioButton();
            this.FrameCounterRadio = new System.Windows.Forms.RadioButton();
            this.FPSRadio = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.YNumeric = new System.Windows.Forms.NumericUpDown();
            this.XNumeric = new System.Windows.Forms.NumericUpDown();
            this.PositionPanel = new System.Windows.Forms.Panel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.button1 = new System.Windows.Forms.Button();
            this.MessageColorBox = new System.Windows.Forms.GroupBox();
            this.MessageColorDialog = new System.Windows.Forms.ColorDialog();
            this.MessageTypeBox.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(417, 398);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 1;
            this.OK.Text = "&OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // MessageTypeBox
            // 
            this.MessageTypeBox.Controls.Add(this.MessagesRadio);
            this.MessageTypeBox.Controls.Add(this.InputDisplayRadio);
            this.MessageTypeBox.Controls.Add(this.LagCounterRadio);
            this.MessageTypeBox.Controls.Add(this.FrameCounterRadio);
            this.MessageTypeBox.Controls.Add(this.FPSRadio);
            this.MessageTypeBox.Location = new System.Drawing.Point(12, 190);
            this.MessageTypeBox.Name = "MessageTypeBox";
            this.MessageTypeBox.Size = new System.Drawing.Size(105, 139);
            this.MessageTypeBox.TabIndex = 2;
            this.MessageTypeBox.TabStop = false;
            this.MessageTypeBox.Text = "Message Type";
            // 
            // MessagesRadio
            // 
            this.MessagesRadio.AutoSize = true;
            this.MessagesRadio.Location = new System.Drawing.Point(6, 114);
            this.MessagesRadio.Name = "MessagesRadio";
            this.MessagesRadio.Size = new System.Drawing.Size(73, 17);
            this.MessagesRadio.TabIndex = 4;
            this.MessagesRadio.Text = "Messages";
            this.MessagesRadio.UseVisualStyleBackColor = true;
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
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.YNumeric);
            this.groupBox1.Controls.Add(this.XNumeric);
            this.groupBox1.Controls.Add(this.PositionPanel);
            this.groupBox1.Location = new System.Drawing.Point(144, 119);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(267, 210);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Position";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(152, 126);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(12, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "y";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 126);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(12, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "x";
            // 
            // YNumeric
            // 
            this.YNumeric.Location = new System.Drawing.Point(170, 124);
            this.YNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.YNumeric.Name = "YNumeric";
            this.YNumeric.Size = new System.Drawing.Size(66, 20);
            this.YNumeric.TabIndex = 2;
            // 
            // XNumeric
            // 
            this.XNumeric.Location = new System.Drawing.Point(31, 124);
            this.XNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
            this.XNumeric.Name = "XNumeric";
            this.XNumeric.Size = new System.Drawing.Size(66, 20);
            this.XNumeric.TabIndex = 1;
            // 
            // PositionPanel
            // 
            this.PositionPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.PositionPanel.Location = new System.Drawing.Point(16, 18);
            this.PositionPanel.Name = "PositionPanel";
            this.PositionPanel.Size = new System.Drawing.Size(220, 100);
            this.PositionPanel.TabIndex = 0;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.button1);
            this.groupBox2.Controls.Add(this.MessageColorBox);
            this.groupBox2.Location = new System.Drawing.Point(18, 13);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(393, 100);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Message Color";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(48, 45);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(65, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Change...";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // MessageColorBox
            // 
            this.MessageColorBox.Location = new System.Drawing.Point(6, 38);
            this.MessageColorBox.Name = "MessageColorBox";
            this.MessageColorBox.Size = new System.Drawing.Size(36, 30);
            this.MessageColorBox.TabIndex = 0;
            this.MessageColorBox.TabStop = false;
            // 
            // MessageConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 433);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.MessageTypeBox);
            this.Controls.Add(this.OK);
            this.Name = "MessageConfig";
            this.Text = "Configure On Screen Messages";
            this.Load += new System.EventHandler(this.MessageConfig_Load);
            this.MessageTypeBox.ResumeLayout(false);
            this.MessageTypeBox.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.YNumeric)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.XNumeric)).EndInit();
            this.groupBox2.ResumeLayout(false);
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
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown YNumeric;
        private System.Windows.Forms.NumericUpDown XNumeric;
        private System.Windows.Forms.Panel PositionPanel;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox MessageColorBox;
        private System.Windows.Forms.ColorDialog MessageColorDialog;
    }
}