namespace BizHawk.Client.EmuHawk
{
	partial class AddBreakpointDialog
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
			this.AddButton = new System.Windows.Forms.Button();
			this.BreakpointTypeGroupbox = new System.Windows.Forms.GroupBox();
			this.ExecuteRadio = new System.Windows.Forms.RadioButton();
			this.WriteRadio = new System.Windows.Forms.RadioButton();
			this.ReadRadio = new System.Windows.Forms.RadioButton();
			this.AddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.BreakpointTypeGroupbox.SuspendLayout();
			this.SuspendLayout();
			// 
			// AddButton
			// 
			this.AddButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.AddButton.Location = new System.Drawing.Point(152, 92);
			this.AddButton.Name = "AddButton";
			this.AddButton.Size = new System.Drawing.Size(60, 23);
			this.AddButton.TabIndex = 100;
			this.AddButton.Text = "&Add";
			this.AddButton.UseVisualStyleBackColor = true;
			this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
			// 
			// BreakpointTypeGroupbox
			// 
			this.BreakpointTypeGroupbox.Controls.Add(this.ExecuteRadio);
			this.BreakpointTypeGroupbox.Controls.Add(this.WriteRadio);
			this.BreakpointTypeGroupbox.Controls.Add(this.ReadRadio);
			this.BreakpointTypeGroupbox.Location = new System.Drawing.Point(15, 31);
			this.BreakpointTypeGroupbox.Name = "BreakpointTypeGroupbox";
			this.BreakpointTypeGroupbox.Size = new System.Drawing.Size(196, 52);
			this.BreakpointTypeGroupbox.TabIndex = 1;
			this.BreakpointTypeGroupbox.TabStop = false;
			// 
			// ExecuteRadio
			// 
			this.ExecuteRadio.AutoSize = true;
			this.ExecuteRadio.Location = new System.Drawing.Point(119, 19);
			this.ExecuteRadio.Name = "ExecuteRadio";
			this.ExecuteRadio.Size = new System.Drawing.Size(64, 17);
			this.ExecuteRadio.TabIndex = 12;
			this.ExecuteRadio.Text = "Execute";
			this.ExecuteRadio.UseVisualStyleBackColor = true;
			// 
			// WriteRadio
			// 
			this.WriteRadio.AutoSize = true;
			this.WriteRadio.Location = new System.Drawing.Point(63, 19);
			this.WriteRadio.Name = "WriteRadio";
			this.WriteRadio.Size = new System.Drawing.Size(50, 17);
			this.WriteRadio.TabIndex = 11;
			this.WriteRadio.Text = "Write";
			this.WriteRadio.UseVisualStyleBackColor = true;
			// 
			// ReadRadio
			// 
			this.ReadRadio.AutoSize = true;
			this.ReadRadio.Checked = true;
			this.ReadRadio.Location = new System.Drawing.Point(6, 19);
			this.ReadRadio.Name = "ReadRadio";
			this.ReadRadio.Size = new System.Drawing.Size(51, 17);
			this.ReadRadio.TabIndex = 10;
			this.ReadRadio.TabStop = true;
			this.ReadRadio.Text = "Read";
			this.ReadRadio.UseVisualStyleBackColor = true;
			// 
			// AddressBox
			// 
			this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
			this.AddressBox.Location = new System.Drawing.Point(77, 5);
			this.AddressBox.Name = "AddressBox";
			this.AddressBox.Nullable = false;
			this.AddressBox.Size = new System.Drawing.Size(135, 20);
			this.AddressBox.TabIndex = 1;
			this.AddressBox.Text = "0";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(59, 13);
			this.label1.TabIndex = 3;
			this.label1.Text = "Address 0x";
			// 
			// AddBreakpointDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(224, 123);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.AddressBox);
			this.Controls.Add(this.BreakpointTypeGroupbox);
			this.Controls.Add(this.AddButton);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddBreakpointDialog";
			this.ShowIcon = false;
			this.Text = "Add Breakpoint";
			this.Load += new System.EventHandler(this.AddBreakpointDialog_Load);
			this.BreakpointTypeGroupbox.ResumeLayout(false);
			this.BreakpointTypeGroupbox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button AddButton;
		private System.Windows.Forms.GroupBox BreakpointTypeGroupbox;
		private System.Windows.Forms.RadioButton ExecuteRadio;
		private System.Windows.Forms.RadioButton WriteRadio;
		private System.Windows.Forms.RadioButton ReadRadio;
		private HexTextBox AddressBox;
		private System.Windows.Forms.Label label1;
	}
}