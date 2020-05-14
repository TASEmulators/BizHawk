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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnMAMELaunchGame = new System.Windows.Forms.Button();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(6, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(198, 45);
            this.label3.TabIndex = 5;
            this.label3.Text = "Load a ROM with the classic BizHawk autodetection method. But why not just use Op" +
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
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(370, 221);
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
            this.txtLibretroCore.AllowDrop = true;
            this.txtLibretroCore.Location = new System.Drawing.Point(81, 23);
            this.txtLibretroCore.Name = "txtLibretroCore";
            this.txtLibretroCore.ReadOnly = true;
            this.txtLibretroCore.Size = new System.Drawing.Size(314, 20);
            this.txtLibretroCore.TabIndex = 6;
            this.txtLibretroCore.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtLibretroCore_DragDrop);
            this.txtLibretroCore.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtLibretroCore_DragEnter);
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
            this.groupBox3.Location = new System.Drawing.Point(235, 99);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(210, 100);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "BizHawk Classic";
            // 
            // btnClassicLaunchGame
            // 
            this.btnClassicLaunchGame.Location = new System.Drawing.Point(102, 71);
            this.btnClassicLaunchGame.Name = "btnClassicLaunchGame";
            this.btnClassicLaunchGame.Size = new System.Drawing.Size(102, 23);
            this.btnClassicLaunchGame.TabIndex = 6;
            this.btnClassicLaunchGame.Text = "Launch Game";
            this.btnClassicLaunchGame.UseVisualStyleBackColor = true;
            this.btnClassicLaunchGame.Click += new System.EventHandler(this.btnClassicLaunchGame_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.btnMAMELaunchGame);
            this.groupBox1.Location = new System.Drawing.Point(13, 99);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(216, 100);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "MAME Arcade";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(6, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(204, 42);
            this.label1.TabIndex = 1;
            this.label1.Text = "Load .zip archive as MAME Arcade ROM (do not unzip)";
            this.label1.Click += new System.EventHandler(this.btnMAMELaunchGame_Click);
            // 
            // btnMAMELaunchGame
            // 
            this.btnMAMELaunchGame.Location = new System.Drawing.Point(108, 71);
            this.btnMAMELaunchGame.Name = "btnMAMELaunchGame";
            this.btnMAMELaunchGame.Size = new System.Drawing.Size(102, 23);
            this.btnMAMELaunchGame.TabIndex = 0;
            this.btnMAMELaunchGame.Text = "Launch Game";
            this.btnMAMELaunchGame.UseVisualStyleBackColor = true;
            this.btnMAMELaunchGame.Click += new System.EventHandler(this.btnMAMELaunchGame_Click);
            // 
            // OpenAdvancedChooser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(457, 256);
            this.Controls.Add(this.groupBox1);
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
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

		}



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
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button btnMAMELaunchGame;
	}
}