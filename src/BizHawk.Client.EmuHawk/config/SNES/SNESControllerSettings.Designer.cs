namespace BizHawk.Client.EmuHawk
{
	partial class SNESControllerSettings
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
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label5 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.label4 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.Port2ComboBox = new System.Windows.Forms.ComboBox();
            this.Port1ComboBox = new System.Windows.Forms.ComboBox();
            this.MouseSpeedLabel1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.LimitAnalogChangeCheckBox = new System.Windows.Forms.CheckBox();
            this.MouseSpeedLabel2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MouseSpeedLabel3 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MouseNagLabel1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.MouseNagLabel2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SuspendLayout();
            // 
            // OkBtn
            // 
            this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkBtn.Location = new System.Drawing.Point(170, 264);
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.Size = new System.Drawing.Size(60, 23);
            this.OkBtn.TabIndex = 4;
            this.OkBtn.Text = "&OK";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Location = new System.Drawing.Point(236, 264);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(60, 23);
            this.CancelBtn.TabIndex = 5;
            this.CancelBtn.Text = "&Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Text = "SNES Controller Settings";
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(9, 88);
            this.label5.Name = "label5";
            this.label5.Text = "Port 2:";
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(12, 38);
            this.label4.Name = "label4";
            this.label4.Text = "Port 1:";
            // 
            // Port2ComboBox
            // 
            this.Port2ComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Port2ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Port2ComboBox.FormattingEnabled = true;
            this.Port2ComboBox.Location = new System.Drawing.Point(12, 104);
            this.Port2ComboBox.Name = "Port2ComboBox";
            this.Port2ComboBox.Size = new System.Drawing.Size(284, 21);
            this.Port2ComboBox.TabIndex = 20;
            this.Port2ComboBox.SelectedIndexChanged += new System.EventHandler(this.PortComboBox_SelectedIndexChanged);
            // 
            // Port1ComboBox
            // 
            this.Port1ComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Port1ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Port1ComboBox.FormattingEnabled = true;
            this.Port1ComboBox.Location = new System.Drawing.Point(12, 54);
            this.Port1ComboBox.Name = "Port1ComboBox";
            this.Port1ComboBox.Size = new System.Drawing.Size(284, 21);
            this.Port1ComboBox.TabIndex = 19;
            this.Port1ComboBox.SelectedIndexChanged += new System.EventHandler(this.PortComboBox_SelectedIndexChanged);
            // 
            // MouseSpeedLabel1
            // 
            this.MouseSpeedLabel1.Location = new System.Drawing.Point(12, 195);
            this.MouseSpeedLabel1.Name = "MouseSpeedLabel1";
            this.MouseSpeedLabel1.Text = "For casual play this should be checked";
            // 
            // LimitAnalogChangeCheckBox
            // 
            this.LimitAnalogChangeCheckBox.AutoSize = true;
            this.LimitAnalogChangeCheckBox.Location = new System.Drawing.Point(15, 175);
            this.LimitAnalogChangeCheckBox.Name = "LimitAnalogChangeCheckBox";
            this.LimitAnalogChangeCheckBox.Size = new System.Drawing.Size(173, 17);
            this.LimitAnalogChangeCheckBox.TabIndex = 24;
            this.LimitAnalogChangeCheckBox.Text = "Limit Analog Change Sensitivity";
            this.LimitAnalogChangeCheckBox.UseVisualStyleBackColor = true;
            // 
            // MouseSpeedLabel2
            // 
            this.MouseSpeedLabel2.Location = new System.Drawing.Point(12, 208);
            this.MouseSpeedLabel2.Name = "MouseSpeedLabel2";
            this.MouseSpeedLabel2.Text = "The full range of values are rather unusuable in";
            // 
            // MouseSpeedLabel3
            // 
            this.MouseSpeedLabel3.Location = new System.Drawing.Point(12, 221);
            this.MouseSpeedLabel3.Name = "MouseSpeedLabel3";
            this.MouseSpeedLabel3.Text = "normal situations, but good if you need total control";
            // 
            // MouseNagLabel1
            // 
            this.MouseNagLabel1.Location = new System.Drawing.Point(12, 135);
            this.MouseNagLabel1.Name = "MouseNagLabel1";
            this.MouseNagLabel1.Text = "*Note: mouse and scope controls should be bound to an";
            // 
            // MouseNagLabel2
            // 
            this.MouseNagLabel2.Location = new System.Drawing.Point(45, 148);
            this.MouseNagLabel2.Name = "MouseNagLabel2";
            this.MouseNagLabel2.Text = "analog stick not the mouse";
            // 
            // SNESControllerSettings
            // 
            this.AcceptButton = this.OkBtn;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.ClientSize = new System.Drawing.Size(308, 299);
            this.Controls.Add(this.MouseNagLabel2);
            this.Controls.Add(this.MouseNagLabel1);
            this.Controls.Add(this.MouseSpeedLabel3);
            this.Controls.Add(this.MouseSpeedLabel2);
            this.Controls.Add(this.LimitAnalogChangeCheckBox);
            this.Controls.Add(this.MouseSpeedLabel1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.Port2ComboBox);
            this.Controls.Add(this.Port1ComboBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Name = "SNESControllerSettings";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Controller Settings";
            this.Load += new System.EventHandler(this.SNESControllerSettings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private BizHawk.WinForms.Controls.LocLabelEx label5;
		private BizHawk.WinForms.Controls.LocLabelEx label4;
		private System.Windows.Forms.ComboBox Port2ComboBox;
		private System.Windows.Forms.ComboBox Port1ComboBox;
		private BizHawk.WinForms.Controls.LocLabelEx MouseSpeedLabel1;
		private System.Windows.Forms.CheckBox LimitAnalogChangeCheckBox;
		private BizHawk.WinForms.Controls.LocLabelEx MouseSpeedLabel2;
		private BizHawk.WinForms.Controls.LocLabelEx MouseSpeedLabel3;
		private BizHawk.WinForms.Controls.LocLabelEx MouseNagLabel1;
		private BizHawk.WinForms.Controls.LocLabelEx MouseNagLabel2;
	}
}