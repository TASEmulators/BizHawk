namespace BizHawk.Client.EmuHawk
{
	partial class PathConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PathConfig));
            this.Ok = new System.Windows.Forms.Button();
            this.Cancel = new System.Windows.Forms.Button();
            this.PathTabControl = new System.Windows.Forms.TabControl();
            this.SaveBtn = new System.Windows.Forms.Button();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SpecialCommandsBtn = new System.Windows.Forms.Button();
            this.RecentForROMs = new System.Windows.Forms.CheckBox();
            this.DefaultsBtn = new System.Windows.Forms.Button();
            this.tcMain = new System.Windows.Forms.TabControl();
            this.tpGlobal = new System.Windows.Forms.TabPage();
            this.tpSystems = new System.Windows.Forms.TabPage();
            this.lblSystem = new BizHawk.WinForms.Controls.LocLabelEx();
            this.comboSystem = new System.Windows.Forms.ComboBox();
            this.tcMain.SuspendLayout();
            this.tpSystems.SuspendLayout();
            this.SuspendLayout();
            // 
            // Ok
            // 
            resources.ApplyResources(this.Ok, "Ok");
            this.Ok.Name = "Ok";
            this.Ok.UseVisualStyleBackColor = true;
            this.Ok.Click += new System.EventHandler(this.Ok_Click);
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // PathTabControl
            // 
            resources.ApplyResources(this.PathTabControl, "PathTabControl");
            this.PathTabControl.Multiline = true;
            this.PathTabControl.Name = "PathTabControl";
            this.PathTabControl.SelectedIndex = 0;
            this.PathTabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            // 
            // SaveBtn
            // 
            resources.ApplyResources(this.SaveBtn, "SaveBtn");
            this.SaveBtn.Name = "SaveBtn";
            this.SaveBtn.UseVisualStyleBackColor = true;
            this.SaveBtn.Click += new System.EventHandler(this.SaveBtn_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // SpecialCommandsBtn
            // 
            resources.ApplyResources(this.SpecialCommandsBtn, "SpecialCommandsBtn");
            this.SpecialCommandsBtn.Name = "SpecialCommandsBtn";
            this.SpecialCommandsBtn.UseVisualStyleBackColor = true;
            this.SpecialCommandsBtn.Click += new System.EventHandler(this.SpecialCommandsBtn_Click);
            // 
            // RecentForROMs
            // 
            resources.ApplyResources(this.RecentForROMs, "RecentForROMs");
            this.RecentForROMs.Name = "RecentForROMs";
            this.RecentForROMs.UseVisualStyleBackColor = true;
            this.RecentForROMs.CheckedChanged += new System.EventHandler(this.RecentForRoms_CheckedChanged);
            // 
            // DefaultsBtn
            // 
            resources.ApplyResources(this.DefaultsBtn, "DefaultsBtn");
            this.DefaultsBtn.Name = "DefaultsBtn";
            this.DefaultsBtn.UseVisualStyleBackColor = true;
            this.DefaultsBtn.Click += new System.EventHandler(this.DefaultsBtn_Click);
            // 
            // tcMain
            // 
            resources.ApplyResources(this.tcMain, "tcMain");
            this.tcMain.Controls.Add(this.tpGlobal);
            this.tcMain.Controls.Add(this.tpSystems);
            this.tcMain.Name = "tcMain";
            this.tcMain.SelectedIndex = 0;
            // 
            // tpGlobal
            // 
            resources.ApplyResources(this.tpGlobal, "tpGlobal");
            this.tpGlobal.Name = "tpGlobal";
            this.tpGlobal.UseVisualStyleBackColor = true;
            // 
            // tpSystems
            // 
            resources.ApplyResources(this.tpSystems, "tpSystems");
            this.tpSystems.Controls.Add(this.lblSystem);
            this.tpSystems.Controls.Add(this.comboSystem);
            this.tpSystems.Controls.Add(this.PathTabControl);
            this.tpSystems.Name = "tpSystems";
            this.tpSystems.UseVisualStyleBackColor = true;
            // 
            // lblSystem
            // 
            resources.ApplyResources(this.lblSystem, "lblSystem");
            this.lblSystem.Name = "lblSystem";
            // 
            // comboSystem
            // 
            resources.ApplyResources(this.comboSystem, "comboSystem");
            this.comboSystem.FormattingEnabled = true;
            this.comboSystem.Name = "comboSystem";
            this.comboSystem.SelectedIndexChanged += new System.EventHandler(this.comboSystem_SelectedIndexChanged);
            // 
            // PathConfig
            // 
            this.AcceptButton = this.Ok;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.tcMain);
            this.Controls.Add(this.DefaultsBtn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SpecialCommandsBtn);
            this.Controls.Add(this.RecentForROMs);
            this.Controls.Add(this.SaveBtn);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.Ok);
            this.Name = "PathConfig";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.NewPathConfig_Load);
            this.tcMain.ResumeLayout(false);
            this.tpSystems.ResumeLayout(false);
            this.tpSystems.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button Ok;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.TabControl PathTabControl;
		private System.Windows.Forms.Button SaveBtn;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.Button SpecialCommandsBtn;
		private System.Windows.Forms.CheckBox RecentForROMs;
		private System.Windows.Forms.Button DefaultsBtn;
		private System.Windows.Forms.TabControl tcMain;
		private System.Windows.Forms.TabPage tpGlobal;
		private System.Windows.Forms.TabPage tpSystems;
		private System.Windows.Forms.ComboBox comboSystem;
		private WinForms.Controls.LocLabelEx lblSystem;
	}
}