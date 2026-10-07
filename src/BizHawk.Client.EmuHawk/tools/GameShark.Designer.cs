namespace BizHawk.Client.EmuHawk
{
	partial class GameShark
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameShark));
            this.mnuGameShark = new System.Windows.Forms.MenuStrip();
            this.btnClear = new System.Windows.Forms.Button();
            this.lblCheat = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtCheat = new System.Windows.Forms.TextBox();
            this.btnGo = new System.Windows.Forms.Button();
            this.lblDescription = new BizHawk.WinForms.Controls.LocLabelEx();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // mnuGameShark
            // 
            resources.ApplyResources(this.mnuGameShark, "mnuGameShark");
            this.mnuGameShark.Name = "mnuGameShark";
            // 
            // btnClear
            // 
            resources.ApplyResources(this.btnClear, "btnClear");
            this.btnClear.Name = "btnClear";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.BtnClear_Click);
            // 
            // lblCheat
            // 
            resources.ApplyResources(this.lblCheat, "lblCheat");
            this.lblCheat.Name = "lblCheat";
            // 
            // txtCheat
            // 
            resources.ApplyResources(this.txtCheat, "txtCheat");
            this.txtCheat.Name = "txtCheat";
            // 
            // btnGo
            // 
            resources.ApplyResources(this.btnGo, "btnGo");
            this.btnGo.Name = "btnGo";
            this.btnGo.UseVisualStyleBackColor = true;
            this.btnGo.Click += new System.EventHandler(this.Go_Click);
            // 
            // lblDescription
            // 
            resources.ApplyResources(this.lblDescription, "lblDescription");
            this.lblDescription.Name = "lblDescription";
            // 
            // txtDescription
            // 
            resources.ApplyResources(this.txtDescription, "txtDescription");
            this.txtDescription.Name = "txtDescription";
            // 
            // GameShark
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.txtDescription);
            this.Controls.Add(this.lblDescription);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.lblCheat);
            this.Controls.Add(this.txtCheat);
            this.Controls.Add(this.btnGo);
            this.Controls.Add(this.mnuGameShark);
            this.MainMenuStrip = this.mnuGameShark;
            this.MaximizeBox = false;
            this.Name = "GameShark";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip mnuGameShark;
		internal System.Windows.Forms.Button btnClear;
		internal BizHawk.WinForms.Controls.LocLabelEx lblCheat;
		internal System.Windows.Forms.TextBox txtCheat;
		internal System.Windows.Forms.Button btnGo;
		private BizHawk.WinForms.Controls.LocLabelEx lblDescription;
		private System.Windows.Forms.TextBox txtDescription;
	}
}