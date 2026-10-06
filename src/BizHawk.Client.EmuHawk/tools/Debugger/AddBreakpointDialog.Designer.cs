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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddBreakpointDialog));
            this.AddBtn = new System.Windows.Forms.Button();
            this.BreakpointTypeGroupbox = new System.Windows.Forms.GroupBox();
            this.ExecuteRadio = new System.Windows.Forms.RadioButton();
            this.WriteRadio = new System.Windows.Forms.RadioButton();
            this.ReadRadio = new System.Windows.Forms.RadioButton();
            this.label1 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.CancelBtn = new System.Windows.Forms.Button();
            this.AddressBox = new BizHawk.Client.EmuHawk.HexTextBox();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.AddressMaskBox = new BizHawk.Client.EmuHawk.HexTextBox();
            this.BreakpointTypeGroupbox.SuspendLayout();
            this.SuspendLayout();
            // 
            // AddBtn
            // 
            resources.ApplyResources(this.AddBtn, "AddBtn");
            this.AddBtn.Name = "AddBtn";
            this.AddBtn.UseVisualStyleBackColor = true;
            this.AddBtn.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // BreakpointTypeGroupbox
            // 
            resources.ApplyResources(this.BreakpointTypeGroupbox, "BreakpointTypeGroupbox");
            this.BreakpointTypeGroupbox.Controls.Add(this.ExecuteRadio);
            this.BreakpointTypeGroupbox.Controls.Add(this.WriteRadio);
            this.BreakpointTypeGroupbox.Controls.Add(this.ReadRadio);
            this.BreakpointTypeGroupbox.Name = "BreakpointTypeGroupbox";
            this.BreakpointTypeGroupbox.TabStop = false;
            // 
            // ExecuteRadio
            // 
            resources.ApplyResources(this.ExecuteRadio, "ExecuteRadio");
            this.ExecuteRadio.Name = "ExecuteRadio";
            this.ExecuteRadio.UseVisualStyleBackColor = true;
            // 
            // WriteRadio
            // 
            resources.ApplyResources(this.WriteRadio, "WriteRadio");
            this.WriteRadio.Name = "WriteRadio";
            this.WriteRadio.UseVisualStyleBackColor = true;
            // 
            // ReadRadio
            // 
            resources.ApplyResources(this.ReadRadio, "ReadRadio");
            this.ReadRadio.Checked = true;
            this.ReadRadio.Name = "ReadRadio";
            this.ReadRadio.TabStop = true;
            this.ReadRadio.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // CancelBtn
            // 
            resources.ApplyResources(this.CancelBtn, "CancelBtn");
            this.CancelBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.CancelBtn.Name = "CancelBtn";
            this.toolTip1.SetToolTip(this.CancelBtn, resources.GetString("CancelBtn.ToolTip"));
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // AddressBox
            // 
            resources.ApplyResources(this.AddressBox, "AddressBox");
            this.AddressBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.AddressBox.Name = "AddressBox";
            this.AddressBox.Nullable = false;
            this.toolTip1.SetToolTip(this.AddressBox, resources.GetString("AddressBox.ToolTip"));
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            this.toolTip1.SetToolTip(this.label2, resources.GetString("label2.ToolTip"));
            // 
            // AddressMaskBox
            // 
            resources.ApplyResources(this.AddressMaskBox, "AddressMaskBox");
            this.AddressMaskBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.AddressMaskBox.Name = "AddressMaskBox";
            this.AddressMaskBox.Nullable = false;
            this.toolTip1.SetToolTip(this.AddressMaskBox, resources.GetString("AddressMaskBox.ToolTip"));
            // 
            // AddBreakpointDialog
            // 
            this.AcceptButton = this.AddBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.AddressMaskBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.AddressBox);
            this.Controls.Add(this.BreakpointTypeGroupbox);
            this.Controls.Add(this.AddBtn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddBreakpointDialog";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.AddBreakpointDialog_Load);
            this.BreakpointTypeGroupbox.ResumeLayout(false);
            this.BreakpointTypeGroupbox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button AddBtn;
		private System.Windows.Forms.GroupBox BreakpointTypeGroupbox;
		private System.Windows.Forms.RadioButton ExecuteRadio;
		private System.Windows.Forms.RadioButton WriteRadio;
		private System.Windows.Forms.RadioButton ReadRadio;
		private HexTextBox AddressBox;
		private BizHawk.WinForms.Controls.LocLabelEx label1;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button CancelBtn;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private HexTextBox AddressMaskBox;
	}
}
