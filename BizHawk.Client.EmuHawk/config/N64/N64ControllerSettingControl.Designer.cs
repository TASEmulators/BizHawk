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
			this.EnabledCheckbox = new System.Windows.Forms.CheckBox();
			this.PakTypeDropdown = new System.Windows.Forms.ComboBox();
			this.ControllerNameLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// EnabledCheckbox
			// 
			this.EnabledCheckbox.AutoSize = true;
			this.EnabledCheckbox.Location = new System.Drawing.Point(80, 2);
			this.EnabledCheckbox.Name = "EnabledCheckbox";
			this.EnabledCheckbox.Size = new System.Drawing.Size(78, 17);
			this.EnabledCheckbox.TabIndex = 0;
			this.EnabledCheckbox.Text = "Connected";
			this.EnabledCheckbox.UseVisualStyleBackColor = true;
			this.EnabledCheckbox.CheckedChanged += new System.EventHandler(this.EnabledCheckbox_CheckedChanged);
			// 
			// PakTypeDropdown
			// 
			this.PakTypeDropdown.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.PakTypeDropdown.FormattingEnabled = true;
			this.PakTypeDropdown.Items.AddRange(new object[] {
            "None",
            "Memory Card",
            "Rumble Pak",
            "Transfer Pak"});
			this.PakTypeDropdown.Location = new System.Drawing.Point(160, 0);
			this.PakTypeDropdown.Name = "PakTypeDropdown";
			this.PakTypeDropdown.Size = new System.Drawing.Size(121, 21);
			this.PakTypeDropdown.TabIndex = 1;
			// 
			// ControllerNameLabel
			// 
			this.ControllerNameLabel.AutoSize = true;
			this.ControllerNameLabel.Location = new System.Drawing.Point(3, 4);
			this.ControllerNameLabel.Name = "ControllerNameLabel";
			this.ControllerNameLabel.Size = new System.Drawing.Size(60, 13);
			this.ControllerNameLabel.TabIndex = 2;
			this.ControllerNameLabel.Text = "Controller 1";
			// 
			// N64ControllerSettingControl
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.Controls.Add(this.ControllerNameLabel);
			this.Controls.Add(this.PakTypeDropdown);
			this.Controls.Add(this.EnabledCheckbox);
			this.Name = "N64ControllerSettingControl";
			this.Size = new System.Drawing.Size(290, 22);
			this.Load += new System.EventHandler(this.N64ControllerSettingControl_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox EnabledCheckbox;
		private System.Windows.Forms.ComboBox PakTypeDropdown;
		private System.Windows.Forms.Label ControllerNameLabel;
	}
}
