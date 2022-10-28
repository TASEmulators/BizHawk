namespace BizHawk.Client.EmuHawk
{
	partial class RCheevosGameInfoForm
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
			this.gameIconBox = new System.Windows.Forms.PictureBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.titleTextBox = new System.Windows.Forms.TextBox();
			this.totalPointsBox = new System.Windows.Forms.TextBox();
			this.currentLboardBox = new System.Windows.Forms.TextBox();
			this.richPresenceBox = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.gameIconBox)).BeginInit();
			this.SuspendLayout();
			// 
			// gameIconBox
			// 
			this.gameIconBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left)));
			this.gameIconBox.Location = new System.Drawing.Point(12, 12);
			this.gameIconBox.Name = "gameIconBox";
			this.gameIconBox.Size = new System.Drawing.Size(100, 100);
			this.gameIconBox.TabIndex = 0;
			this.gameIconBox.TabStop = false;
			// 
			// label2
			// 
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(118, 68);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(107, 13);
			this.label2.TabIndex = 2;
			this.label2.Text = "Current Leaderboard:";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(195, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(30, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Title:";
			// 
			// label3
			// 
			this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(145, 95);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(80, 13);
			this.label3.TabIndex = 3;
			this.label3.Text = "Rich Presence:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(122, 41);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(103, 13);
			this.label5.TabIndex = 5;
			this.label5.Text = "Total Earned Points:";
			// 
			// titleTextBox
			// 
			this.titleTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.titleTextBox.Location = new System.Drawing.Point(231, 12);
			this.titleTextBox.Name = "titleTextBox";
			this.titleTextBox.ReadOnly = true;
			this.titleTextBox.Size = new System.Drawing.Size(261, 20);
			this.titleTextBox.TabIndex = 6;
			// 
			// totalPointsBox
			// 
			this.totalPointsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.totalPointsBox.Location = new System.Drawing.Point(231, 38);
			this.totalPointsBox.Name = "totalPointsBox";
			this.totalPointsBox.ReadOnly = true;
			this.totalPointsBox.Size = new System.Drawing.Size(261, 20);
			this.totalPointsBox.TabIndex = 7;
			// 
			// currentLboardBox
			// 
			this.currentLboardBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.currentLboardBox.Location = new System.Drawing.Point(231, 65);
			this.currentLboardBox.Name = "currentLboardBox";
			this.currentLboardBox.ReadOnly = true;
			this.currentLboardBox.Size = new System.Drawing.Size(261, 20);
			this.currentLboardBox.TabIndex = 8;
			// 
			// richPresenceBox
			// 
			this.richPresenceBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.richPresenceBox.Location = new System.Drawing.Point(231, 92);
			this.richPresenceBox.Name = "richPresenceBox";
			this.richPresenceBox.ReadOnly = true;
			this.richPresenceBox.Size = new System.Drawing.Size(261, 20);
			this.richPresenceBox.TabIndex = 9;
			// 
			// RCheevosGameInfoForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(504, 121);
			this.Controls.Add(this.richPresenceBox);
			this.Controls.Add(this.currentLboardBox);
			this.Controls.Add(this.totalPointsBox);
			this.Controls.Add(this.titleTextBox);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.gameIconBox);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(300, 160);
			this.Name = "RCheevosGameInfoForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Game Info";
			((System.ComponentModel.ISupportInitialize)(this.gameIconBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox gameIconBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox titleTextBox;
		private System.Windows.Forms.TextBox totalPointsBox;
		private System.Windows.Forms.TextBox currentLboardBox;
		private System.Windows.Forms.TextBox richPresenceBox;
	}
}