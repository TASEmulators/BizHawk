namespace BizHawk.Client.EmuHawk
{
	partial class NesVsSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NesVsSettings));
            this.OkBtn = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.Dipswitch1CheckBox = new System.Windows.Forms.CheckBox();
            this.Dipswitch2CheckBox = new System.Windows.Forms.CheckBox();
            this.Dipswitch3CheckBox = new System.Windows.Forms.CheckBox();
            this.Dipswitch4CheckBox = new System.Windows.Forms.CheckBox();
            this.Dipswitch5CheckBox = new System.Windows.Forms.CheckBox();
            this.Dipswitch6CheckBox = new System.Windows.Forms.CheckBox();
            this.Dipswitch7CheckBox = new System.Windows.Forms.CheckBox();
            this.Dipswitch8CheckBox = new System.Windows.Forms.CheckBox();
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
            // Dipswitch1CheckBox
            // 
            resources.ApplyResources(this.Dipswitch1CheckBox, "Dipswitch1CheckBox");
            this.Dipswitch1CheckBox.Name = "Dipswitch1CheckBox";
            this.Dipswitch1CheckBox.UseVisualStyleBackColor = true;
            // 
            // Dipswitch2CheckBox
            // 
            resources.ApplyResources(this.Dipswitch2CheckBox, "Dipswitch2CheckBox");
            this.Dipswitch2CheckBox.Name = "Dipswitch2CheckBox";
            this.Dipswitch2CheckBox.UseVisualStyleBackColor = true;
            // 
            // Dipswitch3CheckBox
            // 
            resources.ApplyResources(this.Dipswitch3CheckBox, "Dipswitch3CheckBox");
            this.Dipswitch3CheckBox.Name = "Dipswitch3CheckBox";
            this.Dipswitch3CheckBox.UseVisualStyleBackColor = true;
            // 
            // Dipswitch4CheckBox
            // 
            resources.ApplyResources(this.Dipswitch4CheckBox, "Dipswitch4CheckBox");
            this.Dipswitch4CheckBox.Name = "Dipswitch4CheckBox";
            this.Dipswitch4CheckBox.UseVisualStyleBackColor = true;
            // 
            // Dipswitch5CheckBox
            // 
            resources.ApplyResources(this.Dipswitch5CheckBox, "Dipswitch5CheckBox");
            this.Dipswitch5CheckBox.Name = "Dipswitch5CheckBox";
            this.Dipswitch5CheckBox.UseVisualStyleBackColor = true;
            // 
            // Dipswitch6CheckBox
            // 
            resources.ApplyResources(this.Dipswitch6CheckBox, "Dipswitch6CheckBox");
            this.Dipswitch6CheckBox.Name = "Dipswitch6CheckBox";
            this.Dipswitch6CheckBox.UseVisualStyleBackColor = true;
            // 
            // Dipswitch7CheckBox
            // 
            resources.ApplyResources(this.Dipswitch7CheckBox, "Dipswitch7CheckBox");
            this.Dipswitch7CheckBox.Name = "Dipswitch7CheckBox";
            this.Dipswitch7CheckBox.UseVisualStyleBackColor = true;
            // 
            // Dipswitch8CheckBox
            // 
            resources.ApplyResources(this.Dipswitch8CheckBox, "Dipswitch8CheckBox");
            this.Dipswitch8CheckBox.Name = "Dipswitch8CheckBox";
            this.Dipswitch8CheckBox.UseVisualStyleBackColor = true;
            // 
            // NesVsSettings
            // 
            this.AcceptButton = this.OkBtn;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.CancelBtn;
            this.Controls.Add(this.Dipswitch8CheckBox);
            this.Controls.Add(this.Dipswitch7CheckBox);
            this.Controls.Add(this.Dipswitch6CheckBox);
            this.Controls.Add(this.Dipswitch5CheckBox);
            this.Controls.Add(this.Dipswitch4CheckBox);
            this.Controls.Add(this.Dipswitch3CheckBox);
            this.Controls.Add(this.Dipswitch2CheckBox);
            this.Controls.Add(this.Dipswitch1CheckBox);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.OkBtn);
            this.Name = "NesVsSettings";
            this.ShowIcon = false;
            this.Load += new System.EventHandler(this.NesVsSettings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkBtn;
		private System.Windows.Forms.Button CancelBtn;
		private System.Windows.Forms.CheckBox Dipswitch1CheckBox;
		private System.Windows.Forms.CheckBox Dipswitch2CheckBox;
		private System.Windows.Forms.CheckBox Dipswitch3CheckBox;
		private System.Windows.Forms.CheckBox Dipswitch4CheckBox;
		private System.Windows.Forms.CheckBox Dipswitch5CheckBox;
		private System.Windows.Forms.CheckBox Dipswitch6CheckBox;
		private System.Windows.Forms.CheckBox Dipswitch7CheckBox;
		private System.Windows.Forms.CheckBox Dipswitch8CheckBox;
	}
}