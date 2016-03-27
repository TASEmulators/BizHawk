namespace BizHawk.Client.EmuHawk
{
	partial class N64ControllersSetup
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(N64ControllersSetup));
			this.OkBtn = new System.Windows.Forms.Button();
			this.CancelBtn = new System.Windows.Forms.Button();
			this.ControllerSetting4 = new BizHawk.Client.EmuHawk.N64ControllerSettingControl();
			this.ControllerSetting3 = new BizHawk.Client.EmuHawk.N64ControllerSettingControl();
			this.ControllerSetting2 = new BizHawk.Client.EmuHawk.N64ControllerSettingControl();
			this.ControllerSetting1 = new BizHawk.Client.EmuHawk.N64ControllerSettingControl();
			this.SuspendLayout();
			// 
			// OkBtn
			// 
			this.OkBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkBtn.Location = new System.Drawing.Point(169, 145);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(60, 23);
			this.OkBtn.TabIndex = 0;
			this.OkBtn.Text = "&OK";
			this.OkBtn.UseVisualStyleBackColor = true;
			this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
			// 
			// CancelBtn
			// 
			this.CancelBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.CancelBtn.Location = new System.Drawing.Point(235, 145);
			this.CancelBtn.Name = "CancelBtn";
			this.CancelBtn.Size = new System.Drawing.Size(60, 23);
			this.CancelBtn.TabIndex = 1;
			this.CancelBtn.Text = "&Cancel";
			this.CancelBtn.UseVisualStyleBackColor = true;
			this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
			// 
			// ControllerSetting4
			// 
			this.ControllerSetting4.ControllerNumber = 4;
			this.ControllerSetting4.IsConnected = false;
			this.ControllerSetting4.Location = new System.Drawing.Point(12, 114);
			this.ControllerSetting4.Name = "ControllerSetting4";
			this.ControllerSetting4.PakType = BizHawk.Emulation.Cores.Nintendo.N64.N64SyncSettings.N64ControllerSettings.N64ControllerPakType.NO_PAK;
			this.ControllerSetting4.Size = new System.Drawing.Size(291, 28);
			this.ControllerSetting4.TabIndex = 5;
			// 
			// ControllerSetting3
			// 
			this.ControllerSetting3.ControllerNumber = 3;
			this.ControllerSetting3.IsConnected = false;
			this.ControllerSetting3.Location = new System.Drawing.Point(12, 80);
			this.ControllerSetting3.Name = "ControllerSetting3";
			this.ControllerSetting3.PakType = BizHawk.Emulation.Cores.Nintendo.N64.N64SyncSettings.N64ControllerSettings.N64ControllerPakType.NO_PAK;
			this.ControllerSetting3.Size = new System.Drawing.Size(291, 28);
			this.ControllerSetting3.TabIndex = 4;
			// 
			// ControllerSetting2
			// 
			this.ControllerSetting2.ControllerNumber = 2;
			this.ControllerSetting2.IsConnected = false;
			this.ControllerSetting2.Location = new System.Drawing.Point(12, 46);
			this.ControllerSetting2.Name = "ControllerSetting2";
			this.ControllerSetting2.PakType = BizHawk.Emulation.Cores.Nintendo.N64.N64SyncSettings.N64ControllerSettings.N64ControllerPakType.NO_PAK;
			this.ControllerSetting2.Size = new System.Drawing.Size(291, 28);
			this.ControllerSetting2.TabIndex = 3;
			// 
			// ControllerSetting1
			// 
			this.ControllerSetting1.ControllerNumber = 1;
			this.ControllerSetting1.IsConnected = false;
			this.ControllerSetting1.Location = new System.Drawing.Point(12, 12);
			this.ControllerSetting1.Name = "ControllerSetting1";
			this.ControllerSetting1.PakType = BizHawk.Emulation.Cores.Nintendo.N64.N64SyncSettings.N64ControllerSettings.N64ControllerPakType.NO_PAK;
			this.ControllerSetting1.Size = new System.Drawing.Size(291, 28);
			this.ControllerSetting1.TabIndex = 2;
			// 
			// N64ControllersSetup
			// 
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.CancelBtn;
			this.ClientSize = new System.Drawing.Size(307, 180);
			this.Controls.Add(this.ControllerSetting4);
			this.Controls.Add(this.ControllerSetting3);
			this.Controls.Add(this.ControllerSetting2);
			this.Controls.Add(this.ControllerSetting1);
			this.Controls.Add(this.CancelBtn);
			this.Controls.Add(this.OkBtn);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "N64ControllersSetup";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Controller Settings";
			this.Load += new System.EventHandler(this.N64ControllersSetup_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private N64ControllerSettingControl ControllerSetting1;
		private N64ControllerSettingControl ControllerSetting2;
		private N64ControllerSettingControl ControllerSetting3;
		private N64ControllerSettingControl ControllerSetting4;
	}
}