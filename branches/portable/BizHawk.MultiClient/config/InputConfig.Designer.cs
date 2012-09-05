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
			this.IDX_CONTROLLERENABLED = new System.Windows.Forms.CheckBox();
			this.ControllComboBox = new System.Windows.Forms.ComboBox();
			this.SystemGroupBox = new System.Windows.Forms.GroupBox();
			this.SystemComboBox = new System.Windows.Forms.ComboBox();
			this.AllowLR = new System.Windows.Forms.CheckBox();
			this.AutoTab = new System.Windows.Forms.CheckBox();
			this.label38 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.ControllerImage)).BeginInit();
			this.ControllerSelectGroupBox.SuspendLayout();
			this.SystemGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// OK
			// 
			this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OK.Location = new System.Drawing.Point(259, 329);
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
			this.Cancel.Location = new System.Drawing.Point(342, 329);
			this.Cancel.Name = "Cancel";
			this.Cancel.Size = new System.Drawing.Size(75, 23);
			this.Cancel.TabIndex = 1;
			this.Cancel.Text = "&Cancel";
			this.Cancel.UseVisualStyleBackColor = true;
			this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
			// 
			// ButtonsGroupBox
			// 
			this.ButtonsGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ButtonsGroupBox.Location = new System.Drawing.Point(12, 84);
			this.ButtonsGroupBox.Name = "ButtonsGroupBox";
			this.ButtonsGroupBox.Size = new System.Drawing.Size(241, 268);
			this.ButtonsGroupBox.TabIndex = 2;
			this.ButtonsGroupBox.TabStop = false;
			this.ButtonsGroupBox.Text = "Buttons";
			// 
			// ControllerImage
			// 
			this.ControllerImage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ControllerImage.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.ControllerImage.Location = new System.Drawing.Point(259, 84);
			this.ControllerImage.Name = "ControllerImage";
			this.ControllerImage.Size = new System.Drawing.Size(169, 214);
			this.ControllerImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.ControllerImage.TabIndex = 3;
			this.ControllerImage.TabStop = false;
			// 
			// ControllerSelectGroupBox
			// 
			this.ControllerSelectGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ControllerSelectGroupBox.Controls.Add(this.IDX_CONTROLLERENABLED);
			this.ControllerSelectGroupBox.Controls.Add(this.ControllComboBox);
			this.ControllerSelectGroupBox.Location = new System.Drawing.Point(13, 28);
			this.ControllerSelectGroupBox.Name = "ControllerSelectGroupBox";
			this.ControllerSelectGroupBox.Size = new System.Drawing.Size(240, 50);
			this.ControllerSelectGroupBox.TabIndex = 4;
			this.ControllerSelectGroupBox.TabStop = false;
			this.ControllerSelectGroupBox.Text = "Controller";
			// 
			// IDX_CONTROLLERENABLED
			// 
			this.IDX_CONTROLLERENABLED.AutoSize = true;
			this.IDX_CONTROLLERENABLED.Checked = true;
			this.IDX_CONTROLLERENABLED.CheckState = System.Windows.Forms.CheckState.Checked;
			this.IDX_CONTROLLERENABLED.Location = new System.Drawing.Point(169, 23);
			this.IDX_CONTROLLERENABLED.Name = "IDX_CONTROLLERENABLED";
			this.IDX_CONTROLLERENABLED.Size = new System.Drawing.Size(65, 17);
			this.IDX_CONTROLLERENABLED.TabIndex = 1;
			this.IDX_CONTROLLERENABLED.Text = "Enabled";
			this.IDX_CONTROLLERENABLED.UseVisualStyleBackColor = true;
			this.IDX_CONTROLLERENABLED.Visible = false;
			// 
			// ControllComboBox
			// 
			this.ControllComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.ControllComboBox.FormattingEnabled = true;
			this.ControllComboBox.Location = new System.Drawing.Point(6, 19);
			this.ControllComboBox.Name = "ControllComboBox";
			this.ControllComboBox.Size = new System.Drawing.Size(157, 21);
			this.ControllComboBox.TabIndex = 0;
			this.ControllComboBox.SelectedIndexChanged += new System.EventHandler(this.ControllComboBox_SelectedIndexChanged);
			// 
			// SystemGroupBox
			// 
			this.SystemGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.SystemGroupBox.Controls.Add(this.SystemComboBox);
			this.SystemGroupBox.Location = new System.Drawing.Point(259, 28);
			this.SystemGroupBox.Name = "SystemGroupBox";
			this.SystemGroupBox.Size = new System.Drawing.Size(158, 50);
			this.SystemGroupBox.TabIndex = 6;
			this.SystemGroupBox.TabStop = false;
			this.SystemGroupBox.Text = "System";
			// 
			// SystemComboBox
			// 
			this.SystemComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.SystemComboBox.FormattingEnabled = true;
			this.SystemComboBox.Items.AddRange(new object[] {
            "NES",
						"SNES",
            "Gameboy",
            "SMS / GG / SG-1000",
            "Sega Genesis",
            "PC Engine / SuperGrafx",
            "TI-83"});
			this.SystemComboBox.Location = new System.Drawing.Point(6, 19);
			this.SystemComboBox.Name = "SystemComboBox";
			this.SystemComboBox.Size = new System.Drawing.Size(146, 21);
			this.SystemComboBox.TabIndex = 2;
			this.SystemComboBox.SelectedIndexChanged += new System.EventHandler(this.SystemComboBox_SelectedIndexChanged);
			// 
			// AllowLR
			// 
			this.AllowLR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AllowLR.AutoSize = true;
			this.AllowLR.Location = new System.Drawing.Point(259, 304);
			this.AllowLR.Name = "AllowLR";
			this.AllowLR.Size = new System.Drawing.Size(107, 17);
			this.AllowLR.TabIndex = 7;
			this.AllowLR.Text = "Allow L+R / U+D";
			this.AllowLR.UseVisualStyleBackColor = true;
			// 
			// AutoTab
			// 
			this.AutoTab.AutoSize = true;
			this.AutoTab.Location = new System.Drawing.Point(19, 5);
			this.AutoTab.Name = "AutoTab";
			this.AutoTab.Size = new System.Drawing.Size(70, 17);
			this.AutoTab.TabIndex = 8;
			this.AutoTab.Text = "Auto Tab";
			this.AutoTab.UseVisualStyleBackColor = true;
			this.AutoTab.CheckedChanged += new System.EventHandler(this.AutoTab_CheckedChanged);
			// 
			// label38
			// 
			this.label38.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label38.AutoSize = true;
			this.label38.Location = new System.Drawing.Point(9, 356);
			this.label38.Name = "label38";
			this.label38.Size = new System.Drawing.Size(153, 13);
			this.label38.TabIndex = 9;
			this.label38.Text = "* Escape clears a key mapping";
			// 
			// InputConfig
			// 
			this.AcceptButton = this.OK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.Cancel;
			this.ClientSize = new System.Drawing.Size(440, 378);
			this.Controls.Add(this.label38);
			this.Controls.Add(this.AutoTab);
			this.Controls.Add(this.AllowLR);
			this.Controls.Add(this.SystemGroupBox);
			this.Controls.Add(this.ControllerSelectGroupBox);
			this.Controls.Add(this.ControllerImage);
			this.Controls.Add(this.ButtonsGroupBox);
			this.Controls.Add(this.Cancel);
			this.Controls.Add(this.OK);
			this.DoubleBuffered = true;
#if WINDOWS
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
#endif
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(418, 202);
			this.Name = "InputConfig";
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
        private System.Windows.Forms.CheckBox IDX_CONTROLLERENABLED;
        private System.Windows.Forms.GroupBox SystemGroupBox;
        private System.Windows.Forms.ComboBox SystemComboBox;
        private System.Windows.Forms.CheckBox AllowLR;
		private System.Windows.Forms.CheckBox AutoTab;
		private System.Windows.Forms.Label label38;
    }
}