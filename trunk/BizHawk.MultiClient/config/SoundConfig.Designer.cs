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
            this.SuspendLayout();
            // 
            // Cancel
            // 
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(198, 174);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 0;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // OK
            // 
            this.OK.Location = new System.Drawing.Point(112, 174);
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
            this.SoundOnCheckBox.Location = new System.Drawing.Point(12, 12);
            this.SoundOnCheckBox.Name = "SoundOnCheckBox";
            this.SoundOnCheckBox.Size = new System.Drawing.Size(74, 17);
            this.SoundOnCheckBox.TabIndex = 2;
            this.SoundOnCheckBox.Text = "Sound On";
            this.SoundOnCheckBox.UseVisualStyleBackColor = true;
            // 
            // MuteFrameAdvance
            // 
            this.MuteFrameAdvance.AutoSize = true;
            this.MuteFrameAdvance.Location = new System.Drawing.Point(12, 35);
            this.MuteFrameAdvance.Name = "MuteFrameAdvance";
            this.MuteFrameAdvance.Size = new System.Drawing.Size(128, 17);
            this.MuteFrameAdvance.TabIndex = 3;
            this.MuteFrameAdvance.Text = "Mute Frame Advance";
            this.MuteFrameAdvance.UseVisualStyleBackColor = true;
            // 
            // SoundConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(280, 209);
            this.Controls.Add(this.MuteFrameAdvance);
            this.Controls.Add(this.SoundOnCheckBox);
            this.Controls.Add(this.OK);
            this.Controls.Add(this.Cancel);
            this.Name = "SoundConfig";
            this.Text = "Sound Configuration";
            this.Load += new System.EventHandler(this.SoundConfig_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.CheckBox SoundOnCheckBox;
        private System.Windows.Forms.CheckBox MuteFrameAdvance;
    }
}