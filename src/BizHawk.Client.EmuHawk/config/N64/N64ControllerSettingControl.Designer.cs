namespace BizHawk.Client.EmuHawk
{
	partial class N64ControllerSettingControl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(N64ControllerSettingControl));
            this.EnabledCheckbox = new System.Windows.Forms.CheckBox();
            this.PakTypeDropdown = new System.Windows.Forms.ComboBox();
            this.ControllerNameLabel = new BizHawk.WinForms.Controls.LocLabelEx();
            this.SuspendLayout();
            // 
            // EnabledCheckbox
            // 
            resources.ApplyResources(this.EnabledCheckbox, "EnabledCheckbox");
            this.EnabledCheckbox.Name = "EnabledCheckbox";
            this.EnabledCheckbox.UseVisualStyleBackColor = true;
            this.EnabledCheckbox.CheckedChanged += new System.EventHandler(this.EnabledCheckbox_CheckedChanged);
            // 
            // PakTypeDropdown
            // 
            resources.ApplyResources(this.PakTypeDropdown, "PakTypeDropdown");
            this.PakTypeDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PakTypeDropdown.FormattingEnabled = true;
            this.PakTypeDropdown.Items.AddRange(new object[] {
            resources.GetString("PakTypeDropdown.Items"),
            resources.GetString("PakTypeDropdown.Items1"),
            resources.GetString("PakTypeDropdown.Items2"),
            resources.GetString("PakTypeDropdown.Items3")});
            this.PakTypeDropdown.Name = "PakTypeDropdown";
            // 
            // ControllerNameLabel
            // 
            resources.ApplyResources(this.ControllerNameLabel, "ControllerNameLabel");
            this.ControllerNameLabel.Name = "ControllerNameLabel";
            // 
            // N64ControllerSettingControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.ControllerNameLabel);
            this.Controls.Add(this.PakTypeDropdown);
            this.Controls.Add(this.EnabledCheckbox);
            this.Name = "N64ControllerSettingControl";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox EnabledCheckbox;
		private System.Windows.Forms.ComboBox PakTypeDropdown;
		private BizHawk.WinForms.Controls.LocLabelEx ControllerNameLabel;
	}
}
