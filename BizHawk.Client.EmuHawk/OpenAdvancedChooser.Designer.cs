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
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnLibretroLaunchNoGame = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.txtLibretroCore = new System.Windows.Forms.TextBox();
            this.btnLibretroLaunchGame = new System.Windows.Forms.Button();
            this.btnSetLibretroCore = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btnClassicLaunchGame = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(6, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(250, 29);
            this.label3.TabIndex = 5;
            this.label3.Text = "Load a rom with the classic BizHawk autodetection method. But why not just use Op" +
    "en Rom?";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 26);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Current Core:";
            // 
            // btnLibretroLaunchNoGame
            // 
            this.btnLibretroLaunchNoGame.Location = new System.Drawing.Point(217, 50);
            this.btnLibretroLaunchNoGame.Name = "btnLibretroLaunchNoGame";
            this.btnLibretroLaunchNoGame.Size = new System.Drawing.Size(102, 23);
            this.btnLibretroLaunchNoGame.TabIndex = 1;
            this.btnLibretroLaunchNoGame.Text = "Launch No Game";
            this.btnLibretroLaunchNoGame.UseVisualStyleBackColor = true;
            this.btnLibretroLaunchNoGame.Click += new System.EventHandler(this.btnLibretroLaunchNoGame_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(370, 176);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.txtLibretroCore);
            this.groupBox2.Controls.Add(this.btnLibretroLaunchGame);
            this.groupBox2.Controls.Add(this.btnSetLibretroCore);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.btnLibretroLaunchNoGame);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(433, 81);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Libretro";
            // 
            // txtLibretroCore
            // 
            this.txtLibretroCore.Location = new System.Drawing.Point(81, 23);
            this.txtLibretroCore.Name = "txtLibretroCore";
            this.txtLibretroCore.ReadOnly = true;
            this.txtLibretroCore.Size = new System.Drawing.Size(314, 20);
            this.txtLibretroCore.TabIndex = 6;
            // 
            // btnLibretroLaunchGame
            // 
            this.btnLibretroLaunchGame.Location = new System.Drawing.Point(325, 50);
            this.btnLibretroLaunchGame.Name = "btnLibretroLaunchGame";
            this.btnLibretroLaunchGame.Size = new System.Drawing.Size(102, 23);
            this.btnLibretroLaunchGame.TabIndex = 5;
            this.btnLibretroLaunchGame.Text = "Launch Game";
            this.btnLibretroLaunchGame.UseVisualStyleBackColor = true;
            this.btnLibretroLaunchGame.Click += new System.EventHandler(this.btnLibretroLaunchGame_Click);
            // 
            // btnSetLibretroCore
            // 
            this.btnSetLibretroCore.AutoSize = true;
            this.btnSetLibretroCore.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnSetLibretroCore.Location = new System.Drawing.Point(401, 21);
            this.btnSetLibretroCore.Name = "btnSetLibretroCore";
            this.btnSetLibretroCore.Size = new System.Drawing.Size(26, 23);
            this.btnSetLibretroCore.TabIndex = 4;
            this.btnSetLibretroCore.Text = "...";
            this.btnSetLibretroCore.UseVisualStyleBackColor = true;
            this.btnSetLibretroCore.Click += new System.EventHandler(this.btnSetLibretroCore_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btnClassicLaunchGame);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Location = new System.Drawing.Point(12, 99);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(277, 100);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "BizHawk Classic";
            // 
            // btnClassicLaunchGame
            // 
            this.btnClassicLaunchGame.Location = new System.Drawing.Point(169, 71);
            this.btnClassicLaunchGame.Name = "btnClassicLaunchGame";
            this.btnClassicLaunchGame.Size = new System.Drawing.Size(102, 23);
            this.btnClassicLaunchGame.TabIndex = 6;
            this.btnClassicLaunchGame.Text = "Launch Game";
            this.btnClassicLaunchGame.UseVisualStyleBackColor = true;
            this.btnClassicLaunchGame.Click += new System.EventHandler(this.btnClassicLaunchGame_Click);
            // 
            // OpenAdvancedChooser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(457, 208);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OpenAdvancedChooser";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Open Advanced";
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button btnLibretroLaunchNoGame;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.Button btnSetLibretroCore;
		private System.Windows.Forms.TextBox txtLibretroCore;
		private System.Windows.Forms.Button btnLibretroLaunchGame;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Button btnClassicLaunchGame;
	}
}