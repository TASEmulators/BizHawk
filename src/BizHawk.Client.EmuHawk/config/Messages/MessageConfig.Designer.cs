namespace BizHawk.Client.EmuHawk
{
	partial class MessageConfig
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MessageConfig));
            this.OK = new System.Windows.Forms.Button();
            this.MessageTypeBox = new BizHawk.WinForms.Controls.LocSzGroupBoxEx();
            this.ColorBox = new BizHawk.WinForms.Controls.LocSzGroupBoxEx();
            this.Cancel = new System.Windows.Forms.Button();
            this.ResetDefaultsButton = new System.Windows.Forms.Button();
            this.StackMessagesCheckbox = new System.Windows.Forms.CheckBox();
            this.MessageEditor = new BizHawk.Client.EmuHawk.MessageEdit();
            this.SuspendLayout();
            // 
            // OK
            // 
            resources.ApplyResources(this.OK, "OK");
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.Name = "OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // MessageTypeBox
            // 
            resources.ApplyResources(this.MessageTypeBox, "MessageTypeBox");
            this.MessageTypeBox.Name = "MessageTypeBox";
            // 
            // ColorBox
            // 
            resources.ApplyResources(this.ColorBox, "ColorBox");
            this.ColorBox.Name = "ColorBox";
            // 
            // Cancel
            // 
            resources.ApplyResources(this.Cancel, "Cancel");
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Name = "Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ResetDefaultsButton
            // 
            resources.ApplyResources(this.ResetDefaultsButton, "ResetDefaultsButton");
            this.ResetDefaultsButton.Name = "ResetDefaultsButton";
            this.ResetDefaultsButton.UseVisualStyleBackColor = true;
            this.ResetDefaultsButton.Click += new System.EventHandler(this.ResetDefaultsButton_Click);
            // 
            // StackMessagesCheckbox
            // 
            resources.ApplyResources(this.StackMessagesCheckbox, "StackMessagesCheckbox");
            this.StackMessagesCheckbox.Name = "StackMessagesCheckbox";
            this.StackMessagesCheckbox.UseVisualStyleBackColor = true;
            // 
            // MessageEditor
            // 
            resources.ApplyResources(this.MessageEditor, "MessageEditor");
            this.MessageEditor.Name = "MessageEditor";
            // 
            // MessageConfig
            // 
            this.AcceptButton = this.OK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.Controls.Add(this.MessageEditor);
            this.Controls.Add(this.StackMessagesCheckbox);
            this.Controls.Add(this.ResetDefaultsButton);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.ColorBox);
            this.Controls.Add(this.MessageTypeBox);
            this.Controls.Add(this.OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "MessageConfig";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.MessageConfig_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private BizHawk.WinForms.Controls.LocSzGroupBoxEx MessageTypeBox;
		private BizHawk.WinForms.Controls.LocSzGroupBoxEx ColorBox;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button ResetDefaultsButton;
		private System.Windows.Forms.CheckBox StackMessagesCheckbox;
		private MessageEdit MessageEditor;
	}
}