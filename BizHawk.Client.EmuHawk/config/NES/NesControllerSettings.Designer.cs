namespace BizHawk.Client.EmuHawk
{
	partial class NesControllerSettings
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NesControllerSettings));
			this.CancelBtn = new System.Windows.Forms.Button();
			this.OkBtn = new System.Windows.Forms.Button();
			this.checkBoxFamicom = new System.Windows.Forms.CheckBox();
			this.label5 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.comboBoxNESR = new System.Windows.Forms.ComboBox();
			this.comboBoxNESL = new System.Windows.Forms.ComboBox();
			this.comboBoxFamicom = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(300, 219);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 3;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(234, 219);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 2;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// checkBoxFamicom
			// 
			this.checkBoxFamicom.AutoSize = true;
			this.checkBoxFamicom.Location = new System.Drawing.Point(12, 21);
			this.checkBoxFamicom.Name = "checkBoxFamicom";
			this.checkBoxFamicom.Size = new System.Drawing.Size(68, 17);
			this.checkBoxFamicom.TabIndex = 4;
			this.checkBoxFamicom.Text = "Famicom";
			this.checkBoxFamicom.UseVisualStyleBackColor = true;
			this.checkBoxFamicom.CheckedChanged += new System.EventHandler(this.checkBoxFamicom_CheckedChanged);
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(6, 138);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(82, 13);
			this.label5.TabIndex = 12;
			this.label5.Text = "NES Right Port:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(9, 98);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(75, 13);
			this.label4.TabIndex = 11;
			this.label4.Text = "NES Left Port:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(9, 58);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(104, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "Famicom Expansion:";
			// 
			// comboBoxNESR
			// 
			this.comboBoxNESR.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxNESR.FormattingEnabled = true;
			this.comboBoxNESR.Location = new System.Drawing.Point(9, 154);
			this.comboBoxNESR.Name = "comboBoxNESR";
			this.comboBoxNESR.Size = new System.Drawing.Size(329, 21);
			this.comboBoxNESR.TabIndex = 9;
			// 
			// comboBoxNESL
			// 
			this.comboBoxNESL.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxNESL.FormattingEnabled = true;
			this.comboBoxNESL.Location = new System.Drawing.Point(9, 114);
			this.comboBoxNESL.Name = "comboBoxNESL";
			this.comboBoxNESL.Size = new System.Drawing.Size(329, 21);
			this.comboBoxNESL.TabIndex = 8;
			// 
			// comboBoxFamicom
			// 
			this.comboBoxFamicom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBoxFamicom.Enabled = false;
			this.comboBoxFamicom.FormattingEnabled = true;
			this.comboBoxFamicom.Location = new System.Drawing.Point(9, 74);
			this.comboBoxFamicom.Name = "comboBoxFamicom";
			this.comboBoxFamicom.Size = new System.Drawing.Size(329, 21);
			this.comboBoxFamicom.TabIndex = 7;
			// 
			// NesControllerSettings
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(372, 254);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.comboBoxNESR);
			this.Controls.Add(this.comboBoxNESL);
			this.Controls.Add(this.comboBoxFamicom);
			this.Controls.Add(this.checkBoxFamicom);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "NesControllerSettings";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "NES Controller Settings";
			this.Load += new System.EventHandler(this.NesControllerSettings_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.CheckBox checkBoxFamicom;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox comboBoxNESR;
		private System.Windows.Forms.ComboBox comboBoxNESL;
		private System.Windows.Forms.ComboBox comboBoxFamicom;
	}
}