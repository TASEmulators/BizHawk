namespace BizHawk.Client.EmuHawk
{
	partial class LuaBox
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
			this.LuaGroupBox = new System.Windows.Forms.GroupBox();
			this.RunLuaFunctionButton = new System.Windows.Forms.Button();
			this.LuaAutoFunctionCheckbox = new System.Windows.Forms.CheckBox();
			this.LuaGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// LuaGroupBox
			// 
			this.LuaGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.LuaGroupBox.Controls.Add(this.LuaAutoFunctionCheckbox);
			this.LuaGroupBox.Controls.Add(this.RunLuaFunctionButton);
			this.LuaGroupBox.Location = new System.Drawing.Point(3, 3);
			this.LuaGroupBox.Name = "LuaGroupBox";
			this.LuaGroupBox.Size = new System.Drawing.Size(198, 52);
			this.LuaGroupBox.TabIndex = 0;
			this.LuaGroupBox.TabStop = false;
			this.LuaGroupBox.Text = "Lua";
			// 
			// RunLuaFunctionButton
			// 
			this.RunLuaFunctionButton.Location = new System.Drawing.Point(6, 19);
			this.RunLuaFunctionButton.Name = "RunLuaFunctionButton";
			this.RunLuaFunctionButton.Size = new System.Drawing.Size(86, 23);
			this.RunLuaFunctionButton.TabIndex = 0;
			this.RunLuaFunctionButton.Text = "Run function";
			this.RunLuaFunctionButton.UseVisualStyleBackColor = true;
			// 
			// LuaAutoFunctionCheckbox
			// 
			this.LuaAutoFunctionCheckbox.AutoSize = true;
			this.LuaAutoFunctionCheckbox.Location = new System.Drawing.Point(98, 23);
			this.LuaAutoFunctionCheckbox.Name = "LuaAutoFunctionCheckbox";
			this.LuaAutoFunctionCheckbox.Size = new System.Drawing.Size(89, 17);
			this.LuaAutoFunctionCheckbox.TabIndex = 1;
			this.LuaAutoFunctionCheckbox.Text = "Auto function";
			this.LuaAutoFunctionCheckbox.UseVisualStyleBackColor = true;
			// 
			// LuaBox
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.LuaGroupBox);
			this.Name = "LuaBox";
			this.Size = new System.Drawing.Size(204, 58);
			this.LuaGroupBox.ResumeLayout(false);
			this.LuaGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox LuaGroupBox;
		private System.Windows.Forms.Button RunLuaFunctionButton;
		private System.Windows.Forms.CheckBox LuaAutoFunctionCheckbox;
	}
}
