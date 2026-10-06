namespace BizHawk.Client.EmuHawk
{
	partial class OpenAdvancedChooser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OpenAdvancedChooser));
            this.label3 = new BizHawk.WinForms.Controls.LocSzLabelEx();
            this.label2 = new BizHawk.WinForms.Controls.LocLabelEx();
            this.btnLibretroLaunchNoGame = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtLibretroCore = new System.Windows.Forms.TextBox();
            this.btnLibretroLaunchGame = new System.Windows.Forms.Button();
            this.btnSetLibretroCore = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnClassicLaunchGame = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new BizHawk.WinForms.Controls.LocSzLabelEx();
            this.btnMAMELaunchGame = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // btnLibretroLaunchNoGame
            // 
            resources.ApplyResources(this.btnLibretroLaunchNoGame, "btnLibretroLaunchNoGame");
            this.btnLibretroLaunchNoGame.Name = "btnLibretroLaunchNoGame";
            this.btnLibretroLaunchNoGame.UseVisualStyleBackColor = true;
            this.btnLibretroLaunchNoGame.Click += new System.EventHandler(this.btnLibretroLaunchNoGame_Click);
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // groupBox2
            // 
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Controls.Add(this.txtLibretroCore);
            this.groupBox2.Controls.Add(this.btnLibretroLaunchGame);
            this.groupBox2.Controls.Add(this.btnSetLibretroCore);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.btnLibretroLaunchNoGame);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // txtLibretroCore
            // 
            resources.ApplyResources(this.txtLibretroCore, "txtLibretroCore");
            this.txtLibretroCore.AllowDrop = true;
            this.txtLibretroCore.Name = "txtLibretroCore";
            this.txtLibretroCore.ReadOnly = true;
            this.txtLibretroCore.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtLibretroCore_DragDrop);
            this.txtLibretroCore.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtLibretroCore_DragEnter);
            // 
            // btnLibretroLaunchGame
            // 
            resources.ApplyResources(this.btnLibretroLaunchGame, "btnLibretroLaunchGame");
            this.btnLibretroLaunchGame.Name = "btnLibretroLaunchGame";
            this.btnLibretroLaunchGame.UseVisualStyleBackColor = true;
            this.btnLibretroLaunchGame.Click += new System.EventHandler(this.btnLibretroLaunchGame_Click);
            // 
            // btnSetLibretroCore
            // 
            resources.ApplyResources(this.btnSetLibretroCore, "btnSetLibretroCore");
            this.btnSetLibretroCore.Name = "btnSetLibretroCore";
            this.btnSetLibretroCore.UseVisualStyleBackColor = true;
            this.btnSetLibretroCore.Click += new System.EventHandler(this.btnSetLibretroCore_Click);
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.btnClassicLaunchGame);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // btnClassicLaunchGame
            // 
            resources.ApplyResources(this.btnClassicLaunchGame, "btnClassicLaunchGame");
            this.btnClassicLaunchGame.Name = "btnClassicLaunchGame";
            this.btnClassicLaunchGame.UseVisualStyleBackColor = true;
            this.btnClassicLaunchGame.Click += new System.EventHandler(this.btnClassicLaunchGame_Click);
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.btnMAMELaunchGame);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            this.label1.Click += new System.EventHandler(this.btnMAMELaunchGame_Click);
            // 
            // btnMAMELaunchGame
            // 
            resources.ApplyResources(this.btnMAMELaunchGame, "btnMAMELaunchGame");
            this.btnMAMELaunchGame.Name = "btnMAMELaunchGame";
            this.btnMAMELaunchGame.UseVisualStyleBackColor = true;
            this.btnMAMELaunchGame.Click += new System.EventHandler(this.btnMAMELaunchGame_Click);
            // 
            // OpenAdvancedChooser
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OpenAdvancedChooser";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private BizHawk.WinForms.Controls.LocSzLabelEx label3;
		private BizHawk.WinForms.Controls.LocLabelEx label2;
		private System.Windows.Forms.Button btnLibretroLaunchNoGame;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Button btnSetLibretroCore;
		private System.Windows.Forms.TextBox txtLibretroCore;
		private System.Windows.Forms.Button btnLibretroLaunchGame;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Button btnClassicLaunchGame;
		private System.Windows.Forms.GroupBox groupBox1;
		private BizHawk.WinForms.Controls.LocSzLabelEx label1;
		private System.Windows.Forms.Button btnMAMELaunchGame;
	}
}