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
            this.OK = new System.Windows.Forms.Button();
            this.MessageTypeBox = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.MessageColorDialog = new System.Windows.Forms.ColorDialog();
            this.Cancel = new System.Windows.Forms.Button();
            this.ResetDefaultsButton = new System.Windows.Forms.Button();
            this.AlertColorDialog = new System.Windows.Forms.ColorDialog();
            this.LInputColorDialog = new System.Windows.Forms.ColorDialog();
            this.MovieInputColorDialog = new System.Windows.Forms.ColorDialog();
            this.StackMessagesCheckbox = new System.Windows.Forms.CheckBox();
            this.MessageEditor = new BizHawk.Client.EmuHawk.MessageEdit();
            this.SuspendLayout();
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OK.Location = new System.Drawing.Point(348, 418);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(75, 23);
            this.OK.TabIndex = 1;
            this.OK.Text = "&OK";
            this.OK.UseVisualStyleBackColor = true;
            this.OK.Click += new System.EventHandler(this.Ok_Click);
            // 
            // MessageTypeBox
            // 
            this.MessageTypeBox.Location = new System.Drawing.Point(12, 12);
            this.MessageTypeBox.Name = "MessageTypeBox";
            this.MessageTypeBox.Size = new System.Drawing.Size(177, 211);
            this.MessageTypeBox.TabIndex = 2;
            this.MessageTypeBox.TabStop = false;
            this.MessageTypeBox.Text = "Message Type";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox2.Location = new System.Drawing.Point(12, 231);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(177, 210);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Message Colors";
            // 
            // MessageColorDialog
            // 
            this.MessageColorDialog.FullOpen = true;
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(429, 418);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(75, 23);
            this.Cancel.TabIndex = 5;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = true;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Click);
            // 
            // ResetDefaultsButton
            // 
            this.ResetDefaultsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ResetDefaultsButton.Location = new System.Drawing.Point(195, 418);
            this.ResetDefaultsButton.Name = "ResetDefaultsButton";
            this.ResetDefaultsButton.Size = new System.Drawing.Size(96, 23);
            this.ResetDefaultsButton.TabIndex = 6;
            this.ResetDefaultsButton.Text = "Restore Defaults";
            this.ResetDefaultsButton.UseVisualStyleBackColor = true;
            this.ResetDefaultsButton.Click += new System.EventHandler(this.ResetDefaultsButton_Click);
            // 
            // AlertColorDialog
            // 
            this.AlertColorDialog.FullOpen = true;
            // 
            // LInputColorDialog
            // 
            this.LInputColorDialog.FullOpen = true;
            // 
            // MovieInputColorDialog
            // 
            this.MovieInputColorDialog.FullOpen = true;
            // 
            // StackMessagesCheckbox
            // 
            this.StackMessagesCheckbox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.StackMessagesCheckbox.AutoSize = true;
            this.StackMessagesCheckbox.Location = new System.Drawing.Point(195, 388);
            this.StackMessagesCheckbox.Name = "StackMessagesCheckbox";
            this.StackMessagesCheckbox.Size = new System.Drawing.Size(105, 17);
            this.StackMessagesCheckbox.TabIndex = 7;
            this.StackMessagesCheckbox.Text = "Stack Messages";
            this.StackMessagesCheckbox.UseVisualStyleBackColor = true;
            // 
            // MessageEditor
            // 
            this.MessageEditor.Location = new System.Drawing.Point(195, 12);
            this.MessageEditor.Name = "MessageEditor";
            this.MessageEditor.Size = new System.Drawing.Size(310, 256);
            this.MessageEditor.TabIndex = 8;
            // 
            // MessageConfig
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(512, 446);
            this.Controls.Add(this.MessageEditor);
            this.Controls.Add(this.StackMessagesCheckbox);
            this.Controls.Add(this.ResetDefaultsButton);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.MessageTypeBox);
            this.Controls.Add(this.OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimumSize = new System.Drawing.Size(404, 375);
            this.Name = "MessageConfig";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configure On Screen Messages";
            this.Load += new System.EventHandler(this.MessageConfig_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OK;
		private System.Windows.Forms.GroupBox MessageTypeBox;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.ColorDialog MessageColorDialog;
		private System.Windows.Forms.Button Cancel;
		private System.Windows.Forms.Button ResetDefaultsButton;
		private System.Windows.Forms.ColorDialog AlertColorDialog;
		private System.Windows.Forms.ColorDialog LInputColorDialog;
		private System.Windows.Forms.ColorDialog MovieInputColorDialog;
		private System.Windows.Forms.CheckBox StackMessagesCheckbox;
		private MessageEdit MessageEditor;
	}
}