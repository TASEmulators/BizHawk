namespace BizHawk.Client.EmuHawk
{
	partial class RCheevosLeaderboardForm
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
			this.titleLabel = new System.Windows.Forms.Label();
			this.descriptionLabel = new System.Windows.Forms.Label();
			this.titleBox = new System.Windows.Forms.TextBox();
			this.descriptionBox = new System.Windows.Forms.TextBox();
			this.scoreLabel = new System.Windows.Forms.Label();
			this.scoreBox = new System.Windows.Forms.TextBox();
			this.lowerIsBetterBox = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// titleLabel
			// 
			this.titleLabel.AutoSize = true;
			this.titleLabel.Location = new System.Drawing.Point(45, 14);
			this.titleLabel.Name = "titleLabel";
			this.titleLabel.Size = new System.Drawing.Size(30, 13);
			this.titleLabel.TabIndex = 1;
			this.titleLabel.Text = "Title:";
			// 
			// descriptionLabel
			// 
			this.descriptionLabel.AutoSize = true;
			this.descriptionLabel.Location = new System.Drawing.Point(12, 40);
			this.descriptionLabel.Name = "descriptionLabel";
			this.descriptionLabel.Size = new System.Drawing.Size(63, 13);
			this.descriptionLabel.TabIndex = 2;
			this.descriptionLabel.Text = "Description:";
			// 
			// titleBox
			// 
			this.titleBox.Location = new System.Drawing.Point(81, 11);
			this.titleBox.Name = "titleBox";
			this.titleBox.ReadOnly = true;
			this.titleBox.Size = new System.Drawing.Size(411, 20);
			this.titleBox.TabIndex = 3;
			// 
			// descriptionBox
			// 
			this.descriptionBox.Location = new System.Drawing.Point(81, 37);
			this.descriptionBox.Name = "descriptionBox";
			this.descriptionBox.ReadOnly = true;
			this.descriptionBox.Size = new System.Drawing.Size(411, 20);
			this.descriptionBox.TabIndex = 4;
			// 
			// scoreLabel
			// 
			this.scoreLabel.AutoSize = true;
			this.scoreLabel.Location = new System.Drawing.Point(37, 63);
			this.scoreLabel.Name = "scoreLabel";
			this.scoreLabel.Size = new System.Drawing.Size(38, 13);
			this.scoreLabel.TabIndex = 5;
			this.scoreLabel.Text = "Score:";
			// 
			// scoreBox
			// 
			this.scoreBox.Location = new System.Drawing.Point(81, 60);
			this.scoreBox.Name = "scoreBox";
			this.scoreBox.ReadOnly = true;
			this.scoreBox.Size = new System.Drawing.Size(411, 20);
			this.scoreBox.TabIndex = 6;
			// 
			// lowerIsBetterBox
			// 
			this.lowerIsBetterBox.AutoCheck = false;
			this.lowerIsBetterBox.AutoSize = true;
			this.lowerIsBetterBox.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.lowerIsBetterBox.Location = new System.Drawing.Point(392, 86);
			this.lowerIsBetterBox.Name = "lowerIsBetterBox";
			this.lowerIsBetterBox.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.lowerIsBetterBox.Size = new System.Drawing.Size(100, 17);
			this.lowerIsBetterBox.TabIndex = 9;
			this.lowerIsBetterBox.Text = "Lower Is Better:";
			this.lowerIsBetterBox.UseVisualStyleBackColor = true;
			// 
			// RCheevosLeaderboardForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(504, 94);
			this.ControlBox = false;
			this.Controls.Add(this.lowerIsBetterBox);
			this.Controls.Add(this.scoreBox);
			this.Controls.Add(this.scoreLabel);
			this.Controls.Add(this.descriptionBox);
			this.Controls.Add(this.titleBox);
			this.Controls.Add(this.descriptionLabel);
			this.Controls.Add(this.titleLabel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(300, 110);
			this.Name = "RCheevosLeaderboardForm";
			this.ShowIcon = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Label descriptionLabel;
		private System.Windows.Forms.TextBox titleBox;
		private System.Windows.Forms.TextBox descriptionBox;
		private System.Windows.Forms.Label scoreLabel;
		private System.Windows.Forms.TextBox scoreBox;
		private System.Windows.Forms.CheckBox lowerIsBetterBox;
	}
}