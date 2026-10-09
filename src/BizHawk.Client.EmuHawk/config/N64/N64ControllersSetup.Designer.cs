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
            resources.ApplyResources(this.OkBtn, "OkBtn");
            this.OkBtn.Name = "OkBtn";
            this.OkBtn.UseVisualStyleBackColor = true;
            this.OkBtn.Click += new System.EventHandler(this.OkBtn_Click);
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // ControllerSetting4
            // 
            resources.ApplyResources(this.ControllerSetting4, "ControllerSetting4");
            this.ControllerSetting4.ControllerNumber = 4;
            this.ControllerSetting4.IsConnected = false;
            this.ControllerSetting4.Name = "ControllerSetting4";
            // 
            // ControllerSetting3
            // 
            resources.ApplyResources(this.ControllerSetting3, "ControllerSetting3");
            this.ControllerSetting3.ControllerNumber = 3;
            this.ControllerSetting3.IsConnected = false;
            this.ControllerSetting3.Name = "ControllerSetting3";
            // 
            // ControllerSetting2
            // 
            resources.ApplyResources(this.ControllerSetting2, "ControllerSetting2");
            this.ControllerSetting2.ControllerNumber = 2;
            this.ControllerSetting2.IsConnected = false;
            this.ControllerSetting2.Name = "ControllerSetting2";
            // 
            // ControllerSetting1
            // 
            resources.ApplyResources(this.ControllerSetting1, "ControllerSetting1");
            this.ControllerSetting1.ControllerNumber = 1;
            this.ControllerSetting1.IsConnected = false;
            this.ControllerSetting1.Name = "ControllerSetting1";
            // 
            // N64ControllersSetup
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.ControllerSetting4);
            this.Controls.Add(this.ControllerSetting3);
            this.Controls.Add(this.ControllerSetting2);
            this.Controls.Add(this.ControllerSetting1);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "N64ControllersSetup";
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