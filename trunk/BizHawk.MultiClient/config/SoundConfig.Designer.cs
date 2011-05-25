namespace BizHawk.MultiClient
{
    partial class SoundConfig
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
            this.Cancel = new System.Windows.Forms.Button();
            this.OK = new System.Windows.Forms.Button();
            this.SoundOnCheckBox = new System.Windows.Forms.CheckBox();
            this.MuteFrameAdvance = new System.Windows.Forms.CheckBox();
            this.SoundVolGroup = new System.Windows.Forms.GroupBox();
            this.SoundVolBar = new System.Windows.Forms.TrackBar();
            this.SoundVolNumeric = new System.Windows.Forms.NumericUpDown();
            this.SampleRateCombo = new System.Windows.Forms.ComboBox();
            this.SampleRateBox = new System.Windows.Forms.GroupBox();
            this.SoundVolGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SoundVolBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SoundVolNumeric)).BeginInit();
            this.SampleRateBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(268, 251);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 0;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(187, 251);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 1;
            this.OK.Text = "&Ok";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // SoundOnCheckBox
            // 
            this.SoundOnCheckBox.AutoSize = true;
            this.SoundOnCheckBox.Location = new System.Drawing.Point(215, 12);
            this.SoundOnCheckBox.Name = "SoundOnCheckBox";
            this.SoundOnCheckBox.Size = new System.Drawing.Size(74, 17);
            this.SoundOnCheckBox.TabIndex = 2;
            this.SoundOnCheckBox.Text = "Sound On";
            this.SoundOnCheckBox.UseVisualStyleBackColor = true;
            this.SoundOnCheckBox.CheckedChanged += new System.EventHandler(this.SoundOnCheckBox_CheckedChanged);
            // 
            // MuteFrameAdvance
            // 
            this.MuteFrameAdvance.AutoSize = true;
            this.MuteFrameAdvance.Location = new System.Drawing.Point(215, 35);
            this.MuteFrameAdvance.Name = "MuteFrameAdvance";
            this.MuteFrameAdvance.Size = new System.Drawing.Size(128, 17);
            this.MuteFrameAdvance.TabIndex = 3;
            this.MuteFrameAdvance.Text = "Mute Frame Advance";
            this.MuteFrameAdvance.UseVisualStyleBackColor = true;
            // 
            // SoundVolGroup
            // 
            this.SoundVolGroup.Controls.Add(this.SoundVolBar);
            this.SoundVolGroup.Controls.Add(this.SoundVolNumeric);
            this.SoundVolGroup.Location = new System.Drawing.Point(12, 12);
            this.SoundVolGroup.Name = "SoundVolGroup";
            this.SoundVolGroup.Size = new System.Drawing.Size(90, 219);
            this.SoundVolGroup.TabIndex = 4;
            this.SoundVolGroup.TabStop = false;
            this.SoundVolGroup.Text = "Volume";
            // 
            // SoundVolBar
            // 
            this.SoundVolBar.LargeChange = 10;
            this.SoundVolBar.Location = new System.Drawing.Point(23, 23);
            this.SoundVolBar.Maximum = 100;
            this.SoundVolBar.Name = "SoundVolBar";
            this.SoundVolBar.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.SoundVolBar.Size = new System.Drawing.Size(42, 164);
            this.SoundVolBar.TabIndex = 1;
            this.SoundVolBar.TickFrequency = 10;
            this.SoundVolBar.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // SoundVolNumeric
            // 
            this.SoundVolNumeric.Location = new System.Drawing.Point(16, 190);
            this.SoundVolNumeric.Name = "SoundVolNumeric";
            this.SoundVolNumeric.Size = new System.Drawing.Size(59, 20);
            this.SoundVolNumeric.TabIndex = 0;
            this.SoundVolNumeric.ValueChanged += new System.EventHandler(this.SoundVolNumeric_ValueChanged);
            // 
            // SampleRateCombo
            // 
            this.SampleRateCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SampleRateCombo.FormattingEnabled = true;
            this.SampleRateCombo.Items.AddRange(new object[] {
            "22050",
            "32000",
            "44100",
            "48000",
            "96000"});
            this.SampleRateCombo.Location = new System.Drawing.Point(6, 19);
            this.SampleRateCombo.Name = "SampleRateCombo";
            this.SampleRateCombo.Size = new System.Drawing.Size(121, 21);
            this.SampleRateCombo.TabIndex = 5;
            // 
            // SampleRateBox
            // 
            this.SampleRateBox.Controls.Add(this.SampleRateCombo);
            this.SampleRateBox.Location = new System.Drawing.Point(127, 71);
            this.SampleRateBox.Name = "SampleRateBox";
            this.SampleRateBox.Size = new System.Drawing.Size(135, 53);
            this.SampleRateBox.TabIndex = 6;
            this.SampleRateBox.TabStop = false;
            this.SampleRateBox.Text = "Sample Rate";
            // 
            // SoundConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(355, 286);
            this.Controls.Add(this.SampleRateBox);
            this.Controls.Add(this.SoundVolGroup);
            this.Controls.Add(this.MuteFrameAdvance);
            this.Controls.Add(this.SoundOnCheckBox);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.Cancel);
            this.Name = "SoundConfig";
            this.ShowIcon = false;
            this.Text = "Sound Configuration";
            this.Load += new System.EventHandler(this.SoundConfig_Load);
            this.SoundVolGroup.ResumeLayout(false);
            this.SoundVolGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SoundVolBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SoundVolNumeric)).EndInit();
            this.SampleRateBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.CheckBox SoundOnCheckBox;
        private System.Windows.Forms.CheckBox MuteFrameAdvance;
        private System.Windows.Forms.GroupBox SoundVolGroup;
        private System.Windows.Forms.NumericUpDown SoundVolNumeric;
        private System.Windows.Forms.TrackBar SoundVolBar;
        private System.Windows.Forms.ComboBox SampleRateCombo;
        private System.Windows.Forms.GroupBox SampleRateBox;
    }
}