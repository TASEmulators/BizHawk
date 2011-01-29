namespace BizHawk.MultiClient
{
    partial class InputConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InputConfig));
            this.OK = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.ButtonsGroupBox = new System.Windows.Forms.GroupBox();
            this.ControllerImage = new System.Windows.Forms.PictureBox();
            this.ControllerSelectGroupBox = new System.Windows.Forms.GroupBox();
            this.ControllComboBox = new System.Windows.Forms.ComboBox();
            this.ControllerEnabledCheckbox = new System.Windows.Forms.CheckBox();
            this.AllowLR = new System.Windows.Forms.CheckBox();
            this.SystemGroupBox = new System.Windows.Forms.GroupBox();
            this.SystemComboBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.ControllerImage)).BeginInit();
            this.ControllerSelectGroupBox.SuspendLayout();
            this.SystemGroupBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(260, 284);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 0;
            this.OK.Text = "&Ok";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.OK_Click);
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(350, 284);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 1;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ButtonsGroupBox
            // 
            this.ButtonsGroupBox.Location = new System.Drawing.Point(12, 68);
            this.ButtonsGroupBox.Name = "ButtonsGroupBox";
            this.ButtonsGroupBox.Size = new System.Drawing.Size(240, 239);
            this.ButtonsGroupBox.TabIndex = 2;
            this.ButtonsGroupBox.TabStop = false;
            this.ButtonsGroupBox.Text = "Buttons";
            // 
            // ControllerImage
            // 
            this.ControllerImage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.ControllerImage.Location = new System.Drawing.Point(258, 73);
            this.ControllerImage.Name = "ControllerImage";
            this.ControllerImage.Size = new System.Drawing.Size(165, 156);
            this.ControllerImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.ControllerImage.TabIndex = 3;
            this.ControllerImage.TabStop = false;
            // 
            // ControllerSelectGroupBox
            // 
            this.ControllerSelectGroupBox.Controls.Add(this.ControllerEnabledCheckbox);
            this.ControllerSelectGroupBox.Controls.Add(this.ControllComboBox);
            this.ControllerSelectGroupBox.Location = new System.Drawing.Point(13, 12);
            this.ControllerSelectGroupBox.Name = "ControllerSelectGroupBox";
            this.ControllerSelectGroupBox.Size = new System.Drawing.Size(239, 50);
            this.ControllerSelectGroupBox.TabIndex = 4;
            this.ControllerSelectGroupBox.TabStop = false;
            this.ControllerSelectGroupBox.Text = "Controller";
            // 
            // ControllComboBox
            // 
            this.ControllComboBox.FormattingEnabled = true;
            this.ControllComboBox.Items.AddRange(new object[] {
            "Joypad 1",
            "Joypad 2",
            "Joypad 3",
            "Joypad 4"});
            this.ControllComboBox.Location = new System.Drawing.Point(6, 19);
            this.ControllComboBox.Name = "ControllComboBox";
            this.ControllComboBox.Size = new System.Drawing.Size(110, 21);
            this.ControllComboBox.TabIndex = 0;
            // 
            // ControllerEnabledCheckbox
            // 
            this.ControllerEnabledCheckbox.AutoSize = true;
            this.ControllerEnabledCheckbox.Checked = true;
            this.ControllerEnabledCheckbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ControllerEnabledCheckbox.Location = new System.Drawing.Point(143, 23);
            this.ControllerEnabledCheckbox.Name = "ControllerEnabledCheckbox";
            this.ControllerEnabledCheckbox.Size = new System.Drawing.Size(65, 17);
            this.ControllerEnabledCheckbox.TabIndex = 1;
            this.ControllerEnabledCheckbox.Text = "Enabled";
            this.ControllerEnabledCheckbox.UseVisualStyleBackColor = true;
            // 
            // AllowLR
            // 
            this.AllowLR.AutoSize = true;
            this.AllowLR.Location = new System.Drawing.Point(260, 253);
            this.AllowLR.Name = "AllowLR";
            this.AllowLR.Size = new System.Drawing.Size(156, 17);
            this.AllowLR.TabIndex = 5;
            this.AllowLR.Text = "Allow Left+Right/Up+Down";
            this.AllowLR.UseVisualStyleBackColor = true;
            // 
            // SystemGroupBox
            // 
            this.SystemGroupBox.Controls.Add(this.SystemComboBox);
            this.SystemGroupBox.Location = new System.Drawing.Point(258, 12);
            this.SystemGroupBox.Name = "SystemGroupBox";
            this.SystemGroupBox.Size = new System.Drawing.Size(158, 50);
            this.SystemGroupBox.TabIndex = 6;
            this.SystemGroupBox.TabStop = false;
            this.SystemGroupBox.Text = "System";
            // 
            // SystemComboBox
            // 
            this.SystemComboBox.FormattingEnabled = true;
            this.SystemComboBox.Items.AddRange(new object[] {
            "SMS / GG / SG-1000",
            "PC Engine / SGX",
            "Gameboy",
            "Sega Genesis",
            "TI-83"});
            this.SystemComboBox.Location = new System.Drawing.Point(6, 19);
            this.SystemComboBox.Name = "SystemComboBox";
            this.SystemComboBox.Size = new System.Drawing.Size(146, 21);
            this.SystemComboBox.TabIndex = 2;
            this.SystemComboBox.SelectedIndexChanged += new System.EventHandler(this.SystemComboBox_SelectedIndexChanged);
            // 
            // InputConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(439, 319);
            this.Controls.Add(this.SystemGroupBox);
            this.Controls.Add(this.AllowLR);
            this.Controls.Add(this.ControllerSelectGroupBox);
            this.Controls.Add(this.ControllerImage);
            this.Controls.Add(this.ButtonsGroupBox);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputConfig";
            this.ShowIcon = false;
            this.Text = "Configure Controllers";
            this.Load += new System.EventHandler(this.InputConfig_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ControllerImage)).EndInit();
            this.ControllerSelectGroupBox.ResumeLayout(false);
            this.ControllerSelectGroupBox.PerformLayout();
            this.SystemGroupBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OK;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.GroupBox ButtonsGroupBox;
        private System.Windows.Forms.PictureBox ControllerImage;
        private System.Windows.Forms.GroupBox ControllerSelectGroupBox;
        private System.Windows.Forms.ComboBox ControllComboBox;
        private System.Windows.Forms.CheckBox ControllerEnabledCheckbox;
        private System.Windows.Forms.CheckBox AllowLR;
        private System.Windows.Forms.GroupBox SystemGroupBox;
        private System.Windows.Forms.ComboBox SystemComboBox;
    }
}